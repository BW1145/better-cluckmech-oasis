"""Compile isolated sprite parts, portraits and effect frames into a runtime atlas."""
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parent.parent
SOURCES = ROOT / 'art' / 'StratagemParts'
DEST = ROOT / 'src' / 'StratagemAssets'
names = ['md6','barrage380','orbitalLaser','eagle500','emsMortar','shieldRelay','laserRover']
rows = [[0,182,368,544,735,909,1086],[0,213,376,566,736,900,1086],
        [0,195,385,548,735,901,1086],[0,210,402,592,774,948,1086],
        [0,199,396,574,751,901,1086],[0,202,392,582,727,906,1086],
        [0,194,370,524,734,920,1086]]
frames = {}


def isolate(tile):
    """Find the principal opaque island; transparent import padding is kept separately."""
    alpha = np.asarray(tile)[:,:,3]
    occupied = alpha >= 144
    seen = np.zeros(occupied.shape, dtype=bool)
    largest = []
    for y,x in zip(*np.nonzero(occupied)):
        if seen[y,x]: continue
        stack = [(int(x),int(y))]
        seen[y,x] = True
        component = []
        while stack:
            px,py = stack.pop()
            component.append((px,py))
            for dx,dy in [(1,0),(-1,0),(0,1),(0,-1)]:
                nx,ny = px+dx,py+dy
                if 0 <= nx < occupied.shape[1] and 0 <= ny < occupied.shape[0] and occupied[ny,nx] and not seen[ny,nx]:
                    seen[ny,nx] = True; stack.append((nx,ny))
        if len(component) > len(largest): largest = component
    if not largest: raise ValueError('Empty frame')
    mask=np.zeros(occupied.shape,dtype=np.uint8)
    for x,y in largest:mask[y,x]=255
    mask=np.asarray(Image.fromarray(mask).filter(ImageFilter.MaxFilter(5)))
    clean=np.array(tile)
    clean[:,:,3]=np.where(mask>0,alpha,0)
    tile=Image.fromarray(clean)
    xs,ys = zip(*largest)
    bounds = (max(0,min(xs)-2),max(0,min(ys)-2),min(tile.width,max(xs)+3),min(tile.height,max(ys)+3))
    return tile.crop(bounds)


def grid(file, columns, lines, index):
    im = Image.open(SOURCES / (file+'.png')).convert('RGBA')
    x,y = index%columns,index//columns
    return im.crop((round(x*im.width/columns),round(y*im.height/lines),round((x+1)*im.width/columns),round((y+1)*im.height/lines)))


def legacy(kind,row,column):
    im = Image.open(DEST / (names[kind]+'.png')).convert('RGBA')
    return im.crop((column*181,rows[kind][row],(column+1)*181,rows[kind][row+1]))


def add(name, tile, anchor=(.5,.5), portrait=False, islands=False):
    if not islands: tile = isolate(tile)
    else:
        a=np.asarray(tile)[:,:,3]
        y,x=np.nonzero(a>=144)
        if not len(x): raise ValueError(name)
        tile=tile.crop((int(x.min()),int(y.min()),int(x.max()+1),int(y.max()+1)))
    limit=38 if portrait else 88
    scale=limit/max(tile.size)
    size=(max(1,round(tile.width*scale)),max(1,round(tile.height*scale)))
    tile=tile.resize(size,Image.Resampling.NEAREST)
    frames[name]=(tile,anchor,portrait,25 if portrait else 32)


def assembled(parts):
    """Art layout uses the deployed rig's mounts and world dimensions."""
    canvas=Image.new('RGBA',(320,320))
    origin=(160,240)
    for name,width,x,y in parts:
        tile,pivot,_,ppu=frames[name]
        scale=width*64/tile.width
        w,h=round(tile.width*scale),round(tile.height*scale)
        canvas.alpha_composite(tile.resize((w,h),Image.Resampling.NEAREST),
            (round(origin[0]+x*64-pivot[0]*w),round(origin[1]-y*64-(1-pivot[1])*h)))
    bounds=canvas.getbbox()
    tile=canvas.crop(bounds)
    pivot=((origin[0]-bounds[0])/tile.width,1-(origin[1]-bounds[1])/tile.height)
    return tile,pivot


add('mine-base',grid('md6Parts',3,2,0),(.5,.22))
add('mine-drum',grid('md6Parts',3,2,1),(.5,.06))
add('mine-left',grid('md6Parts',3,2,2),(.86,.94))
add('mine-right',grid('md6Parts',3,2,3),(.14,.94))
add('mine',grid('md6Parts',3,2,5))
add('shield-base',grid('shieldParts',2,2,0),(.5,.22))
add('shield-tower',grid('shieldParts',2,2,1),(.5,.05))
add('shield-fragments',grid('shieldParts',2,2,3),islands=True)
add('mortar-base',grid('emsPartsResult',4,5,0),(.5,.22))
for n in range(8):
    add('mortar-housing.'+str(n),grid('emsHousings',4,2,n),(.5,.06))
    add('mortar-barrel.'+str(n),grid('emsPartsResult',4,5,8+n),(.5,.1))
