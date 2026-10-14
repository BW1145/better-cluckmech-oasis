param([string]$GameDir = $env:CLUCKMECH_GAME_DIR)
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($GameDir)) { throw 'Specify -GameDir or set CLUCKMECH_GAME_DIR to the game installation directory' }
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
if (Get-Process | Where-Object ProcessName -Like '*Cluckmech*') { throw 'Game is running; installation deferred' }
$gameRoot = [System.IO.Path]::GetFullPath($GameDir).TrimEnd('\')
if (-not (Test-Path -LiteralPath (Join-Path $gameRoot 'Cluckmech Oasis.exe'))) { throw 'Game executable missing' }
$projectXml = [xml][System.IO.File]::ReadAllText((Join-Path $PSScriptRoot 'src/CluckmechCoop.csproj'), [System.Text.Encoding]::UTF8)
$version = $projectXml.Project.PropertyGroup.Version
$packageRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "release/更好的鸡械绿洲-$version-完整包")).TrimEnd('\')
$configPath = Join-Path $gameRoot 'BepInEx/config/BepInEx.cfg'
$preservedConfig = if (Test-Path -LiteralPath $configPath) { [System.IO.File]::ReadAllText($configPath, [System.Text.Encoding]::UTF8) } else { $null }
$moduleConfigPath = Join-Path $gameRoot 'BepInEx/config/billy.cluckmech.coop.cfg'
$preservedModules = if (Test-Path -LiteralPath $moduleConfigPath) { [System.IO.File]::ReadAllText($moduleConfigPath, [System.Text.Encoding]::UTF8) } else { $null }
$files = @(Get-ChildItem -LiteralPath $packageRoot -File -Recurse)
foreach ($file in $files) {
    $relative = $file.FullName.Substring($packageRoot.Length + 1)
    $target = [System.IO.Path]::GetFullPath((Join-Path $gameRoot $relative))
    if (-not $target.StartsWith($gameRoot + '\', [System.StringComparison]::OrdinalIgnoreCase)) { throw 'Installation target escapes game directory' }

}
if (Get-Process | Where-Object ProcessName -Like '*Cluckmech*') { throw 'Game started before installation; installation deferred' }
foreach ($file in $files) {
    $relative = $file.FullName.Substring($packageRoot.Length + 1)
    $target = Join-Path $gameRoot $relative
    New-Item -ItemType Directory -Path ([System.IO.Path]::GetDirectoryName($target)) -Force | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $target -Force
}
if ($preservedConfig) {
    $preservedConfig = [regex]::Replace($preservedConfig, '(?m)^HideManagerGameObject\s*=.*$', 'HideManagerGameObject = true')
    [System.IO.File]::WriteAllText($configPath, $preservedConfig, [System.Text.UTF8Encoding]::new($false))
}
if ($preservedModules) { [System.IO.File]::WriteAllText($moduleConfigPath, $preservedModules, [System.Text.UTF8Encoding]::new($false)) }
foreach ($name in @('deploy.ogg','mine.ogg','ems.ogg','shield-hit.ogg','shield-break.ogg','rover.ogg')) {
    $obsolete = Join-Path $gameRoot ("BepInEx/plugins/CluckmechCoop/StratagemSounds/" + $name)
    if (Test-Path -LiteralPath $obsolete) { Remove-Item -LiteralPath $obsolete -Force }
}
foreach ($name in @('CluckmechCoop.dll', 'CluckmechCoopDiagnostics.dll')) {
    $relative = 'BepInEx/plugins/CluckmechCoop/' + $name
    if ((Get-FileHash -LiteralPath (Join-Path $gameRoot $relative)).Hash -ne (Get-FileHash -LiteralPath (Join-Path $packageRoot $relative)).Hash) { throw "Installed hash mismatch: $name" }
}
$patcher = 'BepInEx/patchers/CluckmechSkipSplash.dll'
if ((Get-FileHash -LiteralPath (Join-Path $gameRoot $patcher)).Hash -ne (Get-FileHash -LiteralPath (Join-Path $packageRoot $patcher)).Hash) { throw 'Installed startup module hash mismatch' }
if ([System.IO.File]::ReadAllText($configPath, [System.Text.Encoding]::UTF8) -notmatch '(?m)^HideManagerGameObject = true\r?$') { throw 'Loader protection setting missing' }
if ([System.IO.File]::ReadAllText((Join-Path $gameRoot 'BepInEx/config/billy.cluckmech.coop.diagnostics.cfg'), [System.Text.Encoding]::UTF8) -notmatch '(?m)^DetailedScan = false\r?$') { throw 'Diagnostic scan setting incorrect' }
"Installed $version; plugin hashes and loader/diagnostic settings verified"
