"""Find complete generated sprite silhouettes, including parts crossing nominal grid lines."""
import numpy as np
from PIL import Image, ImageDraw, ImageFilter


def components(image):
    alpha = np.asarray(image.getchannel('A'))
    parents, runs, previous = [], [], []

    def root(index):
        while parents[index] != index:
            parents[index] = parents[parents[index]]
            index = parents[index]
        return index

    for y, line in enumerate(alpha >= 96):
        changes = np.diff(np.pad(line.astype(np.int8), (1, 1)))
        starts, ends = np.flatnonzero(changes == 1), np.flatnonzero(changes == -1)
        current = []
        for left, right in zip(starts, ends):
            index = len(parents); parents.append(index)
            runs.append((y, int(left), int(right))); current.append(index)
            for earlier in previous:
                _, a, b = runs[earlier]
                if b < left: continue
                if a > right: break
                aroot, broot = root(index), root(earlier)
                if aroot != broot: parents[aroot] = broot
        previous = current
    grouped = {}
    for index, run in enumerate(runs): grouped.setdefault(root(index), []).append(run)
    result = []
    for group in grouped.values():
        area = sum(right - left for _, left, right in group)
        bounds = (min(left for _, left, _ in group), group[0][0], max(right for _, _, right in group), group[-1][0] + 1)
        result.append((area, bounds, group))
    return result


def grid_sprites(image, columns, rows):
    islands = components(image)
    cell_w, cell_h = image.width / columns, image.height / rows
    assigned = {}
    for island in islands:
        area, box, _ = island
        if area < 300: continue
        x = min(columns - 1, max(0, int((box[0] + box[2]) * .5 / cell_w)))
        y = min(rows - 1, max(0, int((box[1] + box[3]) * .5 / cell_h)))
        key = y, x
        if key not in assigned or area > assigned[key][0]: assigned[key] = island
    result = []
    for row in range(rows):
        for col in range(columns):
            if (row, col) not in assigned:
                raise ValueError(f'Missing sprite at row {row}, column {col}')
            primary = assigned[row, col]
            _, box, _ = primary
            selected = [primary]
            # Small disconnected fragments belong to the nearest intact silhouette.
            for island in islands:
                if island is primary or island[0] >= primary[0] * .4 or island[0] < 4: continue
                a = island[1]
                dx = max(box[0] - a[2], a[0] - box[2], 0)
                dy = max(box[1] - a[3], a[1] - box[3], 0)
                if dx * dx + dy * dy <= 64:
                    selected.append(island)
            bounds = (max(0, min(i[1][0] for i in selected) - 3), max(0, min(i[1][1] for i in selected) - 3),
                      min(image.width, max(i[1][2] for i in selected) + 3), min(image.height, max(i[1][3] for i in selected) + 3))
            mask = Image.new('L', image.size)
            draw = ImageDraw.Draw(mask)
            for _, _, group in selected:
                for y, left, right in group: draw.line((left, y, right - 1, y), fill=255)
            mask = mask.filter(ImageFilter.MaxFilter(3))
            pixels = np.asarray(image).copy()
            pixels[:, :, 3] = np.where(np.asarray(mask) > 0, pixels[:, :, 3], 0)
            result.append(Image.fromarray(pixels).crop(bounds))
    return result
