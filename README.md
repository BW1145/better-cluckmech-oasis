# 更好的鸡械绿洲

鸡械绿洲模组合集，支持主菜单分别开启模块。当前运行版本为 **0.6.16**。

玩法、模块设置与安装说明以[使用说明](cluckmech-coop-mod/使用说明.md)为准。

## 项目目录

- `cluckmech-coop-mod/src`：主插件、运行图集与音频。
- `cluckmech-coop-mod/tests`：离线规则、生产逻辑替身、程序集及资源检查。
- `cluckmech-coop-mod/art`：原图、动作制作及音效源码。
- `cluckmech-coop-mod/dependencies`：加载器、许可证及发布默认配置。
- `cluckmech-skip-splash`：独立开屏跳过插件。
- `cluckmech-music`：音乐源码、MIDI与测量数据。

## 构建与检查

使用 Windows、.NET SDK 和游戏安装目录。通过环境变量 `CLUCKMECH_GAME_DIR` 或命令参数提供游戏目录。

```powershell
cd cluckmech-coop-mod
dotnet build src/CluckmechCoop.csproj -c Release -p:GameDir="游戏目录"
dotnet build diagnostics/CluckmechCoopDiagnostics.csproj -c Release -p:GameDir="游戏目录"
dotnet build ../cluckmech-skip-splash/src/CluckmechSkipSplash.csproj -c Release -p:GameDir="游戏目录"
./ValidateUpdate.ps1 -List
./ValidateUpdate.ps1 -GameDir "游戏目录" -Select Rules,Stratagems
./Package.ps1
```

检查入口、依赖与选择范围见[检查说明](cluckmech-coop-mod/tests/CHECKS.md)。按本次改动选择检查，通过后复用结果。检查均离线执行。

退出游戏后执行 `./Install.ps1 -GameDir "游戏目录"`，保留现有配置。

继续开发、资源制作与交付入口见[开发指南](cluckmech-coop-mod/继续开发.md)。云端构建备用入口见[云端构建说明](cluckmech-coop-mod/BUILD_CLOUD.md)。
