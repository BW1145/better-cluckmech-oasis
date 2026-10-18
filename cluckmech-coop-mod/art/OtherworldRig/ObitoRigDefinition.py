"""Native-source Obito rig metadata and lossless semantic mask exporter.

No source pixels are repainted, resized, mirrored, or rotated here. Coordinates
in RAW are measured on obito-source.png; exported metadata is crop-local. The
upper legs are hidden or inseparable from robe-slit lining in this source.
Their masks are intentionally empty; all ambiguous slit pixels stay fixed. Rig anchors behind the robe are
estimated skeletal joints, not claims that hidden pixel artwork exists.

Run this file for the eight-direction JSON, source/layer PNGs, and review sheets.
The reusable API is RIGS and load_direction(index). No runtime assets are touched.
"""
from pathlib import Path
import hashlib
import json
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent
SOURCE = ROOT.parent / 'Otherworld' / 'obito-source.png'
DIRECTIONS = ('down', 'down-left', 'left', 'up-left', 'up', 'up-right', 'right', 'down-right')
ORDER = ('right-upper-leg', 'right-lower-leg', 'left-upper-leg', 'left-lower-leg',
         'torso-robe', 'screen-left-arm', 'screen-right-arm', 'head-hair')
# All coordinates in this compact hand-traced source table are SOURCE pixels.
# Foot/leg entries are screen-left, screen-right; the anatomical name mapping
# explicitly changes with facing. Side-view correspondence is an authored rig
# convention because these generated reference poses do not identify each leg.
RAW = [
 dict(bounds=(85,10,214,269), pelvis=(150,179), neck=(150,108), sole=(150,262),
      anatomical=('right','left'), hips=((138,181),(162,181)), knees=((128,218),(173,218)), ankles=((120,245),(178,245)), soles=((118,262),(179,262)),
      thighs=[[[ (148,182),(149,189),(145,203),(140,213),(138,213),(142,196)]],
              [[ (154,185),(158,198),(161,212),(158,208),(153,192)]]],
      shins=[[[ (138,214),(141,213),(136,227),(131,237),(126,238),(130,224)],[(100,242),(106,237),(133,240),(139,248),(140,268),(98,268)]],
             [[ (162,213),(166,225),(171,237),(167,241),(162,235),(159,219)],[(161,242),(166,239),(190,237),(192,246),(199,250),(199,268),(159,268)]]],
      shoulders=((113,124),(182,125)), hands=((105,184),(192,185)),
      arms=[[(109,119),(119,123),(118,135),(112,155),(111,170),(116,177),(114,193),(103,199),(92,193),(93,174),(100,149),(104,132)],
            [(180,117),(189,125),(195,149),(203,172),(205,190),(197,199),(183,197),(178,187),(182,174),(178,151)]],
      head_y=104),
 dict(bounds=(283,10,420,269), pelvis=(351,178), neck=(355,107), sole=(353,261),
      anatomical=('right','left'), hips=((338,180),(365,181)), knees=((332,216),(377,220)), ankles=((330,244),(379,247)), soles=((329,261),(378,261)),
      thighs=[[[ (340,183),(344,188),(345,204),(343,215),(333,217),(334,202)]],
              [[ (351,188),(358,203),(362,216),(355,212),(352,201)]]],
      shins=[[[ (333,214),(345,213),(345,232),(337,237),(326,237),(328,225)],[(307,247),(325,242),(327,234),(347,236),(348,249),(351,268),(304,268)]],
             [[ (360,217),(365,228),(370,239),(362,242),(357,226)],[(358,249),(365,242),(365,244),(388,244),(390,250),(400,254),(401,268),(355,268)]]],
      shoulders=((323,127),(383,126)), hands=((316,181),(395,183)),
      arms=[[(318,125),(325,125),(325,151),(323,173),(324,182),(320,193),(310,194),(306,186),(310,165),(314,145)],
            [(381,119),(390,124),(399,144),(404,165),(405,187),(397,197),(383,198),(378,187),(381,171),(377,150),(375,132)]],
      head_y=104),
 dict(bounds=(488,10,616,269), pelvis=(554,177), neck=(550,106), sole=(545,262),
      anatomical=('right','left'), hips=((541,181),(565,184)), knees=((533,215),(574,221)), ankles=((518,243),(572,245)), soles=((514,261),(572,262)),
      thighs=[[[ (534,191),(551,195),(551,211),(541,216),(530,214),(530,202)]],
              []],
      shins=[[[ (530,213),(550,214),(552,230),(542,233),(526,234),(524,228)],[(494,249),(508,242),(511,231),(535,234),(532,247),(537,257),(537,268),(492,268)]],
             [[ (554,217),(560,224),(567,235),(560,238),(553,229)],[(551,246),(556,238),(580,236),(585,247),(594,252),(595,268),(549,268)]]],
      shoulders=((549,126),(580,127)), hands=((538,184),(591,183)),
      arms=[[(543,118),(553,121),(560,130),(557,149),(550,173),(553,182),(548,194),(535,201),(522,195),(524,177),(532,159),(536,140),(538,124)],
            [(576,116),(584,122),(592,140),(601,165),(604,177),(600,189),(591,194),(582,186),(586,173),(581,154)]],
      head_y=105),
 dict(bounds=(677,10,815,269), pelvis=(748,179), neck=(744,106), sole=(736,261),
      anatomical=('left','right'), hips=((729,182),(758,184)), knees=((719,215),(764,221)), ankles=((709,244),(766,245)), soles=((707,261),(765,261)),
      thighs=[[[ (716,193),(720,197),(721,213),(716,216),(711,211)]],
              []],
      shins=[[[ (711,212),(717,215),(714,227),(725,230),(723,236),(704,233),(704,225)],[(686,249),(700,242),(703,231),(727,237),(722,248),(726,259),(726,268),(684,268)]],
             [[(739,225),(746,233),(747,238),(739,236)],[(740,247),(746,241),(748,239),(772,235),(780,244),(789,250),(790,268),(737,268)]]],
      shoulders=((724,128),(778,125)), hands=((706,185),(787,182)),
      arms=[[(718,119),(729,123),(736,131),(730,151),(718,173),(721,183),(715,195),(702,200),(691,193),(694,177),(706,156),(710,137)],
            [(775,116),(783,123),(792,145),(800,164),(802,179),(799,191),(786,194),(777,187),(782,174),(777,155)]],
      head_y=104),
 dict(bounds=(862,10,994,269), pelvis=(931,179), neck=(932,106), sole=(929,262),
      anatomical=('left','right'), hips=((916,182),(943,182)), knees=((907,219),(951,219)), ankles=((903,244),(948,245)), soles=((903,262),(950,262)),
      thighs=[[],[]],
      shins=[[[ (888,249),(893,240),(893,235),(918,239),(918,249),(919,268),(886,268)]],
             [[ (923,246),(929,239),(956,239),(957,247),(975,252),(977,268),(921,268)]]],
      shoulders=((892,125),(965,128)), hands=((884,184),(974,185)),
      arms=[[(890,116),(899,122),(895,146),(890,171),(894,180),(890,191),(878,196),(869,188),(869,176),(876,151),(882,130)],
            [(962,120),(970,125),(978,146),(984,169),(987,189),(977,198),(964,195),(959,183),(962,172),(957,149),(956,132)]],
      head_y=103),
 dict(bounds=(1047,10,1191,269), pelvis=(1124,178), neck=(1124,106), sole=(1127,262),
      anatomical=('left','right'), hips=((1108,183),(1140,181)), knees=((1095,221),(1153,216)), ankles=((1096,245),(1156,244)), soles=((1097,262),(1158,262)),
      thighs=[[],[[ (1142,194),(1147,199),(1150,213),(1145,215),(1142,207)]]],
      shins=[[[ (1077,249),(1083,240),(1085,236),(1105,239),(1111,249),(1119,254),(1122,268),(1074,268)]],
             [[(1147,213),(1152,215),(1158,228),(1151,233),(1145,231)],[(1138,246),(1143,236),(1164,233),(1167,242),(1182,251),(1184,268),(1137,268)]]],
      shoulders=((1089,125),(1149,127)), hands=((1079,182),(1160,185)),
      arms=[[(1086,116),(1094,122),(1090,147),(1084,170),(1088,183),(1083,194),(1070,193),(1060,184),(1062,169),(1072,145)],
            [(1145,118),(1155,122),(1161,140),(1169,161),(1178,174),(1178,189),(1168,199),(1153,197),(1146,185),(1148,174),(1139,151),(1138,132)]],
      head_y=104),
 dict(bounds=(1253,10,1382,269), pelvis=(1328,179), neck=(1321,106), sole=(1322,262),
      anatomical=('left','right'), hips=((1312,182),(1338,181)), knees=((1300,220),(1341,215)), ankles=((1302,247),(1341,244)), soles=((1300,262),(1343,262)),
      thighs=[[],[[ (1326,191),(1335,185),(1343,187),(1342,212),(1327,216),(1320,213)]]],
      shins=[[[ (1280,250),(1288,245),(1291,242),(1310,244),(1316,252),(1321,268),(1279,268)]],
             [[ (1325,214),(1344,213),(1344,233),(1325,233),(1320,227)],[(1324,247),(1327,234),(1348,234),(1350,244),(1366,252),(1367,268),(1322,268)]]],
      shoulders=((1298,127),(1344,129)), hands=((1290,185),(1356,180)),
      arms=[[(1293,118),(1304,122),(1311,132),(1307,152),(1300,172),(1303,182),(1299,196),(1285,200),(1273,193),(1274,176),(1280,154),(1285,134)],
            [(1341,120),(1349,130),(1353,150),(1361,174),(1360,188),(1351,192),(1346,183),(1346,163)]],
      head_y=105),
 dict(bounds=(1447,10,1585,269), pelvis=(1523,178), neck=(1527,106), sole=(1524,262),
      anatomical=('right','left'), hips=((1508,181),(1536,181)), knees=((1501,219),(1548,219)), ankles=((1496,246),(1550,246)), soles=((1497,262),(1552,262)),
      thighs=[[[ (1516,186),(1521,181),(1522,195),(1517,211),(1510,216),(1511,203)]],
              [[ (1531,184),(1535,196),(1539,212),(1535,210),(1530,194)]]],
      shins=[[[ (1510,214),(1517,213),(1514,226),(1508,237),(1501,239),(1505,226)],[(1476,249),(1484,244),(1486,238),(1510,243),(1516,252),(1520,268),(1474,268)]],
             [[ (1539,214),(1544,226),(1549,237),(1541,240),(1538,227)],[(1535,247),(1540,241),(1561,240),(1566,248),(1575,254),(1576,268),(1533,268)]]],
      shoulders=((1486,127),(1557,127)), hands=((1482,186),(1568,184)),
      arms=[[(1480,119),(1490,124),(1495,136),(1490,156),(1488,172),(1492,184),(1488,198),(1476,201),(1465,193),(1468,172),(1473,149),(1474,132)],
            [(1554,118),(1562,126),(1567,147),(1572,170),(1575,185),(1570,195),(1558,195),(1551,185),(1555,172),(1552,150)]],
      head_y=104),
]


