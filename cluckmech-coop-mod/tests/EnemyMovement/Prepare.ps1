$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$source = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot '../../src/Difficulty.cs'), [System.Text.Encoding]::UTF8)
function Extract-Block([string]$marker) {
    $start = $source.IndexOf($marker)
    if ($start -lt 0) { throw "Production source missing: $marker" }
    $end = $source.IndexOf('{', $start) + 1
    $depth = 1
    while ($depth -gt 0) {
        if ($source[$end] -eq '{') { $depth++ }
        if ($source[$end] -eq '}') { $depth-- }
        $end++
    }
    $source.Substring($start, $end - $start)
}
$methods = @(
    (Extract-Block '        internal static EncounterSession State('),
    (Extract-Block '        internal static void CaptureBossReward('),
    (Extract-Block '        internal static void Playback('),
    (Extract-Block '        private static bool TryPlayback('),
    (Extract-Block '        private static void ReleaseBossCompletion(')
)
$getters = [regex]::Match($source, '(?s)        internal static float PlaybackCountdown\(.*?(?=        private static void ShowPlayback)').Value
if (-not $getters) { throw 'Production countdown readers missing' }
$methods += $getters
$reward = Extract-Block '    internal sealed class BossReward'
$gate = Extract-Block '    internal static class BossRewardGate'
$header = @'
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CluckmechCoop {
internal sealed class EncounterSession {
 internal readonly EncounterPlan<GameObject> Plan = new EncounterPlan<GameObject>();
 internal readonly DeferredOnce<BossReward> Reward = new DeferredOnce<BossReward>();
 internal readonly DeferredOnce<GameObject> BossWorld = new DeferredOnce<GameObject>();
 internal bool Replay, CountdownWaiting;
 internal int ReplayStage;
 internal float CountdownUntil;
}
internal static class Difficulty {
 private static readonly Dictionary<刷怪器,EncounterSession> rounds = new Dictionary<刷怪器,EncounterSession>();
 private static bool clientComplete;
 internal static void Complete() { State(Plugin.Spawner).Plan.Complete(); State(Plugin.Spawner).Replay = false; clientComplete = true; ReleaseBossCompletion(State(Plugin.Spawner)); }
'@
$generated = $header + ($methods -join "`n") + "`n}`n" + $reward + "`n" + $gate + "`n}"
New-Item -ItemType Directory -Path (Join-Path $PSScriptRoot 'obj') -Force | Out-Null
[System.IO.File]::WriteAllText((Join-Path $PSScriptRoot 'obj/CompletionMethods.cs'), $generated, [System.Text.UTF8Encoding]::new($false))
$nativeUi = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot '../../../cluckmech-research/inspection/game/模块_波次倒计时UI.cs'), [System.Text.Encoding]::UTF8)
foreach ($read in @(@('倒计时', 'PlaybackCountdown'), @('当前波次', 'PlaybackStage'), @('是否计时', 'PlaybackCounting'), @('关卡状态', 'PlaybackPhase'))) {
    $nativeUi = $nativeUi.Replace("波次控制器.$($read[0])", "CluckmechCoop.Difficulty.$($read[1])(波次控制器)")
}
[System.IO.File]::WriteAllText((Join-Path $PSScriptRoot 'obj/CountdownUi.cs'), $nativeUi, [System.Text.UTF8Encoding]::new($false))
