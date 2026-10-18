"""Offline atlas playback against stationary world grids at calibrated speed."""
import json,math
from pathlib import Path
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parent;OUT=ROOT.parents[1]/'src/OtherworldAssets'
def review(hero):
 atlas=Image.open(OUT/f'faithful-{hero}.png').convert('RGBA');regions=json.loads((OUT/f'faithful-{hero}.json').read_text())
 def tile(key):
  x,y,w,h,*_=regions[key];return atlas.crop((x,y,x+w,y+h))
 rest=regions[f'pose.{hero}.0.idle'];visual_scale=32/(rest[7]-rest[5])
 frames=[];contact=Image.new('RGBA',(8*192,5*192),'#687078')
 for f in range(120):
  canvas=Image.new('RGBA',(8*192,240),'#687078');draw=ImageDraw.Draw(canvas)
  for aim in range(8):
   left=aim*192;move=(aim+4)%8;angle=move*math.pi/4;v=(-math.sin(angle),math.cos(angle))
   stride=math.hypot(v[0]*(20 if hero==0 else 16.8),v[1]*(13.2 if hero==0 else 9.6))
   walking=f<80;attack=24<=f<64;cycle=min(f,80)*.042*3
   x=left+80+v[0]*(4 if hero==0 else 4.5)*32/visual_scale*(min(f,80)*.042-1.68)*2;y=90+v[1]*(4 if hero==0 else 4.5)*32/visual_scale*(min(f,80)*.042-1.68)*2
   for gx in range(left-16,left+208,16):draw.line((gx-round(x-left-80)%16,0,gx-round(x-left-80)%16,238),fill='#7e858b')
   for gy in range(-16,256,16):draw.line((left,gy-round(y-90)%16,left+191,gy-round(y-90)%16),fill='#7e858b')
   phase=int((cycle%1)*16)%16;recoil=(f-24)//3%4
   if 88<=f<104:key=f'dash.{hero}.{aim}'
   elif walking:key=f'gait.{hero}.{"attack" if attack else "walk"}.{aim}.4.{phase}'+(f'.{recoil}' if attack else '')
   else:key=f'pose.{hero}.{aim}.idle'
   x=left+96;y=90
   sprite=tile(key);canvas.alpha_composite(sprite.resize((128,128),Image.Resampling.NEAREST),(round(x-64),round(y-120+64)))
   canvas.alpha_composite(sprite,(left+64,173))
   av=(-math.sin(aim*math.pi/4),math.cos(aim*math.pi/4));tx=left+96+av[0]*65;ty=80+av[1]*60
   draw.ellipse((tx-3,ty-3,tx+3,ty+3),outline='#f5bb6a',width=1)
  frames.append(canvas.convert('RGB'))
 for row,f in enumerate((12,30,50,72,96)):contact.alpha_composite(frames[f].convert('RGBA').resize((1536,192)),(0,row*192))
 path=ROOT/f'{("perlica","obito")[hero]}-production-motion.gif';frames[0].save(path,save_all=True,append_images=frames[1:],duration=42,loop=0)
 contact.convert('RGB').save(ROOT/f'{("perlica","obito")[hero]}-production-motion-contact.png');print(path)
if __name__=='__main__':
 import sys
 for h in map(int,sys.argv[1:] or [0,1]):review(h)
