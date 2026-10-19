# 神威转移音效试听

| 文件 | 用途 | 时长 |
|---|---|---:|
| 01-神威吸入.wav | 旋转风压逐渐收束，末端快速合拢 | 0.78秒 |
| 02-神威放出.wav | 短促释放后向外舒展，低频旋动收尾 | 0.72秒 |
| 03-吸入与放出.wav | 吸入、间隔0.65秒、放出的连续试听 | 2.15秒 |

48kHz、双声道、16位PCM，峰值为－5dBFS。由 `../MakeKamuiSounds.py` 合成，噪声种子与滤波、调制、包络及短延迟参数保存在制作源中，测量值见 `音效参数.json`。

参考：[Kamui — Voicemod](https://tuna.voicemod.net/sound/b6cd3f05-fd5e-4fd5-8f71-89af0f74abdc)、[Obito Kamui Sound Effect — Voicy](https://www.voicy.network/sounds/ENALadUPxU6Zqk415hLWYg-obito-kamui-sound-effect)。制作侧已取得前一页面的公开试听录音作为声音参考。

游戏采用85%速度、保持音高的版本：`01-神威吸入-85速度.wav`（约0.900秒）、`02-神威放出-85速度.wav`（约0.835秒）。在原始合成WAV上使用FFmpeg `atempo=0.85`，再以Vorbis quality 5编码为 `src/OtherworldSounds/kamui-in.ogg`、`kamui-out.ogg`。

一次发动共用一次神威语音；每段吸入、主动返回及到期放出分别提供转移声。同一段收容或同批释放多个敌人时合并播放。音效使用游戏音效总线，只在主战场和对应神威空间内按距离播放。
