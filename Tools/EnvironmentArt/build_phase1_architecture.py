"""Author the transfer bay's rectilinear pixel architecture from canonical colors.

Run from the repository root. No random noise, filtering, generated source, or external art.
The wall is deliberately a bounded scene dressing asset, never a replacement terrain atlas.
"""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/Art/Environment/TransferBay'
P = [tuple(map(int, line.split()[:3])) for line in
     (ROOT / 'Assets/Art/Palette/rustline.gpl').read_text().splitlines()
     if line.strip() and line.strip()[0].isdigit()]

# Coordinates below are source pixels; wall origin is world (-34, -16).
W, H = 1024, 768
wall = Image.new('RGBA', (W, H), P[1] + (255,))
d = ImageDraw.Draw(wall)


def rect(box, color):
    d.rectangle(box, fill=P[color] + (255,))


def line(points, color, width=1):
    d.line(points, fill=P[color] + (255,), width=width)


def world(x, y):
    return round((x + 34) * 16), round((32 - y) * 16)


def panel(x, y, w, h):
    rect((x, y, x+w-1, y+h-1), 0)
    rect((x+2, y+2, x+w-3, y+h-3), 1)
    line([(x+2, y+h-3), (x+w-3, y+h-3), (x+w-3, y+2)], 2)
    # Sparse plate seams and inspection bolts, no camouflage or pixel noise.
    for px in (x+6, x+w-7):
        for py in (y+6, y+h-7):
            rect((px, py, px+1, py+1), 2)


def column(x):
    rect((x, 0, x+17, H-1), 0)
    rect((x+2, 0, x+15, H-1), 2)
    rect((x+5, 0, x+12, H-1), 1)
    rect((x+3, 0, x+3, H-1), 3)
    for y in range(24, H, 96):
        rect((x, y, x+17, y+9), 1)
        rect((x+1, y+1, x+16, y+2), 2)
        for px in (x+3, x+13):
            rect((px, y+5, px+1, y+6), 3)


def pipe(x, top, bottom, width=12):
    rect((x, top, x+width-1, bottom), 0)
    rect((x+1, top, x+width-2, bottom), 2)
    rect((x+2, top, x+3, bottom), 3)
    rect((x+width-4, top, x+width-2, bottom), 1)
    for y in range(top+12, bottom-5, 64):
        rect((x-2, y, x+width+1, y+5), 1)
        rect((x-1, y, x+width, y), 3)
        rect((x-1, y+4, x+width, y+5), 0)


FONT = {
    'A':['01110','10001','10001','11111','10001','10001','10001'],
    'B':['11110','10001','10001','11110','10001','10001','11110'],
    'E':['11111','10000','10000','11110','10000','10000','11111'],
    'F':['11111','10000','10000','11110','10000','10000','10000'],
    'G':['01111','10000','10000','10111','10001','10001','01110'],
    'I':['111','010','010','010','010','010','111'],
    'L':['10000','10000','10000','10000','10000','10000','11111'],
    'N':['10001','11001','11001','10101','10011','10011','10001'],
    'R':['11110','10001','10001','11110','10100','10010','10001'],
    'S':['01111','10000','10000','01110','00001','00001','11110'],
    'T':['11111','00100','00100','00100','00100','00100','00100'],
    'V':['10001','10001','10001','10001','10001','01010','00100'],
    'Y':['10001','10001','01010','00100','00100','00100','00100'],
    '0':['01110','10001','10011','10101','11001','10001','01110'],
    '1':['010','110','010','010','010','010','111'],
    ' ':['000']*7,
    '/':['00001','00001','00010','00100','01000','10000','10000'],
}


def text(x, y, value, color=3, scale=1):
    for char in value:
        glyph = FONT[char]
        for row, bits in enumerate(glyph):
            for col, bit in enumerate(bits):
                if bit == '1':
                    rect((x+col*scale, y+row*scale,
                          x+(col+1)*scale-1, y+(row+1)*scale-1), color)
        x += (len(glyph[0])+2)*scale


