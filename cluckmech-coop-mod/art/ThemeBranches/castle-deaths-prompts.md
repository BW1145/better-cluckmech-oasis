# 古堡敌人死亡连续帧

工具：内置 image_gen，透明背景。参考：`sources/castle-minions-a.png` 与 `sources/castle-minions-b.png`。

两张图分别为三列三行，每行表示同一个敌人的“受击失衡、塌落中、完全倒地”连续姿态。每个敌人保留三帧独立原画，共十八帧。按原站姿系列参考高度58像素、棺木70像素归一化；首帧已经弯膝或根部失衡，保留其相对站姿的高度变化，后续帧沿用同一缩放比例。

## castle-deaths-a.png

Use case: stylized-concept. Asset type: production sprite-sheet for a 2D pixel-art tower-defense game. Reference image: castle-minions-a defines the same three creature identities, armour, colours, weapons, sprite perspective and pixel technique. Generate a landscape sprite sheet with exactly three columns and three rows, nine sprites total, approximately1536×1024. True transparent background, generous transparent gutters.

Each row has three sequential death poses, left to right: just hit, recoiling off balance and bending at the knees; halfway falling, body low and diagonally slumped with feet or roots giving way and weapon falling alongside; fully collapsed horizontally on the ground, motion finished.

Row1: the same hooded dried-vine skeletal footman with old steel shoulder plates, torn burgundy cape and complete short sword. Row2: the same pumpkin heavy knight in grey steel plate, orange slit face and dark cape, complete rectangular gold-emblem shield and sword. Row3: the same small thorn-vine hooded crossbowman with quiver and complete wooden crossbow.

Preserve each creature's scale across all three poses. Standing-equivalent body height about250 source pixels; fallen poses naturally shorter and wider. Keep a common ground line and central collapse axis, the whole silhouette and every sword, crossbow and shield edge visible, with35 pixels of transparent margin. Crisp clustered pixels matching the reference. Armour loosens and vines fold while the same equipment remains identifiable in the resting corpse.

## castle-deaths-b.png

Use case: stylized-concept. Asset type: production sprite sheet for a 2D pixel-art tower-defense game. Reference castle-minions-b defines exact creature identities, colours, props, pixel technique and camera. Generate a landscape1536×1024 sprite sheet containing exactly three columns and three rows, nine frames, on a truly transparent background. Each row has three distinct sequential death frames: hit and off balance, halfway collapsing with visibly folding structure, completely fallen and resting.

Row1: the same stone flower gargoyle, grey cracked petal armour around a maroon red-eyed flower face, two stone bat wings and tangled green-purple roots. Wings lose balance while the face remains lifted; then one wing lowers as the petal shell tilts and roots buckle; finally the petals break and both wings fold around the low dead flower and roots.

Row2: the same large hunched green vine bearer carrying the redwood coffin with complete ornate brass frame and burgundy foliage. Roots buckle and coffin tips backward; then the creature slumps as coffin and lid tilt sideways; finally the coffin and bearer lie fully collapsed horizontally, with lid and brass corners visible.

Row3: the same purple grape bat with red eyes, two purple-green membrane wings and brass-neck round ruby wine flask. Wings falter while the flask is retained; then the body descends and wings fold unevenly as the flask tips; finally the grape body lies on the ground with both folded wings and the complete fallen flask beside it. Small wine spill permitted.

Keep anatomy, materials, camera and character size consistent. Standing-equivalent height about250 pixels, coffin300 pixels. Corpse becomes shorter and wider at the same scale. Stable ground contact near each row's bottom. Entire wing tips, lid and flask remain visible inside the cell with generous transparent margins. Crisp clustered pixels and dark outlines match the reference.

## 收集与预览

`BuildCastleDeathAssets.collect(add)` 提供 `castle.{unit}.death.0..2`；六个角色顺序为步卒、骑士、弩手、花鬼、棺木、侍酒蝠。完整轮廓通过 `SpriteIslands.grid_sprites` 提取，所有单角色死亡帧共用画布及脚底挂点。独立运行该脚本生成原站姿与三帧对比图、循环GIF和缩放数据。
