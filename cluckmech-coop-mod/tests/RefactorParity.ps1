param([Parameter(Mandatory=$true)][string]$BaselineAssembly)
$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $PSScriptRoot '../dependencies/BepInEx/BepInEx/core/Mono.Cecil.dll')
$before=[Mono.Cecil.AssemblyDefinition]::ReadAssembly((Resolve-Path -LiteralPath $BaselineAssembly).Path)
$after=[Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $PSScriptRoot '../src/bin/Release/net472/CluckmechCoop.dll'))
$checks=0
function Normalize([string]$value){$value -replace 'd__\d+','d__iterator'}
function Types($types){foreach($type in $types){$type;Types $type.NestedTypes}}
function Attributes($owner){
    @($owner.CustomAttributes | Where-Object {$_.AttributeType.FullName -like '*Harmony*' -or $_.AttributeType.FullName -like 'Newtonsoft.Json.*' -or $_.AttributeType.FullName -eq 'UnityEngine.DefaultExecutionOrder'} | ForEach-Object {
        $_.Constructor.FullName+':'+[Convert]::ToBase64String($_.GetBlob())
    }) -join '|'
}
function Body($method){
    if(-not $method.HasBody){return ''}
    $code=$method.Body.Instructions
    $lines=foreach($instruction in $code){
        $operand=$instruction.Operand
        $value=if($operand -is [Mono.Cecil.Cil.Instruction]){'@'+$code.IndexOf($operand)}
        elseif($operand -is [Mono.Cecil.Cil.Instruction[]]){(@($operand | ForEach-Object {'@'+$code.IndexOf($_)}) -join ',')}
        elseif($operand -is [Mono.Cecil.MemberReference]){
            # The shared helper has the original implementation; callers retain order.
            (Normalize $operand.FullName).Replace('CluckmechCoop.StratagemWorld::Id(', 'CluckmechCoop.NetworkObjects::Id(')
        }
        elseif($operand -is [string]){$operand.Replace($before.Name.Version.ToString(3),'<release-version>').Replace($after.Name.Version.ToString(3),'<release-version>')}
        else{[string]$operand}
        $instruction.OpCode.Name+':'+$value
    }
    $locals=@($method.Body.Variables | ForEach-Object {Normalize $_.VariableType.FullName}) -join ','
    $handlers=@($method.Body.ExceptionHandlers | ForEach-Object {
        [string]$_.HandlerType+':'+(Normalize $_.CatchType.FullName)+':'+$code.IndexOf($_.TryStart)+':'+$code.IndexOf($_.TryEnd)+':'+$code.IndexOf($_.HandlerStart)+':'+$code.IndexOf($_.HandlerEnd)+':'+$code.IndexOf($_.FilterStart)
    }) -join '|'
    $locals+"`n"+$handlers+"`n"+($lines -join "`n")
}
function Check([bool]$ok,[string]$name){if(-not $ok){throw $name};$script:checks++}
try {
    $current=@{}
    foreach($type in (Types $after.MainModule.Types)){$current[(Normalize $type.FullName)]=$type}
    $forwards=@{
        'CluckmechCoop.StratagemWorld::Id'='CluckmechCoop.NetworkObjects'
        'CluckmechCoop.OtherworldDataValidation::Finite'='CluckmechCoop.NetworkValues'
        'CluckmechCoop.NetworkService::Finite'='CluckmechCoop.NetworkValues'
    }
    foreach($oldType in (Types $before.MainModule.Types)){
        $newType=$current[(Normalize $oldType.FullName)]
        Check ($null -ne $newType) "Missing type: $($oldType.FullName)"
        Check ($oldType.Attributes -eq $newType.Attributes -and (Normalize $oldType.BaseType.FullName) -eq (Normalize $newType.BaseType.FullName)) "Changed type: $($oldType.FullName)"
        Check ((Attributes $oldType) -eq (Attributes $newType)) "Changed type attributes: $($oldType.FullName)"
        $oldFields=@($oldType.Fields | ForEach-Object {(Normalize $_.FullName)+':'+$_.Attributes+':'+(Attributes $_)})
        $newFields=@($newType.Fields | ForEach-Object {(Normalize $_.FullName)+':'+$_.Attributes+':'+(Attributes $_)})
        Check (($oldFields -join '|') -ceq ($newFields -join '|')) "Changed fields/order: $($oldType.FullName)"
        foreach($old in $oldType.Methods){
            $signature=Normalize $old.FullName
            $new=@($newType.Methods | Where-Object {(Normalize $_.FullName) -ceq $signature})
            Check ($new.Count -eq 1) "Changed method signature: $signature"
            $new=$new[0]
            Check ($old.Attributes -eq $new.Attributes -and (Attributes $old) -eq (Attributes $new)) "Changed method attributes: $signature"
            $forward=$forwards[$oldType.FullName+'::'+$old.Name]
            if($forward){
                $helper=@($current[$forward].Methods | Where-Object Name -EQ $old.Name)[0]
                Check ((Body $old) -ceq (Body $helper)) "Changed extracted helper: $signature"
                $calls=@($new.Body.Instructions | Where-Object OpCode -Match '^call$')
                Check ($new.Body.Instructions.Count -eq 3 -and $calls.Count -eq 1 -and $calls[0].Operand.FullName -eq $helper.FullName) "Unexpected forwarding behavior: $signature"
            }else{
                $oldBody=Body $old;$newBody=Body $new
                if($oldBody -cne $newBody){Compare-Object ($oldBody -split "`n") ($newBody -split "`n") | Format-Table | Out-Host}
                Check ($oldBody -ceq $newBody) "Changed instructions/order: $signature"
            }
        }
        Check ($oldType.Methods.Count -eq $newType.Methods.Count) "Added methods in existing type: $($oldType.FullName)"
    }
    $ids=$current['CluckmechCoop.OtherworldTalentIds']
    $expected=@(26001..26008)+@(26101..26124)+@(26100)
    Check ((@($ids.Fields | ForEach-Object Constant | Sort-Object) -join ',') -eq (($expected | Sort-Object) -join ',')) 'Published talent IDs changed'
    foreach($resource in $before.MainModule.Resources){
        $next=@($after.MainModule.Resources | Where-Object Name -CEQ $resource.Name)
        Check ($next.Count -eq 1 -and [Convert]::ToBase64String($resource.GetResourceData()) -ceq [Convert]::ToBase64String($next[0].GetResourceData())) "Changed embedded resource: $($resource.Name)"
    }
    "PASS: $checks compiled comparisons of method instructions/order, fields, patch/JSON attributes, helper extraction, talent IDs and embedded resources"
} finally {$before.Dispose();$after.Dispose()}
