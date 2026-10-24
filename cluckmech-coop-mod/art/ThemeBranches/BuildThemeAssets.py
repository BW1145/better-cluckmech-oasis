"""Pack generated theme art with fixed pivots into the production embedded atlases."""
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from SpriteIslands import grid_sprites, components

ROOT = Path(__file__).resolve().parents[2]
SOURCE = Path(__file__).resolve().parent / 'sources'
DEST = ROOT / 'src' / 'ThemeAssets'
frames = {}
mounts = {}


def opaque_bounds(tile):
    alpha = np.asarray(tile.getchannel('A'))
    ys, xs = np.nonzero(alpha >= 96)
    if not len(xs):
        raise ValueError('Empty sprite')
    return int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1


def add(key, image, pivot=(.5, 0), height=None, width=None):
    image = image.convert('RGBA')
    if height is not None or width is not None:
        scale = (height / image.height) if height is not None else (width / image.width)
        image = image.resize((max(1, round(image.width * scale)), max(1, round(image.height * scale))), Image.Resampling.NEAREST)
    frames[key] = image, pivot


def environment():
    source = Image.open(SOURCE / 'castle-environment.png').convert('RGBA')
    # Source silhouettes are measured independently: wide tables retain their full overhang.
    boxes = {
        'floor': (28, 25, 304, 284), 'carpet': (335, 25, 611, 284),
        'wall': (644, 44, 977, 281), 'gate': (997, 1, 1253, 287),
        'flowerbed': (15, 311, 352, 573), 'rubble': (368, 320, 698, 575),
        'pillar': (737, 288, 893, 574), 'pillar.broken': (1000, 287, 1253, 583),
        'table': (1, 574, 473, 891), 'chair': (490, 574, 644, 897),
        'chandelier': (646, 574, 992, 905), 'coffin': (997, 580, 1253, 903),
        'well': (1, 872, 361, 1253), 'banner': (409, 897, 645, 1245),
        'lamp': (711, 898, 923, 1253), 'window': (1024, 892, 1253, 1253)
    }
    for name, box in boxes.items():
        tile = source.crop(box)
        if name in ('floor', 'carpet'):
            # An interior texture patch supplies a flat repeating floor surface.
            tile = tile.crop((24, 24, tile.width - 24, tile.height - 24)).resize((64, 64), Image.Resampling.NEAREST)
            add('castle.' + name, tile, (.5, .5))
        else:
            tile = tile.crop(opaque_bounds(tile))
            add('castle.' + name, tile, height=96 if name in ('pillar', 'pillar.broken', 'gate', 'window') else 72)


