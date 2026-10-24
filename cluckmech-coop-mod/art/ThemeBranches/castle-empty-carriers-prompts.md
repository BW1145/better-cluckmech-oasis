# 古堡搬运者空载动作

使用内置 `image_gen`，`transparent_background=true`，参考 `sources/castle-minions-b.png`，生成 `sources/castle-empty-carriers.png`（1774×887，4列2行）。

第一行保持原棺木搬运者身份，棺盖敞开，露出空棺内部；四帧为站立、左步、右步、近战。第二行保持原侍酒蝠身份，玻璃酒瓶空置；四帧为翼上、翼中、翼下、普通攻击。

`BuildCastleCarrierAssets.collect(add)` 提供 `castle.coffin.empty.0..3` 与 `castle.wine-bat.empty.0..3`。统一PPU32。缩放沿用原制作器70/277、58/260的源像素比，再按身体标志点比例对齐：棺木下沿至根足161:204；侍酒蝠眼线至酒瓶基线145:198。开盖增加的高度保留，根身体跟随原站姿尺度。每组四帧使用同一画布和挂点。

棺木统一画布92×93，pivot `(38/92,5/93)`；侍酒蝠统一画布82×66，pivot `(44/82,4/66)`。已将正式收集结果与原站姿按相同PPU和接地点并排查看，并检查8个资源键、8幅独立动作、完整alpha轮廓及至少3像素边缘留白。

## 生成提示词

```text
Use case: precise-object-edit. Asset type: production 2D game pixel-art sprite sheet, eight poses of two EXACT EXISTING creatures. The input sheet has three rows: use the COFFIN CARRIER from input middle row, and the GRAPE WINE BAT from input bottom row. Preserve their exact identities, faces, anatomical proportions, 3/4 top-down viewpoint, dark outline, fine pixel clusters and burgundy/purple/moss-green palette. Produce a NEW sheet of exactly TWO ROWS by FOUR COLUMNS, equal cells with generous transparent clearance. Render whole silhouettes, all open lids and wings and toes inside their cells. True transparent alpha background. No ground, text, borders, labels, cast shadows or other characters.
OUTPUT TOP ROW: same mossy green root-goblin carrier with two thick root feet, yellow eyes, purple foliage, carrying the SAME gold-banded burgundy wooden coffin lengthwise across its shoulders. It has released its occupants: ONE hinged coffin lid remains fully open in ALL FOUR frames, revealing a plainly EMPTY dark wood interior; no soldiers, heads, bodies, eyes or spare entities inside or alongside it. Its root body retains the same size in all poses; the opened lid increases the total sprite height while root body and coffin box remain full size. Four consecutive poses: 1 empty idle standing; 2 left root-foot forward, right back and torso rocks slightly; 3 right root-foot forward, left back and torso rocks opposite; 4 ordinary short-range attack, a front root-arm lashes down and forward while feet brace, lid still open and coffin still empty. Closely match reference middle-row fourth-cell open empty coffin, without releasing any soldiers.
OUTPUT BOTTOM ROW: SAME red-eyed purple grape-cluster wine bat, same olive-green bony wings with violet membranes and curled stem above head, same gold-framed ROUND GLASS WINE FLASK held at its front. Flask is completely EMPTY: clear slightly cyan glass, visible transparent center with pale glass highlights and only the gold frame; absolutely no red or purple wine, no residual wine pool, no pouring stream. Bat retains reference body size, grape clusters, face and wing anatomy. Four consecutive poses: 1 wings up high; 2 wings midway level; 3 wings down low; 4 ordinary attack, bat leans forward and swings its empty gold glass flask slightly forward as wings brace. Flask remains empty in every cell. Each body and base point stay consistently aligned across the four poses; drawn motion changes limbs and wings, not character scale. Faithful same existing characters rather than redesigns.
```
