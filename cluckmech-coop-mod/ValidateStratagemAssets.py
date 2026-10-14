"""Inspect the exact runtime sprite rectangles and packaged Vorbis effects."""
import argparse
import json
import re
from pathlib import Path
from PIL import Image, ImageDraw

parser = argparse.ArgumentParser()
parser.add_argument('--preview', type=Path)
args = parser.parse_args()
root = Path(__file__).resolve().parent
source = (root / 'src/StratagemRules.cs').read_text(encoding='utf-8')
rows = [list(map(int, re.findall(r'\d+', row))) for row in re.findall(r'new\[\] \{([^}]+)\}', source.split('internal static readonly StratagemDefinition[]')[0])]
names = ['md6', 'barrage380', 'orbitalLaser', 'eagle500', 'emsMortar', 'shieldRelay', 'laserRover']
icons = [(2, 0), (0, 0), (5, 0), (1, 0), (2, 0), (1, 0), (0, 4)]
checks = 0

def check(valid, label):
    global checks
    if not valid:
        raise AssertionError(label)
    checks += 1

preview = Image.new('RGBA', (7*192, 230), (43, 43, 37, 255)) if args.preview else None
for index, name in enumerate(names):
    image = Image.open(root / f'src/StratagemAssets/{name}.png')
    check(image.mode == 'RGBA' and image.size == (1448, 1086), f'{name}: format and dimensions')
    check(image.getchannel('A').getextrema() == (0, 255), f'{name}: real transparency')
    for row in range(6):
        for col in range(8):
            box = (col*181, rows[index][row], (col+1)*181, rows[index][row+1])
            check(0 <= box[0] < box[2] <= image.width and 0 <= box[1] < box[3] <= image.height, f'{name}: sprite {row}:{col}')
    row, col = icons[index]
    icon = image.crop((col*181, rows[index][row], (col+1)*181, rows[index][row+1]))
    check(icon.getchannel('A').getbbox() is not None, f'{name}: visible card icon')
    if preview:
        draw = ImageDraw.Draw(preview)
        x = index*192
        draw.rectangle((x+5, 5, x+186, 224), outline=(138,138,123), width=3)
        draw.rectangle((x+10, 10, x+181, 219), fill=(27,28,25))
        icon.thumbnail((160,164), Image.Resampling.NEAREST)
        preview.alpha_composite(icon, (x+(192-icon.width)//2, 18+(164-icon.height)//2))
        draw.text((x+12, 192), name, fill=(239,191,51))
        small = icon.copy(); small.thumbnail((32,32), Image.Resampling.NEAREST)
        preview.alpha_composite(small, (x+144,187))

manifest = json.loads((root/'src/StratagemSounds/sounds.json').read_text(encoding='utf-8'))
entries = manifest if isinstance(manifest,list) else manifest['sounds']
check(len(entries) == 10, 'ten sound effects')
for entry in entries:
    path = root/'src/StratagemSounds'/entry['file']
    check(path.is_file() and path.read_bytes()[:4] == b'OggS', f'{path.name}: Vorbis container')
if args.preview:
    args.preview.parent.mkdir(parents=True,exist_ok=True)
    preview.save(args.preview)
print(f'PASS: {checks} stratagem asset checks; 336 sprite rectangles, seven card icons and ten effects')