# Conservative animation ownership: the open-robe slit contains inseparable
# trouser/lining/trim edges. Keep that entire region fixed with the robe rather
# than move white trim or fabricate unseen cloth. Both skeletal thigh bones
# remain defined; their rendered masks are empty because no independently
# usable complete thigh segment exists in these source views.
# Full inner edges of the exposed idle arms. Keep the complete hanging limb in
# one ownership mask so its pale inner contour cannot remain beside an attack.
RAW[0]['arms'][0] = [(109,119),(122,123),(122,137),(119,159),(119,174),(116,180),(115,193),(103,199),(92,193),(93,174),(100,149),(104,132)]
RAW[2]['arms'][0] = [(543,118),(554,121),(561,130),(560,151),(557,170),(554,182),(548,197),(535,202),(522,195),(524,177),(532,159),(536,140),(538,124)]
RAW[3]['arms'][0] = [(718,119),(730,123),(737,131),(733,151),(725,174),(722,184),(715,197),(702,201),(691,193),(694,177),(706,156),(710,137)]
RAW[6]['arms'][0] = [(1293,118),(1305,122),(1312,132),(1311,153),(1309,173),(1305,184),(1299,198),(1285,201),(1273,193),(1274,176),(1280,154),(1285,134)]

# These two hem-adjacent seams are traced around the sock, excluding the robe's
# long black lower outline rather than letting that outline travel with a foot.
RAW[3]['shins'][1][-1] = [(739,252),(752,246),(755,239),(768,236),(775,241),(778,249),(790,254),(790,268),(737,268)]
RAW[7]['shins'][0][-1] = [(1476,251),(1484,247),(1486,242),(1499,243),(1510,246),(1516,252),(1520,268),(1474,268)]
RAW[0]['shins'][1][-1] = [(161,249),(167,245),(169,240),(185,239),(190,247),(199,252),(199,268),(159,268)]
for _raw in RAW:
    _raw['thighs'] = [[], []]
    _raw['shins'] = [[side[-1]] for side in _raw['shins']]


