"""Collect the generated White Visitor anatomy with shared canvases and measured contacts."""
from pathlib import Path
import math
from functools import lru_cache

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

from SpriteIslands import components, grid_sprites


SOURCE = Path(__file__).resolve().parent / 'sources'
FULL_W, FULL_H = 448, 480
FACE_SCALE, FACE_TOP, CONTACT_Y = 1.24, 16, 380
VIEWS = ('front', 'quarter', 'side')
# The anchor follows the visible lower jaw through the thirteen drawn yaw angles.
PIVOTS = {'front': .5, 'eighth': .625, 'quarter': .75, 'three-eighth': .82, 'side': .89}
TURN_VIEWS = ('front', 'yaw-075', 'yaw-150', 'eighth', 'yaw-300', 'yaw-375', 'quarter',
              'yaw-525', 'yaw-600', 'three-eighth', 'yaw-750', 'yaw-825', 'side')
for index, view in enumerate(TURN_VIEWS):
    if view not in PIVOTS:
        a, b, amount = ((.5, .625, index / 3) if index < 3 else
                        (.625, .75, (index - 3) / 3) if index < 6 else
                        (.75, .82, (index - 6) / 3) if index < 9 else
                        (.82, .89, (index - 9) / 3))
        PIVOTS[view] = a + (b - a) * amount
BANDS = {'eyes': (0, 252), 'mouth': (210, 356), 'jaw': (314, FULL_H)}


