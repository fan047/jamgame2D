"""Process the two late-stage cave themes from the archived full-resolution art.

Input  : Docs/中期版/后六关矿洞素材原图/背景图{2,3}.png, 方块图{2,3}.png
Output : Assets/Holedown/Art/Theme/ (backgrounds, foregrounds, nine block sprites each)
         Library/Locus/tmp/late-stage/ (before/after pairs, foreground overlay, shape sheets)

Background
    The continuous bright frame, the top plaque and the baked gold launcher ring are
    masked and inpainted from their surroundings, then the outer rim is settled into the
    cave's own dark tone so no bright line survives. Crystal and rock silhouettes keep
    their pixels. Composition, canvas (941x1672) and hue stay unchanged; only the bright
    facets of the top crystals and of the bright bottom crystal cluster are pulled down so
    HUD numbers and the recall area stay readable.

Foreground
    Real rock and crystal pixels are copied in place from the processed background of the
    same group; every decoration-free pixel keeps alpha 0. Sorting stays at build 22's 10.

Blocks
    Nine sprites per group: ordinary / reward / target in square, wide and tall.
    Reward is unified as orange gold, target as teal green; the sheet's sample digits and
    embedded gem badges are removed and the face is repaired before the shapes are derived.

Run without --apply for previews only.
"""
from pathlib import Path
import sys
import numpy as np
import cv2
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / 'Docs/中期版/后六关矿洞素材原图'
THEME = ROOT / 'Assets/Holedown/Art/Theme'
SCRATCH = ROOT / 'Library/Locus/tmp/late-stage'
GROUPS = {2: 'stages4-6', 3: 'stages7-9'}
PLAQUE_RECT = (325, 620, 0, 130)      # x0, x1, y0, y1 of the plaque plate and its ring
BAND_MARGIN = 5                       # extra rows/columns around the detected frame
RIM_FADE = 60
DIM_TOP_BELOW = 360
DIM_BOTTOM_FROM = 1340


def frame_colors(rgb):
    r, g, b = rgb[:, :, 0].astype(int), rgb[:, :, 1].astype(int), rgb[:, :, 2].astype(int)
    lavender = (b > 120) & (b - r > 55) & (g >= r - 12) & (r < 205)
    gold = (r > 120) & (g > 55) & (b < 130) & (r - b > 80) & (r - g > 25)
    return lavender | gold


def detect_bands(rgb):
    """Bright frame runs measured from each edge so the mask follows the actual art."""
    lum = rgb.sum(axis=2)
    h, w = lum.shape
    def run_length(strip):
        bright = strip > 250
        endings = []
        for line in bright:
            start = int(np.argmax(line))
            if not line[start]:
                continue
            end = start
            while end + 1 < len(line) and line[end + 1]:
                end += 1
            endings.append(end + 1)
        # The frame is the run that starts nearest the edge; crystals below it add
        # longer runs on some columns, so take the robust lower band.
        return int(np.percentile(endings, 25)) if endings else 0
    left = run_length(lum[:, :80])
    right = run_length(lum[:, ::-1][:, :80])
    top = run_length(lum[:100, :].T)
    bottom = run_length(lum[::-1][:100, :].T)
    return left, right, top, bottom


def frame_mask(rgb):
    h, w = rgb.shape[:2]
    mask = np.zeros((h, w), np.uint8)
    left, right, top, bottom = detect_bands(rgb)
    left += BAND_MARGIN; right += BAND_MARGIN; top += BAND_MARGIN; bottom += BAND_MARGIN
    mask[:, :left] = 255
    mask[:, w - right:] = 255
    mask[:top, :] = 255
    mask[h - bottom:, :] = 255
    # The frame turns with a rounded corner; take the whole arc instead of leaving a rim.
    corner = max(left, right, top, bottom) + 10
    for cy, cx in ((0, 0), (0, w - corner), (h - corner, 0), (h - corner, w - corner)):
        mask[cy:cy + corner, cx:cx + corner] = 255
    px0, px1, py0, py1 = PLAQUE_RECT
    # Inside the plaque rectangle the cave is flat and dark, so anything bright belongs to
    # the plate or its ring, including the gold lip and its antialiased outline.
    lum = rgb.sum(axis=2)
    plaque = np.zeros((h, w), np.uint8)
    plaque[py0:py1, px0:px1] = (lum[py0:py1, px0:px1] > 120).astype(np.uint8) * 255
    # Grow a little so the ring's dark outline and the plate's antialiased edge go too,
    # while crystal pixels inside the rectangle survive.
    plaque = cv2.dilate(plaque, np.ones((9, 9), np.uint8))
    mask |= plaque
    return mask


