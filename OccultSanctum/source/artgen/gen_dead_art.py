# The general's corpse in the game's own corpse style (after the ogre's and the orc's: the user said the first try
# looked nothing like the game): small and chunky, FLAT colours - two or three tones a material, no noise -, a black
# outline and a soft dark rim round it. The whole man on his back seen from above: bald pale head, steel pauldrons
# with pale spikes, black robe with the grey tabard and belt, arms out at his sides in steel bracers and gauntlets,
# legs apart in black boots; his morningstar dropped by his left hand.
#   usage: python gen_dead_art.py <out dir>
import sys, os, math
from PIL import Image
out = sys.argv[1]; os.makedirs(out, exist_ok=True)
OL = (20, 14, 18, 255)
SKIN = [(110, 96, 90), (154, 140, 130), (198, 186, 172)]
ROBE = [(22, 18, 24), (36, 31, 40), (54, 48, 58)]
TAB = [(84, 84, 90), (112, 112, 118), (140, 140, 146)]
STEEL = [(62, 62, 74), (100, 100, 114), (148, 148, 162)]
BONE = [(170, 160, 140), (222, 214, 192)]
BELT = [(64, 34, 24), (96, 52, 34)]
WOOD = [(66, 44, 28), (100, 68, 42)]
BLOOD = (122, 16, 18)
W, H = 48, 50
im = Image.new("RGBA", (W, H), (0, 0, 0, 0)); p = im.load()
final = Image.new("RGBA", (W, H), (0, 0, 0, 0))
def layer():
    """ends the part drawn so far: outlined on its own and laid over what is there"""
    global im, p
    ring = [(x, y) for y in range(H) for x in range(W) if p[x, y][3] == 0 and any(0 <= x + a < W and 0 <= y + b < H and p[x + a, y + b][3] == 255 for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for q in ring: p[q] = OL
    final.alpha_composite(im)
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0)); p = im.load()
def put(x, y, c):
    x, y = int(round(x)), int(round(y))
    if 0 <= x < W and 0 <= y < H: p[x, y] = c + (255,) if len(c) == 3 else c
def tone(r, ex, ey):
    s = ex * 0.6 + ey * 0.8                     # light from the upper left
    return r[2] if s < -0.35 else r[0] if s > 0.45 else r[1]
def ell(cx, cy, rx, ry, r):
    for y in range(H):
        for x in range(W):
            ex, ey = (x - cx) / rx, (y - cy) / ry
            if ex * ex + ey * ey <= 1.0: put(x, y, tone(r, ex, ey))
def limb(x0, y0, x1, y1, w, r):
    dx, dy = x1 - x0, y1 - y0; L = math.hypot(dx, dy)
    for y in range(H):
        for x in range(W):
            t = max(0.0, min(1.0, ((x - x0) * dx + (y - y0) * dy) / (L * L)))
            qx, qy = x0 + dx * t, y0 + dy * t
            d = math.hypot(x - qx, y - qy)
            if d <= w:
                s = ((x - qx) * -dy + (y - qy) * dx) / (L * w)   # across: -1 one side .. 1 the other
                put(x, y, r[2] if s < -0.45 else r[0] if s > 0.45 else r[1])

cx = 26
# the morningstar dropped by his left hand: the haft, the spiked iron ball
limb(6, 47, 10, 35, 1.0, WOOD + [WOOD[1]])
for y in (44, 45): put(7, y, BELT[1])
layer()
ell(5.5, 31.5, 3.6, 3.6, STEEL)
for a in range(0, 360, 60):
    for n in (4, 5): put(5.5 + math.cos(math.radians(a + 30)) * n, 31.5 + math.sin(math.radians(a + 30)) * n, BONE[1] if n == 5 else BONE[0])
