# seen STRAIGHT FROM ABOVE, like the candles:
#   occ_skullpile (+ _fl_0..2) - a heap of skulls: the round tops of their skulls packed together, the dark holes of
#                                the eyes showing at the front edge of the lower ones, a few bones between, three
#                                candle stubs (round wax tops) stuck on it; the flames over the candle tops
#   occ_rack                   - a spear rack against the wall: a beam along the wall with two posts, the spears
#                                standing upright in it - from above only their steel points and the round ends of
#                                the shafts under them show
# usage: python topdown_art.py <art dir>
import sys, os, math, random
from PIL import Image
out = sys.argv[1]; random.seed(5)
OL = (0, 0, 0, 255)
BONE = [(31, 27, 23), (54, 48, 42), (74, 66, 56), (95, 85, 71), (118, 106, 88), (140, 127, 106), (160, 148, 126)]
WAX = [(58, 51, 42), (94, 85, 74), (138, 127, 112), (179, 168, 150), (214, 204, 184)]
WOOD = [(28, 19, 11), (44, 31, 19), (60, 44, 28), (78, 58, 38), (96, 74, 50)]
IRON = [(30, 30, 36), (58, 58, 68), (96, 96, 108), (140, 140, 152), (190, 190, 200)]
def h2(x, y, s): return (math.sin(x * 12.9898 + y * 78.233 + s * 37.719) * 43758.5453) % 1.0
def g(c, x, y, s=1, k=7): d = (h2(x, y, s) - .5) * 2 * k; return tuple(max(0, min(255, int(v + d))) for v in c) + (255,)
def outline(im):
    w, h = im.size; px = im.load()
    ring = [(x, y) for y in range(h) for x in range(w) if px[x, y][3] == 0 and any(0 <= x + dx < w and 0 <= y + dy < h and px[x + dx, y + dy][3] > 0 and px[x + dx, y + dy] != OL for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for q in ring: px[q] = OL
def disc(px, w, h, cx, cy, rx, ry, colf):
    for y in range(h):
        for x in range(w):
            ex, ey = (x + .5 - cx) / rx, (y + .5 - cy) / ry
            if ex * ex + ey * ey <= 1: px[x, y] = colf(x, y, ex, ey)

# ---------------------------------------------------------------- the skull heap
W, H = 28, 26
CANDLES = [(8, 9), (20, 8), (14, 3)]          # the same places as before: the flames sit on them
im = Image.new("RGBA", (W, H), (0, 0, 0, 0)); px = im.load()
# a dark mound under it all (bones and earth)
disc(px, W, H, 14, 14, 10.5, 9.5, lambda x, y, ex, ey: g(BONE[0], x, y, 2, 4))
# a few bones lying between the skulls
for (x0, y0, x1, y1) in ((3, 18, 11, 22), (17, 21, 25, 17), (4, 7, 9, 4), (22, 4, 25, 12)):
    n = int(max(abs(x1 - x0), abs(y1 - y0)))
    for k in range(n + 1):
        x = round(x0 + (x1 - x0) * k / n); y = round(y0 + (y1 - y0) * k / n)
        px[x, y] = g(BONE[4], x, y, 3, 5)
    for (ex, ey) in ((x0, y0), (x1, y1)): px[ex, ey] = g(BONE[5], ex, ey, 4, 4)
# the skulls: round tops, lit from the upper left; the lower ones show their eye holes at the front (bottom) edge
SK = [(7, 17, 4.2, 3.8, True), (14, 19, 4.4, 4.0, True), (21, 17, 4.2, 3.8, True), (10, 11, 4.0, 3.6, True), (18, 11, 4.0, 3.6, True), (14, 6, 3.8, 3.4, False), (5, 11, 3.0, 2.8, False), (23, 11, 3.0, 2.8, False)]
for (cx, cy, rx, ry, face) in sorted(SK, key=lambda s: s[1]):
    def col(x, y, ex, ey, cx=cx, cy=cy):
        lit = .62 - .4 * (ex + ey) / 1.41 - .3 * (ex * ex + ey * ey) ** 1.5
        i = max(1, min(6, int(round(lit * 7))))
        if ex * ex + ey * ey > .72: i = 1 if ex * ex + ey * ey > .86 else min(i, 2)
        return g(BONE[i], x, y, 6, 6)
    disc(px, W, H, cx, cy, rx, ry, col)
    if face:                                                            # the eye holes and the nose at its front edge
        ey0 = int(cy + ry * .45)
        for dx in (-2, 1):
            for (ax, ay) in ((0, 0), (1, 0), (0, 1), (1, 1)):
                xx, yy = int(cx) + dx + ax, ey0 + ay
                if 0 <= xx < W and 0 <= yy < H: px[xx, yy] = (12, 9, 8, 255)
        nx, ny = int(cx), ey0 + 2
        if 0 <= ny < H: px[nx, ny] = (24, 19, 15, 255)
        for xx in range(int(cx) - 2, int(cx) + 3):                      # the brow ridge above the eyes, catching the light
            if 0 <= ey0 - 1 < H and px[xx, ey0 - 1][3]: px[xx, ey0 - 1] = g(BONE[6], xx, ey0 - 1, 7, 4)
    # the seam across the top of the skull
    sx = int(cx)
    for yy in range(int(cy - ry + 1), int(cy)):
        if 0 <= yy < H and px[sx, yy][3]: px[sx, yy] = g(BONE[3], sx, yy, 8, 4)
# the candle stubs: round wax tops with a drip of wax down the side, the wick black in the middle
for (x, y) in CANDLES:
    disc(px, W, H, x + 1, y + 1.5, 2.6, 2.4, lambda xx, yy, ex, ey: g(WAX[1], xx, yy, 9, 4))     # wax run down on the skull
    disc(px, W, H, x + .5, y + .5, 1.9, 1.9, lambda xx, yy, ex, ey: g(WAX[4] if ex + ey < -.3 else WAX[3], xx, yy, 10, 5))
    px[x, y] = (20, 16, 12, 255)
outline(im)
im.save(os.path.join(out, "occ_skullpile.png")); pile = im
for f in range(3):
    fl = Image.new("RGBA", (W, H), (0, 0, 0, 0)); fp = fl.load(); rnd = random.Random(f * 3 + 1)
    for (x, y) in CANDLES:
        ox = rnd.choice((0, 0, -1, 1)); oy = rnd.choice((0, 0, -1))
        # from above a flame is a small bright core with an orange glow round it
        for (dx, dy, c) in ((0, 0, (255, 214, 120, 255)), (1, 0, (220, 110, 40, 230)), (-1, 0, (220, 110, 40, 230)), (0, -1, (230, 120, 45, 240)), (0, 1, (190, 80, 30, 200)), (0, -2, (160, 60, 25, 170))):
            xx, yy = x + ox + dx, y + oy + dy
            if 0 <= xx < W and 0 <= yy < H: fp[xx, yy] = c
    fl.save(os.path.join(out, "occ_skullpile_fl_%d.png" % f))

# ---------------------------------------------------------------- the spear rack
W, H = 40, 14
im = Image.new("RGBA", (W, H), (0, 0, 0, 0)); px = im.load()
for x in range(2, 38):                                                  # the beam along the wall (its top, lit)
    for y in range(3, 7): px[x, y] = g(WOOD[3] if y == 3 else WOOD[2] if y < 6 else WOOD[1], x, y, 11, 5)
for x0 in (1, 35):                                                      # the two posts: their square tops
    for x in range(x0, x0 + 4):
        for y in range(2, 8): px[x, y] = g(WOOD[4] if (x == x0 or y == 2) else WOOD[2], x, y, 12, 5)
for x in range(4, 36):                                                  # the lower rail in front, where the shafts stand
    for y in range(9, 11): px[x, y] = g(WOOD[2] if y == 9 else WOOD[1], x, y, 13, 5)
for sx in (8, 13, 18, 23, 28, 32):                                      # the spears, upright: from above their points
    # the round end of the shaft, through a hole in the rail
    for (dx, dy) in ((0, 0), (1, 0), (0, 1), (1, 1)): px[sx + dx, 8 + dy] = g(WOOD[3], sx + dx, 8 + dy, 14, 4)
    # the steel point seen from straight above: a small four-sided point, its ridges catching the light
    cx, cy = sx + .5, 5.0
    for (dx, dy, c) in ((0, -2, IRON[2]), (-1, -1, IRON[1]), (0, -1, IRON[4]), (1, -1, IRON[2]), (-1, 0, IRON[2]), (0, 0, IRON[3]), (1, 0, IRON[1]), (0, 1, IRON[2]), (1, 1, IRON[1]), (-1, 1, IRON[1]), (0, 2, IRON[1])):
        xx, yy = int(cx + dx), int(cy + dy)
        px[xx, yy] = c + (255,)
    px[sx, 7] = (130, 22, 24, 255)                                         # a red rag tied under the point
outline(im)
im.save(os.path.join(out, "occ_rack.png")); rack = im

pv = Image.new("RGBA", (28 * 8 + 40 * 8 + 30, 26 * 8), (52, 36, 28, 255))
f0 = Image.open(os.path.join(out, "occ_skullpile_fl_0.png"))
p2 = pile.copy(); p2.alpha_composite(f0)
pv.alpha_composite(p2.resize((28 * 8, 26 * 8), Image.NEAREST), (0, 0)); pv.alpha_composite(rack.resize((40 * 8, 14 * 8), Image.NEAREST), (28 * 8 + 30, 40))
pv.save(os.path.join(os.path.dirname(os.path.abspath(__file__)), "topdown_preview.png"))
print("done")
