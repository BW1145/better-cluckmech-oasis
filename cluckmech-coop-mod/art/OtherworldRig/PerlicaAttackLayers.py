"""Source-pixel firing-arm replacement without replacing Perlica's upper body.

upper(direction, frame) returns the existing rig's original crop dimensions.
Original head, hair and visible torso are retained once. Only authored sleeves,
hands and the complete terminal blade are imported from the first firing column.
No runtime files or the existing gait renderer are edited by this module.
"""
from pathlib import Path
import math
import hashlib
import json
from PIL import Image, ImageDraw, ImageChops, ImageFilter
import numpy as np

ROOT=Path(__file__).resolve().parent
FIRING=ROOT.parent/'Otherworld/perlica-moving-attack-vertical.png'
REAR_PATCH=ROOT/'perlica-rear-raised-arm-source.png'
# Coordinates are native to the first column of each authored firing row.
ARMS=[
 [(91,109),(108,112),(117,118),(130,114),(143,110),(158,113),(166,122),(163,136),(149,141),(134,140),(130,169),(120,174),(113,143),(99,140),(89,133),(86,120)],
 [(135,108),(148,112),(158,121),(154,135),(140,139),(127,135),(113,144),(95,148),(77,161),(16,188),(10,184),(45,150),(72,128),(89,121),(99,116),(117,118),(127,119)],
 [(140,95),(152,100),(157,113),(150,124),(132,134),(115,132),(106,128),(102,123),(86,125),(72,117),(46,108),(0,93),(0,80),(22,83),(64,91),(89,93),(105,95),(116,97),(133,98)],
 [(14,18),(36,25),(58,41),(71,54),(86,59),(99,71),(105,87),(114,91),(123,98),(126,110),(115,118),(99,117),(82,113),(73,103),(73,94),(64,84),(57,66),(43,50),(28,38)],
 [],
 [(140,92),(151,95),(161,88),(171,76),(178,61),(193,32),(201,28),(205,35),(192,66),(186,83),(184,98),(179,110),(163,120),(147,118),(137,109),(133,100)],
 [(132,93),(143,93),(151,88),(166,87),(180,86),(202,79),(247,64),(252,70),(244,84),(220,99),(201,107),(181,108),(172,115),(157,123),(143,120),(129,114),(124,104)],
 [(121,93),(133,98),(145,102),(157,105),(172,110),(187,128),(221,170),(222,181),(211,180),(181,158),(161,142),(145,137),(128,127),(117,119),(111,110)]
]
CENTERS=[126,123,133,130,130,130,125,132]
PIVOT_Y=[121,123,111,98,116,101,106,112]
TOPS=[25,21,10,0,4,0,0,0]
HIPS=[178,164,160,157,165,155,155,153]
# Opposite hanging sleeve/hand, in the ORIGINAL cropped idle image coordinates.
SECOND_ARMS=[
 [(146,113),(157,117),(169,126),(178,142),(185,150),(183,163),(176,170),(169,168),(162,160),(155,149),(144,132),(140,120)],
 [(135,113),(147,116),(164,132),(177,148),(181,165),(172,180),(157,166),(147,151),(138,140)],
 [(109,112),(122,116),(139,132),(155,153),(159,178),(149,194),(130,181),(120,160),(108,137)],
 [(107,112),(124,116),(144,132),(166,153),(173,177),(162,196),(142,180),(126,159),(114,138)],
 [],
 [(40,113),(53,119),(58,142),(50,161),(44,183),(31,193),(20,181),(23,160),(31,137)],
 [(52,115),(66,120),(75,136),(69,159),(56,182),(45,193),(35,181),(39,159),(43,136)],
 [(41,114),(55,119),(67,135),(63,155),(50,180),(36,195),(23,182),(24,163),(33,139)]
]
ANGLES=(-7.,0.,9.,3.)
_CACHE={}
_INFO={}


def _poly(size,points):
    mask=Image.new('L',size)
    if points:ImageDraw.Draw(mask).polygon(points,fill=255)
    return mask


def _foreground(rig):
    im=rig['body'];a=np.asarray(im).astype(int);yy=np.indices((im.height,im.width))[0]
    # Existing actual brown hair and its dark outline are restored from the
    # idle source, rather than importing any firing-pose hair silhouette.
    brown=(a[:,:,0]>a[:,:,1]+13)&(a[:,:,1]>a[:,:,2]+12)&(yy<151)&(a[:,:,3]>0)
    nearby=Image.fromarray((brown*255).astype('uint8')).filter(ImageFilter.MaxFilter(3))
    dark=(a[:,:,0]<100)&(a[:,:,1]<90)&(a[:,:,2]<85)
    keep=brown|((np.asarray(nearby)>0)&dark)
    hair=Image.fromarray((keep*255).astype('uint8'))
    draw=ImageDraw.Draw(hair);draw.rectangle((0,0,im.width,109),fill=255)
    return hair