# legs apart, black boots with steel toes
limb(cx - 3, 35, cx - 6, 43, 2.2, ROBE); limb(cx + 3, 35, cx + 6, 43, 2.2, ROBE)
ell(cx - 6.5, 45, 2.8, 2.2, ROBE); ell(cx + 6.5, 45, 2.8, 2.2, ROBE)
for dx in (-1, 0, 1): put(cx - 6.5 + dx, 46.5, STEEL[1]); put(cx + 6.5 + dx, 46.5, STEEL[1])
layer()
# arms at his sides, a little out: sleeves, steel bracers, gauntlets
limb(cx - 9, 15, cx - 13, 22, 2.2, ROBE); layer()
limb(cx + 9, 15, cx + 13, 22, 2.2, ROBE); layer()
limb(cx - 13, 22, cx - 16, 28, 1.9, STEEL); layer()
limb(cx + 13, 22, cx + 17, 27, 1.9, STEEL); layer()
ell(cx - 16.5, 30.5, 2.2, 2.0, STEEL); put(cx - 18, 31, STEEL[0]); layer()
ell(cx + 17.5, 29.5, 2.2, 2.0, STEEL); put(cx + 19, 30, STEEL[0]); layer()
# the robe: chest to the knees
for y in range(12, 37):
    half = 6.5 if y < 26 else 6.5 + (y - 26) * 0.25
    for x in range(int(cx - half), int(cx + half) + 1):
        e = (x - cx) / half
        put(x, y, ROBE[2] if e < -0.55 else ROBE[0] if e > 0.55 else ROBE[1])
for y in range(29, 37): put(cx - 4, y, ROBE[0]); put(cx + 5, y, ROBE[0])     # folds of the skirt
# the grey tabard, the belt and its buckle, blood on the chest
for y in range(12, 37):
    for x in range(cx - 2, cx + 3): put(x, y, TAB[2] if x == cx - 2 else TAB[0] if x == cx + 2 else TAB[1])
for x in range(int(cx - 6), int(cx + 7)): put(x, 26, BELT[1]); put(x, 27, BELT[0])
put(cx, 26, STEEL[2]); put(cx, 27, STEEL[1])
for (x, y) in ((cx, 17), (cx + 1, 18), (cx - 1, 19), (cx, 19), (cx + 1, 20), (cx, 21)): put(x, y, BLOOD)
layer()
# the steel collar and the pauldrons with their spikes
for x in range(cx - 3, cx + 4): put(x, 11, STEEL[2] if x < cx else STEEL[1]); put(x, 12, STEEL[1] if x < cx else STEEL[0])
layer()
for side in (-1, 1):
    ell(cx + side * 8, 13.5, 4.4, 3.6, STEEL)
    put(cx + side * 8, 12.5, STEEL[2])
    for ang, n in ((-35, 4), (10, 4)):
        a = math.radians(ang if side > 0 else 180 - ang)
        for k in range(n): put(cx + side * 11.5 + math.cos(a) * k, 13 + math.sin(a) * k, BONE[1] if k >= n - 2 else BONE[0])
    layer()
# the head: pale and bald, an old scar, eyes shut, blood at the mouth
ell(cx, 6, 3.6, 4.2, SKIN)
for k in range(3): put(cx - 2 + k, 3 + k, (130, 76, 70))
put(cx - 1, 6, SKIN[0]); put(cx + 2, 6, SKIN[0]); put(cx, 8, (84, 40, 40)); put(cx + 1, 9, BLOOD)
layer()
im = final; p = im.load()
# the black outline, then the soft dark rim of the game's corpses
ring = [(x, y) for y in range(H) for x in range(W) if p[x, y][3] == 0 and any(0 <= x + a < W and 0 <= y + b < H and p[x + a, y + b][3] == 255 and p[x + a, y + b] != OL for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
rim = [(x, y) for y in range(H) for x in range(W) if p[x, y][3] == 0 and any(0 <= x + a < W and 0 <= y + b < H and p[x + a, y + b] == OL for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
for q in rim: p[q] = (0, 0, 0, 70)
im.save(os.path.join(out, "gen_dead.png"))
im.resize((W * 8, H * 8), Image.NEAREST).save(os.path.join(out, "gen_dead_x8.png"))
print("general corpse", W, H)
