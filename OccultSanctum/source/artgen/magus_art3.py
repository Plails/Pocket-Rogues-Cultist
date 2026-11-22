# The archmage's NEW DESIGN on the cultist's own body (the user, 2026-11-21: "keep his body as the cultist's - only
# redraw his design"; pixels as big as the general's). His parts stand where the cultist's stand (the goblin wizard's
# frame), drawn at 2.25 times the cultist's pixels: his body ~32 px wide like the general's, so at the champions' size
# the pixels are the general's. The look: a violet mantle with gold shoulder plates and a high gold collar, a gold clasp
# with a red stone, a hood with a gold crown and red eyes, wide violet sleeves with gold cuffs, pale hands, dark boots
# with gold toes. Flat 2-3 tones lit from the upper left, a dark 1px outline. FRONT = TOP of each picture.
#   usage: python magus_art3.py <SanctumArt dir (the cultist's parts)> <out dir>
import sys, os
from PIL import Image
src, out = sys.argv[1], sys.argv[2]; os.makedirs(out, exist_ok=True)
K = 2.25
OL = (14, 9, 18, 255)
ROBE = [(44, 26, 60), (70, 42, 96), (102, 66, 136)]
VIO = [(108, 54, 156), (150, 92, 206), (198, 152, 240)]
GOLD = [(140, 98, 38), (206, 162, 70), (248, 220, 140)]
SKIN = [(132, 120, 136), (176, 166, 180), (212, 206, 216)]
DARK = [(26, 18, 32), (40, 30, 48)]
EYE = (255, 84, 60)

def box(name):
    """where the cultist's part lies on its canvas, scaled to ours: canvas size, centre, half sizes"""
    im = Image.open(os.path.join(src, name + ".png")).convert("RGBA")
    x0, y0, x1, y1 = im.getchannel("A").getbbox()
    W, H = round(im.width * K), round(im.height * K)
    return W, H, (x0 + x1) / 2 * K, (y0 + y1) / 2 * K, (x1 - x0) / 2 * K, (y1 - y0) / 2 * K
def new(w, h): im = Image.new("RGBA", (w, h), (0, 0, 0, 0)); return im, im.load()
def tone(r, ex, ey):
    s = ex * 0.6 + ey * 0.8
    return r[2] if s < -0.45 else r[0] if s > 0.4 else r[1]
def ell(p, w, h, cx, cy, rx, ry, fn):
    for y in range(h):
        for x in range(w):
            ex, ey = (x + .5 - cx) / rx, (y + .5 - cy) / ry
            if ex * ex + ey * ey <= 1.0:
                c = fn(x, y, ex, ey)
                if c is not None: p[x, y] = c + (255,)
def put(p, w, h, x, y, c):
    x, y = int(round(x)), int(round(y))
    if 0 <= x < w and 0 <= y < h: p[x, y] = c + (255,)
