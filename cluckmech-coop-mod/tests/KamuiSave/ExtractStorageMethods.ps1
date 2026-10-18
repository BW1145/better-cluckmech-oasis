param([Parameter(Mandatory=$true)][string]$OutputPath)
$ErrorActionPreference='Stop'
$source=Get-Content -LiteralPath (Join-Path $PSScriptRoot '../../src/ModuleSettings.cs') -Raw -Encoding UTF8
$methods=foreach($signature in @('private static ES3Settings SaveSettings()','internal static void Save(存档脚本 save)','internal static void Load(存档脚本 save)','private static T ReadAddon<T>(')) {
    $start=$source.IndexOf($signature,[StringComparison]::Ordinal)
    if($start -lt 0){throw "Production save method not found: $signature"}
    $open=$source.IndexOf('{',$start);$depth=1;$end=$open+1
    while($depth -gt 0 -and $end -lt $source.Length){if($source[$end] -eq '{'){$depth++};if($source[$end] -eq '}'){$depth--};$end++}
    if($depth -ne 0){throw "Production method braces unbalanced: $signature"}
    if($source.Substring($start,$open-$start).Contains('=>')){$end=$source.IndexOf(';',$end)+1}
    $source.Substring($start,$end-$start)
}
$header=@'
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using 函数库;
namespace CluckmechCoop { internal static partial class ModuleSettings {
'@
$destination=[IO.Path]::GetFullPath($OutputPath)
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
[IO.File]::WriteAllText($destination,$header+"`n"+($methods -join "`n")+"`n}}`n",[Text.UTF8Encoding]::new($false))
