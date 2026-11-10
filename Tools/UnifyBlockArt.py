"""Rebuild the three block groups from one shared template.

The stage 4-6 blocks already share one silhouette: stepped outer edge, bevelled
top-left highlight, dark bottom-right shade and ore facets. The 1-3 group still
uses the older soft slab and the 7-9 group drifted on its own, so both are
regenerated from the 4-6 structure and only their own palette is kept.

Palette transfer: every source sprite is reduced to a luminance -> colour table
(64 bins), and the template's pixels are remapped through the target group's
table. Silhouette, facet steps and grain come from the template; hue and
brightness steps come from the group that is being rebuilt.

Usage: python Tools/UnifyBlockArt.py
"""

from collections import defaultdict
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
THEME = ROOT / "Assets/Holedown/Art/Theme"

ROLES = ("ordinary", "reward", "target")
SHAPES = ("square", "tall", "wide")
BINS = 64


def paths(group, role, shape):
    """group '' is the 1-3 set, which has no prefix and drops the ordinary role name."""
    prefix = "" if group == "" else f"stages{group}_"
    stem = role if group != "" else "" if role == "ordinary" else role
    return THEME / f"block_{prefix}{stem + '_' if stem else ''}{shape}.png"


def luminance(rgb):
    return rgb[..., 0] * 0.299 + rgb[..., 1] * 0.587 + rgb[..., 2] * 0.114


def ramp(image):
    """Luminance ordered colour table with the sprite's own hue shifts."""
    data = np.asarray(image).astype(float)
    rgb, alpha = data[..., :3], data[..., 3]
    solid = alpha > 200
    if not solid.any():
        raise SystemExit("sprite has no opaque pixels")
    lum = luminance(rgb[solid])
    colours = rgb[solid]
    low, high = np.percentile(lum, 2), np.percentile(lum, 98)
    span = max(high - low, 1.0)
    bins = np.clip(((lum - low) / span * (BINS - 1)).astype(int), 0, BINS - 1)
    totals = np.zeros((BINS, 3))
    counts = np.zeros(BINS)
    np.add.at(totals, bins, colours)
    np.add.at(counts, bins, 1.0)
    # Fill empty bins from the nearest populated neighbour so the table stays monotonic.
    filled = counts > 0
    if not filled.all():
        index = np.arange(BINS)
        known = index[filled]
        means = totals / np.maximum(counts, 1.0)[:, None]
        for empty in index[~filled]:
            nearest = known[np.argmin(np.abs(known - empty))]
            totals[empty] = means[nearest]
            counts[empty] = 1.0
    return totals / counts[:, None]


def rebuild(template_path, palette_path, out_path):
    template = Image.open(template_path).convert("RGBA")
    palette = Image.open(palette_path).convert("RGBA")
    table = ramp(palette)

    data = np.asarray(template).astype(float)
    rgb, alpha = data[..., :3], data[..., 3]
    lum = luminance(rgb)
    solid = alpha > 8
    low, high = np.percentile(lum[solid], 2), np.percentile(lum[solid], 98)
    span = max(high - low, 1.0)
    t = np.clip((lum - low) / span, 0.0, 1.0)
    position = t * (BINS - 1)
    lower = np.floor(position).astype(int)
    upper = np.minimum(lower + 1, BINS - 1)
    weight = (position - lower)[..., None]
    out = table[lower] * (1.0 - weight) + table[upper] * weight
    out[~solid] = 0.0
    Image.fromarray(np.dstack([out, alpha]).astype(np.uint8), "RGBA").save(out_path)
    return out_path.stat().st_size


def main():
    report = defaultdict(list)
    for role in ROLES:
        for shape in SHAPES:
            template = paths("4-6", role, shape)
            if not template.exists():
                raise SystemExit(f"missing template {template}")
            for group in ("", "7-9"):
                target = paths(group, role, shape)
                if not target.exists():
                    raise SystemExit(f"missing target {target}")
                rebuild(template, target, target)
                report[group or "1-3"].append(target.name)
    for group, names in report.items():
        print(f"{group}: rebuilt {len(names)} sprites from the 4-6 template")


if __name__ == "__main__":
    main()
