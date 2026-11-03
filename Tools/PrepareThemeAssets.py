"""Prepare user-authorized theme assets without modifying source artwork.

Uses local deterministic extraction/repair, not an AI image generation service.
All crop rectangles use source-image pixels (left, top, right, bottom).
"""
from pathlib import Path
import argparse
import hashlib
import json
import cv2
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path('D:/Downloads/picture')
OUT = ROOT / 'Assets/Holedown/Art/Theme'
CHECK = ROOT / 'Library/Locus/tmp/theme-check'
EXPECTED = {
    '背景图.png': '3230DEF8BAD19673AA71172AFA6E99D1372AFBC5D0C449BD43576CD03422FDE2',
    '方块图.jpg': '0001431ECDE1B1670C238621A9D66B2FB66B565A78C06BBEC8581D65240BA2E2',
}


def verify_sources():
    for name, expected in EXPECTED.items():
        actual = hashlib.sha256((SOURCE / name).read_bytes()).hexdigest().upper()
        if actual != expected:
            raise ValueError(f'Source changed: {name}: {actual}; review before processing')


def isolated(crop, mode='rock'):
    a = np.asarray(crop.convert('RGB')).copy()
    if mode == 'gold':
        mask = ((a[:,:,0].astype(int) - a[:,:,2]) > 30) & (a[:,:,0] > 100)
    else:
        bg = np.array([31,38,56])
        mask = np.linalg.norm(a.astype(float) - bg, axis=2) > 32
    # Keep the dominant connected subject; fill internal JPEG pinholes, not background.
    count, labels, stats, _ = cv2.connectedComponentsWithStats(mask.astype('uint8'), 8)
    if count < 2:
        raise ValueError('No foreground')
    index = 1 + np.argmax(stats[1:, cv2.CC_STAT_AREA])
    mask = (labels == index).astype('uint8') * 255
    contours, _ = cv2.findContours(mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    cv2.drawContours(mask, contours, -1, 255, cv2.FILLED)
    rgba = np.dstack((a, mask))
    rgba[mask == 0, :3] = 0
    return Image.fromarray(rgba).crop(Image.fromarray(mask).getbbox())


def canvas(subject, size=None, margin=2):
    # No repeated resize/upscale to manufacture nominal dimensions.
    size = size or (subject.width + margin*2, subject.height + margin*2)
    image = Image.new('RGBA', size)
    image.alpha_composite(subject, ((size[0]-subject.width)//2, (size[1]-subject.height)//2))
    return image


def save(name, image):
    path = OUT / name
    if path.exists():
        raise FileExistsError(f'Refusing to overwrite reviewed asset: {path}')
    image.save(path)
    return {'file': name, 'size': list(image.size), 'mode': image.mode,
            'alpha_min_max': list(image.getchannel('A').getextrema()) if image.mode == 'RGBA' else None,
            'sha256': hashlib.sha256(path.read_bytes()).hexdigest()}


def m1():
    background = Image.open(SOURCE / '背景图.png').convert('RGB')
    board = Image.open(SOURCE / '方块图.jpg').convert('RGB')
    records = []
    records.append(save('ball.png', canvas(isolated(board.crop((3090,870,3380,1155)), 'gold'))))
    records.append(save('block_square.png', canvas(isolated(board.crop((1550,130,1920,495))))))
    ring = background.crop((428,43,514,125)).convert('RGBA')
    a = np.array(ring)
    mask = (a[:,:,0].astype(int) - a[:,:,2] > 35) & (a[:,:,0] > 100)
    # Do not fill the contour: the launcher's hole must remain truly transparent.
    a[:,:,3] = mask.astype('uint8')*255
    a[~mask,:3] = 0
    ring = Image.fromarray(a).crop(Image.fromarray(a[:,:,3]).getbbox())
    side = max(ring.size)+4
    records.append(save('launcher.png', canvas(ring, (side,side))))
    repaired = np.array(background)
    # Local scanline reconstruction of the plate/background behind the ring.
    # Neighbour samples belong to the same horizontal structure, leaving the frame untouched.
    for y in range(43,126):
        left = repaired[y,414:428].mean(axis=0)
        right = repaired[y,514:528].mean(axis=0)
        for x in range(428,514):
            t = (x-428)/85
            repaired[y,x] = np.rint(left*(1-t)+right*t).astype('uint8')
    records.append(save('background_game.png', Image.fromarray(repaired)))
    (CHECK / 'm1-results.json').write_text(json.dumps(records,indent=2), encoding='utf-8')
    previews()
    verify_sources()
    print(json.dumps(records, indent=2))


def previews():
    files = list(OUT.glob('*.png'))
    tile = 230
    sheet = Image.new('RGB', (tile*4, ((len(files)+3)//4)*tile), '#263349')
    draw = ImageDraw.Draw(sheet)
    for i,path in enumerate(files):
        image = Image.open(path).convert('RGBA')
        image.thumbnail((200,190), Image.Resampling.NEAREST)
        x,y = (i%4)*tile,(i//4)*tile
        for c,color in enumerate(('#eeeeee','#102038')):
            panel = Image.new('RGBA',(100,190), color)
            thumb = image.copy();thumb.thumbnail((96,180),Image.Resampling.NEAREST)
            panel.alpha_composite(thumb,((100-thumb.width)//2,(190-thumb.height)//2))
            sheet.paste(panel.convert('RGB'),(x+c*100,y+20))
        draw.text((x+4,y+4),path.name,fill='white')
    sheet.save(CHECK/'contact-sheet.png')


if __name__ == '__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--batch',choices=['m1'],required=True)
    args=parser.parse_args()
    verify_sources(); OUT.mkdir(parents=True,exist_ok=True); CHECK.mkdir(parents=True,exist_ok=True)
    m1()
