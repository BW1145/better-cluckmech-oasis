"""Eight-facing, sixteen-phase source-pixel Obito locomotion proof.

The original head, hair, robe and open-robe slit stay in a fixed source layer.
Held-arm attacks use a complete hand and kunai from the original authored
attack row, plus source torso underpaint restricted to the removed idle arm. Independently cut socks/sandals articulate behind that layer.
Hidden thighs have skeletal joints but no fabricated artwork. A deliberately
conservative stride keeps the short source shin covered by the unchanged hem.
This is a review proof, not an exported production animation or finished rig.
"""
import hashlib
import json
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageChops
from ObitoRigDefinition import ROOT, SOURCE, ORDER, RIGS, load_direction

SIZE=(320,320)
SOURCE_FOOT_ANCHOR=(160,300)
PACK_SCALE=.2
PHASE_COUNT=16
CYCLE_SECONDS=.8
GROUND_TRAVEL=(84.,48.)  # native source pixels traveled over one gait cycle
FOOT_BASE_LIFT=15.      # keeps the visible source shin under the existing robe
SWING_LIFT=12.
DIRECTION_VECTORS=((0.,1.),(-2**-.5,2**-.5),(-1.,0.),(-2**-.5,-2**-.5),
                   (0.,-1.),(2**-.5,-2**-.5),(1.,0.),(2**-.5,2**-.5))
MODES=('forward','backward','strafe-left')
CACHE=[]
LAYER_CACHE=[]
for index in range(8):
    source,layers,masks,rig=load_direction(index)
    ownership=Image.new('L',source.size)
    for name in ORDER:
        if name.endswith('-leg'):ownership=ImageChops.lighter(ownership,masks[name])
    body=Image.new('RGBA',source.size)
    body_mask=ImageChops.invert(ownership)
    body.paste(source,(0,0),body_mask)
    rest=body.copy()
    for name in ORDER:
        if name.endswith('-leg'):rest.paste(layers[name],(0,0),masks[name])
    assert rest.tobytes()==source.tobytes(), 'Body removal/rest reconstruction must be exact.'
    parts={}
    for leg in ('left','right'):
        lower=layers[f'{leg}-lower-leg']
        # The sandal stays rigid and translated. Only the short exposed shin
        # uses a knee-to-ankle bone transform, so sole contact remains exact.
        ay=rig['anchors'][f'{leg}-ankle'][1]
        shin_mask=Image.new('L',source.size)
        ImageDraw.Draw(shin_mask).rectangle((0,0,source.width-1,ay-1),fill=255)
        foot_mask=ImageChops.invert(shin_mask)
        shin=Image.new('RGBA',source.size);shin.paste(lower,(0,0),shin_mask)
        foot=Image.new('RGBA',source.size);foot.paste(lower,(0,0),foot_mask)
        parts[leg]=(shin,foot,layers[f'{leg}-upper-leg'])
    origin=(SOURCE_FOOT_ANCHOR[0]-rig['anchors']['pelvis'][0],SOURCE_FOOT_ANCHOR[1]-rig['anchors']['sole'][1])
    CACHE.append((source,body,parts,rig,origin))
    LAYER_CACHE.append((layers,masks))


