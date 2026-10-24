"""Rasterize ThemeOctopusVisual's action-clock transforms at native PPU32 for art QA."""
import json
import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

from BuildOctopusAssets import collect

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
OUT = ROOT / 'artifacts' / 'theme-art' / 'octopus'
FRAMES = {}
collect(lambda key, image, pivot: FRAMES.__setitem__(key, (image, pivot)))
atlas_root = ROOT / 'src' / 'StratagemAssets'
atlas = Image.open(atlas_root / 'stratagem-v2.png').convert('RGBA')
for key, box in json.loads((atlas_root / 'stratagem-v2.json').read_text(encoding='utf-8')).items():
    if key in ('mortar-base', 'mortar-housing.0'):
        FRAMES[key] = (atlas.crop((box['X'], box['Y'], box['X'] + box['W'], box['Y'] + box['H'])), (box['PX'], box['PY']))


def smooth(t):
    t = max(0, min(1, t)); return t * t * (3 - 2 * t)


def rotate(point, angle):
    angle = math.radians(angle); c, s = math.cos(angle), math.sin(angle)
    return np.array((point[0] * c - point[1] * s, point[0] * s + point[1] * c))


class View:
    def __init__(self, box=(-4, -6, 13, 8)):
        self.box = box
        self.size = (round((box[2] - box[0]) * 32), round((box[3] - box[1]) * 32))

    def pixel(self, world):
        return (world[0] - self.box[0]) * 32, (self.box[3] - world[1]) * 32

    def blank(self):
        image = Image.new('RGBA', self.size, '#172d38'); d = ImageDraw.Draw(image)
        for x in range(math.ceil(self.box[0]), math.ceil(self.box[2])):
            px, _ = self.pixel((x, 0)); d.line((px, 0, px, self.size[1]), fill='#243d48')
        for y in range(math.ceil(self.box[1]), math.ceil(self.box[3])):
            _, py = self.pixel((0, y)); d.line((0, py, self.size[0], py), fill='#243d48')
        return image

    def sprite(self, key, point, scale=(1, 1), angle=0):
        source, pivot = FRAMES[key]
        if key == 'deepsea.octopus.part.shaft.0':
            height = max(1, round(source.height * scale[1])); tiled = Image.new('RGBA', (source.width, height))
            for y in range(0, height, source.height): tiled.alpha_composite(source, (0, y))
            source = tiled; scale = (scale[0], 1)
        px, py = pivot[0] * source.width, (1 - pivot[1]) * source.height
        c, s = math.cos(math.radians(angle)), math.sin(math.radians(angle))
        a, b, c2, d = c * scale[0], s * scale[1], -s * scale[0], c * scale[1]
        at = self.pixel(point); tx, ty = at[0] - a * px - b * py, at[1] - c2 * px - d * py
        det = a * d - b * c2
        inv = d / det, -b / det, (-d * tx + b * ty) / det, -c2 / det, a / det, (c2 * tx - a * ty) / det
        return source.transform(self.size, Image.Transform.AFFINE, inv, Image.Resampling.NEAREST)

    def marker(self, image, point, color, label=None):
        x, y = self.pixel(point); d = ImageDraw.Draw(image)
        d.ellipse((x - 3, y - 3, x + 3, y + 3), fill=color)
        if label: d.text((x + 7, y + 4), label, fill=color)


def limb(target, pose, extension, lifted, direction):
    root = np.array((0, 52 / 32)); target = np.array(target, dtype=float)
    end = root + np.array((0, .9)) + (target - root - (0, .9)) * extension
    if lifted: end += (0, 1.1 * math.sin(extension * math.pi))
    coil = pose.startswith('bind-')
    angle = math.degrees(math.atan2(end[1] + (.65 if coil else 0) - root[1], end[0] - root[0])) - 90
    center = {'bind-tip.1': (36 / 32, 35 / 32), 'bind-tip.2': (25 / 32, 38 / 32),
              'bind-tip.0': (1.125, 1.4), 'bind-tip.3': (1.1875, 1)}.get(pose)
    socket = end + (0, .65) - rotate(center, angle) if coil else end.copy()
    if pose == 'slam-tip.1':
        socket -= direction * 1.55; angle = math.degrees(math.atan2(direction[1], direction[0]))
    delta = socket - root
    parts = [
        ('deepsea.octopus.part.stump.0', np.array((0, 0)), (1, 1), 0, root[1] * .01 - .03, 'stump'),
        ('deepsea.octopus.part.shaft.0', (root + socket) * .5, (1, max(.1, np.linalg.norm(delta) + .6) / (48 / 32)),
         math.degrees(math.atan2(delta[1], delta[0])) - 90, min(root[1], socket[1]) * .01 - .04, 'shaft'),
        ('deepsea.octopus.part.' + pose, socket, (1, 1), angle, target[1] * .01 - .055, 'tip'),
        ('deepsea.octopus.part.joint.0', root, (1.3, 1.3), math.degrees(math.atan2(delta[1], delta[0])) - 90,
         min(root[1], socket[1]) * .01 - .08, 'root-joint'),
        ('deepsea.octopus.part.joint.0', socket, (1.3, 1.3), math.degrees(math.atan2(delta[1], delta[0])) - 90,
         min(target[1], socket[1]) * .01 - .08, 'tip-joint'),
    ]
    return parts, {'root_socket': root.tolist(), 'tip_socket': socket.tolist(), 'target': target.tolist(),
                   'extension': extension, 'tip_pose': pose, 'tip_angle': angle,
                   'coil_center': (end + (0, .65)).tolist() if coil else None}


