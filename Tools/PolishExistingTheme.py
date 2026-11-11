"""Targeted cleanup of the 19 existing team-supplied theme PNGs.

Inputs are the reviewed PNG bytes in afb2d78, NOT generated replacement art.
No Unity serialized data is written. Run --group background|blocks|details|logo
then --apply to write that group; otherwise only scratch previews are written.
Dimensions and alpha silhouettes are preserved, including the locally repainted logo plinth.
"""
from pathlib import Path
import argparse
import hashlib
import json
import subprocess
import io
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
REL = 'Assets/Holedown/Art/Theme'
OUT = ROOT / REL
SCRATCH = ROOT / 'Library/Locus/tmp/theme-polish'
BASE = 'afb2d78'
GROUPS = {
    'background': ['background_game'],
    'blocks': ['block_square', 'block_wide', 'block_tall'] + [f'block_{r}_{s}' for r in ('reward','target') for s in ('square','wide','tall')],
    'details': ['ball','launcher','badge_reward','badge_target','crack_light','crack_heavy','fragment','fx_spark'],
    'logo': ['title_logo'],
}

def source(name):
    data = subprocess.check_output(['git','show',f'{BASE}:{REL}/{name}.png'], cwd=ROOT)
    if data.startswith(b'version https://git-lfs.github.com/spec/v1'):
        oid=data.decode('ascii').split('oid sha256:')[1].splitlines()[0]
        common=Path(subprocess.check_output(['git','rev-parse','--git-common-dir'],cwd=ROOT,text=True).strip())
        if not common.is_absolute(): common=ROOT/common
        data=(common/'lfs/objects'/oid[:2]/oid[2:4]/oid).read_bytes()
        assert hashlib.sha256(data).hexdigest()==oid
    return Image.open(io.BytesIO(data)).convert('RGBA')

def clean(im):
    a=np.array(im.convert('RGBA')); a[a[:,:,3]==0,:3]=0
    return Image.fromarray(a)

def palette(im, colors, smooth=True):
    """Nearest-color simplification, alpha is never quantized."""
    a=np.array(im); pal=np.array(colors,dtype=np.float32)
    # Work by row to avoid a full image x palette temporary allocation.
    for y in range(a.shape[0]):
        dist=((a[y,:,:3,None].astype(float)-pal.T[None,:,:])**2).sum(axis=1)
        a[y,:,:3]=pal[dist.argmin(axis=1)].astype('uint8')
    # Remove isolated compression flecks without introducing blended edge colors.
    if smooth:
        filtered=Image.fromarray(a[:,:,:3]).filter(ImageFilter.ModeFilter(5))
        a[:,:,:3]=np.array(filtered)
    return clean(Image.fromarray(a))

ROCK=[(39,55,77),(78,101,127),(130,153,174),(163,185,200),(190,211,222)]
GOLD=[(112,76,44),(162,106,49),(215,155,66),(237,190,105),(250,220,155)]
TEAL=[(30,66,81),(37,105,120),(45,163,157),(89,204,183),(150,231,207)]


def background(im):
    a=np.array(im); rgb=a[:,:,:3].astype(float)
    y,x=np.mgrid[:im.height,:im.width]
    # Quiet middle untouched spatially. Only the existing rock/crystal frame is
    # stepped down in value; no extra decoration or opaque foreground is added.
    frame=(x<130)|(x>810)|(y>1335)|(y<90)
    crystal=(rgb[:,:,1]>rgb[:,:,0]*1.35)&(rgb[:,:,1]>95)&frame
    # Select actual lit decoration, not the rectangular frame region itself;
    # otherwise quantizing the empty gradient creates a visible straight seam.
    rock=frame & ~crystal & ((rgb@np.array([.2126,.7152,.0722]))>65)
    rgb[rock]*=np.array([.88,.93,.94])
    rgb[crystal]=rgb[crystal]*.78+np.array([4,9,12])
    # Keep the middle's existing quiet gradient. Quantize only the decorative
    # frame, and keep crystals in a separate palette (a global quantizer can
    # incorrectly absorb the small cyan regions into the dominant blue rock).
    a[:,:,:3]=np.clip(rgb,0,255).astype('uint8')
    rock_colors=[(24,31,51),(35,43,72),(49,61,96),(66,82,123),(94,112,149)]
    rp=np.array(palette(Image.fromarray(a),rock_colors,smooth=False))
    cp=np.array(palette(Image.fromarray(a),[(22,70,81),(26,105,115),(35,143,139),(65,175,155),(116,202,182)],smooth=False))
    a[rock,:3]=rp[rock,:3];a[crystal,:3]=cp[crystal,:3]
    return clean(Image.fromarray(a))