# Complete hand + kunai pixels cut from the authored attack row of the SAME
# source PNG. The original idle head/robe remain separate and unchanged. These
# masks never include a second whole character, torso, head, or planted foot.
ATTACK_RAW=[
 ((110,601),'screen-left-arm',[(16,551),(63,561),(78,575),(93,578),(105,588),(117,597),(119,612),(109,615),(96,608),(86,604),(64,599),(48,588),(33,573)]),
 ((324,601),'screen-left-arm',[(215,552),(265,562),(280,573),(294,581),(320,587),(329,596),(331,612),(321,615),(304,608),(286,603),(273,599),(251,595),(238,582)]),
 ((535,602),'screen-left-arm',[(414,553),(466,563),(481,573),(499,579),(516,587),(535,593),(542,600),(541,613),(531,616),(515,609),(490,605),(474,598),(451,595),(436,579)]),
 ((725,603),'screen-left-arm',[(623,553),(672,565),(684,577),(701,585),(720,590),(730,600),(729,615),(717,616),(697,609),(674,605),(657,597),(645,584)]),
 ((891,603),'screen-left-arm',[(831,539),(863,558),(877,578),(884,591),(899,597),(900,613),(891,620),(874,612),(858,602),(851,589),(839,579)]),
 ((1147,603),'screen-right-arm',[(1142,596),(1150,589),(1165,587),(1181,579),(1199,576),(1213,570),(1259,555),(1263,563),(1231,587),(1213,594),(1192,601),(1171,606),(1155,615),(1143,614)]),
 ((1328,604),'screen-left-arm',[(1322,598),(1329,591),(1347,589),(1363,584),(1382,575),(1400,571),(1459,553),(1465,562),(1430,589),(1405,599),(1385,605),(1360,610),(1334,615),(1322,609)]),
 ((1566,603),'screen-right-arm',[(1559,598),(1565,591),(1578,588),(1590,581),(1610,576),(1626,568),(1658,555),(1662,562),(1639,582),(1620,594),(1594,604),(1576,613),(1562,617),(1557,608)]),
]
ATTACK_BODY_CENTERS=(150,350,554,748,931,1124,1328,1523)
ATTACK_NECK_Y=(589,589,589,590,589,590,589,593)
ATTACK_BELT_Y=(632,632,632,634,641,640,631,634)
ATTACK_CACHE=[]
ATTACK_UNDERPAINT_COUNTS=[]
_full_source=Image.open(SOURCE).convert('RGBA')
for _direction,(_pivot,_arm_name,_polygon) in enumerate(ATTACK_RAW):
    _x=[p[0] for p in _polygon];_y=[p[1] for p in _polygon]
    _bounds=(min(_x)-2,min(_y)-2,max(_x)+3,max(_y)+3)
    _crop=_full_source.crop(_bounds);_mask=Image.new('L',_crop.size)
    ImageDraw.Draw(_mask).polygon([(x-_bounds[0],y-_bounds[1]) for x,y in _polygon],fill=255)
    _weapon_arm=Image.new('RGBA',_crop.size);_weapon_arm.paste(_crop,(0,0),_mask)
    _layers,_masks=LAYER_CACHE[_direction];_source,_body,_parts,_rig,_origin=CACHE[_direction]
    _remove=ImageChops.lighter(_masks[_arm_name],_masks['head-hair'])
    _attack_body=_body.copy();_attack_body.paste((0,0,0,0),(0,0,*_body.size),_remove)
    # Newly revealed cloth comes from the same original authored attack pose.
    # Its head, arm and weapon are excluded. Only pixels formerly owned by the
    # removed hanging idle arm may receive this sampled underpaint; every other
    # visible idle torso/robe pixel remains the original byte-for-byte layer.
    _cloth_source=_full_source.copy()
    _exclude=Image.new('L',_full_source.size)
    ImageDraw.Draw(_exclude).polygon(_polygon,fill=255)
    _cloth_source.paste((0,0,0,0),(0,0,*_cloth_source.size),_exclude)
    _idle_neck=_rig['anchors']['neck'][1]+_rig['bounds'][1]
    _yratio=(ATTACK_BELT_Y[_direction]-ATTACK_NECK_Y[_direction])/(165-_idle_neck)
    _dx=ATTACK_BODY_CENTERS[_direction]-(_rig['anchors']['pelvis'][0]+_rig['bounds'][0])
    _underpaint=_cloth_source.transform(_source.size,Image.Transform.AFFINE,
        (1,0,_rig['bounds'][0]+_dx,0,_yratio,ATTACK_NECK_Y[_direction]+(_rig['bounds'][1]-_idle_neck)*_yratio),Image.Resampling.NEAREST)
    _underpaint.putalpha(ImageChops.multiply(_underpaint.getchannel('A'),_masks[_arm_name]))
    _attack_body.alpha_composite(_underpaint)
    ATTACK_UNDERPAINT_COUNTS.append(sum(a>8 for a in _underpaint.getchannel('A').tobytes()))
    _rest_legs=Image.new('RGBA',_source.size)
    for _name in ORDER:
        if _name.endswith('-leg'):_rest_legs.paste(_layers[_name],(0,0),_masks[_name])
    _target=_rig['anchors'][_arm_name.replace('-arm','-shoulder')]
    _a=(_pivot[0]-_bounds[0],_pivot[1]-_bounds[1])
    _sign=-1 if _direction<5 else 1
    _b=(_a[0]+90*_sign,_a[1]-35)
    ATTACK_CACHE.append((_weapon_arm,_a,_b,_target,_attack_body,_layers['head-hair'],_rest_legs,_sign))


def movement(direction, mode):
    x,y=DIRECTION_VECTORS[direction]
    if mode=='forward':return (x,y)
    if mode=='backward':return (-x,-y)
    if mode=='strafe-left':return (-y,x)
    raise ValueError(mode)


