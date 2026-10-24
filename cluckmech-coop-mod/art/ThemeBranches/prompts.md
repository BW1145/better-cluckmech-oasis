# 主题支线素材制作

## 古堡环境组件

工具：内置 image_gen。用途：正式关卡的地面纹理、场景实体及部件动画。

制作一张透明背景的像素游戏环境组件图集，严格四列四行等大单元格，单元格间透明留白。固定为机械绿洲式正交俯视偏三分之四视角，粗细一致的像素、灰蓝石材、暗红织物、旧金色金属和紫红枯藤，月光下的西式吸血鬼古堡。每个对象完整置于自己的单元格中心，所有单元格不得串入相邻对象，不带文字、编号或界面。

逐行对象：

1. 可平铺的灰蓝石砖地板正方形；可平铺的暗红旧地毯正方形；带苔痕的石墙横段；带铁栅及金色铰链的拱形城门。
2. 低矮矩形枯蔷薇石花坛；低矮断墙与一小堆碎石；完整的四方雕花石柱；破损但轮廓稳定的同款石柱。
3. 暗红桌布长宴会桌；独立高背木椅；金属多臂吊灯；合拢的华丽深木棺木。
4. 藤蔓缠绕的庭井；竖挂暗红族徽旗；暖色检修壁灯；狭长哥特式彩玻璃窗。

地板与地毯单元格为完整正方形，其余对象使用真实透明背景。采用清晰的成簇像素及有层次的材质阴影，最终显示时与游戏已有建筑相称。

## 深海远景

工具：内置 image_gen。成品：`sources/deepsea-background.png`。正式渲染位于战斗地板、长颈与巨脸之后，宽80格、中心(0,18)，保持原图3:2比例。

提示词：

> Use case: stylized-concept. Asset type: production background texture for a 2D top-down tower defense game's deep ocean bonus maps. Generate a wide 3:2 pixel-art underwater abyss backdrop, horizontal and vertical coverage suitable behind a flat rectangular play deck. There is NO playable floor, NO foreground platform, NO character, NO text or UI. The middle two thirds is very dark clear navy ocean water, softly mottled deep blue depth, subtle cyan suspended specks. At far left and far right, distant abandoned glass greenhouse towers, broken thin industrial scaffold silhouettes, faint cold cyan observation windows and sparse bioluminescent deep-sea plant columns descend vertically into darkness. Farther behind, larger barely visible curved structural ribs disappear into the abyss, evoking immense scale. All distant structures low contrast and darker than the playable sprites that will be layered in front. Clearly crafted pixel clusters and restrained teal-blue palette, not photorealism, not blurry. Match 1990s/modern polished 2D pixel roguelike perspective: top-down slightly elevated view, no horizon line, no surface sunlight. Mood: grand luminous blue deep sea with pressure and unknowable depth. Keep the centre and upper centre empty dark water to support a huge separately animated face and readable combat. Opaque background across the entire image; naturally dark outer edges.
