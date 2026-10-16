"""Register generated shield phases on a shared 256-pixel game canvas."""
from pathlib import Path

import numpy as np
from PIL import Image


def material(tile, impact=False):
    pixels = np.array(tile)
    highlight = np.clip((pixels[:, :, 1].astype(float) - 160) / 80, 0, 1)
    if impact:
        ceiling = .10 + .84 * np.clip((pixels[:, :, 1].astype(float) - 195) / 55, 0, 1)
    else:
        yy, xx = np.mgrid[:256, :256]
        radial = np.hypot((xx - 127.5) / 128, (yy - 127.5) / 128)
        edge = np.clip((radial - .64) / .1, 0, 1)
        ceiling = (.03 + .06 * highlight) * (1 - edge) + (.5 + .15 * highlight) * edge
    pixels[:, :, 3] = np.minimum(pixels[:, :, 3], np.round(255 * ceiling)).astype(np.uint8)
    return Image.fromarray(pixels)


def register(tile, scale, source_anchor, target_anchor=(127.5, 127.5)):
    return tile.transform((256, 256), Image.Transform.AFFINE,
                          (1 / scale, 0, source_anchor[0] - target_anchor[0] / scale,
                           0, 1 / scale, source_anchor[1] - target_anchor[1] / scale),
                          resample=Image.Resampling.NEAREST)


def interpolate(sequence, count=8):
    """Premultiplied interpolation retains clean transparent pixel edges."""
    arrays = [np.asarray(tile).astype(float) / 255 for tile in sequence]
    for position in np.linspace(0, len(arrays) - 1, count):
        index = min(int(position), len(arrays) - 2)
        amount = position - index
        first, second = arrays[index:index + 2]
        alpha = first[:, :, 3:] * (1 - amount) + second[:, :, 3:] * amount
        rgb = first[:, :, :3] * first[:, :, 3:] * (1 - amount) + second[:, :, :3] * second[:, :, 3:] * amount
        rgb = np.divide(rgb, alpha, out=np.zeros_like(rgb), where=alpha > 0)
        yield Image.fromarray(np.round(np.concatenate((rgb, alpha), axis=2) * 255).astype(np.uint8))


def compile_shield(frames):
    source = Path(__file__).resolve().parent / 'ShieldEnergy'
    idle = Image.new('RGBA', (256, 256))
    idle.alpha_composite(Image.open(source / 'idle.png').convert('RGBA').resize((240, 240), Image.Resampling.NEAREST), (8, 8))
    idle = material(idle)
    frames['fx.shield.loop.3'] = (idle, (.5, .5), False, 32)

    phases = {}
    # Measured full-rim references keep all generated fragments registered in place.
    for filename, scale, anchor in [('deployment', 194 / 536, (312.5, 312)),
                                     ('break', 194 / 506, (311.5, 320.5))]:
        sheet = Image.open(source / (filename + '.png')).convert('RGBA')
        cells = [sheet.crop((x * 627, y * 627, (x + 1) * 627, (y + 1) * 627))
                 for y in range(2) for x in range(2)]
        phases[filename] = [material(register(tile, scale, anchor)) for tile in cells]
    phases['start'] = phases.pop('deployment') + [idle]
    phases['end'] = [idle] + phases.pop('break') + [Image.new('RGBA', (256, 256))]

    sheet = Image.open(source / 'impact.png').convert('RGBA')
    # The generated gutter is below the first row's complete arc, rather than at half height.
    regions = [(0, 96, 627, 665), (627, 96, 1254, 665),
               (0, 665, 627, 1254), (627, 665, 1254, 1254)]
    contacts = [(507, 398), (1025, 399), (510, 946), (1040, 946)]
    patches = []
    for region, contact in zip(regions, contacts):
        anchor = (contact[0] - region[0], contact[1] - region[1])
        patches.append(material(register(sheet.crop(region), .34, anchor, (192, 128)), impact=True))
    phases['hit'] = patches + [Image.new('RGBA', (256, 256))]
    for phase in ['start', 'end', 'hit']:
        for index, tile in enumerate(interpolate(phases[phase])):
            pivot = (.75, .5) if phase == 'hit' else (.5, .5)
            frames[f'fx.shield.{phase}.{index}'] = (tile, pivot, False, 32)