def foot_offset(phase, move, leg_index):
    p=(phase+leg_index*.5)%1
    if p<.5:
        along=.5-2*p;lift=0.;planted=True
    else:
        along=-.5+2*(p-.5);lift=math.sin((p-.5)*math.tau)*SWING_LIFT;planted=False
    # Half the cycle is a stance with the opposite slope to world travel.
    return (move[0]*along*math.hypot(move[0]*GROUND_TRAVEL[0],move[1]*GROUND_TRAVEL[1])/2,
            move[1]*along*math.hypot(move[0]*GROUND_TRAVEL[0],move[1]*GROUND_TRAVEL[1])/2-FOOT_BASE_LIFT-lift),planted


def transform_bone(im,a,b,ta,tb,origin):
    sx,sy=b[0]-a[0],b[1]-a[1];tx,ty=tb[0]-ta[0],tb[1]-ta[1]
    sn=math.hypot(sx,sy);tn=math.hypot(tx,ty)
    if sn<1e-9 or tn<1e-9:raise ValueError('zero-length bone')
    su=(sx/sn,sy/sn);tu=(tx/tn,ty/tn);sv=(-su[1],su[0]);tv=(-tu[1],tu[0]);ratio=sn/tn
    A=su[0]*ratio*tu[0]+sv[0]*tv[0];B=su[0]*ratio*tu[1]+sv[0]*tv[1]
    D=su[1]*ratio*tu[0]+sv[1]*tv[0];E=su[1]*ratio*tu[1]+sv[1]*tv[1]
    x,y=ta[0]+origin[0],ta[1]+origin[1]
    return im.transform(SIZE,Image.Transform.AFFINE,(A,B,a[0]-A*x-B*y,D,E,a[1]-D*x-E*y),Image.Resampling.NEAREST)


def render_pose(direction,phase,move,walking=True,attacking=False,attack_frame=0):
    source,body,parts,rig,origin=CACHE[direction]
    result=Image.new('RGBA',SIZE)
    if not walking and not attacking:
        result.paste(source,origin)
        return result,{}
    poses={}
    if not walking:
        result.alpha_composite(ATTACK_CACHE[direction][6],origin)
    for li,leg in enumerate(('left','right')) if walking else []:
        shin,foot,upper=parts[leg]
        hip=rig['anchors'][f'{leg}-hip'];knee=rig['anchors'][f'{leg}-knee'];ankle=rig['anchors'][f'{leg}-ankle'];sole=rig['anchors'][f'{leg}-sole']
        offset,planted=foot_offset(phase,move,li);dx,dy=offset
        target_ankle=(ankle[0]+dx,ankle[1]+dy)
        # Hip remains fixed to the authored pelvis. Knee flexion is an explicit
        # independent joint; no whole-body stretch or lower-body facing flip.
        target_knee=(knee[0]+dx*.38,knee[1]+dy*.42)
        result.alpha_composite(transform_bone(upper,hip,knee,hip,target_knee,origin))
        result.alpha_composite(transform_bone(shin,knee,ankle,target_knee,target_ankle,origin))
        result.alpha_composite(foot,(round(origin[0]+dx),round(origin[1]+dy)))
        poses[leg]={'hip':list(hip),'knee':list(target_knee),'ankle':list(target_ankle),
                    'sole':[sole[0]+dx,sole[1]+dy],'planted':planted}
    if attacking:
        weapon_arm,a,b,target,attack_body,head,rest_legs,sign=ATTACK_CACHE[direction]
        result.alpha_composite(attack_body,origin)
        angle=math.radians((-4.,0.,8.,2.)[int(attack_frame)%4]*sign)
        vx,vy=b[0]-a[0],b[1]-a[1]
        end=(target[0]+vx*math.cos(angle)-vy*math.sin(angle),target[1]+vx*math.sin(angle)+vy*math.cos(angle))
        result.alpha_composite(transform_bone(weapon_arm,a,b,target,end,origin))
        result.alpha_composite(head,origin)
    else:
        result.alpha_composite(body,origin)
    return result,poses


def render(direction, moveDirection, phase, walking=True, attacking=False, attack_frame=0):
    """Production-facing proof API: fixed 320x320 source canvas, sole (160,300).

    direction is the held source facing 0..7. moveDirection is an independent
    direction 0..7 or a normalized (x,y) source-screen travel vector. A None
    movement uses facing. Packing all returned images by .2 yields 64x64.
    """
    if moveDirection is None:
        move=DIRECTION_VECTORS[direction]
    elif isinstance(moveDirection,int):
        move=DIRECTION_VECTORS[moveDirection%8]
    else:
        x,y=moveDirection;length=math.hypot(x,y)
        move=(x/length,y/length) if length else (0.,0.)
    return render_pose(direction,float(phase)%1,move,walking,attacking,attack_frame)[0]


