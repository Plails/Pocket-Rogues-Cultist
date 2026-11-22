# Bones lying about the floor, in the game's own way (its TileBones / DD_BonesAndGrass): muted, dirty bone colours,
# no black outline - a soft dark edge instead - so they lie IN the floor rather than on it (the user: the old ones looked
# bad). 20x16 at 24 px a tile, three kinds:
#   occ_bones_0 - two long bones, one across the other, a few chips
#   occ_bones_1 - a skull on its side, its jaw fallen by it
#   occ_bones_2 - a few ribs and a short bone
#   usage: python bones_art.py <out dir>
import sys, os, math
from PIL import Image
out = sys.argv[1]; os.makedirs(out, exist_ok=True)
L, M, D = (168, 156, 132), (132, 120, 100), (90, 80, 68)
W, H = 20, 16
def h2(x, y, s): return (math.sin(x * 12.9898 + y * 78.233 + s * 37.719) * 43758.5453) % 1.0
def new(): im = Image.new("RGBA", (W, H), (0, 0, 0, 0)); return im, im.load()
def put(p, x, y, c, a=225):
    x, y = int(round(x)), int(round(y))
    if 0 <= x < W and 0 <= y < H: p[x, y] = c + (a,)
def bone(p, x0, y0, x1, y1):
    n = int(max(abs(x1 - x0), abs(y1 - y0))) + 1
    for k in range(n):
        t = k / max(1, n - 1); x, y = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
        put(p, x, y, L if k % 3 else M)
        put(p, x + 0.6, y + 0.6, M)                                           # its shaded underside
    for (ex, ey) in ((x0, y0), (x1, y1)):                                     # the knobbed ends
        put(p, ex - 0.6, ey, L); put(p, ex + 0.6, ey - 0.6, L); put(p, ex, ey + 0.8, M)
def finish(im, p, name, seed):
    # dirt: a few bone pixels darkened, then a soft dark edge (not a black line)
    for y in range(H):
        for x in range(W):
            if p[x, y][3] and h2(x, y, seed) < 0.18: p[x, y] = D + (p[x, y][3],)
    edge = [(x, y) for y in range(H) for x in range(W) if p[x, y][3] == 0 and any(0 <= x + a < W and 0 <= y + b < H and p[x + a, y + b][3] > 0 for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for q in edge: p[q] = (24, 18, 18, 95)
    im.save(os.path.join(out, name + ".png"))
    return im

im, p = new()
bone(p, 3, 11, 15, 5); bone(p, 6, 4, 14, 12)
for (x, y) in ((17, 11), (2, 5), (11, 14)): put(p, x, y, M)
a = finish(im, p, "occ_bones_0", 1)

im, p = new()
cx, cy = 8, 7
for y in range(H):
    for x in range(W):
        ex, ey = (x - cx) / 4.2, (y - cy) / 3.6
        if ex * ex + ey * ey <= 1: put(p, x, y, L if ex + ey < -0.2 else M)
for (x, y) in ((6, 7), (7, 7), (9, 6)): put(p, x, y, (40, 32, 30), 240)        # the eye holes
put(p, 8, 9, (60, 50, 44), 240)                                                 # the nose
for x in range(11, 13): put(p, x, 9, D)                                         # the teeth line
for k in range(5): put(p, 13 + k, 12 + (k % 2), M if k % 2 else L)             # the jaw, fallen apart
put(p, 13, 11, L); put(p, 17, 11, L)
b = finish(im, p, "occ_bones_1", 2)

im, p = new()
for i in range(4):                                                              # ribs: little curved bones
    for k in range(6):
        t = k / 5; x = 4 + i * 3 + t * 2; y = 4 + math.sin(t * math.pi) * 2.2 + t * 5
        put(p, x, y, L if k < 4 else M)
bone(p, 5, 13, 11, 13)
put(p, 16, 6, M); put(p, 17, 7, L)
c = finish(im, p, "occ_bones_2", 3)

sh = Image.new("RGBA", (W * 3 + 8, H), (70, 56, 50, 255))
for i, im in enumerate((a, b, c)): sh.alpha_composite(im, (i * (W + 4), 0))
sh.resize((sh.width * 8, sh.height * 8), Image.NEAREST).save(os.path.join(out, "bones_preview.png"))
print("bones")
