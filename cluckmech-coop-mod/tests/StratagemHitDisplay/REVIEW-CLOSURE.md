# PR #1 三项审查收尾

## 修改范围

1. **持续伤害反馈**：`OtherworldState.HostDisplay` 记录一份状态是否来自客机跳过的伤害包或房主专属 `OtherworldActions.Status` / `OtherworldStates.Spread` 入口。原版 `状态类.Clone` 使用 `MemberwiseClone`，因此标记随实际状态保留；`OtherworldStates.Copy` 继承标记，神威恢复的房主状态显式标记。普通本地状态不因同一个攻击者属于带土就一起补发。状态求和仍使用原版那一次循环，复用已有批次只回传缺失份额；战备关闭、异界角色开启时也工作。
2. **发送前过滤**：`GuestHitVisibility` 使用现有玩家空间与 `NetworkIdentity.observers`；在收集命中、分配结果记录前过滤，并在100毫秒批次发送前复核玩家/目标转移与观察者变化。双方带土分处各自空间时，不发另一人的不可见反馈；客机自己的可见空间正常接收。单位、血量、死亡同步与实际伤害不改，无新增全场查询。
3. **显示验收契约**：`CoopRepairChecks.CheckDisplayShow` 检查 normal/critical/colored 委托入口、只调用显示委托、无游戏字段写入，移除易碎的“Invoke恰好3次”断言。云端测试直接执行该检查并读取本次编译的生产 `Show` 方法IL；缺失显示入口、意外伤害调用要使检查失败。

## 回归入口

```sh
python3 cluckmech-coop-mod/tests/StratagemHitDisplay/Prepare.py
dotnet run --project cluckmech-coop-mod/tests/StratagemHitDisplay/StratagemHitDisplayTests.csproj -c Release
dotnet run --project cluckmech-coop-mod/tests/CoopRepairLimits/CoopRepairLimitsTests.csproj -c Release
```

原有战备测试原样执行；新增测试编译真实的状态前缀、Copy、快照恢复状态初始化表达式、批次发送、空间/观察者筛选及显示契约。周围的Unity、网络和状态列表宿主使用替身，已就绪目标与未就绪目标仍遵守原有回放规则。

覆盖：角色及普通鸡械的受保护包；同来源但客机已有的独立状态；技能结束后跳伤；混合来源与灼烧/中毒/流血颜色；来源销毁、复制、旧快照恢复；单机/客机权限；双带土不同空间、客机进入自身空间、发送前切换空间/销毁/隐藏/换连接；大量不可见命中零消息；嵌套作用域与异常退出；实际编译Show的正向及反例检查。

原版接缝已从用户提供的固定哈希DLL读取：`增加状态`为0x06000A35，先Clone再加入状态列表；`状态类.Clone`为0x06000A3D，仅调用MemberwiseClone；`状态持续`为0x06000A3F；状态求和为0x06000A45。原版DLL与反编译内容不进入仓库。

原伤害/状态持续时间/统计/虚化/清理与跨空间反伤不改。本次是PR源码更新；既有旧候选ZIP应在重新构建后替换。
