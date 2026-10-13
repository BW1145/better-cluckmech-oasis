$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$source = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot '../../src/Difficulty.cs'), [System.Text.Encoding]::UTF8)
$methods = foreach ($name in @('RememberBar', 'RefreshBar')) {
    $start = $source.IndexOf("        internal static void $name(")
    if ($start -lt 0) { throw "Production method missing: $name" }
    $brace = $source.IndexOf('{', $start)
    $depth = 1
    $end = $brace + 1
    while ($depth -gt 0) {
        if ($source[$end] -eq '{') { $depth++ }
        if ($source[$end] -eq '}') { $depth-- }
        $end++
    }
    $source.Substring($start, $end - $start)
}
$body = "using System.Collections.Generic;`nnamespace CluckmechCoop { internal static class Difficulty { private static readonly Dictionary<BossBloodstrip,float> barMaximums = new Dictionary<BossBloodstrip,float>();`n" + ($methods -join "`n") + "`n} }"
$gateStart = $source.IndexOf('    internal static class BossBarReadyGate')
$gateEnd = $source.IndexOf('    [HarmonyPatch]', $gateStart)
$body += "`nnamespace CluckmechCoop { " + $source.Substring($gateStart, $gateEnd-$gateStart) + " }"
New-Item -ItemType Directory -Path (Join-Path $PSScriptRoot 'obj') -Force | Out-Null
[System.IO.File]::WriteAllText((Join-Path $PSScriptRoot 'obj/BossBarMethods.cs'), $body, [System.Text.UTF8Encoding]::new($false))
