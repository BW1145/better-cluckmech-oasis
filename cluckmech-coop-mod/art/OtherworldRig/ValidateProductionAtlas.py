"""Validate shipped atlas addressing, pivots, unique poses and recoil independence."""
import json,hashlib
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parent;OUT=ROOT.parents[1]/'src/OtherworldAssets'
def validate(hero):
 regions=json.loads((OUT/f'faithful-{hero}.json').read_text());im=Image.open(OUT/f'faithful-{hero}.png').convert('RGBA');minimum=16;recoilmin=4
 def get(key):
  x,y,w,h,*b=regions[key];assert w==h==64;assert x>=0 and y>=0 and x+w<=im.width and y+h<=im.height
  tile=im.crop((x,y,x+w,y+h));assert tuple(b)==tile.getchannel('A').point(lambda a:255 if a>=128 else 0).getbbox();assert b[0]>0 and b[1]>0 and b[2]<64 and b[3]<64
  return hashlib.sha256(tile.tobytes()).digest()
 for aim in range(8):
  get(f'dash.{hero}.{aim}');get(f'pose.{hero}.{aim}.idle')
  if hero==1:
   assert len({get(f'dash.{hero}.{aim}.{phase}') for phase in range(8)})>=6
  for p in range(4):get(f'pose.{hero}.{aim}.attack.{p}')
  for rel in range(8):
   gait=[]
   for f in range(16):
    gait.append(get(f'gait.{hero}.walk.{aim}.{rel}.{f}'))
    recoil={get(f'gait.{hero}.attack.{aim}.{rel}.{f}.{p}') for p in range(4)};recoilmin=min(recoilmin,len(recoil))
   minimum=min(minimum,len(set(gait)))
 assert len(regions)==5168+(64 if hero==1 else 0)
 return dict(hero=hero,keys=len(regions),minimumDistinctGaitPhases=minimum,minimumDistinctRecoilPhases=recoilmin,atlas=list(im.size),textureMiB=im.width*im.height*4/1048576)
if __name__=='__main__':
 import sys
 reports=[validate(int(h)) for h in sys.argv[1:] or [0,1]]
 print(json.dumps(reports,indent=2));(ROOT/'production-integrity-report.json').write_text(json.dumps(reports,indent=2)+'\n')
