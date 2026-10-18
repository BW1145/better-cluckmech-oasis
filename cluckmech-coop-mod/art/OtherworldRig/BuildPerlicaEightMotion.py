"""Eight authored facing silhouettes with source-pixel segmented leg articulation.
The original head/hair/body stays registered; independently clocked authored
arm layers are composited by PerlicaAttackLayers before fixed-pivot packing.
"""
import math
import numpy as np
from PIL import Image,ImageDraw,ImageChops,ImageFilter
from pathlib import Path
from PerlicaRigDefinition import definition
ROOT=Path(__file__).resolve().parent
SOURCE=Image.open(ROOT.parent/'Otherworld/perlica-source.png').convert('RGBA')
CANVAS=(320,320);SCALE=.2;GROUND=(160,300)

def bone(im,a,b,c,d,offset):
 sx,sy=b[0]-a[0],b[1]-a[1];tx,ty=d[0]-c[0],d[1]-c[1]
 sn=math.hypot(sx,sy);tn=math.hypot(tx,ty);su=(sx/sn,sy/sn);tu=(tx/tn,ty/tn);sv=(-su[1],su[0]);tv=(-tu[1],tu[0]);r=sn/tn
 A=su[0]*r*tu[0]+sv[0]*tv[0];B=su[0]*r*tu[1]+sv[0]*tv[1];D=su[1]*r*tu[0]+sv[1]*tv[0];E=su[1]*r*tu[1]+sv[1]*tv[1]
 x,y=c[0]+offset[0],c[1]+offset[1]
 return im.transform(CANVAS,Image.Transform.AFFINE,(A,B,a[0]-A*x-B*y,D,E,a[1]-D*x-E*y),Image.Resampling.NEAREST)

def make_rig(direction):
 rig=definition(direction);image=SOURCE.crop(rig['bounds']);cut=Image.new('L',image.size);parts=[]
 for leg in rig['legs']:
  mask=Image.new('L',image.size);ImageDraw.Draw(mask).polygon(leg['polygon'],fill=255)
  mask=mask.filter(ImageFilter.MaxFilter(9))
  # Capture the complete outline/anti-alias fringe, while leaving the tunic
  # hem untouched. Ownership is exclusive even where expanded masks overlap.
  ImageDraw.Draw(mask).rectangle((0,0,image.width,round(min(p[1] for p in leg['polygon'])+1)),fill=0)
  # The source has detached one-pixel outline fragments between the feet.
  # Assign those to the corresponding leg as well, never the static body.
  middle=sum(l['hip'][0] for l in rig['legs'])/2
  left= min(p[0] for l in rig['legs'] for p in l['polygon'])-4 if len(parts)==0 else middle
  right=middle if len(parts)==0 else max(p[0] for l in rig['legs'] for p in l['polygon'])+4
  ImageDraw.Draw(mask).rectangle((round(left),round(min(l['knee'][1] for l in rig['legs'])+3),round(right),image.height),fill=255)
  mask=ImageChops.subtract(mask,cut);leg['mask']=mask
  cut=ImageChops.lighter(cut,mask)
  layers=[]
  for upper in (True,False):
   segment=mask.copy();draw=ImageDraw.Draw(segment);ky=leg['knee'][1]
   draw.rectangle((0,round(ky+4) if upper else 0,image.width,image.height if upper else round(ky-4)),fill=0)
   part=Image.new('RGBA',image.size);part.paste(image,(0,0),segment);layers.append(part)
  parts.append((leg,*layers))
 body=image.copy();body.putalpha(ImageChops.subtract(body.getchannel('A'),cut))
 # A complete firing-arm owner mask includes the entire connected blade.
 arm=rig['arm'];arm_mask=Image.new('L',image.size);ad=ImageDraw.Draw(arm_mask)
 ad.polygon(arm['polygon'],fill=255)
 ad.line([arm['shoulder'],arm['hand']],fill=255,width=30)
 ad.line([arm['hand'],arm['tip']],fill=255,width=26)
 for q in (arm['hand'],arm['tip']):ad.ellipse((q[0]-8,q[1]-8,q[0]+8,q[1]+8),fill=255)
 if direction==0:
  from BuildReferenceLayers import POLYGONS
  arm_mask=Image.new('L',image.size);ad=ImageDraw.Draw(arm_mask)
  # Front proof crop has identical x/y origin and one extra transparent row.
  for polygon in POLYGONS['left-arm-weapon']:ad.polygon(polygon,fill=255)
  arm.update(shoulder=(98,119),hand=(74,155),tip=(9,227))
 else:
  # Exposed skin and brown rear-hair belong to the stationary anatomy, never
  # to a moving sleeve. Color restriction applies only above the glove.
  pixels=np.asarray(image).astype(int);m=np.array(arm_mask)
  yy=np.indices(m.shape)[0]
  brown=(pixels[:,:,0]>pixels[:,:,1]+10)&(pixels[:,:,1]>pixels[:,:,2]+7)&(yy<arm['hand'][1]+2)
  m[brown]=0;arm_mask=Image.fromarray(m)
 arm_image=Image.new('RGBA',image.size);arm_image.paste(image,(0,0),arm_mask)
 body_without_arm=body.copy();body_without_arm.putalpha(ImageChops.subtract(body.getchannel('A'),arm_mask))
 rig.update(arm_mask=arm_mask,arm_image=arm_image,body_without_arm=body_without_arm)
 rig.update(image=image,body=body,parts=parts,offset=(GROUND[0]-rig['sole'][0],GROUND[1]-rig['sole'][1]))
 return rig
