"""Mount the generated castle boss bodies independently from their live tethers."""
from pathlib import Path

from PIL import Image
from SpriteIslands import grid_sprites


SOURCE = Path(__file__).resolve().parent / 'sources'
STATES = ('idle.0', 'attack.0', 'attack.1', 'death.0')

# Absolute source measurements: the cut follows the upper anatomical edge;
# contacts follow the kernel/tail or the standing feet and prone abdomen.
# One common source-pixel scale per body keeps fallen poses physically lower.
BODIES = (
    ('castle.flesh-knot.', 1, 64 / 252,
     ((109, 247), (483, 247), (875, 248), (1172, 249)),
     (310, 316, 309, 249),
     ((205, 496), (573, 504), (967, 505), (1356, 513))),
    ('castle.hanging-count.', 2, 96 / 299,
     ((77, 502), (394, 502), (749, 529), (1163, 554)),
     (533, 579, 646, 630),
     ((209, 781), (570, 764), (982, 765), (1360, 765))),
)


def collect(add):
    """Collect same-key boss bodies after the base castle_eldritch collector."""
    image = Image.open(SOURCE / 'castle-eldritch.png').convert('RGBA')
    source = grid_sprites(image, 4, 4)
    for prefix, row, scale, origins, cuts, anchors in BODIES:
        frames = []
        for col in range(4):
            tile = source[row * 4 + col]
            x, y = origins[col]
            top = cuts[col] - y
            tile = tile.crop((0, top, tile.width, tile.height))
            tile = tile.resize((max(1, round(tile.width * scale)),
                                max(1, round(tile.height * scale))), Image.Resampling.NEAREST)
            contact = round((anchors[col][0] - x) * scale), round((anchors[col][1] - cuts[col]) * scale)
            frames.append((tile, contact))
        left = max(contact[0] for _, contact in frames)
        right = max(tile.width - contact[0] for tile, contact in frames)
        top = max(contact[1] for _, contact in frames)
        bottom = max(tile.height - contact[1] for tile, contact in frames)
        size = left + right + 8, top + bottom + 8
        pivot = (left + 4) / size[0], (bottom + 4) / size[1]
        output = []
        for state, (tile, contact) in zip(STATES, frames):
            canvas = Image.new('RGBA', size)
            canvas.alpha_composite(tile, (left + 4 - contact[0], top + 4 - contact[1]))
            add(prefix + state, canvas, pivot)
            output.append(canvas)
        for state in ('walk.0', 'walk.1'):
            add(prefix + state, output[0], pivot)


if __name__ == '__main__':
    collect(lambda key, image, pivot: print(key, image.size, pivot))