def _remove_mask(direction,rig,foreground):
    im=rig['body'];mask=rig['arm_mask'].copy();d=ImageDraw.Draw(mask)
    arm=rig['arm']
    # Capture the complete white blade and dark outline even where the rough
    # original arm corridor missed its far edge. No stationary second blade.
    d.line([arm['hand'],arm['tip']],fill=255,width=44)
    for p in (arm['hand'],arm['tip']):d.ellipse((p[0]-16,p[1]-16,p[0]+16,p[1]+16),fill=255)
    if direction<4:
        edge=min(p[0] for leg in rig['legs'] for p in leg['polygon'])-1
        d.rectangle((0,166,round(edge),im.height),fill=255)
    else:
        edge=max(p[0] for leg in rig['legs'] for p in leg['polygon'])+1
        d.rectangle((round(edge),166,im.width,im.height),fill=255)
    if SECOND_ARMS[direction]:d.polygon(SECOND_ARMS[direction],fill=255)
    # Head, rear hair and original exposed shoulder pixels always own the
    # foreground. This prevents cutting away hair to expose a replacement arm.
    mask=mask.filter(ImageFilter.MaxFilter(5))
    mask=ImageChops.subtract(mask,foreground)
    return mask


def _source_arm(direction):
    if direction==4:
        source=Image.open(REAR_PATCH).convert('RGBA')
        points=[(665,250),(680,110),(705,8),(731,8),(744,114),(761,247),(767,307),(788,360),(801,457),(789,516),(781,588),(759,637),(740,598),(733,520),(741,449),(749,392),(750,337),(733,280)]
        mask=_poly(source.size,points)
        return source,mask,(755.,545.),.22,'generated rear raised-arm fragment only'
    src=Image.open(FIRING).convert('RGBA');y0=round(direction*src.height/8);y1=round((direction+1)*src.height/8)
    source=src.crop((0,y0,260,y1));mask=_poly(source.size,ARMS[direction])
    return source,mask,(float(CENTERS[direction]),float(PIVOT_Y[direction])),153/(HIPS[direction]-TOPS[direction]),'authored firing first column'


def _mapping(source,size,pivot,target,scale,angle):
    c=math.cos(angle);s=math.sin(angle)
    A=c/scale;B=s/scale;D=-s/scale;E=c/scale
    return source.transform(size,Image.Transform.AFFINE,
        (A,B,pivot[0]-A*target[0]-B*target[1],D,E,pivot[1]-D*target[0]-E*target[1]),Image.Resampling.NEAREST)


def _clean_arm_residue(image, foreground):
    """Remove detached old-arm scraps, retaining the silhouette's antialiasing.

    Ownership cleanup is restricted below the original head and outside hair.
    The connected opaque silhouette is expanded two pixels only to keep its
    original antialiased edge. No pixels are painted or moved by this pass.
    """
    a=np.asarray(image).copy();a[a[:,:,3]<=8]=0
    opaque=a[:,:,3]>=128;h,w=opaque.shape;seen=np.zeros((h,w),dtype=bool)
    components=[]
    for y,x in zip(*np.nonzero(opaque)):
        if seen[y,x]:continue
        stack=[(int(x),int(y))];seen[y,x]=True;component=[]
        while stack:
            xx,yy=stack.pop();component.append((xx,yy))
            for ny in range(max(0,yy-1),min(h,yy+2)):
                for nx in range(max(0,xx-1),min(w,xx+2)):
                    if opaque[ny,nx] and not seen[ny,nx]:
                        seen[ny,nx]=True;stack.append((nx,ny))
        components.append(component)
    if not components:return Image.fromarray(a)
    largest=max(components,key=len);support=Image.new('L',image.size)
    pixels=support.load()
    for x,y in largest:pixels[x,y]=255
    support=support.filter(ImageFilter.MaxFilter(5))
    eligible=(np.indices((h,w))[0]>=110)&(np.asarray(foreground)==0)
    a[eligible&(np.asarray(support)==0)]=0
    return Image.fromarray(a)