def block(im,name):
    role='reward' if 'reward' in name else 'target' if 'target' in name else 'ordinary'
    # Five source-relative value bands retain existing bevel shapes and alpha.
    a=np.array(im); rgb=a[:,:,:3].astype(float); lum=rgb@np.array([.2126,.7152,.0722])
    l,t,r,b=im.getbbox(); face=np.median(lum[t+80:b-80,l+80:r-80])
    bands=np.digitize(lum-face,[-48,-18,12,37])
    pal=np.array(GOLD if role=='reward' else TEAL if role=='target' else ROCK,dtype='uint8')
    a[:,:,:3]=pal[bands]
    # Rebuild only the straight spans of the old 80px nine-slice. Corners remain
    # source-derived. Modal scanlines remove stretched flecks / copy seams.
    for yy in range(t,min(t+50,b)):
        vals,c=np.unique(bands[yy,l+80:r-80],return_counts=True)
        if len(vals): a[yy,l+80:r-80,:3]=pal[vals[c.argmax()]]
    for yy in range(max(t,b-48),b):
        vals,c=np.unique(bands[yy,l+80:r-80],return_counts=True)
        if len(vals): a[yy,l+80:r-80,:3]=pal[min(2,vals[c.argmax()])]
    for xx in list(range(l,min(l+48,r)))+list(range(max(l,r-48),r)):
        vals,c=np.unique(bands[t+80:b-80,xx],return_counts=True)
        if len(vals): a[t+80:b-80,xx,:3]=pal[vals[c.argmax()]]
    # The number field is a single clean mineral plane, no duplicated scars.
    a[t+50:b-48,l+48:r-48,:3]=pal[2]
    # Right/bottom facing bevels cannot carry the brightest left-lit band.
    a[b-22:b,:,:3]=pal[np.minimum(bands[b-22:b,:],1)]
    a[:,r-20:r,:3]=pal[np.minimum(bands[:,r-20:r],1)]
    a[:,:,3]=np.array(im)[:,:,3]
    return clean(Image.fromarray(a))


