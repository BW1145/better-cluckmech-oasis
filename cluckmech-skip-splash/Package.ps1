$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[System.Text.UTF8Encoding]::new($false)
$loader=Join-Path $PSScriptRoot '../cluckmech-coop-mod/dependencies/BepInEx'
$package=Join-Path $PSScriptRoot 'release/鸡械绿洲跳过开屏-1.0.0-完整包'
New-Item -ItemType Directory -Path (Join-Path $package 'BepInEx/patchers') -Force | Out-Null
Copy-Item -Path (Join-Path $loader 'BepInEx/*') -Destination (Join-Path $package 'BepInEx') -Recurse -Force
foreach($file in 'winhttp.dll','doorstop_config.ini','.doorstop_version','changelog.txt'){Copy-Item -LiteralPath (Join-Path $loader $file) -Destination $package -Force}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'src/bin/Release/net472/CluckmechSkipSplash.dll') -Destination (Join-Path $package 'BepInEx/patchers') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '使用说明.md') -Destination $package -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../cluckmech-coop-mod/dependencies/BepInEx-LICENSE.txt') -Destination $package -Force
Compress-Archive -Path (Join-Path $package '*') -DestinationPath ($package+'.zip') -Force
'Packaged independent full loader and splash patcher'
