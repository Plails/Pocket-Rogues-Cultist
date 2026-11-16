# a few more things for the sanctum's rooms, in the same grainy dark style (1 px black outline):
#   occ_target  - a straw practice target on a wooden stand seen from above, painted red rings, bolts stuck in it
#                 (its face towards the bottom of the picture)

# usage: python extra_art.py <art dir>
import sys, os, random, math
from PIL import Image
out = sys.argv[1]; random.seed(7)
OL = (0, 0, 0, 255)
def outline(im):
    w, h = im.size; px = im.load()
    ring = [(x, y) for y in range(h) for x in range(w) if px[x, y][3] == 0 and any(0 <= x + dx < w and 0 <= y + dy < h and px[x + dx, y + dy][3] > 0 and px[x + dx, y + dy] != OL for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for q in ring: px[q] = OL
def grain(c, k=10):
    d = random.randint(-k, k); return tuple(max(0, min(255, v + d)) for v in c[:3]) + (255,)

# the target: a thick straw disc (an ellipse seen from above at a slant), the stand's two legs behind it
W, H = 38, 20
im = Image.new("RGBA", (W, H), (0, 0, 0, 0)); px = im.load()
WOOD = [(42, 28, 18), (60, 42, 26), (78, 56, 34)]
for x in list(range(7, 10)) + list(range(28, 31)):
    for y in range(1, 9): px[x, y] = grain(WOOD[1 + (x % 2)], 6)
for x in range(6, 32):
    for y in range(3, 5): px[x, y] = grain(WOOD[0], 5)
cx, cy, rx, ry = 18.5, 11.5, 16, 7
STRAW = [(118, 96, 52), (146, 120, 66), (172, 146, 84)]
for y in range(H):
    for x in range(W):
        e = ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2
        if e <= 1:
            r = math.sqrt(e)
            if r < 0.22: c = (150, 24, 26)
            elif r < 0.42: c = (206, 196, 168)
            elif r < 0.62: c = (130, 22, 24)
            elif r < 0.8: c = (196, 184, 152)
            else: c = STRAW[(x * 7 + y * 3) % 3]
            if y + .5 > cy + ry * 0.55: c = tuple(int(v * 0.72) for v in c)   # the disc's thickness, in shade
            px[x, y] = grain(c, 8)
for (bx, by, ang) in ((13, 10, 200), (22, 13, -20), (17, 8, 120)):   # bolts stuck in the straw
    for i in range(5):
        x = int(round(bx + math.cos(math.radians(ang)) * i)); y = int(round(by + math.sin(math.radians(ang)) * i * 0.5))
        if 0 <= x < W and 0 <= y < H: px[x, y] = (40, 30, 22, 255) if i < 4 else (210, 210, 200, 255)
outline(im)
im.save(os.path.join(out, "occ_target.png"))

print("target drawn")

# ---- more furniture for the side rooms
random.seed(11)
def canvas(w, h): im = Image.new("RGBA", (w, h), (0, 0, 0, 0)); return im, im.load()
WOOD = [(36, 24, 16), (52, 36, 22), (70, 50, 30), (88, 64, 40)]
STRAW = [(110, 88, 46), (140, 114, 62), (168, 140, 80)]
IRON = [(40, 40, 48), (70, 70, 80), (110, 110, 122), (160, 160, 170)]

# occ_dummy: a straw training dummy on a post, seen from above: a round straw body, a cross-bar of arms, a sack head
im, px = canvas(26, 26)
for y in range(26):
    for x in range(26):
        if ((x - 12.5) / 6.5) ** 2 + ((y - 13.5) / 5.5) ** 2 <= 1: px[x, y] = grain(STRAW[(x + 2 * y) % 3], 8)
for x in range(2, 24):
    for y in range(12, 15): px[x, y] = grain(WOOD[2 if y == 13 else 1], 6)         # the arms' cross-bar
for x in (2, 3, 22, 23):
    for y in range(11, 16): px[x, y] = grain(STRAW[1], 8)                          # straw stuffed at its ends
for y in range(26):
    for x in range(26):
        if ((x - 12.5) / 3.6) ** 2 + ((y - 8.5) / 3.2) ** 2 <= 1: px[x, y] = grain((150, 132, 98), 8)   # the sack head
for (x, y) in ((11, 8), (14, 8), (12, 10), (13, 10)): px[x, y] = (60, 40, 30, 255)
for (x, y) in ((9, 16), (10, 17), (15, 15), (16, 17), (12, 18)): px[x, y] = (120, 20, 22, 255)   # red rags tied on
outline(im); im.save(os.path.join(out, "occ_dummy.png"))

# occ_rack: a weapon rack against the wall (seen from above, its back to the top): spears and a crossbow on it
im, px = canvas(40, 14)
for x in range(1, 39):
    for y in range(2, 6): px[x, y] = grain(WOOD[1 + (y == 3)], 6)                  # the rack's beam
for x in (3, 36):
    for y in range(1, 9): px[x, y] = grain(WOOD[0], 4)
for sx in (7, 12, 17, 27, 32):                                                     # spears leaning in it
    for y in range(0, 13): px[sx, y] = grain(WOOD[2], 5)
    px[sx, 12] = IRON[3] + (255,); px[sx, 11] = IRON[2] + (255,); px[sx - 1, 11] = IRON[1] + (255,); px[sx + 1, 11] = IRON[1] + (255,)
    px[sx, 9] = (130, 22, 24, 255)
for x in range(20, 25):                                                            # a crossbow hung on it
    px[x, 8] = WOOD[3] + (255,)
for y in range(6, 12): px[22, y] = WOOD[2] + (255,)
outline(im); im.save(os.path.join(out, "occ_rack.png"))

# occ_bedroll: a rolled-out bedding of grey wool with a dark blanket and a straw pillow
im, px = canvas(16, 30)
for y in range(2, 28):
    for x in range(2, 14): px[x, y] = grain((70, 66, 72) if y < 15 else (46, 30, 34), 6)
for y in range(3, 8):
    for x in range(4, 12): px[x, y] = grain(STRAW[2], 8)
for x in range(2, 14): px[x, 15] = (30, 20, 24, 255)
outline(im); im.save(os.path.join(out, "occ_bedroll.png"))
print("dummy, rack, bedroll drawn")

# occ_cart: a hand cart seen from above, its handles towards the left (the one who pushes it walks behind, the cart
# goes to the right), loaded with a crate, a sack and a barrel; two wheels at its sides
random.seed(21)
im, px = canvas(34, 24)
for x in range(8, 31):
    for y in range(4, 20): px[x, y] = grain(WOOD[1 + ((x // 3) % 2)], 6)            # the bed of planks
for x in range(8, 31):
    px[x, 4] = WOOD[0] + (255,); px[x, 19] = WOOD[0] + (255,)
for x in range(0, 9):                                                              # the two handles
    for y in (6, 17): px[x, y] = grain(WOOD[2], 5)
for (wx, wy) in ((19, 1), (19, 21)):                                               # wheels at the sides
    for x in range(wx - 4, wx + 5):
        for y in range(wy - 1, wy + 2):
            if 0 <= y < 24: px[x, y] = grain((30, 26, 24), 4) if abs(x - wx) < 4 else (60, 56, 50, 255)
for x in range(10, 18):                                                            # a crate
    for y in range(6, 13): px[x, y] = grain(WOOD[3] if (x + y) % 5 else WOOD[2], 6)
for y in range(6, 13): px[13, y] = WOOD[0] + (255,)
for y in range(13, 19):                                                            # a sack
    for x in range(11, 18):
        if ((x - 14) / 3.6) ** 2 + ((y - 15.5) / 2.8) ** 2 <= 1: px[x, y] = grain((128, 112, 84), 8)
for y in range(7, 18):                                                             # a barrel on its side
    for x in range(21, 29):
        if ((x - 24.5) / 3.8) ** 2 + ((y - 12.5) / 5.4) ** 2 <= 1: px[x, y] = grain(WOOD[2] if (y % 4) else (60, 60, 64), 6)
outline(im); im.save(os.path.join(out, "occ_cart.png"))
print("cart drawn")

# occ_rug: a worn dark red rug with a black border and a faded sigil, for the side rooms' floors (2 x 3 tiles)
random.seed(31)
im, px = canvas(48, 72)
for y in range(2, 70):
    for x in range(2, 46):
        edge = x < 6 or x > 41 or y < 6 or y > 65
        c = (30, 18, 22) if edge else (92, 20, 26)
        if not edge and (x - 24) ** 2 + (y - 36) ** 2 in range(110, 150): c = (150, 120, 70)   # a faded gold ring
        if not edge and abs(x - 24) <= 1 and 24 < y < 48: c = (150, 120, 70)
        if not edge and abs(y - 36) <= 1 and 12 < x < 36: c = (150, 120, 70)
        if edge and (x + y) % 6 == 0: c = (110, 90, 50)
        px[x, y] = grain(c, 9)
for y in (0, 1, 70, 71):                                                            # fringes
    for x in range(4, 44, 3): px[x, y] = (140, 120, 90, 255)
for _ in range(40):                                                                  # wear
    x, y = random.randint(7, 40), random.randint(7, 64)
    p = px[x, y]; px[x, y] = (int(p[0] * .7), int(p[1] * .7), int(p[2] * .7), 255)
outline(im); im.save(os.path.join(out, "occ_rug.png"))

# occ_bones_0..2: a few old bones on the floor (decals for the corridors)
for k in range(3):
    random.seed(40 + k)
    im, px = canvas(20, 16)
    for _ in range(2 + k):
        x0, y0 = random.randint(3, 15), random.randint(3, 12); ang = random.uniform(0, math.pi); ln = random.randint(4, 7)
        for i in range(ln):
            x = int(round(x0 + math.cos(ang) * (i - ln / 2))); y = int(round(y0 + math.sin(ang) * (i - ln / 2)))
            if 1 <= x < 19 and 1 <= y < 15: px[x, y] = grain((196, 186, 160), 10)
        for e in (-ln / 2, ln / 2 - 1):
            x = int(round(x0 + math.cos(ang) * e)); y = int(round(y0 + math.sin(ang) * e))
            for dx, dy in ((0, 1), (1, 0)):
                if 1 <= x + dx < 19 and 1 <= y + dy < 15: px[x + dx, y + dy] = grain((214, 204, 178), 8)
    if k == 2:                                                                       # a small skull
        for y in range(5, 10):
            for x in range(8, 13): px[x, y] = grain((206, 196, 170), 8)
        px[9, 7] = px[11, 7] = (20, 16, 14, 255)
    outline(im); im.save(os.path.join(out, "occ_bones_%d.png" % k))
print("rug and bones drawn")

# occ_dais: a round stone dais (2 x 2 tiles) with a ring of red runes - the secret rooms' chests stand on it
random.seed(51)
im, px = canvas(48, 48)
for y in range(48):
    for x in range(48):
        r = math.hypot(x + .5 - 24, y + .5 - 24)
        if r <= 22:
            c = (70, 66, 74) if r > 19 else (96, 92, 100) if r > 17 else (84, 80, 88)
            if 17.6 < r < 19.2:
                a = math.degrees(math.atan2(y - 24, x - 24)) % 30
                c = (150, 28, 30) if a < 12 else (60, 20, 24)
            if r <= 22 and r > 21: c = (40, 38, 44)
            px[x, y] = grain(c, 7)
for k in range(6):                                                                   # cracks
    a = random.uniform(0, 6.28); r0 = random.uniform(4, 14)
    for i in range(5):
        x = int(24 + math.cos(a) * (r0 + i)); y = int(24 + math.sin(a) * (r0 + i))
        px[x, y] = (40, 38, 44, 255)
outline(im); im.save(os.path.join(out, "occ_dais.png"))
print("dais drawn")
