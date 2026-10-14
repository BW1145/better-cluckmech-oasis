$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
function Read-Block([string]$source,[string]$marker) {
    $start = $source.IndexOf($marker)
    if ($start -lt 0) { throw "Missing production block: $marker" }
    $end = $source.IndexOf('{',$start)+1; $depth = 1
    while ($depth -gt 0) { if ($source[$end] -eq '{') { $depth++ }; if ($source[$end] -eq '}') { $depth-- }; $end++ }
    $source.Substring($start,$end-$start)
}
$damage = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot '../../src/StratagemWorld.cs'),[System.Text.Encoding]::UTF8)
$fields = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot '../../src/StratagemFields.cs'),[System.Text.Encoding]::UTF8)
$native = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot '../../../cluckmech-research/inspection/game/模块_状态属性.cs'),[System.Text.Encoding]::UTF8)
$output = 'using System; using System.Collections.Generic; using System.Reflection; using UnityEngine; using HarmonyLib; namespace CluckmechCoop {' +
    (Read-Block $damage '    internal sealed class StratagemDamage') +
    (Read-Block $fields '    internal sealed class StratagemStateHandle') + '}' +
    'public partial class 模块_状态属性 {' + (Read-Block $native 'public class 状态类') +
    (Read-Block $native 'public void 增加状态(') + (Read-Block $native 'public bool 查询状态(') + '}'
New-Item -ItemType Directory -Path (Join-Path $PSScriptRoot 'obj') -Force | Out-Null
[System.IO.File]::WriteAllText((Join-Path $PSScriptRoot 'obj/ProductionEffects.cs'),$output,[System.Text.UTF8Encoding]::new($false))
