# Art of the Occult Sanctum mod: drawn pixel by pixel at the game's 24 px per tile, seen from straight above like the
# game's own props, 1 px dark outline + soft dark halo, palette sampled from the Void Crypt (floor #242438, wood
# #4e3c32, iron #54545e) and pushed to an occult crimson.  usage: python artgen.py <outdir>
import math, os, random, sys
from PIL import Image

OUT = sys.argv[1] if len(sys.argv) > 1 else "art"
os.makedirs(OUT, exist_ok=True)

def hx(h, a=255):
    h = h.lstrip("#"); return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)

OL = hx("#0b0608")
# stone of the sanctum: dark basalt with a red cast
ST = [hx(c) for c in ("#140d12", "#1d141b", "#271b24", "#32232e", "#3e2c39", "#4c3746")]
# the floor's flagstones: like the crypt's (#242438) but blood-dark
FL = [hx(c) for c in ("#120a0e", "#1a1015", "#22151b", "#2a1a21", "#321f27", "#3a242d")]
BONE = [hx(c) for c in ("#3b342e", "#5a5047", "#7c7064", "#a69280", "#c8b8a0")]
WOOD = [hx(c) for c in ("#0b0807", "#2a2019", "#3e3129", "#4e3c32", "#5a4134", "#6b4f3f")]
RED = [hx(c) for c in ("#2a060b", "#4a0d15", "#6b1520", "#8c1f2a", "#b0283a", "#d64646", "#ff7a5a")]
GOLD = [hx(c) for c in ("#3a2c14", "#5e4a22", "#8a6e34", "#b8964a")]
VIO = [hx(c) for c in ("#1a0820", "#30103c", "#4c1a5c", "#6e2a7c", "#9a44a0")]
CLEAR = (0, 0, 0, 0)


class Img:
    def __init__(s, w, h): s.w, s.h = w, h; s.p = [[CLEAR] * w for _ in range(h)]
    def ok(s, x, y): return 0 <= x < s.w and 0 <= y < s.h
    def get(s, x, y): return s.p[y][x] if s.ok(x, y) else CLEAR
    def put(s, x, y, c):
        x, y = int(math.floor(x)), int(math.floor(y))
        if s.ok(x, y): s.p[y][x] = c
    def blend(s, x, y, c, a=1.0):
        x, y = int(math.floor(x)), int(math.floor(y))
        if not s.ok(x, y): return
        sa = c[3] / 255 * a
        if sa <= 0: return
        d = s.p[y][x]; da = d[3] / 255; oa = sa + da * (1 - sa)
        s.p[y][x] = tuple(int(round((c[i] * sa + d[i] * da * (1 - sa)) / oa)) for i in range(3)) + (int(round(oa * 255)),)
    def disc(s, cx, cy, r, c, bl=False):
        for y in range(int(cy - r - 1), int(cy + r + 2)):
            for x in range(int(cx - r - 1), int(cx + r + 2)):
                if (x + .5 - cx) ** 2 + (y + .5 - cy) ** 2 <= r * r: (s.blend if bl else s.put)(x, y, c)
    def ring(s, cx, cy, r0, r1, c, bl=False):
        for y in range(int(cy - r1 - 1), int(cy + r1 + 2)):
            for x in range(int(cx - r1 - 1), int(cx + r1 + 2)):
                d = math.hypot(x + .5 - cx, y + .5 - cy)
                if r0 <= d <= r1: (s.blend if bl else s.put)(x, y, c)
    def rect(s, x0, y0, x1, y1, c):
        for y in range(int(y0), int(y1)):
            for x in range(int(x0), int(x1)): s.put(x, y, c)
    def line(s, x0, y0, x1, y1, c, w=1.0, bl=False):
        n = max(1, int(math.hypot(x1 - x0, y1 - y0) * 3)); seen = set()
        for i in range(n + 1):
            t = i / n; x = x0 + (x1 - x0) * t; y = y0 + (y1 - y0) * t
            if w <= 1.01:
                k = (int(math.floor(x)), int(math.floor(y)))
                if k not in seen: seen.add(k); (s.blend if bl else s.put)(k[0], k[1], c)
            else:
                r = w / 2
                for yy in range(int(y - r - 1), int(y + r + 2)):
                    for xx in range(int(x - r - 1), int(x + r + 2)):
                        if (xx + .5 - x) ** 2 + (yy + .5 - y) ** 2 <= r * r and (xx, yy) not in seen:
                            seen.add((xx, yy)); (s.blend if bl else s.put)(xx, yy, c)
    def outline(s, c=OL, thr=100):
        add = []
        for y in range(s.h):
            for x in range(s.w):
                if s.p[y][x][3] >= thr: continue
                if any(s.get(x + dx, y + dy)[3] >= thr for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))): add.append((x, y))
        for x, y in add: s.p[y][x] = c
    def halo(s):
        add = []
        for y in range(s.h):
            for x in range(s.w):
                if s.p[y][x][3] > 0: continue
                near = below = False
                for dy in (-1, 0, 1):
                    for dx in (-1, 0, 1):
                        if s.get(x + dx, y + dy)[3] > 200:
                            near = True
                            if dy == -1: below = True
                if near: add.append((x, y, 110 if below else 55))
        for x, y, a in add: s.p[y][x] = (0, 0, 0, a)
    def finish(s): s.outline(); s.halo(); return s
    def draw(s, o, ox, oy):
        for y in range(o.h):
            for x in range(o.w):
                c = o.p[y][x]
                if c[3]: s.blend(ox + x, oy + y, c)
    def save(s, name):
        im = Image.new("RGBA", (s.w, s.h)); im.putdata([c for row in s.p for c in row]); im.save(os.path.join(OUT, name + ".png"))
    def pil(s):
        im = Image.new("RGBA", (s.w, s.h)); im.putdata([c for row in s.p for c in row]); return im


