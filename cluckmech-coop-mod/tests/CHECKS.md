# 更新检查入口

在模组目录运行 `./ValidateUpdate.ps1 -GameDir <游戏目录> -NativeStateSource <原生状态源码> -Select All`。可以用 `-Select RuntimeScopes,ModuleLifecycle` 只执行受影响的检查；`-BuildMain` 构建主插件，`-Package` 使用已有三个 Release DLL 生成完整包。诊断与跳屏插件需要先按各自 csproj 构建。日志写入忽略的 `artifacts/checks`，失败立即返回非零退出码。

| 检查层 | Suite | 实际覆盖 |
|---|---|---|
| 纯规则 | ActionLifetime、Actor、Targets | 生产动作版本/输入与技能状态；生产范围查询加几何与单位替身，含递归独立快照 |
| 生产逻辑加替身 | Visibility、SessionNotifications、Effects、SensorPrune、Invulnerability | 隐藏/销毁/重现、渲染缓存与会话消息注册；生产状态与区域聚合，原生状态代码摘录，传感器多碰撞体和无敌转换 |
| 生产生命周期加替身 | RuntimeScopes、ModuleLifecycle、Modules、Storage | 实际场景清理协调器/会话状态；完整模块owner、两局与换关、三种角色与模块组合、ES3存储替身的损坏原档保护 |
| 原生类型/序列化 | SaveValidation、Protocol、EffectProtocol | 实际游戏存档类型及JSON；实际Mirror字节固定样本和往返；战备协议枚举 |
| 实际程序集绑定 | ActionWiring、RuntimeWiring、NotificationWiring、Transpilers | 最终DLL异步状态机接线、清理与会话注册释放；原生方法签名、精确转译匹配次数、缺失/重复接缝、组合顺序及IL栈 |

以上检查不会运行 Unity 或游戏，也不代表真实联机、视觉或整局游玩验收。旧 `Integration`、`Stratagems` 夹具仍有基线过时接口，不计入本次通过项；本轮使用上表可运行且直接关联的生产逻辑套件。`All` 会完整运行一次，不用于重复执行已通过且未受改动影响的检查。

`Prepare.ps1` 需要本机同版本 `模块_状态属性.cs`，仅在忽略的 obj 中生成检查输入；原生源码与游戏程序集均不得加入仓库或安装包。云端可复用同版本保留引用，并对照交接哈希。