def outline(im):
    w, h = im.size; p = im.load()
    ring = [(x, y) for y in range(h) for x in range(w) if p[x, y][3] == 0 and any(0 <= x + a < w and 0 <= y + b < h and p[x + a, y + b][3] == 255 and p[x + a, y + b] != OL for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for q in ring: p[q] = OL

# body: where the cultist's body is
W, H, cx, cy, rx, ry = box("cult_body")
im, p = new(W, H)
ell(p, W, H, cx, cy + 0.5, rx, ry, lambda x, y, ex, ey: tone(ROBE, ex, ey))                                  # the mantle
for side in (-1, 1):
    sx = cx + side * rx * 0.68
    ell(p, W, H, sx, cy - 0.5, rx * 0.3, ry * 0.62, lambda x, y, ex, ey: (GOLD[2] if ey < -0.2 and ex * side < 0.2 else GOLD[1] if ey < 0.5 else GOLD[0]) if ex * ex + ey * ey > 0.5 else tone(VIO, ex, ey))
    for k in range(3): put(p, W, H, sx + side * (rx * 0.3 - 1 + k), cy - ry * 0.55 - k, GOLD[2] if k == 2 else GOLD[1])
ell(p, W, H, cx, cy - ry * 0.45, rx * 0.36, ry * 0.5, lambda x, y, ex, ey: (GOLD[2] if ex < 0 else GOLD[1]) if ex * ex + ey * ey > 0.4 and ey < 0.35 else None)   # the high collar
for (dx, c) in ((-1, (230, 44, 52)), (0, (160, 22, 36))): put(p, W, H, cx - 0.5 + dx, cy + 0.2, c)
put(p, W, H, cx - 2.5, cy + 0.2, GOLD[1]); put(p, W, H, cx + 1.5, cy + 0.2, GOLD[1])
for y in range(int(cy + 2), int(cy + ry)): put(p, W, H, cx - 0.5, y, VIO[0])
outline(im); im.save(os.path.join(out, "mag3_body.png"))

# head: a hood smaller than the cultist's big head (the champions' heads are small), crowned, red eyes
W, H, cx, cy, rx, ry = box("cult_head")
im, p = new(W, H); hr = min(rx, ry) * 0.62
ell(p, W, H, cx, cy + 1, hr, hr * 1.05, lambda x, y, ex, ey: tone(ROBE, ex, ey))
ell(p, W, H, cx, cy + 1 - hr * 0.33, hr * 0.6, hr * 0.42, lambda x, y, ex, ey: DARK[0] if ey > -0.45 else None)
put(p, W, H, cx - hr * 0.33, cy + 1 - hr * 0.33, EYE); put(p, W, H, cx + hr * 0.2, cy + 1 - hr * 0.33, EYE)
for dx in range(-3, 3): put(p, W, H, cx + dx, cy + 1 - hr * 0.9, GOLD[2] if dx < 0 else GOLD[1])
for dx in (-3, -1, 1): put(p, W, H, cx + dx + 0.3, cy - hr * 1.1, GOLD[2])
outline(im); im.save(os.path.join(out, "mag3_head.png"))

# upper arm: a wide sleeve
W, H, cx, cy, rx, ry = box("cult_upperarm")
im, p = new(W, H); ell(p, W, H, cx, cy, rx, ry, lambda x, y, ex, ey: GOLD[1] if abs(ey + 0.1) < 0.14 else tone(ROBE, ex, ey)); outline(im); im.save(os.path.join(out, "mag3_upperarm.png"))
# forearm: the sleeve to a gold cuff at the wrist (above)
W, H, cx, cy, rx, ry = box("cult_forearm")
im, p = new(W, H); ell(p, W, H, cx, cy, rx * 0.95, ry, lambda x, y, ex, ey: (GOLD[2] if ex < 0 else GOLD[1]) if ey < -0.62 else (VIO[1] if ey < -0.45 else tone(ROBE, ex, ey))); outline(im); im.save(os.path.join(out, "mag3_forearm.png"))
# hand: pale
W, H, cx, cy, rx, ry = box("cult_hand")
im, p = new(W, H); ell(p, W, H, cx, cy, rx * 0.9, ry * 0.9, lambda x, y, ex, ey: tone(SKIN, ex, ey)); outline(im); im.save(os.path.join(out, "mag3_hand.png"))
# leg: a dark boot under the robe's hem, a gold toe (front = top)
W, H, cx, cy, rx, ry = box("cult_leg")
im, p = new(W, H); ell(p, W, H, cx, cy, rx * 0.85, ry, lambda x, y, ex, ey: GOLD[1] if ey < -0.78 else tone([DARK[0], DARK[1], ROBE[1]], ex, ey)); outline(im); im.save(os.path.join(out, "mag3_leg.png"))

# the corpse (on his back), from the same parts
dead = Image.new("RGBA", (round(50 * K / 2 * 2), round(42 * K / 2 * 2)), (0, 0, 0, 0))
def at(n, x, y, rot=0):
    img = Image.open(os.path.join(out, n + ".png")).rotate(rot, expand=True)
    dead.alpha_composite(img, (int(x - img.width / 2), int(y - img.height / 2)))
Wd, Hd = dead.size
at("mag3_leg", Wd / 2 - 6, Hd / 2 + 16, 180); at("mag3_leg", Wd / 2 + 6, Hd / 2 + 16, 165)
at("mag3_forearm", Wd / 2 - 26, Hd / 2 + 2, 60); at("mag3_forearm", Wd / 2 + 26, Hd / 2 + 4, -70)
at("mag3_hand", Wd / 2 - 34, Hd / 2 + 4, 60); at("mag3_hand", Wd / 2 + 34, Hd / 2 + 8, -70)
at("mag3_body", Wd / 2, Hd / 2 + 2)
at("mag3_upperarm", Wd / 2 - 15, Hd / 2 + 2); at("mag3_upperarm", Wd / 2 + 15, Hd / 2 + 2)
at("mag3_head", Wd / 2 - 2, Hd / 2 - 9, 15)
pd = dead.load()
for (x, y) in ((Wd // 2 + 16, Hd // 2 - 16), (Wd // 2 + 17, Hd // 2 - 16), (Wd // 2 + 18, Hd // 2 - 16), (Wd // 2 + 16, Hd // 2 - 17), (Wd // 2 + 18, Hd // 2 - 17)): pd[x, y] = GOLD[1] + (255,)
dead.save(os.path.join(out, "mag3_dead.png"))

# a sheet
names = ["mag3_body", "mag3_head", "mag3_upperarm", "mag3_forearm", "mag3_hand", "mag3_leg", "mag3_dead"]
row = [Image.open(os.path.join(out, n + ".png")).convert("RGBA") for n in names]
for g in ("gen_preview", "gen_body", "mag_body", "mag_head"):
    f = os.path.join(src, g + ".png")
    if os.path.exists(f): row.append(Image.open(f).convert("RGBA"))
sh = Image.new("RGBA", (sum(i.width + 4 for i in row), max(i.height for i in row)), (96, 90, 90, 255)); x = 0
for i in row: sh.alpha_composite(i, (x, 0)); x += i.width + 4
sh.resize((sh.width * 5, sh.height * 5), Image.NEAREST).save(os.path.join(out, "mag3_sheet.png"))
print("archmage v3", Image.open(os.path.join(out, "mag3_body.png")).getchannel("A").getbbox())