add('mortar-spark',grid('emsPartsResult',4,5,17),islands=True)
add('mortar-shell',grid('emsPartsResult',4,5,18))
add('rover-pack',grid('roverParts',3,3,0))
for n in range(8): add('rover-drone.'+str(n),grid('roverParts',3,3,n+1))

layouts={
    20001:[('mine-base',1.8,0,0),('mine-drum',.96,0,.32),('mine-left',.55,-.3,.99),('mine-right',.55,.3,.99)],
    20005:[('mortar-base',1.8,0,0),('mortar-housing.2',1.4*frames['mortar-housing.2'][0].width/frames['mortar-housing.2'][0].height,0,.3),
           ('mortar-barrel.2',.95*frames['mortar-barrel.2'][0].width/frames['mortar-barrel.2'][0].height,0,.86)],
    20006:[('shield-tower',.76,0,.35),('shield-base',1.8,0,0)],
    20007:[('rover-pack',.85,0,0)]}
portrait_mortar=[('mortar-base',1.8,0,0),
    ('mortar-housing.3',1.4*frames['mortar-housing.3'][0].width/frames['mortar-housing.3'][0].height,0,.3),
    ('mortar-barrel.3',.95*frames['mortar-barrel.3'][0].width/frames['mortar-barrel.3'][0].height,0,.86)]
icons=[assembled(layouts[20001])[0],legacy(1,0,0),legacy(2,5,0),legacy(3,1,0),
       assembled(portrait_mortar)[0],assembled(layouts[20006])[0],legacy(6,0,4)]
for n,tile in enumerate(icons):
    unit=20001+n
    add('icon.'+str(unit),tile,portrait=True,islands=unit in layouts)
    if unit in layouts:
        image,pivot=assembled(layouts[unit])
    else:
        # Signal cards deploy the same ground beacon shown by the live rig.
        source_tile=legacy(1,0,0) if unit==20002 else legacy(2,5,0)
        add('preview-source',source_tile)
        image,pivot=assembled([('preview-source',1.2,0,0)])
        del frames['preview-source']
    frames['preview.'+str(unit)]=(image,pivot,False,64)
add('icon.20101',grid('md6Parts',3,2,5),portrait=True)
add('icon.20102',grid('roverParts',3,3,1),portrait=True)

active=[25011,25014,25015,25016,25021,25022,25023,25024,25026,
        25031,25032,25033,25035,25036,25041,25043,25044,25045,25046,
        25051,25052,25053,25055,25061,25062,25063,25064,25065,
        25071,25072,25074,25075,25076]
for index,affix in enumerate(active): add('affix.'+str(affix),grid('upgradeIcons',6,6,index),portrait=True)

fx=['flash','fire-small','fire-big','fragments','smoke','smoke-thin','crater.0','crater.1','streak','core','shock','spark']
for index,name in enumerate(fx):
    add('fx.'+name,grid('vfxParts',4,3,index),(.5,0) if name in ['fire-big','smoke','smoke-thin'] else (.5,.5),islands=name in ['fragments','smoke-thin'])
selected={1:{0:[0]},2:{5:[0],1:[0]},3:{0:[0],1:[0,4]}}
for kind,selections in selected.items():
    for row,columns in selections.items():
        for column in columns:
            anchor=(.5,0) if (kind==1 and row in [2,3]) or (kind==3 and row in [2,3]) else (.5,.5)
            add('old.'+str(kind)+'.'+str(row)+'.'+str(column),legacy(kind,row,column),anchor)

CELL=160
columns=16
lines=(len(frames)+columns-1)//columns
atlas=Image.new('RGBA',(columns*CELL,lines*CELL))
metadata={}
for index,(name,(tile,pivot,portrait,ppu)) in enumerate(frames.items()):
    ox,oy=(index%columns)*CELL,(index//columns)*CELL
    if portrait:
        canvas=Image.new('RGBA',(50,50))
        canvas.alpha_composite(tile,((50-tile.width)//2,(50-tile.height)//2))
        atlas.alpha_composite(canvas,(ox,oy))
        x,y,w,h=ox,oy,50,50
    else:
        x,y=ox+(CELL-tile.width)//2,oy+(CELL-tile.height)//2
        w,h=tile.size
        atlas.alpha_composite(tile,(x,y))
    metadata[name]={'X':x,'Y':y,'W':w,'H':h,'PX':pivot[0],'PY':pivot[1],'PPU':ppu}
    if name.startswith('mortar-barrel.'):
        alpha=np.asarray(tile)[:,:,3]
        ys,xs=np.nonzero(alpha[:5]>=144)
        metadata[name]['MX']=(float(xs.mean())-pivot[0]*w)/32
        metadata[name]['MY']=(h-float(ys.mean())-pivot[1]*h)/32
atlas.save(DEST/'stratagem-v2.png',optimize=True)
(DEST/'stratagem-v2.json').write_text(json.dumps(metadata,separators=(',',':'))+'\n',encoding='utf-8')
print(json.dumps({'sprites':len(metadata),'atlas':atlas.size,'bytes':(DEST/'stratagem-v2.png').stat().st_size,'activeUpgrades':len(active)}))
