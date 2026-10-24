# 离线检查入口

运行版本：0.6.18。入口为模组目录下的 `ValidateUpdate.ps1`。

```powershell
./ValidateUpdate.ps1 -List
./ValidateUpdate.ps1 -Select Rules,Stratagems
./ValidateUpdate.ps1 -GameDir "游戏目录" -Select PerlicaHits,ObitoBalance
```

`-List` 仅列出名称；`-Select` 运行所选项并去重；`All` 包含下表全部检查，需要全部外部依赖。通常选择本次改动直接关联的项，通过后沿用结果。`RefactorParity` 另需 `-BaselineAssembly` 提供整理前主插件。`-BuildMain` 构建主插件，`-Package` 使用已有三个Release DLL打包。

| 选择名称 | 检查范围 | 依赖 |
|---|---|---|
| RefactorParity | 对照同一编译器生成的整理前后主插件，比较执行指令、字段顺序、补丁/JSON声明、天赋ID及内嵌资源；仅归一化版本字符串和有单独等价检查的公共查询调用 | BaselineAssembly |
| Rules | 转交守恒、鸡械快照比较、远征波次和倍率 | .NET 8 |
| CoopRepairs、CoopRepairNetwork、CoopRepairWiring | 原版周期生成、神威模板身份、客机反馈与真实程序集接线；排队模式单独选择 | GameDir；前两项另需NativeGameSource |
| CoopRepairLimits、StratagemHitDisplay | 模板索引工作量、延迟显示队列边界、战备与状态显示分批，使用既有替身 | .NET 8、Newtonsoft.Json 13.0.3；后者另需Python |
| TransferProtocol | 直接运行生产TransferService：双方prepare/state/commit、重复消息、金币与鸡械守恒、精确快照及断线清理；同步/排队传输替身，设置同步就绪作为输入 | GameDir |
| Stratagems | 战备参数与强化池、费用和冷却、范围衰减、护盾几何、吞噬选择、护卫犬与光束 | .NET 8 |
| Characters、RunSave、Music | 原版角色补丁、原版核心存档修复、音乐池与加载顺序 | Music另需GameDir |
| Actor、Motion、DashNetwork、Targets、PauseUi | 新角色输入/技能状态、动作、联机冲刺插值、范围查询、暂停页显示 | .NET 8 |
| ActionLifetime、RuntimeScopes | 动作失效和场景/会话生命周期 | .NET 8 |
| Visibility、CanvasOwnership、NestedHierarchy、SessionNotifications | 可见性、Canvas及监听所有权、会话处理器释放 | .NET 8 |
| SaveValidation、Storage、Modules、ModuleLifecycle | 附加数据验证、存储保护、模块组合与换关/重开 | GameDir |
| ThemeBranches | 生产支线状态的两主题进入、事件选择、五节点推进、返回、第四层抽选和JSON继续 | GameDir |
| ThemeBranchWiring | 最终DLL主题入口、原版地图同步与结算接线、真实地图JSON克隆和设置消息长度 | GameDir、最终主插件DLL |
| Protocol | 实际Mirror消息序列化和固定字节样本 | GameDir |
| Effects、EffectProtocol、SensorPrune、Invulnerability | 战备状态、协议、传感器与无敌处理 | NativeStateSource |
| Encounter、BossBar、EnemyMovement | 遭遇计数、Boss血条、原版移动及结算协调 | 后两项需NativeGameSource；EnemyMovement另需NativeNavigationSource |
| TransferUi、Rig | 原版风格界面布局和战备部件动作，使用替身对象 | .NET 8；输出至artifacts/checks |
| Bindings、NativeBindings、RuntimePaths | 模组/原版程序集接缝、签名及资源/运行路径 | GameDir、最终主插件DLL |
| Transpilers、OtherworldBindings | 精确补丁匹配、组合顺序、IL栈、新角色绑定与研究门槛 | GameDir、最终主插件DLL |
| PerlicaHits、ObitoBalance、BronzeMinerals、FlameTrigger | 闪链实际命中接线、带土数值、青铜龙及矿石、豪火触发点 | GameDir、最终主插件DLL |
| SettlementRepairs、EnergyAudio、DashChain | 结算补丁、能源与音效接线、分段冲刺 | GameDir、最终主插件DLL |
| Shop、ShopStock、ParticleInit、NativeRepairs | 商店生成与池子、粒子部件查找、原版修复绑定 | GameDir、最终主插件DLL |
| ActionWiring、RuntimeWiring、NotificationWiring | 最终DLL动作失效、场景和通知释放接线 | 最终主插件DLL |
| UiLayout、StratagemAssets、ProductionAtlas | 布局约束、战备图集、角色正式动作图集 | Python、Pillow；ProductionAtlas另需NumPy |
| AssetSelection | 用正式嵌入图集执行生产角色素材选择器 | Python、.NET 8 SDK |

## 外部资料参数

- `GameDir`：游戏安装目录，也可由 `CLUCKMECH_GAME_DIR` 提供。
- `NativeStateSource`：同版本 `模块_状态属性.cs` 私有调查源码。
- `NativeGameSource`（兼容别名 `NativeSource`）：同版本原版源码目录；BossBar读取 `BossBloodstrip.cs`，EnemyMovement读取移动、仇恨、Boss与倒计时源码；CoopRepairs还需世界类、原版周期词条和 `函数库/词条函数.cs`。
- `NativeNavigationSource`：同版本PolyNavAgent源码文件。

私有源码摘录仅生成到各测试的 `obj`，不进入仓库和安装包。测试读取生产源码或最终程序集；替身只负责提供原版/Unity依赖。这些检查验证各自列出的范围，不执行游戏、物理场景或真实双机传输。

日志写入 `artifacts/checks/<名称>.log`，本次入口结果写入 `latest.json`。失败返回非零退出码；修正后只复查失败项和受影响部分。

## 素材制作复核

`Rig` 默认执行完整战备部件检查；局部制作复核还可直接使用其 `--vfx-revision`、`--rover-beam`、`--dock-alignment`、`--rover-visual`、`--laser-seam`、`--revision` 模式，按该程序参数提供JSON输出路径。角色离线动作预览见 `art/OtherworldRig/README.md`。这些制作工具与原图保留供后续修改使用。
