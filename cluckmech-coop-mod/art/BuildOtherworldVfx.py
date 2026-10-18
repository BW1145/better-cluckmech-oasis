"""Pack separately authored effects with stable game-space anchors."""
import json
from pathlib import Path
from PIL import Image

root=Path(__file__).resolve().parent
source=root/'Otherworld'
output=root.parent/'src'/'OtherworldAssets'
atlas=Image.new('RGBA',(1024,1024))
regions={}
cursor=0

def record(key,tile):
    global cursor
    x=(cursor%8)*128;y=(cursor//8)*128
    atlas.alpha_composite(tile,(x,y))
    box=tile.getchannel('A').point(lambda v:255 if v>=96 else 0).getbbox() or (0,0,1,1)
    regions[key]=[x,y,tile.width,tile.height,*box]
    cursor+=1

for family,filename,rows in [('projectile','electric-projectile-source.png',2),('chain','electric-chain-source.png',2),('strike','electric-strike-source.png',2),('impact','electric-impact-source.png',2),('fire','spiral-fire-source.png',4)]:
    sheet=Image.open(source/filename).convert('RGBA')
    for i in range(4*rows):
        x=i%4;y=i//4
        cell=sheet.crop((round(x*sheet.width/4),round(y*sheet.height/rows),round((x+1)*sheet.width/4),round((y+1)*sheet.height/rows)))
        cell.thumbnail((120,120),Image.Resampling.NEAREST)
        tile=Image.new('RGBA',(128,128));tile.alpha_composite(cell,((128-cell.width)//2,(128-cell.height)//2))
        record(('spiral.' if family=='fire' else 'electric.')+family+'.'+str(i),tile)

block=Image.open(source/'orbital-block-source.png').convert('RGBA')
box=block.getchannel('A').point(lambda v:255 if v>=96 else 0).getbbox()
block=block.crop(box);block.thumbnail((116,240),Image.Resampling.NEAREST)
tile=Image.new('RGBA',(128,256));tile.alpha_composite(block,((128-block.width)//2,240-block.height))
record('orbital.block',tile)
atlas.save(output/'vfx.png')
(output/'vfx.json').write_text(json.dumps(regions,separators=(',',':')),encoding='utf-8')
print(f'Packed {len(regions)} effects; RGBA {atlas.width*atlas.height*4} bytes')
