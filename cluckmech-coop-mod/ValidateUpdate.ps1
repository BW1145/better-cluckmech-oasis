param(
    [string]$GameDir=$env:CLUCKMECH_GAME_DIR,
    [string]$NativeStateSource,
    [string[]]$Select=@('All'),
    [switch]$BuildMain,
    [switch]$Package
)
$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[Text.UTF8Encoding]::new($false)
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE='1'
$root=$PSScriptRoot
$output=Join-Path $root 'artifacts/checks'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$results=[Collections.Generic.List[object]]::new()
function Invoke-Check([string]$name,[string]$command,[string[]]$arguments){
    $log=Join-Path $output "$name.log"
    & $command @arguments 2>&1 | Tee-Object -FilePath $log
    $code=$LASTEXITCODE
    $results.Add([pscustomobject]@{Suite=$name;ExitCode=$code;Log="artifacts/checks/$name.log"})
    if($code -ne 0){throw "Check failed: $name (exit $code)"}
}
function Invoke-Test([string]$name,[string]$folder,[string]$project,[string[]]$extra=@(),[switch]$Native){
    $arguments=@('run','--project',(Join-Path $root "tests/$folder/$project"),'-c','Release','-p:NuGetAudit=false','--no-launch-profile')
    if($Native){
        if(-not $GameDir){throw "GameDir is required for $name"}
        $arguments+="-p:GameDir=$GameDir"
    }
    if($extra.Count){$arguments+=@('--')+$extra}
    Invoke-Check $name 'dotnet' $arguments
}
$known=@('ActionLifetime','Actor','Targets','RuntimeScopes','Visibility','SessionNotifications','Effects','EffectProtocol','SensorPrune','Invulnerability','SaveValidation','Storage','Modules','ModuleLifecycle','Protocol','ActionWiring','RuntimeWiring','NotificationWiring','Transpilers')
foreach($name in $Select){if($name -ne 'All' -and $name -notin $known){throw "Unknown suite: $name"}}
$suites=if('All' -in $Select){$known}else{$Select}
try {
    if($BuildMain){
        if(-not $GameDir){throw 'GameDir is required for a native-reference build'}
        Invoke-Check 'BuildMain' 'dotnet' @('build',(Join-Path $root 'src/CluckmechCoop.csproj'),'-c','Release',"-p:GameDir=$GameDir",'-p:NuGetAudit=false','--nologo')
    }
    $effectsPrepared=$false
    foreach($suite in $suites){
        switch($suite){
            'ActionLifetime' {Invoke-Test $suite 'UnitLifecycle' 'UnitLifecycleTests.csproj'}
            'Actor' {Invoke-Test $suite 'OtherworldActor' 'OtherworldActorTests.csproj'}
            'Targets' {Invoke-Test $suite 'OtherworldTargets' 'OtherworldTargetsTests.csproj'}
            'RuntimeScopes' {Invoke-Test $suite 'RuntimeLifecycle' 'RuntimeLifecycleTests.csproj'}
            'Visibility' {Invoke-Test $suite 'OtherworldVisibility' 'OtherworldVisibilityTests.csproj'}
            'SessionNotifications' {Invoke-Test $suite 'OtherworldVisibility' 'OtherworldVisibilityTests.csproj' @('--session-notifications')}
            {$_ -in @('Effects','EffectProtocol','SensorPrune','Invulnerability')} {
                if(-not $effectsPrepared){
                    if(-not $NativeStateSource){throw 'NativeStateSource must point to a private decompilation of the installed native state implementation'}
                    Invoke-Check 'PrepareEffects' 'pwsh' @('-NoProfile','-File',(Join-Path $root 'tests/StratagemEffects/Prepare.ps1'),'-NativeSource',$NativeStateSource)
                    $effectsPrepared=$true
                }
                $extra=if($suite -eq 'EffectProtocol'){@('--effect-protocol')}elseif($suite -eq 'SensorPrune'){@('--sensor-prune')}elseif($suite -eq 'Invulnerability'){@('--invulnerability')}else{@()}
                Invoke-Test $suite 'StratagemEffects' 'StratagemEffectsTests.csproj' $extra
            }
            {$_ -in @('SaveValidation','Storage','Modules','ModuleLifecycle')} {
                $extra=if($suite -eq 'Storage'){@('storage')}elseif($suite -eq 'Modules'){@('modules')}elseif($suite -eq 'ModuleLifecycle'){@('--lifecycle')}else{@()}
                Invoke-Test $suite 'KamuiSave' 'KamuiSaveTests.csproj' $extra -Native
            }
            'Protocol' {Invoke-Test $suite 'OtherworldProtocol' 'OtherworldProtocolTests.csproj' @() -Native}
            {$_ -in @('ActionWiring','RuntimeWiring','NotificationWiring')} {
                $arguments=@('-NoProfile','-File',(Join-Path $root 'tests/UnitLifecycle/Validate.ps1'))
                if($suite -eq 'RuntimeWiring'){$arguments+='-RuntimeOnly'}
                if($suite -eq 'NotificationWiring'){$arguments+='-NotificationsOnly'}
                Invoke-Check $suite 'pwsh' $arguments
            }
            'Transpilers' {
                if(-not $GameDir){throw 'GameDir is required for actual-assembly bindings'}
                Invoke-Test $suite 'Bindings' 'BindingsTests.csproj' @($root,$GameDir,'--transpilers')
            }
        }
    }
    if($Package){Invoke-Check 'Package' 'pwsh' @('-NoProfile','-File',(Join-Path $root 'Package.ps1'))}
} finally {
    $results | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'latest.json') -Encoding utf8
}