def arm_state(kind, seconds):
    target = np.array((8., -3. if kind == 'slam' else -2.))
    direction = target / np.linalg.norm(target)
    if kind == 'slam':
        if seconds < 1.5:
            t = seconds / 1.5
            if t >= .72: return (*limb(target, 'slam-tip.0', smooth((t - .72) / .28), True, direction), target, 'warning: extend')
            pose = 'windup.0' if t < .4 else 'windup.1'
        elif seconds < 2.35:
            age = seconds - 1.5
            return (*limb(target, 'slam-tip.1' if age < .12 else 'slam-tip.0',
                          1 if age < .12 else 1 - smooth((age - .12) / .73), age >= .12, direction), target,
                    'strike / damage' if age < .12 else 'recover')
        else: pose = 'idle.0'
        return [('deepsea.slam-arm.' + pose, (0, 0), (1, 1), 0, -.025, 'body')], {}, target, pose
    if seconds < 2:
        t = seconds / 2
        reach = 0 if t <= .3 else smooth((t - .3) / .7)
        if reach > .01: return (*limb(target, 'bind-tip.0', reach, False, direction), target, 'warning: reach')
        return [('deepsea.bind-arm.reach.0', (0, 0), (1, 1), 0, -.025, 'body')], {}, target, 'warning: brace'
    if seconds < 6:
        # Two small authoritative position corrections exercise netId-following while the victim stays bound.
        target += (0 if seconds < 3.2 else .6 if seconds < 4.4 else 1., 0)
        return (*limb(target, 'bind-tip.1' if seconds - 2 < .25 else 'bind-tip.2', 1, False, direction), target, 'hold: follow target netId')
    if seconds < 6.7:
        target += (1, 0)
        return (*limb(target, 'bind-tip.3', 1 - smooth((seconds - 6) / .7), False, direction), target, 'release / retract')
    return [('deepsea.bind-arm.idle.0', (0, 0), (1, 1), 0, -.025, 'body')], {}, target, 'idle'


def arm_frame(kind, seconds, mark=False):
    view = View(); image = view.blank(); parts, details, target, label = arm_state(kind, seconds)
    parts += [('mortar-base', target, (.65, .65), 0, target[1] * .01 - .021, 'target-base'),
              ('mortar-housing.0', target + (0, .2), (.65, .65), 0, target[1] * .01 - .025, 'target')]
    masks = {}
    for key, point, scale, angle, _, name in sorted(parts, key=lambda entry: -entry[4]):
        layer = view.sprite(key, point, scale, angle); image.alpha_composite(layer); masks[name] = np.array(layer.getchannel('A')) >= 96
    if 'shaft' in masks:
        details['overlap_pixels'] = {name: int(np.count_nonzero(masks['shaft'] & masks[name])) for name in ('stump', 'tip')}
    if mark:
        view.marker(image, target, '#ffe07c', 'target feet')
        if details:
            view.marker(image, details['root_socket'], '#8bfff0')
            view.marker(image, details['tip_socket'], '#8bfff0')
            if details['coil_center']: view.marker(image, details['coil_center'], '#ff91e6', 'coil centre')
    d = ImageDraw.Draw(image); d.rectangle((0, 0, image.width, 44), fill='#0f2029')
    d.text((8, 6), f'{kind.upper()}  game time {seconds:.2f}s  |  {label}', fill='#edf4ef')
    d.text((8, 23), 'Native PPU32 | actor-message timing | textured socket / shaft / tip', fill='#9ebfca')
    return image.convert('RGB'), details


def mouth_alignment():
    view = View((-14, -10, 10, 12)); image = view.blank()
    image.alpha_composite(view.sprite('deepsea.octopus.emit.0', (-89.5 / 32, -7.5)))
    d = ImageDraw.Draw(image)
    a, b = view.pixel((-4, 1)), view.pixel((4, -2))
    d.rectangle((*a, *b), outline='#ffe07c', width=2)
    view.marker(image, (0, -.5), '#ffe07c', 'measured beak = native box centre')
    view.marker(image, (0, 0), '#ff91e6', 'actor origin')
    view.marker(image, (-89.5 / 32, -7.5), '#8bfff0', 'body feet pivot')
    d.rectangle((0, 0, image.width, 66), fill='#0f2029')
    d.text((8, 7), 'OCTOPUS mouth / native hitbox alignment | PPU32 | idle width 18 world units', fill='#edf4ef')
    d.text((8, 26), 'emit canvas 611x538 | beak pixel (395,310) | feet pixel (305.5,534)', fill='#9ebfca')
    d.text((8, 45), 'rig offset (-2.796875,-7.5) | beak world (0,-0.5) | native trigger 8x3', fill='#9ebfca')
    image.convert('RGB').save(OUT / 'octopus-mouth-hitbox.png')


