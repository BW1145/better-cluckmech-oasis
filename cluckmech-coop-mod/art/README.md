# 战备美术与声音

七份透明PNG图集由AI绘图工具按确认的装备造型制作，使用斜俯视视角、灰色金属装甲、警示黄、像素描边和活动零件。每份图集1448×1086，八列、六组动作；运行切片边界保存在`StratagemCatalog.AtlasRows`，纹理使用最近邻采样。

| 图集 | 内容 |
|---|---|
| md6.png | 升起、撒雷、待机、地雷状态、损毁、爆炸及尘土 |
| barrage380.png | 信标、落点提示、爆炸、尘土、弹坑与高速尾迹 |
| orbitalLaser.png | 点火、循环光束、地面熔融、结束与信标 |
| eagle500.png | 飞机、炸弹、入土、爆炸、烟雾、冲击波与弹坑 |
| emsMortar.png | 朝向、后坐、弹丸、静电场、消散与瓦斯场 |
| shieldRelay.png | 部署、充能、蜂窝护罩、受击、破盾、损毁与重启 |
| laserRover.png | 背包基座、无人机朝向、射击、返航、停靠、起飞与损毁 |

MD-6轮廓参考[实机装备截图](https://www.gamesradar.com/helldivers-2-minefield-anti-personnel/)；EMS迫击炮参考[原版游戏截图](https://steamcommunity.com/sharedfiles/filedetails/?id=3198345382)。图集中装备与效果为重新绘制的素材。

十份短音效由`MakeStratagemSounds.py`合成，制作参数固定，包含部署、地雷、炮击、重型爆炸、激光启动与循环、EMS、护盾受击与破碎、无人机射击。音效使用Vorbis，运行时经原版Master Audio播放，资源按插件安装位置解析。

`ValidateStratagemAssets.py`检查运行图集、全部336个切片、透明背景、音效清单和OGG文件头；可通过`--preview`输出离线资源检查图。
