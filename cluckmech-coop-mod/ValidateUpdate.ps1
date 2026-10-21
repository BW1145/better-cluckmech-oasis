param(
    [string]$GameDir=$env:CLUCKMECH_GAME_DIR,
    [string]$NativeStateSource,
    [Alias('NativeSource')][string]$NativeGameSource,
    [string]$NativeNavigationSource,
    [string[]]$Select=@('All'),
    [switch]$List,
    [switch]$BuildMain,
    [switch]$Package
)
$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[Text.UTF8Encoding]::new($false)
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE='1'
$root=$PSScriptRoot
$output=Join-Path $root 'artifacts/checks'
$results=[Collections.Generic.List[object]]::new()
function Invoke-Check([string]$name,[string]$command,[string[]]$arguments){
    $log=Join-Path $output "$name.log"
    & $command @arguments 2>&1 | Tee-Object -FilePath $log
    $code=$LASTEXITCODE
    $results.Add([pscustomobject]@{Suite=$name;ExitCode=$code;Log="artifacts/checks/$name.log"})
    if($code -ne 0){throw "Check failed: $name (exit $code)"}
}
function Invoke-Test([string]$name,[string]$folder,[string]$project,[string[]]$extra=@(),[switch]$Native,[string[]]$Properties=@()){
    $arguments=@('run','--project',(Join-Path $root "tests/$folder/$project"),'-c','Release','-p:NuGetAudit=false','--no-launch-profile')
    if($Native){
        if(-not $GameDir){throw "GameDir is required for $name"}
        $arguments+="-p:GameDir=$GameDir"
    }
    $arguments+=$Properties
    if($extra.Count){$arguments+=@('--')+$extra}
    Invoke-Check $name 'dotnet' $arguments
}
$bindingModes=[ordered]@{
    Bindings='';Transpilers='--transpilers';OtherworldBindings='--otherworld';PerlicaHits='--perlica-hits'
    ObitoBalance='--obito-balance';BronzeMinerals='--bronze-minerals';FlameTrigger='--flame-trigger'
    SettlementRepairs='--settlement-repairs';EnergyAudio='--energy-audio';DashChain='--dash-chain'
    CoopRepairWiring='--coop-repairs';Shop='--shop';ShopStock='--shop-stock';ParticleInit='--particle-init';NativeRepairs='--native-repairs'
}
$projects=[ordered]@{
    Rules=@('','RulesTests.csproj');Characters=@('Characters','CharacterTests.csproj')
    Stratagems=@('Stratagems','StratagemTests.csproj');Motion=@('OtherworldMotion','OtherworldMotionTests.csproj')
    DashNetwork=@('OtherworldDashNetwork','OtherworldDashNetworkTests.csproj');PauseUi=@('PauseUi','PauseUiTests.csproj')
    RunSave=@('RunSave','RunSaveTests.csproj');TransferUi=@('TransferUi','TransferUiTests.csproj')
    CoopRepairLimits=@('CoopRepairLimits','CoopRepairLimitsTests.csproj');Rig=@('Rig','RigTests.csproj');TransferProtocol=@('TransferProtocol','TransferProtocolTests.csproj')
}
$known=@('CoopRepairs','CoopRepairNetwork','StratagemHitDisplay','ActionLifetime','Actor','Targets','RuntimeScopes','Visibility','CanvasOwnership','NestedHierarchy','SessionNotifications','Effects','EffectProtocol','SensorPrune','Invulnerability','SaveValidation','Storage','Modules','ModuleLifecycle','Protocol','ActionWiring','RuntimeWiring','NotificationWiring','Music','Encounter','BossBar','EnemyMovement','NativeBindings','RuntimePaths','UiLayout','StratagemAssets','ProductionAtlas','AssetSelection')+@($projects.Keys)+@($bindingModes.Keys)
if($List){$known;return}
foreach($name in $Select){if($name -ne 'All' -and $name -notin $known){throw "Unknown suite: $name"}}
$suites=@(if('All' -in $Select){$known}else{$Select}) | Select-Object -Unique
New-Item -ItemType Directory -Path $output -Force | Out-Null
try {
    if($BuildMain){
        if(-not $GameDir){throw 'GameDir is required for a native-reference build'}
        Invoke-Check 'BuildMain' 'dotnet' @('build',(Join-Path $root 'src/CluckmechCoop.csproj'),'-c','Release',"-p:GameDir=$GameDir",'-p:NuGetAudit=false','--nologo')
    }
    $effectsPrepared=$false
    $coopPrepared=$false
    foreach($suite in $suites){
        if($projects.Contains($suite)){
            $entry=$projects[$suite]
            $extra=if($suite -in @('Rig','TransferUi')){@($root)}else{@()}
            if($suite -eq 'TransferProtocol'){Invoke-Test $suite $entry[0] $entry[1] $extra -Native}
            else{Invoke-Test $suite $entry[0] $entry[1] $extra}
            continue
        }
        if($bindingModes.Contains($suite)){
            if(-not $GameDir){throw "GameDir is required for $suite"}
            $extra=@($root,$GameDir)
            if($bindingModes[$suite]){$extra+=$bindingModes[$suite]}
            Invoke-Test $suite 'Bindings' 'BindingsTests.csproj' $extra
            continue
        }
        switch($suite){
            {$_ -in @('CoopRepairs','CoopRepairNetwork')} {
                if(-not $NativeGameSource){throw 'NativeGameSource (alias NativeSource) is required for CoopRepairs'}
                if(-not $coopPrepared){
                    Invoke-Check 'PrepareCoopRepairs' 'pwsh' @('-NoProfile','-File',(Join-Path $root 'tests/CoopRepairs/Prepare.ps1'),'-NativeSource',$NativeGameSource)
                    $coopPrepared=$true
                }
                $extra=if($suite -eq 'CoopRepairNetwork'){@('--network')}else{@()}
                Invoke-Test $suite 'CoopRepairs' 'CoopRepairsTests.csproj' $extra -Native
            }
            'StratagemHitDisplay' {
                Invoke-Check 'PrepareStratagemHitDisplay' 'python' @((Join-Path $root 'tests/StratagemHitDisplay/Prepare.py'))
                Invoke-Test $suite 'StratagemHitDisplay' 'StratagemHitDisplayTests.csproj'
            }
            'ActionLifetime' {Invoke-Test $suite 'UnitLifecycle' 'UnitLifecycleTests.csproj'}
            'Actor' {Invoke-Test $suite 'OtherworldActor' 'OtherworldActorTests.csproj'}
            'Targets' {Invoke-Test $suite 'OtherworldTargets' 'OtherworldTargetsTests.csproj'}
            'RuntimeScopes' {Invoke-Test $suite 'RuntimeLifecycle' 'RuntimeLifecycleTests.csproj'}
            'Visibility' {Invoke-Test $suite 'OtherworldVisibility' 'OtherworldVisibilityTests.csproj'}
            'CanvasOwnership' {Invoke-Test $suite 'OtherworldVisibility' 'OtherworldVisibilityTests.csproj' @('--canvas-ownership')}
            'NestedHierarchy' {Invoke-Test $suite 'OtherworldVisibility' 'OtherworldVisibilityTests.csproj' @('--nested-hierarchy')}
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
            'Music' {Invoke-Test $suite 'Music' 'MusicTests.csproj' @() -Native}
            {$_ -in @('Encounter','BossBar','EnemyMovement')} {
                $properties=@()
                $prepare=@('-NoProfile','-File',(Join-Path $root "tests/$suite/Prepare.ps1"))
                if($suite -ne 'Encounter'){
                    if(-not $NativeGameSource){throw "NativeGameSource is required for $suite"}
                    $properties+="-p:NativeGameSource=$NativeGameSource"
                }
                if($suite -eq 'EnemyMovement'){
                    if(-not $NativeNavigationSource){throw 'NativeNavigationSource is required for EnemyMovement'}
                    $properties+="-p:NativeNavigationSource=$NativeNavigationSource"
                    $prepare+=@('-NativeGameSource',$NativeGameSource)
                }
                Invoke-Check "Prepare$suite" 'pwsh' $prepare
                Invoke-Test $suite $suite "$($suite)Tests.csproj" -Properties $properties
            }
            {$_ -in @('NativeBindings','RuntimePaths')} {
                $file=if($suite -eq 'NativeBindings'){'ValidateBindings.ps1'}else{'ValidateRuntimePaths.ps1'}
                $arguments=@('-NoProfile','-File',(Join-Path $root $file))
                if($suite -eq 'NativeBindings'){
                    if(-not $GameDir){throw 'GameDir is required for NativeBindings'}
                    $arguments+=@('-GameDir',$GameDir)
                }
                Invoke-Check $suite 'pwsh' $arguments
            }
            {$_ -in @('UiLayout','StratagemAssets','ProductionAtlas','AssetSelection')} {
                $file=switch($suite){
                    UiLayout {'ValidateUiLayout.py'}
                    StratagemAssets {'ValidateStratagemAssets.py'}
                    ProductionAtlas {'art/OtherworldRig/ValidateProductionAtlas.py'}
                    AssetSelection {'tests/OtherworldAssetSelection/run.py'}
                }
                $arguments=@((Join-Path $root $file))
                if($suite -eq 'AssetSelection'){
                    $dotnetRoot=Split-Path (Get-Command dotnet -ErrorAction Stop).Source
                    $arguments+=@('--dotnet-root',$dotnetRoot,'--output',(Join-Path $output 'asset-selection'))
                }
                Invoke-Check $suite 'python' $arguments
            }
        }
    }
    if($Package){Invoke-Check 'Package' 'pwsh' @('-NoProfile','-File',(Join-Path $root 'Package.ps1'))}
} finally {
    $results | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'latest.json') -Encoding utf8
}
