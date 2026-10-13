# 更好的鸡械绿洲

鸡械绿洲的联机模组合集。支持主菜单分别开启模块。

| 模块 | 效果 |
|---|---|
| 倍速 | 战斗中按住左Ctrl，双方进入2倍速。 |
| 更好的角色右键强化 | 增强战斗鸡、工程狮、肌肉鼠、阿法狗和八酱兔的右键。 |
| 基地车后备隐藏能源 | F发动10秒紧急强化，整局双方共用一次。 |
| 音乐扩展 | 七区域各增加一首战斗曲；普通Boss从原版两首和《铜管通缉令》中等概率选择；主菜单播放《春日影》。 |
| 强化成长 | 区域完成后提升指定鸡械强化上限；商店购买工匠刷新次数。 |
| 商店扩展 | 同家商店付费刷新3次后提供4台鸡械。 |
| 强化远征 | 清完原版敌人后完整追加强化敌人，最终完成领取额外奖励。 |
| 队友转交 | 向队友转交金币和指定鸡械，保留强化内容及上限。 |
| 跳过开屏 | 启动时跳过商标展示，主菜单可开关，下次启动生效。 |

## 目录

- `cluckmech-coop-mod`：主插件、界面素材、运行音频、代码检查及构建安装脚本。
- `cluckmech-skip-splash`：独立的开屏跳过插件。
- `cluckmech-music`：音乐制作脚本、MIDI、OGG和测量数据。

## 构建与检查

在Windows安装.NET SDK和鸡械绿洲。先设置 `CLUCKMECH_GAME_DIR` 为游戏安装目录；构建也可通过 `-p:GameDir="游戏目录"` 指定，安装及检查脚本可通过 `-GameDir "游戏目录"` 指定。参数优先于环境变量。BepInEx 5.4.23.5加载器依赖包含在主插件目录。

```powershell
cd cluckmech-coop-mod
dotnet build src/CluckmechCoop.csproj -c Release
dotnet build diagnostics/CluckmechCoopDiagnostics.csproj -c Release
dotnet build ../cluckmech-skip-splash/src/CluckmechSkipSplash.csproj -c Release
dotnet run --project tests/Music/MusicTests.csproj -c Release
dotnet run --project tests/Integration/IntegrationTests.csproj -c Release
dotnet run --project tests/RunSave/RunSaveTests.csproj -c Release
dotnet run --project tests/Characters/CharacterTests.csproj -c Release
dotnet run --project tests/TransferUi/TransferUiTests.csproj -c Release
./ValidateBindings.ps1
./ValidateRuntimePaths.ps1
./Package.ps1
```

退出游戏后执行`./Install.ps1 -GameDir 游戏目录`安装完整包，并保留现有模块设置。其他行为检查和资源检查见[开发记录](cluckmech-coop-mod/继续开发.md)及[验证记录](cluckmech-coop-mod/验证记录.md)。

主插件构建使用运行目录的九份OGG；音乐制作脚本另需Python、FFmpeg、FluidSynth及SoundFont。FFmpeg和FluidSynth从系统PATH读取，也可分别用 `FFMPEG`、`FLUIDSYNTH` 环境变量指定；`SOUNDFONT` 指定音色库，默认使用音乐目录内的 `tools/GeneralUser-GS.sf2`。主菜单音乐通过 `python cluckmech-coop-mod/PrepareMenuMusic.py "源音频文件"` 制作。参考清单的路径相对于 `cluckmech-music` 目录。音乐响度在离线阶段校准，游戏运行时直接播放。
