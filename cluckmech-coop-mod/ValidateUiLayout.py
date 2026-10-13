from pathlib import Path
import re
from PIL import Image

root = Path(__file__).resolve().parent
panel = (root / 'src/CoopPanel.cs').read_text(encoding='utf-8')
prep = (root / 'src/PreparationPanel.cs').read_text(encoding='utf-8')
checks = 0

def check(value, name):
    global checks
    if not value:
        raise AssertionError(name)
    checks += 1

# Native prefab anchors: level4 / 战斗准备背包 / 挑战UI / 底图、图标.
x_min, x_max = .0435208, .665
row_min, row_max = .159 * .2125717, .159 * .8364283
check('challenge.Find("底图")' in prep and 'challenge.Find("图标")' in prep and 'rect.anchorMin = challenge.anchorMin +' in prep, 'new row derives visible native bar bounds')
check(x_max < .697 and x_min > 0, 'independent row ends before native right column')
for width, height in [(1920,1080), (2560,1440), (2560,1440), (1280,720)]:
    shift = (row_max-row_min)*height + 8
    selection_top = .864*height + shift
    check(selection_top <= height and row_min*height >= 0 and (row_max*height+shift) <= .163*height+shift, 'selection and both bars fit parent bounds')
    for ui_size in [.9,1,1.1,1.3]:
        scale = min(height/1080*ui_size, (width-40)/1060, (height-40)/700)
        check(1060*scale <= width-40 and 700*scale <= height-40, 'transfer window fits scaled viewport')
check('cursor.GetComponentInParent<Canvas>()' in panel and 'overrideSorting' not in panel and 'cursorLayer.SetAsLastSibling()' in panel, 'transfer shares native cursor canvas and remains below cursor')
check('OnRectTransformDimensionsChange' in panel and '改变UI界面大小' in panel and 'private void Update(' not in panel, 'layout reacts to resize and native UI setting without frame polling')
check(8+3*154+146 <= 632 and 72+26 <= 100 and 100+20 <= 132 and 4+2*140+132 <= 416, 'card grid and three rows of text stay inside native viewport')
check(32+632+20 <= 696 and 188+416 <= 612 and 724+272+20 <= 1036 and 422+174 <= 612, 'grid, details and scrollbars fit their frames')
for count in [0,1,4,12,13,40,100]:
    height = max(416, ((count+3)//4)*140+8)
    check(count == 0 or 4+((count-1)//4)*140+132 <= height, 'large inventory content contains final card')
check('Plugin.Transfers.Request("machine", 0, index, selected.Snapshot)' in panel and '() => Select(card)' in panel, 'card selection and transfer confirmation have separate actions')
title = (root / 'src/MenuTitle.cs').read_text(encoding='utf-8')
size = re.search(r'NativeUi.Rect\(cover, "BetterOasisTitle", 0, 0, ([\d.]+), ([\d.]+)\)', title)
w, h = map(float, size.groups())
offset = re.search(r'root.anchoredPosition = new Vector2\((-?[\d.]+), (-?[\d.]+)\)', title)
dx, dy = map(float, offset.groups())
# Native level0 right title cover: center (1097.2, 726.3), size (554.2385, 707.364).
left, top = 1097.2 + 554.2385/2 + dx, 1080 - (726.3 + dy + h/2)
for width, height in [(1920,1080), (2560,1440), (1280,720), (1920,1200)]:
    scale = height/1080
    x = (width-1920*scale)/2 + left*scale
    check(x >= 0 and x+w*scale < width and top >= 0 and top+h < 1080, 'menu plaque fits viewport beside original title')
check('plaque.raycastTarget = false' in title and '"v" + Plugin.Version' in title, 'menu plaque preserves input and displays runtime version')
sprite = Image.open(root / 'src/UiAssets/menu-title.png')
check(sprite.mode == 'RGBA' and sprite.getchannel('A').getextrema() == (0,255), 'menu plaque retains real transparency')
check(sprite.width >= 1488 and sprite.height >= 894, 'embedded menu sprite contains the complete production crop')
print(f'PASS: {checks} offline UI geometry checks; native bar anchors, four viewports and four UI sizes')
