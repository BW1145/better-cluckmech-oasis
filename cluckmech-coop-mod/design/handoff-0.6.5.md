# 0.6.5 阶段交接

基准main和Release均为v0.6.4 / b1e4e4947d3010fa1bea1376a11ebf6e38764012。本轮独立分支为`fix/lifecycle-and-runtime-0.6.5`。八项修复独立提交`9ec04cf8796892d460384892cbd1b6c8104383f9`，后续结构与验证提交见分支历史。

八项修复和本轮结构整理已完成，独立审查中两项回归已修复复核。源码和运行素材全部在Git跟踪范围内；没有需要仅从本机搬运的新运行素材。原工作目录中的未跟踪后续设计清单未纳入本轮副本。当前待父任务最终复核、放行发布；不要提前发布Release或安装进正在使用的游戏目录。

## 文件与证据

完整包：`cluckmech-coop-mod/release/Better-Cluckmech-Oasis-0.6.5-Full.zip`，33,825,556字节；SHA256：`2a111f1e648787d2845cc7e1ee8265fa1217d754a88ded6bec9c09fa55026dbc`。

主DLL：`e79c691ddd0bde7d57846ea0254e7106dd8035dcff90d6f9dc924be2c0d647df`；诊断DLL：`aafaa4266632fc501ffda0c3c3a126d3c5d9fb7faaed65db73b6e7123f78cfb3`；跳屏DLL：`bf81a0364a2e220cfd5ce9a4f797e41dedaa4fe15de1a2470037df280680f124`。

安装包、bin/obj、原生引用及本机日志不在Git中。源码、测试与验证记录已保存；离线验证的实际覆盖及限制见`../验证记录.md`与`../tests/CHECKS.md`。本机日志在忽略的`artifacts/checks`、`artifacts/eight-fixes`和`artifacts/structure`。不需要重复执行已通过且源码未变的检查。

## 依赖

本轮使用.NET SDK 10.0.203，测试目标net8.0（本机有.NET 8运行时）；三个插件目标net472。原生游戏引用相对清单和SHA256在`build-references-0.6.5.sha256`，含实际Mirror、Unity、PolyNav、Newtonsoft与Game程序集。云端应复用上一轮保留的私有引用并核对该清单，不能将这些程序集或原生反编译源码公开上传。

BepInEx及Harmony使用仓库`dependencies/BepInEx`已跟踪版本，不新增运行依赖。区域效果检查还需要同版私有`模块_状态属性.cs`；清单最后一行记录其hash。该文件可从私有原生引用重新反编译，不属于发行素材，Prepare脚本仅向忽略的obj生成检查源码。

## 构建与检查

在仓库根目录为三个项目执行以下模式：

```powershell
dotnet build cluckmech-coop-mod/src/CluckmechCoop.csproj -c Release -p:GameDir=<private-game-dir> -p:NuGetAudit=false --nologo
dotnet build cluckmech-coop-mod/diagnostics/CluckmechCoopDiagnostics.csproj -c Release -p:GameDir=<private-game-dir> -p:NuGetAudit=false --nologo
dotnet build cluckmech-skip-splash/src/CluckmechSkipSplash.csproj -c Release -p:GameDir=<private-game-dir> -p:NuGetAudit=false --nologo
```

在模组目录使用`ValidateUpdate.ps1 -Select <suite>`执行需要的关联检查，`-GameDir`与`-NativeStateSource`均为显式参数。全部suite和覆盖层次见tests/CHECKS.md。打包运行`Package.ps1`，它会验证loader、插件哈希、音频和默认配置。不得运行Install.ps1。

Linux可使用已有`BuildCloud.py --dotnet-root <sdk-root> --game-dir <private-game-dir> --output <output> --package`，该路径属于已有离线回退构建；本轮最终二进制由Windows MSBuild生成。云端若只需复核源码和既有证据，不必为平台切换重跑全部检查；net472实际绑定程序需要兼容执行环境。

## 状态所有权

插件持有事件订阅、配置偏好和损坏档写保护；卸载时先释放场景、会话再解除补丁。联机会话持有握手/版本确认、异界消息与销毁通知注册，断线注销。ModuleRunState持有本局固定模块快照、进度与远征选择，跨场景保留，菜单新局重置。SceneLifecycle持有场景回调与战斗Begin资格，切换先关闭作用域，再按顺序取消动作和清理场景缓存。旧场景回调不能写入新场景。协议/存档版本策略见tests/OtherworldProtocol/README.md。

## 发布

用户已明确授权发布源码与完整安装包。父任务复核放行后，针对批准的完整提交创建v0.6.5，使用`release-notes-0.6.5.md`作为发布说明并上传Full.zip；GitHub自动源码归档须指向同一tag提交。发布后读回Release、tag与asset digest确认源码链接、包链接和SHA256。当前尚未创建tag或Release，main也未快进。
