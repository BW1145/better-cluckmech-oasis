# 更好的鸡械绿洲

鸡械绿洲的联机模组合集。支持主菜单分别开启模块。

| 模块 | 效果 |
|---|---|
| 倍速 | 左Shift切换双方2倍速；按住Ctrl开启3倍速，松开回到1倍速。 |
| 更好的角色右键强化 | 增强角色右键；肚子鳄、水母骑士优先吞噬鼠标目标。 |
| 基地车后备隐藏能源 | F发动10秒紧急强化，初始共用一次，事件可增加2次。 |
| 音乐扩展 | 矿洞三首曲目等概率播放，其他六区域各增加一首战斗曲；普通Boss从原版两首和《铜管通缉令》中等概率选择；主菜单播放《春日影》。 |
| 强化成长 | 区域完成后C级上限+3、B级+1，可跳过；问号事件提供刷新、鸡械扩容、动力容量和后备能源补给。 |
| 商店扩展 | 同店刷新3次提供4台商品；本局每30次付费刷新自选鸡械，每800金币商店消费获得奖励轮盘。 |
| 强化远征 | 追加生命×2、攻击×2的敌群，完成领取额外奖励；最终Boss生命×10、攻击×2。 |
| 战备协同 | 持有后备能源和不同评级鸡械，获得本场角色生命、攻击、攻速及致命保护增益；地图显示进度。 |
| 财不露白 | 每位携带超过2000金币的玩家为下场战斗招来12只强敌，分3批出现。 |
| 队友转交 | 向队友转交金币和指定鸡械，保留强化内容及上限。 |
| 跳过开屏 | 启动时跳过商标展示，主菜单可开关，下次启动生效。 |
| 地狱潜兵战备鸡械 | 七台新鸡械与33项专属强化，接入原版商店、奖励、工匠及携带卡槽。 |
| 异界角色 | 佩丽卡与宇智波带土，各四种初始天赋及分支成长；带土拥有本局跨关保留的独立神威空间。 |
| 原版运行修复 | 修复随从动画、伤害通知、火山气泡及场景界面错误；开关下次启动生效。 |

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
dotnet run --project tests/Stratagems/StratagemTests.csproj -c Release
./tests/StratagemEffects/Prepare.ps1
dotnet run --project tests/StratagemEffects/StratagemEffectsTests.csproj -c Release
./ValidateBindings.ps1
./ValidateRuntimePaths.ps1
python ./ValidateStratagemAssets.py
./Package.ps1
```

退出游戏后执行`./Install.ps1 -GameDir 游戏目录`安装完整包，并保留现有模块设置。其他行为检查和资源检查见[开发记录](cluckmech-coop-mod/继续开发.md)及[验证记录](cluckmech-coop-mod/验证记录.md)。

主插件构建使用运行目录的九份OGG；音乐制作脚本另需Python、FFmpeg、FluidSynth及SoundFont。FFmpeg和FluidSynth从系统PATH读取，也可分别用 `FFMPEG`、`FLUIDSYNTH` 环境变量指定；`SOUNDFONT` 指定音色库，默认使用音乐目录内的 `tools/GeneralUser-GS.sf2`。主菜单音乐通过 `python cluckmech-coop-mod/PrepareMenuMusic.py "源音频文件"` 制作。参考清单的路径相对于 `cluckmech-music` 目录。音乐响度在离线阶段校准，游戏运行时直接播放。

战备PNG图集嵌入插件，十份OGG音效位于插件旁的`StratagemSounds`。素材检查使用Pillow，音效制作脚本`art/MakeStratagemSounds.py`使用NumPy和SoundFile。制作参考与资源说明见[战备美术](cluckmech-coop-mod/art/README.md)。
