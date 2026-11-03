"""M3 deterministic local extraction/repair. No image model, network, or Unity API.

Only writes the 15 allowlisted PNGs plus scratch under Library/Locus/tmp/theme-m3.
Run: python Tools/CompleteThemeAssets.py [--check-only]
An existing output may be replaced ONLY if its hash matches our previous manifest.
All source rectangles are exclusive-right/bottom, in original source pixels.
"""
from pathlib import Path
import argparse
import hashlib
import json
import math
import cv2
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path('D:/Downloads/picture')
OUT = ROOT / 'Assets/Holedown/Art/Theme'
CHECK = ROOT / 'Library/Locus/tmp/theme-m3'
EXPECTED = {
    '背景图.png': '3230DEF8BAD19673AA71172AFA6E99D1372AFBC5D0C449BD43576CD03422FDE2',
    '方块图.jpg': '0001431ECDE1B1670C238621A9D66B2FB66B565A78C06BBEC8581D65240BA2E2',
}
NAMES = ['block_wide', 'block_tall'] + [f'block_{role}_{shape}' for role in ('reward', 'target') for shape in ('wide', 'square', 'tall')] + ['badge_reward', 'badge_target', 'crack_light', 'crack_heavy', 'title_logo', 'fragment', 'fx_spark']
PROTECTED = ['ball.png', 'block_square.png', 'launcher.png', 'background_game.png']
RECORDS = []


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def verify_sources():
    actual = {n: sha(SOURCE / n) for n in EXPECTED}
    assert actual == EXPECTED, f'Source hash mismatch: {actual}'
    return actual