def _local(point, bounds):
    return [point[0]-bounds[0], point[1]-bounds[1]]


def _definition(index):
    raw = RAW[index]; b = raw['bounds']; width, height = b[2]-b[0], b[3]-b[1]
    anchors = {name:_local(raw[name],b) for name in ('pelvis','neck','sole')}
    polygons = {name:[] for name in ORDER}
    legs = {}
    for side, anatomical in enumerate(raw['anatomical']):
        for bone, key in (('hip','hips'),('knee','knees'),('ankle','ankles'),('sole','soles')):
            anchors[f'{anatomical}-{bone}'] = _local(raw[key][side],b)
        for part,key in (('upper','thighs'),('lower','shins')):
            name=f'{anatomical}-{part}-leg'
            polygons[name]=[[_local(p,b) for p in poly] for poly in raw[key][side]]
        legs[anatomical]={'source_screen_side':'left' if side==0 else 'right',
            'upper_layer':f'{anatomical}-upper-leg','lower_layer':f'{anatomical}-lower-leg',
            'hip':anchors[f'{anatomical}-hip'],'knee':anchors[f'{anatomical}-knee'],
            'ankle':anchors[f'{anatomical}-ankle'],'sole':anchors[f'{anatomical}-sole'],
            'occluded_joint_art_available':False}
    for i,side in enumerate(('left','right')):
        name=f'screen-{side}-arm'
        polygons[name]=[[_local(p,b) for p in raw['arms'][i]]]
        anchors[f'screen-{side}-shoulder']=_local(raw['shoulders'][i],b)
        anchors[f'screen-{side}-hand']=_local(raw['hands'][i],b)
    split=round((anchors['left-ankle'][0]+anchors['right-ankle'][0])/2)
    foot_y=247-b[1]
    for side,name in enumerate(raw['anatomical']):
        left,right=(0,split-1) if side==0 else (split,width-1)
        polygons[f'{name}-lower-leg'].append([[left,foot_y],[right,foot_y],[right,height-1],[left,height-1]])
    polygons['head-hair']=[[[0,0],[width-1,0],[width-1,raw['head_y']-b[1]],[0,raw['head_y']-b[1]]]]
    return {'direction':index,'name':DIRECTIONS[index],'bounds':list(b),'canvas':[width,height],
        'source_pixels_per_rig_unit':1,'order':list(ORDER),'anchors':anchors,'legs':legs,
        'polygons':polygons,'default_pixel_owner':'torso-robe',
        'label_precedence':'order except torso-robe; later polygons replace earlier labels',
        'fixed_layers':['torso-robe','head-hair'],
        'status':'lossless source-pixel partition; hidden joints require separate approved underpaint'}