def detail(im,name):
    if name.startswith('crack'):
        a=np.array(im); visible=a[:,:,3]>0
        # Retain every traced branch; dark slit and restrained cool rock lip.
        lip=(a[:,:,0]>100)&visible
        a[visible,:3]=[31,51,68];a[lip,:3]=[127,163,172]
        # Keep the reviewed light/heavy opacity and every transparent pixel.
        return clean(Image.fromarray(a))
    if name=='ball':
        # Preserve the original stepped circular silhouette; replace its soft,
        # sunken-looking gradient with four hard light planes and one glint.
        out=palette(im,[(161,111,39),(222,169,57),(250,217,100),(255,243,173),(255,255,233)])
        a=np.array(out); yy,xx=np.mgrid[:im.height,:im.width]
        mask=(a[:,:,3]>0); cx,cy=128,128
        a[mask,:3]=[250,217,100]
        a[mask & (xx+yy>326),:3]=[222,169,57]
        a[mask & (xx+yy<171),:3]=[255,243,173]
        a[mask & (xx>49)&(xx<94)&(yy>40)&(yy<67),:3]=[255,255,233]
        a[mask & (xx>58)&(xx<85)&(yy>31)&(yy<85),:3]=[255,255,233]
        # Deepest outside pixels remain an outline, never a dark central hole.
        alpha=im.getchannel('A'); inner=np.array(alpha.filter(ImageFilter.MinFilter(15)))>0
        a[mask & ~inner,:3]=[176,126,43]
        return clean(Image.fromarray(a))
    if name=='launcher':
        return palette(im,GOLD)
    if name=='badge_reward':
        return palette(im,GOLD)
    if name=='badge_target':
        return palette(im,TEAL)
    if name=='fragment':
        out=palette(im,ROCK);a=np.array(out)
        # Keep the original major planes; mode cleanup removes tiny JPEG pits.
        a[:,:,3]=np.array(im)[:,:,3]
        return clean(Image.fromarray(a))
    if name=='fx_spark':
        out=im.copy(); a=np.array(out)
        a[a[:,:,3]>0,:3]=[246,207,125]
        # Clarify the existing center, retain the asymmetrical ray silhouette.
        yy,xx=np.mgrid[:im.height,:im.width]; mask=(a[:,:,3]>0)&(abs(xx-106)+abs(yy-100)<37)
        a[mask,:3]=[255,255,233]
        return clean(Image.fromarray(a))
    raise ValueError(name)


def logo(im):
    # Keep the original arch silhouette and all three crystal silhouettes.
    a=np.array(im); rgb=a[:,:,:3].astype(float)
    envelope=Image.new('L',im.size);ed=ImageDraw.Draw(envelope)
    for pts in [
        [(234,147),(269,94),(285,90),(304,121),(303,170),(269,195),(237,209)],
        [(296,176),(301,132),(327,77),(338,64),(366,101),(365,132),(344,179),(338,231),(318,241),(299,216)],
        [(347,158),(369,139),(384,137),(406,168),(419,217),(405,233),(380,218),(360,190)],
    ]:ed.polygon(pts,fill=255)
    crystal=(np.array(envelope)>0)&(a[:,:,3]>0)
    rock=palette(im,ROCK); out=rock.copy();d=ImageDraw.Draw(out)
    # Individual planes across the old repeated rock instances, intentionally
    # asymmetric. These repaint inside the source alpha, not a new arch design.
    faces=[([(123,166),(158,113),(182,129),(162,180),(139,193)],ROCK[2]),
           ([(159,105),(200,57),(241,51),(212,106),(184,128)],ROCK[3]),
           ([(236,71),(274,25),(313,25),(307,80),(268,98)],ROCK[4]),
           ([(326,25),(376,29),(397,51),(389,95),(345,74)],ROCK[3]),
           ([(400,57),(447,77),(480,113),(447,122),(418,94)],ROCK[3]),
           ([(456,122),(488,114),(529,161),(504,188),(477,171)],ROCK[2]),
           ([(143,213),(170,198),(203,214),(184,255),(153,252)],ROCK[3]),
           ([(453,190),(493,190),(505,222),(479,265),(457,246)],ROCK[2])]
    for pts,c in faces:d.polygon(pts,fill=c)
    # Lower joints: broad, connected contact planes rather than three squashed
    # copies of the same rock. Original lower silhouette is used as the mask.
    d.polygon([(198,246),(248,231),(283,246),(268,274),(229,283),(205,268)],fill=ROCK[3])
    d.polygon([(229,283),(268,274),(283,246),(283,281),(251,294)],fill=ROCK[1])
    d.polygon([(279,262),(324,249),(368,261),(363,282),(325,300),(290,289)],fill=ROCK[3])
    d.polygon([(290,289),(325,300),(363,282),(360,296),(327,308),(294,298)],fill=ROCK[1])
    d.polygon([(360,248),(408,232),(453,242),(436,270),(391,285)],fill=ROCK[3])
    d.polygon([(391,285),(436,270),(453,242),(455,268),(410,292)],fill=ROCK[1])
    # Align new internal planes to a four-pixel step, without moving the source
    # outer silhouette or the opening. Shared crystal facets are similarly cleaned.
    stepped=out.resize((160,80),Image.Resampling.NEAREST).resize(im.size,Image.Resampling.NEAREST)
    p=np.array(stepped);p[:,:,3]=a[:,:,3]
    # Re-establish the three original crystal light planes locally. RGB nearest
    # quantization of the extracted JPEG facets creates mottling, so use explicit
    # broad facets clipped to the original crystal chroma mask instead.
    crystal_layer=Image.new('RGB',im.size,TEAL[1]);cd=ImageDraw.Draw(crystal_layer)
    for pts,color in [
        ([(235,146),(271,94),(285,90),(279,142),(251,166)],TEAL[3]),
        ([(251,166),(279,142),(303,124),(299,171),(269,194),(237,208)],TEAL[2]),
        ([(301,132),(338,64),(329,128),(318,146),(317,230),(299,212)],TEAL[2]),
        ([(329,128),(338,64),(365,101),(361,129)],TEAL[3]),
        ([(348,159),(370,138),(384,137),(401,157),(384,184),(365,176)],TEAL[3]),
        ([(365,176),(384,184),(409,171),(417,217),(404,232),(380,214)],TEAL[2]),
    ]:cd.polygon(pts,fill=color)
    teal=np.array(crystal_layer.resize((160,80),Image.Resampling.NEAREST).resize(im.size,Image.Resampling.NEAREST))
    p[crystal,:3]=teal[crystal,:3]
    p[:,:,:3]=np.array(Image.fromarray(p[:,:,:3]).filter(ImageFilter.ModeFilter(3)))
    return clean(Image.fromarray(p))