def _build(direction):
    # Deliberately lazy: the gait module may itself import upper().
    from BuildPerlicaEightMotion import RIGS
    rig=RIGS[direction];original=rig['body'];size=original.size
    foreground=_foreground(rig);remove=_remove_mask(direction,rig,foreground)
    source,arm_mask,pivot,scale,origin=_source_arm(direction)
    arm=Image.new('RGBA',source.size);arm.paste(source,(0,0),arm_mask)
    if direction==4:
        target=(rig['arm']['shoulder'][0],rig['arm']['shoulder'][1])
    else:
        target=(rig['pelvis'][0],170+(PIVOT_Y[direction]-HIPS[direction])*scale)
    # Uniform arm-only fit protects the complete blade inside the caller's
    # original crop. The body's original scale and head size never change.
    bbox=arm.getbbox();rotated=[]
    for degrees in ANGLES:
        a=math.radians(degrees);c=math.cos(a);s=math.sin(a)
        for x,y in ((bbox[0],bbox[1]),(bbox[2],bbox[1]),(bbox[0],bbox[3]),(bbox[2],bbox[3])):
            dx=x-pivot[0];dy=y-pivot[1];rotated.append((dx*c-dy*s,dx*s+dy*c))
    for dx,dy in rotated:
        if dx<0:scale=min(scale,(target[0]-2)/-dx)
        elif dx>0:scale=min(scale,(size[0]-3-target[0])/dx)
        if dy<0:scale=min(scale,(target[1]-2)/-dy)
        elif dy>0:scale=min(scale,(size[1]-3-target[1])/dy)
    base=original.copy();base.paste((0,0,0,0),(0,0,*size),remove)
    # Reuse authored cloth only in the removed arm's interior torso socket.
    # The source arm itself is excluded, so this never creates another weapon.
    underpaint=Image.new('RGBA',size)
    if direction!=4:
        cloth=source.copy();cloth.paste((0,0,0,0),(0,0,*source.size),arm_mask)
        body_scale=153/(HIPS[direction]-TOPS[direction])
        body_target=(rig['pelvis'][0],170+(PIVOT_Y[direction]-HIPS[direction])*body_scale)
        underpaint=_mapping(cloth,size,pivot,body_target,body_scale,0)
        interior=Image.new('L',size);ImageDraw.Draw(interior).rectangle((rig['pelvis'][0]-35,111,rig['pelvis'][0]+36,174),fill=255)
        underpaint.putalpha(ImageChops.multiply(underpaint.getchannel('A'),ImageChops.multiply(remove,interior)))
        base.alpha_composite(underpaint)
    # Some raised-source sleeves also occlude the shirt. Fill those remaining
    # newly exposed interior sockets with exact pixels from the original lower
    # white tunic, never with a new drawing or a second head/hair layer.
    fallback=Image.new('RGBA',size);fp=fallback.load();op=original.load();rp=remove.load();bp=base.load()
    cx=round(rig['pelvis'][0])
    for yy in range(112,min(174,size[1])):
        for xx in range(max(0,cx-25),min(size[0],cx+26)):
            if rp[xx,yy] and bp[xx,yy][3]<128:
                sx=max(cx-18,min(cx+18,xx));sy=max(145,min(168,yy+30))
                fp[xx,yy]=op[sx,sy]
    base.alpha_composite(fallback)
    # Preserve head/hair once, above the moving arm. All original torso pixels
    # outside the removed old-arm owner remain in base unmodified.
    fg=Image.new('RGBA',size);fg.paste(original,(0,0),foreground)
    base.paste((0,0,0,0),(0,0,*size),foreground)
    phases=[]
    for frame,degrees in enumerate(ANGLES):
        out=base.copy();out.alpha_composite(_mapping(arm,size,pivot,target,scale,math.radians(degrees)));out.alpha_composite(fg)
        phases.append(_clean_arm_residue(out,foreground))
    _CACHE[direction]=phases
    _INFO[direction]={'direction':direction,'arm_source':origin,'arm_only_uniform_scale':scale,
        'target_pivot':target,'original_crop':list(size),'recoil_degrees':list(ANGLES),
        'old_arm_removal_pixels':sum(v>0 for v in remove.tobytes()),
        'foreground_source_hash':hashlib.sha256(fg.tobytes()).hexdigest(),
        'native_distinct_attack_phases':len({hashlib.sha256(p.tobytes()).hexdigest() for p in phases}),
        'packed_distinct_attack_phases':len({hashlib.sha256(p.resize((max(1,round(p.width*.2)),max(1,round(p.height*.2))),Image.Resampling.NEAREST).tobytes()).hexdigest() for p in phases})}


def upper(direction,frame):
    direction=int(direction)%8
    if direction not in _CACHE:_build(direction)
    return _CACHE[direction][int(frame)%4].copy()


def review():
    from BuildPerlicaEightMotion import RIGS
    out=Image.new('RGB',(4*280,8*290),'#626970');draw=ImageDraw.Draw(out)
    for direction in range(8):
        rig=RIGS[direction]
        for frame in range(4):
            canvas=Image.new('RGBA',(320,320));canvas.alpha_composite(upper(direction,frame),tuple(map(round,rig['offset'])))
            # Original legs are shown solely to inspect the upper/lower join.
            legs=rig['image'].copy();alpha=ImageChops.subtract(legs.getchannel('A'),rig['body'].getchannel('A'));legs.putalpha(alpha)
            canvas.alpha_composite(legs,tuple(map(round,rig['offset'])))
            tile=canvas.resize((256,256),Image.Resampling.NEAREST)
            out.paste(tile,(frame*280,direction*290+24),tile)
            draw.text((frame*280+8,direction*290+7),f'{direction}: attack {frame}',fill='white')
    out.save(ROOT/'perlica-exact-arm-attack-montage.png')
    (ROOT/'perlica-exact-arm-attack-validation.json').write_text(json.dumps(list(_INFO.values()),indent=2),encoding='utf-8')
    assert all(x['native_distinct_attack_phases']==4 and x['packed_distinct_attack_phases']==4 for x in _INFO.values())
    print('PASS: four source-arm phases for all eight directions; original head/hair/visible torso retained.')
    print(ROOT/'perlica-exact-arm-attack-montage.png')

if __name__=='__main__':review()