RIGS = [_definition(i) for i in range(8)]


def load_direction(index):
    """Return original crop, layer dict, binary ownership masks, and metadata.

    Every layer shares the exact crop origin. Masks partition all source bytes,
    including RGB under transparent alpha. Restore with mask-based paste, not
    repeated straight-alpha blending, when doing the byte-exact static check.
    """
    rig = RIGS[index]
    source = Image.open(SOURCE).convert('RGBA').crop(rig['bounds'])
    labels = Image.new('L',source.size,ORDER.index('torso-robe'))
    draw = ImageDraw.Draw(labels)
    for name in ORDER:
        if name=='torso-robe':continue
        for polygon in rig['polygons'][name]:
            draw.polygon([tuple(p) for p in polygon], fill=ORDER.index(name))
    # Below global y=247 there are only sandals/ankles in the inspected first
    # row. Give every such pixel to one leg, including anti-aliased outer edges,
    # so no static shoe outline remains in the robe/body layer during motion.
    split=round((rig['anchors']['left-ankle'][0]+rig['anchors']['right-ankle'][0])/2)
    screen_left=RAW[index]['anatomical'][0];screen_right=RAW[index]['anatomical'][1]
    y=247-rig['bounds'][1]
    draw.rectangle((0,y,split-1,source.height-1),fill=ORDER.index(f'{screen_left}-lower-leg'))
    draw.rectangle((split,y,source.width-1,source.height-1),fill=ORDER.index(f'{screen_right}-lower-leg'))
    layers={};masks={}
    for i,name in enumerate(ORDER):
        mask=labels.point(lambda value, target=i:255 if value==target else 0)
        layer=Image.new('RGBA',source.size);layer.paste(source,(0,0),mask)
        masks[name]=mask;layers[name]=layer
    return source,layers,masks,rig


