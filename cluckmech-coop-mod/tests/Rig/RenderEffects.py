"""Composite poses exported from production VFX classes; no animation rules here."""
import json,sys
from pathlib import Path
from PIL import Image,ImageDraw

root=Path(__file__).resolve().parents[2]
poses=json.loads(Path(sys.argv[1]).read_text(encoding='utf-8'))
output=Path(sys.argv[2]);output.mkdir(parents=True,exist_ok=True)
atlas=Image.open(root/'src/StratagemAssets/stratagem-v2.png').convert('RGBA')
boxes=json.loads((root/'src/StratagemAssets/stratagem-v2.json').read_text(encoding='utf-8'))
reference=root.parent/'cluckmech-research/inspection/stratagem-art'
background=Image.open(sys.argv[3]).convert('RGBA')
if len(sys.argv)>4:background=background.crop(tuple(map(int,sys.argv[4].split(','))))

def draw_part(canvas,part,center,ppu):
    box=boxes[part['name']]
    sprite=atlas.crop((box['X'],box['Y'],box['X']+box['W'],box['Y']+box['H']))
    w=max(1,round(box['W']/box['PPU']*part['sx']*ppu))
    h=max(1,round(box['H']/box['PPU']*part['sy']*ppu))
    sprite=sprite.resize((w,h),Image.Resampling.NEAREST)
    sprite.putalpha(sprite.getchannel('A').point(lambda a:round(a*part['alpha'])))
    size=max(w,h)*2+8
    pad=Image.new('RGBA',(size,size));pivot=(size//2,size//2)
    pad.alpha_composite(sprite,(round(pivot[0]-box['PX']*w),round(pivot[1]-(1-box['PY'])*h)))
    if part.get('angle',0):pad=pad.rotate(part['angle'],resample=Image.Resampling.NEAREST,center=pivot)
    canvas.alpha_composite(pad,(round(center[0]+part['x']*ppu-pivot[0]),round(center[1]-part['y']*ppu-pivot[1])))

rigs={p['kind']:p for p in poses if p['kind'].startswith('rig-')}
def render(p):
    orbital=p['kind'].startswith('orbital')
    canvas=Image.new('RGBA',(720,680))
    for y in range(0,680,background.height):
        for x in range(0,720,background.width):canvas.alpha_composite(background,(x,y))
    center=(360,570 if orbital else 355);ppu=64
    # Live ground layers sort below the original unit sprites and deployed bodies.
    for part in sorted([x for x in p['parts'] if x['z']>=100.2],key=lambda v:-v['z']):draw_part(canvas,part,center,ppu)
    if p['kind'].startswith(('shield','ems','orbital','eagle-blast','barrage-blast')):
        for unit,dx,dy in [(1,-1.3,.8),(11,1.3,-.6),(20,-.5,-1.4)]:
            sprite=Image.open(reference/(str(unit)+'.png')).convert('RGBA')
            sprite=sprite.crop(sprite.getbbox());scale=58/max(sprite.size);sprite=sprite.resize((round(sprite.width*scale),round(sprite.height*scale)),Image.Resampling.NEAREST)
            canvas.alpha_composite(sprite,(round(center[0]+dx*ppu-sprite.width/2),round(center[1]-dy*ppu-sprite.height)))
        if p['kind'].startswith('shield'):
            for part in rigs['rig-20006']['parts']:draw_part(canvas,part,center,ppu)
    for part in sorted([x for x in p['parts'] if x['z']<100.2],key=lambda v:-v['z']):draw_part(canvas,part,center,ppu)
    ImageDraw.Draw(canvas).text((12,12),f"{p['kind']}  {p['time']:.2f}s",fill='white')
    return canvas

groups={}
for pose in poses:groups.setdefault(pose['kind'],[]).append(pose)
selected=[]
for kind,items in groups.items():
    images=[render(p) for p in items]
    images[0].save(output/(kind+'.gif'),save_all=True,append_images=images[1:],duration=100,loop=0,disposal=2)
    selected.append(images[min(len(images)-1,len(images)//2)].resize((360,340)))
    if kind in ['shield','ems','ems-overlap','shield-hit-4.5-0','shield-hit-0-4.5','eagle-blast','orbital']:
        images[len(images)//2].save(output/(kind+'.png'))
columns=4;contact=Image.new('RGBA',(columns*360,((len(selected)+columns-1)//columns)*340))
for n,image in enumerate(selected):contact.alpha_composite(image,(n%columns*360,n//columns*340))
contact.save(output/'contact.png')
print(f'Rendered {len(poses)} production poses into {len(groups)} animation groups.')
