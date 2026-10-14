"""Validate runtime atlas bounds, framed portraits, unique upgrade art and required audio."""
import argparse
import json
from pathlib import Path
from PIL import Image,ImageDraw

parser=argparse.ArgumentParser();parser.add_argument('--preview',type=Path);args=parser.parse_args()
root=Path(__file__).resolve().parent
atlas=Image.open(root/'src/StratagemAssets/stratagem-v2.png')
boxes=json.loads((root/'src/StratagemAssets/stratagem-v2.json').read_text(encoding='utf-8'))
checks=0
def check(ok,label):
    global checks
    if not ok:raise AssertionError(label)
    checks+=1

check(atlas.mode=='RGBA' and atlas.getchannel('A').getextrema()==(0,255),'RGBA with complete transparency')
occupied=[];icons=[];hashes=set()
for name,b in boxes.items():
    x,y,w,h=[b[key] for key in ['X','Y','W','H']]
    check(0<=x<x+w<=atlas.width and 0<=y<y+h<=atlas.height,name+': import bounds')
    rect=(x,y,x+w,y+h)
    check(all(rect[2]<=a[0] or rect[0]>=a[2] or rect[3]<=a[1] or rect[1]>=a[3] for a in occupied),name+': independent rectangle')
    occupied.append(rect);tile=atlas.crop(rect);bounds=tile.getchannel('A').getbbox()
    check(bounds is not None,name+': visible silhouette')
    if name.startswith(('icon.','affix.')):
        check(w==96 and h==96 and max(bounds[2]-bounds[0],bounds[3]-bounds[1])<=72,name+': 75% portrait framing')
        if name.startswith('affix.'):
            value=tile.tobytes();check(value not in hashes,name+': distinct upgrade art');hashes.add(value)
        icons.append((name,tile))
    if name.startswith('mortar-barrel.'):
        check(all(abs(b[k])<4 for k in ['MX','MY']),name+': finite muzzle coordinates')
check(len(hashes)==33,'33 distinct upgrade icons')
check(sum(name.startswith('mortar-housing.') for name in boxes)==8,'eight matching turret housings')
check(sum(name.startswith('mortar-barrel.') for name in boxes)==8,'eight matching barrels')
check(sum(name.startswith('rover-drone.') for name in boxes)==8,'eight drone directions')
entries=json.loads((root/'src/StratagemSounds/sounds.json').read_text(encoding='utf-8'))
check({e['file'] for e in entries}=={'barrage.ogg','eagle.ogg','laser-start.ogg','laser-loop.ogg'},'four required sound clips')
for entry in entries:
    check((root/'src/StratagemSounds'/entry['file']).read_bytes()[:4]==b'OggS',entry['file']+': Vorbis')
if args.preview:
    preview=Image.new('RGBA',(8*128,((len(icons)+7)//8)*140),(36,37,31,255));draw=ImageDraw.Draw(preview)
    for n,(name,tile) in enumerate(icons):
        x,y=n%8*128,n//8*140
        draw.rectangle((x+3,y+3,x+124,y+132),outline=(140,139,125),width=3)
        preview.alpha_composite(tile,(x+16,y+8));draw.text((x+12,y+112),name,fill=(244,202,65))
    args.preview.parent.mkdir(parents=True,exist_ok=True);preview.save(args.preview)
print(f'PASS: {checks} atlas, portrait, direction and sound checks; {len(boxes)} runtime sprites')