def strip_grid(file, rows, logical_height=58):
    image = Image.open(SOURCE / file).convert('RGBA')
    extracted = grid_sprites(image, 6, len(rows))
    for row, unit in enumerate(rows):
        tiles = extracted[row * 6:(row + 1) * 6]
        bounds = [opaque_bounds(tile) for tile in tiles]
        reference_height = max(b[3] - b[1] for b in bounds[:5])
        scale = (70 if unit == 'castle.coffin' else logical_height) / reference_height
        normalized = []
        for tile, box in zip(tiles, bounds):
            body = tile.crop(box)
            normalized.append(body.resize((max(1, round(body.width * scale)), max(1, round(body.height * scale))), Image.Resampling.NEAREST))
        canvas_size = (max(t.width for t in normalized) + 8, max(t.height for t in normalized) + 8)
        for index, tile in enumerate(normalized):
            canvas = Image.new('RGBA', canvas_size)
            canvas.alpha_composite(tile, ((canvas.width - tile.width) // 2, canvas.height - 4 - tile.height))
            suffixes = ['idle.0', 'walk.0', 'walk.1', 'attack.0', 'attack.1', 'death.0']
            add(unit + '.' + suffixes[index], canvas, (.5, 4 / canvas.height))
        if unit == 'castle.flower-ghost':
            add(unit + '.unfold.0', frames[unit + '.idle.0'][0], frames[unit + '.idle.0'][1])
            add(unit + '.unfold.1', frames[unit + '.walk.0'][0], frames[unit + '.walk.0'][1])


def count():
    poses = []
    sheets = (
        ('castle-count-basic.png', 96 / 405,
         ['idle.0', 'walk.0', 'walk.1', 'walk.2', 'attack.0', 'attack.1'],
         [(255, 469), (779, 466), (1271, 472), (278, 935), (766, 939), (1247, 933)]),
        ('castle-count-special.png', 94 / 349,
         ['charge.0', 'charge.1', 'charge.2', 'vines.0', 'stun.0', 'death.0'],
         [(303, 479), (774, 469), (1298, 483), (261, 933), (771, 931), (1255, 940)]),
    )
    for file, scale, names, anchors in sheets:
        image = Image.open(SOURCE / file).convert('RGBA')
        tiles = grid_sprites(image, 3, 2)
        bodies = {}
        for area, box, runs in components(image):
            if area < 20000: continue
            index = min(1, int((box[1] + box[3]) / image.height)) * 3 + min(2, int((box[0] + box[2]) * 1.5 / image.width))
            if index not in bodies or area > bodies[index][0]: bodies[index] = (area, box)
        for index, (name, anchor, tile) in enumerate(zip(names, anchors, tiles)):
            box = bodies[index][1]
            contact = (round((anchor[0] - box[0] + 3) * scale), round((anchor[1] - box[1] + 3) * scale))
            tile = tile.resize((round(tile.width * scale), round(tile.height * scale)), Image.Resampling.NEAREST)
            poses.append((name, tile, contact))
    left = max(p[2][0] for p in poses); right = max(p[1].width - p[2][0] for p in poses)
    top = max(p[2][1] for p in poses); bottom = max(p[1].height - p[2][1] for p in poses)
    size = (left + right + 8, top + bottom + 8)
    pivot = ((left + 4) / size[0], (bottom + 4) / size[1])
    for name, tile, contact in poses:
        canvas = Image.new('RGBA', size)
        canvas.alpha_composite(tile, (left + 4 - contact[0], top + 4 - contact[1]))
        add('castle.count.' + name, canvas, pivot)
    add('castle.count.phase.0', *frames['castle.count.vines.0'])


def effects():
    image = Image.open(SOURCE / 'castle-effects.png').convert('RGBA')
    centers = [(190, 230), (580, 230), (975, 220), (1350, 240),
               (190, 470), (576, 470), (968, 460), (1350, 470),
               (190, 690), (580, 705), (968, 690), (1350, 720),
               (190, 915), (580, 895), (970, 900), (1350, 915)]
    groups = [[] for _ in centers]
    for area, box, runs in components(image):
        if area < 4: continue
        center = ((box[0] + box[2]) * .5, (box[1] + box[3]) * .5)
        index = min(range(16), key=lambda i: (center[0] - centers[i][0]) ** 2 + (center[1] - centers[i][1]) ** 2)
        groups[index].append((box, runs))
    tiles = []
    pixels = np.asarray(image)
    for group in groups:
        mask = Image.new('L', image.size); draw = ImageDraw.Draw(mask)
        for _, runs in group:
            for y, left, right in runs: draw.line((left, y, right - 1, y), fill=255)
        mask = mask.filter(ImageFilter.MaxFilter(3))
        rgba = pixels.copy(); rgba[:, :, 3] = np.where(np.asarray(mask) > 0, rgba[:, :, 3], 0)
        tile = Image.fromarray(rgba)
        tiles.append(tile.crop(opaque_bounds(tile)))
    for row, prefix in enumerate(['vines', 'impact', 'warning', 'wine']):
        series = tiles[row * 4:row * 4 + 4]
        scale = 128 / max(t.width for t in series)
        size = (136, max(round(t.height * scale) for t in series) + 8)
        for col in range(4):
            tile = series[col]
            key = 'castle.fx.' + prefix + '.' + str(col)
            if prefix == 'warning':
                add(key, tile, (.5, .5), width=128)
            else:
                tile = tile.resize((round(tile.width * scale), round(tile.height * scale)), Image.Resampling.NEAREST)
                canvas = Image.new('RGBA', size)
                canvas.alpha_composite(tile, ((size[0] - tile.width) // 2,
                    size[1] - 4 - tile.height if prefix == 'vines' else (size[1] - tile.height) // 2))
                add(key, canvas, (.5, 4 / size[1] if prefix == 'vines' else .5))


def castle_eldritch():
    image = Image.open(SOURCE / 'castle-eldritch.png').convert('RGBA')
    tiles = grid_sprites(image, 4, 4)
    for row, unit in enumerate(['castle.banquet-eater', 'castle.flesh-knot', 'castle.hanging-count']):
        series = [tiles[row * 4 + col] for col in range(4)]
        scale = (96 if row == 2 else 64) / (opaque_bounds(series[0])[3] - opaque_bounds(series[0])[1])
        series = [t.resize((round(t.width * scale), round(t.height * scale)), Image.Resampling.NEAREST) for t in series]
        size = (max(t.width for t in series) + 8, max(t.height for t in series) + 8)
        for col, state in enumerate(['idle.0', 'attack.0', 'attack.1', 'death.0']):
            tile = series[col]
            canvas = Image.new('RGBA', size)
            canvas.alpha_composite(tile, ((size[0] - tile.width) // 2, size[1] - 4 - tile.height))
            add(unit + '.' + state, canvas, (.5, 4 / size[1]))
        for walk in ['walk.0', 'walk.1']:
            add(unit + '.' + walk, *frames[unit + '.idle.0'])
    for col, key in enumerate(['cable.0', 'cable.1', 'shadow.0', 'portal.0']):
        add('castle.fx.' + key, tiles[12 + col], (.5, .5), width=96)


def water_effects():
    minions = Image.open(SOURCE / 'deepsea-minions.png').convert('RGBA')
    add('deepsea.fx.projectile.0', minions.crop((1224, 535, 1295, 607)), (.5, .5), width=32)
    image = Image.open(SOURCE / 'deepsea-water-effects.png').convert('RGBA')
    tiles = grid_sprites(image, 4, 3)
    for row, prefix in enumerate(['flow', 'splash', 'portal']):
        for col in range(4):
            add('deepsea.fx.' + prefix + '.' + str(col), tiles[row * 4 + col], (.5, .5), width=128)
    image = Image.open(SOURCE / 'deepsea-octopus.png').convert('RGBA')
    tiles = grid_sprites(image, 4, 4)
    for col, prefix in enumerate(['globule', 'breath', 'emergence', 'beak']):
        add('deepsea.fx.' + prefix + '.0', tiles[12 + col], (.5, .5), width=128)


def deepsea_environment():
    add('deepsea.background', Image.open(SOURCE / 'deepsea-background.png'), (.5, .5))
    image = Image.open(SOURCE / 'deepsea-environment.png').convert('RGBA')
    boxes = {
        'floor': (20, 8, 310, 279), 'grate': (350, 8, 611, 279),
        'glass': (628, 48, 978, 269), 'rail': (989, 116, 1285, 270),
        'well': (8, 341, 380, 587), 'pump': (400, 271, 602, 604),
        'pipe': (670, 321, 958, 606), 'tank': (975, 281, 1288, 606),
        'rack': (10, 635, 365, 873), 'platform': (366, 617, 673, 873),
        'valve': (701, 603, 939, 882), 'lamp': (1023, 604, 1269, 884),
        'window': (10, 870, 332, 1219), 'tower': (344, 870, 671, 1219),
        'plants': (675, 870, 965, 1219), 'leak': (968, 910, 1289, 1219)
    }
    for key, box in boxes.items():
        tile = image.crop(box)
        if key in ('floor', 'grate'):
            tile = tile.crop((26, 26, tile.width - 26, tile.height - 26)).resize((64, 64), Image.Resampling.NEAREST)
            add('deepsea.' + key, tile, (.5, .5))
        else:
            add('deepsea.' + key, tile.crop(opaque_bounds(tile)), height=128 if key in ('pump', 'well', 'window', 'tower') else 80)


def pack(theme):
    selected = [(key, value) for key, value in frames.items() if key.startswith(theme + '.')]
    selected.sort(key=lambda item: (-item[1][0].height, -item[1][0].width, item[0]))
    width = 2048 if theme == 'deepsea' else 1024
    while True:
        x = y = 2
        row_height = 0
        placements = []
        for key, (tile, pivot) in selected:
            if x + tile.width + 2 > width:
                x = 2; y += row_height + 4; row_height = 0
            placements.append((key, tile, pivot, x, y))
            x += tile.width + 4; row_height = max(row_height, tile.height)
        # RGBA32 with clamp sampling and no mipmaps supports an exact-sized atlas.
        height = (y + row_height + 5) // 4 * 4
        if height <= 8192:
            break
        width *= 2
    atlas = Image.new('RGBA', (width, height))
    entries = {}
    for key, tile, pivot, x, y in placements:
        atlas.alpha_composite(tile, (x, y))
        entries[key] = dict(X=x, Y=y, W=tile.width, H=tile.height, PX=pivot[0], PY=pivot[1], PPU=32)
        if key in mounts: entries[key].update(MX=mounts[key][0], MY=mounts[key][1])
    DEST.mkdir(parents=True, exist_ok=True)
    atlas.save(DEST / (theme + '.png'))
    (DEST / (theme + '.json')).write_text(json.dumps(entries, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(f'{theme}: {len(entries)} sprites, {width}x{height}, RGBA {width*height*4//1024} KiB')


if __name__ == '__main__':
    environment()
    strip_grid('castle-minions-a.png', ['castle.footman', 'castle.knight', 'castle.crossbow'])
    strip_grid('castle-minions-b.png', ['castle.flower-ghost', 'castle.coffin', 'castle.wine-bat'])
    from BuildCastleDeathAssets import collect as deaths
    from BuildCastleCarrierAssets import collect as carriers
    from BuildKnightRearAssets import collect as rear
    deaths(add); carriers(add); rear(add)
    count()
    effects()
    castle_eldritch()
    from BuildCastleBossAssets import collect as castle_boss
    castle_boss(add)
    deepsea_environment()
    strip_grid('deepsea-minions.png', ['deepsea.grape', 'deepsea.shell-guard', 'deepsea.anemone', 'deepsea.lily'])
    strip_grid('deepsea-minor-bosses.png', ['deepsea.crown-lily', 'deepsea.valve-anemone'], logical_height=96)
    from BuildBloodAssets import collect as blood
    from BuildDeepSeaDeathAssets import collect as deep_deaths
    from BuildOctopusAssets import collect as octopus
    from BuildWhiteAssets import collect as white
    blood(add)
    deep_deaths(add)
    water_effects()
    octopus(add)
    white(add, mounts)
    pack('castle')
    pack('deepsea')