# Recessed steel plates form large, legible structural bays.
for x in range(16, W-16, 128):
    for y in range(16, H-16, 128):
        panel(x, y, 112, 112)

# Large central crane well: stepped metal surround, inner dark service recess.
x0, y0 = world(-12, 23)
x1, y1 = world(12, -1)
rect((x0-8, y0-8, x1+8, y1), 2)
rect((x0-5, y0-5, x1+5, y1), 0)
rect((x0, y0, x1, y1), 1)
for x in (x0+8, x1-13):
    pipe(x, y0+4, y1, 6)
for y in range(y0+44, y1, 80):
    line([(x0+24,y), (x1-24,y)], 0, 2)
    line([(x0+24,y+2), (x1-24,y+2)], 2)
    # Braces remain lower contrast than the existing hero machinery.
    line([(x0+24,y-36), (x0+64,y)], 2, 3)
    line([(x1-24,y-36), (x1-64,y)], 2, 3)

for wx in (-32, -24, -14, 14, 24, 29):
    column(world(wx, 32)[0])

# Flanged pipe stacks in side service bays; no repeated machinery sprites.
for wx, top in ((-30,27), (-28,27), (-18,21), (18,27), (20,27), (26,19)):
    px, py = world(wx, top)
    pipe(px, py, H-1, 10 if wx in (-28,20) else 14)

# Overhead load-bearing crane track, visibly connected to the existing uprights.
left, rail_y = world(-24, 22)
right, _ = world(25, 22)
rect((left, rail_y, right, rail_y+23), 0)
rect((left, rail_y+2, right, rail_y+5), 3)
rect((left, rail_y+6, right, rail_y+18), 2)
rect((left, rail_y+19, right, rail_y+23), 1)
for x in range(left+8, right-8, 32):
    line([(x,rail_y+18), (x+12,rail_y+6), (x+24,rail_y+18)], 1, 2)
    rect((x+10,rail_y+3,x+11,rail_y+4), 4)

# Sparse cold fixtures with stepped, palette-authored light on the backing plate.
for wx in (-19, -5, 9, 23):
    x,y = world(wx, 16)
    rect((x-5,y-2,x+29,y+15), 2)
    rect((x-8,y-5,x+32,y+4), 0)
    rect((x-5,y-4,x+29,y-3), 3)
    rect((x,y-1,x+24,y), 18)
    rect((x+2,y-1,x+21,y-1), 19)
    rect((x-2,y+1,x-1,y+4), 3)
    rect((x+25,y+1,x+26,y+4), 3)

# Inset service hatches. They read as background because the edges stay dark.
for wx in (-22, 16):
    x,y = world(wx, 8)
    panel(x,y,64,112)
    rect((x+8,y+10,x+55,y+104), 0)
    rect((x+10,y+12,x+53,y+102), 1)
    for vy in range(y+24,y+90,10):
        rect((x+16,vy,x+47,vy+2), 2)
    rect((x+49,y+92,x+51,y+97), 3)
    rect((x+15,y+108,x+48,y+109), 2)

# Painted wayfinding belongs to the architecture, subdued behind combat silhouettes.
x,y = world(-6, 26)
text(x,y,'01',4,3)
text(x+55,y+1,'SALVAGE',3)
text(x+55,y+12,'TRANSFER',3)
for wx in (-25, 25):
    x,y = world(wx, 10)
    text(x,y,'01',3,2)

# Bounded oxidation at joints, never an all-over camouflage texture.
for wx, wy in ((-24,22), (14,22), (-32,2), (24,-4)):
    x,y = world(wx,wy)
    rect((x+4,y+5,x+7,y+22),8)
    rect((x+4,y+5,x+6,y+8),9)

OUT.mkdir(parents=True, exist_ok=True)
assert all(a == 255 and (r,g,b) in P for r,g,b,a in wall.getdata())
wall.save(OUT / 'transfer_service_wall.png')
print('transfer_service_wall.png', wall.size, 'native RGBA32: 3 MiB')
