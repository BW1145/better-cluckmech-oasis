param([string]$GameDir=$env:CLUCKMECH_GAME_DIR)
$ErrorActionPreference='Stop'
if ([string]::IsNullOrWhiteSpace($GameDir)) { throw 'Specify -GameDir or set CLUCKMECH_GAME_DIR to the game installation directory' }
[Console]::OutputEncoding=[System.Text.UTF8Encoding]::new($false)
if(Get-Process | Where-Object ProcessName -Like '*Cluckmech*'){throw 'Game is running; installation deferred'}
if(-not (Test-Path -LiteralPath (Join-Path $GameDir 'BepInEx/core/BepInEx.Preloader.dll'))){throw 'Install the full loader package first'}
$target=Join-Path $GameDir 'BepInEx/patchers/CluckmechSkipSplash.dll'
New-Item -ItemType Directory -Path ([System.IO.Path]::GetDirectoryName($target)) -Force | Out-Null
$source=Join-Path $PSScriptRoot 'src/bin/Release/net472/CluckmechSkipSplash.dll'
Copy-Item -LiteralPath $source -Destination $target -Force
if((Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $target).Hash){throw 'Installed splash patcher differs'}
'Installed independent splash skip; hash verified'
