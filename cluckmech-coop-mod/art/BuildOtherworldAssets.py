"""Slice authored character/VFX sheets into a shared runtime atlas."""
import json
from pathlib import Path
from PIL import Image

root=Path(__file__).resolve().parent
source=root/'Otherworld'
output=root.parent/'src'/'OtherworldAssets'
output.mkdir(parents=True,exist_ok=True)
atlas=Image.new('RGBA',(1024,2048))
rects={}
cursor=0
def add(key,im,pixels=64,scale=None,centered=False,anchor=None,opaque=False):
 global cursor
 box=im.getchannel('A').point(lambda alpha:255 if alpha>=128 else 0).getbbox() if opaque else im.getbbox()
 if not box: box=(0,0,1,1)
 cropped=im if centered else im.crop(box)
 if scale is None: scale=min((pixels-8)/cropped.width,(pixels-8)/cropped.height)
 cropped=cropped.resize((max(1,round(cropped.width*scale)),max(1,round(cropped.height*scale))),Image.Resampling.NEAREST)
 tile=Image.new('RGBA',(64,64))
 left=(64-cropped.width)//2 if anchor is None else 32-round((anchor-box[0])*scale)
 tile.alpha_composite(cropped,(left,(64-cropped.height)//2 if centered else 60-cropped.height))
 x=(cursor%16)*64;y=(cursor//16)*64
 visible=tile.getchannel('A').point(lambda alpha:255 if alpha>=128 else 0).getbbox() or (0,0,1,1)
 atlas.alpha_composite(tile,(x,y));rects[key]=[x,y,64,64,*visible];cursor+=1

def character(hero,sheet,rows,action=None,height=None,vertical=False):
 cells=[sheet.crop((round(frame*sheet.width/4),round(direction*sheet.height/8),round((frame+1)*sheet.width/4),round((direction+1)*sheet.height/8))) for frame in range(4) for direction in range(8)] if vertical else [sheet.crop((round(col*sheet.width/8),rows[row],round((col+1)*sheet.width/8),rows[row+1])) for row in range(4) for col in range(8)]
 if hero==12 and vertical and action=='walk':cells[3*8+3]=cells[1*8+3]
 bounds=[cell.getchannel('A').point(lambda alpha:255 if alpha>=128 else 0).getbbox() or (0,0,1,1) for cell in cells]
 anchors=[]
 for cell,b in zip(cells,bounds):
  bottom=cell.getchannel('A').crop((b[0],round(b[3]-(b[3]-b[1])*.25),b[2],b[3])).point(lambda alpha:255 if alpha>=128 else 0).getbbox()
  anchors.append(b[0]+(bottom[0]+bottom[2])/2 if bottom else (b[0]+b[2])/2)
 scale=min(56/max(2*max(a-b[0],b[2]-a) for a,b in zip(anchors,bounds)),48/max(b[3]-b[1] for b in bounds))
 for i,cell in enumerate(cells): add(f'hero.{hero}.{i//8}.{i%8}' if action is None else f'anim.{hero}.{action}.{i//8}.{i%8}',cell,scale=scale if height is None else height/(bounds[i][3]-bounds[i][1]),anchor=anchors[i],opaque=True)
 return round((bounds[0][3]-bounds[0][1])*scale)

perlica=Image.open(source/'perlica-source.png').convert('RGBA')
perlica_height=character(12,perlica,[0,270,506,725,perlica.height])
obito=Image.open(source/'obito-source.png').convert('RGBA')
obito_height=character(13,obito,[0,270,506,725,obito.height])
for hero,name in ((12,'perlica'),(13,'obito')):
 for action in ('walk','attack','moving-attack'):
  vertical=(source/f'{name}-{action}-vertical.png').exists()
  sheet=Image.open(source/(f'{name}-{action}-vertical.png' if vertical else f'{name}-{action}.png')).convert('RGBA')
  if vertical:
   character(hero,sheet,None,action,perlica_height if hero==12 else obito_height,True)
   continue
  alpha=sheet.getchannel('A');spans=[];start=None
  for y in range(sheet.height):
   active=sum(value>=128 for value in alpha.crop((0,y,sheet.width,y+1)).get_flattened_data())>=sheet.width*.008
   if active and start is None:start=y
   if not active and start is not None:spans.append((start,y));start=None
  if start is not None:spans.append((start,sheet.height))
  if len(spans)==4:rows=[0]+[(spans[i-1][1]+spans[i][0])//2 for i in range(1,4)]+[sheet.height]
  else:
   counts=[sum(value>=128 for value in alpha.crop((0,y,sheet.width,y+1)).get_flattened_data()) for y in range(sheet.height)]
   rows=[0]+[min(range(round(sheet.height*low),round(sheet.height*high)),key=lambda y:counts[y]) for low,high in ((.23,.33),(.48,.60),(.72,.84))]+[sheet.height]
  character(hero,sheet,rows,action,perlica_height if hero==12 else obito_height)
effects=Image.open(source/'effects-source.png').convert('RGBA')
effect_rows=[0,155,305,423,608,704,835,980,effects.height]
for row in (0,1,2,5,6):
 for col in range(8):
  add(f'effect.{row}.{col}',effects.crop((round(col*effects.width/8),effect_rows[row],round((col+1)*effects.width/8),effect_rows[row+1])),scale=min(56/(effects.width/8),56/(effect_rows[row+1]-effect_rows[row])),centered=True)
kamui=Image.open(source/'kamui-source.png').convert('RGBA')
columns,rows=4,4
for i in range(16):
 col,row=i%columns,i//columns
 cell=kamui.crop((round(col*kamui.width/columns),round(row*kamui.height/rows),round((col+1)*kamui.width/columns),round((row+1)*kamui.height/rows)))
 add(f'kamui.{i//8}.{i%8}',cell,scale=min(56/(kamui.width/columns),56/(kamui.height/rows)),centered=True)
icons=Image.open(source/'talents-source.png').convert('RGBA')
talents=list(range(26001,26009))+list(range(26101,26125))
for i,talent in enumerate(talents):
 col,row=i%8,i//8
 add(f'talent.{talent}',icons.crop((round(col*icons.width/8),round(row*icons.height/4),round((col+1)*icons.width/8),round((row+1)*icons.height/4))),centered=True)
projectiles=Image.open(source/'projectiles-source.png').convert('RGBA')
for row in range(2):
 for col in range(8):
  add(f'projectile.{row}.{col}',projectiles.crop((round(col*projectiles.width/8),round(row*projectiles.height/2),round((col+1)*projectiles.width/8),round((row+1)*projectiles.height/2))),centered=True)
atlas=atlas.crop((0,0,1024,((cursor+15)//16)*64))
atlas.save(output/'characters.png')
(output/'characters.json').write_text(json.dumps(rects,ensure_ascii=False,separators=(',',':')),encoding='utf-8')
Image.open(source/'space-source.png').resize((768,512),Image.Resampling.NEAREST).save(output/'space.png')
print(f'Packed {cursor} sprites, atlas RGBA {atlas.width*atlas.height*4} bytes')