def packed(im):
    # One scale is used for every facing and phase, independent of alpha bounds.
    size=(round(SIZE[0]*PACK_SCALE),round(SIZE[1]*PACK_SCALE))
    out=im.resize(size,Image.Resampling.NEAREST)
    return out.crop((0,0,64,64))


def build():
    import numpy as np
    from scipy.ndimage import label
    atlas=Image.new('RGBA',(3*PHASE_COUNT*64,8*64))
    montage=Image.new('RGB',(3*PHASE_COUNT*64,8*87+42),'#626970');draw=ImageDraw.Draw(montage)
    draw.text((8,8),'OBITO | Original source art | 16 phases per travel mode | Arms, head, robe and slit fixed',fill='white')
    for m,mode in enumerate(MODES):draw.text((m*PHASE_COUNT*64+8,25),mode,fill='#ede6ac')
    validation=[];poses=[];max_contact_drift=0.;connectivity=[]
    for direction in range(8):
        for mi,mode in enumerate(MODES):
            move=movement(direction,mode);hashes=set()
            for phase_i in range(PHASE_COUNT):
                phase=phase_i/PHASE_COUNT
                im,pose=render_pose(direction,phase,move)
                tile=packed(im);x=(mi*PHASE_COUNT+phase_i)*64;y=direction*64
                atlas.paste(tile,(x,y))
                montage.paste(tile,(x,direction*87+62),tile)
                hashes.add(hashlib.sha256(im.tobytes()).hexdigest())
                poses.append({'direction':direction,'mode':mode,'phase':phase_i,'joints':pose})
                a=np.asarray(im)[:,:,3]>128;labels,n=label(a);counts=np.bincount(labels.ravel())[1:]
                significant=sum(int(c)>25 for c in counts)
                if significant>1:connectivity.append({'direction':direction,'mode':mode,'phase':phase_i,'components_gt_25_pixels':significant})
            validation.append({'direction':direction,'mode':mode,'unique_native_phase_images':len(hashes)})
            # Sample contiguous stance intervals densely; world + relative sole
            # is constant analytically. Raster positioning error is <= .5 px.
            for leg_index in range(2):
                previous=None
                for sample in range(257):
                    phase=sample/256
                    offset,planted=foot_offset(phase,move,leg_index)
                    world=(move[0]*math.hypot(move[0]*GROUND_TRAVEL[0],move[1]*GROUND_TRAVEL[1])*phase,move[1]*math.hypot(move[0]*GROUND_TRAVEL[0],move[1]*GROUND_TRAVEL[1])*phase)
                    contact=(world[0]+offset[0],world[1]+offset[1])
                    if planted and previous is not None:
                        max_contact_drift=max(max_contact_drift,math.dist(contact,previous))
                    previous=contact if planted else None
        draw.text((3,direction*87+45),f'{direction}: {RIGS[direction]["name"]}',fill='white')
    assert max_contact_drift<1e-10,max_contact_drift
    assert all(v['unique_native_phase_images']==PHASE_COUNT for v in validation),validation
    atlas.save(ROOT/'obito-source-motion-16phase.png')
    montage.save(ROOT/'obito-source-motion-montage.png')
    # Animated review: 8 facings x 3 independent travel modes, stationary world
    # grids and actual actor translation. Every tile preserves its facing.
    cell_w,cell_h=240,205;frames=[]
    for frame in range(PHASE_COUNT*2):
        phase=(frame%PHASE_COUNT)/PHASE_COUNT;world_phase=frame/PHASE_COUNT
        review=Image.new('RGB',(3*cell_w,8*cell_h+30),'#626970');d=ImageDraw.Draw(review)
        d.text((8,8),'Original Obito art | Each facing: forward / backward / strafe-left',fill='white')
        for direction in range(8):
            for mi,mode in enumerate(MODES):
                x=mi*cell_w;y=direction*cell_h+30;move=movement(direction,mode)
                d.rectangle((x,y,x+cell_w-1,y+cell_h-1),outline='#444b51')
                for gx in range(x+10,x+cell_w,20):d.line((gx,y+27,gx,y+cell_h-5),fill='#737b82')
                for gy in range(y+27,y+cell_h,20):d.line((x+10,gy,x+cell_w-5,gy),fill='#737b82')
                d.text((x+7,y+8),f'{direction} {RIGS[direction]["name"]} / {mode}',fill='white')
                im,pose=render_pose(direction,phase,move)
                tile=packed(im).resize((192,192),Image.Resampling.NEAREST)
                world_x=move[0]*math.hypot(move[0]*GROUND_TRAVEL[0],move[1]*GROUND_TRAVEL[1])*world_phase*PACK_SCALE*3
                world_y=move[1]*math.hypot(move[0]*GROUND_TRAVEL[0],move[1]*GROUND_TRAVEL[1])*world_phase*PACK_SCALE*3
                # Center the two-cycle translation in each tile.
                xx=round(x+24+world_x-move[0]*math.hypot(move[0]*GROUND_TRAVEL[0],move[1]*GROUND_TRAVEL[1])*PACK_SCALE*3)
                yy=round(y+15+world_y-move[1]*math.hypot(move[0]*GROUND_TRAVEL[0],move[1]*GROUND_TRAVEL[1])*PACK_SCALE*3)
                review.paste(tile,(xx,yy),tile)
        frames.append(review)
    frames[0].save(ROOT/'obito-source-motion-proof.gif',save_all=True,append_images=frames[1:],duration=50,loop=0,disposal=2)
    # Four independent attack poses over a fixed walking phase and original head.
    attack_montage=Image.new('RGB',(4*256,8*270),'#626970');ad=ImageDraw.Draw(attack_montage)
    attack_unique=[]
    for direction in range(8):
        seen=set()
        for attack_frame in range(4):
            image,_=render_pose(direction,.25,movement(direction,'backward'),True,True,attack_frame)
            seen.add(hashlib.sha256(image.tobytes()).hexdigest())
            tile=packed(image).resize((256,256),Image.Resampling.NEAREST)
            x=attack_frame*256;y=direction*270
            attack_montage.paste(tile,(x,y+14),tile)
            ad.text((x+4,y+2),f'{direction} {RIGS[direction]["name"]}: attack {attack_frame}',fill='white')
        attack_unique.append(len(seen))
    assert attack_unique==[4]*8,attack_unique
    attack_montage.save(ROOT/'obito-source-four-phase-attack-montage.png')
    report={'attack_phase_count':4,'attack_native_unique_per_direction':attack_unique,'attack_arm_source':'obito-source.png authored attack row, original hand plus complete kunai','attack_underpaint_source':'same authored attack-row torso, restricted to removed idle-arm ownership','attack_underpaint_visible_pixels':ATTACK_UNDERPAINT_COUNTS,'phases_per_motion':PHASE_COUNT,'source_canvas':list(SIZE),'source_foot_anchor':list(SOURCE_FOOT_ANCHOR),'packed_canvas':[64,64],'packed_foot_anchor':[32,60],'facing_count':8,'modes':list(MODES),
        'body_rest_reconstruction_changed_rgba_bytes':0,'fixed_body_source_hashes':[hashlib.sha256(c[1].tobytes()).hexdigest() for c in CACHE],
        'analytic_planted_sole_max_drift_source_pixels':max_contact_drift,
        'raster_sole_rounding_bound_source_pixels_per_axis':.5,
        'source_pixels_per_cycle':list(GROUND_TRAVEL),'packed_pixels_per_cycle':[v*PACK_SCALE for v in GROUND_TRAVEL],'scale':PACK_SCALE,
        'native_phase_uniqueness':validation,'disconnected_significant_components':connectivity,
        'limitations':['No new thigh or shoulder artwork was drawn. The conservative gait leaves all ambiguous slit/lining pixels fixed; attack-side cloth comes from the original authored attack pose.',
            'The skeleton drives hips, knees and ankles independently, but no visible upper-thigh pixels can move because their source views are occluded or inseparable from lining.',
            'Larger strides and fully exposed articulated legs still require reviewed hidden underpaint; this is not a production-complete rig.',
            'The GIF loops after two cycles, resetting world translation only at the loop boundary.']}
    (ROOT/'obito-source-motion-validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    (ROOT/'obito-source-motion-poses.json').write_text(json.dumps(poses,indent=2),encoding='utf-8')
    print(f'PASS: 8 facings x 3 travel modes x 16 distinct native phases. Planted sole analytic drift {max_contact_drift:.3g} source px.')
    print(f'CONNECTIVITY: {len(connectivity)} frames have >1 opaque component larger than 25 source pixels; see validation report.')
    print('CONSTRAINT: hidden thigh masks remain empty; visible feet/shins move under the fixed source robe. Attack-side cloth uses the original authored attack pose.')

if __name__=='__main__':build()
