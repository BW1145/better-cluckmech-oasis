import json
import os
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[3] / 'cluckmech-research/tools/unitypy'))
import UnityPy
from UnityPy.files.ObjectReader import ObjectReader
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parents[2]
custom = len(sys.argv) > 1
output = root / ('evidence/module-ui' if custom else 'evidence/transfer-ui')
output.mkdir(exist_ok=True)
items = json.loads((Path(sys.argv[1]) if custom else root / 'tests/TransferUi/bin/Release/net8.0/layout.json').read_text(encoding='utf-8'))
names = {item['sprite'] for item in items if item['kind'] == 'image' and item['sprite']}
original_read = ObjectReader.parse_as_object

def read_object(self, *args, **kwargs):
    kwargs['check_read'] = False
    return original_read(self, *args, **kwargs)

ObjectReader.parse_as_object = read_object
game_dir = os.environ.get("CLUCKMECH_GAME_DIR")
if not game_dir:
    raise SystemExit("Set CLUCKMECH_GAME_DIR to the game installation directory")
game = Path(game_dir) / "Cluckmech Oasis_Data"
environment = UnityPy.load(str(game / 'sharedassets0.assets'))
sprites = {}
for obj in environment.objects:
    if obj.type.name == 'Sprite':
        data = obj.read()
        if data.m_Name in names:
            sprites[data.m_Name] = (data.image, (data.m_Border.x, data.m_Border.y, data.m_Border.z, data.m_Border.w))
    elif obj.type.name == 'Font':
        data = obj.read()
        if data.m_Name == '中文像素字体':
            (output / 'pixel.ttf').write_bytes(bytes(data.m_FontData))

def resized(image, size):
    return image.resize(tuple(map(int, size)), Image.Resampling.NEAREST)

def sliced(image, size, border, multiplier):
    width, height = size
    left, bottom, right, top = map(int, border)
    factor = 4 / (multiplier or 1)
    dl, dt, dr, db = (round(n * factor) for n in (left, top, right, bottom))
    source_x = [0, left, image.width-right, image.width]
    source_y = [0, top, image.height-bottom, image.height]
    dest_x = [0, dl, width-dr, width]
    dest_y = [0, dt, height-db, height]
    result = Image.new('RGBA', size)
    for row in range(3):
        for col in range(3):
            box = (source_x[col], source_y[row], source_x[col+1], source_y[row+1])
            target = (dest_x[col+1]-dest_x[col], dest_y[row+1]-dest_y[row])
            if box[2] > box[0] and box[3] > box[1] and min(target) > 0:
                result.paste(resized(image.crop(box), target), (dest_x[col], dest_y[row]))
    return result

canvas = Image.new('RGBA', (1460, 900) if custom else (1060, 700), (25, 25, 24, 255))
for item in items:
    if item['kind'] == 'scroll':
        continue
    width, height = round(item['w']), round(item['h'])
    if width <= 0 or height <= 0:
        continue
    layer = Image.new('RGBA', (width, height))
    if item['kind'] == 'image':
        name = item['sprite']
        if not name:
            layer = Image.new('RGBA', (width, height), tuple(round(v*255) for v in item['color']))
        else:
            sprite, border = sprites[name]
        if name and item['tiled']:
            tile = resized(sprite, (max(1, round(sprite.width*4/(item['multiplier'] or 1))), max(1, round(sprite.height*4/(item['multiplier'] or 1)))))
            for y in range(0, height, tile.height):
                for x in range(0, width, tile.width):
                    layer.paste(tile, (x, y))
        elif name and (any(border) or name == '选择框'):
            layer = sliced(sprite, (width, height), (3, 3, 3, 3) if name == '选择框' else border, item['multiplier'])
        elif name:
            factor = min(width/sprite.width, height/sprite.height)
            icon = resized(sprite, (max(1, round(sprite.width*factor)), max(1, round(sprite.height*factor))))
            layer.paste(icon, ((width-icon.width)//2, (height-icon.height)//2))
        if name and item['color'][3] != 1:
            layer.putalpha(layer.getchannel('A').point(lambda value: round(value*item['color'][3])))
    elif item['text']:
        font = ImageFont.truetype(str(output / 'pixel.ttf'), item['size'])
        draw = ImageDraw.Draw(layer)
        color = tuple(round(v*255) for v in item['color'])
        lines = []
        for paragraph in item['text'].split('\n'):
            current = ''
            for char in paragraph:
                if current and font.getlength(current + char) > width:
                    lines.append(current); current = ''
                current += char
            lines.append(current)
        step = item['size'] * item.get('spacing', 1)
        text_height = len(lines) * step
        start_y = 0 if item.get('top') else (height - text_height) / 2
        for index, line in enumerate(lines):
            if item['center']:
                draw.text((width/2, start_y + index*step + step/2), line, font=font, fill=color, anchor='mm')
            else:
                draw.text((0, start_y + index*step + step/2), line, font=font, fill=color, anchor='lm')
    x, y = round(item['x']), round(item['y'])
    if item['clip']:
        cx, cy, cw, ch = map(round, item['clip'])
        left, top, right, bottom = max(x, cx), max(y, cy), min(x+width, cx+cw), min(y+height, cy+ch)
        if right <= left or bottom <= top:
            continue
        layer = layer.crop((left-x, top-y, right-x, bottom-y))
        x, y = left, top
    canvas.alpha_composite(layer, (x, y))

# The native ScrollRect positions and sizes its handle from content height.
for item in items:
    if item['kind'] == 'scroll' and item['contentHeight'] > item['h']:
        thumb = sliced(sprites['UI_备战界面_1'][0], (12, round(item['h']**2/item['contentHeight'])), sprites['UI_备战界面_1'][1], 4)
        canvas.alpha_composite(thumb, (round(item['x']), round(item['y'])))
canvas.convert('RGB').save(output / ('模组控制面板预览.png' if custom else '转交界面预览.png'))
print('PASS: preview rendered from production panel hierarchy, native sprites and pixel font')
