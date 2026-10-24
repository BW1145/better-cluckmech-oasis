"""Collect the generated blood-mother and blood-sac animation sprites at PPU 32."""
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

from SpriteIslands import components


SOURCE = Path(__file__).resolve().parent / 'sources'
FRAGMENT_OWNERS = {
    'blood-mother-attacks.png': {(1166, 455, 1180, 470): 2},
}

# Each contact point was measured against the rooted body, rather than the
# projectile, extended limb, loose droplets, or collapsed outer membrane.
SHEETS = (
    ('blood-mother-awaken.png', 3, 2, 160 / 483,
     ((266, 445), (772, 444), (1281, 444),
      (269, 964), (779, 974), (1293, 971)),
     tuple('deepsea.blood-mother.' + state for state in
           ('idle.0', 'idle.1', 'awaken.0', 'awaken.1', 'awaken.2', 'awaken.3'))),
    ('blood-mother-attacks.png', 4, 2, 160 / 347,
     ((197, 453), (568, 450), (935, 451), (1339, 452),
      (196, 887), (635, 882), (956, 887), (1347, 897)),
     tuple('deepsea.blood-mother.' + action + '.' + str(frame)
           for action in ('attack', 'sweep') for frame in range(4))),
    ('blood-mother-death.png', 4, 1, 156 / 504,
     ((284, 601), (821, 603), (1355, 602), (1891, 603)),
     tuple('deepsea.blood-mother.death.' + str(frame) for frame in range(4))),
    ('blood-sac-actions.png', 4, 3, 42 / 276,
     ((209, 334), (580, 332), (958, 343), (1328, 344),
      (207, 667), (578, 668), (960, 672), (1334, 674),
      (205, 974), (590, 970), (958, 970), (1335, 965)),
     tuple('deepsea.blood-sac.' + state for state in
           ('flight.0', 'flight.1', 'idle.0', 'idle.1',
            'burst.0', 'burst.1', 'burst.2', 'burst.3',
            'death.0', 'death.1', 'death.2', 'death.3'))),
)


def _distance_squared(a, b):
    dx = max(a[0] - b[2], b[0] - a[2], 0)
    dy = max(a[1] - b[3], b[1] - a[3], 0)
    return dx * dx + dy * dy


def _extract(image, columns, rows, fragment_owners=None):
    """Keep entire silhouettes across grid lines, plus their detached droplets."""
    islands = components(image)
    cell_w, cell_h = image.width / columns, image.height / rows
    primary = {}
    for island in islands:
        area, box, _ = island
        if area < 2000:
            continue
        column = min(columns - 1, int((box[0] + box[2]) * .5 / cell_w))
        row = min(rows - 1, int((box[1] + box[3]) * .5 / cell_h))
        index = row * columns + column
        if index not in primary or area > primary[index][0]:
            primary[index] = island
    if len(primary) != columns * rows:
        raise ValueError(f'Expected {columns * rows} bodies, found {len(primary)}')

    groups = {index: [island] for index, island in primary.items()}
    primary_ids = {id(island) for island in primary.values()}
    for island in islands:
        if id(island) in primary_ids or island[0] < 4:
            continue
        index = (fragment_owners or {}).get(island[1])
        if index is None:
            index = min(primary, key=lambda key: _distance_squared(island[1], primary[key][1]))
        # This range covers the generated rupture droplets while keeping them
        # assigned to the intact body with the closest visible contour.
        if _distance_squared(island[1], primary[index][1]) > (min(cell_w, cell_h) * .3) ** 2:
            raise ValueError(f'Detached fragment needs inspection: {island[1]}')
        groups[index].append(island)

    result = []
    pixels = np.asarray(image)
    for index in range(columns * rows):
        selected = groups[index]
        left = max(0, min(item[1][0] for item in selected) - 3)
        top = max(0, min(item[1][1] for item in selected) - 3)
        right = min(image.width, max(item[1][2] for item in selected) + 3)
        bottom = min(image.height, max(item[1][3] for item in selected) + 3)
        mask = Image.new('L', image.size)
        draw = ImageDraw.Draw(mask)
        for _, _, runs in selected:
            for y, x0, x1 in runs:
                draw.line((x0, y, x1 - 1, y), fill=255)
        mask = mask.filter(ImageFilter.MaxFilter(3))
        rgba = pixels[top:bottom, left:right].copy()
        rgba[:, :, 3] = np.where(np.asarray(mask)[top:bottom, left:right] > 0, rgba[:, :, 3], 0)
        result.append((Image.fromarray(rgba), (left, top)))
    return result


def _frames():
    result = {}
    for filename, columns, rows, scale, anchors, keys in SHEETS:
        image = Image.open(SOURCE / filename).convert('RGBA')
        extracted = _extract(image, columns, rows, FRAGMENT_OWNERS.get(filename))
        for key, (tile, origin), anchor in zip(keys, extracted, anchors):
            size = max(1, round(tile.width * scale)), max(1, round(tile.height * scale))
            sprite = tile.resize(size, Image.Resampling.NEAREST)
            contact = (round((anchor[0] - origin[0]) * scale),
                       round((anchor[1] - origin[1]) * scale))
            result[key] = sprite, contact
    return result


def collect(add):
    """Call add(key, RGBA, pivot) for all thirty complete animation frames."""
    frames = _frames()
    for family in ('deepsea.blood-mother.', 'deepsea.blood-sac.'):
        chosen = [(key, tile, contact) for key, (tile, contact) in frames.items()
                  if key.startswith(family)]
        extent_left = max(contact[0] for _, _, contact in chosen)
        extent_right = max(tile.width - contact[0] for _, tile, contact in chosen)
        extent_top = max(contact[1] for _, _, contact in chosen)
        extent_bottom = max(tile.height - contact[1] for _, tile, contact in chosen)
        width = extent_left + extent_right + 8
        height = extent_top + extent_bottom + 8
        anchor = extent_left + 4, extent_top + 4
        pivot = anchor[0] / width, (height - anchor[1]) / height
        for key, tile, contact in chosen:
            canvas = Image.new('RGBA', (width, height))
            canvas.alpha_composite(tile, (anchor[0] - contact[0], anchor[1] - contact[1]))
            add(key, canvas, pivot)


if __name__ == '__main__':
    collected = {}
    collect(lambda key, image, pivot: collected.__setitem__(key, (image, pivot)))
    for family, columns in (('blood-mother', 6), ('blood-sac', 4)):
        selected = [(key.rsplit(family + '.', 1)[1], tile, pivot)
                    for key, (tile, pivot) in collected.items() if '.' + family + '.' in key]
        width, height = selected[0][1].size
        rows = (len(selected) + columns - 1) // columns
        sheet = Image.new('RGBA', (columns * (width + 12), rows * (height + 26)), (16, 26, 38, 255))
        draw = ImageDraw.Draw(sheet)
        for index, (state, tile, pivot) in enumerate(selected):
            x, y = (index % columns) * (width + 12) + 6, (index // columns) * (height + 26)
            sheet.alpha_composite(tile, (x, y + 18))
            draw.text((x, y + 2), state, fill=(225, 230, 240, 255))
            # The contact mark lives in the preview margin, outside game sprites.
            px, py = x + round(pivot[0] * width), y + 18 + round((1 - pivot[1]) * height)
            draw.line((px - 2, py, px + 2, py), fill=(70, 220, 170, 255))
        sheet.save(SOURCE / (family + '-preview.png'))
        print(f'{family}: {len(selected)} frames, shared canvas {width}x{height}, PPU 32, pivot {selected[0][2]}')
