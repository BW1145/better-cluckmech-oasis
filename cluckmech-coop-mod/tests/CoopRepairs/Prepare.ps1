param([Parameter(Mandatory=$true)][string]$NativeSource)
$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[Text.UTF8Encoding]::new($false)
function Block([string]$source,[string]$marker){
    $start=$source.IndexOf($marker);if($start -lt 0){throw "Missing source block: $marker"}
    $end=$source.IndexOf('{',$start)+1;$depth=1
    while($depth -gt 0){if($source[$end] -eq '{'){$depth++};if($source[$end] -eq '}'){$depth--};$end++}
    $source.Substring($start,$end-$start)
}
function Read([string]$path){[IO.File]::ReadAllText($path,[Text.Encoding]::UTF8)}
$head='using Newtonsoft.Json;using System;using System.Collections.Generic;using UnityEngine;using Mirror;using HarmonyLib;using 函数库;using Random=UnityEngine.Random;using Object=UnityEngine.Object;'
$out=$head+'public class 世界类 {'+(Block (Read (Join-Path $NativeSource '世界类.cs')) 'public class 伤害数据包')+'}'
$native=Read (Join-Path $NativeSource '模块_受伤.cs')
$out+='public partial class 模块_受伤 {'
foreach($marker in @('public void 实现伤害(','public void 受到伤害(float ','public void 受到伤害(模块_状态持续效果.','public void 受到伤害(世界类.')){$out+=Block $native $marker}
$out+='}'
$out+='namespace 函数库 {public class 词条函数 {public class 召唤 {'+(Block (Read (Join-Path $NativeSource '函数库/词条函数.cs')) 'public static GameObject 召唤衍生物(')+'}}}'
foreach($pair in @(@('词条_100134_钢铁蜗牛.cs','SnailNative'),@('词条_194_滚烫步伐.cs','HotStepNative'))){
    $out+='public class '+$pair[1]+':Component {public GameObject 自身=>gameObject;public GameObject 生成物;public 模块_基本属性 基本属性;public float 计时器=2.5f;private float 当前计时器=2.5f;public void Tick()=>生成生成物();'+(Block (Read (Join-Path $NativeSource $pair[0])) 'private void 生成生成物()')+'}'
}
$mod=Join-Path $PSScriptRoot '../../src'
$out+='namespace CluckmechCoop {'
$out+=Block (Read (Join-Path $mod 'OtherworldModifiers.cs')) 'internal static class OtherworldDirectDamage'
$out+=Block (Read (Join-Path $mod 'OtherworldModifiers.cs')) 'internal static class OtherworldFlatDamage'
$out+=Block (Read (Join-Path $mod 'OtherworldModifiers.cs')) 'internal static class OtherworldStatusDamage'
foreach($marker in @('internal sealed class KamuiSavedUnit','internal sealed class KamuiSavedStatus','internal sealed class KamuiSavedRoom','internal sealed class KamuiSavedDrop')){$out+=Block (Read (Join-Path $mod 'KamuiSaveValidation.cs')) $marker}
$out+=Block (Read (Join-Path $mod 'OtherworldProtocol.cs')) 'internal struct OtherworldMessage'
$out+='internal static partial class OtherworldNetwork {'+(Block (Read (Join-Path $mod 'OtherworldNetwork.cs')) 'internal static void SendDisplay(')+'}'
$out+='}'
New-Item -ItemType Directory -Path (Join-Path $PSScriptRoot 'obj') -Force | Out-Null
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'obj/Production.cs'),$out,[Text.UTF8Encoding]::new($false))
Write-Output 'Prepared private native methods and current production guards/schema; Unity, display and network use test substitutes.'
