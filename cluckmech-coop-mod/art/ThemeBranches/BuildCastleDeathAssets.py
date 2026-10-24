"""Collect six castle enemies' three-pose death animations at their standing-series scale."""
import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

from SpriteIslands import grid_sprites

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
SOURCE = HERE / 'sources'
SHEETS = (
    ('castle-deaths-a.png', 'castle-minions-a.png', ('footman', 'knight', 'crossbow')),
    ('castle-deaths-b.png', 'castle-minions-b.png', ('flower-ghost', 'coffin', 'wine-bat')),
)
# Impact poses already bend at the knees/roots. Preserve that loss of height relative to idle.
IMPACT_HEIGHT = {'knight': .90, 'coffin': .90}


def opaque_bounds(image):
    ys, xs = np.nonzero(np.asarray(image.getchannel('A')) >= 96)
    if not len(xs):
        raise ValueError('Death source contains an empty silhouette')
    return int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1


def sequences():
    for death_file, reference_file, names in SHEETS:
        deaths = grid_sprites(Image.open(SOURCE / death_file).convert('RGBA'), 3, 3)
        references = grid_sprites(Image.open(SOURCE / reference_file).convert('RGBA'), 6, 3)
        for row, name in enumerate(names):
            old = references[row * 6:(row + 1) * 6]
            old_bounds = [opaque_bounds(tile) for tile in old]
            logical_height = 70 if name == 'coffin' else 58
            # This is exactly the main builder's existing standing/attack reference.
            reference_height = max(b[3] - b[1] for b in old_bounds[:5])
            standing_scale = logical_height / reference_height
            old_idle = old[0].crop(old_bounds[0])
            idle = old_idle.resize((round(old_idle.width * standing_scale), round(old_idle.height * standing_scale)), Image.Resampling.NEAREST)
            tiles = [t.crop(opaque_bounds(t)) for t in deaths[row * 3:(row + 1) * 3]]
            impact_height = idle.height * IMPACT_HEIGHT.get(name, .95)
            death_scale = impact_height / tiles[0].height
            normalized = [t.resize((max(1, round(t.width * death_scale)), max(1, round(t.height * death_scale))), Image.Resampling.NEAREST) for t in tiles]
            old_width = max(round((b[2] - b[0]) * standing_scale) for b in old_bounds) + 8
            old_height = max(round((b[3] - b[1]) * standing_scale) for b in old_bounds) + 8
            canvas_size = (max(old_width, max(t.width for t in normalized) + 8), max(old_height, max(t.height for t in normalized) + 8))

            def aligned(tile):
                canvas = Image.new('RGBA', canvas_size)
                canvas.alpha_composite(tile, ((canvas.width - tile.width) // 2, canvas.height - 4 - tile.height))
                return canvas

            frames = [aligned(t) for t in normalized]
            if not (normalized[0].height > normalized[1].height > normalized[2].height):
                raise ValueError(f'{name}: expected a descending impact/collapse/rest sequence')
            if normalized[2].height >= idle.height * .85:
                raise ValueError(f'{name}: corpse height exceeds its standing-series relationship')
            if len({frame.tobytes() for frame in frames}) != 3:
                raise ValueError(f'{name}: each death pose needs a distinct image')
            yield name, frames, (.5, 4 / canvas_size[1]), aligned(idle), {
                'source': death_file, 'reference': reference_file, 'reference_height': reference_height,
                'logical_body_height': logical_height, 'standing_scale': standing_scale, 'death_scale': death_scale,
                'idle_opaque_height': idle.height, 'death_opaque_heights': [t.height for t in normalized],
                'canvas': list(canvas_size), 'pivot': [.5, 4 / canvas_size[1]], 'pixels_per_unit': 32,
            }


def collect(add):
    """Use after the standing-series collector so these three poses supply death.0 through death.2."""
    for name, frames, pivot, _, _ in sequences():
        for index, frame in enumerate(frames):
            add(f'castle.{name}.death.{index}', frame, pivot)


def previews(destination):
    destination.mkdir(parents=True, exist_ok=True)
    rows = list(sequences())
    width, row_height, scale = 448, 112, 1
    contact = Image.new('RGB', (width, row_height * 6 + 24), '#19232a')
    draw = ImageDraw.Draw(contact)
    for col, label in enumerate(('standing reference', 'impact', 'collapse', 'rest')):
        draw.text((col * 112 + 5, 5), label, fill='#d8e7e9')
    for row, (name, frames, _, idle, metadata) in enumerate(rows):
        for col, sprite in enumerate([idle] + frames):
            sprite = sprite.resize((sprite.width * scale, sprite.height * scale), Image.Resampling.NEAREST)
            # Contact sheet shows native PPU32 sprite pixels at one-to-one scale.
            x = col * 112 + 56 - sprite.width // 2
            y = row * row_height + 24 + 90 - sprite.height
            contact.paste(sprite, (x, y), sprite)
        draw.text((5, 24 + row * row_height + 94), name, fill='#d8e7e9')
    contact.save(destination / 'castle-deaths-contact.png')
    gif_frames = []
    scale = 3
    max_width = max(frames[0].width for _, frames, _, _, _ in rows) * scale + 48
    max_row_height = max(frames[0].height for _, frames, _, _, _ in rows) * scale + 25
    for index in range(4):
        canvas = Image.new('RGB', (max_width, max_row_height * len(rows)), '#19232a')
        labels = ImageDraw.Draw(canvas)
        for row, (name, frames, _, idle, _) in enumerate(rows):
            sprite = ([idle] + frames)[index]
            sprite = sprite.resize((sprite.width * scale, sprite.height * scale), Image.Resampling.NEAREST)
            canvas.paste(sprite, ((max_width - sprite.width) // 2, row * max_row_height + 18), sprite)
            labels.text((5, row * max_row_height + 2), name, fill='#d8e7e9')
        gif_frames.append(canvas)
    gif_frames[0].save(destination / 'castle-deaths-motion.gif', save_all=True, append_images=gif_frames[1:],
                       duration=[500, 170, 190, 650], loop=0, disposal=2)
    (destination / 'castle-deaths-scale.json').write_text(json.dumps({name: metadata for name, _, _, _, metadata in rows}, indent=2), encoding='utf-8')
    print(f'18 distinct death frames; contact/GIF/scale evidence: {destination}')


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--preview-dir', type=Path, default=ROOT / 'artifacts' / 'theme-art' / 'castle-deaths')
    previews(parser.parse_args().preview_dir)