def h2(x, y, seed):  # hash noise 0..1
    n = (x * 374761393 + y * 668265263 + seed * 2147483647) & 0xFFFFFFFF
    n = ((n ^ (n >> 13)) * 1274126177) & 0xFFFFFFFF
    return (n ^ (n >> 16)) / 0xFFFFFFFF


def skull(im, cx, cy, sc=1.0):
    """a small skull seen from above (the crown of the head, the brow, the eye pits)"""
    im.disc(cx, cy, 4.2 * sc, BONE[2]); im.disc(cx - .6 * sc, cy - .8 * sc, 3.0 * sc, BONE[3]); im.disc(cx - 1.2 * sc, cy - 1.6 * sc, 1.4 * sc, BONE[4])
    im.rect(cx - 2.5 * sc, cy + 2.6 * sc, cx + 2.5 * sc, cy + 4.6 * sc, BONE[2])
    im.put(cx - 2 * sc, cy + 1 * sc, OL); im.put(cx - 1 * sc, cy + 1 * sc, OL); im.put(cx + 1 * sc, cy + 1 * sc, OL); im.put(cx + 2 * sc - 1, cy + 1 * sc, OL)
    for k in range(-2, 3, 2): im.put(cx + k * sc, cy + 4 * sc, BONE[0])


