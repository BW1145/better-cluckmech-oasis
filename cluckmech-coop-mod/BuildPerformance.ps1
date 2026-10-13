$ErrorActionPreference = 'Stop'
$OutputEncoding = [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Add-Type -Path (Join-Path $PSScriptRoot 'dependencies\BepInEx\BepInEx\core\Mono.Cecil.dll')
$inputDll = Join-Path $PSScriptRoot 'backups\0.1.0\CluckmechCoop.dll'
$outputDir = Join-Path $PSScriptRoot 'performance\bin'
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($inputDll)
try {
    $panel = $assembly.MainModule.Types | Where-Object FullName -EQ 'CluckmechCoop.CoopPanel'
    $update = $panel.Methods | Where-Object Name -EQ 'Update'
    $instructions = $update.Body.Instructions
    $clockCalls = @($instructions | Where-Object { $_.OpCode.Name -eq 'call' -and $_.Operand.FullName -eq 'System.Single UnityEngine.Time::get_realtimeSinceStartup()' })
    if ($clockCalls.Count -ne 2) { throw 'Unexpected panel refresh layout' }
    $first = $instructions.IndexOf($clockCalls[0])
    $last = $instructions.IndexOf(@($instructions | Where-Object { $_.OpCode.Name -eq 'stfld' -and $_.Operand.Name -eq 'refreshAt' })[0])
    $gate = @($instructions | Select-Object -Skip $first -First ($last - $first + 1))
    $after = $instructions[$last + 1]
    foreach ($instruction in $instructions) {
        if ($instruction.Operand -is [Mono.Cecil.Cil.Instruction] -and $instruction.Operand -eq $gate[0] -and $instruction -notin $gate) {
            $instruction.Operand = $after
        }
    }
    $originalStart = $instructions[0]
    $processor = $update.Body.GetILProcessor()
    foreach ($instruction in $gate) { $processor.Remove($instruction) }
    foreach ($instruction in $gate) { $processor.InsertBefore($originalStart, $instruction) }
    # Widen moved branches so instruction offsets remain valid.
    $opcodes = @{}
    foreach ($field in [Mono.Cecil.Cil.OpCodes].GetFields()) {
        $opcode = $field.GetValue($null)
        $opcodes[$opcode.Name] = $opcode
    }
    foreach ($instruction in $instructions) {
        if ($instruction.OpCode.OperandType -eq [Mono.Cecil.Cil.OperandType]::ShortInlineBrTarget) {
            $instruction.OpCode = $opcodes[$instruction.OpCode.Name.Substring(0, $instruction.OpCode.Name.Length - 2)]
        }
    }
    $assembly.Name.Version = [Version]'0.1.2.0'
    $plugin = $assembly.MainModule.Types | Where-Object FullName -EQ 'CluckmechCoop.Plugin'
    $attribute = $plugin.CustomAttributes | Where-Object { $_.AttributeType.FullName -eq 'BepInEx.BepInPlugin' }
    $attribute.ConstructorArguments[2] = [Mono.Cecil.CustomAttributeArgument]::new($assembly.MainModule.TypeSystem.String, '0.1.2')
    foreach ($type in $assembly.MainModule.Types) {
        foreach ($method in $type.Methods | Where-Object HasBody) {
            foreach ($instruction in $method.Body.Instructions) {
                if ($instruction.OpCode.Name -eq 'ldstr' -and $instruction.Operand -is [string]) {
                    if ($instruction.Operand -eq '0.1.0') { $instruction.Operand = '0.1.2' }
                    elseif ($instruction.Operand -eq '强化远征与队友转交 0.1.0 loaded') { $instruction.Operand = '强化远征与队友转交 0.1.2 loaded' }
                }
            }
        }
    }
    $assembly.Write((Join-Path $outputDir 'CluckmechCoop.dll'))
} finally { $assembly.Dispose() }
'Built performance update 0.1.2 from installed gameplay baseline'
