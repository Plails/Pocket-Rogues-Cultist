# The winged devil (an enemy called up by the cultists' ritual) = the game's goblin assassin's parts (shapes kept)
# recoloured to dark red devil skin with black-red rags, small horns added to the head, a pair of bat wings drawn
# for its back (two frames, they flap), the corpse likewise. Plus the ritual's portal (4 frames).
# usage: python devil_art.py <goblin parts dir> <out dir>
import sys, os, math, random
from PIL import Image

src, out = sys.argv[1], sys.argv[2]
SKIN = [(46, 8, 10), (78, 14, 16), (112, 24, 22), (146, 38, 30), (178, 58, 40)]
RAG = [(10, 7, 9), (20, 14, 16), (32, 22, 24), (46, 32, 32)]
HORN = [(40, 34, 28), (88, 78, 64), (150, 138, 116), (200, 190, 168)]
lum = lambda p: .3 * p[0] + .59 * p[1] + .11 * p[2]
OL = (0, 0, 0, 255)


def recolor(name, outname):
    im = Image.open(os.path.join(src, name + ".png")).convert("RGBA")
    w, h = im.size
    px = im.load()
    body = [(x, y) for y in range(h) for x in range(w) if px[x, y][3] > 200 and lum(px[x, y]) > 22]
    ls = sorted(lum(px[x, y]) for x, y in body); lo, hi = (ls[0], ls[-1]) if ls else (0, 1)
    for (x, y) in body:
        p = px[x, y]
        t = (lum(p) - lo) / max(1, hi - lo)
        greenish = p[1] >= p[0] - 6 and p[1] > p[2] + 22          # the goblin's olive skin (green ~ red, little blue)
        greyish = abs(p[0] - p[1]) < 14 and abs(p[1] - p[2]) < 14 and lum(p) > 120     # the daggers' steel
        if greyish: px[x, y] = (0, 0, 0, 0); continue
        r = SKIN                                                  # a devil is red all over (the rags too)
        px[x, y] = r[min(len(r) - 1, max(0, round(t * (len(r) - 1) + .3)))] + (p[3],)
    return im


os.makedirs(out, exist_ok=True)
for part in ("Body", "UpperArm", "ForeArm", "Hand"):
    recolor("goblinAssassin_parts_" + part, None).save(os.path.join(out, "devil_" + part.lower() + ".png"))
recolor("goblinAssassin_deadBody_0", None).save(os.path.join(out, "devil_dead.png"))

# the head with two small horns curving back (the face is at the top of the picture), on a bigger canvas with the
# head kept at the same spot relative to its pivot (the mod uses the goblin head's pivot fraction - so the canvas
# keeps the goblin's 14 x 12 and the horns are drawn inside it, over the ears)
head = recolor("goblinAssassin_parts_Head", None)
hp = head.load()
for side in (-1, 1):
    for t in range(5):
        x = 7 + side * (2.2 + t * .7) - (1 if side < 0 else 0)
        y = 5 - t * 0.9
        xi, yi = int(round(x)), int(round(y))
        if 0 <= xi < 14 and 0 <= yi < 12:
            hp[xi, yi] = HORN[min(3, 3 - t // 2)] + (255,)
            if 0 <= yi + 1 < 12 and hp[xi, yi + 1][3] < 200: hp[xi, yi + 1] = OL
head.save(os.path.join(out, "devil_head.png"))


def wing(frame, side):
    """a bat wing seen from above, spread out to one side from the shoulder (the pivot is at the inner edge)"""
    W, H = 22, 18
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0)); px = im.load()
    spread = 1.0 if frame == 0 else 0.62
    root = (1, 8)
    tips = [(20 * spread + 1, 2), (18 * spread + 1, 9), (14 * spread + 1, 15)]
    def inside(x, y):
        # the membrane: inside the polygon root - tips, scalloped between the tips
        pts = [root] + tips + [(1, 12)]
        n = len(pts); c = False; j = n - 1
        for i in range(n):
            xi, yi = pts[i]; xj, yj = pts[j]
            if ((yi > y) != (yj > y)) and (x < (xj - xi) * (y - yi) / (yj - yi + 1e-9) + xi): c = not c
            j = i
        return c
    for y in range(H):
        for x in range(W):
            if inside(x + .5, y + .5):
                px[x, y] = (60, 12, 16, 255) if (x + y) % 5 else (80, 18, 20, 255)
    for t in tips:                                           # the bones
        for k in range(12):
            u = k / 11; x = root[0] + (t[0] - root[0]) * u; y = root[1] + (t[1] - root[1]) * u
            px[int(x), int(y)] = (30, 8, 10, 255)
    # outline
    add = []
    for y in range(H):
        for x in range(W):
            if px[x, y][3] == 0 and any(0 <= x + dx < W and 0 <= y + dy < H and px[x + dx, y + dy][3] > 0 and px[x + dx, y + dy] != OL for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))): add.append((x, y))
    for x, y in add: px[x, y] = OL
    if side < 0: im = im.transpose(Image.FLIP_LEFT_RIGHT)
    return im


for f in range(2):
    wing(f, 1).save(os.path.join(out, "devil_wing_r_%d.png" % f))
    wing(f, -1).save(os.path.join(out, "devil_wing_l_%d.png" % f))

# the ritual's portal: a swirling hole of dark red and violet light, 4 frames
N = 56
for f in range(4):
    im = Image.new("RGBA", (N, N), (0, 0, 0, 0)); px = im.load(); c = N / 2
    for y in range(N):
        for x in range(N):
            dx, dy = x + .5 - c, y + .5 - c
            r = math.hypot(dx, dy) / (N / 2 - 2)
            if r > 1: continue
            a = math.atan2(dy, dx)
            sw = math.sin(a * 3 - r * 8 + f * math.pi / 2)
            if r > .9: col = (150, 20, 30, 200)
            elif r < .25: col = (8, 2, 6, 255)
            elif sw > .3: col = (190, 36, 46, 255)
            elif sw > -.3: col = (110, 20, 70, 255)
            else: col = (40, 6, 22, 255)
            px[x, y] = col
    im.save(os.path.join(out, "occ_portal_%d.png" % f))
print("devil + portal drawn")
