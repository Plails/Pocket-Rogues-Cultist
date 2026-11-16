# Art of the Occult Sanctum mod, v2: in the manner of the game's goblin kitchen lair (Gates_Lair_GoblinKitchen,
# GoblinKitchen_Floor, KitchenCabinet): 24 px per tile, seen from straight above, grainy surfaces (pixels a shade up
# or down), dark and muted, 1 px black outline and a 2 px soft dark halo. The gate is a low narrow bar with bulky
# posts at both ends, set over a black hole in the wall.  usage: python artgen.py <outdir>
import math, os, random, sys
from PIL import Image

OUT = sys.argv[1] if len(sys.argv) > 1 else "art"
os.makedirs(OUT, exist_ok=True)


def hx(h, a=255):
    h = h.lstrip("#"); return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


OL = (0, 0, 0, 255)
# all dark and muted, like the goblin kitchen (its floor #382a1f..#55473c, its wood #1e140b..#6c4d35)
ST = [hx(c) for c in ("#0e0a0b", "#171112", "#1f1719", "#281e20", "#312628", "#3b2e2f", "#463738")]      # black stone
FL = [hx(c) for c in ("#120c0d", "#191113", "#201618", "#271b1d", "#2e2022", "#352527", "#3d2b2c")]      # floor stones
BONE = [hx(c) for c in ("#1f1b17", "#36302a", "#4a4238", "#5f5547", "#766a58", "#8c7f6a")]
WOOD = [hx(c) for c in ("#0d0905", "#1e140b", "#2c2014", "#392c21", "#473b30", "#513d2f", "#605144")]
RED = [hx(c) for c in ("#1c0507", "#2c080b", "#3e0c10", "#521116", "#6a181c", "#842024", "#a02c2a")]
GOLD = [hx(c) for c in ("#22190c", "#352812", "#4a3a1c", "#5e4a26")]
IRON = [hx(c) for c in ("#111115", "#1a1a1f", "#2b2b33", "#3d3d47")]
GLASS_V = [hx(c) for c in ("#100816", "#1c0f26", "#2a1838", "#3c244c")]
GLASS_G = [hx(c) for c in ("#0b130e", "#132018", "#1d2e22", "#2a4030")]
CLEAR = (0, 0, 0, 0)


def h2(x, y, seed):  # hash noise 0..1
    n = (int(x) * 374761393 + int(y) * 668265263 + seed * 2147483647) & 0xFFFFFFFF
    n = ((n ^ (n >> 13)) * 1274126177) & 0xFFFFFFFF
    return (n ^ (n >> 16)) / 0xFFFFFFFF


def grain(ramp, i, x, y, seed=1, amount=.38):
    """a shade of the ramp with the game's pixel grain: now and then a pixel one step lighter or darker"""
    r = h2(x, y, seed)
    if r < amount / 2: i -= 1
    elif r > 1 - amount / 2: i += 1
    return ramp[max(0, min(len(ramp) - 1, i))]


def G(ramp, i, seed=1, amount=.38):
    return lambda x, y: grain(ramp, i, x, y, seed, amount)