def build():
    out=ROOT/'obito-source-rig';out.mkdir(exist_ok=True)
    reports=[];source_cells=[];reconstructed_cells=[];label_cells=[]
    palette=['#598df1','#2ad0ef','#e065a5','#f1ad59','#7c7270','#bc77e5','#72d17a','#e5e2ba']
    for i in range(8):
        source,layers,masks,rig=load_direction(i)
        reconstructed=Image.new('RGBA',source.size)
        visible_counts={}
        colored=Image.new('RGBA',source.size)
        for name in ORDER:
            reconstructed.paste(layers[name],(0,0),masks[name])
            layers[name].save(out/f'{i}-{DIRECTIONS[i]}-{name}.png')
            masks[name].save(out/f'{i}-{DIRECTIONS[i]}-{name}-mask.png')
            visible_counts[name]=sum(1 for a,m in zip(source.getchannel('A').tobytes(),masks[name].tobytes()) if a>8 and m)
            c=Image.new('RGBA',source.size,palette[ORDER.index(name)]);c.putalpha(layers[name].getchannel('A'))
            colored.alpha_composite(c)
        assert reconstructed.tobytes()==source.tobytes(),f'direction {i}: changed source bytes'
        # Ownership must be mutually exclusive and complete, independently of RGBA.
        mask_bytes=[masks[name].tobytes() for name in ORDER]
        assert all(sum(values)==255 for values in zip(*mask_bytes)),f'direction {i}: non-partition masks'
        source.save(out/f'{i}-{DIRECTIONS[i]}-reference.png')
        reconstructed.save(out/f'{i}-{DIRECTIONS[i]}-reconstructed.png')
        reports.append({'direction':i,'name':DIRECTIONS[i],'changed_rgba_bytes':0,'changed_pixels':0,
            'exclusive_complete_ownership':True,'native_crop_size':list(source.size),
            'source_crop_sha256':hashlib.sha256(source.tobytes()).hexdigest(),
            'visible_pixels_alpha_gt_8_by_layer':visible_counts})
        source_cells.append(source);reconstructed_cells.append(reconstructed);label_cells.append(colored)
    source_sha=hashlib.sha256(SOURCE.read_bytes()).hexdigest()
    metadata={'schema':'otherworld-source-pixel-rig-v1','character':'obito',
        'source':'../Otherworld/obito-source.png','source_sha256':source_sha,
        'coordinate_system':'native source pixels; x right, y down; all layer images retain the crop canvas',
        'rest_reconstruction':'paste each original RGBA layer through its binary ownership mask',
        'animation_notes':[
            'Never rotate the whole lower body 180 degrees. Drive each ankle/sole independently from world travel while facing remains the selected source direction.',
            'Draw transformed leg segments behind the unchanged torso-robe and head-hair. Upper- and lower-leg axes use their own hip/knee/ankle anchors.',
            'The whole open-robe slit, including ambiguous dark cloth, stays fixed so no robe trim moves with the legs.',
            'Skeletal anchors behind the robe are estimates. Empty thigh masks mean occlusion, not missing validation. No hidden limb/shoulder pixels were invented.',
            'Pose artwork has slight anatomical ambiguity across views. Screen-side arm names intentionally avoid claiming consistent sleeve anatomy.',
            'Do not rescale directions independently: all eight are native 1:1 source pixels with a common y crop. Align using sole/pelvis anchors, not alpha bounds.',
            'Low-alpha gutter noise inside the crop is retained for exact reconstruction. The inspected crop includes all substantive silhouette pixels but excludes other rows.',
            'A lossless rest reconstruction proves pixel ownership only; it does not prove articulated joints are filled or animation-ready.'
        ],'directions':RIGS}
    (ROOT/'obito-eight-direction-rig.json').write_text(json.dumps(metadata,indent=2),encoding='utf-8')
    (ROOT/'obito-source-rig-validation.json').write_text(json.dumps({'source_sha256':source_sha,'results':reports},indent=2),encoding='utf-8')
    # Native sheet includes exact source and exact reconstruction beside one another.
    cw,ch=310,315
    native=Image.new('RGB',(cw*4,ch*2),'#626970');d=ImageDraw.Draw(native)
    for i,(s,r) in enumerate(zip(source_cells,reconstructed_cells)):
        x=(i%4)*cw;y=(i//4)*ch
        d.text((x+7,y+7),f'{i} {DIRECTIONS[i]} | original / reconstructed',fill='white')
        native.paste(s,(x+8,y+29),s);native.paste(r,(x+160,y+29),r)
        d.text((x+7,y+294),'1 source px = 1 sheet px; 0 changed RGBA bytes',fill='white')
    native.save(ROOT/'obito-source-native-contact-sheet.png')
    # Enlarged sheet nearest-neighbor; semantic overlay and anchors are separate
    # from the untouched reference so review graphics cannot alter source layers.
    zoom=3;cw=900;ch=850
    enlarged=Image.new('RGB',(cw*4,ch*2),'#626970');d=ImageDraw.Draw(enlarged)
    for i,(s,c) in enumerate(zip(source_cells,label_cells)):
        x=(i%4)*cw;y=(i//4)*ch
        d.text((x+10,y+8),f'{i} {DIRECTIONS[i]} | untouched original / semantic ownership',fill='white')
        for dx,im in ((10,s),(455,c)):
            im=im.resize((im.width*zoom,im.height*zoom),Image.Resampling.NEAREST)
            enlarged.paste(im,(x+dx,y+32),im)
        for name,p in RIGS[i]['anchors'].items():
            if name.startswith('screen'):continue
            xx=x+455+p[0]*zoom;yy=y+32+p[1]*zoom
            d.ellipse((xx-3,yy-3,xx+3,yy+3),fill='white',outline='black')
        d.text((x+10,y+822),'3x nearest neighbor. Robe/head kept fixed; white dots = rig anchors.',fill='white')
    enlarged.save(ROOT/'obito-source-enlarged-contact-sheet.png')
    print('PASS: 8 native-source directions, 8 exclusive semantic layers each, 0 changed RGBA bytes in every reconstructed crop.')
    print('NOTE: hidden thigh/shoulder geometry is not painted; estimated anchors are provided behind the unchanged robe.')
    print(ROOT/'obito-eight-direction-rig.json')

if __name__=='__main__':
    build()
