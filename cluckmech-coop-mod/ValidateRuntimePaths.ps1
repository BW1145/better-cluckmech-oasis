$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Add-Type -Path (Join-Path $PSScriptRoot 'dependencies\BepInEx\BepInEx\core\Mono.Cecil.dll')
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $PSScriptRoot 'src\bin\Release\net472\CluckmechCoop.dll'))
function Get-Type($name) { $assembly.MainModule.Types | Where-Object FullName -EQ "CluckmechCoop.$name" }
function Get-Method($type, $name) { (Get-Type $type).Methods | Where-Object Name -EQ $name }
try {
    foreach ($skin in @('panel', 'button', 'grain', 'card', 'selection', 'gold', 'menu-title')) {
        if (-not ($assembly.MainModule.Resources | Where-Object Name -EQ "CluckmechCoop.UiAssets.$skin.png")) { throw "Native UI skin resource missing: $skin" }
    }
    if ((Get-Type 'CoopPanel').Methods.Name -contains 'Update' -or (Get-Type 'Plugin').Methods.Name -contains 'Update') {
        throw 'Mod panel or plugin still performs per-frame work'
    }
    foreach ($atlas in @('stratagem-v2.png','stratagem-v2.json')) {
        if (-not ($assembly.MainModule.Resources | Where-Object Name -EQ "CluckmechCoop.StratagemAssets.$atlas")) { throw "Stratagem resource missing: $atlas" }
    }
    foreach ($entry in @(@('StratagemUnit','Update'),@('StratagemRover','Update'),@('StratagemWorld','Living'),@('StratagemWorld','Target'))) {
        if ((Get-Method $entry[0] $entry[1]).Body.Instructions | Where-Object { $_.Operand -and $_.Operand.Name -match '^FindObject|^FindGameObject' }) { throw "Stratagem hot path searches the scene: $($entry[0]).$($entry[1])" }
    }
    if (-not ((Get-Method 'StratagemEffects' 'Emit').Body.Instructions | Where-Object { $_.Operand -and $_.Operand.Name -eq 'PlayReady' })) { throw 'Single-player stratagem effects depend on a network identifier' }
    foreach ($name in @('get_Summary', 'get_Challenge', 'get_CanTrade')) {
        $queries = @((Get-Method 'Plugin' $name).Body.Instructions | Where-Object {
            $_.OpCode.Name -in @('call', 'callvirt') -and $_.Operand.Name -match '^Find'
        })
        if ($queries.Count) { throw "Cached state getter searches scene objects: $name" }
    }
    if ((Get-Method 'ModuleSettings' 'get_NodeKey').Body.Instructions | Where-Object { $_.Operand -and $_.Operand.Name -eq '目标场景id' }) { throw 'Node identity includes mutable scene routing target' }
    $refresh = (Get-Method 'CoopPanel' 'RefreshState').Body.Instructions
    $combat = @($refresh | Where-Object { $_.OpCode.Name -eq 'ldsfld' -and $_.Operand.Name -eq 'InCombat' })[0]
    $trade = @($refresh | Where-Object { $_.OpCode.Name -eq 'call' -and $_.Operand.Name -eq 'get_CanTrade' })[0]
    if ($refresh.IndexOf($combat) -ge $refresh.IndexOf($trade)) { throw 'Trade check precedes combat guard' }
    $between = @($refresh | Select-Object -Skip $refresh.IndexOf($combat) -First ($refresh.IndexOf($trade) - $refresh.IndexOf($combat)))
    if (-not ($between | Where-Object { $_.OpCode.Name -eq 'ret' })) { throw 'Combat does not return before trade checks' }
    $mark = (Get-Method 'MarkEnemy' 'Postfix').Body.Instructions
    if (@($mark | Where-Object { $_.OpCode.Name -in @('call', 'callvirt') -and $_.Operand.Name -match '^Find' }).Count) {
        throw 'Enemy creation searches the scene'
    }
    if ($mark | Where-Object { $_.Operand -and $_.Operand.Name -eq 'Generated' }) { throw 'Generic hostile Awake still enrolls encounter units' }
    if (-not ((Get-Method 'Difficulty' 'Generated').Body.Instructions | Where-Object { $_.Operand -and $_.Operand.Name -eq 'CountsForEncounter' })) { throw 'Native wave registration lacks scene-object filtering' }
    foreach ($entry in @(@('ModuleSettings', 'Save'), @('ModuleSettings', 'Load'), @('RestoreRunSlot', 'Prefix'))) {
        if (-not ((Get-Method $entry[0] $entry[1]).Body.Instructions | Where-Object { $_.Operand -and $_.Operand.Name -eq 'IsRunSlot' })) { throw "Save scope missing: $($entry[0]).$($entry[1])" }
    }
    if (-not ((Get-Method 'Plugin' 'Loaded').Body.Instructions | Where-Object { $_.OpCode.Name -eq 'stsfld' -and $_.Operand.Name -eq 'Blocked' })) { throw 'New run does not clear failed continuation gate' }
    foreach ($name in @('FinalAdvance', 'FinalJump', 'VictoryGate', 'DelayGate', 'SettlementGate')) {
        if (-not ((Get-Method $name 'Prefix').Body.Instructions | Where-Object { $_.OpCode.Name -eq 'call' -and $_.Operand.Name -eq 'Finish' })) {
            throw "Completion gate missing: $name"
        }
    }
    foreach ($entry in @(@('BossRewardGate', 'Prefix', 'CaptureBossReward'), @('BossVictoryGate', 'Transpiler', 'BossVictory'), @('BossBarMaximum', 'Prefix', 'RefreshBar'), @('PlaybackCountdownUi', 'Transpiler', 'PlaybackCountdown'))) {
        $entryMethod = Get-Method $entry[0] $entry[1]
        $entryInstructions = @($entryMethod.Body.Instructions)
        foreach ($nested in (Get-Type $entry[0]).NestedTypes) {
            foreach ($method in $nested.Methods) { if ($method.HasBody) { $entryInstructions += @($method.Body.Instructions) } }
        }
        if (-not ($entryInstructions | Where-Object { $_.Operand -and $_.Operand.ToString() -match $entry[2] })) {
            throw "Boss completion or health-bar route missing: $($entry[0])"
        }
    }
    $clientCompletion = (Get-Method 'Difficulty' 'ClientFinished').Body.Instructions
    $bonus = @($clientCompletion | Where-Object { $_.Operand -and $_.Operand.Name -eq 'CompleteReward' })[0]
    $session = @($clientCompletion | Where-Object { $_.Operand -and $_.Operand.Name -eq 'TryGetValue' })[0]
    if (-not $bonus -or $clientCompletion.IndexOf($bonus) -ge $clientCompletion.IndexOf($session)) { throw 'Client reward depends on prior completion state creation' }
    foreach ($entry in @(@('Difficulty', 'HasLiving'), @('AttackScale', 'Postfix'))) {
        $calls = (Get-Method $entry[0] $entry[1]).Body.Instructions | Where-Object { $_.Operand -and $_.Operand.Name -match 'RemoveWhere|FindObject|TryGetComponent|GetComponent' }
        if ($calls) { throw "Hot path contains collection scanning or component queries: $($entry[0])" }
    }
    if ((Get-Type 'TransferService').Methods.Name -contains 'Tick' -or (Get-Type 'Plugin').Methods.Name -contains 'ServiceTick') { throw 'Periodic transfer tick remains' }
    $cards = (Get-Method 'CoopPanel' 'UpdateCards').Body.Instructions | Where-Object { $_.Operand -and $_.Operand.Name -eq 'Destroy' }
    if ($cards) { throw 'Mechanical list refresh destroys cards' }
    $throw = (Get-Type 'BossThrowDamage').CustomAttributes | Where-Object { $_.AttributeType.Name -eq 'HarmonyPatch' }
    if ($throw.ConstructorArguments[0].Value.Name -ne '最终boss状态机' -or $throw.ConstructorArguments[1].Value -ne '投掷实现') { throw 'Projectile patch is not bound to the oasis throw entry' }
    foreach ($name in @('Difficulty', 'CoopPanel')) {
        $instructions = @((Get-Type $name).Methods | Where-Object HasBody | ForEach-Object { $_.Body.Instructions })
        if ($instructions | Where-Object { $_.Operand -and $_.Operand.ToString() -match 'BroadcastNotice|RefreshCombatText|CreateDynamicFontFromOSFont' }) { throw 'Display text still drives encounter sync or UI uses an OS font' }
    }
    foreach ($entry in @(@('CombatFeatures', 'Update'), @('CombatFeatures', 'ApplySpeed'), @('UnitModifiers', 'Move'), @('UnitModifiers', 'Attack'), @('UnitModifiers', 'AttackSpeed'), @('UnitModifiers', 'HealthMultiplier'), @('UnitModifiers', 'Block'), @('RatAttackGate', 'Prefix'))) {
        $queries = @((Get-Method $entry[0] $entry[1]).Body.Instructions | Where-Object {
            $_.OpCode.Name -in @('call', 'callvirt') -and $_.Operand.Name -match '^Find|^GetComponents'
        })
        if ($queries.Count) { throw "New hot path searches scene or component lists: $($entry[0]).$($entry[1])" }
    }
    if ((Get-Method 'GrowthPanel' 'Refresh').Body.Instructions | Where-Object { $_.Operand -and $_.Operand.Name -eq 'Destroy' }) { throw 'Growth inventory refresh destroys cards' }
    foreach ($name in @('ModulePanel', 'GrowthPanel', 'ShopFeatures', 'MusicExpansion')) {
        if ((Get-Type $name).Methods.Name -contains 'Update') { throw "New UI or music module polls every frame: $name" }
    }
    'PASS: cached references, event-driven UI and transfer, reused cards, counted enemies, cached attack modifiers, independent completion sync and targeted oasis projectile patch' 
} finally { $assembly.Dispose() }
