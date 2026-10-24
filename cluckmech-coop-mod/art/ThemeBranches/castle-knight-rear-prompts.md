# 南瓜重甲骑士后视姿态

工具：内置 image_gen。设计参考：`sources/castle-minions-a.png` 中间一行南瓜重甲骑士。产物：`sources/castle-knight-rear.png`，三列两行六姿态。

## 制作提示

Use case: stylized-concept. Production sprite animation sheet for the same pumpkin heavy knight from the middle row of castle-minions-a. Generate three columns by two rows, six isolated sprites, on a transparent landscape1536×1024 sheet with broad transparent gutters. Every pose faces diagonally up-right, north-east, away from the viewer. Show the rear of its round grey steel helmet, rear steel backplate, old gold edging and ragged purple-burgundy vine cape. Preserve its shoulder plates, boots, vine sprigs, pumpkin shape, materials and clustered pixels.

The same rectangular dark steel shield remains in the anatomical left hand, carried forward toward its north-east movement direction. Its straps, inside surface and back edge make the forward guard direction visible. The same complete sword remains in the anatomical right hand. Retain the existing armour construction and shield silhouette.

Row-major poses: rear idle; left-foot forward step; right-foot forward step; sword wind-up while shield guards forward; sword slash or thrust reaching north-east; fully fallen rear-facing knight with complete sword and shield resting beside its armour. Maintain character scale and ground anchor. Standing-equivalent height about300 source pixels; the fallen pose becomes shorter and wider at the same scale. Every weapon tip and shield corner fits within generous transparent whitespace. Use the same three-quarter overhead game camera.

## 蓄势姿态定向修订

Precise sprite edit. Preserve the three-column, two-row knight sheet. Replace the bottom-left pose using the bottom-middle pose as the arm and shoulder reference. The sword arm comes from the visible right shoulder on the viewer's right. Bend that elbow, draw its sword hand back close to the right shoulder just right of the helmet, and lift the blade diagonally toward the upper-right. Keep the other shoulder low and the shield directed forward on the far side. The sword silhouette remains to the right of the helmet and backplate. This creates a compact preparation for the following extended thrust. Preserve armour, sprite size, foot anchor and the other poses. Transparent background and complete sword tip inside the cell margin.

## 接入与尺度

`BuildKnightRearAssets.collect(add)` 提供 `castle.knight.back.idle.0`、`walk.0`、`walk.1`、`attack.0`、`attack.1`、`death.0`。

通过 `SpriteIslands.grid_sprites` 提取完整轮廓，保留越过名义格线的长剑。沿现有骑士站姿系列的58像素参考高度确定正面比例，再以正背站姿同高校准新图的统一缩放；步态、攻击与倒地共用此比例、画布和脚底挂点。独立运行脚本生成实际尺寸正背对照、三倍动图及缩放数据。
