$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $PSScriptRoot '../../dependencies/BepInEx/BepInEx/core/Mono.Cecil.dll')
$assembly=[Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $PSScriptRoot '../../src/bin/Release/net472/CluckmechCoop.dll'))
function Get-ModType([string]$name){$assembly.MainModule.Types | Where-Object Name -EQ $name}
function Calls($method,[string]$name){@($method.Body.Instructions | Where-Object {$_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.Name -eq $name})}
try {
    $transfer=(Get-ModType 'KamuiWorld').Methods | Where-Object Name -EQ 'Transfer'
    $invalidate=@(Calls $transfer 'ChangingSpace')[0]
    $position=@(Calls $transfer 'set_position')[0]
    if(-not $invalidate -or -not $position -or $invalidate.Offset -ge $position.Offset){throw 'Transfer must invalidate actions before moving the unit'}
    if(-not (Calls $transfer 'MovePosition')){throw 'Transfer must replace any pending Rigidbody pull target'}
    foreach($entry in @(@('OtherworldActions','Pull'),@('OtherworldActions','DelayedBombard'),@('OtherworldActions','OrbitalImpact'),@('OtherworldActions','Flame'),@('KamuiWorld','DashRoutine'))){
        $iterator=(Get-ModType $entry[0]).NestedTypes | Where-Object Name -Like "<$($entry[1])>*"
        $move=$iterator.Methods | Where-Object Name -EQ 'MoveNext'
        if(-not (Calls $move 'Capture') -or -not (Calls $move 'Current')){throw "Missing action lease: $($entry[1])"}
        if($entry[1] -eq 'Pull'){
            if(@(Calls $move 'Current')[0].Offset -ge @(Calls $move 'MovePosition')[0].Offset){throw 'Pull writes position before checking cancellation'}
        }
    }
    $removed=(Get-ModType 'UnitLifecycle').Methods | Where-Object Name -EQ 'Removed'
    foreach($name in @('Invalidate','Remove','Removed')){if(-not (Calls $removed $name)){throw "Lifecycle removal missing $name"}}
    foreach($entry in @(@('RemoveStratagemEnemy','Prefix'),@('OtherworldUnit','OnDisable'))){
        $method=(Get-ModType $entry[0]).Methods | Where-Object Name -EQ $entry[1]
        if(-not (Calls $method 'Removed')){throw "Native/unit removal bypasses common lifecycle: $($entry[0])"}
    }
    'PASS: compiled transfer, pending physics move, five coroutines and removal lifecycle wiring'
} finally {$assembly.Dispose()}