def extend_inwards(mask, rgb):
    """Fill each masked pixel from the nearest content pixel further away from its edge."""
    out = rgb.copy()
    h, w, _ = rgb.shape
    ys, xs = np.where(mask > 0)
    for y, x in zip(ys, xs):
        dl, dr, dt, db = x, w - 1 - x, y, h - 1 - y
        near = min(dl, dr, dt, db)
        step = (0, 1) if near == dl else (0, -1) if near == dr else (1, 0) if near == dt else (-1, 0)
        cy, cx = y, x
        for _ in range(max(h, w)):
            cy += step[0]; cx += step[1]
            if not (0 <= cy < h and 0 <= cx < w):
                break
            if not mask[cy, cx]:
                out[y, x] = rgb[cy, cx]
                break
    return out


def snap_to_palette(rgb, mask, radius=14, keep=256):
    """Keep the pixel-art look: filled pixels take the nearest colour the art already uses."""
    ring = cv2.dilate(mask, np.ones((radius * 2 + 1, radius * 2 + 1), np.uint8)) & ~mask
    colors, counts = np.unique(rgb[ring > 0].reshape(-1, 3), axis=0, return_counts=True)
    palette = colors[np.argsort(-counts)[:keep]].astype(np.int16)
    ys, xs = np.where(mask > 0)
    out = rgb.copy()
    for start in range(0, len(ys), 20000):
        block = rgb[ys[start:start + 20000], xs[start:start + 20000]].astype(np.int16)
        distance = ((block[:, None, :] - palette[None, :, :]) ** 2).sum(axis=2)
        out[ys[start:start + 20000], xs[start:start + 20000]] = palette[np.argmin(distance, axis=1)].astype(np.uint8)
    return out


def settle_rim(rgb, mask):
    """Bring the former frame band to the cave void tone so no rim reads as a border."""
    h, w = rgb.shape[:2]
    ys = np.concatenate([np.arange(0, 10), np.arange(h - 10, h)])
    xs = np.concatenate([np.arange(0, 10), np.arange(w - 10, w)])
    outer = np.median(np.concatenate([rgb[ys, :, :].reshape(-1, 3), rgb[:, xs, :].reshape(-1, 3)]), axis=0)
    fy = np.arange(h)[:, None].astype(float)
    fx = np.arange(w)[None, :].astype(float)
    edge = np.minimum(np.minimum(fx, (w - 1) - fx), np.minimum(fy, (h - 1) - fy))
    # The top strip is settled separately (see flatten_top) so one tone covers the plaque,
    # the former frame and the cave air below it.
    band = (edge < RIM_FADE) & (mask > 0) & (fy > PLAQUE_RECT[3] + 20)
    settled = rgb.astype(float)
    settled[band] = settled[band] * 0.18 + outer * 0.82
    return np.clip(settled, 0, 255)


def dim_highlights(rgb, top_strength=0.32, bottom_strength=0.16):
    """Pull the bright top and bottom crystal facets down; shadows keep their value."""
    out = rgb.astype(float)
    lum = out.sum(axis=2) / 3.0
    weight = np.zeros_like(lum)
    weight[:DIM_TOP_BELOW] = top_strength
    weight[DIM_BOTTOM_FROM:] = bottom_strength
    # Bright facets only, and more the brighter they are.
    ramp = np.clip((lum - 80.0) / 70.0, 0.0, 1.0)
    out *= (1.0 - weight * ramp)[:, :, None]
    return np.clip(out, 0, 255).astype(np.uint8)


def cave_tone(rgb):
    """Colour of the empty play field, measured away from every decoration."""
    band = rgb[560:900, 300:640].reshape(-1, 3)
    return np.median(band, axis=0)