def _normalized(tile, scale=FACE_SCALE):
    size = round(tile.width * scale), round(tile.height * scale)
    image = tile.resize(size, Image.Resampling.NEAREST)
    if size[0] > FULL_W or FACE_TOP + size[1] > FULL_H:
        raise ValueError(f'White Visitor frame exceeds its shared canvas: {size}')
    result = Image.new('RGBA', (FULL_W, FULL_H))
    result.alpha_composite(image, ((FULL_W - image.width) // 2, FACE_TOP))
    return result


def _faces():
    eyes = grid_sprites(Image.open(SOURCE / 'white-face-eyes-empty.png').convert('RGBA'), 4, 3)
    angled_eyes = grid_sprites(Image.open(SOURCE / 'white-face-eyes.png').convert('RGBA'), 4, 3)
    mouths = grid_sprites(Image.open(SOURCE / 'white-face-mouths.png').convert('RGBA'), 4, 3)
    result = {}
    for row, view in enumerate(VIEWS):
        for frame in range(4):
            result[view, 'eyes', frame] = _normalized((eyes if row == 0 else angled_eyes)[row * 4 + frame])
            result[view, 'mouth', frame] = _normalized(mouths[row * 4 + frame])
            result[view, 'jaw', frame] = result[view, 'mouth', frame]
    phase = grid_sprites(Image.open(SOURCE / 'white-face-phase-mouths.png').convert('RGBA'), 4, 3)
    for row, view in enumerate(VIEWS):
        for frame in range(4):
            result[view, 'phase-mouth', frame] = _normalized(phase[row * 4 + frame])
            result[view, 'phase-jaw', frame] = result[view, 'phase-mouth', frame]
            # Keep the torn cheek silhouette from this same pose. Transfer only the generated eye sockets.
            face = result[view, 'phase-mouth', frame].copy()
            opened = result[view, 'eyes', 3].copy()
            if view == 'front':
                for pupil, x in zip(_anatomy()[1], (154, 296)):
                    _fixed_iris(opened, pupil, x, 188, 18, 14)
            x0, x1 = ((112, 340) if view == 'front' else (224, 438) if view == 'quarter' else (314, 447))
            y0, y1 = 144, 212
            pixels = np.asarray(opened).copy()
            xx, yy = np.meshgrid(np.arange(FULL_W), np.arange(FULL_H))
            feather = np.minimum(np.minimum((xx - x0) / 12, (x1 - xx) / 12),
                                 np.minimum((yy - y0) / 10, (y1 - yy) / 10))
            pixels[:, :, 3] = (pixels[:, :, 3] * np.clip(feather, 0, 1)).astype(np.uint8)
            face.alpha_composite(Image.fromarray(pixels))
            result[view, 'phase-eyes', frame] = face
    turn = grid_sprites(Image.open(SOURCE / 'white-face-turn.png').convert('RGBA'), 5, 1)
    turn_scale = eyes[0].height * FACE_SCALE / turn[0].height
    pupils = _anatomy()[1]
    for index, view in ((1, 'eighth'), (3, 'three-eighth')):
        face = _normalized(turn[index], turn_scale)
        contacts = ((180, 179, 18, 14), (328, 179, 13, 13)) if index == 1 else ((278, 170, 18, 14), (378, 168, 7, 10))
        for pupil, (x, y, width, height) in zip(pupils, contacts):
            _fixed_iris(face, pupil, x, y, width, height)
        result[view, 'eyes', 3] = face
        result[view, 'mouth', 0] = face
        result[view, 'jaw', 0] = face
    yaw = grid_sprites(Image.open(SOURCE / 'white-face-yaw.png').convert('RGBA'), 4, 2)
    for tile, view in zip(yaw, (TURN_VIEWS[i] for i in (1, 2, 4, 5, 7, 8, 10, 11))):
        face = _normalized(tile, eyes[0].height * FACE_SCALE / tile.height)
        result[view, 'eyes', 3] = face
        result[view, 'mouth', 0] = face
        result[view, 'jaw', 0] = face
    phase_yaw = grid_sprites(Image.open(SOURCE / 'white-face-phase-yaw.png').convert('RGBA'), 5, 2)
    for tile, view in zip(phase_yaw, (TURN_VIEWS[i] for i in (1, 2, 3, 4, 5, 7, 8, 9, 10, 11))):
        face = _normalized(tile, eyes[0].height * FACE_SCALE / tile.height)
        result[view, 'phase-mouth', 2] = face
        result[view, 'phase-jaw', 2] = face
        result[view, 'phase-eyes', 2] = face
    # Extract the lower-head contour from the attached short neck; the independent long neck lives behind the deck.
    for (view, part, frame), face in list(result.items()):
        result[view, part, frame] = _head_only(face, view, part.startswith('phase-'), frame if part != 'eyes' else 0)
    return result


def _head_only(face, view, phase, frame):
    front = np.array(((65, 280), (90, 330), (130, 355), (185, 378), (265, 378), (320, 355), (355, 330), (380, 280)))
    quarter = np.array(((95, 260), (127, 308), (175, 339), (235, 361), (300, 382), (366, 390), (410, 385), (435, 300)))
    side = np.array(((170, 265), (205, 310), (245, 332), (290, 355), (350, 373), (408, 380), (430, 362), (444, 310)))
    angle = TURN_VIEWS.index(view) / 6
    a, b, t = (front, quarter, angle) if angle <= 1 else (quarter, side, angle - 1)
    edge = a + (b - a) * t
    drop = ((0, 15, 52, 0) if phase else (0, 12, 44, 0))[frame]
    edge[:, 1] += drop
    limit = np.interp(np.arange(FULL_W), edge[:, 0], edge[:, 1])
    # One pixel edge blending preserves source colors while separating connected anatomical silhouettes.
    mask = np.clip(limit[None, :] - np.arange(FULL_H)[:, None] + 1, 0, 1)
    pixels = np.asarray(face).copy(); pixels[:, :, 3] = (pixels[:, :, 3] * mask).astype(np.uint8)
    return Image.fromarray(pixels)


def _piece(face, part):
    top, bottom = BANDS[part]
    image = face.crop((0, top, FULL_W, bottom))
    if part != 'jaw':
        # The upper band fades over a fully covered lower band, preserving total opacity.
        fade_start = BANDS['mouth'][0] if part == 'eyes' else BANDS['jaw'][0]
        alpha = np.asarray(image.getchannel('A')).copy()
        amount = np.clip((bottom - 1 - np.arange(image.height) - top) / (bottom - 1 - fade_start), 0, 1)
        alpha = (alpha * amount[:, None]).astype(np.uint8)
        image.putalpha(Image.fromarray(alpha))
    return image, (0, (FULL_H - bottom) / 32)


def _island(image, island):
    _, box, runs = island
    left, top = max(0, box[0] - 3), max(0, box[1] - 3)
    right, bottom = min(image.width, box[2] + 3), min(image.height, box[3] + 3)
    mask = Image.new('L', image.size)
    draw = ImageDraw.Draw(mask)
    for y, x0, x1 in runs:
        draw.line((x0, y, x1 - 1, y), fill=255)
    mask = mask.filter(ImageFilter.MaxFilter(3))
    pixels = np.asarray(image).copy()
    pixels[:, :, 3] = np.where(np.asarray(mask) > 0, pixels[:, :, 3], 0)
    return Image.fromarray(pixels).crop((left, top, right, bottom))


@lru_cache(maxsize=1)
def _anatomy():
    image = Image.open(SOURCE / 'white-anatomy.png').convert('RGBA')
    islands = sorted((part for part in components(image) if part[0] >= 1000), reverse=True, key=lambda part: part[0])
    if len(islands) < 3:
        raise ValueError('The generated neck and both pupils must be separate silhouettes')
    neck = _island(image, islands[0])
    pupils = sorted(islands[1:3], key=lambda part: part[1][0])
    neck = neck.resize((FULL_W, round(neck.height * FULL_W / neck.width)), Image.Resampling.NEAREST)
    return neck, [_island(image, part).resize((18, 14), Image.Resampling.NEAREST) for part in pupils]


def _fixed_iris(face, pupil, x, y, width, height):
    """Project the generated iris into the measured sclera of the intermediate drawn view."""
    iris = Image.new('RGBA', face.size)
    iris.alpha_composite(pupil.resize((width, height), Image.Resampling.NEAREST), (x - width // 2, y - height // 2))
    source, pixels = np.asarray(face), np.asarray(iris).copy()
    aperture = (source[:, :, 0] > 145) & (source[:, :, 1] > 155) & (source[:, :, 2] > 155)
    pixels[:, :, 3] = np.where(aperture, pixels[:, :, 3], 0)
    face.alpha_composite(Image.fromarray(pixels))


def _eye_masks(faces):
    # Regions lie inside the generated socket rims. Color selection follows the actual sclera.
    regions = ((126, 175, 184, 198), (267, 175, 324, 198))
    masks = {}
    for eye, region in enumerate(regions):
        base = np.asarray(faces['front', 'eyes', 3])
        x0, y0, x1, y1 = region
        full = np.zeros((FULL_H, FULL_W), dtype=np.uint8)
        light = (base[y0:y1, x0:x1, 0] > 145) & (base[y0:y1, x0:x1, 1] > 155) & (base[y0:y1, x0:x1, 2] > 155)
        full[y0:y1, x0:x1] = light * 255
        for frame in range(1, 4):
            pose = np.asarray(faces['front', 'eyes', frame])
            aperture = (pose[:, :, 0] > 145) & (pose[:, :, 1] > 155) & (pose[:, :, 2] > 155)
            alpha = np.where(aperture, full, 0).astype(np.uint8)
            image = Image.new('RGBA', (FULL_W, FULL_H), (255, 255, 255, 0))
            image.putalpha(Image.fromarray(alpha))
            if frame == 3 and np.count_nonzero(alpha) < 100:
                raise ValueError('White Visitor sclera mask needs its socket coordinates checked')
            masks[eye, frame] = image
    return masks


def _hands():
    source = Image.open(SOURCE / 'white-hands-cutout.png').convert('RGBA')
    tiles = grid_sprites(source, 3, 2)
    # Palm centres are measured on the complete silhouettes, ahead of wrist and finger extensions.
    centres = ((182, 235), (200, 225), (256, 189), (218, 247), (210, 236), (210, 215))
    width, height, scale = 352, 352, .57
    contact = (176, 200)
    states = ('idle.0', 'approach.0', 'attack.0', 'attack.1', 'recoil.0', 'death.0')
    result = {}
    for state, tile, centre in zip(states, tiles, centres):
        sprite = tile.resize((round(tile.width * scale), round(tile.height * scale)), Image.Resampling.NEAREST)
        offset = contact[0] - round(centre[0] * scale), contact[1] - round(centre[1] * scale)
        bounds = sprite.getbbox()
        if bounds is None or offset[0] + bounds[0] < 0 or offset[1] + bounds[1] < 0 or \
                offset[0] + bounds[2] > width or offset[1] + bounds[3] > height:
            raise ValueError(f'White hand {state} crosses its contact canvas')
        canvas = Image.new('RGBA', (width, height)); canvas.alpha_composite(sprite, offset)
        result[state] = canvas, (.5, (height - contact[1]) / height)
    return result


def collect(add, mounts):
    """Pass every generated frame to the shared packer; MY restores each vertical band's position."""
    faces = _faces()
    for (view, part, frame), face in faces.items():
        band = part.removeprefix('phase-')
        image, mount = _piece(face, band)
        bounds = image.getbbox()
        # Vertical padding carries no anatomy. Keep the full logical width and restore the measured bottom through MY.
        image = image.crop((0, bounds[1], FULL_W, bounds[3]))
        mount = (0, (FULL_H - BANDS[band][0] - bounds[3]) / 32)
        key = f'deepsea.white-face.{view}.{part}.{frame}'
        add(key, image, (PIVOTS[view], 0)); mounts[key] = mount
    neck, pupils = _anatomy()
    add('deepsea.white-neck.0', neck, (.5, 1))
    masks = _eye_masks(faces)
    icon = faces['front', 'eyes', 3].copy()
    for i, x in enumerate((154, 296)):
        iris = Image.new('RGBA', (FULL_W, FULL_H)); iris.alpha_composite(pupils[i], (x - 9, 181))
        pixels = np.asarray(iris).copy()
        pixels[:, :, 3] = np.minimum(pixels[:, :, 3], np.asarray(masks[i, 3].getchannel('A')))
        icon.alpha_composite(Image.fromarray(pixels))
    add('deepsea.white-visitor.idle.0', icon.resize((96, round(FULL_H * 96 / FULL_W)), Image.Resampling.NEAREST))
    for i, side in enumerate(('left', 'right')):
        pupil = Image.new('RGBA', (FULL_W, 36))
        pupil.alpha_composite(pupils[i], ((154 if i == 0 else 296) - 9, 10))
        key = f'deepsea.white-pupil.{side}'
        add(key, pupil); mounts[key] = (0, (FULL_H - 207) / 32)
        for frame in range(1, 4):
            key = f'deepsea.white-eye-mask.{side}.{frame}'
            add(key, masks[i, frame].crop((0, 170, FULL_W, 207)))
            mounts[key] = (0, (FULL_H - 207) / 32)
        key = f'deepsea.white-eye-mask.{side}'
        add(key, masks[i, 3].crop((0, 170, FULL_W, 207)))
        mounts[key] = (0, (FULL_H - 207) / 32)
    for state, (hand, pivot) in _hands().items():
        add('deepsea.white-hand.' + state, hand, pivot)


def _contact_sheet(items, columns, name):
    cell_w, cell_h = max(image.width for _, image in items), max(image.height for _, image in items)
    rows = math.ceil(len(items) / columns)
    result = Image.new('RGBA', (columns * (cell_w + 12), rows * (cell_h + 28)), (14, 30, 46, 255))
    draw = ImageDraw.Draw(result)
    for index, (label, image) in enumerate(items):
        x, y = index % columns * (cell_w + 12) + 6, index // columns * (cell_h + 28) + 23
        result.alpha_composite(image, (x, y)); draw.text((x, y - 18), label, fill=(218, 231, 240, 255))
    result.save(SOURCE / name)


if __name__ == '__main__':
    faces = _faces(); masks = _eye_masks(faces); _, pupils = _anatomy()
    # Draw authored head poses at their production canvas size before compositor tinting.
    _contact_sheet([(view + ': mouth ' + str(frame), faces[view, 'mouth', frame])
                    for view in VIEWS for frame in range(4)], 4, 'white-mouth-preview.png')
    _contact_sheet([(view + ': phase ' + str(frame), faces[view, 'phase-mouth', frame])
                    for view in VIEWS for frame in range(4)], 4, 'white-phase-preview.png')
    turn_frames = []
    for view in TURN_VIEWS:
        face = faces[view, 'eyes', 3].copy()
        if view == 'front':
            for pupil, x in zip(pupils, (154, 296)):
                _fixed_iris(face, pupil, x, 188, 18, 14)
        turn_frames.append((view, face))
    _contact_sheet(turn_frames, 7, 'white-turn-preview.png')
    _contact_sheet([(state, frame) for state, (frame, _) in _hands().items()], 3, 'white-hand-preview.png')
    frames = []
    for i in range(80):
        age = i / 20
        eye_frame = min(3, int(max(0, (age - 2.2) / .6) * 4))
        canvas = Image.new('RGBA', (576, 576), (8, 24, 41, 255))
        head = Image.new('RGBA', (FULL_W, FULL_H))
        for part, lamp in (('jaw', .8), ('mouth', 1.25), ('eyes', 1.7)):
            frame = eye_frame if part == 'eyes' else 0
            piece, _ = _piece(faces['front', part, frame], part)
            amount = 0
            for source in (.8, 1.25, 1.7):
                value = max(0, min(1, (age - source) / .25)); value = value * value * (3 - 2 * value)
                distance = abs(source - lamp)
                spill = 1 if distance < .01 else .55 if distance < .46 else .18
                amount = max(amount, value * spill)
            light = .15 + .5 * amount
            pixels = np.asarray(piece).copy(); pixels[:, :, :3] = (pixels[:, :, :3] * light).astype(np.uint8)
            head.alpha_composite(Image.fromarray(pixels), (0, BANDS[part][0]))
        if eye_frame > 0:
            for eye, x in enumerate((154, 296)):
                iris = Image.new('RGBA', (FULL_W, FULL_H))
                shift = round(min(1, max(0, (age - 2.2) / .5)) * 2)
                iris.alpha_composite(pupils[eye], (x - 9 - shift, 181))
                a = np.asarray(iris).copy()
                a[:, :, 3] = np.minimum(a[:, :, 3], np.asarray(masks[eye, eye_frame].getchannel('A')))
                head.alpha_composite(Image.fromarray(a))
        close = max(0, min(1, (age - 2.2) / 1.4)); close = close * close * (3 - 2 * close)
        size = round(FULL_W * (1 + .03 * close)), round(FULL_H * (1 + .03 * close))
        head = head.resize(size, Image.Resampling.NEAREST)
        canvas.alpha_composite(head, ((576 - head.width) // 2, 40 + round(close * 9)))
        frames.append(canvas.convert('RGB'))
    frames[0].save(SOURCE / 'white-intro-preview.gif', save_all=True, append_images=frames[1:], duration=50, loop=0)
    output, mounts = {}, {}
    collect(lambda key, image, pivot=(.5, 0): output.__setitem__(key, image), mounts)
    print(f'White Visitor: {len(output)} resources; full face canvas {FULL_W}x{FULL_H}; 13 drawn yaw views; 4 eye and mouth poses per contact view; 6 hand poses')
    print(f'Jaw contact: ({FULL_W / 2}, {CONTACT_Y}), full-canvas bottom offset {(FULL_H - CONTACT_Y) / 32:.4f} source units')
