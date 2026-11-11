"""Build the Lucky Break launcher icon from the shipped art.

Subject is the golden ball the player fires (``Assets/Holedown/Art/Theme/ball.png``),
not the title wordmark or the crystal. The ball keeps its own hard light planes,
outline and glint; every pixel of the source art is preserved, only trimmed to its
alpha bounds and centred on the cave tone from the title key art.

Writes only under Library until --apply, which writes the PNGs into
Assets/Holedown/Art/Branding/Icon. Import settings and PlayerSettings are applied
by the Unity side of the pipeline, not here.
"""
from pathlib import Path
import argparse
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
BALL = ROOT / 'Assets/Holedown/Art/Theme/ball.png'
PREVIEW = ROOT / 'Library/Locus/tmp/icon'
OUT = ROOT / 'Assets/Holedown/Art/Branding/Icon'

# How much of the tile the ball fills. A circle reads best when it is generous:
# it stays a clean disc down to 36 px, whereas a smaller one starts to read as a dot.
FILL = .74
# Share of the source radius that supplies colour; beyond this is the shipped
# stepped outline, which the icon replaces with its own uniform ring.
INNER = .90
# Strength of the spherical light falloff, and the outline colour from the sprite.
SHADE = .30
# Outline colour, sampled from the shipped sprite so the ring matches the art.
RIM = np.array([176, 126, 43], np.float32)
# Icon keeps the shipped sprite's own stepped edge; --round selects the rebuilt disc.
ROUND_BALL = False
MASTER = 1024
# Android adaptive icons crop the foreground to the central 66% safe zone.
SAFE = .66
LEGACY = (192, 144, 96, 72, 48, 36)
# Android adaptive slots, densest first. Each slot needs both layers at its size.
ADAPTIVE = (432, 324, 216, 162, 108, 81)
# Windows player icon sizes, in the order PlayerSettings expects.
WINDOWS = (1024, 512, 256, 128, 64, 48, 32, 16)


def key_art():
    return np.array(Image.open(KEY_ART).convert('RGB')).astype(np.float32)


def _sample(src, u, v):
    """Bilinear sample of an RGBA float array at pixel coordinates."""
    h, w, _ = src.shape
    u = np.clip(u, 0, w - 1.001); v = np.clip(v, 0, h - 1.001)
    x0 = u.astype(np.int32); y0 = v.astype(np.int32)
    fx = (u - x0)[..., None]; fy = (v - y0)[..., None]
    a = src[y0, x0]; b = src[y0, x0 + 1]; c = src[y0 + 1, x0]; d = src[y0 + 1, x0 + 1]
    return a * (1 - fx) * (1 - fy) + b * fx * (1 - fy) + c * (1 - fx) * fy + d * fx * fy


def foreground(size=MASTER, rounded=ROUND_BALL):
    """The fired ball.

    Default keeps the shipped sprite exactly as drawn: its own stepped silhouette,
    its own uneven outline and its own glint. ``rounded`` instead rebuilds a true
    disc (uniform rim, anti-aliased edge, light spherical falloff); it is kept so
    the alternative stays reproducible, but the icon uses the shipped edge.
    """
    src = np.array(Image.open(BALL).convert('RGBA')).astype(np.float32)
    mask = src[:, :, 3] > 128
    ys, xs = np.nonzero(mask)
    assert len(xs), 'ball.png has no visible pixels'
    if not rounded:
        # Trim to the visible bounds and pad to a square so scaling cannot squash
        # the disc; colours and alpha are untouched.
        top, bottom, left, right = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
        sprite = src[top:bottom, left:right]
        side = max(sprite.shape[0], sprite.shape[1])
        canvas = np.zeros((side, side, 4), np.float32)
        oy = (side - sprite.shape[0]) // 2
        ox = (side - sprite.shape[1]) // 2
        canvas[oy:oy + sprite.shape[0], ox:ox + sprite.shape[1]] = sprite
        image = Image.fromarray(canvas.astype('uint8'), 'RGBA')
        return image if size == side else image.resize((size, size), Image.Resampling.LANCZOS)
    cx = (xs.min() + xs.max() + 1) / 2
    cy = (ys.min() + ys.max() + 1) / 2
    radius = ((xs.max() - xs.min() + 1) + (ys.max() - ys.min() + 1)) / 4
    yy, xx = np.mgrid[0:size, 0:size].astype(np.float32) + .5
    center = size / 2
    dx = (xx - center) / center; dy = (yy - center) / center
    r = np.hypot(dx, dy)
    rgb = _sample(src, cx + dx * radius * INNER, cy + dy * radius * INNER)[:, :, :3]
    normal_z = np.sqrt(np.clip(1 - r ** 2, 0, 1))
    light = np.array([-0.45, -0.55, 0.70]); light /= np.linalg.norm(light)
    lambert = np.clip(dx * light[0] + dy * light[1] + normal_z * light[2], 0, 1)
    rgb = rgb * (1 - SHADE + SHADE * (0.55 + 1.05 * lambert))[:, :, None]
    rgb[r > INNER] = RIM
    alpha = np.clip((1 - r) * center, 0, 1)
    out = np.dstack([np.clip(rgb, 0, 255), alpha * 255]).astype('uint8')
    out[out[:, :, 3] == 0, :3] = 0
    return Image.fromarray(out, 'RGBA')


