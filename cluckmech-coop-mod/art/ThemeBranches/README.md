# 主题支线美术资源

`sources/`保存生图原稿；各组`*-prompts.md`记录制作要求。正式游戏资源由本目录的Python脚本收集，输出到`src/ThemeAssets/`，随主插件嵌入。

在安装了Pillow和NumPy的Python环境中，从本目录运行：

```powershell
python -X utf8 BuildThemeAssets.py
```

| 入口 | 负责内容 |
|---|---|
| BuildThemeAssets.py | 场景组件、共用敌人、伯爵、特效与图集打包 |
| SpriteIslands.py | 按透明轮廓提取完整对象 |
| BuildCastleDeathAssets.py、BuildCastleCarrierAssets.py、BuildKnightRearAssets.py | 古堡死亡、补给送达及骑士背面动作 |
| BuildCastleBossAssets.py | 肉结、倒悬伯爵与牵线部件 |
| BuildBloodAssets.py、BuildDeepSeaDeathAssets.py | 囊母、血囊、深海死亡与溺行者 |
| BuildOctopusAssets.py | 巨章本体、两种腕足、连接部件及水压特效 |
| BuildWhiteAssets.py | 白渊多角度巨脸、眼睑、口腔、颈部及手掌 |

动画组使用统一源图缩放比例与公共画布，挂点相对画布保存；分层部件使用`MX/MY`装配偏移。图集记录使用原有`StratagemSpriteRect`格式，包含矩形、归一化轴心及32 PPU。`ThemeAssets`按主题延迟加载并缓存，纹理上传后释放CPU副本。

`ThemeEnemyVisual`连接共用敌人的状态；`ThemeBossPresentation`、`ThemeCastleBossVisual`、`ThemeOctopusVisual`和`ThemeWhiteVisitorVisual`连接对应Boss的动作。攻击预警由正式攻击事件驱动，逐帧材质与原版粒子配合。

动作预览使用正式切片、挂点及游戏动作时序。检查切片边界、状态衔接、转角帧数、部件连接、目标落点与实际视口；开发图像和日志放在现有`artifacts/`目录。
