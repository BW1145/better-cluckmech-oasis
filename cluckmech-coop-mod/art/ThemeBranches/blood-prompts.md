# 血母与血囊动画制作

使用内置 `image_gen` 制作，四次调用均设置 `transparent_background=true`。正式源图保存在 `sources/`；构建由 `BuildBloodAssets.collect(add)` 收集，与主图集共用 PPU 32。

## 帧序与接地点

| 源图 | 原始尺寸 | 布局和正式帧序 |
|---|---|---|
| `blood-mother-awaken.png` | 1536×1024 | 3列2行：idle 2帧、awaken 4帧 |
| `blood-mother-attacks.png` | 1536×1024 | 4列2行：attack 4帧、sweep 4帧 |
| `blood-mother-death.png` | 2172×724 | 4列1行：death 4帧 |
| `blood-sac-actions.png` | 1536×1024 | 4列3行：flight 2帧、idle 2帧、burst 4帧、death 4帧 |

母体每帧使用250×203统一画布，根部接地点 `(132,175)`，Unity pivot `(132/250,28/203)`。苏醒后主体约160像素高，即5格；休眠时囊体保持压缩。投囊液滴与横扫腕足完整保留在画布中。

血囊每帧使用66×55统一画布，接地点 `(33,49)`，Unity pivot `(0.5,6/55)`。完整飞行囊体约42像素高，即1.31格；落地压扁、膨胀和破裂轮廓使用同一缩放。

推荐动作播放跨度：母体苏醒1.2秒、投囊约0.9秒、横扫约1.0秒、死亡1.2秒；血囊飞行2帧循环、落地2帧缓慢脉动、爆裂约0.4秒、受击死亡约0.5秒。行为控制器按真实预警、接触与结束时刻选帧。

按alpha连通轮廓提取主体，并将分离液滴归属到各自主体。母体横扫轮廓跨过名义格线，提取采用完整alpha岛。投囊释放帧的一颗边缘液滴记录了明确归属。各帧接地点来自本体根部测量，投射物与伸展腕足保持相对于根部的位置。

## 生成提示词

### 血母休眠与苏醒

参考图：`sources/deepsea-octopus.png`（画风、透视、像素纹理）。

```text
Use case: stylized-concept. Asset type: production 2D game pixel-art animation sprite sheet. The input image is STYLE REFERENCE ONLY: match its 3/4 top-down viewpoint, rich dithered pixel clusters, dark indigo outline, cool blue-violet sea creature skin, and crisp readable material shading; create a NEW creature. Primary subject: BLOOD MOTHER, a huge rooted abyssal blood-reservoir organism: squat pear-shaped translucent wine-red/purple central bladder, thick branching blue-violet vein ribs wrapping its sides, pale mauve folded organic collar, four short heavy curled anchoring lobes at its base, a single narrow cyan sensory slit high on the front, and a folded circular throat aperture at lower front. It is one coherent monster, not a group, no legs resembling a human. Bladder is visibly full of crimson fluid with dark lobulated organs beneath the membrane; cold cyan pinpoint reflections, not magical glow. It will stand about five game tiles tall.
Create exactly SIX FULL SILHOUETTES, arranged in a strict 3-column by 2-row grid on a wide TRANSPARENT canvas. Every cell is equally sized, has at least 12% clear padding, and shows the SAME IDENTICAL creature at the SAME CAMERA, SAME BODY SCALE and SAME base contact line; no border, grid, letters, numbers, backdrop, ground tile or cast shadow. Nothing crosses a cell boundary. Top row: cell1 IDLE0 closed low dormant bladder, slit half shut; cell2 IDLE1 small organic inhale, membrane slightly wider but same rooting; cell3 AWAKEN0 dormant compressed, four lobes tucked. Bottom row: cell1 AWAKEN1 bladder lifts as veins contract, slit begins opening and collar unfolds; cell2 AWAKEN2 full elevation, collar open, throat dilates, anchoring lobes tense outward; cell3 AWAKEN3 alert settled combat stance, sensory slit fully open, inflated reservoir, throat closes after one gasp. The FOUR awaken drawings form genuine sequential begin-rise-open-settle poses rather than palette changes. Preserve topology of all parts. Each creature fully inside its cell. True alpha transparent background, clean separate sprites, no broad bloom.
```

### 血母投囊与横扫

参考图：`sources/blood-mother-awaken.png`（同一角色、材质、挂点与配色）。

