"""Lossless semantic-mask proof for the existing Perlica art, not a redraw.

Masks only partition original pixels. No resampling occurs until BOTH original
and reconstructed images are displayed at the same review scale. Hidden limb,
shoulder and rear-hair surfaces are NOT invented here. Those need separate
painting/review before this can become an articulated production animation.
"""
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageChops
ROOT=Path(__file__).resolve().parent
SOURCE=ROOT.parent/'Otherworld/perlica-source.png'
# Manually inspected character bounds, not a uniform grid that cuts off gloves
# and includes the neighboring character's weapon. Shared coordinates for all layers.
BOUNDS=(35,25,227,264)
POLYGONS={
 'rear-hair': [[(55,65),(81,63),(85,96),(78,122),(63,139),(51,128)],[(144,60),(172,56),(177,98),(188,123),(177,138),(151,120)]],
 'left-leg': [[(90,159),(119,159),(119,239),(86,239)]],
 'right-leg': [[(119,159),(146,159),(153,239),(119,239)]],
 'left-arm-weapon': [[(96,113),(105,119),(101,135),(95,145),(87,158),(84,164),(76,174),(19,231),(3,233),(4,221),(51,162),(61,145),(73,131),(82,122),(92,123)],[(0,170),(79,170),(79,239),(0,239)]],
 'right-arm': [[(146,113),(157,117),(169,126),(178,142),(185,150),(183,163),(176,170),(169,168),(162,160),(155,149),(144,132),(140,120)]],
 'head-front-fringe': [[(0,0),(192,0),(192,94),(144,94),(142,111),(132,102),(126,99),(113,101),(105,110),(94,116),(84,103),(0,103)]]
}
ORDER=['rear-hair','left-leg','right-leg','torso','left-arm-weapon','right-arm','head-front-fringe']
ANCHORS={'pelvis':[119,164],'neck':[120,98],'left-shoulder':[86,114],'right-shoulder':[148,114],'left-knee':[106,187],'right-knee':[131,187],'sole':[119,220]}

def build():
 source=Image.open(SOURCE).convert('RGBA').crop(BOUNDS)
 # Label every pixel once. Semantic priorities are explicit; ownership has no
 # relationship to current visible bounds, feet, frame crop or sorting changes.
 labels=Image.new('L',source.size,ORDER.index('torso'));draw=ImageDraw.Draw(labels)
 for name,polygons in POLYGONS.items():
  for polygon in polygons:draw.polygon(polygon,fill=ORDER.index(name))
 result=Image.new('RGBA',source.size)
 for index,name in enumerate(ORDER):
  mask=labels.point(lambda value:255 if value==index else 0)
  layer=Image.new('RGBA',source.size);layer.paste(source,(0,0),mask)
  layer.save(ROOT/f'perlica-front-{name}.png')
  result.paste(layer,(0,0),mask)
 assert ImageChops.difference(source,result).getbbox() is None
 # Explicit channel check also tests invisible RGB, which getbbox may ignore.
 assert source.tobytes()==result.tobytes(), 'Reconstruction changed source pixels'
 source.save(ROOT/'perlica-front-reference.png');result.save(ROOT/'perlica-front-reconstructed.png')
 (ROOT/'perlica-front-rig.json').write_text(json.dumps({'source':'../Otherworld/perlica-source.png','bounds':BOUNDS,'canvas':source.size,'order':ORDER,'anchors':ANCHORS,'polygons':POLYGONS,'status':'static lossless mask proof; occluded joints not yet painted'},indent=2),encoding='utf-8')
 review=Image.new('RGB',(900,660),'#777b80');d=ImageDraw.Draw(review)
 for x,label,im in ((20,'ORIGINAL SOURCE',source),(460,'LOSSLESS LAYER RECONSTRUCTION',result)):
  d.text((x,15),label,fill='white')
  enlarged=im.resize((384,478),Image.Resampling.NEAREST);review.paste(enlarged,(x,45),enlarged)
  # Fixed target world-art display, identical sampling/pivot in both columns.
  native=im.copy();native.thumbnail((56,56),Image.Resampling.NEAREST)
  review.paste(native,(x+164,560),native)
  d.text((x+112,630),'64px display cell',fill='white')
 review.save(ROOT/'perlica-faithful-static-comparison.png')
 print('PASS: 192x239 RGBA source reconstructed byte-for-byte from 7 semantic layers; 0 changed pixels; shared origin and anchors.')
if __name__=='__main__':build()
