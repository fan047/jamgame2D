"""Slice the archived UI sheet into 9-slice panel and button sprites.

Source: D:/Downloads/picture/UI素材1.png (1312x1199 RGB, white background).
Archived copy: Assets/Holedown/Art/UI/Source/UISheet1.png

Crops, measured on the source:
  panel  x 56..1255  y 67..856    deep blue ore frame with bevel and notches
  gold   x 296..1014 y 951..1118  orange-gold button on white
  blue   x 337..966  y 609..739   blue-grey button, sits on the panel

The white sheet background (and the panel backdrop behind the blue button) becomes
transparent through a flood fill from the crop border, so bevels keep their soft edge.
The ivory primary button reuses the blue silhouette with a luminance ramp, which keeps
the bevel geometry while moving the colour onto the existing ivory role.

Usage: python Tools/SliceUIAssets.py
"""

from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets/Holedown/Art/UI/Source/UISheet1.png"
OUT = ROOT / "Assets/Resources/MineUI"

PANEL = (52, 63, 1260, 861)
GOLD = (292, 947, 1019, 1123)
BLUE = (333, 605, 970, 743)


def flood_alpha(rgb, background, tolerance):
    """Alpha 0 where the background spreads inward from the crop border."""
    height, width, _ = rgb.shape
    near = np.linalg.norm(rgb - np.array(background, dtype=float), axis=2) < tolerance
    outside = np.zeros((height, width), dtype=bool)
    queue = deque()
    for x in range(width):
        for y in (0, height - 1):
            if near[y, x] and not outside[y, x]:
                outside[y, x] = True
                queue.append((y, x))
    for y in range(height):
        for x in (0, width - 1):
            if near[y, x] and not outside[y, x]:
                outside[y, x] = True
                queue.append((y, x))
    while queue:
        y, x = queue.popleft()
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = y + dy, x + dx
            if 0 <= ny < height and 0 <= nx < width and near[ny, nx] and not outside[ny, nx]:
                outside[ny, nx] = True
                queue.append((ny, nx))
    return np.where(outside, 0.0, 1.0)


def soften(rgb, alpha):
    """Keep a one pixel rim on hard background edges so the cutout does not alias."""
    core = (alpha > 0.5).astype(float)
    grown = core.copy()
    for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        grown = np.maximum(grown, np.roll(core, (dy, dx), axis=(0, 1)))
    rim = (grown > 0.5) & (alpha <= 0.5)
    out = alpha.copy()
    out[rim] = 0.55
    return out


def cut(box, background, tolerance, name, patches=()):
    x0, y0, x1, y1 = box
    rgb = np.asarray(Image.open(SOURCE).convert("RGB").crop(box)).astype(float)
    # The sample buttons sit inside the panel; the frame keeps its stretched centre, so the
    # interior fill must be clean. Copying a clean interior column across each sample keeps
    # the vertical gradient while removing the artwork's own buttons.
    for px0, py0, px1, py1 in patches:
        column = rgb[py0 - y0:py1 - y0, px0 - 40 - x0:px0 - 20 - x0]
        width = px1 - px0
        tile = np.tile(column, (1, int(np.ceil(width / column.shape[1])), 1))[:, :width]
        rgb[py0 - y0:py1 - y0, px0 - x0:px1 - x0] = tile
    alpha = soften(rgb, flood_alpha(rgb, background, tolerance))
    rgba = np.dstack([rgb, alpha * 255.0]).astype(np.uint8)
    image = Image.fromarray(rgba, "RGBA")
    image.save(OUT / name)
    return image


def recolor(image, stops, name):
    """Map luminance onto a colour ramp, keeping the bevel shading and alpha."""
    data = np.asarray(image).astype(float)
    rgb, alpha = data[:, :, :3], data[:, :, 3:4]
    lum = rgb @ np.array([0.299, 0.587, 0.114])
    solid = alpha[:, :, 0] > 0.4
    low, high = np.percentile(lum[solid], 1), np.percentile(lum[solid], 99)
    t = np.clip((lum - low) / max(high - low, 1e-3), 0, 1)
    positions = np.array([p for p, _ in stops])
    colors = np.array([c for _, c in stops], dtype=float)
    out = np.zeros_like(rgb)
    for channel in range(3):
        out[:, :, channel] = np.interp(t, positions, colors[:, channel])
    Image.fromarray(np.dstack([out, alpha]).astype(np.uint8), "RGBA").save(OUT / name)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    cut(PANEL, (254, 254, 254), 26, "UIPanelFrame.png",
        patches=((330, 420, 995, 600), (315, 595, 995, 785)))
    gold = cut(GOLD, (254, 254, 254), 26, "UIButtonGold.png")
    blue = cut(BLUE, (26, 34, 57), 30, "UIButtonBlue.png")
    recolor(blue, [(0.00, (0x8A, 0x86, 0x6E)), (0.55, (0xF5, 0xF0, 0xD8)),
                   (0.80, (0xFF, 0xFC, 0xEE)), (1.00, (0xFF, 0xFF, 0xFF))], "UIButtonIvory.png")
    # The gold sheet sample stays as the reward role; the ivory ramp keeps the same bevel.
    print("panel", Image.open(OUT / "UIPanelFrame.png").size)
    print("gold", gold.size, "blue", blue.size)


if __name__ == "__main__":
    main()
