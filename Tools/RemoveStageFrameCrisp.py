"""Rebuild only stages 1-3 from afb2d78 LFS pixels, without smoothing.

Requires Pillow, numpy, scipy. Default writes review/proof files under Library;
--apply alone writes the PNG (never its meta), after verifying it is the reviewed
Git HEAD asset or this script's exact deterministic output.
"""
from pathlib import Path
import argparse
import hashlib
import json
import subprocess
import numpy as np
from PIL import Image, ImageDraw
from scipy.ndimage import distance_transform_edt
from PolishExistingTheme import source, background, ROOT

ASSET = ROOT / 'Assets/Holedown/Art/Theme/background_game.png'
SCRATCH = ROOT / 'Library/Locus/tmp/frame-crisp'


def frame_mask(original):
    """Measured inner silhouette, plus plaque/shadow; protect foreground overlaps."""
    w, h = original.size
    interior = Image.new('L', (w, h))
    draw = ImageDraw.Draw(interior)
    # Include the inner shadow at the two rounded top corners; its diagonal
    # highlight must not become a nearest-neighbor source at the canvas corner.
    draw.polygon([(58, 50), (883, 50), (899, 65), (899, 1608),
                  (880, 1628), (60, 1628), (42, 1608), (42, 65)], fill=255)
    draw.polygon([(347, 18), (592, 18), (594, 44), (557, 99),
                  (388, 99), (347, 47)], fill=0)
    mask = np.array(interior) == 0
    rgb = np.array(original)[:, :, :3].astype(float)
    crystal = (rgb[:, :, 1] > rgb[:, :, 0] * 1.35) & (rgb[:, :, 1] > 85)
    # The left crystal overlaps the ring. Protect those real foreground pixels;
    # the removed frame must be replaced with cave, not extruded green columns.
    mask[crystal] = False
    return mask


def rebuild():
    original = source('background_game')
    assert original.size == (941, 1672)
    toned = np.array(background(original))
    mask = frame_mask(original)
    # Exact Euclidean nearest valid pixel; no color average, interpolation,
    # inpainting, modal filter or feather. Sources are strictly nonmasked.
    rgb = np.array(original)[:, :, :3].astype(float)
    crystal = (rgb[:, :, 1] > rgb[:, :, 0] * 1.35) & (rgb[:, :, 1] > 85)
    # Restrict eligible sources to cave pixels. Crystals remain untouched but
    # cannot be stretched into a rectangular new face over the removed ring.
    unavailable = mask | crystal
    indices = distance_transform_edt(unavailable, return_distances=False, return_indices=True)
    result = toned.copy()
    result[mask] = toned[indices[0][mask], indices[1][mask]]
    assert np.array_equal(result[~mask], toned[~mask])
    assert np.all(result[:, :, 3] == 255)
    assert not np.any(mask[indices[0][mask], indices[1][mask]])
    assert np.array_equal(result[mask], toned[indices[0][mask], indices[1][mask]])
    return original, Image.fromarray(result), mask


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--apply', action='store_true')
    args = parser.parse_args()
    proof = SCRATCH / 'validation.json'
    previous = json.loads(proof.read_text(encoding='utf-8')) if proof.exists() else {}
    reviewed = set(previous.get('reviewed_outputs', []))
    if previous.get('sha256'): reviewed.add(previous['sha256'])
    original, result, mask = rebuild()
    SCRATCH.mkdir(parents=True, exist_ok=True)
    original.save(SCRATCH / 'original.png')
    result.save(SCRATCH / 'background-crisp.png')
    overlay = np.array(original)
    overlay[mask, :3] = (overlay[mask, :3].astype(float) * .4 + np.array([255, 40, 60]) * .6).astype('uint8')
    Image.fromarray(overlay).save(SCRATCH / 'mask-overlay.png')
    Image.fromarray((mask * 255).astype('uint8')).save(SCRATCH / 'mask.png')
    current = Image.open(ASSET).convert('RGBA')
    pair = Image.new('RGB', (941 * 3, 1672))
    for i, im in enumerate((original, current, result)):
        pair.paste(im, (i * 941, 0))
    pair.resize((1128, 668), Image.Resampling.NEAREST).save(SCRATCH / 'original-current-crisp.png')
    boxes = {'top': (0, 0, 941, 120), 'bottom': (0, 1530, 941, 1672),
             'left': (0, 1000, 150, 1400), 'right': (791, 600, 941, 1000),
             'tl': (0, 0, 160, 160), 'tr': (781, 0, 941, 160),
             'bl': (0, 1512, 160, 1672), 'br': (781, 1512, 941, 1672)}
    for name, box in boxes.items():
        width, height = box[2]-box[0], box[3]-box[1]
        detail = Image.new('RGB', (width * 2, height))
        detail.paste(original.crop(box), (0, 0))
        detail.paste(result.crop(box), (width, 0))
        detail.save(SCRATCH / ('detail-' + name + '.png'))
    for w, h in ((540, 960), (720, 1280), (1080, 2400)):
        stage = result.resize((w, round(w * 1672 / 941)), Image.Resampling.NEAREST)
        canvas = Image.new('RGB', (w, h), '#10172a')
        canvas.paste(stage, (0, (h-stage.height)//2))
        canvas.save(SCRATCH / f'preview-{w}x{h}.png')
    record = {'source': 'afb2d78:Assets/Holedown/Art/Theme/background_game.png (Git LFS)',
              'size': list(result.size), 'masked_pixels': int(mask.sum()),
              'fill': 'exact Euclidean nearest nonmasked cave pixel (protected crystals excluded as sources)',
              'reviewed_outputs': sorted(reviewed),
              'alpha_255': True, 'blur_or_feather': False,
              'sha256': hashlib.sha256((SCRATCH/'background-crisp.png').read_bytes()).hexdigest()}
    (SCRATCH / 'validation.json').write_text(json.dumps(record, indent=2), encoding='utf-8')
    print(json.dumps(record, indent=2))
    if args.apply:
        # The user may have edited the PNG after preview. Never silently overwrite it.
        head = subprocess.check_output(['git', 'show', 'HEAD:Assets/Holedown/Art/Theme/background_game.png'], cwd=ROOT)
        if head.startswith(b'version https://git-lfs.github.com/spec/v1'):
            expected = head.decode('ascii').split('oid sha256:')[1].splitlines()[0]
        else:
            expected = hashlib.sha256(head).hexdigest()
        current_hash = hashlib.sha256(ASSET.read_bytes()).hexdigest()
        assert current_hash in {expected, record['sha256']} | reviewed, 'unreviewed local background modification'
        # Match the previous quiet gradient away from frame processing.
        assert np.array_equal(np.array(current)[150:1300, 160:780], np.array(result)[150:1300, 160:780]), 'quiet middle changed'
        result.save(ASSET)
        print('Applied PNG only; meta untouched.')


if __name__ == '__main__':
    main()
