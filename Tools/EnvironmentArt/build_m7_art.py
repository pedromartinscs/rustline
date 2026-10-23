"""Rustline M7 art: exact structural pixels and canonical conversion of original generated props.

Run from repository root: python Tools/EnvironmentArt/build_m7_art.py
Source PNGs belong in Assets/Art/Source/Environment/M7; no network dependencies.
"""
from pathlib import Path
import hashlib
import re
import uuid
from PIL import Image, ImageDraw, ImageOps

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/Art/Environment/Hardening'
SOURCE = ROOT / 'Assets/Art/Source/Environment/M7'
PALETTE = [tuple(bytes.fromhex(h)) for h in re.findall(r'`#([0-9A-F]{6})`', (ROOT/'docs/PALETTE.md').read_text())[:28]]


def save(image, name, legacy=None):
    OUT.mkdir(parents=True, exist_ok=True)
    path = OUT / (name + '.png')
    pixels = list(image.getdata())
    assert all(p[3] in (0, 255) and (p[3] == 0 or p[:3] in PALETTE) for p in pixels), name
    image.save(path)
    # Existing legacy dressing validators require their authored cropped slice/pivot.
    # Clone that import geometry once under a new, deterministic asset GUID.
    if legacy and not path.with_suffix('.png.meta').exists():
        old = ROOT / ('Assets/Art/Environment/' + legacy + '.png.meta')
        content = old.read_text().replace(Path(legacy).name, name)
        guid = uuid.uuid5(uuid.NAMESPACE_URL, path.relative_to(ROOT).as_posix()).hex
        content = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + guid, content, count=1)
        path.with_suffix('.png.meta').write_text(content)
    print(name, image.size, hashlib.sha256(path.read_bytes()).hexdigest()[:16])


def steel(width, height, mirror=False):
    """Continuous load-bearing edge, closed ribs, bolts, sparse intact patina; no cracks."""
    im = Image.new('RGBA', (width, height), PALETTE[1] + (255,))
    d = ImageDraw.Draw(im)
    def rect(box, c): d.rectangle(box, fill=PALETTE[c] + (255,))
    rect((0, 0, 3, height-1), 2)
    rect((4, 0, width-7, height-1), 3)
    rect((5, 0, 5, height-1), 4)
    rect((width-6, 0, width-1, height-1), 2)
    rect((width-2, 0, width-1, height-1), 5)
    for y in range(0, height, 32):
        rect((7,y+3,width-9,min(height-1,y+29)),1)
        rect((8,y+4,width-10,min(height-1,y+5)),4)
        for rib in range(y+10,min(height,y+28),6):
            rect((9,rib,width-11,rib+1),2)
            rect((9,rib,width-11,rib),3)
        rect((3,y+1,width-4,y+2),4)
        for x in (2,width-5):
            rect((x,y+6,x+2,y+8),0); rect((x,y+6,x+1,y+6),5)
            rect((x,y+24,x+2,y+26),0); rect((x,y+24,x+1,y+24),4)
        # A few small bounded rust scuffs, never an open edge or rubble silhouette.
        if y % 64 == 0:
            rect((6,y+21,8,y+22),8); rect((6,y+21,7,y+21),9)
    return ImageOps.mirror(im) if mirror else im


def floor(variant):
    im = Image.new('RGBA',(48,32), PALETTE[1]+(255,)); d=ImageDraw.Draw(im)
    def rect(box,c): d.rectangle(box,fill=PALETTE[c]+(255,))
    rect((0,0,47,1),5); rect((0,2,47,4),4); rect((0,5,47,7),2)
    rect((0,8,47,27),3); rect((0,28,47,31),2)
    for x in range(0,48,16):
        rect((x,8,x+1,27),4); rect((x+3,11,x+13,24),1)
        rect((x+5,14,x+11,15),2); rect((x+5,19,x+11,20),2)
        rect((x+2,9,x+3,10),5); rect((x+2,25,x+3,26),4)
    if variant in ('left','right'):
        x=2 if variant=='left' else 42
        rect((x,7,x+3,29),4); rect((x,12,x+2,14),9)
    if variant=='mid_b': rect((22,6,27,7),8)
    return im


def convert_prop(name, size):
    path = SOURCE / (name + '.png')
    if not path.exists():
        print('Waiting for original source:', path.relative_to(ROOT)); return
    source = Image.open(path).convert('RGBA')
    # Use generated alpha for extraction. Discard only partial edge alpha, then retain
    # one connected assembly at native density with no filtered resampling/dithering.
    alpha = source.getchannel('A').point(lambda a: 255 if a >= 128 else 0)
    box = alpha.getbbox()
    assert box, name
    source.putalpha(alpha)
    source = source.crop(box).resize(size,Image.Resampling.NEAREST)
    cache = {}
    # Keep environment accents to the quieter metal/rust ramp and Hazard Yellow.
    # Warning Orange is gameplay feedback and has a pre-existing GPU roundtrip
    # difference in this project; do not propagate that color into new scenery.
    prop_palette = [p for i,p in enumerate(PALETTE) if i != 13]
    output = []
    for r,g,b,a in source.getdata():
        if not a: output.append((0,0,0,0)); continue
        rgb = (r,g,b)
        if rgb not in cache:
            cache[rgb] = min(prop_palette,key=lambda p:sum((p[i]-rgb[i])**2 for i in range(3)))
        output.append(cache[rgb]+(255,))
    result=Image.new('RGBA',size); result.putdata(output); save(result,name)


if __name__ == '__main__':
    save(steel(33,240),'reinforced_shaft_a','Architecture/ServiceShaft/shaft_wall_a')
    save(steel(36,240,True),'reinforced_shaft_b','Architecture/ServiceShaft/shaft_wall_b')
    for part in ('top','mid','bottom'):
        save(steel(50,150),'reinforced_wall_'+part,'Architecture/Wall/wall_edge_'+part)
    for part in ('left','mid_a','mid_b','right'):
        save(floor(part),'reinforced_floor_'+part,'Architecture/Floor/floor_edge_'+part)
    plate = steel(32,32).resize((16,16),Image.Resampling.NEAREST)
    save(plate,'reinforced_plate_right')
    save(ImageOps.mirror(plate),'reinforced_plate_left')
    frame = Image.new('RGBA',(32,64),(0,0,0,0)); d=ImageDraw.Draw(frame)
    for x in (1,25):
        d.rectangle((x,0,x+5,63),fill=PALETTE[2]+(255,))
        d.line((x,0,x,63),fill=PALETTE[4]+(255,))
    for y in (0,32):
        d.rectangle((1,y,30,y+4),fill=PALETTE[3]+(255,))
        d.line((7,y+5,24,y+30),fill=PALETTE[2]+(255,),width=3)
        for x in (3,27): d.point((x,y+2),fill=PALETTE[5]+(255,))
    save(frame,'service_column')
    for name,size in [('pressure_reclaimer',(160,192)),('exhaust_station',(224,144)),('conduit_rack',(256,128))]:
        convert_prop(name,size)