def siege_frame(seconds):
    view = View((-14, -13, 10, 12)); image = view.blank(); target = np.array((4., -11.))
    if seconds < 1.5:
        t = seconds / 1.5; pose = 'open.' + str(min(1, int(t * 4))) if t < .5 else 'charge.0'
    elif seconds < 1.9: pose = 'emit.0'
    elif seconds < 2.35: pose = 'close.' + str(min(1, int((seconds - 1.9) * 5)))
    else: pose = 'idle.0'
    image.alpha_composite(view.sprite('deepsea.octopus.' + pose, (-89.5 / 32, -7.5)))
    if 1.5 <= seconds < 1.9:
        frame = int((seconds - 1.5) * 12) % 4
        mouth = np.array((0., -.5)); delta = target - mouth; key = 'deepsea.octopus.fx.jet.' + str(frame)
        sprite = FRAMES[key][0]
        scale = np.linalg.norm(delta) * 32 / (159, 163, 168, 170)[frame], 1.2 / (sprite.height / 32)
        image.alpha_composite(view.sprite(key, mouth, scale, math.degrees(math.atan2(delta[1], delta[0]))))
    if 1.5 <= seconds < 2:
        index = min(3, int((seconds - 1.5) / .5 * 4)); key = 'deepsea.octopus.fx.splash.' + str(index)
        scale = 3.6 / (FRAMES[key][0].width / 32)
        image.alpha_composite(view.sprite(key, target, (scale, scale)))
    view.marker(image, (0, -.5), '#ffe07c', 'beak')
    view.marker(image, target, '#ffe07c', 'actual base target')
    d = ImageDraw.Draw(image); d.rectangle((0, 0, image.width, 45), fill='#0f2029')
    d.text((8, 6), f'SIEGE {seconds:.2f}s | 1.5s windup -> 0.4s focused water -> closing', fill='#edf4ef')
    d.text((8, 25), 'Jet source = measured beak = native attack origin; visible jet endpoint = actual base', fill='#9ebfca')
    return image.convert('RGB')


def contact(frames, filename, columns=3):
    w, h = frames[0].size
    image = Image.new('RGB', (w * columns, h * math.ceil(len(frames) / columns)), '#172d38')
    for i, frame in enumerate(frames): image.paste(frame, (i % columns * w, i // columns * h))
    image.save(OUT / filename)


def main():
    OUT.mkdir(parents=True, exist_ok=True); audit = {}
    for kind, duration, keys in (
        ('slam', 2.8, (.55, 1.10, 1.35, 1.50, 1.80, 2.25)),
        ('bind', 7.2, (.40, 1.20, 1.90, 2.05, 3.60, 4.80, 6.10, 6.50, 6.90))):
        frames = []; overlaps = []
        for i in range(round(duration * 20) + 1):
            image, data = arm_frame(kind, i / 20); frames.append(image)
            if 'overlap_pixels' in data: overlaps.append((round(i / 20, 2), data['overlap_pixels']))
        frames[0].save(OUT / f'octopus-{kind}-timing.gif', save_all=True, append_images=frames[1:], duration=50, loop=0, disposal=2)
        contact([arm_frame(kind, time, True)[0] for time in keys], f'octopus-{kind}-timing-contact.png')
        audit[kind] = {'keyframes': {str(time): arm_frame(kind, time)[1] for time in keys}, 'socket_overlap_pixels': overlaps}
    mouth_alignment()
    frames = [siege_frame(i / 20) for i in range(53)]
    frames[0].save(OUT / 'octopus-siege-timing.gif', save_all=True, append_images=frames[1:], duration=50, loop=0, disposal=2)
    contact([siege_frame(t) for t in (1.40, 1.50, 1.60, 1.75, 1.85, 2.15)], 'octopus-siege-timing-contact.png', 3)
    audit['core'] = {'emit_canvas': [611, 538], 'feet_pixel': [305.5, 534], 'beak_pixel': [395, 310],
                     'beak_from_feet': [89.5 / 32, 7], 'rig_offset': [-89.5 / 32, -7.5], 'world_beak': [0, -.5],
                     'native_box_center': [0, -.5], 'native_box_size': [8, 3]}
    (OUT / 'octopus-action-geometry.json').write_text(json.dumps(audit, indent=2), encoding='utf-8')
    for name in ('slam', 'bind'):
        rows = audit[name]['socket_overlap_pixels']
        print(name, 'minimum joint overlap:', {joint: min(row[1][joint] for row in rows) for joint in ('stump', 'tip')})
    print('Action timing GIFs, contact sheets and mouth/hitbox evidence:', OUT)


if __name__ == '__main__': main()
