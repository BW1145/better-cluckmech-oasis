param([string]$GameDir = $env:CLUCKMECH_GAME_DIR)
if ([string]::IsNullOrWhiteSpace($GameDir)) { throw 'Specify -GameDir or set CLUCKMECH_GAME_DIR to the game installation directory' }
$ErrorActionPreference = 'Stop'
$OutputEncoding = [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new()
Add-Type -Path (Join-Path $PSScriptRoot '..\dependencies\BepInEx\BepInEx\core\Mono.Cecil.dll')
$managedDir = Join-Path $GameDir 'Cluckmech Oasis_Data/Managed'
function Read-AllTypes($types) {
    foreach ($type in $types) { $type; if ($type.HasNestedTypes) { Read-AllTypes $type.NestedTypes } }
}
$destroyCalls = [System.Collections.Generic.List[object]]::new()
$initializers = [System.Collections.Generic.List[object]]::new()
foreach ($file in Get-ChildItem -LiteralPath $managedDir -Filter '*.dll') {
    $assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($file.FullName)
    try {
        foreach ($type in Read-AllTypes $assembly.MainModule.Types) {
            foreach ($method in $type.Methods) {
                if (-not $method.HasBody) { continue }
                $runtimeAttributes = @($method.CustomAttributes | Where-Object { $_.AttributeType.Name -eq 'RuntimeInitializeOnLoadMethodAttribute' })
                if ($runtimeAttributes.Count) {
                    $initializers.Add([pscustomobject]@{ Assembly=$file.Name; Method=$method.FullName; Stage=($runtimeAttributes.ConstructorArguments.Value -join ',') })
                }
                foreach ($instruction in $method.Body.Instructions) {
                    if ($instruction.OpCode.Name -notin @('call','callvirt')) { continue }
                    $called = $instruction.Operand
                    if ($called.DeclaringType.FullName -ne 'UnityEngine.Object' -or $called.Name -notin @('Destroy','DestroyImmediate')) { continue }
                    $destroyCalls.Add([pscustomobject]@{
                        Assembly=$file.Name
                        Method=$method.FullName
                        Startup=($runtimeAttributes.Count -gt 0 -or $method.Name -in @('Awake','.cctor'))
                        Call=$called.FullName
                        IL=('IL_{0:X4}' -f $instruction.Offset)
                    })
                }
            }
        }
    } finally { $assembly.Dispose() }
}
$report = [pscustomobject]@{ DestroyCalls=$destroyCalls; RuntimeInitializers=$initializers }
$reportPath = Join-Path $PSScriptRoot '..\evidence\startup-destruction\managed-call-audit.json'
[System.IO.File]::WriteAllText($reportPath, ($report | ConvertTo-Json -Depth 6), [System.Text.UTF8Encoding]::new($false))
"Scanned $((Get-ChildItem -LiteralPath $managedDir -Filter '*.dll').Count) managed assemblies; $($destroyCalls.Count) Destroy calls; $($initializers.Count) runtime initializers"
$destroyCalls | Where-Object Startup | Select-Object Assembly,Method,IL | Format-Table -AutoSize
