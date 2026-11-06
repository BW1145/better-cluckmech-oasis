param([Parameter(Mandatory=$true)][string]$GameDir)
$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[Text.UTF8Encoding]::new($false)
$modRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output=Join-Path $modRoot 'artifacts/shop-preview-0.6.20'
$player=Join-Path $output 'player'
$dataLink=Join-Path $player 'Cluckmech Oasis_Data'
$monoLink=Join-Path $player 'MonoBleedingEdge'
New-Item -ItemType Directory -Path $output -Force | Out-Null
New-Item -ItemType Directory -Path $player -Force | Out-Null
foreach($name in @('Cluckmech Oasis.exe','UnityPlayer.dll','UnityCrashHandler64.exe')){
    Copy-Item -LiteralPath (Join-Path $GameDir $name) -Destination $player -Force
}
New-Item -ItemType Junction -Path $dataLink -Target (Join-Path $GameDir 'Cluckmech Oasis_Data') | Out-Null
New-Item -ItemType Junction -Path $monoLink -Target (Join-Path $GameDir 'MonoBleedingEdge') | Out-Null
$loader=Join-Path $modRoot 'dependencies/BepInEx'
Copy-Item -LiteralPath (Join-Path $loader 'BepInEx') -Destination $player -Recurse -Force
foreach($name in @('winhttp.dll','doorstop_config.ini','.doorstop_version')){
    Copy-Item -LiteralPath (Join-Path $loader $name) -Destination $player -Force
}
$plugins=Join-Path $player 'BepInEx/plugins'
New-Item -ItemType Directory -Path $plugins -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $modRoot 'src/bin/Release/net472/CluckmechCoop.dll') -Destination $plugins -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'bin/Release/net472/CluckmechCoopDiagnostics.dll') -Destination $plugins -Force
$patchers=Join-Path $player 'BepInEx/patchers'
New-Item -ItemType Directory -Path $patchers -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $modRoot '../cluckmech-skip-splash/src/bin/Release/net472/CluckmechSkipSplash.dll') -Destination $patchers -Force
$config=Join-Path $player 'BepInEx/config'
New-Item -ItemType Directory -Path $config -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $modRoot 'dependencies/Defaults/BepInEx.cfg') -Destination $config -Force
[IO.File]::WriteAllText((Join-Path $player 'steam_appid.txt'),'2453360',[Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText((Join-Path $config 'billy.cluckmech.coop.cfg'),"[Modules]`nMusic = false`nOtherworld = false`nThemeBranches = false`nStratagems = false`nNativeRepairs = true`n",[Text.UTF8Encoding]::new($false))
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
[ComImport,Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] class PreviewAudioEnumerator {}
[ComImport,Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface PreviewAudioDevices {
 [PreserveSig] int EnumAudioEndpoints(int flow,int mask,out IntPtr devices);
 [PreserveSig] int GetDefaultAudioEndpoint(int flow,int role,out PreviewAudioDevice device);
}
[ComImport,Guid("D666063F-1587-4E43-81F1-B948E807363F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface PreviewAudioDevice {
 [PreserveSig] int Activate(ref Guid id,int context,IntPtr activation,[MarshalAs(UnmanagedType.IUnknown)] out object endpoint);
}
[ComImport,Guid("5CDF2C82-841E-4546-9722-0CF74078229A"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface PreviewAudioVolume {
 [PreserveSig] int RegisterControlChangeNotify(IntPtr callback);
 [PreserveSig] int UnregisterControlChangeNotify(IntPtr callback);
 [PreserveSig] int GetChannelCount(out uint count);
 [PreserveSig] int SetMasterVolumeLevel(float level,Guid context);
 [PreserveSig] int SetMasterVolumeLevelScalar(float level,Guid context);
 [PreserveSig] int GetMasterVolumeLevel(out float level);
 [PreserveSig] int GetMasterVolumeLevelScalar(out float level);
 [PreserveSig] int SetChannelVolumeLevel(uint channel,float level,Guid context);
 [PreserveSig] int SetChannelVolumeLevelScalar(uint channel,float level,Guid context);
 [PreserveSig] int GetChannelVolumeLevel(uint channel,out float level);
 [PreserveSig] int GetChannelVolumeLevelScalar(uint channel,out float level);
 [PreserveSig] int SetMute([MarshalAs(UnmanagedType.Bool)] bool mute,Guid context);
 [PreserveSig] int GetMute([MarshalAs(UnmanagedType.Bool)] out bool mute);
}
public static class ShopPreviewAudio {
 static PreviewAudioVolume Volume() {
  var devices=(PreviewAudioDevices)new PreviewAudioEnumerator(); PreviewAudioDevice device;
  Marshal.ThrowExceptionForHR(devices.GetDefaultAudioEndpoint(0,1,out device));
  var id=typeof(PreviewAudioVolume).GUID; object endpoint;
  Marshal.ThrowExceptionForHR(device.Activate(ref id,23,IntPtr.Zero,out endpoint)); return (PreviewAudioVolume)endpoint;
 }
 public static bool Muted() {bool mute;Marshal.ThrowExceptionForHR(Volume().GetMute(out mute));return mute;}
 public static void Mute(bool value) {Marshal.ThrowExceptionForHR(Volume().SetMute(value,Guid.Empty));}
}
'@
$originalMute=[ShopPreviewAudio]::Muted()
$previewProcess=$null
try {
    [ShopPreviewAudio]::Mute($true)
    if(-not [ShopPreviewAudio]::Muted()){throw 'Preview audio isolation failed.'}
    $arguments=@('-screen-fullscreen','0','-screen-width','1600','-screen-height','1000','-logFile',('"'+(Join-Path $output 'player.log')+'"'),('"--bco-shop-preview='+$output+'"'))
    $previewProcess=Start-Process -FilePath (Join-Path $player 'Cluckmech Oasis.exe') -WorkingDirectory $player -ArgumentList $arguments -WindowStyle Hidden -PassThru
    if(-not $previewProcess.WaitForExit(45000)){$previewProcess.Kill();throw 'Preview exceeded its bounded runtime.'}
    if($previewProcess.ExitCode -ne 0){throw "Preview exited with code $($previewProcess.ExitCode)."}
    Get-Content -LiteralPath (Join-Path $output 'player.log') -Encoding UTF8 | Select-String '\[SHOP-PREVIEW\]'
} finally {
    if($previewProcess -ne $null -and -not $previewProcess.HasExited){$previewProcess.Kill();$previewProcess.WaitForExit()}
    [ShopPreviewAudio]::Mute($originalMute)
    if(Test-Path -LiteralPath $dataLink){[IO.Directory]::Delete($dataLink)}
    if(Test-Path -LiteralPath $monoLink){[IO.Directory]::Delete($monoLink)}
    # Reuse the local loader/runtime cache on later visual checks.
}
