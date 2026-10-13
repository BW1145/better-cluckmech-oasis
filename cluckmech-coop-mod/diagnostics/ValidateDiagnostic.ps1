param([string]$GameDir = $env:CLUCKMECH_GAME_DIR)
if ([string]::IsNullOrWhiteSpace($GameDir)) { throw 'Specify -GameDir or set CLUCKMECH_GAME_DIR to the game installation directory' }
$ErrorActionPreference = 'Stop'
$OutputEncoding = [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new()
Add-Type -Path (Join-Path $PSScriptRoot '..\dependencies\BepInEx\BepInEx\core\Mono.Cecil.dll')
$diagnosticPath = Join-Path $PSScriptRoot 'bin\Release\net472\CluckmechCoopDiagnostics.dll'
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($diagnosticPath)
try {
    $allTypes = @($assembly.MainModule.Types)
    foreach ($type in $assembly.MainModule.Types) { $allTypes += @($type.NestedTypes) }
    $instructions = @($allTypes.Methods | Where-Object HasBody | ForEach-Object { $_.Body.Instructions })
    $gameWrites = @($instructions | Where-Object { $_.OpCode.Name -in @('stfld','stsfld') -and $_.Operand.DeclaringType.Scope.Name -eq 'Assembly-CSharp' })
    if ($gameWrites.Count) { throw 'Diagnostic writes game fields' }
    $prefixType = $allTypes | Where-Object FullName -EQ 'CluckmechCoopDiagnostics.DestructionTrace'
    $prefix = $prefixType.Methods | Where-Object Name -EQ 'BeforeDestroy'
    if ($prefix.ReturnType.FullName -ne 'System.Void' -or @($prefix.Parameters | Where-Object { $_.ParameterType.IsByReference }).Count) {
        throw 'Destroy observer may cancel or replace original arguments'
    }
    $mutations = @($prefix.Body.Instructions | Where-Object {
        $_.OpCode.Name -in @('call','callvirt') -and $_.Operand.DeclaringType.FullName -eq 'UnityEngine.Object' -and $_.Operand.Name -in @('Destroy','DestroyImmediate')
    })
    if ($mutations.Count) { throw 'Destroy observer invokes destruction itself' }
    'PASS: diagnostic writes no game fields; destroy observer preserves the original call and its arguments'
} finally { $assembly.Dispose() }
$corePath = Join-Path $GameDir 'Cluckmech Oasis_Data/Managed/UnityEngine.CoreModule.dll'
$core = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($corePath)
try {
    $objectType = $core.MainModule.Types | Where-Object FullName -EQ 'UnityEngine.Object'
    foreach ($name in @('Destroy','DestroyImmediate')) {
        $method = @($objectType.Methods | Where-Object { $_.Name -eq $name -and $_.Parameters.Count -eq 1 -and $_.Parameters[0].ParameterType.FullName -eq 'UnityEngine.Object' })
        if ($method.Count -ne 1 -or -not $method[0].HasBody) { throw "Managed observer binding unavailable: $name" }
    }
    'PASS: both observed destruction entry points have managed method bodies in the installed Unity version'
} finally { $core.Dispose() }
