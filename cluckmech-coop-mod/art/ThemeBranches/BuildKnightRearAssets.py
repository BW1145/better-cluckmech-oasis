"""Collect the pumpkin knight's rear-facing poses with its front-series scale and foot anchor."""
import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

from SpriteIslands import grid_sprites

HERE = Path(__file__).resolve().parent
SOURCE = HERE / 'sources'
POSES = ('idle.0', 'walk.0', 'walk.1', 'attack.0', 'attack.1', 'death.0')


def bounds(image):
    ys, xs = np.nonzero(np.asarray(image.getchannel('A')) >= 96)
    if not len(xs):
        raise ValueError('Knight pose contains an empty silhouette')
    return int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1


def poses():
    reference = grid_sprites(Image.open(SOURCE / 'castle-minions-a.png').convert('RGBA'), 6, 3)[6:12]
    rear = grid_sprites(Image.open(SOURCE / 'castle-knight-rear.png').convert('RGBA'), 3, 2)
    reference = [tile.crop(bounds(tile)) for tile in reference]
    rear = [tile.crop(bounds(tile)) for tile in rear]
    standing_scale = 58 / max(tile.height for tile in reference[:5])
    # The generated rear sheet uses larger source pixels. Match idle body height first, then
    # retain this one scale for walking, raised weapons and the lower fallen silhouette.
    idle_height = round(reference[0].height * standing_scale)
    rear_scale = idle_height / rear[0].height
    front = [tile.resize((max(1, round(tile.width * standing_scale)), max(1, round(tile.height * standing_scale))), Image.Resampling.NEAREST) for tile in reference]
    back = [tile.resize((max(1, round(tile.width * rear_scale)), max(1, round(tile.height * rear_scale))), Image.Resampling.NEAREST) for tile in rear]
    size = (max(tile.width for tile in front + back) + 8, max(tile.height for tile in front + back) + 8)

    def aligned(tile):
        canvas = Image.new('RGBA', size)
        canvas.alpha_composite(tile, ((size[0] - tile.width) // 2, size[1] - 4 - tile.height))
        return canvas

    if len({tile.tobytes() for tile in back}) != len(POSES):
        raise ValueError('Knight rear poses must have six distinct silhouettes')
    if not back[-1].height < back[0].height:
        raise ValueError('The fallen rear pose retains its reduced physical height')
    return [aligned(t) for t in front], [aligned(t) for t in back], (.5, 4 / size[1]), {
        'reference': 'castle-minions-a.png', 'source': 'castle-knight-rear.png', 'pixels_per_unit': 32,
        'logical_body_height': 58, 'front_scale': standing_scale, 'rear_scale': rear_scale,
        'front_opaque_heights': [t.height for t in front], 'rear_opaque_heights': [t.height for t in back],
        'canvas': list(size), 'pivot': [.5, 4 / size[1]],
    }


def collect(add):
    _, back, pivot, _ = poses()
    for name, image in zip(POSES, back):
        add('castle.knight.back.' + name, image, pivot)


def preview(destination):
    destination.mkdir(parents=True, exist_ok=True)
    front, back, _, metadata = poses()
    cell_width, cell_height = max(front[0].width + 16, 100), max(front[0].height + 30, 106)
    contact = Image.new('RGB', (cell_width * 6, cell_height * 2 + 24), '#19232a')
    draw = ImageDraw.Draw(contact)
    for col, name in enumerate(POSES):
        draw.text((col * cell_width + 5, 4), name, fill='#d8e7e9')
    for row, series in enumerate((front, back)):
        for col, image in enumerate(series):
            contact.paste(image, (col * cell_width + (cell_width - image.width) // 2, row * cell_height + 24), image)
        draw.text((5, row * cell_height + 24 + front[0].height + 4), 'front reference' if row == 0 else 'rear north-east', fill='#d8e7e9')
    contact.save(destination / 'castle-knight-front-back.png')
    movie = []
    for image in back:
        frame = Image.new('RGB', ((image.width + 12) * 3, (image.height + 12) * 3), '#19232a')
        image = image.resize((image.width * 3, image.height * 3), Image.Resampling.NEAREST)
        frame.paste(image, (18, 18), image); movie.append(frame)
    movie[0].save(destination / 'castle-knight-rear-motion.gif', save_all=True, append_images=movie[1:],
                  duration=[400, 170, 170, 260, 220, 600], loop=0, disposal=2)
    (destination / 'castle-knight-rear-scale.json').write_text(json.dumps(metadata, indent=2), encoding='utf-8')
    print(f'6 rear poses, matched front/back idle height: {metadata["front_opaque_heights"][0]}/{metadata["rear_opaque_heights"][0]} px; {destination}')


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--preview-dir', type=Path, default=HERE.parents[1] / 'artifacts' / 'theme-art' / 'knight-rear')
    preview(parser.parse_args().preview_dir)