class Img:
    def __init__(s, w, h): s.w, s.h = w, h; s.p = [[CLEAR] * w for _ in range(h)]
    def ok(s, x, y): return 0 <= x < s.w and 0 <= y < s.h
    def get(s, x, y): return s.p[y][x] if s.ok(x, y) else CLEAR
    def put(s, x, y, c):
        x, y = int(math.floor(x)), int(math.floor(y))
        if s.ok(x, y): s.p[y][x] = c(x, y) if callable(c) else c
    def blend(s, x, y, c, a=1.0):
        x, y = int(math.floor(x)), int(math.floor(y))
        if not s.ok(x, y): return
        sa = c[3] / 255 * a
        if sa <= 0: return
        d = s.p[y][x]; da = d[3] / 255; oa = sa + da * (1 - sa)
        s.p[y][x] = tuple(int(round((c[i] * sa + d[i] * da * (1 - sa)) / oa)) for i in range(3)) + (int(round(oa * 255)),)
    def area(s, x0, y0, x1, y1, test, c):
        for y in range(int(math.floor(y0)), int(math.ceil(y1))):
            for x in range(int(math.floor(x0)), int(math.ceil(x1))):
                if test(x, y): s.put(x, y, c)
    def disc(s, cx, cy, r, c): s.area(cx - r - 1, cy - r - 1, cx + r + 2, cy + r + 2, lambda x, y: (x + .5 - cx) ** 2 + (y + .5 - cy) ** 2 <= r * r, c)
    def ring(s, cx, cy, r0, r1, c): s.area(cx - r1 - 1, cy - r1 - 1, cx + r1 + 2, cy + r1 + 2, lambda x, y: r0 <= math.hypot(x + .5 - cx, y + .5 - cy) <= r1, c)
    def rect(s, x0, y0, x1, y1, c): s.area(x0, y0, x1, y1, lambda x, y: True, c)
    def line(s, x0, y0, x1, y1, c, w=1.0):
        n = max(1, int(math.hypot(x1 - x0, y1 - y0) * 3))
        for i in range(n + 1):
            t = i / n; x = x0 + (x1 - x0) * t; y = y0 + (y1 - y0) * t
            if w <= 1.01: s.put(x, y, c)
            else: s.disc(x, y, w / 2, c)
    def outline(s, c=OL, thr=100):
        add = [(x, y) for y in range(s.h) for x in range(s.w) if s.p[y][x][3] < thr and any(s.get(x + dx, y + dy)[3] >= thr for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
        for x, y in add: s.p[y][x] = c
    def halo(s, alphas=(95, 45)):
        for a in alphas:
            add = []
            for y in range(s.h):
                for x in range(s.w):
                    if s.p[y][x][3] > 0: continue
                    if any(s.get(x + dx, y + dy)[3] > 30 for dx in (-1, 0, 1) for dy in (-1, 0, 1)):
                        add.append((x, y, a + (25 if s.get(x, y - 1)[3] > 30 else 0)))   # heavier under things
            for x, y, aa in add: s.p[y][x] = (0, 0, 0, min(255, aa))
    def finish(s): s.outline(); s.halo(); return s
    def save(s, name):
        im = Image.new("RGBA", (s.w, s.h)); im.putdata([c for row in s.p for c in row]); im.save(os.path.join(OUT, name + ".png"))


def skull(im, cx, cy, sc=1.0, horns=False):
    """a skull seen from above: the crown, the brow ridge, two eye pits"""
    if horns:
        for side in (-1, 1):
            for t in range(10):
                a = t / 9
                x = cx + side * (3.2 + a * 3.6) * sc; y = cy - 1 * sc - math.sin(a * 2.8) * 2.4 * sc + a * 2.5 * sc
                im.disc(x, y, (1.3 - a * .6) * sc, G(BONE, 3 if t < 6 else 2, 7))
    im.disc(cx, cy, 3.6 * sc, G(BONE, 3, 5))
    im.disc(cx - .7 * sc, cy - .8 * sc, 2.2 * sc, G(BONE, 4, 6))
    im.rect(cx - 2 * sc, cy + 2.2 * sc, cx + 2 * sc, cy + 4 * sc, G(BONE, 2, 8))
    for dx in (-1.6, 1.0): im.rect(cx + dx * sc - .5, cy + .6 * sc, cx + dx * sc + .9 * sc, cy + 1.8 * sc, OL)


def rune(im, x, y, k, c):
    """one of 8 little glyphs, 3x4 px"""
    g = ["010111010010", "101010101010", "111100111001", "100110101011", "011100110001", "111010010111", "101111101101", "001011101100"][k % 8]
    for i, ch in enumerate(g):
        if ch == "1": im.put(x + i % 3, y + i // 3, c)


# ------------------------------------------------------------------------------------------------ the gate
GW, GH = 56, 34           # a doorway 2.3 tiles wide; its threshold lies on the wall's lower edge
POSTS = (8, 42)


def eyes(im, x, y, sc=1.0):
    """two glowing red eyes in a skull's pits (like the game's Jack gates)"""
    for dx in (-1.6, 1.0):
        im.put(x + dx * sc, y + .9 * sc, hx("#c42a26")); im.put(x + dx * sc + 1, y + .9 * sc, hx("#7a1414"))


def goat(im, cx, cy, sc=1.0):
    """a goat's skull seen from above: long face, eye pits, horns curling out to both sides"""
    for side in (-1, 1):
        for t in range(22):
            a = t / 21
            x = cx + side * (2.5 * sc + (1 - math.cos(a * 2.6)) * 4.2 * sc)
            y = cy - 2.5 * sc - math.sin(a * 2.6) * 3.6 * sc + a * 1.5 * sc
            w = (1.7 - a * 1.0) * sc
            im.disc(x, y, w + .6, BONE[0]); im.disc(x, y, w, G(BONE, 3 if (t // 3) % 2 else 2, 120))
    im.disc(cx, cy - .5 * sc, 3.8 * sc, G(BONE, 3, 121))                       # the crown
    im.rect(cx - 2.2 * sc, cy + 1.5 * sc, cx + 2.2 * sc, cy + 6 * sc, G(BONE, 3, 122))   # the long face
    im.rect(cx - 1.4 * sc, cy + 5.5 * sc, cx + 1.4 * sc, cy + 7 * sc, G(BONE, 2, 123))
    im.disc(cx - .8 * sc, cy - 1.4 * sc, 1.7 * sc, G(BONE, 4, 124))
    for dx in (-1.9, 1.0):
        im.rect(cx + dx * sc - .4, cy + .6 * sc, cx + dx * sc + 1.2 * sc, cy + 2.2 * sc, OL)
    eyes(im, cx, cy + .4 * sc, sc)
    im.put(cx - .5, cy + 4.5 * sc, OL); im.put(cx + .5, cy + 4.5 * sc, OL)        # nostrils


MARBLE = [hx(c) for c in ("#151418", "#222026", "#302d34", "#403c44", "#524d56", "#666069", "#7c767e", "#938c94")]


def marble(x, y, base, seed=200):
    """dark marble with paler veins: a shade of the ramp, lighter along thin wandering lines"""
    v = math.sin(x * .55 + y * .21 + math.sin(y * .37 + seed) * 2.2) + math.sin(x * .13 - y * .48 + seed * .7) * .8
    i = base + (1 if abs(v) < .14 else 0)
    return grain(MARBLE, i, x, y, seed, .25)


def gate():
    """v7: real gates in the wall: a pair of closed doors of dark wood with iron bands, studs and ring handles and
    a red sigil painted across the join, in a doorway framed by two dark-marble pillars and a lintel, a marble
    threshold at the bottom (the floor side)"""
    im = Img(GW, GH)
    cx = GW / 2
    # the doors
    for x in range(11, GW - 11):
        for y in range(5, GH - 4):
            leaf = 0 if x < cx else 1
            plank = (x - 11) // 4
            c = grain(WOOD, 3 if plank % 2 else 2, x, y, 300 + plank, .45)
            if (x - 11) % 4 == 0: c = WOOD[1]
            if abs(x + .5 - cx) < 1: c = WOOD[0]                                         # the join
            im.put(x, y, c)
    for by in (9, 17, GH - 8):                                                          # iron bands with studs
        im.rect(11, by, GW - 11, by + 2, lambda x, y: IRON[3] if y == by else IRON[2])
        for x in range(13, GW - 12, 4): im.put(x, by, IRON[4])
    for hx_ in (cx - 4, cx + 3):                                                        # ring handles
        im.ring(hx_ + .5, 14, 1.3, 2.2, IRON[4]); im.put(hx_, 12, IRON[2])
    im.ring(cx, 22, 2.6, 3.5, RED[3]); im.put(cx - .5, 21.5, RED[4])                     # a red sigil across the join
    for k in range(3): im.put(cx - 1 + k, 18 + k, RED[2])
    # the lintel over the doorway
    im.rect(6, 0, GW - 6, 6, lambda x, y: MARBLE[7] if y == 0 else MARBLE[1] if y == 5 else marble(x, y, 4, 310))
    for x in range(9, GW - 9, 5): im.rect(x, 2, x + 3, 4, lambda xx, yy: marble(xx, yy, 5, 311))
    im.disc(cx, 3, 2.2, RED[3]); im.put(cx - .5, 2.5, RED[5])                           # a red stone in the keystone
    # the pillars: square, with a capital at the top and a wider base at the bottom
    for px in (5.5, GW - 5.5):
        im.rect(px - 4.5, 0, px + 4.5, GH - 2, lambda x, y, px=px: MARBLE[6] if x < px - 3.5 else MARBLE[1] if x > px + 3.5 else marble(x, y, 4, 312))
        im.rect(px - 5.5, 0, px + 5.5, 4, lambda x, y: MARBLE[7] if y == 0 else marble(x, y, 5, 313))      # capital
        im.rect(px - 5.5, GH - 6, px + 5.5, GH - 1, lambda x, y: MARBLE[2] if y >= GH - 2 else marble(x, y, 4, 314))  # base
        for y in range(7, GH - 7, 3): im.put(px - 2, y, MARBLE[3]); im.put(px + 2, y + 1, MARBLE[3])   # fluting
    # the threshold
    im.rect(10, GH - 4, GW - 10, GH, lambda x, y: MARBLE[6] if y == GH - 4 else marble(x, y, 3, 315))
    return im.finish()


def hole():
    """the black way in, laid over the wall above the gate"""
    im = Img(48, 26)
    for y in range(im.h):
        for x in range(im.w):
            dx = abs(x + .5 - im.w / 2) / (im.w / 2)
            top = (im.h - 1 - y) / im.h          # 0 at the bottom (the gate), 1 at the top
            a = 1 - max(dx ** 3, top ** 2.2)
            if a <= .02: continue
            c = (6, 3, 4) if h2(x, y, 31) > .1 else (14, 6, 8)
            im.put(x, y, c + (int(255 * min(1, a * 1.35)),))
    return im


# ------------------------------------------------------------------------------------------------ the floor
FT = 30                       # the floor texture covers 30 x 30 tiles; the mod cuts a tile of it for every floor tile


def floor_big():
    """the sanctum's floor stones for any shape of room: one seamless picture, cut into tiles in the game"""
    N = FT * 24
    im = Img(N, N)
    rnd = random.Random(5)
    pts, cell = [], 11
    for gy in range(-1, N // cell + 2):
        for gx in range(-1, N // cell + 2):
            pts.append(((gx + rnd.uniform(.2, .8)) * cell, (gy + rnd.uniform(.2, .8)) * cell, rnd.random()))
    grid = {}
    for i, (px, py, _) in enumerate(pts): grid.setdefault((int(px // cell), int(py // cell)), []).append(i)
    for y in range(N):
        for x in range(N):
            gx, gy = int(x // cell), int(y // cell)
            best = second = 1e9; bi = -1
            for oy in (-1, 0, 1):
                for ox in (-1, 0, 1):
                    for i in grid.get((gx + ox, gy + oy), ()):
                        d = (pts[i][0] - x - .5) ** 2 + (pts[i][1] - y - .5) ** 2
                        if d < best: second = best; best = d; bi = i
                        elif d < second: second = d
            edge = math.sqrt(second) - math.sqrt(best)
            sh = pts[bi][2]
            if edge < 1.6: c = grain(FL, 0 if edge < .9 else 1, x, y, 2, .3)
            else:
                base = 3 if sh < .35 else 4 if sh < .8 else 5
                if pts[bi][1] - y > 2.5 and edge < 3.5: base += 1
                elif y - pts[bi][1] > 2.5 and edge < 3.0: base -= 1
                c = grain(FL, base, x, y, 3, .45)
            im.put(x, y, c)
    for _ in range(40):          # bones lying about, as in the lairs
        bx, by, ang = rnd.uniform(10, N - 10), rnd.uniform(10, N - 10), rnd.uniform(0, 3.14)
        for t in range(9): im.put(bx + math.cos(ang) * (t - 4), by + math.sin(ang) * (t - 4), G(BONE, 2, 11))
        im.put(bx - math.cos(ang) * 5, by - math.sin(ang) * 5, BONE[3]); im.put(bx + math.cos(ang) * 5, by + math.sin(ang) * 5, BONE[3])
    return im


def carpet_tile(end):
    """one tile's length of the worn dark-red runner, 3 tiles wide; the end piece has a fringe at its top"""
    im = Img(72, 24)
    cx = 36
    for y in range(24):
        for x in range(6, 66):
            dx = min(x - 6, 65 - x)
            if dx < 2: c = grain(GOLD, 1 if dx else 0, x, y, 4)
            elif dx == 4: c = grain(GOLD, 1, x, y, 5)
            else:
                ux = abs(x - cx); uy = y - 12
                c = grain(RED, 2, x, y, 6, .5)
                if abs(ux + abs(uy) - 13) < 1.0: c = grain(GOLD, 1, x, y, 7)
                elif ux + abs(uy) < 4: c = grain(RED, 3, x, y, 8)
            if h2(x // 4, y // 4, 9 + end) < .05: c = grain(FL, 3, x, y, 10)
            if end and y < 3: c = None if (x % 2 or y < 1) else GOLD[1]
            if c is not None: im.put(x, y, c)
    return im


def blood(v):
    im = Img(40, 30)
    rnd = random.Random(60 + v)
    for _ in range(3):
        bx, by, r = rnd.uniform(12, 28), rnd.uniform(10, 20), rnd.uniform(4, 8)
        for _ in range(int(r * r * 2.2)):
            a = rnd.uniform(0, 6.283); d = r * math.sqrt(rnd.random())
            im.blend(bx + math.cos(a) * d * 1.4, by + math.sin(a) * d, RED[0], .8)
    return im


# ------------------------------------------------------------------------------------------------ ritual circle
CW = 132


def circle(frame):
    """drawn in dried blood that barely glows (the second frame a little more)"""
    im = Img(CW, CW)
    c = CW / 2
    core = RED[4] if frame == 0 else RED[5]
    dark = RED[2]
    R1, R2 = 61, 50
    pts = [(c + math.cos(-math.pi / 2 + k * 2 * math.pi / 5) * R2, c + math.sin(-math.pi / 2 + k * 2 * math.pi / 5) * R2) for k in range(5)]
    star = [pts[(k * 2) % 5] for k in range(6)]
    im.ring(c, c, R1 - 1.6, R1 + 1.6, dark); im.ring(c, c, R2 - 1.4, R2 + 1.4, dark)
    for a, b in zip(star, star[1:]): im.line(a[0], a[1], b[0], b[1], dark, 3)
    im.ring(c, c, R1 - .7, R1 + .7, lambda x, y: core if h2(x, y, 1) > .18 else dark)
    im.ring(c, c, R2 - .6, R2 + .6, lambda x, y: core if h2(x, y, 2) > .18 else dark)
    for a, b in zip(star, star[1:]): im.line(a[0], a[1], b[0], b[1], core, 1)
    for k in range(16):
        a = k * 2 * math.pi / 16 + .2
        rune(im, c + math.cos(a) * 55 - 1, c + math.sin(a) * 55 - 2, k, core)
    for (x, y) in pts: im.disc(x, y, 2.2, dark)
    im.ring(c, c, 7, 8.2, dark)
    return im


# ------------------------------------------------------------------------------------------------ altar
def altar():
    im = Img(64, 34)
    x0, y0, x1, y1 = 3, 5, 61, 30
    im.rect(x0, y0, x1, y1, lambda x, y: ST[5] if y == y0 else ST[1] if y >= y1 - 2 else ST[2] if x < x0 + 2 or x > x1 - 3 else grain(ST, 3, x, y, 4, .5))
    im.rect(x0 - 1, y1 - 3, x1 + 1, y1 + 1, G(ST, 2, 5))      # the plinth
    im.rect(25, y0 + 1, 39, y1 + 2, lambda x, y: GOLD[1] if x in (26, 37) else grain(RED, 2 if 26 < x < 37 else 1, x, y, 6, .5))
    for k in range(3): rune(im, 31, y0 + 5 + k * 7, k + 2, GOLD[2])
    skull(im, 32, 14, 1.25, horns=True)
    im.rect(8, 10, 21, 22, OL); im.rect(9, 11, 15, 21, G(BONE, 3, 7)); im.rect(15, 11, 20, 21, G(BONE, 2, 8))
    for y in range(13, 20, 2): im.line(10, y, 14, y, BONE[1]); im.line(16, y, 19, y, BONE[1])
    im.line(45, 23, 56, 12, IRON[3]); im.line(46, 23, 57, 12, IRON[2]); im.rect(43, 22, 47, 26, WOOD[2])
    im.disc(53, 25, 4, G(ST, 4, 9)); im.disc(53, 25, 2.8, RED[1]); im.put(52, 24, RED[3])
    return im.finish()


# ------------------------------------------------------------------------------------------------ column
def column():
    im = Img(34, 34)
    c = 17
    im.disc(c, c, 14.5, ST[1])
    im.disc(c, c, 13.5, G(ST, 2, 1, .5))
    im.disc(c - 1, c - 1, 11, G(ST, 3, 2, .5))
    im.ring(c, c, 8.4, 9.6, G(RED, 1, 3))
    for k in range(8):
        a = k * math.pi / 4
        im.put(c + math.cos(a) * 9, c + math.sin(a) * 9, RED[3])
    im.disc(c - 1, c - 1, 7, G(ST, 4, 4, .5)); im.disc(c - 2, c - 2, 3.5, G(ST, 5, 5))
    im.line(c + 3, c - 10, c + 6, c - 4, ST[1]); im.line(c - 9, c + 4, c - 4, c + 7, ST[1])   # cracks
    return im.finish()


# ------------------------------------------------------------------------------------------------ tables
def table(var):
    im = Img(48, 30)
    x0, y0, x1, y1 = 2, 3, 46, 27

    def plank(x, y):
        if (y - y0) % 6 == 5: return WOOD[0]
        i = 3 if ((y - y0) // 6) % 2 else 2
        if (y - y0) % 6 == 0: i += 1
        return grain(WOOD, i, x, y, 1 + (y - y0) // 6, .5)
    im.rect(x0, y0, x1, y1, plank)
    if var == 0:
        skull(im, 12, 13, .95)
        im.rect(23, 8, 35, 19, OL); im.rect(24, 9, 29, 18, G(BONE, 3, 3)); im.rect(29, 9, 34, 18, G(BONE, 2, 4))
        rune(im, 30, 11, 1, RED[3])
        im.disc(40, 10, 3, G(GLASS_V, 2, 5)); im.put(39, 9, GLASS_V[3])
        im.disc(39, 21, 2.4, G(GLASS_G, 2, 6)); im.put(38, 20, GLASS_G[3])
        im.disc(17, 22, 2.6, G(BONE, 4, 7)); im.put(17, 22, BONE[1])
    else:
        im.rect(6, 7, 20, 12, G(BONE, 3, 8)); im.rect(6, 7, 8, 12, BONE[1]); im.rect(18, 7, 20, 12, BONE[1])
        im.disc(11, 20, 4.2, G(ST, 4, 9)); im.disc(11, 20, 3, RED[1]); im.put(10, 19, RED[3])
        for (bx, by) in [(26, 11), (31, 14), (28, 20)]: im.disc(bx, by, 2, G(BONE, 3, 10))
        im.line(24, 11, 33, 14, BONE[2], 1.5)
        im.disc(40, 11, 2.6, G(BONE, 4, 11)); im.put(40, 11, BONE[1])
        im.disc(39, 21, 3, G(GLASS_V, 2, 12))
    return im.finish()


# ------------------------------------------------------------------------------------------------ candles
CANDLE_SPOTS = [(5, 6, 2.0), (10, 4, 1.7), (12, 10, 2.2), (6, 12, 1.6)]


def candles():
    im = Img(18, 18)
    for (x, y, r) in CANDLE_SPOTS:
        im.disc(x + .5, y + 1.5, r + 1.2, G(BONE, 1, 1))
        im.disc(x, y, r, G(BONE, 4, 2)); im.put(x, y, BONE[0])
    return im.finish()


def flames(frame):
    im = Img(18, 18)
    rnd = random.Random(frame * 13 + 1)
    for (x, y, r) in CANDLE_SPOTS:
        ox = rnd.choice((0, 0, -1, 1)); oy = rnd.choice((0, -1))
        im.blend(x + ox, y - 1 + oy, (200, 90, 30, 255), .9); im.blend(x + ox, y - 2 + oy, (150, 50, 20, 255), .7)
        for dx, dy in ((1, 0), (-1, 0), (0, 1)): im.blend(x + ox + dx, y - 1 + oy + dy, (120, 40, 15, 255), .35)
    return im


def icon():
    im = Img(24, 24)
    im.disc(12, 12, 10.5, G(ST, 2, 1)); im.ring(12, 12, 8.6, 9.8, RED[4])
    p = [(12 + math.cos(-math.pi / 2 + k * 2 * math.pi / 5) * 8.2, 12 + math.sin(-math.pi / 2 + k * 2 * math.pi / 5) * 8.2) for k in range(5)]
    for k in range(5): a, b = p[(k * 2) % 5], p[(k * 2 + 2) % 5]; im.line(a[0], a[1], b[0], b[1], RED[5])
    return im.finish()


# ------------------------------------------------------------------------------------------------ debris
def debris(kind, v):
    """what is left of a broken thing, scattered by the game like its crates' planks: splinters, stone, wax"""
    im = Img(22, 22)
    rnd = random.Random({"wood": 3, "stone": 5, "wax": 7}[kind] * 31 + v)
    if kind == "wood":
        for _ in range(3 + v):
            x, y = rnd.uniform(5, 17), rnd.uniform(5, 17); a = rnd.uniform(0, 3.14); L = rnd.uniform(4, 8)
            im.line(x - math.cos(a) * L / 2, y - math.sin(a) * L / 2, x + math.cos(a) * L / 2, y + math.sin(a) * L / 2, G(WOOD, 3, 20 + v), 2)
        for _ in range(4): im.put(rnd.uniform(4, 18), rnd.uniform(4, 18), WOOD[2])
    elif kind == "stone":
        for _ in range(3 + v):
            x, y, r = rnd.uniform(6, 16), rnd.uniform(6, 16), rnd.uniform(1.8, 3.4)
            im.disc(x, y, r, G(ST, 3, 30 + v, .5)); im.put(x - r / 2, y - r / 2, ST[5])
        for _ in range(5): im.put(rnd.uniform(3, 19), rnd.uniform(3, 19), ST[2])
    else:
        im.disc(11, 12, 5, G(BONE, 1, 40)); im.disc(10, 11, 3, G(BONE, 3, 41))
        for _ in range(2 + v):
            x, y, a = rnd.uniform(6, 16), rnd.uniform(6, 16), rnd.uniform(0, 3.14)
            im.line(x, y, x + math.cos(a) * 4, y + math.sin(a) * 4, G(BONE, 4, 42), 2)
    return im.finish()


def rubble():
    """the gates' way blocked with fallen stone after the visit: a heap between the posts, no dark backing"""
    im = Img(40, 18)
    rnd = random.Random(9)
    for _ in range(22):
        x, y, r = rnd.uniform(7, 33), rnd.uniform(6, 13), rnd.uniform(2, 3.8)
        im.disc(x, y, r, G(ST, rnd.choice((2, 3, 4)), rnd.randint(1, 99), .5)); im.put(x - r / 2, y - r / 2, ST[5])
    return im.finish()


# ------------------------------------------------------------------------------------------------ cages
IRON = [hx(c) for c in ("#0c0c10", "#17171d", "#24242c", "#33333d", "#45454f")]
ROT = [hx(c) for c in ("#141a10", "#222b19", "#323d24", "#45512f", "#5a6a3c")]          # rotting flesh
SKIN = [hx(c) for c in ("#1e1c1e", "#2e2b2d", "#423d3c", "#58514c", "#6e6560")]         # a dead prisoner's skin
RAG = [hx(c) for c in ("#120e0b", "#1e1812", "#2b231a", "#3a3024")]


def body(im, cx, cy, kind):
    """a dead (or not quite dead) prisoner lying curled in the cage, seen from above"""
    if kind == "skel":
        skull(im, cx - 5, cy - 6, .9)
        for t in range(5): im.line(cx - 3 + t * .4, cy - 2 + t * 2, cx + 3 + t * .4, cy - 2 + t * 2, G(BONE, 3, 50 + t), 1)   # ribs
        im.line(cx, cy - 3, cx + 2, cy + 8, BONE[2], 1.4)                                                     # spine
        im.line(cx + 2, cy + 8, cx + 7, cy + 11, G(BONE, 3, 51), 1.4); im.line(cx + 1, cy + 8, cx - 3, cy + 12, G(BONE, 3, 52), 1.4)  # legs
        im.line(cx - 3, cy, cx - 8, cy + 4, G(BONE, 3, 53), 1.2); im.line(cx + 3, cy, cx + 8, cy - 3, G(BONE, 3, 54), 1.2)          # arms
        return
    flesh = ROT if kind == "rot" else SKIN
    im.disc(cx - 5, cy - 6, 3.6, G(flesh, 3, 60)); im.disc(cx - 5.6, cy - 6.6, 1.6, G(flesh, 4, 61))      # head
    im.rect(cx - 4, cy - 3, cx + 4, cy + 6, G(RAG if kind == "zomb" else flesh, 2, 62, .5))                  # torso / rags
    im.line(cx - 3, cy - 1, cx - 9, cy + 3, G(flesh, 3, 63), 2); im.line(cx + 3, cy - 1, cx + 9, cy - 4, G(flesh, 3, 64), 2)
    im.line(cx - 2, cy + 6, cx - 5, cy + 12, G(RAG if kind == "zomb" else flesh, 2, 65), 2.2); im.line(cx + 2, cy + 6, cx + 7, cy + 11, G(RAG if kind == "zomb" else flesh, 2, 66), 2.2)
    if kind == "rot":
        for (x, y) in ((cx - 1, cy), (cx + 2, cy + 3), (cx - 6, cy - 5)): im.put(x, y, hx("#7a8a3a"))       # sores
    else:
        im.line(cx - 7, cy + 3, cx - 10, cy + 6, IRON[3]); im.line(cx + 6, cy + 11, cx + 9, cy + 13, IRON[3])  # shackles
    im.put(cx - 6, cy - 6, OL); im.put(cx - 4, cy - 6, OL)


def cage():
    """an iron cage seen from above, empty: a square frame and bars across, see-through between them - a live
    monster of the game stands inside it (drawn under the bars)"""
    im = Img(40, 40)
    for x in range(9, 33, 6): im.rect(x, 4, x + 1, 36, lambda xx, yy: IRON[3] if yy % 6 else IRON[4])     # the bars
    for (x0, y0, x1, y1) in ((3, 3, 37, 6), (3, 34, 37, 37), (3, 3, 6, 37), (34, 3, 37, 37)): im.rect(x0, y0, x1, y1, G(IRON, 2, 71))
    im.rect(3, 3, 37, 4, IRON[4]); im.rect(3, 19, 37, 20, G(IRON, 2, 72))                         # rim, middle band
    for (x, y) in ((4, 4), (35, 4), (4, 35), (35, 35)): im.put(x, y, IRON[4])                    # rivets
    im.ring(20, 20, 2.2, 3.4, G(IRON, 3, 73))                                                      # the ring it hangs by
    return im.finish()


def iron_debris(v):
    im = Img(22, 22)
    rnd = random.Random(80 + v)
    for _ in range(3 + v):
        x, y, a = rnd.uniform(5, 17), rnd.uniform(5, 17), rnd.uniform(0, 3.14)
        im.line(x - math.cos(a) * 4, y - math.sin(a) * 4, x + math.cos(a) * 4, y + math.sin(a) * 4, G(IRON, 3, 81 + v), 1.6)
    return im.finish()


def remains(kind):
    """what was in the cage, left on the floor when it breaks"""
    im = Img(30, 30)
    body(im, 15, 13, kind)
    return im.finish()


# ------------------------------------------------------------------------------------------------ books on the floor
BOOKC = [[hx(c) for c in ("#1a0a0c", "#2c1014", "#40161c")], [hx(c) for c in ("#0e1218", "#161e28", "#222c3a")],
         [hx(c) for c in ("#151008", "#22190d", "#33261a")], [hx(c) for c in ("#100c14", "#1c1424", "#2a1e36")]]


def books(v):
    """books dropped on the floor: closed ones at odd angles, an open one, loose pages"""
    im = Img(26, 26)
    rnd = random.Random(90 + v)

    def closed(cx, cy, ang, col):
        ca, sa = math.cos(ang), math.sin(ang)
        for y in range(26):
            for x in range(26):
                u = (x + .5 - cx) * ca + (y + .5 - cy) * sa; w = -(x + .5 - cx) * sa + (y + .5 - cy) * ca
                if abs(u) < 4.5 and abs(w) < 3.2:
                    c = col[2] if w < -2.2 else col[1]
                    if abs(u) > 3.8: c = BONE[2] if u > 0 else col[0]       # pages' edge / spine
                    im.put(x, y, grain([col[0], c, col[2]], 1, x, y, 91 + v) if c == col[1] else c)
    def opened(cx, cy):
        im.rect(cx - 6, cy - 4, cx + 6, cy + 4, OL); im.rect(cx - 5, cy - 3, cx, cy + 3, G(BONE, 3, 95)); im.rect(cx, cy - 3, cx + 5, cy + 3, G(BONE, 2, 96))
        for y in range(int(cy - 2), int(cy + 3), 2): im.line(cx - 4, y, cx - 1, y, BONE[1]); im.line(cx + 1, y, cx + 4, y, BONE[1])
    def page(x, y):
        im.rect(x, y, x + 3, y + 4, G(BONE, 3, 97)); im.put(x + 1, y + 1, BONE[1])
    if v == 0: closed(10, 10, .4, BOOKC[0]); closed(16, 16, -.9, BOOKC[1])
    elif v == 1: opened(13, 13); page(3, 18); page(20, 5)
    elif v == 2: closed(13, 12, 1.2, BOOKC[2]); page(5, 4); page(18, 19); page(7, 19)
    else: closed(9, 15, -.3, BOOKC[3]); closed(15, 9, .7, BOOKC[0]); closed(17, 17, 1.9, BOOKC[2])
    return im.finish()


# ------------------------------------------------------------------------------------------------ fireplace, shelves, benches
def fireplace():
    """a stone fireplace against a wall, seen from STRAIGHT ABOVE like everything in the game: the top of a thick
    U of stone (its back along the wall - the top of the picture - and two side walls), the ash pit with logs
    inside it, a flat hearth stone in front; the fire is a separate glowing picture"""
    im = Img(72, 32)
    # the back: a thick block of stone along the wall, its top lit
    im.rect(2, 1, 70, 9, lambda x, y: ST[5] if y == 1 else ST[2] if y == 8 else grain(ST, 4 if (x // 9) % 2 else 3, x, y, 140, .45))
    for x in range(11, 70, 9): im.rect(x, 2, x + 1, 8, ST[1])
    # the two side walls of the U, their tops seen from above
    for (x0, x1) in ((2, 15), (57, 70)):
        im.rect(x0, 8, x1, 27, lambda x, y, x0=x0: ST[5] if (x == x0 or y == 8) else ST[1] if (x == x1 - 1 or y == 26) else grain(ST, 4, x, y, 141, .45))
        im.rect(x0 + 1, 17, x1 - 1, 18, ST[2])                                         # a joint
    # the pit: dark ash and embers between them, logs lying in it
    im.rect(15, 9, 57, 27, lambda x, y: (36, 18, 14, 255) if h2(x, y, 142) < .12 else (18, 12, 12, 255) if h2(x, y, 143) < .5 else (26, 16, 14, 255))
    for (lx, ly, ang) in ((26, 17, .4), (36, 15, -.3), (46, 18, .25), (31, 21, -.6)):
        dx, dy = math.cos(ang) * 6, math.sin(ang) * 6
        im.line(lx - dx, ly - dy, lx + dx, ly + dy, G(WOOD, 2, 144), 3)
        im.line(lx - dx, ly - dy - 1, lx + dx, ly + dy - 1, WOOD[4])
        im.disc(lx + dx, ly + dy, 1.4, WOOD[5])                                         # a cut end
    # the hearth stone in front (the bottom of the picture: the room side)
    im.rect(12, 27, 60, 32, lambda x, y: ST[4] if y == 27 else grain(ST, 3, x, y, 145, .45))
    for x in (24, 36, 48): im.rect(x, 28, x + 1, 32, ST[2])
    return im.finish()


def fire(frame):
    """the flames seen from above: tongues of fire on the logs, brightest in the middle"""
    im = Img(72, 32)
    rnd = random.Random(frame * 7 + 3)
    for _ in range(16):
        x, y = rnd.uniform(20, 52), rnd.uniform(12, 24)
        r = rnd.uniform(1.4, 3.2)
        im.disc(x, y, r, (210, 80, 24, 200)); im.disc(x, y, r * .55, (250, 170, 70, 255))
    for _ in range(8): im.put(rnd.uniform(16, 56), rnd.uniform(9, 26), (255, 200, 110, 255))
    return im


def shelf():
    """a bookcase against the wall seen from above: the tops of its books in a row, a skull and a candle on it"""
    im = Img(52, 18)
    im.rect(1, 1, 51, 17, lambda x, y: WOOD[4] if y == 1 else WOOD[0] if y >= 15 else grain(WOOD, 2, x, y, 150, .5))
    rnd = random.Random(151)
    x = 3
    while x < 48:
        w = rnd.choice((2, 2, 3)); col = rnd.choice(BOOKC)
        h0 = rnd.randint(4, 7)
        im.rect(x, h0, x + w, 14, lambda xx, yy, col=col: grain(col, 1, xx, yy, 152, .4)); im.rect(x, h0, x + w, h0 + 1, col[2])
        x += w + (1 if rnd.random() < .2 else 0)
        if rnd.random() < .08: x += 4
    skull(im, 40, 7, .6)
    return im.finish()


def bench():
    im = Img(46, 14)
    im.rect(1, 2, 45, 12, lambda x, y: WOOD[4] if y == 2 else WOOD[1] if y >= 11 else grain(WOOD, 3 if (y // 3) % 2 else 2, x, y, 160, .5))
    for x in (4, 41): im.rect(x, 1, x + 2, 13, WOOD[1])
    return im.finish()


# ------------------------------------------------------------------------------------------------ the cult's things
# a dark cult's halls: candles everywhere, red banners, iron candelabra, braziers of red fire, hooded statues,
# sacrifice slabs, lecterns, obelisks, skull heaps with candles, sigils drawn on the floor
CREAM = [hx(c) for c in ("#3a332a", "#5e554a", "#8a7f70", "#b3a896", "#d6ccb8")]


def candelabra():
    """an iron candle stand seen from above: a ring on three feet, five arms, a candle on each"""
    im = Img(26, 26)
    c = 13
    for k in range(3):
        a = k * 2 * math.pi / 3 + .5
        im.line(c, c, c + math.cos(a) * 11, c + math.sin(a) * 11, G(IRON, 2, 400 + k), 1.6)
    im.ring(c, c, 6.2, 7.6, G(IRON, 3, 403))
    for k in range(5):
        a = -math.pi / 2 + k * 2 * math.pi / 5
        x, y = c + math.cos(a) * 6.9, c + math.sin(a) * 6.9
        im.disc(x, y, 1.9, G(CREAM, 3, 404 + k)); im.put(x, y, CREAM[0])
    im.disc(c, c, 2.4, G(IRON, 3, 410)); im.disc(c, c, 1.4, G(CREAM, 3, 411))
    return im.finish()


def candelabra_flames(frame):
    im = Img(26, 26)
    rnd = random.Random(frame * 5 + 9)
    c = 13
    for k in range(6):
        if k < 5:
            a = -math.pi / 2 + k * 2 * math.pi / 5
            x, y = c + math.cos(a) * 6.9, c + math.sin(a) * 6.9
        else:
            x, y = c, c
        ox = rnd.choice((0, 0, -1, 1))
        im.blend(x + ox, y - 1, (210, 100, 35, 255), .95); im.blend(x + ox, y - 2, (150, 50, 20, 255), .7)
        im.blend(x + ox + 1, y - 1, (120, 40, 15, 255), .35); im.blend(x + ox - 1, y - 1, (120, 40, 15, 255), .35)
    return im


def banner():
    """a red banner hung on the wall: an iron rod along the wall (top), the cloth hanging into the room, gold trim,
    a sigil, a torn hem"""
    im = Img(22, 34)
    im.rect(1, 1, 21, 4, G(IRON, 3, 420)); im.put(1, 2, IRON[4]); im.put(20, 2, IRON[4])
    for y in range(4, 32):
        for x in range(4, 18):
            hem = 28 + (2 if x % 4 < 2 else 0) + (1 if x in (4, 17) else 0)
            if y > hem: continue
            if x in (4, 17) or y == 4: c = grain(GOLD, 2, x, y, 421)
            elif x in (5, 16): c = grain(RED, 1, x, y, 422)
            else: c = grain(RED, 3 if (x - 4) % 5 else 2, x, y, 423, .45)
            im.put(x, y, c)
    im.ring(11, 15, 3.2, 4.2, GOLD[3])
    for (a, b) in (((11, 11.5), (8, 17)), ((8, 17), (14, 17)), ((14, 17), (11, 11.5))): im.line(a[0], a[1], b[0], b[1], GOLD[2])
    return im.finish()


def brazier_red():
    """an iron fire bowl on three feet, full of coals (the red fire is a separate glowing picture)"""
    im = Img(28, 28)
    c = 14
    for k in range(3):
        a = k * 2 * math.pi / 3 + .3
        im.disc(c + math.cos(a) * 11, c + math.sin(a) * 11, 2.2, G(IRON, 2, 430 + k))
    im.disc(c, c, 10, G(IRON, 2, 433)); im.ring(c, c, 8.6, 10, G(IRON, 3, 434))
    im.disc(c, c, 8, lambda x, y: (40, 10, 8, 255) if h2(x, y, 435) > .3 else (90, 25, 12, 255))
    for k in range(8): im.put(c + math.cos(k) * 9.3, c + math.sin(k) * 9.3, IRON[4])
    return im.finish()


def red_fire(frame):
    im = Img(28, 28)
    rnd = random.Random(frame * 11 + 2)
    for _ in range(14):
        x, y = 14 + rnd.uniform(-6, 6), 14 + rnd.uniform(-6, 5)
        r = rnd.uniform(1, 2.6)
        im.disc(x, y, r, (170, 30, 40, 220)); im.disc(x, y - .6, r * .55, (240, 90, 70, 255))
    for _ in range(5): im.put(14 + rnd.uniform(-8, 8), 14 + rnd.uniform(-9, 2), (255, 140, 110, 255))
    return im


def statue():
    """a hooded cultist of stone on a plinth, seen from above: the hood, the shoulders, hands holding a red orb"""
    im = Img(32, 32)
    im.rect(3, 3, 29, 29, lambda x, y: MARBLE[5] if (x == 3 or y == 3) else MARBLE[1] if (x == 28 or y == 28) else marble(x, y, 3, 440))
    im.disc(16, 15, 10, lambda x, y: marble(x, y, 2, 441))                  # the robe's shoulders
    im.disc(15, 15, 8.5, lambda x, y: marble(x, y, 3, 442))
    im.disc(16, 13, 5.6, lambda x, y: marble(x, y, 1, 443))                 # the hood
    im.disc(15, 12, 4.4, lambda x, y: marble(x, y, 3, 444)); im.disc(14, 11, 1.8, MARBLE[6])
    im.disc(16, 22.5, 2.8, RED[4]); im.disc(15.4, 22, 1.2, RED[6])           # the orb
    for x in (12, 20): im.disc(x, 22, 1.6, lambda xx, yy: marble(xx, yy, 4, 445))   # hands
    return im.finish()


def sacrifice():
    """a long stone slab with chains and shackles at its corners, blood on it and a knife"""
    im = Img(50, 30)
    im.rect(3, 4, 47, 26, lambda x, y: ST[5] if y == 4 else ST[1] if y >= 24 else grain(ST, 3, x, y, 450, .5))
    for (bx, by, r) in ((22, 14, 6), (30, 17, 4)):
        for _ in range(int(r * r * 2)):
            a = random.random() * 6.28; d = r * math.sqrt(random.random())
            im.blend(bx + math.cos(a) * d * 1.3, by + math.sin(a) * d, RED[1], .85)
    for (x, y) in ((5, 6), (44, 6), (5, 23), (44, 23)):
        im.ring(x + .5, y + .5, 1.2, 2.2, IRON[4])
        for t in range(5): im.put(x + (t if x < 20 else -t) * .9, y + (3 + t if y < 15 else -3 - t) * .6, IRON[3])
    im.line(34, 10, 41, 7, IRON[4]); im.rect(32, 9, 35, 12, WOOD[2])
    return im.finish()


def lectern():
    """a reading stand with a great open book and a candle"""
    im = Img(26, 24)
    im.rect(3, 6, 23, 20, lambda x, y: WOOD[4] if y == 6 else WOOD[1] if y >= 18 else grain(WOOD, 3, x, y, 460, .5))
    im.rect(5, 7, 21, 17, OL); im.rect(6, 8, 13, 16, G(BONE, 3, 461)); im.rect(13, 8, 20, 16, G(BONE, 2, 462))
    for y in range(9, 15, 2): im.line(7, y, 12, y, BONE[1]); im.line(14, y, 19, y, BONE[1])
    rune(im, 15, 9, 3, RED[3])
    im.disc(21, 4, 1.8, G(CREAM, 3, 463)); im.put(21, 4, CREAM[0])
    return im.finish()


def obelisk():
    """a black stone obelisk seen from above: a four-sided point, red runes on its faces"""
    im = Img(26, 26)
    c = 13
    for y in range(26):
        for x in range(26):
            dx, dy = x + .5 - c, y + .5 - c
            if abs(dx) + abs(dy) > 10.5: continue
            face = (0 if dy < 0 else 2) + (0 if dx < 0 else 1)
            shade = (5, 3, 4, 1)[face]
            im.put(x, y, grain(ST, shade, x, y, 470 + face, .4))
    for (x, y) in ((9, 9), (15, 9), (9, 15), (15, 15)): rune(im, x, y, x + y, RED[4])
    im.put(c - .5, c - .5, ST[6])
    return im.finish()


def obelisk_glow(frame):
    im = Img(34, 34)
    a = 50 if frame == 0 else 80
    im.ring(17, 17, 12, 14.5, (150, 20, 30, a))
    return im


def skullpile():
    """a heap of skulls with candles stuck on it"""
    im = Img(28, 26)
    for (x, y, s) in ((8, 16, .8), (19, 16, .8), (13, 18, .85), (11, 10, .75), (17, 10, .75), (14, 5, .7)):
        skull(im, x, y, s)
    for (x, y) in ((8, 9), (20, 8), (14, 3)):
        im.disc(x, y, 1.6, G(CREAM, 3, 480)); im.put(x, y, CREAM[0])
    return im.finish()


def skullpile_flames(frame):
    im = Img(28, 26)
    rnd = random.Random(frame * 3 + 1)
    for (x, y) in ((8, 9), (20, 8), (14, 3)):
        ox = rnd.choice((0, 0, -1, 1))
        im.blend(x + ox, y - 1, (210, 100, 35, 255), .95); im.blend(x + ox, y - 2, (150, 50, 20, 255), .7)
    return im


def sigil(v):
    """sigils drawn on the floor in dried blood: a triangle in a circle, a small star, a ring of runes"""
    n = (48, 36, 44)[v]
    im = Img(n, n)
    c = n / 2
    col = RED[3]
    if v == 0:
        im.ring(c, c, c - 3, c - 2, col)
        pts = [(c + math.cos(-math.pi / 2 + k * 2 * math.pi / 3) * (c - 3), c + math.sin(-math.pi / 2 + k * 2 * math.pi / 3) * (c - 3)) for k in range(3)]
        for k in range(3): im.line(pts[k][0], pts[k][1], pts[(k + 1) % 3][0], pts[(k + 1) % 3][1], col)
        im.ring(c, c, 3, 4, col)
    elif v == 1:
        pts = [(c + math.cos(-math.pi / 2 + k * 2 * math.pi / 5) * (c - 3), c + math.sin(-math.pi / 2 + k * 2 * math.pi / 5) * (c - 3)) for k in range(5)]
        for k in range(5): a, b = pts[(k * 2) % 5], pts[(k * 2 + 2) % 5]; im.line(a[0], a[1], b[0], b[1], col)
    else:
        im.ring(c, c, c - 3, c - 2, col); im.ring(c, c, c - 9, c - 8, col)
        for k in range(10):
            a = k * 2 * math.pi / 10
            rune(im, c + math.cos(a) * (c - 5.5) - 1, c + math.sin(a) * (c - 5.5) - 2, k, col)
    return im


def cult_parts(parts):
    parts["occ_candelabra"] = candelabra()
    for f in range(3): parts["occ_candelabra_fl_%d" % f] = candelabra_flames(f)
    parts["occ_banner"] = banner()
    parts["occ_brazier_red"] = brazier_red()
    for f in range(3): parts["occ_rfire_%d" % f] = red_fire(f)
    parts["occ_statue"] = statue()
    parts["occ_sacrifice"] = sacrifice()
    parts["occ_lectern"] = lectern()
    parts["occ_obelisk"] = obelisk()
    for f in range(2): parts["occ_obelisk_glow_%d" % f] = obelisk_glow(f)
    parts["occ_skullpile"] = skullpile()
    for f in range(3): parts["occ_skullpile_fl_%d" % f] = skullpile_flames(f)
    for v in range(3): parts["occ_sigil_%d" % v] = sigil(v)


# ------------------------------------------------------------------------------------------------ the cultist (an enemy)
# parts drawn at the same pixel sizes as the game's goblin swordsman's (the mod puts them on that monster with the
# goblin's own pivots and pixels-per-unit): seen from above, facing up (the top of each picture is the front)
CLOAK = [hx(c) for c in ("#060508", "#0e0c10", "#17141a", "#211d25", "#2c2731", "#38323e")]
TRIM = [hx(c) for c in ("#3a0a0e", "#5e1016", "#86181e", "#a82a26")]
SKIN = [hx(c) for c in ("#3c3632", "#5a524c", "#7a706a", "#968b84")]


def c_body():
    """24 x 24: the shoulders and the cloak from above, its edge trimmed with red"""
    im = Img(24, 24)
    cx, cy = 12, 13
    for y in range(24):
        for x in range(24):
            dx, dy = (x + .5 - cx) / 10.5, (y + .5 - cy) / 7.5
            d = dx * dx + dy * dy
            if d > 1: continue
            if d > .72 and dy > -.35: c = grain(TRIM, 2 if d < .88 else 1, x, y, 500)     # the red hem
            else:
                c = grain(CLOAK, 3 if dy < -.3 else 2, x, y, 501, .4)
                if abs(x + .5 - cx) < .8 and dy > -.2: c = CLOAK[1]                     # the cloak's fold down the back
            im.put(x, y, c)
    im.rect(cx - 3, cy - 8, cx + 3, cy - 5, G(CLOAK, 1, 502))                          # the collar under the hood
    return im.finish()


def c_head():
    """14 x 12: the hood, its opening at the front (top), a dark face with two dull red eyes"""
    im = Img(14, 12)
    im.disc(7, 6.5, 5.3, G(CLOAK, 3, 510, .4))
    im.disc(7, 8.5, 3.8, G(CLOAK, 2, 511))                                              # the hood's peak behind
    im.disc(7, 3.6, 3.0, CLOAK[0])                                                      # the opening
    for x in range(4, 11): im.put(x, 1.5 + abs(x - 7) * .35, TRIM[2])                    # red trim round the opening
    im.put(5.6, 3.5, TRIM[3]); im.put(8.4, 3.5, TRIM[3])                                # eyes
    im.disc(5.5, 5.5, 1, CLOAK[4])
    return im.finish()


def c_upperarm():
    im = Img(12, 12)
    im.disc(6, 6, 3.6, G(CLOAK, 3, 520)); im.disc(5.3, 5.3, 1.4, CLOAK[4])
    return im.finish()


def c_forearm():
    """10 x 12: a sleeve; its end (towards the hand, the top) trimmed red"""
    im = Img(10, 12)
    im.rect(2, 3, 8, 10, G(CLOAK, 3, 530)); im.rect(2, 2, 8, 4, G(TRIM, 2, 531))
    im.put(3, 5, CLOAK[4])
    return im.finish()


def c_hand():
    """12 x 12: a pale bare fist round the pivot (2/3, 2/3 from the lower left = the upper middle)"""
    im = Img(12, 12)
    im.disc(8, 4, 2.7, G(SKIN, 2, 540)); im.disc(7.4, 3.4, 1.1, SKIN[3])
    for k in range(3): im.put(6.5 + k * 1.2, 1.8, SKIN[1])
    return im.finish()


def c_leg():
    im = Img(12, 12)
    im.disc(6, 6, 3.0, G(CLOAK, 2, 550)); im.put(5, 5, CLOAK[4])
    return im.finish()


def c_preview():
    """36 x 36: the whole cultist (the bestiary's picture)"""
    im = Img(36, 36)
    b, h = c_body(), c_head()
    for y in range(24):
        for x in range(24):
            p = b.p[y][x]
            if p[3]: im.put(x + 6, y + 9, p)
    for (hx_, hy) in ((5, 10), (31, 10)):
        im.disc(hx_, hy + 6, 3, G(CLOAK, 3, 560)); im.rect(hx_ - 2, hy + 1, hx_ + 3, hy + 3, G(TRIM, 2, 561)); im.disc(hx_ + .5, hy - .5, 2.4, G(SKIN, 2, 562))
    for y in range(12):
        for x in range(14):
            p = h.p[y][x]
            if p[3]: im.put(x + 11, y + 6, p)
    return im


def c_dead():
    """36 x 36: a dead cultist lying spread on the floor in his cloak"""
    im = Img(36, 36)
    for y in range(36):
        for x in range(36):
            dx, dy = (x + .5 - 18) / 13, (y + .5 - 19) / 9
            d = dx * dx + dy * dy
            if d > 1: continue
            im.put(x, y, grain(TRIM, 2, x, y, 570) if d > .8 else grain(CLOAK, 2, x, y, 571, .4))
    im.disc(18, 9, 4.2, G(CLOAK, 3, 572)); im.disc(18, 8, 2.3, CLOAK[0])
    im.disc(5, 13, 2.2, G(SKIN, 2, 573)); im.disc(30, 26, 2.2, G(SKIN, 2, 574))
    for (bx, by) in ((14, 24), (22, 15)): im.blend(bx, by, RED[1], .9); im.blend(bx + 1, by, RED[0], .8)
    return im.finish()


def cultist_parts(parts):
    parts["cult_body"] = c_body(); parts["cult_head"] = c_head(); parts["cult_upperarm"] = c_upperarm()
    parts["cult_forearm"] = c_forearm(); parts["cult_hand"] = c_hand(); parts["cult_leg"] = c_leg()
    parts["cult_preview"] = c_preview(); parts["cult_dead"] = c_dead()


parts = {"occ_rubble": rubble(), "occ_floor_big": floor_big(), "occ_carpet": carpet_tile(0), "occ_carpet_end": carpet_tile(1), "occ_fireplace": fireplace(), "occ_shelf": shelf(), "occ_bench": bench(), "occ_altar": altar(), "occ_column": column(),
         "occ_candles": candles(), "occ_icon": icon()}
for f in range(2): parts["occ_circle_%d" % f] = circle(f)
for v in range(2): parts["occ_table_%d" % v] = table(v)
for f in range(3): parts["occ_flames_%d" % f] = flames(f)
for k, nv in (("wood", 3), ("stone", 3), ("wax", 2)):
    for v in range(nv): parts["occ_%s_destr_%d" % (k, v)] = debris(k, v)
parts["occ_cage"] = cage()
for v in range(3): parts["occ_blood_%d" % v] = blood(v)
for f in range(3): parts["occ_fire_%d" % f] = fire(f)
for v in range(3): parts["occ_iron_destr_%d" % v] = iron_debris(v)
for v in range(4): parts["occ_books_%d" % v] = books(v)
cult_parts(parts)
for old in os.listdir(OUT):
    if old.endswith(".png") and old[:-4] not in parts and not old.startswith(("occ_gate", "cult_", "devil_", "occ_portal_", "xbow_", "spear_", "occ_cracks", "occ_target", "occ_dummy", "occ_rack", "occ_bedroll", "occ_cart", "occ_rug", "occ_bones_", "occ_dais")): os.remove(os.path.join(OUT, old))   # occ_gate comes from gate_from_pantry.py   # e.g. the old portal frames
for n, im in parts.items(): im.save(n)
print("saved", len(parts), "pictures to", OUT)