```text
Use case: stylized-concept. Asset type: production animation sprite sheet. Input is the EXACT CHARACTER and pixel rendering reference. Preserve this blood mother's indigo vein-rib cage, translucent crimson-violet swollen reservoir, pale pink front collar, vertical cyan eye slit and lower circular toothed throat, rooted four curled short anchoring lobes, 3/4 overhead camera. Draw EIGHT genuine hand-drawn animation key poses of this SAME monster, not palette variants. Two rows of FOUR equally sized separated cells on a transparent landscape canvas, huge clear padding on all edges and between cells. Same central base contact point, same head/body size in every cell, no gridlines or labels.
TOP ROW, four consecutive projectile-spitting poses: 1 windup, reservoir compresses backwards and throat collar draws inward; 2 contact, throat extends forward/down in a muscular tube with a small red-purple organic sac visibly emerging at its lip; 3 release, throat snaps wide with a short narrow crimson fluid strand and one fully detached sac directly in front, reservoir flattens slightly, anchored base stays still; 4 recoil/recovery, tube retracts and body rises towards its settled alert stance. Include detached sac only in release cell; it fits fully within that cell and stays close to mouth, not a long beam.
BOTTOM ROW, four consecutive wide sweeping organic-lobe attack poses: 1 the near-left front anchoring lobe uncoils and draws up across body as a thick muscular blue-violet feeler; 2 the feeler lashes outward to viewer-left horizontally in a heavy low arc, bladder leans slightly opposite; 3 feeler crosses in front towards viewer-right, dark crimson underside visible, impact splash beads close to tip; 4 feeler retracts into same curled anchoring lobe and body settles. ONE lobe only changes pose, other anatomy and sensory slit stay consistent. The extended feeler must fit completely within the clear cell bounds, no artwork touching any border. Rich small pixel clusters and visible dark contours, crisp pixel art at game scale, same texture density as reference, no soft painted style, no bloom, no backdrop, no extra ground, no text. True transparent alpha background.
```

### 血母死亡

参考图：`sources/blood-mother-awaken.png`（同一角色与组织结构）。

```text
Use case: stylized-concept. Asset type: production 2D game sprite sheet. This input is the exact BLOOD MOTHER identity reference; keep its cold indigo vein ribs and four short curled suction lobes, ruby translucent bladder, pale mauve front collar and slit cyan eye. Same crisp dense pixel clusters and 3/4 top-down view. Draw exactly FOUR consecutive DEATH animation poses in one horizontal row with equal-sized cells and at least 12% transparent padding around each complete silhouette. The same monster at the same scale, base contact point and camera across all four. Frame1 death begins: strong one-sided bladder contraction, cyan slit narrowing, branching dark cracks spreading under red membrane while front collar slackens; frame2 rupture contact: bladder membrane splits into a few thick connected torn lobes, crimson fluid gushes downward between anchored feet, vein ribs flex outwards, eye extinguishes; frame3 collapse: reservoir visibly deflated to half height, ribs folding down, pale collar slumps, heavy tissue crumples on its original base; frame4 final remains: flattened coherent indigo-purple torn carcass with only small dark red residual bubbles and a compact pool at its base, entirely motionless and fully visible. All four show changing anatomy and a continuous plausible collapse, preserve recognizable silhouette elements. No eruption that fills the whole cell, no fire, no magical glow. No text, grid, backdrop, floor tile or cast shadow. Genuine transparent alpha background. Entire artwork within each cell, no crossing cell boundaries. A wide landscape sprite-sheet canvas.
```

### 血囊完整状态

参考图：`sources/blood-mother-awaken.png`（血囊膜、脉络与冷色反光）。

```text
Use case: stylized-concept. Asset type: production pixel-art game animation sprites. Input is STYLE AND MATERIAL reference for the blood-red translucent bladder and blue-violet veins. Create a distinct small BLOOD SAC: a single round semi-transparent crimson-purple organic membrane capsule, roughly spherical, with three thin dark indigo vascular ribs, visible suspended red tissue core, glossy cold cyan pin highlights, and a small pale mauve attachment scar. No eye, mouth, limbs, machinery or face. Crisp dark pixel outline, rich clustered pixel shading; same 3/4 overhead perspective and sea-creature tissue palette as reference. This attackable organism is about 1.3 game tiles high. Sprite sheet contains TWELVE separate fully visible drawn poses in a strict FOUR-column by THREE-row grid with equal-sized cells; generous 15% transparent margins in EVERY cell, no cell boundaries, text or labels. SAME scale and SAME bottom contact location throughout; fragments and liquid stay contained within their own cell, no touching neighbors. Transparent alpha background.
ROW1 cols1-2 FLIGHT0 FLIGHT1: intact buoyant capsule in two consecutive tumbling poses, differing rib rotation and slightly squash/stretch oval; cols3-4 LANDED IDLE0 IDLE1: sac resting on a small flattened organic base, idle heartbeat inhale/exhale with core visibly flexing.
ROW2 cols1-4 BURST0..3, four consecutive violent rupture drawings: 1 sac balloons taut and vascular seams stretch; 2 contact split, membrane tears in three radial directions while core lunges outward; 3 full rupture, connected curled membrane petals open low and outward with a close ring of crimson droplets around the original body; 4 end, membrane collapses into ragged empty cup and small separated droplets fall into a shallow compact puddle. No light explosion, preserve fluid/tissue character.
ROW3 cols1-4 DEATH0..3, four consecutive destroyed-before-detonation drawings: 1 puncture leaks a thin red stream, body dents; 2 capsule folds sideways, vascular ribs sag, liquid drains; 3 collapsed skin folds nearly flat around dark core; 4 final quiet deflated torn membrane remains, tiny compact dark red residue, clear readable death end state.
Every frame is an actually different drawing, complete begin-contact-end sequences, not opacity variants. No backdrop, ground tile, shadow disk, gradients outside silhouette, aura or bloom. Maintain exactly twelve sprites and true transparency.
```
