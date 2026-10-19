# Faithful source-art animation

The source PNGs in `../Otherworld` remain the visual reference. This rig owns
individual source-pixel limbs, retaining the original face, hair, outfit and
silhouette. It does not rescale the whole character per animation frame.

## Production contract

- 16 gait phases, eight held aim directions, eight relative travel sectors.
- Four arm/recoil phases independently selected from gait phase.
- Same-source standing poses; Obito has eight dash stages per direction.
- Fixed 64×64 tile, sole anchor `(32,60)`, Unity pivot `(.5,.0625)`, 32 PPU.
- Full-frame compositions are deduplicated into one texture per character.
  Phase and warp shaders therefore always see the entire visible character.
- Runtime public hero IDs are 12 (Perlica), 13 (Obito); file bank IDs are 0, 1.

`BuildProductionAtlas.py` writes `src/OtherworldAssets/faithful-{0,1}.{png,json}`.
`ValidateProductionAtlas.py` verifies 5,168 Perlica and 5,232 Obito addresses, bounds,
transparent gutters, visible-bound metadata, and actual packed phase diversity.
`ReviewProductionMotion.py` plays the packed atlas against a moving world grid.

## Motion and limits

Travel and aim directions are independent. Local hip/knee/ankle joints drive the
visible legs. The longer Obito robe continues to occlude his thighs; no exposed
replacement thighs are invented. His source socks and sandals move behind the
robe, with a raised stance that keeps limb joins covered.

The geometric stride follows the actual movement vector even diagonally.
Current packed strides are Perlica 20 horizontal / 13.2 vertical pixels, Obito
16.8 / 9.6. Intermediate directions use the ellipse-derived scalar stride,
not independent x/y scaling that would change contact direction.

At the game's native character height and speed, exact ground locking would
require implausibly fast animation. Runtime caps the readable running cadence.
Consequently the normal-speed motion is stylized running/backpedaling rather
than a claim of physically exact planted-foot contact. Slow calibrated playback
can verify the geometric stance separately.

Obito's dash animates his source dash-row art through eight stages. Torso and
robe lean up to 25 degrees in side views; lower limbs counter-swing from their
hip anchors, with a bounded crouch and rise. All tiles retain the same canvas
and sole pivot. Runtime uses a 0.35-second eased movement, holds the recovery
pose during the 0.2-second interval, and caches two short-lived trail renderers.
`ReviewProductionMotion.py --dash` renders the shipped pose bank and timing as
an offline three-segment GIF and contact sheet.

## Source and review files

`BuildReferenceLayers.py` is the byte-exact Perlica front ownership proof.
`BuildPerlicaEightMotion.py` supplies Perlica's held-facing leg articulation.
`ObitoRigDefinition.py` and `BuildObitoMotionProof.py` supply Obito ownership,
source-only arm replacement and locomotion.

The generated Perlica rear arm patch is used only behind the original rear
hair. Hidden torso sockets reuse existing source cloth. Generated faces and
entire generated bodies are never substituted for the original character. Review PNGs/GIFs are offline artifacts,
not evidence that the game was launched or live gameplay tested.

Offline build dependencies: Python 3, Pillow, NumPy and SciPy.

## Reproduce the shipped atlases

From the repository root:

```sh
python art/OtherworldRig/BuildProductionAtlas.py 0 1
python art/OtherworldRig/ValidateProductionAtlas.py 0 1
```

Required original inputs, already in `art/Otherworld`:
`perlica-source.png`, `perlica-moving-attack-vertical.png`, `obito-source.png`.
The only added raster source is `perlica-rear-raised-arm-source.png`; only its
occluded rear arm/blade fragment is used, behind the exact original rear hair.
All rig coordinates and semantic masks are contained in the Python sources.
Intermediate PNG layers and generated JSON rig exports are not build inputs.
Review movies/contact sheets are deliberately kept outside the source tree.
