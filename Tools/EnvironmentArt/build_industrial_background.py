"""Small repeatable industrial planes and a 16-module architectural atlas, Canonical 28.

Python/Pillow already used by the existing art pipeline. No third-party source art.
Run from the repo root. Existing Phase 1/2 assets are never modified.
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageOps

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/Art/Environment/IndustrialBackground'
P = [tuple(map(int, line.split()[:3])) for line in
     (ROOT/'Assets/Art/Palette/rustline.gpl').read_text().splitlines()
     if line.strip() and line.strip()[0].isdigit()]


def color(i): return P[i] + (255,)


def save(im, name):
    if name != 'architecture_modules':
        # Matching opposite edge texels are a simple, auditable seam contract.
        for y in range(im.height): im.putpixel((im.width-1,y), im.getpixel((0,y)))
        for x in range(im.width): im.putpixel((x,im.height-1), im.getpixel((x,0)))
    assert all(a in (0,255) and (a == 0 or (r,g,b) in P) for r,g,b,a in im.getdata())
    OUT.mkdir(parents=True,exist_ok=True)
    im.save(OUT/(name+'.png'))
    print(name, im.size, 'RGBA32 bytes', im.width*im.height*4)


def far():
    im=Image.new('RGBA',(512,512),color(0)); d=ImageDraw.Draw(im)
    # Large closed mechanical forms, never amorphous silhouettes. The periodic rails and
    # columns cross opposite edges identically; the end rows/columns are exact matches.
    for x,w in ((32,32),(224,56),(448,20)):
        d.rectangle((x,0,x+w,511),fill=color(1))
        d.rectangle((x+6,0,x+w-6,511),fill=color(0))
        for y in range(0,512,128):
            d.rectangle((x-4,y+24,x+w+4,y+38),fill=color(1))
    for y in (64,320):
        d.rectangle((0,y,511,y+16),fill=color(1))
        for x in range(0,512,64):
            d.line((x,y+17,x+48,y+72),fill=color(1),width=5)
    for box in ((90,104,192,270),(312,368,412,482)):
        d.rectangle(box,fill=color(1))
        x,y,r,b=box
        d.rectangle((x+8,y+12,r-8,b-12),fill=color(0))
        for vy in range(y+28,b-12,32): d.rectangle((x+10,vy,r-10,vy+4),fill=color(1))
    return im


def mid():
    im=Image.new('RGBA',(512,512),(0,0,0,0)); d=ImageDraw.Draw(im)
    for x,w in ((8,18),(172,28),(388,14)):
        d.rectangle((x,0,x+w,511),fill=color(0))
        d.rectangle((x+2,0,x+w-2,511),fill=color(2))
        d.rectangle((x+6,0,x+w-6,511),fill=color(1))
        for y in range(16,512,96):
            d.rectangle((x-3,y,x+w+3,y+7),fill=color(1))
            d.line((x-2,y,x+w+2,y),fill=color(2),width=1)
    # Two remote wall bays with openings: far infrastructure stays visible between them.
    for x,y,w,h in ((40,52,112,180),(228,284,144,192)):
        d.rectangle((x,y,x+w,y+h),fill=color(0))
        d.rectangle((x+3,y+3,x+w-3,y+h-3),fill=color(1))
        d.line((x+3,y+h-3,x+w-3,y+h-3,x+w-3,y+3),fill=color(2))
        for vy in range(y+30,y+h-12,30): d.line((x+12,vy,x+w-12,vy),fill=color(0),width=2)
    for y in (248,496):
        d.rectangle((0,y,511,y+6),fill=color(0))
        d.line((0,y+1,511,y+1),fill=color(2),width=2)
    return im


def module(kind):
    im=Image.new('RGBA',(128,128),(0,0,0,0)); d=ImageDraw.Draw(im)
    def rect(box,c): d.rectangle(box,fill=color(c))
    def line(points,c,width=1): d.line(points,fill=color(c),width=width)
    def panel(inset=5):
        rect((0,0,127,127),1)
        rect((inset,inset,127-inset,127-inset),0)
        rect((inset+2,inset+2,125-inset,125-inset),1)
        line((inset+2,125-inset,125-inset,125-inset,125-inset,inset+2),2)
        for x in (inset+5,122-inset):
            for y in (inset+5,122-inset): rect((x,y,x+1,y+1),2)
    def pipe(x,w=12):
        rect((x,0,x+w-1,127),0); rect((x+1,0,x+w-2,127),2)
        rect((x+2,0,x+3,127),3); rect((x+w-4,0,x+w-2,127),1)
        for y in (24,88):
            rect((x-2,y,x+w+1,y+6),0); line((x-1,y,x+w,y),3)
    if kind in (0,1,2,3,5,10,11,14,15): panel(3 if kind==15 else 7)
    if kind==0:
        line((20,62,107,62),0,2)
    elif kind==1:
        rect((24,16,103,111),0); rect((27,19,100,108),1)
        line((28,108,100,108,100,19),2)
    elif kind==2:
        rect((22,8,105,122),0); rect((26,12,101,122),2)
        rect((30,16,97,122),1); line((64,18,64,121),0,3)
        for y in (24,74,116): line((33,y,94,y),2,2)
        rect((12,68,17,92),0); rect((13,70,14,72),18)
    elif kind==3:
        rect((18,22,109,102),0)
        for y in range(28,98,8): rect((24,y,103,y+2),2)
    elif kind==4:
        for x,w in ((18,10),(50,18),(96,12)): pipe(x,w)
    elif kind==5:
        for x in (16,20,24,28,88,92):
            line((x,0,x,127),0,3); line((x,0,x,127),2)
        for y in (32,96): rect((12,y,98,y+3),2)
    elif kind in (6,7):
        rect((0,0,17,127),0); rect((2,0,15,127),2); rect((6,0,12,127),1)
        for y in (0,64):
            line((0,y,58,y),1,6); line((16,y+4,56,y+44),2,3)
        if kind==7: im=ImageOps.mirror(im)
    elif kind==8:
        for box in ((0,0,127,8),(0,119,127,127),(0,0,8,127),(119,0,127,127)): rect(box,0)
        line((10,127,10,10,127,10),2,2)
        line((0,116,116,116,116,0),1,3)
    elif kind==9:
        rect((0,48,127,74),0); rect((0,50,127,53),3); rect((0,54,127,70),1)
        for x in range(0,128,32): line((x,70,x+16,54,x+31,70),2,2)
        line((0,72,127,72),2,2)
    elif kind==10:
        rect((18,12,110,116),0); rect((23,17,105,111),2)
        rect((26,20,102,108),1)
        for y in range(32,100,14): line((36,y,92,y),0,3)
        rect((94,91,97,98),3)
    elif kind==11:
        pipe(16,8); rect((52,40,110,96),0); rect((55,43,107,93),2)
        rect((60,48,102,75),1); rect((64,52,86,53),18)
        line((76,97,76,127),2,3)
    elif kind==12:
        rect((0,80,127,116),0); rect((0,83,127,108),1)
        line((0,83,127,83),2,3); line((0,112,127,112),2,2)
        for x in range(8,128,24): rect((x,90,x+10,101),0)
    elif kind==13:
        rect((52,0,77,127),0); rect((54,0,75,127),2); rect((59,0,70,127),1)
        for y in (20,84): rect((48,y,81,y+9),1); line((49,y,80,y),3)
    elif kind==14:
        rect((16,16,111,111),0); rect((19,19,108,108),2); rect((22,22,105,105),1)
        rect((32,44,95,83),0); line((64,45,64,82),2,2)
        for x in (28,98): rect((x,28,x+2,30),3)
    # Joint corrosion only on two authored modules; no all-over noise.
    if kind in (4,13): rect((54,23,56,35),8); rect((54,23,55,26),9)
    return im


if __name__=='__main__':
    save(far(),'far_infrastructure')
    save(mid(),'mid_service_network')
    atlas=Image.new('RGBA',(512,512),(0,0,0,0))
    for i in range(16): atlas.paste(module(i),((i%4)*128,(i//4)*128))
    save(atlas,'architecture_modules')
