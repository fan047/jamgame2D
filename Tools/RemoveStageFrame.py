"""Remove the baked rectangular stage frame from the existing background PNG.

The bright ring (and the top plaque that belongs to it) is masked out and filled
from its surroundings with OpenCV inpainting, so existing rock facets continue
naturally instead of a hard border. Size, alpha and the rest of the composition
are untouched. Run without flags for previews, with --apply to write the asset.
"""
from pathlib import Path
import sys
import numpy as np
from PIL import Image
import cv2

ROOT = Path(__file__).resolve().parents[1]
ASSET = ROOT / 'Assets/Holedown/Art/Theme/background_game.png'
SCRATCH = ROOT / 'Library/Locus/tmp/frame-removal'

def main():
    apply = '--apply' in sys.argv
    original = Image.open(ASSET).convert('RGBA')
    a = np.array(original)
    rgb, alpha = a[:, :, :3].copy(), a[:, :, 3]
    h, w = alpha.shape
    mask = np.zeros((h, w), np.uint8)
    # Ring: measured light bands at x 16..31 / 909..924, y 20..38 / 1640..1653,
    # including the rounded corners they are joined by.
    mask[12:1644, 0:44] = 255
    mask[12:1644, 897:941] = 255
    mask[0:100, 0:w] = 255
    mask[1630:h, 0:w] = 255
    # Top plaque (trapezoid slab) that reads as a signboard over the cave.
    mask[12:100, 340:605] = 255
    fixed = cv2.inpaint(rgb, mask, 9, cv2.INPAINT_TELEA)
    # Settle the repaired edge into the cave's outer tone without a bright rim.
    fy = np.arange(h)[:, None].astype(float)
    fx = np.arange(w)[None, :].astype(float)
    edge = np.minimum(np.minimum(fx, (w - 1) - fx), np.minimum(fy, (h - 1) - fy))
    fade = np.clip(edge / 34.0, 0, 1)
    outer = np.array([25, 33, 65], float)
    fixed = fixed.astype(float) * fade[:, :, None] + outer * (1 - fade[:, :, None])
    out = Image.fromarray(np.dstack([np.clip(fixed, 0, 255).astype('uint8'), alpha]))
    assert out.size == original.size
    assert np.array_equal(np.array(out)[:, :, 3], alpha), 'alpha changed'
    out_rgba = np.array(out)
    assert not np.any(out_rgba[alpha == 0][:, :3]), 'hidden matte RGB'
    SCRATCH.mkdir(parents=True, exist_ok=True)
    out.save(SCRATCH / 'background-no-frame.png')
    for name, box in {'top': (0, 0, 941, 150), 'bottom': (0, 1520, 941, 1672),
                      'left': (0, 560, 130, 1440), 'right': (811, 560, 941, 1440)}.items():
        src, dst = original.crop(box), out.crop(box)
        pair = Image.new('RGB', ((box[2] - box[0]) * 2 + 8, box[3] - box[1]), '#ff00ff')
        pair.paste(src, (0, 0)); pair.paste(dst, (box[2] - box[0] + 8, 0))
        pair.save(SCRATCH / f'pair-{name}.png')
    pair = Image.new('RGB', (941 * 2 + 8, 1672), '#ff00ff')
    pair.paste(original, (0, 0)); pair.paste(out, (949, 0))
    pair.resize((941, 836), Image.Resampling.LANCZOS).save(SCRATCH / 'pair-full.png')
    print({'masked_pixels': int((mask > 0).sum()), 'applied': apply})
    if apply:
        out.save(ASSET)

if __name__ == '__main__': main()
