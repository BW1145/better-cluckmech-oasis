"""Collect the existing castle carriers' open-coffin and empty-flask poses."""
from pathlib import Path

import numpy as np
from PIL import Image

from SpriteIslands import grid_sprites


SOURCE = Path(__file__).resolve().parent / 'sources'

# Native art uses 70 / 277 and 58 / 260 pixels per source pixel. The new
# sheet's matching body landmarks (coffin underside-to-feet; eye-to-flask
# baseline) determine the scale, so the open lid retains its full extension.
FAMILIES = (
    ('castle.coffin.empty.', 70 / 277 * 161 / 204,
     ((208, 456), (654, 456), (1067, 456), (1480, 463))),
    ('castle.wine-bat.empty.', 58 / 260 * 145 / 198,
     ((238, 851), (672, 864), (1097, 895), (1572, 872))),
)


def _bounds(image):
    y, x = np.nonzero(np.asarray(image.getchannel('A')) >= 96)
    return int(x.min()), int(y.min()), int(x.max()) + 1, int(y.max()) + 1


def collect(add):
    """Call add(key, RGBA, pivot) for four empty poses of each carrier."""
    image = Image.open(SOURCE / 'castle-empty-carriers.png').convert('RGBA')
    tiles = grid_sprites(image, 4, 2)
    # Each complete island remains within its measured bounds. These are
    # silhouette origins, including three pixels of the extraction margin.
    origins = ((59, 64), (484, 55), (918, 55), (1326, 43),
               (28, 499), (455, 563), (900, 588), (1329, 518))
    for row, (prefix, scale, anchors) in enumerate(FAMILIES):
        frames = []
        for column, anchor in enumerate(anchors):
            index = row * 4 + column
            tile = tiles[index]
            origin = origins[index]
            tile = tile.resize((max(1, round(tile.width * scale)),
                                max(1, round(tile.height * scale))), Image.Resampling.NEAREST)
            contact = (round((anchor[0] - origin[0]) * scale),
                       round((anchor[1] - origin[1]) * scale))
            frames.append((tile, contact))
        left = max(contact[0] for _, contact in frames)
        right = max(tile.width - contact[0] for tile, contact in frames)
        top = max(contact[1] for _, contact in frames)
        bottom = max(tile.height - contact[1] for tile, contact in frames)
        size = left + right + 8, top + bottom + 8
        pivot = (left + 4) / size[0], (bottom + 4) / size[1]
        for index, (tile, contact) in enumerate(frames):
            canvas = Image.new('RGBA', size)
            canvas.alpha_composite(tile, (left + 4 - contact[0], top + 4 - contact[1]))
            add(prefix + str(index), canvas, pivot)


if __name__ == '__main__':
    frames = {}
    collect(lambda key, image, pivot: frames.__setitem__(key, (image, pivot)))
    for key, (image, pivot) in frames.items():
        print(key, image.size, pivot, 'visible', _bounds(image))
