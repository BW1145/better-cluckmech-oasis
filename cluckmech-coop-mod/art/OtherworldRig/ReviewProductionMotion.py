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
def review_dash():
 atlas=Image.open(OUT/'faithful-1.png').convert('RGBA');regions=json.loads((OUT/'faithful-1.json').read_text())
 frames=[];shots=[];fps=40;duration=.35;pause=.2
 for tick in range(96):
  time=tick/fps-.25;canvas=Image.new('RGBA',(720,224),'#555e64');draw=ImageDraw.Draw(canvas)
  for x in range(0,720,24):draw.line((x,30,x,224),fill='#687279')
  for y in range(30,224,24):draw.line((0,y,720,y),fill='#687279')
  segment=min(2,max(0,int(max(0,time)/(duration+pause))))
  age=max(0,time)-segment*(duration+pause);p=min(1,max(0,age/duration));travel=p*p*(3-2*p)
  x=70+(segment+travel)*155;key=f'dash.1.6.{min(7,int(age/duration*8))}' if 0<=time<1.45 else 'pose.1.6.idle'
  if time>=1.45:x=535
  if time<0:x=70
  r=regions[key];sprite=atlas.crop((r[0],r[1],r[0]+64,r[1]+64)).resize((128,128),Image.Resampling.NEAREST)
  canvas.alpha_composite(sprite,(round(x)-64,55))
  text='START' if time<0 else 'END' if time>=1.45 else f'SEGMENT {segment+1} | '+('DASH' if age<duration else '0.2s INTERVAL')
  draw.text((12,8),'OBITO | source atlas | 1x game time | offline motion preview',fill='white')
  draw.text((12,205),text,fill='#f8d37b');frames.append(canvas.convert('RGB'))
  if tick in (13,17,21,25,29,33,39,43,47,51,55,61):shots.append(canvas.convert('RGB'))
 path=ROOT/'obito-dash-motion.gif';frames[0].save(path,save_all=True,append_images=frames[1:],duration=25,loop=0)
 contact=Image.new('RGB',(720*2,224*6))
 for i,shot in enumerate(shots):contact.paste(shot,((i%2)*720,(i//2)*224))
 contact.save(ROOT/'obito-dash-motion-contact.png');print(path)

if __name__=='__main__':
 import sys
 if '--dash' in sys.argv:review_dash()
 else:
  for h in map(int,sys.argv[1:] or [0,1]):review(h)