RIGS=[make_rig(d) for d in range(8)]

def displacement(phase,move,index):
 p=(phase+index*.5)%1
 if p<.5:s=.5-2*p;lift=0
 else:s=-.5+2*(p-.5);lift=math.sin((p-.5)*math.tau)*15
 stride=math.hypot(move[0]*50,move[1]*33)
 return move[0]*s*stride,move[1]*s*stride-lift,p<.5

def render(direction,move_direction,phase,walking=True,attacking=False,attack_frame=0):
 rig=RIGS[direction];out=Image.new('RGBA',CANVAS);offset=rig['offset']
 if not walking and not attacking:out.alpha_composite(rig['image'],tuple(map(round,offset)));return out
 angle=move_direction*math.pi/4;move=(-math.sin(angle),math.cos(angle))
 # Feet are ordered by world depth; body/robe/hair always occlude upper legs.
 posed=[]
 for index,(leg,upper,lower) in enumerate(rig['parts']):
  dx,dy,contact=displacement(phase,move,index) if walking else (0,14,True);hip,knee,ankle=[leg[k] for k in ('hip','knee','ankle')]
  target=(ankle[0]+dx,ankle[1]-14+dy)
  k=(knee[0]+dx*.48,knee[1]+(-14+dy)*.45)
  posed.append((target[1],bone(upper,hip,knee,hip,k,offset),bone(lower,knee,ankle,k,target,offset)))
 for _,upper,lower in sorted(posed,key=lambda part:part[0]):out.alpha_composite(upper);out.alpha_composite(lower)
 if attacking:
  out.alpha_composite(attack_upper(direction,attack_frame),tuple(map(round,offset)))
 else:out.alpha_composite(rig['body'],tuple(map(round,offset)))
 return out

def attack_upper(direction,frame):
 from PerlicaAttackLayers import upper
 return upper(direction,frame)

def preview():
 # Three rows per facing: forward, backpedal, lateral. Original silhouette
 # remains oriented to aim in all three, including rear-hair occlusion.
 sheet=Image.new('RGBA',(8*160,3*160),'#777b80');frames=[]
 for frame in range(16):
  im=Image.new('RGBA',sheet.size,'#777b80')
  for aim in range(8):
   for row,relative in enumerate((0,4,2)):
    sprite=render(aim,(aim+relative)%8,frame/16).resize((128,128),Image.Resampling.NEAREST)
    im.alpha_composite(sprite,(aim*160+16,row*160+16))
  frames.append(im.convert('RGB'))
 frames[0].save(ROOT/'perlica-eight-facing-gait.gif',save_all=True,append_images=frames[1:],duration=50,loop=0)
 frames[4].save(ROOT/'perlica-eight-facing-gait.png')
 # Source-rest proof is exact before consistent packing; no head rescale per cell.
 for rig in RIGS:
  recomposed=Image.new('RGBA',rig['image'].size)
  for leg,_,_ in rig['parts']:
   mask=leg['mask'];recomposed.paste(rig['image'],(0,0),mask)
  alpha=rig['body'].getchannel('A');recomposed.alpha_composite(rig['body'])
  # Alpha overlays are byte-equal in opaque art; compare visible source pixels.
  for src,dst in zip(rig['image'].get_flattened_data(),recomposed.get_flattened_data()):
   if src[3]==255:assert src==dst
 print('PASS: 8 original facing banks;16 articulated poses each, forward/back/strafe preview; original opaque pixels recomposed without changes. Independent arm composition is provided by PerlicaAttackLayers.')
if __name__=='__main__':preview()