def flatten_top(rgb, mask):
    """Fill the former top frame and plaque with the empty cave tone.

    One measured cave colour keeps the repaired strip identical to the air around it, so no
    rectangle, bar, seam or invented facet can read as a frame, and every real rock and
    crystal below the strip keeps its own drawn outline.
    """
    px0, px1, py0, py1 = PLAQUE_RECT
    top = py0 + 60
    cave = cave_tone(rgb)
    out = rgb.copy()
    for (x0, x1, y0, y1) in ((0, rgb.shape[1], 0, top), (px0, px1, 0, py1)):
        sub = mask[y0:y1, x0:x1] > 0
        target = out[y0:y1, x0:x1]
        target[sub] = cave
        out[y0:y1, x0:x1] = target
    return out


def process_background(tag, apply):
    rgb = np.array(Image.open(SRC / f'背景图{tag}.png').convert('RGB'))
    mask = frame_mask(rgb)
    filled = cv2.inpaint(rgb, mask, 6, cv2.INPAINT_TELEA)
    settled = snap_to_palette(settle_rim(filled, mask).astype(np.uint8), mask)
    settled = flatten_top(settled, mask)
    processed = dim_highlights(np.clip(settled, 0, 255).astype(np.uint8))
    image = Image.fromarray(processed)
    SCRATCH.mkdir(parents=True, exist_ok=True)
    image.save(SCRATCH / f'bg{tag}-after-full.png')
    before = Image.fromarray(rgb)
    pair = Image.new('RGB', (941 * 2 + 8, 1672), '#ff00ff')
    pair.paste(before, (0, 0)); pair.paste(image, (949, 0))
    pair.resize((941, 836), Image.Resampling.LANCZOS).save(SCRATCH / f'bg{tag}-before-after.png')
    image.resize((470, 836), Image.Resampling.LANCZOS).save(SCRATCH / f'bg{tag}-after.png')
    for name, box in {'top': (0, 0, 941, 160), 'bottom': (0, 1512, 941, 1672),
                      'left': (0, 560, 140, 1440), 'right': (801, 560, 941, 1440)}.items():
        src, dst = before.crop(box), image.crop(box)
        side = Image.new('RGB', ((box[2] - box[0]) * 2 + 8, box[3] - box[1]), '#ff00ff')
        side.paste(src, (0, 0)); side.paste(dst, (box[2] - box[0] + 8, 0))
        side.save(SCRATCH / f'bg{tag}-edge-{name}.png')
    print({'group': GROUPS[tag], 'bands': detect_bands(rgb), 'masked': int((mask > 0).sum())})
    if apply:
        image.save(THEME / f'background_{GROUPS[tag]}.png')
    return processed




BLOCK_BOXES = {'square': (1569, 145, 330, 330), 'wide': (2049, 145, 550, 330), 'tall': (2709, 145, 340, 550)}
BLOCK_CANVAS = {'square': (338, 338), 'wide': (550, 340), 'tall': (340, 555)}
BLOCK_CROP = {'square': (330, 330), 'wide': (550, 330), 'tall': (340, 550)}
# Sheet positions 6 and 10 hold the role templates (target and reward). Each group is cut
# from its own sheet; the printed sample number and the embedded gem badge are removed and
# the face is repaired locally before the shapes are spliced out of that same wide block.
SPECIAL_BOXES = {'target': (2359, 875, 550, 330), 'reward': (2369, 1325, 540, 330)}
# Measured content rectangles (sample number and embedded gem badge) inside those two
# templates. The two sheets share the same layout, so one pair of rectangles serves both.
SPECIAL_CONTENT = {'target': (85, 474, 66, 272), 'reward': (84, 470, 64, 248)}
# Role ramps follow the first theme's teal target and orange-gold reward, from the
# lightest facet to the darkest edge, so all three groups read as one series.
ROLE_RAMPS = {
    'target': [(150, 231, 207), (89, 204, 183), (45, 163, 157), (37, 105, 120), (30, 66, 81)],
    'reward': [(250, 220, 155), (237, 190, 105), (215, 155, 66), (162, 106, 49), (112, 76, 44)],
}
SPLICE_CAP = 56
DECORATION_BRIGHTNESS = 80
# Bare cave-wall veins sit just above the cave: their median facet is far dimmer than any
# real rock or crystal, so components under this median are wall texture, not decoration.
DECORATION_MEDIAN = 120
MIN_DECORATION_AREA = 200
MAX_INNER_POCKET = 30000


