param([string]$GameDir = $env:CLUCKMECH_GAME_DIR)
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($GameDir)) { throw 'Specify -GameDir or set CLUCKMECH_GAME_DIR to the game installation directory' }
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new()
Add-Type -Path (Join-Path $PSScriptRoot 'dependencies\BepInEx\BepInEx\core\Mono.Cecil.dll')
$assemblyPath = Join-Path $GameDir 'Cluckmech Oasis_Data\Managed\Assembly-CSharp.dll'
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($assemblyPath)
try {
    $shop = $assembly.MainModule.Types | Where-Object Name -EQ '商店脚本'
    $shopSummary = $shop.Fields | Where-Object Name -EQ '总结'
    if (-not $shopSummary -or $shopSummary.FieldType.Name -ne '总结脚本') { throw 'Shop purchase summary binding changed' }
    $purchase = $shop.Methods | Where-Object Name -EQ '购买'
    $calls = @($purchase.Body.Instructions | Where-Object { $_.OpCode.Name -in @('call', 'callvirt') } | ForEach-Object { $_.Operand.Name })
    if ($calls.IndexOf('获得炮台') -lt 0 -or $calls.IndexOf('扣钱') -le $calls.IndexOf('获得炮台')) { throw 'Native purchase grant/deduction order changed' }
    $targets = @(
        @('战斗背包', '显示挑战文本', 0),
        @('刷怪器', '波次状态处理', 0),
        @('模块_波次倒计时UI', 'Update', 0),
        @('模块_波次倒计时UI', '倒计时出现', 0),
        @('模块_波次倒计时UI', '显示波次', 0),
        @('刷怪器', '刷怪', 1),
        @('刷怪器', '下一波次初始化', 0),
        @('刷怪器', '下一波次初始化', 1),
        @('刷怪器', 'Start', 0),
        @('刷怪器', '重置并开始计时', 0),
        @('刷怪器', '游戏胜利', 0),
        @('刷怪器', '延迟结算', 0),
        @('刷怪器', '结算界面切换', 0),
        @('总结脚本', 'Start', 0),
        @('总结脚本', '获得金钱', 1),
        @('战斗背包', 'OnEnable', 0),
        @('战斗背包', '开关关卡挑战', 1),
        @('模块_基本属性', 'Awake', 0),
        @('模块_攻击属性', '运算攻击力', 0),
        @('模块_攻击属性', '运算攻击速度', 0),
        @('最终boss状态机', '投掷实现', 0),
        @('词条_100028_Boss胜利判断', '死亡反馈', 0),
        @('词条_100028_Boss胜利判断', '死亡触发', 0),
        @('词条_100028_Boss胜利判断', '子物体死亡检测', 0),
        @('BossBloodstrip', '初始化', 0),
        @('BossBloodstrip', 'Update', 0),
        @('BossBloodstrip', '血量变化', 0),
        @('BossBloodstrip', '初始血条', 1),
        @('联机玩家控件', 'UserCode_发送消息广播_cmd__String__String', 2),
        @('联机玩家控件', '发送消息广播_Rpc', 2),
        @('场景切换按钮', '切换场景', 1),
        @('UI_设置界面', 'Start', 0),
        @('模块_主界面场景控制', 'Start', 0),
        @('设置管理器', '改变UI界面大小', 1),
        @('cz词缀库', '获取词缀名称', 1),
        @('战斗准备背包', '切换战斗场景', 0),
        @('存档脚本', '保存', 0),
        @('存档脚本', '读取', 0),
        @('存档脚本', '存档载入内存', 0),
        @('总结脚本', '获得炮台', 1),
        @('总结脚本', '删除炮台', 1),
        @('总结脚本', '获得强化词条', 2),
        @('总结脚本', '背包排序', 0),
        @('设置管理器', '改变游戏时间流速', 1),
        @('模块_基本属性', 'Start', 0),
        @('模块_基本属性', '运算抗性', 0),
        @('模块_移动属性', '运算移动速度', 0),
        @('模块_玩家_技能', 'Start', 0),
        @('模块_玩家_技能', '技能实现', 0),
        @('词条_100072_霰弹专家', 'Start', 0),
        @('词条_100014_工程狮', '光环', 1),
        @('词条_100033_肌肉鼠', '光环实现', 1),
        @('词条_100033_肌肉鼠', '光环', 1),
        @('模块_光环_治疗_肌肉鼠', 'Start', 0),
        @('模块_玩家_攻击', '攻击实现', 0),
        @('模块_玩家_死亡', '玩家死亡', 0),
        @('词条_100115_阿法狗', '技能实现', 0),
        @('词条_100115_阿法狗', 'Start', 0),
        @('词条_100115_阿法狗', '召唤单位', 1),
        @('模块_基本属性', '运算生命值上限', 0),
        @('模块_基本属性', '运算格挡值', 0),
        @('词条_100191_八酱兔', '技能实现实现', 0),
        @('词条_100191_八酱兔', '随机圆形落点', 2),
        @('词条_100191_八酱兔', '召唤单位', 1),
        @('商店脚本', 'Start', 0),
        @('商店脚本', '商店刷新', 0),
        @('商店脚本', '商店商品运算', 0),
        @('商店脚本', '读取储存的商店信息', 0),
        @('商店脚本', '购买', 0),
        @('选择词条流程脚本', '刷新词条', 0),
        @('选择词条流程脚本', '初始化', 0),
        @('场景函数', '切换场景', 1),
        @('dw单位库', 'OnEnable', 0),
        @('dw单位库', '是否抽取', 1),
        @('cz词缀库', 'Start', 0),
        @('cz词缀库', '获取词缀描述', 1),
        @('cz升级词缀库', 'Start', 0),
        @('cz升级词缀库', '获取单位词缀列表', 2),
        @('词条数据模板', '获取词缀名称', 0),
        @('词条数据模板', '获取词缀描述', 0),
        @('模块_基本属性', 'Awake', 0),
        @('模块_基本属性', 'OnDisable', 0),
        @('模块_基本属性', '名称', 0),
        @('模块_基本属性', '概述', 0),
        @('模块_基本属性', '说明', 0),
        @('模块_移动', 'Start', 0),
        @('模块_移动', '初始化', 0),
        @('模块_死亡', '死亡', 0),
        @('模块_伤害精灵', 'Start', 0),
        @('模块_伤害精灵', 'OnTriggerEnter2D', 1),
        @('模块_伤害精灵', '死亡', 0),
        @('建造系统', '背包cd初始化', 0),
        @('建造系统', '结束拖动延迟', 0),
        @('建造系统', '炮台运算', 1),
        @('建造系统', '开始拖动', 0),
        @('联机玩家控件', 'UserCode_放置单位_cmd__放置单位数据', 1)
    )
    foreach ($target in $targets) {
        $type = $assembly.MainModule.Types | Where-Object Name -EQ $target[0]
        $method = $type.Methods | Where-Object { $_.Name -eq $target[1] -and $_.Parameters.Count -eq $target[2] -and $_.HasBody }
        if (-not $method) { throw "Game method missing: $($target[0]).$($target[1])" }
    }
    $spawner = $assembly.MainModule.Types | Where-Object Name -EQ '刷怪器'
    if (-not ($spawner.Fields | Where-Object Name -EQ '战斗事件')) { throw 'Spawner combat event reference missing' }
    $combatEvents = $assembly.MainModule.Types | Where-Object Name -EQ '事件管理器_战斗'
    foreach ($name in @('事件_敌人生成', '事件_Boss生成')) {
        if (-not ($combatEvents.Fields | Where-Object Name -EQ $name)) { throw "Combat event missing: $name" }
    }
    $boss = $assembly.MainModule.Types | Where-Object Name -EQ '词条_100028_Boss胜利判断'
    if (-not ($boss.Fields | Where-Object Name -EQ '引用器')) { throw 'Boss feedback reference missing' }
    foreach ($name in @('死亡触发', '子物体死亡检测')) {
        $calls = @(($boss.Methods | Where-Object Name -EQ $name).Body.Instructions | Where-Object {
            $_.OpCode.Name -eq 'callvirt' -and $_.Operand.Name -eq 'SendMessage' -and $_.Operand.Parameters.Count -eq 1
        })
        if ($calls.Count -ne 1) { throw "Boss victory message target changed: $name" }
    }
    $throw = ($assembly.MainModule.Types | Where-Object Name -EQ '最终boss状态机').Methods | Where-Object Name -EQ '投掷实现'
    $instantiations = @($throw.Body.Instructions | Where-Object { $_.Operand -and $_.Operand.Name -eq 'Instantiate' -and $_.Operand.Parameters.Count -eq 3 })
    if ($instantiations.Count -ne 1 -or $instantiations[0].Operand.ReturnType.ToString() -notmatch 'GameObject|!!0') { throw 'Oasis throw instantiation seam changed' }
    $bloodstrip = $assembly.MainModule.Types | Where-Object Name -EQ 'BossBloodstrip'
    foreach ($name in @('基本属性', '生命值缓存')) {
        if (-not ($bloodstrip.Fields | Where-Object Name -EQ $name)) { throw "Boss bar field missing: $name" }
    }
    $musicAssembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $GameDir 'Cluckmech Oasis_Data/Managed/Assembly-CSharp-firstpass.dll'))
    try {
        $controller = $musicAssembly.MainModule.Types | Where-Object FullName -EQ 'DarkTonic.MasterAudio.PlaylistController'
        if (-not ($controller.Methods | Where-Object { $_.Name -eq 'ChangePlaylist' -and $_.Parameters.Count -eq 3 })) { throw 'Music playlist change binding missing' }
        foreach ($entry in @(@('Start', 0), @('StartPlaylist', 2))) {
            if (-not ($controller.Methods | Where-Object { $_.Name -eq $entry[0] -and $_.Parameters.Count -eq $entry[1] -and $_.HasBody })) { throw "Music startup binding missing: $($entry[0])" }
        }
        if (-not ($controller.Fields | Where-Object Name -EQ 'isShuffle')) { throw 'Music shuffle field missing' }
        $audio = $musicAssembly.MainModule.Types | Where-Object FullName -EQ 'DarkTonic.MasterAudio.MasterAudio'
        foreach ($entry in @(@('CreateSoundGroup',3), @('StopAllOfSound',1))) {
            if (-not ($audio.Methods | Where-Object { $_.Name -eq $entry[0] -and $_.Parameters.Count -eq $entry[1] })) { throw "Stratagem audio binding missing: $($entry[0])" }
        }
    } finally { $musicAssembly.Dispose() }
    $mirror = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $GameDir 'Cluckmech Oasis_Data/Managed/Mirror.dll'))
    try {
        $identity = $mirror.MainModule.Types | Where-Object FullName -EQ 'Mirror.NetworkIdentity'
        if (-not ($identity.Fields | Where-Object { $_.Name -eq '_assetId' -and $_.FieldType.FullName -eq 'System.UInt32' })) { throw 'Network prefab asset identifier binding changed' }
        $client = $mirror.MainModule.Types | Where-Object FullName -EQ 'Mirror.NetworkClient'
        if (-not ($client.Methods | Where-Object Name -EQ 'PrepareToSpawnSceneObjects')) { throw 'Network scene prefab registration entry missing' }
    } finally { $mirror.Dispose() }
    "PASS: $($targets.Count) game method bindings; SHA256 $((Get-FileHash -LiteralPath $assemblyPath -Algorithm SHA256).Hash)"
} finally { $assembly.Dispose() }
