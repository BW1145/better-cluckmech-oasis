$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new()
$modRoot = $PSScriptRoot
$projectXml = [xml][System.IO.File]::ReadAllText((Join-Path $modRoot 'src\CluckmechCoop.csproj'), [System.Text.Encoding]::UTF8)
$version = $projectXml.Project.PropertyGroup.Version
$packageDir = Join-Path $modRoot "release\更好的鸡械绿洲-$version-完整包"
$loaderDir = Join-Path $modRoot 'dependencies\BepInEx'
$pluginDir = Join-Path $packageDir 'BepInEx\plugins\CluckmechCoop'
New-Item -ItemType Directory -Path $pluginDir -Force | Out-Null
Copy-Item -Path (Join-Path $loaderDir 'BepInEx\*') -Destination (Join-Path $packageDir 'BepInEx') -Recurse -Force
foreach ($name in @('winhttp.dll', 'doorstop_config.ini', '.doorstop_version', 'changelog.txt')) {
    Copy-Item -LiteralPath (Join-Path $loaderDir $name) -Destination (Join-Path $packageDir $name) -Force
}
Copy-Item -LiteralPath (Join-Path $modRoot 'src\bin\Release\net472\CluckmechCoop.dll') -Destination $pluginDir -Force
Copy-Item -LiteralPath (Join-Path $modRoot 'diagnostics\bin\Release\net472\CluckmechCoopDiagnostics.dll') -Destination $pluginDir -Force
$patcherDir = Join-Path $packageDir 'BepInEx/patchers'
New-Item -ItemType Directory -Path $patcherDir -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $modRoot '../cluckmech-skip-splash/src/bin/Release/net472/CluckmechSkipSplash.dll') -Destination $patcherDir -Force
$musicDir = Join-Path $pluginDir 'Music'
New-Item -ItemType Directory -Path $musicDir -Force | Out-Null
Copy-Item -Path (Join-Path $modRoot 'src\MusicAssets\*') -Destination $musicDir -Force
$soundDir = Join-Path $pluginDir 'StratagemSounds'
New-Item -ItemType Directory -Path $soundDir -Force | Out-Null
foreach ($sound in @('barrage.ogg','eagle.ogg','laser-start.ogg','laser-loop.ogg','sounds.json')) {
    Copy-Item -LiteralPath (Join-Path $modRoot "src/StratagemSounds/$sound") -Destination $soundDir -Force
}
$configDir = Join-Path $packageDir 'BepInEx\config'
$otherworldSoundDir = Join-Path $pluginDir 'OtherworldSounds'
New-Item -ItemType Directory -Path $otherworldSoundDir -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $modRoot 'src/OtherworldSounds/kamui.ogg') -Destination $otherworldSoundDir -Force
New-Item -ItemType Directory -Path $configDir -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $modRoot 'evidence\startup-destruction\BepInEx.cfg.updated') -Destination (Join-Path $configDir 'BepInEx.cfg') -Force
[System.IO.File]::WriteAllText((Join-Path $configDir 'billy.cluckmech.coop.diagnostics.cfg'), "[Diagnostics]`r`nDetailedScan = false`r`n", [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText((Join-Path $configDir 'billy.cluckmech.coop.cfg'), "[Modules]`r`nSpeed = true`r`nCharacters = true`r`nEnergy = true`r`nMusic = true`r`nGrowth = true`r`nShop = true`r`nExpedition = true`r`nTransfer = true`r`nSkipSplash = true`r`nStratagems = true`r`nNativeRepairs = true`r`nBoons = true`r`nWealth = true`r`nOtherworld = true`r`n", [System.Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath (Join-Path $modRoot '使用说明.md') -Destination $packageDir -Force
$designDir = Join-Path $packageDir 'design'
New-Item -ItemType Directory -Path $designDir -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $modRoot 'design/异界角色设计.md') -Destination $designDir -Force
Copy-Item -LiteralPath (Join-Path $modRoot 'dependencies\BepInEx-LICENSE.txt') -Destination $packageDir -Force
$archivePath = Join-Path $modRoot "release/Better-Cluckmech-Oasis-$version-Full.zip"
Compress-Archive -Path (Join-Path $packageDir '*') -DestinationPath $archivePath -Force
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($archivePath)
try {
    foreach ($name in @('CluckmechCoop.dll', 'CluckmechCoopDiagnostics.dll', 'CluckmechSkipSplash.dll')) {
        $entry = @($archive.Entries | Where-Object Name -EQ $name)
        if ($entry.Count -ne 1) { throw "Plugin count mismatch: $name" }
        $stream = $entry[0].Open()
        $sha = [System.Security.Cryptography.SHA256]::Create()
        try { $entryHash = [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-', '') }
        finally { $stream.Dispose(); $sha.Dispose() }
        $binaryDir = if ($name -eq 'CluckmechSkipSplash.dll') { $patcherDir } else { $pluginDir }
        if ($entryHash -ne (Get-FileHash -LiteralPath (Join-Path $binaryDir $name) -Algorithm SHA256).Hash) { throw "Plugin hash mismatch: $name" }
    }
    $audioEntries = @($archive.Entries | Where-Object { $_.FullName.Replace('\', '/') -match '^BepInEx/plugins/CluckmechCoop/Music/.+\.ogg$' })
    if ($audioEntries.Count -ne 10) { throw 'Expected ten OGG music tracks' }
    $soundEntries = @($archive.Entries | Where-Object { $_.FullName.Replace('\', '/') -match '^BepInEx/plugins/CluckmechCoop/StratagemSounds/.+\.ogg$' })
    if ($soundEntries.Count -ne 4) { throw 'Expected four stratagem sound effects' }
    if (-not ($archive.Entries | Where-Object { $_.FullName.Replace('\', '/') -eq 'BepInEx/plugins/CluckmechCoop/OtherworldSounds/kamui.ogg' })) { throw 'Kamui sound effect missing' }
    if (-not ($archive.Entries | Where-Object { $_.FullName.Replace('\', '/') -eq 'BepInEx/plugins/CluckmechCoop/Music/mix.json' })) { throw 'Music calibration missing' }
    foreach ($path in @('winhttp.dll' , 'doorstop_config.ini', 'BepInEx/core/BepInEx.dll', 'BepInEx/core/0Harmony.dll', 'BepInEx/core/BepInEx.Preloader.dll')) {
        if (-not ($archive.Entries | Where-Object { $_.FullName.Replace('\', '/') -eq $path })) { throw "Loader file missing: $path" }
    }
    foreach ($item in @(@('BepInEx.cfg', '(?m)^HideManagerGameObject = true\r?$'), @('billy.cluckmech.coop.diagnostics.cfg', '(?m)^DetailedScan = false\r?$'), @('billy.cluckmech.coop.cfg', '(?ms)^\[Modules\].*^Expedition = true\r?$.*^Transfer = true\r?$'))) {
        $entry = @($archive.Entries | Where-Object Name -EQ $item[0])
        if ($entry.Count -ne 1) { throw "Configuration missing: $($item[0])" }
        $reader = [System.IO.StreamReader]::new($entry[0].Open(), [System.Text.Encoding]::UTF8)
        try { if ($reader.ReadToEnd() -notmatch $item[1]) { throw "Configuration invalid: $($item[0])" } }
        finally { $reader.Dispose() }
    }
} finally { $archive.Dispose() }
'PASS: full loader, plugin hashes and configuration verified'
Get-Item -LiteralPath $archivePath | Select-Object FullName,Length
Get-FileHash -LiteralPath $archivePath -Algorithm SHA256 | Select-Object Hash
