"""Collect authored giant-octopus poses, articulated arm parts and separate water effects."""
import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

from SpriteIslands import grid_sprites

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
SOURCE = HERE / 'sources'
BODY = ('idle.0', 'open.0', 'open.1', 'open.2', 'charge.0', 'emit.0', 'close.0', 'close.1',
        'death.0', 'death.1', 'death.2', 'idle.1')
SLAM = ('idle.0', 'windup.0', 'windup.1', 'down.0', 'strike.0', 'recover.0', 'recover.1', 'death.0')
BIND = ('idle.0', 'reach.0', 'reach.1', 'coil.0', 'coil.1', 'release.0', 'recover.0', 'death.0')
PARTS = ('stump.0', 'shaft.0', 'slam-tip.0', 'slam-tip.1', 'bind-tip.0', 'bind-tip.1', 'bind-tip.2', 'bind-tip.3')


def trim(image):
    ys, xs = np.nonzero(np.asarray(image.getchannel('A')) >= 96)
    return image.crop((int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1))


def sheet(name, rows):
    return [trim(tile) for tile in grid_sprites(Image.open(SOURCE / f'deepsea-octopus-{name}.png').convert('RGBA'), 4, rows)]


def normalize(tiles, width, align='root'):
    # One source-to-game scale per sequence keeps raised poses and collapsed corpses proportional.
    scale = width / tiles[0].width
    resized = [tile.resize((max(1, round(tile.width * scale)), max(1, round(tile.height * scale))), Image.Resampling.NEAREST) for tile in tiles]
    size = max(t.width for t in resized) + 8, max(t.height for t in resized) + 8
    frames = []
    for tile in resized:
        canvas = Image.new('RGBA', size)
        x = 4 if align == 'left' else (size[0] - tile.width) // 2
        y = (size[1] - tile.height) // 2 if align == 'left' else size[1] - 4 - tile.height
        canvas.alpha_composite(tile, (x, y)); frames.append(canvas)
    pivot = (4 / size[0], .5) if align == 'left' else (.5, 4 / size[1])
    return frames, pivot, {'source_scale': scale, 'opaque_sizes': [list(t.size) for t in resized], 'canvas': list(size), 'pivot': list(pivot), 'ppu': 32}


def banks():
    for name, unit, labels, width, rows in (
        ('body', 'octopus', BODY, 18 * 32, 3), ('slam', 'slam-arm', SLAM, 3 * 32, 2),
        ('bind', 'bind-arm', BIND, 3 * 32, 2)):
        frames, pivot, metadata = normalize(sheet(name, rows), width)
        if len(set(frame.tobytes() for frame in frames)) != len(frames):
            raise ValueError(f'{name}: repeated authored pose')
        yield f'deepsea.{unit}.', labels, frames, pivot, metadata
    for index, tile in enumerate(sheet('parts', 2)):
        # Rooted stump, shaft centre and isolated tip sockets have independent explicit mounts.
        width = (3, .95, 2.7, 3.4, 2.8, 2.8, 2.8, 2.8)[index] * 32
        frame = tile.resize((round(width), max(1, round(tile.height * width / tile.width))), Image.Resampling.NEAREST)
        if index == 1:
            # A connected arm uses the authored shaft's intact skin, with its end caps inside the joints.
            intact = frame.crop((0, 12, frame.width, frame.height - 12))
            yield 'deepsea.octopus.part.', ('joint.0',), (frame.crop((0, 22, frame.width, 50)),), (.5, .5), {'canvas': [frame.width, 28], 'pivot': [.5, .5], 'ppu': 32}
            yield 'deepsea.octopus.part.', (PARTS[index],), (intact,), (.5, .5), {'canvas': list(intact.size), 'pivot': [.5, .5], 'ppu': 32}
            continue
        # Pixel-measured sockets in the normalized images: shaft ends meet these centres.
        socket = ((48, 57), (15, 36), (34, 87), (5, 31), (15.3, 88.35), (15, 78), (23, 81), (14.4, 77.08))[index]
        pivot = socket[0] / frame.width, 1 - socket[1] / frame.height
        yield 'deepsea.octopus.part.', (PARTS[index],), (frame,), pivot, {'canvas': list(frame.size), 'pivot': list(pivot), 'ppu': 32}
    tiles = sheet('water', 3)
    for row, name in enumerate(('jet', 'fan', 'splash')):
        frames, pivot, metadata = normalize(tiles[row * 4:row * 4 + 4], 160, 'root' if row == 2 else 'left')
        yield f'deepsea.octopus.fx.{name}.', tuple(str(i) for i in range(4)), frames, pivot, metadata


def collect(add):
    """Run after the original deepsea collector; the new poses provide this boss's complete banks."""
    for prefix, names, frames, pivot, _ in banks():
        for name, frame in zip(names, frames): add(prefix + name, frame, pivot)
        if prefix in ('deepsea.octopus.', 'deepsea.slam-arm.', 'deepsea.bind-arm.'):
            add(prefix + 'rest.0', frames[names.index('death.2' if prefix == 'deepsea.octopus.' else 'death.0')], pivot)


def previews(destination):
    destination.mkdir(parents=True, exist_ok=True)
    data = list(banks()); metadata = {}
    for prefix, labels, frames, pivot, details in data:
        metadata[prefix + labels[0]] = details
        if len(frames) == 1: continue
        cell = (190, 180) if 'octopus.' not in prefix or '.fx.' in prefix else (300, 300)
        rows = (len(frames) + 3) // 4
        contact = Image.new('RGB', (cell[0] * 4, cell[1] * rows), '#1b303c')
        draw = ImageDraw.Draw(contact)
        for i, (label, frame) in enumerate(zip(labels, frames)):
            tile = frame.copy(); tile.thumbnail((cell[0] - 12, cell[1] - 28), Image.Resampling.NEAREST)
            contact.paste(tile, (i % 4 * cell[0] + (cell[0] - tile.width) // 2, i // 4 * cell[1] + 23 + (cell[1] - 28 - tile.height) // 2), tile)
            draw.text((i % 4 * cell[0] + 6, i // 4 * cell[1] + 6), label, fill='#e0edf0')
        name = prefix.strip('.').replace('.', '-')
        contact.save(destination / (name + '-contact.png'))
        gif = []
        for frame in frames:
            back = Image.new('RGB', frame.size, '#1b303c'); back.paste(frame, (0, 0), frame); gif.append(back)
        gif[0].save(destination / (name + '-motion.gif'), save_all=True, append_images=gif[1:], duration=180, loop=0, disposal=2)
    (destination / 'octopus-scale.json').write_text(json.dumps(metadata, indent=2), encoding='utf-8')
    print(f'{sum(len(row[2]) for row in data)} authored frames/parts; preview: {destination}')


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--preview-dir', type=Path, default=ROOT / 'artifacts' / 'theme-art' / 'octopus')
    previews(parser.parse_args().preview_dir)
