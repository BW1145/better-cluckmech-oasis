$ErrorActionPreference = 'Stop'
$OutputEncoding = [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Add-Type -Path (Join-Path $PSScriptRoot 'dependencies\BepInEx\BepInEx\core\Mono.Cecil.dll')
$baseline = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $PSScriptRoot 'backups\0.1.0\CluckmechCoop.dll'))
$updated = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $PSScriptRoot 'performance\bin\CluckmechCoop.dll'))
function Get-MethodBodySignature($method) {
    @($method.Body.Instructions | ForEach-Object {
        $operand = [string]$_.Operand
        if ($_.OpCode.Name -eq 'ldstr') { $operand = $operand.Replace('0.1.2', '0.1.0') }
        "$($_.OpCode.Name)|$operand"
    }) -join "`n"
}
try {
    $checked = 0
    foreach ($type in $baseline.MainModule.Types) {
        $newType = @($updated.MainModule.Types | Where-Object FullName -EQ $type.FullName)
        if ($newType.Count -ne 1) { throw "Type changed: $($type.FullName)" }
        foreach ($method in $type.Methods | Where-Object HasBody) {
            $newMethod = @($newType[0].Methods | Where-Object FullName -EQ $method.FullName)
            if ($newMethod.Count -ne 1) { throw "Method changed: $($method.FullName)" }
            if ($type.FullName -eq 'CluckmechCoop.CoopPanel' -and $method.Name -eq 'Update') { continue }
            if ((Get-MethodBodySignature $method) -ne (Get-MethodBodySignature $newMethod[0])) { throw "Gameplay method changed: $($method.FullName)" }
            $checked++
        }
    }
    $panel = $updated.MainModule.Types | Where-Object FullName -EQ 'CluckmechCoop.CoopPanel'
    $body = ($panel.Methods | Where-Object Name -EQ 'Update').Body.Instructions
    if ($body[0].Operand.FullName -ne 'System.Single UnityEngine.Time::get_realtimeSinceStartup()' -or
        $body[2].Operand.Name -ne 'refreshAt' -or $body[3].OpCode.Name -ne 'bge.un' -or $body[4].OpCode.Name -ne 'ret') {
        throw 'Panel work is reachable before refresh interval check'
    }
    if ($body[3].Operand -ne $body[5] -or $body[7].OpCode.Name -ne 'ldc.r4' -or
        [math]::Abs($body[7].Operand - 0.3) -gt 0.00001 -or $body[9].Operand.Name -ne 'refreshAt') {
        throw 'Panel refresh deadline update changed'
    }
    $gate = @($body | Select-Object -First 10)
    foreach ($instruction in @($body | Select-Object -Skip 10)) {
        if ($instruction.Operand -is [Mono.Cecil.Cil.Instruction] -and $instruction.Operand -in $gate) { throw 'Panel loops into refresh gate' }
    }
    foreach ($instruction in $body) {
        if ($instruction.Operand -is [Mono.Cecil.Cil.Instruction] -and -not $body.Contains($instruction.Operand)) { throw 'Invalid branch target' }
    }
    $plugin = $updated.MainModule.Types | Where-Object FullName -EQ 'CluckmechCoop.Plugin'
    $attribute = $plugin.CustomAttributes | Where-Object { $_.AttributeType.FullName -eq 'BepInEx.BepInPlugin' }
    $message = $updated.MainModule.Types | Where-Object FullName -EQ 'CluckmechCoop.Message'
    $versions = @(($message.Methods | Where-Object Name -EQ '.ctor').Body.Instructions | Where-Object { $_.OpCode.Name -eq 'ldstr' -and $_.Operand -eq '0.1.2' })
    if ($attribute.ConstructorArguments[2].Value -ne '0.1.2' -or $versions.Count -ne 1) { throw 'Network and plugin versions differ' }
    "PASS: $checked method bodies preserve gameplay and transfers; panel searches gated at 0.3 seconds; network version matches 0.1.2"
} finally { $baseline.Dispose(); $updated.Dispose() }
$diagnostic = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $PSScriptRoot 'diagnostics\bin\Release\net472\CluckmechCoopDiagnostics.dll'))
try {
    $probe = $diagnostic.MainModule.Types | Where-Object FullName -EQ 'CluckmechCoopDiagnostics.Probe'
    foreach ($name in @('Update', 'Snapshot')) {
        $body = ($probe.Methods | Where-Object Name -EQ $name).Body.Instructions
        $guard = @($body | Where-Object { $_.OpCode.Name -eq 'ldfld' -and $_.Operand.Name -eq 'DetailedScan' })
        $index = $body.IndexOf($guard[0])
        if ($guard.Count -ne 1 -or $body[$index - 1].OpCode.Name -ne 'ldarg.0' -or
            $body[$index + 1].OpCode.Name -notin @('brtrue.s', 'brtrue') -or $body[$index + 2].OpCode.Name -ne 'ret') {
            throw "Diagnostic detailed scanning lacks entry gate: $name"
        }
        foreach ($instruction in @($body | Select-Object -First $index)) {
            if ($instruction.OpCode.Name -in @('call', 'callvirt') -and $instruction.Operand.DeclaringType.Namespace -eq 'UnityEngine') {
                throw "Unity scene scanning precedes diagnostic gate: $name"
            }
        }
    }
    $entry = $diagnostic.MainModule.Types | Where-Object FullName -EQ 'CluckmechCoopDiagnostics.DiagnosticPlugin'
    $body = ($entry.Methods | Where-Object Name -EQ 'Awake').Body.Instructions
    $bind = @($body | Where-Object { $_.OpCode.Name -eq 'callvirt' -and $_.Operand.Name -eq 'Bind' })
    if ($bind.Count -ne 1 -or $body[$body.IndexOf($bind[0]) - 2].OpCode.Name -ne 'ldc.i4.0') { throw 'Detailed scanning default is not disabled' }
    'PASS: detailed scanning disabled by default; diagnostic update and snapshots skip scanning in lightweight mode'
} finally { $diagnostic.Dispose() }
