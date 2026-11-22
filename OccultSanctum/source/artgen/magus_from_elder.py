# The archmage (the cult's highest rank: cultist -> elder -> archmage), the user: "the same design and style as the
# cultist and the elder, one to one, only a little changed". So his parts ARE the elder's (the cultist's shapes with
# the elder's gold trim), only twice the size by Scale2x (the shapes untouched, the pixels as fine as
# the soldiers' at his size) and a little more of rank:
#   - the hood's rim gold AND red (the elder's gold, the cultist's red), a gold crown with three points on the front,
#     red eyes burning in the hood
#   - the shoulders' gold trim with red studs, a red gem at the chest
#   - the cuffs gold and red; a red glow round the hands
#   usage: python magus_from_elder.py <SanctumArt dir> <out dir>
import sys, os
from PIL import Image
src, out = sys.argv[1], sys.argv[2]; os.makedirs(out, exist_ok=True)
RED = [(120, 20, 26), (176, 34, 36), (226, 70, 56)]
GOLD = [(150, 112, 44), (214, 176, 90), (246, 224, 150)]
PURPLE = [(64, 22, 92), (112, 44, 156), (168, 92, 212)]
def _unused_to_purple(im):
    """the archmage's own colour (the user: he looked just like the elder): the elder's gold trim (and any red) turned
    the champions' violet, light for light"""
    p = im.load(); w, h = im.size
    for y in range(h):
        for x in range(w):
            c = p[x, y]
            if c[3] > 0 and (is_gold(c) or is_red(c)):
                l = (c[0] + c[1] + c[2]) / 3
                p[x, y] = (PURPLE[2] if l > 150 else PURPLE[1] if l > 90 else PURPLE[0]) + (c[3],)
GLOW = (255, 96, 70)
OL = (6, 4, 6, 255)

