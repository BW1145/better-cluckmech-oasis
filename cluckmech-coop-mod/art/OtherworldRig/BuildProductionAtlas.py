"""Bake source-owned limbs into fixed-pivot, deduplicated 64px tiles.
No frame-specific scaling, runtime layering, or gait/attack clock coupling.
"""
import json,math,hashlib
from pathlib import Path
from PIL import Image,ImageDraw,ImageChops
ROOT=Path(__file__).resolve().parent
OUT=ROOT.parents[1]/'src/OtherworldAssets'
FRAMES=16

def dash(hero,direction,frame=None):
 bounds=[[(28,271,215,476),(241,271,438,473),(442,271,619,472),(633,271,812,473),(838,271,1003,476),(1062,271,1240,474),(1260,271,1437,474),(1456,271,1650,476)],[(75,276,212,502),(299,276,425,499),(489,278,631,499),(684,277,821,499),(865,276,982,502),(1036,277,1192,502),(1248,277,1382,499),(1456,277,1588,502)]][hero][direction]
 name=('perlica','obito')[hero];source=Image.open(ROOT.parent/f'Otherworld/{name}-source.png').convert('RGBA')
 l,t,r,b=bounds;crop=source.crop((l-4,t-4,r+4,b+4));im=Image.new('RGBA',(320,320));im.alpha_composite(crop,(round(160-crop.width/2),300-(b-t+4)))
 if hero==1:
  pivot=(160,229);mask=Image.new('L',im.size);ImageDraw.Draw(mask).rectangle((0,0,320,pivot[1]+10),fill=255)
  upper=im.copy();upper.putalpha(ImageChops.multiply(im.getchannel('A'),mask))
  lower=im.copy();lower.putalpha(ImageChops.subtract(im.getchannel('A'),mask))
  lean=12 if frame is None else (0,8,18,25,22,13,4,0)[frame]
  upper=upper.rotate(lean*math.sin(direction*math.pi/4),resample=Image.Resampling.NEAREST,center=pivot)
  if frame is not None:
   legs=Image.new('RGBA',im.size)
   swing=(0,-10,16,25,20,3,-10,0)[frame]
   for side in range(2):
    half=Image.new('L',im.size);ImageDraw.Draw(half).rectangle((side*160,0,(side+1)*160-1,320),fill=255)
    leg=lower.copy();leg.putalpha(ImageChops.multiply(lower.getchannel('A'),half))
    angle=swing*(1 if side==0 else -1)*(math.sin(direction*math.pi/4) or .65)
    leg=leg.rotate(angle,resample=Image.Resampling.NEAREST,center=(152+side*16,229))
    legs.alpha_composite(leg)
   lower=legs
  lower.alpha_composite(upper);im=lower
  if frame is not None:
   drop=(1,5,1,-2,-2,0,4,2)[frame]
   shifted=Image.new('RGBA',im.size);shifted.alpha_composite(im,(0,drop));im=shifted
 return im

def build(hero):
 if hero==0:from BuildPerlicaEightMotion import render
 else:from BuildObitoMotionProof import render
 unique=[];hashes={};keys={}
 def add(key,image):
  tile=image.resize((64,64),Image.Resampling.NEAREST)
  tile.putalpha(tile.getchannel('A').point(lambda a:0 if a<=8 else a))
  gutter=tile.getchannel('A').getbbox()
  assert gutter and gutter[0]>0 and gutter[1]>0 and gutter[2]<64 and gutter[3]<64,(key,gutter)
  bbox=tile.getchannel('A').point(lambda a:255 if a>=128 else 0).getbbox()
  digest=hashlib.sha256(tile.tobytes()).digest()
  if digest not in hashes:hashes[digest]=len(unique);unique.append(tile)
  keys[key]=(hashes[digest],bbox)
 for aim in range(8):
  add(f'dash.{hero}.{aim}',dash(hero,aim))
  if hero==1:
   for phase in range(8):add(f'dash.{hero}.{aim}.{phase}',dash(hero,aim,phase))
  add(f'pose.{hero}.{aim}.idle',render(aim,aim,0,False,False,0))
  for recoil in range(4):add(f'pose.{hero}.{aim}.attack.{recoil}',render(aim,aim,0,False,True,recoil))
  for relative in range(8):
   move=(aim+relative)%8
   for frame in range(FRAMES):
    add(f'gait.{hero}.walk.{aim}.{relative}.{frame}',render(aim,move,frame/FRAMES,True,False,0))
    for recoil in range(4):add(f'gait.{hero}.attack.{aim}.{relative}.{frame}.{recoil}',render(aim,move,frame/FRAMES,True,True,recoil))
  print('hero',hero,'facing',aim,'tiles',len(unique),flush=True)
 cols=64;rows=math.ceil(len(unique)/cols);atlas=Image.new('RGBA',(cols*64,rows*64));regions={}
 for index,tile in enumerate(unique):atlas.paste(tile,((index%cols)*64,(index//cols)*64))
 for key,(index,bbox) in keys.items():regions[key]=[(index%cols)*64,(index//cols)*64,64,64,*bbox]
 OUT.mkdir(exist_ok=True)
 png=OUT/f'faithful-{hero}.png';meta=OUT/f'faithful-{hero}.json'
 atlas.save(png.with_suffix('.png.tmp'),format='PNG');meta.with_suffix('.json.tmp').write_text(json.dumps(regions,separators=(',',':'))+'\n')
 png.with_suffix('.png.tmp').replace(png);meta.with_suffix('.json.tmp').replace(meta)
 return dict(hero=hero,gaitFrames=FRAMES,attackFrames=4,keys=len(keys),uniqueTiles=len(unique),atlasSize=atlas.size,textureBytes=atlas.width*atlas.height*4,pivot=[.5,.0625],pixelsPerUnit=32,strideAxesPixels=[20,13.2] if hero==0 else [16.8,9.6])
if __name__=='__main__':
 import sys
 heroes=[int(v) for v in sys.argv[1:]] or [0,1]
 reports=[build(hero) for hero in heroes]
 (ROOT/'production-atlas-validation.json').write_text(json.dumps(reports,indent=2)+'\n');print(reports)