def background(size):
    """Quiet indigo cave tone, lifted towards the centre so the gold reads warm."""
    y, x = np.mgrid[0:size, 0:size].astype(np.float32)
    cx = cy = (size - 1) / 2.0
    radial = np.clip(1.0 - np.sqrt((x - cx) ** 2 + (y - cy) ** 2) / (size * .62), 0.0, 1.0) ** 1.6
    base = np.array([26, 28, 56], np.float32)
    lift = np.array([44, 30, 62], np.float32)
    glow = np.array([120, 72, 30], np.float32)
    tone = base + lift * radial[:, :, None] + glow * (radial ** 2.4)[:, :, None] * .45
    return Image.fromarray(np.clip(tone, 0, 255).astype('uint8'), 'RGB')


def place(canvas, fill):
    """Size the ball to `fill` of the canvas and centre it."""
    size = canvas.width
    # Build at the placement size so the anti-aliased edge is one pixel here, not
    # a downsample of a much larger master.
    fg = foreground(max(8, round(size * fill)))
    x = (size - fg.width) // 2
    y = (size - fg.height) // 2
    canvas.alpha_composite(fg, (x, y)) if canvas.mode == 'RGBA' else canvas.paste(fg, (x, y), fg)
    return canvas


def compose_square(size):
    """Legacy/full-bleed icon: the ball over the background."""
    return place(background(size).convert('RGBA'), FILL).convert('RGB')


def build():
    master = compose_square(MASTER)
    # Adaptive layers share one canvas size so the launcher can resize the 108dp
    # grid; the foreground already sits inside the safe zone.
    fg_layer = place(Image.new('RGBA', (MASTER, MASTER), (0, 0, 0, 0)), SAFE)
    return master, fg_layer, background(MASTER)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--apply', action='store_true')
    parser.add_argument('--round', action='store_true',
                        help='rebuild the ball as a smooth disc instead of using the shipped stepped edge')
    args = parser.parse_args()
    global ROUND_BALL
    ROUND_BALL = args.round
    master, fg, bg = build()
    PREVIEW.mkdir(parents=True, exist_ok=True)
    master.save(PREVIEW / 'icon-master.png')
    fg.save(PREVIEW / 'icon-foreground.png')
    bg.save(PREVIEW / 'icon-background.png')
    for size in LEGACY:
        compose_square(size).save(PREVIEW / f'icon-{size}.png')
    # Contact sheet: launcher sizes, then the composed layers under the masks
    # phones actually apply (circle, squircle, square).
    sheet = Image.new('RGB', (24 + len(LEGACY) * 136, 360), '#e6e7ec')
    for i, size in enumerate(LEGACY):
        tile = compose_square(size).convert('RGB').resize((128, 128), Image.Resampling.LANCZOS)
        sheet.paste(tile, (12 + i * 136, 14))
    adaptive = background(256).convert('RGBA')
    place(adaptive, SAFE)
    for i, radius in enumerate((128, 62, 0)):
        mask = Image.new('L', (1024, 1024), 0)
        draw = ImageDraw.Draw(mask)
        if radius == 128:
            draw.ellipse((0, 0, 1023, 1023), fill=255)
        else:
            draw.rounded_rectangle((0, 0, 1023, 1023), radius=radius * 4, fill=255)
        mask = mask.resize((256, 256), Image.Resampling.LANCZOS)
        cell = Image.new('RGB', (256, 256), '#c8cbd4')
        cell.paste(adaptive, (0, 0), mask)
        sheet.paste(cell.resize((128, 128), Image.Resampling.LANCZOS), (12 + i * 150, 200))
    sheet.save(PREVIEW / 'icon-sheet.png')
    print({'ball': FILL, 'master': master.size, 'foreground': fg.size, 'legacy': LEGACY,
           'adaptive': ADAPTIVE, 'applied': args.apply})
    if args.apply:
        OUT.mkdir(parents=True, exist_ok=True)
        master.save(OUT / 'LuckyBreakIcon.png')
        fg.save(OUT / 'LuckyBreakIconForeground.png')
        bg.save(OUT / 'LuckyBreakIconBackground.png')
        for size in LEGACY:
            compose_square(size).save(OUT / f'LuckyBreakIcon{size}.png')
        for size in WINDOWS:
            compose_square(size).save(OUT / f'LuckyBreakIconWin{size}.png')
        # Adaptive layers, one pair per slot size so the build logs no icon resize.
        for size in ADAPTIVE:
            place(Image.new('RGBA', (size, size), (0, 0, 0, 0)), SAFE).save(OUT / f'LuckyBreakIconFg{size}.png')
            background(size).save(OUT / f'LuckyBreakIconBg{size}.png')
        print('written to', OUT)


if __name__ == '__main__':
    main()