def dominant_tones(rgba, count=5):
    solid = rgba[rgba[:, :, 3] > 200][:, :3]
    colors, counts = np.unique(solid, axis=0, return_counts=True)
    order = np.argsort(-counts)[:count]
    tones = colors[order].astype(np.int16)
    luminance = tones.sum(axis=1)
    return tones[np.argsort(-luminance)]


def recolor(rgba, ramp):
    """Map the source tones onto a role ramp by rank, keeping alpha and facet shapes."""
    out = rgba.copy()
    tones = dominant_tones(rgba)
    ramp = np.array(ramp, np.int16)[:len(tones)]
    solid = out[:, :, 3] > 0
    pixels = out[solid][:, :3].astype(np.int16)
    distance = ((pixels[:, None, :] - tones[None, :, :]) ** 2).sum(axis=2)
    rank = np.argmin(distance, axis=1)
    replaced = out[solid]
    replaced[:, :3] = ramp[rank]
    out[solid] = replaced
    return out


def sheet_piece(sheet, box):
    x, y, w, h = box
    piece = sheet[y:y + h, x:x + w].copy()
    piece[piece[:, :, 3] < 8] = 0
    return piece


def clean_special(piece, role):
    """Remove the printed number and the embedded gem badge, then repair the face.

    The rectangle that holds both sits inside the flat face, so the face rows directly
    below it are repeated over it: the material stays continuous, while the baked bevel,
    the edge shadow and the silhouette are left untouched.
    """
    x0, x1, y0, y1 = SPECIAL_CONTENT[role]
    x1 = min(x1, piece.shape[1] - 1)
    y1 = min(y1, piece.shape[0] - 1)
    alpha = piece[:, :, 3]
    tones, counts = np.unique(piece[alpha > 200][:, :3], axis=0, return_counts=True)
    face = tones[np.argmax(counts)]
    out = piece.copy()
    out[y0:y1 + 1, x0:x1 + 1, :3] = face[None, None, :]
    return out, face


def splice(rgba, target_w, target_h, cap=SPLICE_CAP):
    """Nine-slice the wide block: keep the bevel caps, stretch the flat facets between."""
    h, w = rgba.shape[:2]
    cols = np.concatenate([np.arange(0, cap),
                           np.round(np.linspace(cap, w - cap - 1, target_w - 2 * cap)).astype(int),
                           np.arange(w - cap, w)])
    rows = np.concatenate([np.arange(0, cap),
                           np.round(np.linspace(cap, h - cap - 1, target_h - 2 * cap)).astype(int),
                           np.arange(h - cap, h)])
    return rgba[rows[:, None], cols[None, :]]


def place(piece, canvas_wh):
    canvas_w, canvas_h = canvas_wh
    h, w = piece.shape[:2]
    canvas = np.zeros((canvas_h, canvas_w, 4), np.uint8)
    ox, oy = (canvas_w - w) // 2, (canvas_h - h) // 2
    sx, sy = max(0, -ox), max(0, -oy)
    dx, dy = max(0, ox), max(0, oy)
    cw, ch = min(w - sx, canvas_w - dx), min(h - sy, canvas_h - dy)
    canvas[dy:dy + ch, dx:dx + cw] = piece[sy:sy + ch, sx:sx + cw]
    return canvas