def solid_mask(mask):
    """Largest connected subject, filled silhouette. Never used for cracks."""
    n, labels, stats, _ = cv2.connectedComponentsWithStats(mask.astype('uint8'), 8)
    assert n > 1, 'Empty subject'
    m = (labels == 1 + np.argmax(stats[1:, cv2.CC_STAT_AREA])).astype('uint8') * 255
    contours, _ = cv2.findContours(m, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    cv2.drawContours(m, contours, -1, 255, cv2.FILLED)
    return m


def cut(image, rect, kind='rock'):
    a = np.array(image.crop(rect).convert('RGB'))
    f = a.astype(float)
    if kind == 'rock':
        mask = np.linalg.norm(f - [31, 38, 56], axis=2) > 42
    elif kind == 'crystal':
        mask = (f[:, :, 1] > f[:, :, 0] * 1.30) & (f[:, :, 1] > 72) & (f[:, :, 2] > 65)
    elif kind == 'spark':
        mask = ((f[:, :, 0] > 205) & (f[:, :, 1] > 170) & (f[:, :, 0] - f[:, :, 2] > 24)) | (f.min(axis=2) > 234)
    elif kind == 'target':
        # Crystal facets share the exact block-face color: chroma-keying alone
        # tears out the left-middle facet. Use the reviewed pixel-stepped silhouette.
        # Absolute coordinates were traced against target-icon-zoom.png.
        points = [(2515, 948), (2524, 948), (2524, 957), (2533, 957),
                  (2533, 966), (2542, 966), (2542, 975), (2551, 975),
                  (2551, 984), (2560, 984), (2560, 993), (2569, 993),
                  (2569, 1002), (2578, 1002), (2578, 1011), (2587, 1011),
                  (2587, 1020), (2596, 1020), (2596, 1047), (2599, 1047),
                  (2599, 1065), (2590, 1065), (2590, 1083), (2581, 1083),
                  (2581, 1092), (2572, 1092), (2572, 1101), (2563, 1101),
                  (2563, 1110), (2554, 1110), (2554, 1119), (2545, 1119),
                  (2545, 1128), (2536, 1128), (2536, 1137), (2518, 1137),
                  (2518, 1128), (2509, 1128), (2509, 1119), (2500, 1119),
                  (2500, 1110), (2491, 1110), (2491, 1092), (2482, 1092),
                  (2482, 1083), (2473, 1083), (2473, 1065), (2464, 1065),
                  (2464, 1047), (2458, 1047), (2458, 1029), (2467, 1029),
                  (2467, 1011), (2476, 1011), (2476, 993), (2485, 993),
                  (2485, 984), (2494, 984), (2494, 966), (2503, 966),
                  (2503, 957), (2515, 957)]
        m = Image.new('L', (a.shape[1], a.shape[0]))
        ImageDraw.Draw(m).polygon([(x-rect[0], y-rect[1]) for x, y in points], fill=255)
        mask = np.array(m) > 0
    else:
        # Interior reward icon against its measured flat block face.
        bg = np.array([231, 161, 64])
        mask = np.linalg.norm(f - bg, axis=2) > 33
    alpha = solid_mask(mask)
    if kind == 'reward':
        # Remove the one-pixel orange JPEG fringe without blurring the pixel steps.
        alpha = cv2.erode(alpha, np.ones((3, 3), dtype='uint8'), iterations=1)
    if kind == 'spark':
        # The lower rays were flattened onto a grey block in the JPEG. Retain
        # their silhouette, but remove inherited grey/tan backing from the core.
        white = a.min(axis=2) > 234
        a[alpha > 0] = [255, 250, 185]
        a[(alpha > 0) & white] = [255, 255, 240]
    a[alpha == 0] = 0
    im = Image.fromarray(np.dstack([a, alpha]))
    return im.crop(im.getbbox())


def pad(image, margin=3):
    result = Image.new('RGBA', (image.width + margin * 2, image.height + margin * 2))
    result.alpha_composite(image, (margin, margin))
    return result


def nine_slice(image, size, border=80):
    """Preserve corners and edge thickness; resample only straight spans. No rotation."""
    w, h = image.size
    dw, dh = size
    result = Image.new('RGBA', size)
    xs, ys = [0, border, w-border, w], [0, border, h-border, h]
    xd, yd = [0, border, dw-border, dw], [0, border, dh-border, dh]
    for j in range(3):
        for i in range(3):
            tile = image.crop((xs[i], ys[j], xs[i+1], ys[j+1]))
            tile = tile.resize((xd[i+1]-xd[i], yd[j+1]-yd[j]), Image.Resampling.NEAREST)
            result.paste(tile, (xd[i], yd[j]))
    return result


def repair_color(board, role):
    rect = (2350, 1310, 2920, 1670) if role == 'reward' else (2350, 860, 2920, 1225)
    raw = board.crop(rect).convert('RGB')
    a = np.array(raw)
    # Both symbols and JPEG halos lie inside these local rectangles, not the bevel.
    boxes = [(114, 79, 285, 251), (305, 76, 488, 252)] if role == 'reward' else [(94, 72, 254, 285), (302, 73, 453, 270)]
    # Per-row median from the clean gap between icon and number preserves the flat face.
    lo, hi = ((286, 302) if role == 'reward' else (258, 297))
    rows = np.median(a[:, lo:hi], axis=1).astype('uint8')
    for l, t, r, b in boxes:
        for y in range(t, b):
            a[y, l:r] = rows[y]
    # Save the exact repaired crop before alpha extraction for independent review.
    fixed = Image.fromarray(a)
    fixed.save(CHECK / f'{role}-repair.png')
    return cut(fixed, (0, 0, fixed.width, fixed.height)), rect, boxes


def cracks(heavy=False):
    """Hand-traced, palette-derived pixel fissures on a 64px logical grid.

    Source reference: damaged JPG blocks. Not a whole block subtraction or AI output.
    Center (25..39, 23..41) stays transparent for runtime HP text.
    """
    im = Image.new('RGBA', (64, 64))
    d = ImageDraw.Draw(im)
    paths = [([(20, 2), (20, 8), (23, 11), (21, 15), (18, 18), (15, 20)], 1),
             ([(21, 15), (24, 18), (26, 20)], 1),
             ([(8, 61), (11, 57), (10, 53), (8, 50), (8, 46)], 1),
             ([(60, 38), (55, 40), (54, 44), (50, 47), (48, 51)], 1)]
    if heavy:
        paths += [([(20, 2), (19, 8), (23, 12), (20, 17), (15, 22), (12, 27), (8, 30), (9, 35), (13, 39)], 2),
                  ([(8, 30), (6, 34), (5, 40)], 1),
                  ([(60, 38), (59, 48), (55, 54), (50, 57), (43, 54), (39, 51), (32, 48), (29, 47)], 2),
                  ([(50, 57), (48, 61)], 1),
                  ([(8, 61), (15, 55), (19, 52), (22, 47), (23, 43)], 2),
                  ([(49, 5), (53, 10), (57, 15), (59, 21), (58, 27)], 2),
                  ([(49, 5), (44, 10), (40, 13)], 1)]
    for path, width in paths:
        # Restrained rock-colored lip with deeper slit, no opaque backing.
        d.line([(x-1, y) for x, y in path], fill=(181, 195, 219, 170), width=width)
        d.line(path, fill=(53, 65, 99, 225 if heavy else 190), width=width)
    d.rectangle((25, 23, 39, 41), fill=(0, 0, 0, 0))
    return im.resize((256, 256), Image.Resampling.NEAREST)


def logo(rock, crystal, small_crystal):
    """Local rock/crystal collage, no type, no invented game name."""
    im = Image.new('RGBA', (640, 320))
    # Rock arch and low plinth around a genuinely transparent cave opening.
    positions = [(130, 173, 103), (120, 106, 115), (155, 48, 119), (229, 24, 118),
                 (310, 24, 118), (385, 48, 119), (425, 106, 115), (411, 173, 103)]
    for i, (x, y, side) in enumerate(positions):
        tile = rock.transpose(Image.Transpose.FLIP_LEFT_RIGHT) if i > 3 else rock
        tile = tile.copy(); tile.thumbnail((side, side), Image.Resampling.NEAREST)
        im.alpha_composite(tile, (x, y))
    for x, y, w, h, mirror in [(236, 91, 72, 151, True), (295, 62, 74, 194, False), (354, 135, 64, 120, False)]:
        tile = crystal if h > 160 else small_crystal
        if mirror:
            tile = tile.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
        tile = tile.copy(); tile.thumbnail((w, h), Image.Resampling.NEAREST)
        im.alpha_composite(tile, (x, y))
    # Flattened source-rock instances form a contiguous plinth, hiding cut crystal bases.
    for x, y in [(200, 231), (278, 244), (356, 231)]:
        tile = rock.resize((104, 65), Image.Resampling.NEAREST)
        im.alpha_composite(tile, (x, y))
    return im


def emit(name, image, source, rects, algorithm, limitations=''):
    assert name in NAMES
    image = image.convert('RGBA')
    a = np.array(image)
    a[a[:, :, 3] == 0, :3] = 0
    image = Image.fromarray(a)
    alpha = a[:, :, 3]
    assert alpha.min() == 0 and alpha.max() > 0
    assert not np.any(alpha[0]) and not np.any(alpha[-1])
    assert not np.any(alpha[:, 0]) and not np.any(alpha[:, -1])
    old = CHECK / 'manifest.json'
    previous = json.loads(old.read_text(encoding='utf-8')) if old.exists() else {}
    known = {r['file']: r['sha256'] for r in previous.get('assets', [])}
    path = OUT / f'{name}.png'
    if path.exists():
        assert known.get(path.name) == sha(path), f'Refuse to overwrite unreviewed output: {path}'
    image.save(path)
    RECORDS.append({'file': path.name, 'source': source, 'source_rects': rects,
                    'algorithm': algorithm, 'size': list(image.size),
                    'alpha_extrema': [int(alpha.min()), int(alpha.max())],
                    'transparent_pixels': int((alpha == 0).sum()),
                    'visible_pixels': int((alpha > 0).sum()),
                    'partial_alpha_pixels': int(((alpha > 0) & (alpha < 255)).sum()),
                    'visible_bbox': list(image.getbbox()), 'clear_edges': True,
                    'sha256': sha(path), 'limitations': limitations})


def previews_and_check(records):
    assert len(records) == 15 and {r['file'] for r in records} == {n+'.png' for n in NAMES}, 'Exact 15-item allowlist'
    sheet = Image.new('RGB', (1000, math.ceil(len(records)/3) * 235), '#263349')
    sd = ImageDraw.Draw(sheet)
    for i, record in enumerate(records):
        name = record['file']
        im = Image.open(OUT / name).convert('RGBA')
        a = np.array(im)
        assert list(im.size) == record['size'] and sha(OUT / name) == record['sha256']
        assert a[:, :, 3].min() == 0 and a[:, :, 3].max() > 0
        assert not np.any(a[0, :, 3]) and not np.any(a[-1, :, 3])
        assert not np.any(a[:, 0, 3]) and not np.any(a[:, -1, 3])
        assert not np.any(a[a[:, :, 3] == 0, :3]), 'Hidden matte RGB'
        pair = Image.new('RGB', (im.width*2, im.height))
        for j, color in enumerate(('#eee5d5', '#102038')):
            panel = Image.new('RGBA', im.size, color); panel.alpha_composite(im)
            pair.paste(panel.convert('RGB'), (j*im.width, 0))
        pair.save(CHECK / f'{Path(name).stem}-two-bg.png')
        pair.thumbnail((324, 200), Image.Resampling.NEAREST)
        x, y = i % 3 * 333, i // 3 * 235
        sd.text((x+4, y+4), name, fill='white')
        sheet.paste(pair, (x+(330-pair.width)//2, y+25+(200-pair.height)//2))
    sheet.save(CHECK / 'contact-sheet.png')
    # Nine-role overview with no runtime badges or numbers (review separation).
    blocks = Image.new('RGBA', (790, 620), '#102038')
    bd = ImageDraw.Draw(blocks)
    for row, role in enumerate(('', 'reward_', 'target_')):
        for col, shape in enumerate(('square', 'wide', 'tall')):
            im = Image.open(OUT / f'block_{role}{shape}.png').convert('RGBA')
            im.thumbnail((218, 165), Image.Resampling.NEAREST)
            x, y = col*260, row*205
            blocks.alpha_composite(im, (x+(250-im.width)//2, y+28+(170-im.height)//2))
            bd.text((x+8, y+5), role+shape, fill='white')
    blocks.save(CHECK / 'nine-shapes.png')
    # Offline composition only, not a Unity runtime capture. Exercise wide/tall stretch.
    overlay_sheet = Image.new('RGBA', (900, 470), '#102038')
    od = ImageDraw.Draw(overlay_sheet)
    for row, name in enumerate(('crack_light', 'crack_heavy')):
        for col, shape in enumerate(('square', 'wide', 'tall')):
            base = Image.open(OUT / f'block_{shape}.png').convert('RGBA')
            crack = Image.open(OUT / f'{name}.png').convert('RGBA').resize(base.size, Image.Resampling.NEAREST)
            base.alpha_composite(crack)
            base.thumbnail((270, 194), Image.Resampling.NEAREST)
            x, y = col*300, row*235
            overlay_sheet.alpha_composite(base, (x+(290-base.width)//2, y+30+(195-base.height)//2))
            od.text((x+6, y+5), name+' / '+shape+' (offline)', fill='white')
    overlay_sheet.save(CHECK / 'crack-shape-overlay.png')
    for name in ('crack_light', 'crack_heavy'):
        im = Image.open(OUT / f'{name}.png').convert('RGBA')
        assert not np.any(np.array(im)[92:168, 100:160, 3]), 'HP safe center'
    light = np.array(Image.open(OUT / 'crack_light.png'))[:, :, 3]
    heavy = np.array(Image.open(OUT / 'crack_heavy.png'))[:, :, 3]
    assert (heavy > 0).sum() > (light > 0).sum() * 1.5
    return len(records)


def main(check_only=False):
    CHECK.mkdir(parents=True, exist_ok=True)
    before = verify_sources()
    protected = {n: sha(OUT / n) for n in PROTECTED}
    if check_only:
        manifest = json.loads((CHECK / 'manifest.json').read_text(encoding='utf-8'))
        count = previews_and_check(manifest['assets'])
    else:
        board = Image.open(SOURCE / '方块图.jpg').convert('RGB')
        background = Image.open(SOURCE / '背景图.png').convert('RGB')
        for name, rect in [('block_wide', (2040, 130, 2610, 495)), ('block_tall', (2700, 130, 3060, 710))]:
            emit(name, pad(cut(board, rect)), '方块图.jpg', [rect], 'Native extraction: background-distance >42; largest filled connected silhouette; 3px transparent padding. No rotation.')
        for role in ('reward', 'target'):
            clean, rect, boxes = repair_color(board, role)
            for shape, size in [('wide', clean.size), ('square', (334, 334)), ('tall', (334, 550))]:
                im = clean if shape == 'wide' else nine_slice(clean, size)
                emit(f'block_{role}_{shape}', pad(im), '方块图.jpg', [rect], f'Local icon/number removal boxes {boxes}; clean-gap per-row median; native wide / 80px nine-slice preserved corners and bevel, nearest spans.', 'Locally reconstructed flat face; square/tall are nine-slice derivatives, not original painted shapes. Inspect at runtime size.')
        badge_rects = {'reward': (2470, 1400, 2630, 1560), 'target': (2450, 944, 2600, 1142)}
        for role, rect in badge_rects.items():
            method = 'Reviewed pixel-stepped silhouette (facets share face color), original RGB preserved.' if role == 'target' else 'Color-distance icon isolation in tight ROI; dominant silhouette filled; 1px inward trim removes orange JPEG fringe; native interior pixels retained.'
            limitation = 'Original crystal has no clean alpha; silhouette is manually traced, preserving same-colored facets.' if role == 'target' else 'Reward silhouette follows the squat upward-arrow artwork, including its shaded extrusion.'
            emit('badge_'+role, pad(cut(board, rect, role)), '方块图.jpg', [rect], method, limitation)
        for heavy in (False, True):
            emit('crack_heavy' if heavy else 'crack_light', cracks(heavy), '方块图.jpg (reference only)', [(1550, 1010, 2130, 1370), (1550, 1410, 2130, 1770)], 'Hand-traced pixel fissure paths from damaged-block reference; palette-derived lip/slit; 64x64 logical grid, nearest x4; center kept clear.', 'Reference-derived redraw, not literal crack extraction. Shared square overlay will distort on wide/tall; Unity full-rect import required.')
        rect = (2470, 1805, 2765, 2110)
        rock = cut(board, rect)
        emit('fragment', pad(rock), '方块图.jpg', [rect], 'Native largest detached rock extraction, background-distance silhouette; no artificial resize.')
        cr, sr = (30, 1084, 109, 1305), (686, 1337, 756, 1458)
        crystal, small = cut(background, cr, 'crystal'), cut(background, sr, 'crystal')
        emit('title_logo', logo(rock, crystal, small), '方块图.jpg + 背景图.png', [rect, cr, sr], '640x320 local transparent cave-arch collage: 11 native rock instances (3 compressed into plinth), 3 keyed cyan crystals; nearest resampling, selected horizontal mirrors; no text.', 'Reference-derived collage, not AI generation or newly painted logo. Repeated rock motif and cut crystal bases need art-direction review.')
        rect = (1930, 1825, 2160, 2055)
        emit('fx_spark', pad(cut(board, rect, 'spark')), '方块图.jpg', [rect], 'Bright cream/gold core key, filled dominant silhouette; exclude grey block and isolated secondary sparks; restore cream/white core palette to remove baked grey/tan at lower rays; no baked glow.', 'Original spark overlaps a block: brightness/chroma key reconstructs a hard pixel silhouette; soft source glow intentionally discarded.')
        count = previews_and_check(RECORDS)
        after = verify_sources()
        assert before == after
        assert protected == {n: sha(OUT / n) for n in PROTECTED}
        (CHECK / 'manifest.json').write_text(json.dumps({'method': 'local deterministic processing; no image model', 'sources_before': before, 'sources_after': after, 'protected_assets': protected, 'assets': RECORDS}, ensure_ascii=False, indent=2), encoding='utf-8')
    assert verify_sources() == before
    assert protected == {n: sha(OUT / n) for n in PROTECTED}
    print(f'PASS: {count}/15 RGBA assets; real transparency; clear outer edges; zero hidden matte RGB; checksummed outputs; center-safe cracks; heavy>light coverage; source/protected hashes unchanged.')
    print('Previews and manifest:', CHECK)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check-only', action='store_true')
    main(parser.parse_args().check_only)
