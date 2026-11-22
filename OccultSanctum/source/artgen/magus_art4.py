# The archmage = the cultist and the elder, a little changed (the user, 2026-11-21: "his design must be like the
# ordinary cultist's and its elite version's, only a little changed"). His parts are the ELDER's own pictures (the
# cultist's shapes and shading, the elder's trim) redrawn crisp at 2.25 times their pixels (his pixels as fine as the
# general's at the champions' size): every new pixel takes the class and colour of the elder's pixel under it, the
# outline is drawn fresh one pixel thick round a smooth silhouette (no blurry doubling). Changed: the trim is the
# archmage's VIOLET with GOLD where the elder's trim catches the light; a small gold crown on the hood; two red eyes.
#   usage: python magus_art4.py <SanctumArt dir> <out dir>
import sys, os
from PIL import Image
src, out = sys.argv[1], sys.argv[2]; os.makedirs(out, exist_ok=True)
K = 2.25
OL = (8, 6, 10, 255)
VIO = [(86, 40, 128), (128, 70, 186), (176, 124, 232)]
GOLD = [(150, 108, 44), (214, 172, 82), (248, 222, 146)]

def lum(c): return (c[0] + c[1] + c[2]) / 3
def is_trim(c): return c[3] > 0 and ((c[0] > 110 and c[1] > 0.72 * c[0] and c[2] < 120 and c[0] - c[2] > 50) or (c[0] > 95 and c[1] < 55 and c[2] < 60))   # gold or red trim, not brown wood
def is_skin(c): return c[3] > 0 and lum(c) > 90 and abs(c[0] - c[1]) < 30 and abs(c[1] - c[2]) < 30

def redraw(name, outname, trim=True):
    im = Image.open(os.path.join(src, name + ".png")).convert("RGBA")
    w, h = im.size; W, H = round(w * K), round(h * K)
    sp = im.load()
    # a smooth silhouette: the alpha enlarged smoothly, cut at half
    a = im.getchannel("A").resize((W, H), Image.BILINEAR).load()
    o = Image.new("RGBA", (W, H), (0, 0, 0, 0)); p = o.load()
    for Y in range(H):
        for X in range(W):
            if a[X, Y] < 128: continue
            sx, sy = min(w - 1, int(X / K)), min(h - 1, int(Y / K))
            c = sp[sx, sy]
            if c[3] == 0:                                              # (just outside the old edge: the nearest inside pixel)
                best = None
                for dx in (-1, 0, 1):
                    for dy in (-1, 0, 1):
                        x2, y2 = sx + dx, sy + dy
                        if 0 <= x2 < w and 0 <= y2 < h and sp[x2, y2][3] > 0 and lum(sp[x2, y2]) > 14: best = sp[x2, y2]; break
                    if best: break
                c = best or (24, 22, 28, 255)
            if trim and is_trim(c):
                l = lum(c)
                col = GOLD[2] if l > 150 else GOLD[1] if l > 120 else VIO[2] if l > 90 else VIO[1] if l > 60 else VIO[0]
                p[X, Y] = col + (255,)
            elif lum(c) < 14:                                            # the old outline inside: the robe's darkest
                p[X, Y] = (20, 18, 24, 255)
            else:
                p[X, Y] = c[:3] + (255,)
    # a fresh 1px outline round the silhouette (inside its edge)
    q = o.load()
    edge = [(X, Y) for Y in range(H) for X in range(W) if q[X, Y][3] > 0 and any(not (0 <= X + dx < W and 0 <= Y + dy < H) or q[X + dx, Y + dy][3] == 0 for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for e in edge: q[e] = OL
    o.save(os.path.join(out, outname + ".png"))
    return o

redraw("elder_body", "mag4_body")
hd = redraw("elder_head", "mag4_head")
redraw("elder_upperarm", "mag4_upperarm")
redraw("elder_forearm", "mag4_forearm")
redraw("cult_hand", "mag4_hand", trim=False)
redraw("elder_leg", "mag4_leg")

# the head, changed a little: a small gold crown over the hood's front and two red eyes in its shadow
p = hd.load(); W, H = hd.size
ys = [y for y in range(H) for x in range(W) if p[x, y][3] > 0]; xs = [x for y in range(H) for x in range(W) if p[x, y][3] > 0]
top, cx = min(ys), (min(xs) + max(xs)) // 2
for dx in range(-3, 4):
    if p[cx + dx, top + 1][3] > 0: p[cx + dx, top + 1] = GOLD[2 if dx < 0 else 1] + (255,)
for dx in (-3, 0, 3):
    p[cx + dx, top] = GOLD[2] + (255,)
    if top > 0: p[cx + dx, top - 1] = OL
for x in range(cx - 4, cx + 5):
    if top > 0 and p[x, top][3] == 0: p[x, top] = OL
eyeY = top + round((max(ys) - top) * 0.42)
for dx in (-2, 2):
    p[cx + dx, eyeY] = (255, 84, 60, 255); p[cx + dx + (1 if dx > 0 else -1), eyeY] = (190, 40, 36, 255)
hd.save(os.path.join(out, "mag4_head.png"))

# the corpse: the elder's own, redrawn the same way
redraw("elder_dead", "mag4_dead")

# a sheet: the cultist, the elder, the archmage (each part), the general for the pixel size
row = []
for n in ("cult_body", "cult_head", "elder_body", "elder_head"):
    row.append(Image.open(os.path.join(src, n + ".png")).convert("RGBA").resize((round(24 * K) if "body" in n else round(12 * K),) * 2, Image.NEAREST))
for n in ("mag4_body", "mag4_head", "mag4_upperarm", "mag4_forearm", "mag4_hand", "mag4_leg", "mag4_dead"):
    row.append(Image.open(os.path.join(out, n + ".png")).convert("RGBA"))
for g in ("gen_body", "gen_preview"):
    f = os.path.join(src, g + ".png")
    if os.path.exists(f): row.append(Image.open(f).convert("RGBA"))
sh = Image.new("RGBA", (sum(i.width + 4 for i in row), max(i.height for i in row)), (120, 112, 112, 255)); x = 0
for i in row: sh.alpha_composite(i, (x, 0)); x += i.width + 4
sh.resize((sh.width * 5, sh.height * 5), Image.NEAREST).save(os.path.join(out, "mag4_sheet.png"))
print("archmage v4", Image.open(os.path.join(out, "mag4_body.png")).getchannel("A").getbbox())
