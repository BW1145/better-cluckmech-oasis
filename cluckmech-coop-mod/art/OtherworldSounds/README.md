# 异界角色音效

本目录保存经用户试听选用的五段合成音效原始 WAV 与制作参数。运行资源位于 `src/OtherworldSounds`，以 Vorbis quality 5 编码，保持原始采样率及电平。

| 制作源 | 运行资源 | 触发 |
|---|---|---|
| 01-佩丽卡-电弧普攻.wav | perlica-shot.ogg | 佩丽卡普通攻击 |
| 02-佩丽卡-落雷.wav | perlica-thunder.ogg | 普通右键雷击 |
| 03-佩丽卡-轨道重块.wav | perlica-orbital.ogg | 轨道残骸轰击 |
| 04-带土-忍具投掷.wav | obito-throw.ogg | 带土普通攻击 |
| 05-带土-螺旋火焰.wav | obito-fire.ogg | 神威武库反击 |

角色效果音使用原版音效总线与距离音量，组音量为0.65，每组最多同时播放2个声音。神威发动录音组音量为0.75。解码缓存按资源复用，场景结束时停止加载。
