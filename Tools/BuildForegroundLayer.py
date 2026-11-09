"""Extract the real rock/crystal art from the existing background as a foreground layer.

The cave backdrop is a flat navy gradient whose brightest channel never exceeds 69, while
rocks (slate) and crystals (teal) are far brighter. A brightness key therefore isolates
the real decorations; small enclosed pockets inside one decoration are closed again so a
rock reads as a solid shape. Nothing is moved, raised, mirrored or extended, no
rectangular band is pasted and no solid tone is painted into the empty middle: the
cave field and the gaps between decorations keep alpha 0, and every kept pixel is the
original background pixel.

Output: Assets/Holedown/Art/Theme/foreground_game.png (941x1672 RGBA, binary alpha).
Run without --apply for scratch previews only.
"""
from pathlib import Path
import sys
import numpy as np
import cv2
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / 'Assets/Holedown/Art/Theme/background_game.png'
OUT = ROOT / 'Assets/Holedown/Art/Theme/foreground_game.png'
SCRATCH = ROOT / 'Library/Locus/tmp/foreground'
STAGE = (9.4, 16.702232)
STAGE_CENTER = (4.0, 4.8)
# Brightest cave-field channel is 69; decorations start above 74.
DECORATION_BRIGHTNESS = 74
MIN_DECORATION_AREA = 200
# Smooth shading inside one rock can fall below the key. Close enclosed pockets up to
# this size so the shape is solid again; the open cave field is never filled.
MAX_INNER_POCKET = 30000


def decoration_mask(rgb):
    seed = (rgb.max(axis=2) >= DECORATION_BRIGHTNESS).astype(np.uint8)
    count, labels, stats, _ = cv2.connectedComponentsWithStats(seed, 8)
    mask = np.zeros_like(seed)
    for i in range(1, count):
        if stats[i, 4] >= MIN_DECORATION_AREA:
            mask[labels == i] = 1
    return mask


def close_inner_pockets(mask):
    """Fill pockets a decoration encloses, never the open field or the image border."""
    h, w = mask.shape
    count, labels, stats, _ = cv2.connectedComponentsWithStats((mask == 0).astype(np.uint8), 4)
    filled = mask.copy()
    for i in range(1, count):
        x, y, cw, ch, area = stats[i]
        if area > MAX_INNER_POCKET or x == 0 or y == 0 or x + cw >= w or y + ch >= h:
            continue
        filled[labels == i] = 1
    return filled


def main():
    apply = '--apply' in sys.argv
    rgb = np.array(Image.open(SRC).convert('RGB'))
    h, w, _ = rgb.shape
    mask = close_inner_pockets(decoration_mask(rgb))
    # Original pixels only: the foreground is a copy of the art it belongs to.
    out = np.dstack([rgb.copy(), np.where(mask > 0, 255, 0).astype('uint8')])
    out[out[:, :, 3] == 0] = 0
    image = Image.fromarray(out)
    assert image.size == (941, 1672)
    assert image.getchannel('A').getextrema() == (0, 255)

    SCRATCH.mkdir(parents=True, exist_ok=True)
    image.save(SCRATCH / 'foreground_game.png')
    # Overlaying the foreground on its own background must not change one pixel: the
    # proof that no band, wall or invented tone was added.
    base = Image.open(SRC).convert('RGBA')
    over = base.copy()
    over.alpha_composite(image)
    difference = np.abs(np.array(over.convert('RGB')).astype(int) - np.array(base.convert('RGB')).astype(int)).max(axis=2)
    assert int((difference > 0).sum()) == 0, 'foreground must reproduce the original art exactly'
    for name, bg in (('dark', (16, 22, 36)), ('light', (238, 229, 213))):
        panel = Image.new('RGBA', image.size, bg + (255,))
        panel.alpha_composite(image)
        panel.resize((941 // 2, 1672 // 2), Image.Resampling.NEAREST).save(SCRATCH / f'preview-{name}.png')
    # Offline block check: row 0 blocks behind background + foreground, to see which
    # decoration really covers a block and that numbers stay in the open field.
    stage = base.copy()
    block = Image.open(ROOT / 'Assets/Holedown/Art/Theme/block_square.png').convert('RGBA')
    block = block.resize((90, 90), Image.Resampling.LANCZOS)
    for column in range(8):
        cx = int(round((column + 0.5 + 0.7) * (941 / STAGE[0])))
        stage.alpha_composite(block, (cx - 45, 1266 - 45))
    stage.alpha_composite(image)
    stage.resize((941 // 2, 1672 // 2), Image.Resampling.LANCZOS).save(SCRATCH / 'preview-blocks.png')
    crop = (0, 1240, 941, 1672)
    pair = Image.new('RGB', (941, (crop[3] - crop[1]) * 2 + 8), '#ff00ff')
    pair.paste(base.crop(crop).convert('RGB'), (0, 0))
    pair.paste(over.crop(crop).convert('RGB'), (0, crop[3] - crop[1] + 8))
    pair.save(SCRATCH / 'pair-over-background.png')
    print({'foreground_pixels': int(mask.sum()),
           'coverage_percent': round(float(mask.mean()) * 100, 2),
           'diff_pixels': int((difference > 0).sum()), 'applied': apply})
    if apply:
        image.save(OUT)


if __name__ == '__main__':
    main()