def rune(im, x, y, k, c):
    """one of 8 little occult glyphs, 3x4 px"""
    G = ["010111010010", "101010101010", "111100111001", "100110101011", "011100110001", "111010010111", "101111101101", "001011101100"]
    g = G[k % len(G)]
    for i, ch in enumerate(g):
        if ch == "1": im.put(x + i % 3, y + i // 3, c)


# ------------------------------------------------------------------------------------------------ the gate
GW, GH = 96, 56                            # 4 x 2.3 tiles
LT = 20                                    # the lintel (over the wall) is this many px tall
PCX, PTOP, PRX, PRY = 47.5, LT - 2, 23.5, 30   # the portal: a half oval hanging from the lintel into the room
PILL = ((13.5, 29), (81.5, 29))            # the two pillars


def horn(im, px, py, side, size):
    """a curled ram's horn seen from above, growing from (px, py) outwards"""
    for t in range(34):
        a = t / 33
        r = size * (1 - a * .72)
        ang = math.pi * (1.0 + a * 1.7)
        x = px + side * (size * .45 + math.cos(ang) * r) * 1.0; y = py - size * .2 + math.sin(ang) * r * .85
        w = size * .32 * (1 - a * .6)
        im.disc(x, y, w + .9, BONE[0]); im.disc(x, y, w, BONE[2 if (t // 3) % 2 else 3]); im.disc(x - .4, y - .5, max(.5, w * .45), BONE[4])


def gate():
    im = Img(GW, GH)
    # the step in front of it: a half ring of worn red stone
    for y in range(LT - 4, GH):
        for x in range(GW):
            dx, dy = (x + .5 - PCX) / 33, (y + .5 - (LT - 4)) / 38
            d = dx * dx + dy * dy
            if 0.6 < d <= 1:
                a = math.atan2(y + .5 - (LT - 4), x + .5 - PCX)
                seg = int((a / math.pi) * 11)
                joint = abs((a / math.pi) * 11 - seg - .5) > .45
                im.put(x, y, ST[1] if joint else (ST[3] if d < .72 else ST[2]))
    # the portal's opening (filled by the glowing sprite) has a dark rim
    for y in range(PTOP, GH):
        for x in range(GW):
            d = ((x + .5 - PCX) / PRX) ** 2 + ((y + .5 - PTOP) / PRY) ** 2
            if 0.84 < d <= 1.0: im.put(x, y, ST[0])
    # the lintel: big blocks of black stone set into the wall
    for y in range(1, LT):
        for x in range(4, GW - 4):
            row = y // 7
            off = 7 if row % 2 else 0
            bx = (x - 4 + off) // 14
            jx = (x - 4 + off) % 14 == 0
            jy = y % 7 == 0
            c = ST[1] if (jx or jy) else (ST[4] if y % 7 == 1 else ST[3] if h2(bx, row, 3) > .5 else ST[2])
            if not (jx or jy) and h2(x, y, 9) < .07: c = ST[1]
            im.put(x, y, c)
    # glowing runes cut along the lintel
    for i, x in enumerate([24, 31, 59, 66]): rune(im, x, 8 + (i % 2) * 2, i, RED[4])
    # the keystone: a horned skull over the door
    im.rect(38, 0, 58, LT + 1, ST[2]); im.rect(39, 0, 57, 2, ST[4]); im.rect(38, LT, 58, LT + 1, ST[1])
    skull(im, 48, 10, 1.5)
    for side in (-1, 1):   # the skull's horns, sweeping out along the lintel
        for t in range(14):
            a = t / 13
            x = 48 + side * (6 + a * 8); y = 6 - math.sin(a * 2.6) * 4 + a * 3
            im.disc(x, y, 1.6 - a * .8, BONE[1]); im.disc(x - .3, y - .4, 1.0 - a * .5, BONE[3 if t < 8 else 2])
    # the pillars: round, with a band of red runes, a curled ram's horn on the outer side
    for (px, py) in PILL:
        side = -1 if px < GW / 2 else 1
        im.disc(px, py, 11, ST[1]); im.disc(px, py, 10, ST[2]); im.disc(px - 1, py - 1, 8, ST[3])
        im.ring(px, py, 6.3, 7.4, RED[1])
        for k in range(10):
            a = k * math.pi / 5
            im.put(px + math.cos(a) * 6.85, py + math.sin(a) * 6.85, RED[4])
        im.disc(px - 1, py - 1, 5.2, ST[4]); im.disc(px - 2, py - 2, 2.6, ST[5])
        im.disc(px - 1, py - 1, 1.6, RED[3])
    return im.finish()


def portal(frame):
    """the glowing surface in the doorway (drawn without the game's lighting: it shines in the dark)"""
    im = Img(GW, GH)
    for y in range(PTOP, GH):
        for x in range(GW):
            dx, dy = (x + .5 - PCX) / PRX, (y + .5 - PTOP) / PRY
            d = dx * dx + dy * dy
            if d > .86: continue
            r = math.sqrt(d)
            a = math.atan2(dy, dx)
            sw = math.sin(a * 3 + r * 9 - frame * math.pi / 2)
            v = (1 - r) * .55 + (sw * .5 + .5) * .45
            if r > .78: c = RED[5]
            elif r > .68: c = RED[4]
            elif v > .62: c = RED[4] if frame % 2 == 0 else RED[3]
            elif v > .45: c = VIO[3]
            elif v > .3: c = VIO[2]
            elif v > .18: c = VIO[1]
            else: c = VIO[0]
            if r < .2 and (frame + int(x + y)) % 5 == 0: c = RED[6]
            im.put(x, y, c)
    # sparks rising out of it
    rnd = random.Random(frame * 31 + 7)
    for _ in range(6):
        x = PCX + rnd.uniform(-16, 16); y = PTOP + rnd.uniform(5, 24)
        im.put(x, y, RED[6]); im.blend(x + 1, y, RED[5], .6)
    return im


# ------------------------------------------------------------------------------------------------ the floor
RS = 15                     # the room is 15 x 15 tiles
FWP = RS * 24               # 360 px


def floor():
    im = Img(FWP, FWP)
    rnd = random.Random(5)
    # flagstones like the crypt's own floor: irregular slabs with dark joints (a Voronoi of jittered points)
    pts = []
    cell = 17
    for gy in range(-1, FWP // cell + 2):
        for gx in range(-1, FWP // cell + 2):
            pts.append(((gx + rnd.uniform(.15, .85)) * cell, (gy + rnd.uniform(.15, .85)) * cell, rnd.random()))
    grid = {}
    for i, (px, py, _) in enumerate(pts): grid.setdefault((int(px // cell), int(py // cell)), []).append(i)
    for y in range(FWP):
        for x in range(FWP):
            gx, gy = int(x // cell), int(y // cell)
            best = second = 1e9; bi = -1
            for oy in (-1, 0, 1):
                for ox in (-1, 0, 1):
                    for i in grid.get((gx + ox, gy + oy), ()):
                        d = (pts[i][0] - x - .5) ** 2 + (pts[i][1] - y - .5) ** 2
                        if d < best: second = best; best = d; bi = i
                        elif d < second: second = d
            edge = math.sqrt(second) - math.sqrt(best)
            shade = pts[bi][2]
            if edge < 1.1: c = FL[0]
            elif edge < 2.0: c = FL[1]
            else:
                c = FL[2] if shade < .3 else FL[3] if shade < .75 else FL[4]
                if edge < 3.2 and (pts[bi][1] < y): c = FL[min(5, FL.index(c) + 1)]   # light on the slab's upper rim
                if h2(x, y, 11) < .05: c = FL[1]
            im.put(x, y, c)
    cx = FWP / 2
    # the red carpet from the gate (south) to the altar (north), broken by the ritual circle
    def carpet(y0, y1):
        x0, x1 = cx - 34, cx + 34
        for y in range(int(y0), int(y1)):
            for x in range(int(x0), int(x1)):
                dx = min(x - x0, x1 - 1 - x)
                if dx < 2: c = GOLD[1] if dx == 1 else GOLD[0]
                elif dx < 4: c = RED[1]
                elif dx == 4: c = GOLD[2]
                else:
                    ux = abs(x - cx); uy = (y - y0) % 24 - 12
                    c = RED[2]
                    if abs(ux + abs(uy) - 14) < 1.0: c = GOLD[1]          # a diamond pattern
                    elif ux + abs(uy) < 5: c = RED[3]
                    if h2(x, y, 21) < .08: c = RED[1]
                im.put(x, y, c)
        for x in range(int(x0), int(x1), 2):    # fringes
            im.put(x, y0 - 1, GOLD[1]); im.put(x, y1, GOLD[1])
    carpet(cx + 64, FWP - 4)
    carpet(3 * 24 + 10, cx - 64)
    # blood: spilled at the altar and run into the joints round the circle
    for (bx, by, r) in [(cx - 20, 70, 9), (cx + 26, 64, 6), (cx + 70, 200, 5), (cx - 80, 150, 6), (cx + 12, 250, 4)]:
        for _ in range(int(r * r * 2.2)):
            a = rnd.uniform(0, 6.283); d = r * math.sqrt(rnd.random())
            im.blend(bx + math.cos(a) * d * 1.4, by + math.sin(a) * d, RED[0], .85)
            im.blend(bx + math.cos(a) * d * .8, by + math.sin(a) * d * .6, RED[1], .7)
    # a darker band along the walls (the game's walls drop a soft shadow on the floor)
    for y in range(FWP):
        for x in range(FWP):
            e = min(x, y, FWP - 1 - x, FWP - 1 - y)
            if e < 6: im.blend(x, y, (0, 0, 0, 255), (6 - e) / 6 * .45)
    return im


# ------------------------------------------------------------------------------------------------ ritual circle
CW = 132


def circle(frame):
    im = Img(CW, CW)
    c = CW / 2
    core = RED[4] if frame == 0 else RED[5]
    hi = RED[5] if frame == 0 else RED[6]
    glow = (RED[3][0], RED[3][1], RED[3][2], 70 if frame == 0 else 105)
    R1, R2 = 61, 50
    pts = [(c + math.cos(-math.pi / 2 + k * 2 * math.pi / 5) * R2, c + math.sin(-math.pi / 2 + k * 2 * math.pi / 5) * R2) for k in range(5)]
    star = [pts[(k * 2) % 5] for k in range(6)]
    # a soft glow under every line first
    im.ring(c, c, R1 - 3, R1 + 3, glow, True); im.ring(c, c, R2 - 3, R2 + 3, glow, True)
    for a, b in zip(star, star[1:]): im.line(a[0], a[1], b[0], b[1], glow, 5, True)
    im.ring(c, c, R1 - .9, R1 + .9, core); im.ring(c, c, R2 - .7, R2 + .7, core)
    im.ring(c, c, R1 - 4.6, R1 - 3.6, RED[3])
    for a, b in zip(star, star[1:]): im.line(a[0], a[1], b[0], b[1], core, 1.6)
    # glyphs between the two rings
    for k in range(16):
        a = k * 2 * math.pi / 16 + .2
        rune(im, c + math.cos(a) * 55 - 1, c + math.sin(a) * 55 - 2, k, hi)
    # candles' marks at the star's points and a dark heart in the middle
    for (x, y) in pts: im.disc(x, y, 2.4, hi)
    im.ring(c, c, 7, 8.4, core); im.disc(c, c, 6.5, (40, 6, 10, 200))
    return im


# ------------------------------------------------------------------------------------------------ altar
def altar():
    im = Img(68, 38)
    x0, y0, x1, y1 = 3, 6, 65, 34
    for y in range(y0, y1):
        for x in range(x0, x1):
            c = ST[2]
            if y == y0 or y == y0 + 1: c = ST[4]
            elif y >= y1 - 3: c = ST[1]
            elif x <= x0 + 1: c = ST[3]
            elif x >= x1 - 2: c = ST[1]
            elif h2(x, y, 4) < .06: c = ST[1]
            im.put(x, y, c)
    # a red cloth over it, hanging down the front
    for y in range(y0 + 2, y1 + 3):
        for x in range(26, 42):
            c = RED[2] if 27 < x < 40 else RED[1]
            if y > y1 - 1 and (x % 3 == 0): continue
            if x in (28, 39): c = GOLD[1]
            im.put(x, y, c)
    for k in range(3): rune(im, 32 + (k % 2), y0 + 6 + k * 7, k + 2, GOLD[2])
    skull(im, 34, 15, 1.3)
    # an open black book on the left, a ritual knife and a bowl of blood on the right
    im.rect(8, 11, 22, 23, OL); im.rect(9, 12, 15, 22, BONE[3]); im.rect(15, 12, 21, 22, BONE[2])
    for y in range(14, 21, 2): im.line(10, y, 14, y, BONE[1]); im.line(16, y, 20, y, BONE[0])
    im.put(15, 12, OL); rune(im, 16, 14, 5, RED[3])
    im.line(46, 24, 58, 13, BONE[4], 1); im.line(47, 24, 59, 13, hx("#8c93a3"), 1); im.rect(44, 23, 48, 27, WOOD[2]); im.put(45, 24, GOLD[3])
    im.disc(54, 26, 4.2, ST[3]); im.disc(54, 26, 3.0, RED[1]); im.disc(53, 25, 1.2, RED[3])
    return im.finish()


# ------------------------------------------------------------------------------------------------ column
def column():
    im = Img(38, 38)
    c = 19
    im.disc(c, c, 16.5, ST[1]); im.disc(c, c, 15, ST[2]); im.disc(c - 1, c - 1, 12.5, ST[3])
    im.ring(c, c, 10, 11.4, RED[1])
    for k in range(10):
        a = k * math.pi / 5
        rune(im, c + math.cos(a) * 13.2 - 1, c + math.sin(a) * 13.2 - 2, k, RED[3])
    im.disc(c - 1, c - 1, 8.5, ST[4]); im.disc(c - 2, c - 2, 5, ST[5])
    for k in range(5):   # a little pentagram cut into the top
        a1 = -math.pi / 2 + k * 4 * math.pi / 5; a2 = -math.pi / 2 + (k + 1) * 4 * math.pi / 5
        im.line(c - 1 + math.cos(a1) * 6, c - 1 + math.sin(a1) * 6, c - 1 + math.cos(a2) * 6, c - 1 + math.sin(a2) * 6, RED[2])
    return im.finish()


# ------------------------------------------------------------------------------------------------ tables
def table(var):
    im = Img(52, 34)
    x0, y0, x1, y1 = 2, 3, 50, 31
    for y in range(y0, y1):
        plank = (y - y0) // 7
        for x in range(x0, x1):
            j = (y - y0) % 7 == 6
            c = WOOD[1] if j else (WOOD[4] if (y - y0) % 7 == 0 else WOOD[3] if plank % 2 else WOOD[2])
            if not j and h2(x // 5, y, plank + 3) < .1: c = WOOD[2]
            im.put(x, y, c)
    for x in (x0 + 1, x1 - 2):
        for y in (y0 + 2, y1 - 3): im.put(x, y, WOOD[0])
    if var == 0:
        skull(im, 13, 14, 1.0)
        im.rect(25, 9, 38, 21, OL); im.rect(26, 10, 31, 20, BONE[3]); im.rect(31, 10, 37, 20, BONE[2])
        for y in range(12, 19, 2): im.line(27, y, 30, y, BONE[1])
        rune(im, 32, 12, 1, RED[3])
        im.disc(44, 11, 3.2, VIO[2]); im.disc(43, 10, 1.2, VIO[4]); im.rect(43, 6, 46, 8, WOOD[1])
        im.disc(42, 23, 2.6, hx("#2e5a3a")); im.disc(41, 22, 1, hx("#7ac08a"))
        im.disc(18, 25, 3.0, BONE[4]); im.disc(18, 25, 1.4, BONE[2])           # a candle stub
    else:
        im.rect(7, 8, 22, 13, BONE[3]); im.rect(7, 8, 9, 13, BONE[1]); im.rect(20, 8, 22, 13, BONE[1])   # a scroll
        for x in range(10, 19, 2): im.put(x, 10, RED[2])
        im.disc(12, 22, 4.6, ST[3]); im.disc(12, 22, 3.3, RED[1]); im.disc(11, 21, 1.2, RED[3])   # a bowl of blood
        for (bx, by) in [(28, 12), (33, 15), (30, 22)]: im.disc(bx, by, 2.4, BONE[3]); im.put(bx - 1, by - 1, BONE[4])  # bones
        im.line(26, 12, 35, 15, BONE[2], 1.5)
        im.disc(43, 12, 3.0, BONE[4]); im.disc(43, 12, 1.4, BONE[2])
        im.disc(42, 23, 3.4, VIO[1]); im.disc(41, 22, 1.4, VIO[3]); im.rect(41, 18, 44, 20, WOOD[1])
    return im.finish()


# ------------------------------------------------------------------------------------------------ candles
CANDLE_SPOTS = [(5, 6, 2.2), (10, 4, 1.9), (12, 10, 2.4), (6, 12, 1.7)]


def candles():
    im = Img(18, 18)
    for (x, y, r) in CANDLE_SPOTS:
        im.disc(x + .5, y + 1.5, r + 1.3, BONE[1])            # wax run down onto the floor
        im.disc(x, y, r, BONE[4]); im.disc(x + .6, y + .6, r - .8, BONE[3]); im.put(x, y, BONE[0])
    return im.finish()


def flames(frame):
    im = Img(18, 18)
    rnd = random.Random(frame * 13 + 1)
    for (x, y, r) in CANDLE_SPOTS:
        ox = rnd.choice((0, 0, -1, 1)); oy = rnd.choice((0, -1))
        im.disc(x + ox, y - 1 + oy, 1.9, (255, 120, 40, 90), True)
        im.put(x + ox, y - 1 + oy, hx("#ffd27a")); im.put(x + ox, y - 2 + oy, hx("#ff9a3c"))
    return im


def icon():
    im = Img(24, 24)
    im.disc(12, 12, 10.5, ST[1]); im.ring(12, 12, 8.6, 9.8, RED[4])
    p = [(12 + math.cos(-math.pi / 2 + k * 2 * math.pi / 5) * 8.2, 12 + math.sin(-math.pi / 2 + k * 2 * math.pi / 5) * 8.2) for k in range(5)]
    for k in range(5): a, b = p[(k * 2) % 5], p[(k * 2 + 2) % 5]; im.line(a[0], a[1], b[0], b[1], RED[5])
    return im.finish()


# ------------------------------------------------------------------------------------------------ save + preview
parts = {}
parts["occ_gate"] = gate()
for f in range(4): parts["occ_portal_%d" % f] = portal(f)
parts["occ_floor"] = floor()
for f in range(2): parts["occ_circle_%d" % f] = circle(f)
parts["occ_altar"] = altar()
parts["occ_column"] = column()
for v in range(2): parts["occ_table_%d" % v] = table(v)
parts["occ_candles"] = candles()
for f in range(3): parts["occ_flames_%d" % f] = flames(f)
parts["occ_icon"] = icon()
for n, im in parts.items(): im.save(n)
print("saved", len(parts), "pictures to", OUT)
