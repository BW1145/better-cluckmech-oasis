"""Collect deep-sea collapses and the drowned carrier at a shared contact point."""
from pathlib import Path
import numpy as np
from PIL import Image
from SpriteIslands import grid_sprites

SOURCE = Path(__file__).resolve().parent / 'sources'


def bounds(image):
    y, x = np.nonzero(np.asarray(image.getchannel('A')) >= 96)
    return int(x.min()), int(y.min()), int(x.max()) + 1, int(y.max()) + 1


def series(add, prefix, tiles, names, height, reference=0):
    # One scale per character; later collapsed drawings keep their lower height.
    box = bounds(tiles[reference])
    scale = height / (box[3] - box[1])
    poses = []
    for tile in tiles:
        tile = tile.crop(bounds(tile))
        poses.append(tile.resize((max(1, round(tile.width * scale)), max(1, round(tile.height * scale))), Image.Resampling.NEAREST))
    size = max(p.width for p in poses) + 8, max(p.height for p in poses) + 8
    for name, pose in zip(names, poses):
        canvas = Image.new('RGBA', size)
        canvas.alpha_composite(pose, ((size[0] - pose.width) // 2, size[1] - 4 - pose.height))
        add(prefix + '.' + name, canvas, (.5, 4 / size[1]))


def collect(add):
    tiles = grid_sprites(Image.open(SOURCE / 'deepsea-deaths.png').convert('RGBA'), 3, 4)
    for row, unit in enumerate(['grape', 'shell-guard', 'anemone', 'lily']):
        series(add, 'deepsea.' + unit, tiles[row * 3:row * 3 + 3],
               ['death.0', 'death.1', 'death.2'], 54 if row == 0 else 58)
    tiles = grid_sprites(Image.open(SOURCE / 'deepsea-minor-deaths.png').convert('RGBA'), 3, 2)
    for row, unit in enumerate(['crown-lily', 'valve-anemone']):
        series(add, 'deepsea.' + unit, tiles[row * 3:row * 3 + 3],
               ['death.0', 'death.1', 'death.2'], 88)
    tiles = grid_sprites(Image.open(SOURCE / 'deepsea-drowned.png').convert('RGBA'), 4, 2)
    series(add, 'deepsea.drowned', tiles,
           ['idle.0', 'walk.0', 'walk.1', 'attack.0', 'attack.1', 'death.0', 'death.1', 'death.2'], 58)