def run():
    ap=argparse.ArgumentParser();ap.add_argument('--group',choices=list(GROUPS),required=True);ap.add_argument('--apply',action='store_true');args=ap.parse_args()
    dest=SCRATCH/('candidate-'+args.group);dest.mkdir(parents=True,exist_ok=True)
    previous=json.loads((dest/'validation.json').read_text(encoding='utf-8')) if (dest/'validation.json').exists() else []
    known={v['file']:v['sha256'] for v in previous}
    records=[]
    for name in GROUPS[args.group]:
        original=source(name)
        result=background(original) if args.group=='background' else block(original,name) if args.group=='blocks' else logo(original) if args.group=='logo' else detail(original,name)
        assert result.size==original.size
        aa=np.array(result); ba=np.array(original)
        assert np.array_equal(aa[:,:,3],ba[:,:,3]),name+' alpha silhouette changed'
        assert not np.any(aa[aa[:,:,3]==0,:3]),name+' hidden matte'
        assert np.any(aa!=ba),name+' unchanged'
        result.save(dest/(name+'.png'))
        if args.apply:
            current=Image.open(OUT/(name+'.png')).convert('RGBA')
            assert np.array_equal(np.array(current),ba) or np.array_equal(np.array(current),aa) or hashlib.sha256((OUT/(name+'.png')).read_bytes()).hexdigest()==known.get(name+'.png'),name+' unreviewed local edit'
            result.save(OUT/(name+'.png'))
        pair=Image.new('RGB',(original.width*2,original.height),'#182330');pair.paste(original,(0,0),original);pair.paste(result,(original.width,0),result);pair.save(dest/(name+'-pair.png'))
        records.append({'file':name+'.png','size':list(result.size),'alpha_preserved':True,'changed_rgb_pixels':int(np.any(aa[:,:,:3]!=ba[:,:,:3],axis=2).sum()),'sha256':hashlib.sha256((dest/(name+'.png')).read_bytes()).hexdigest()})
    (dest/'validation.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
    print(json.dumps(records,indent=2))

if __name__=='__main__':run()
