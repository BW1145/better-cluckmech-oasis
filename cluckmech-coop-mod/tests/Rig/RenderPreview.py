"""Render production rig poses exported by RigTests; the renderer contains no motion rules."""
import json
from pathlib import Path
from PIL import Image, ImageDraw

root=Path(__file__).resolve().parents[2]
atlas=Image.open(root/'src/StratagemAssets/stratagem-v2.png').convert('RGBA')
boxes=json.loads((root/'src/StratagemAssets/stratagem-v2.json').read_text(encoding='utf-8'))
poses=json.loads((root/'evidence/0.5.1/rig-poses.json').read_text(encoding='utf-8'))
output=root/'evidence/0.5.1'

def pose_image(pose, size=240):
    canvas=Image.new('RGBA',(size,size),(48,52,39,255))
    draw=ImageDraw.Draw(canvas)
    draw.line((20,190,220,190),fill=(100,108,83),width=1)
    for part in sorted(pose['parts'],key=lambda p:-p['z']):
        b=boxes[part['name']]
        sprite=atlas.crop((b['X'],b['Y'],b['X']+b['W'],b['Y']+b['H']))
        w=max(1,round(b['W']*part['sx']*2));h=max(1,round(b['H']*part['sy']*2))
        sprite=sprite.resize((w,h),Image.Resampling.NEAREST)
        if part['alpha']<1:sprite.putalpha(sprite.getchannel('A').point(lambda a:round(a*part['alpha'])))
        # A part rotates around its imported pivot, with its fixed mount retained.
        pad=Image.new('RGBA',(w*2+8,h*2+8))
        pivot=(pad.width//2,pad.height//2)
        pad.alpha_composite(sprite,(round(pivot[0]-b['PX']*w),round(pivot[1]-(1-b['PY'])*h)))
        pad=pad.rotate(part['angle'],resample=Image.Resampling.NEAREST,center=pivot)
        canvas.alpha_composite(pad,(round(120+part['x']*64-pivot[0]),round(190-part['y']*64-pivot[1])))
    draw.text((10,10),f"{pose['id']} {pose['stage']} {pose['time']:.2f}s",fill=(230,194,70))
    return canvas

for unit in [20001,20005,20006,20102]:
    selected=[p for p in poses if p['id']==unit and p['stage']!='destroyed']
    images=[pose_image(p) for p in selected]
    images[0].save(output/f'rig-{unit}.gif',save_all=True,append_images=images[1:],duration=140,loop=0,disposal=2)
views=[]
for unit in [20001,20002,20003,20004,20005,20006,20007,20101,20102]:
    selected=[p for p in poses if p['id']==unit and (p['stage'] in ['deploy','fire-2','fire-4','dock','launch'])]
    views.extend(selected[:8])
columns=8;rows=(len(views)+columns-1)//columns
contact=Image.new('RGBA',(columns*240,rows*240),(30,31,25,255))
for n,pose in enumerate(views):contact.alpha_composite(pose_image(pose),(n%columns*240,n//columns*240))
contact.save(output/'rig-contact.png')
print(f'Rendered {len(poses)} production pose records; four animated previews and contact sheet.')