def up15(im):
    """twice the size by Scale2x (the pixel-art doubling that keeps diagonals smooth), then the doubled outline
    thinned back to one pixel - the cultist's very shapes, with the pixels as fine as the soldiers' at his size
    (a plain doubling would have made each of his pixels twice as big as everyone's)"""
    w, h = im.size; W, H = w * 2, h * 2
    o = Image.new("RGBA", (W, H)); p, q = im.load(), o.load()
    def g(x, y): return p[min(w - 1, max(0, x)), min(h - 1, max(0, y))]
    for y in range(h):
        for x in range(w):
            E = g(x, y); B, D, F, Hh = g(x, y - 1), g(x - 1, y), g(x + 1, y), g(x, y + 1)
            e0 = D if D == B and B != F and D != Hh else E
            e1 = F if B == F and B != D and F != Hh else E
            e2 = D if D == Hh and D != B and Hh != F else E
            e3 = F if Hh == F and D != Hh and B != F else E
            q[2 * x, 2 * y], q[2 * x + 1, 2 * y], q[2 * x, 2 * y + 1], q[2 * x + 1, 2 * y + 1] = e0, e1, e2, e3
    def dark(c): return c[3] > 200 and c[0] + c[1] + c[2] < 45
    outer = [(x, y) for y in range(H) for x in range(W) if dark(q[x, y]) and any(not (0 <= x + a < W and 0 <= y + b < H) or q[x + a, y + b][3] == 0 for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)))
             and all(not (0 <= x + a < W and 0 <= y + b < H) or q[x + a, y + b][3] == 0 or dark(q[x + a, y + b]) for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for r in outer: q[r] = (0, 0, 0, 0)
    return o

def is_gold(c): return c[3] > 0 and c[0] > 120 and c[1] > 90 and c[2] < 120 and c[0] - c[2] > 60
def is_red(c): return c[3] > 0 and c[0] > 100 and c[1] < 70 and c[2] < 80

def warm(c): return c[3] > 0 and c[0] >= c[1] > c[2] and c[0] - c[2] > 30 and c[0] > 70     # any gold or brown of the trim
def to_purple(im):
    p = im.load(); w, h = im.size
    for y in range(h):
        for x in range(w):
            c = p[x, y]
            if c[3] > 0 and (warm(c) or is_red(c)):
                l = (c[0] + c[1] + c[2]) / 3
                p[x, y] = (PURPLE[2] if l > 150 else PURPLE[1] if l > 90 else PURPLE[0]) + (c[3],)

def load(n): return up15(Image.open(os.path.join(src, n + ".png")).convert("RGBA"))
def put(p, w, h, x, y, c):
    if 0 <= x < w and 0 <= y < h: p[x, y] = c + (255,) if len(c) == 3 else c

# (the user: the first try looked odd - speckled red-and-gold rims, tall crown points, a big head. Now cleaner: the
# elder's gold trim kept whole, red only as thin inner lines and a few clear stones; a low crown; a smaller head)
def inner_line(im, col):
    """a thin red line just inside the gold trim (gold pixels touching the dark inside)"""
    p = im.load(); w, h = im.size
    pts = [(x, y) for y in range(h) for x in range(w) if is_gold(p[x, y]) and any(0 <= x + a < w and 0 <= y + b < h and p[x + a, y + b][3] > 200 and not is_gold(p[x + a, y + b]) and sum(p[x + a, y + b][:3]) > 30 for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for q in pts[::2]: p[q] = col + (255,)

def shrink(im, n):
    """n rows and n columns fewer, taking out rows/columns that repeat their neighbour (the doubling made many) - the
    pixels stay their size, the picture gets smaller"""
    import itertools
    for _ in range(n):
        w, h = im.size; p = im.load()
        rows = [y for y in range(1, h - 1) if all(p[x, y] == p[x, y - 1] for x in range(w))]
        y0 = rows[len(rows) // 2] if rows else h // 2
        im2 = Image.new("RGBA", (w, h - 1)); im2.paste(im.crop((0, 0, w, y0)), (0, 0)); im2.paste(im.crop((0, y0 + 1, w, h)), (0, y0)); im = im2
        w, h = im.size; p = im.load()
        cols = [x for x in range(1, w - 1) if all(p[x, y] == p[x - 1, y] for y in range(h))]
        x0 = cols[len(cols) // 2] if cols else w // 2
        im2 = Image.new("RGBA", (w - 1, h)); im2.paste(im.crop((0, 0, x0, h)), (0, 0)); im2.paste(im.crop((x0 + 1, 0, w, h)), (x0, 0)); im = im2
    out_ = Image.new("RGBA", (im.size[0] + 2 * n - (2 * n) % 2, im.size[1] + 2 * n - (2 * n) % 2))   # back on a canvas of the old size, centred
    out_.alpha_composite(im, (n, n))
    return out_

# body: the elder's, its gold trim whole with a thin red line inside, one clear blood stone at the chest
b = load("elder_body"); p = b.load(); w, h = b.size
golds = [(x, y) for y in range(h) for x in range(w) if is_gold(p[x, y])]
to_purple(b)
cx = w // 2; gy = (min(y for (x, y) in golds) + 1) if golds else h // 2
for (dx, dy, c) in ((-2, 0, GOLD[1]), (-1, 0, GOLD[2]), (0, 0, GOLD[2]), (1, 0, GOLD[1]), (-1, 1, PURPLE[2]), (0, 1, PURPLE[1]), (-2, 1, GOLD[0]), (1, 1, GOLD[0])): put(p, w, h, cx + dx, gy + dy, c)   # a gold clasp with a violet stone
b.save(os.path.join(out, "mag_body.png"))

# head: the elder's hood a little smaller (same pixels), gold rim with a red inner line, a low gold crown - a band with
# small points - and two red eyes in the hood's shadow
hd = shrink(load("elder_head"), 2); to_purple(hd); p = hd.load(); w, h = hd.size
top = min(y for y in range(h) for x in range(w) if p[x, y][3] > 0)
cx = w // 2
for dx in range(-3, 3): put(p, w, h, cx + dx, top + 1, GOLD[2] if dx < 0 else GOLD[1])           # the crown's band
for dx in (-3, -1, 1): put(p, w, h, cx + dx, top, GOLD[2])                                        # its small points

for dx in (-2, 1): put(p, w, h, cx + dx, top + 4, GLOW)                                           # the eyes
q = hd.load()
ring = [(x, y) for y in range(h) for x in range(w) if q[x, y][3] == 0 and any(0 <= x + a < w and 0 <= y + c < h and q[x + a, y + c][3] == 255 and q[x + a, y + c][:3] in (GOLD[1], GOLD[2]) for a, c in ((1, 0), (-1, 0), (0, -1)))]
for r in ring: q[r] = OL
hd.save(os.path.join(out, "mag_head.png"))

# arms: the elder's, the gold cuffs with a thin red line
for n in ("upperarm", "forearm"):
    im = load("elder_" + n); to_purple(im); im.save(os.path.join(out, "mag_" + n + ".png"))

# hands: the cultist's pale hands with a red light about them; the legs: the elder's
hn = load("cult_hand"); p = hn.load(); w, h = hn.size
rim = [(x, y) for y in range(h) for x in range(w) if p[x, y][3] == 0 and any(0 <= x + a < w and 0 <= y + c < h and p[x + a, y + c][3] > 200 for a, c in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
pass                                                                                      # (no red glow: plain hands, the user)
hn.save(os.path.join(out, "mag_hand.png"))
load("elder_leg").save(os.path.join(out, "mag_leg.png"))

# the corpse: the elder's, doubled the same way, its gold trim half red
dd = load("elder_dead"); p = dd.load(); w, h = dd.size
to_purple(dd)
dd.save(os.path.join(out, "mag_dead.png"))

names = ["mag_body", "mag_head", "mag_upperarm", "mag_forearm", "mag_hand", "mag_leg"]
ims = [Image.open(os.path.join(out, n + ".png")).convert("RGBA") for n in names]
el = [up15(Image.open(os.path.join(src, n + ".png")).convert("RGBA")) for n in ("cult_body", "cult_head", "elder_body", "elder_head")]
row = el + ims
sh = Image.new("RGBA", (sum(i.width + 4 for i in row), max(i.height for i in row)), (110, 100, 100, 255)); x = 0
for i in row: sh.alpha_composite(i, (x, 0)); x += i.width + 4
sh.resize((sh.width * 8, sh.height * 8), Image.NEAREST).save(os.path.join(out, "mag_compare.png"))
print("archmage from the elder")
