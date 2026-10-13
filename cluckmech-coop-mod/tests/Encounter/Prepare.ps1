$ErrorActionPreference='Stop'
$source=[System.IO.File]::ReadAllText((Join-Path $PSScriptRoot '../../src/Difficulty.cs'),[System.Text.Encoding]::UTF8)
function Extract([string]$marker) {
 $start=$source.IndexOf($marker)
 if($start -lt 0){throw "Missing production method: $marker"}
 $end=$source.IndexOf('{',$start)+1;$depth=1
 while($depth -gt 0){if($source[$end] -eq '{'){$depth++};if($source[$end] -eq '}'){$depth--};$end++}
 $source.Substring($start,$end-$start)
}
$methods=@(
 (Extract '        internal static EncounterSession State('),
 (Extract '        internal static bool CountsForEncounter('),
 (Extract '        internal static void Generated('),
 (Extract '        internal static void Mark('),
 (Extract '        private static bool HasLiving(')
)
$classes=@((Extract '    public sealed class EncounterEnemy'),(Extract '    internal static class MarkEnemy'))
$header=@'
using System.Collections.Generic;
using UnityEngine;
namespace CluckmechCoop {
internal static class Difficulty {
 private static readonly Dictionary<刷怪器,EncounterSession> rounds=new();
 internal static 刷怪器 Spawning;
 internal static readonly Dictionary<模块_攻击属性,Strength> Attacks=new();
 private static bool IsFinalBoss(GameObject unit)=>unit.GetComponent<最终boss状态机>()!=null;
'@
$body=$header+($methods -join "`n")+"`n}`n"+($classes -join "`n")+"`n}"
New-Item -ItemType Directory (Join-Path $PSScriptRoot 'obj') -Force | Out-Null
[System.IO.File]::WriteAllText((Join-Path $PSScriptRoot 'obj/EncounterMethods.cs'),$body,[System.Text.UTF8Encoding]::new($false))