def extract_blocks(tag, apply):
    sheet = np.array(Image.open(SRC / f'方块图{tag}.png').convert('RGBA'))
    group = GROUPS[tag]
    sprites = {}
    for shape, box in BLOCK_BOXES.items():
        sprites[('ordinary', shape)] = place(sheet_piece(sheet, box), BLOCK_CANVAS[shape])
    for role, box in SPECIAL_BOXES.items():
        cleaned, face = clean_special(sheet_piece(sheet, box), role)
        for shape, (cw, ch) in BLOCK_CROP.items():
            spliced = splice(cleaned, cw, ch)
            coloured = recolor(spliced, ROLE_RAMPS[role])
            sprites[(role, shape)] = place(coloured, BLOCK_CANVAS[shape])
        Image.fromarray(cleaned).save(SCRATCH / f'cleaned{tag}-{role}-wide.png')
        print({'group': group, 'role': role, 'source_box': box, 'content_rect': SPECIAL_CONTENT[role]})
    order = [('ordinary', 'square'), ('ordinary', 'wide'), ('ordinary', 'tall'),
             ('reward', 'square'), ('reward', 'wide'), ('reward', 'tall'),
             ('target', 'square'), ('target', 'wide'), ('target', 'tall')]
    for name, backdrop in (('light', (238, 229, 213)), ('dark', (18, 24, 42))):
        cell = 300
        sheet_img = Image.new('RGBA', (cell * 3 + 16, cell * 3 + 16), backdrop + (255,))
        for index, key in enumerate(order):
            piece = Image.fromarray(sprites[key])
            piece.thumbnail((cell - 12, cell - 12), Image.LANCZOS)
            gx, gy = index % 3, index // 3
            sheet_img.alpha_composite(piece, (gx * cell + 8 + (cell - 12 - piece.width) // 2,
                                              gy * cell + 8 + (cell - 12 - piece.height) // 2))
        sheet_img.convert('RGB').save(SCRATCH / f'blocks{tag}-{name}.png')
    if apply:
        for (role, shape), canvas in sprites.items():
            Image.fromarray(canvas).save(THEME / f'block_{group}_{role}_{shape}.png')
    print({'group': group, 'sprites': len(sprites)})


def decoration_mask(rgb):
    brightness = rgb.max(axis=2)
    seed = (brightness >= DECORATION_BRIGHTNESS).astype(np.uint8)
    count, labels, stats, _ = cv2.connectedComponentsWithStats(seed, 8)
    mask = np.zeros_like(seed)
    for i in range(1, count):
        if stats[i, 4] < MIN_DECORATION_AREA:
            continue
        # Wall veins are thin and dim; rocks and crystals carry a bright facet body.
        if np.median(brightness[labels == i]) < DECORATION_MEDIAN:
            continue
        mask[labels == i] = 1
    h, w = mask.shape
    count, labels, stats, _ = cv2.connectedComponentsWithStats((mask == 0).astype(np.uint8), 4)
    filled = mask.copy()
    for i in range(1, count):
        x, y, cw, ch, area = stats[i]
        if area > MAX_INNER_POCKET or x == 0 or y == 0 or x + cw >= w or y + ch >= h:
            continue
        filled[labels == i] = 1
    return filled


def extract_foreground(tag, processed, apply):
    group = GROUPS[tag]
    mask = decoration_mask(processed)
    out = np.dstack([processed, np.where(mask > 0, 255, 0).astype(np.uint8)])
    out[out[:, :, 3] == 0] = 0
    image = Image.fromarray(out)
    assert image.size == (941, 1672)
    base = Image.fromarray(processed).convert('RGBA')
    over = base.copy(); over.alpha_composite(image)
    difference = np.abs(np.array(over.convert('RGB')).astype(int) - np.array(base.convert('RGB')).astype(int)).max(axis=2)
    assert int((difference > 0).sum()) == 0, 'foreground must reproduce the processed art exactly'
    image.save(SCRATCH / f'fg{tag}-full.png')
    panel = Image.new('RGBA', image.size, (18, 24, 42, 255)); panel.alpha_composite(image)
    panel.resize((470, 836), Image.Resampling.LANCZOS).save(SCRATCH / f'fg{tag}-preview.png')
    print({'group': group, 'foreground_pixels': int(mask.sum()), 'coverage_percent': round(float(mask.mean()) * 100, 2)})
    if apply:
        image.save(THEME / f'foreground_{group}.png')


def main():
    apply = '--apply' in sys.argv
    for tag in GROUPS:
        processed = process_background(tag, apply)
        extract_foreground(tag, processed, apply)
        extract_blocks(tag, apply)


if __name__ == '__main__':
    main()
