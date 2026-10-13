param([string]$GameDir = $env:CLUCKMECH_GAME_DIR)
if ([string]::IsNullOrWhiteSpace($GameDir)) { throw 'Specify -GameDir or set CLUCKMECH_GAME_DIR to the game installation directory' }
$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[System.Text.UTF8Encoding]::new($false)
Add-Type -Path (Join-Path $PSScriptRoot '../cluckmech-coop-mod/dependencies/BepInEx/BepInEx/core/Mono.Cecil.dll')
Add-Type -Path (Join-Path $PSScriptRoot 'src/bin/Release/net472/CluckmechSkipSplash.dll')
$setting=[CluckmechSkipSplash.Patcher].GetMethod('ReadEnabled',[System.Reflection.BindingFlags]'Static,NonPublic')
$fixture=[System.IO.Path]::GetTempFileName()
try {
 foreach($sample in @(@("[Modules]`nSkipSplash = false",$false),@("[Modules]`nSkipSplash = true",$true),@("[Other]`nSkipSplash = false",$true),@("[Modules]`nSpeed = false",$true),@("[Modules]`nSkipSplash = FALSE`n[Other]`nSkipSplash = true",$false),@("[Modules]`nSkipSplash = invalid",$true))) {
  [System.IO.File]::WriteAllText($fixture,$sample[0],[System.Text.UTF8Encoding]::new($false))
  if($setting.Invoke($null,@($fixture)) -ne $sample[1]){throw 'Startup module setting incorrect'}
 }
} finally {Remove-Item -LiteralPath $fixture -Force}
if(-not $setting.Invoke($null,@($fixture))){throw 'Missing startup setting should enable splash skip'}
'PASS: seven startup setting checks; section scope, enable, disable and defaults'
$managed=Join-Path $GameDir 'Cluckmech Oasis_Data/Managed'
$native=Join-Path $managed 'Unity.2D.Animation.Runtime.dll'
$before=(Get-FileHash -LiteralPath $native).Hash
$resolver=[Mono.Cecil.DefaultAssemblyResolver]::new();$resolver.AddSearchDirectory($managed)
$parameters=[Mono.Cecil.ReaderParameters]::new();$parameters.AssemblyResolver=$resolver
$asm=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($native,$parameters)
try {
 $calls=Get-Content -LiteralPath (Join-Path $managed '../RuntimeInitializeOnLoads.json') -Raw -Encoding UTF8 | ConvertFrom-Json
 $callback=@($calls.root | Where-Object {$_.assemblyName -eq 'Unity.2D.Animation.Runtime' -and $_.className -eq '$BurstDirectCallInitializer' -and $_.methodName -eq 'Initialize'})
 if($callback.Count -ne 1 -or $callback[0].loadTypes -ne 2){throw 'AfterAssembliesLoaded callback changed'}
 $entry=($asm.MainModule.Types | Where-Object Name -EQ '$BurstDirectCallInitializer').Methods | Where-Object Name -EQ 'Initialize'
 $original=@($entry.Body.Instructions | ForEach-Object {$_.OpCode.Name+' '+$_.Operand})
 [CluckmechSkipSplash.Patcher]::Patch($asm)
 if($entry.Body.Instructions[0].OpCode.Name -ne 'ldc.i4.0' -or $entry.Body.Instructions[1].Operand.FullName -notmatch 'SplashScreen::Stop'){throw 'Immediate stop not inserted before native initializer'}
 $preserved=@($entry.Body.Instructions | Select-Object -Skip 2 | ForEach-Object {$_.OpCode.Name+' '+$_.Operand})
 if(($original -join "`n") -cne ($preserved -join "`n")){throw 'Native initializer modified'}
 [CluckmechSkipSplash.Patcher]::Patch($asm)
 if($entry.Body.Instructions.Count -ne $original.Count+2){throw 'Repeated patch duplicates stop'}
 $stream=[System.IO.MemoryStream]::new()
 try {$asm.Write($stream);$stream.Position=0;$reloaded=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($stream);$reloaded.Dispose()}
 finally{$stream.Dispose()}
 if((Get-FileHash -LiteralPath $native).Hash -ne $before){throw 'Native assembly changed on disk'}
 'PASS: early callback binding, StopImmediate, original initializer, repeat patch, assembly serialization and original file hash'
} finally{$asm.Dispose();$resolver.Dispose()}
