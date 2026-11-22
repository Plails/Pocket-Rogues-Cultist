# The archmage drawn anew (the user, 2026-11-19: "redraw him, the pixels like the zombie guardian's and the skeleton
# champion's, and a design that looks better"). Drawn the way the game draws its champions (the zombie guardian):
# clear round shapes, flat 2-3 tones lit from the upper left, a dark 1px outline, light enough to read on the dark
# floor; his body ~28 px wide like the guardian's (the same pixel count, so at the same size the same pixel).
# The rank colours stay (cultist red, elder gold, archmage VIOLET with a gold crown).
# Seen from above, his FRONT is the TOP of each picture. Canvases as the rig's parts at 2x (body 48, parts 24, head 26).
#   usage: python magus_art2.py <out dir>
import sys, os, math
from PIL import Image
out = sys.argv[1]; os.makedirs(out, exist_ok=True)
OL = (16, 10, 20, 255)
ROBE = [(40, 28, 54), (62, 44, 82), (90, 66, 116)]
VIO = [(104, 58, 150), (146, 94, 198), (194, 150, 236)]
GOLD = [(138, 98, 40), (204, 160, 72), (246, 216, 136)]
BONE = [(160, 150, 128), (214, 206, 182), (246, 242, 226)]
SKIN = [(128, 118, 132), (170, 162, 174), (204, 198, 208)]
EYE = (255, 92, 64)

def new(w, h): im = Image.new("RGBA", (w, h), (0, 0, 0, 0)); return im, im.load()
def tone(r, ex, ey):
    s = ex * 0.6 + ey * 0.8                                   # lit from the upper left (-x, -y)
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

# ------------------------------------------------------------- body 48x48: a violet mantle over the shoulders, gold-
# rimmed violet pauldrons with bone horns, a high standing collar at the front, a gold clasp with a red stone, a gold
# stripe down the back
W = H = 48; cx, cy = 24.0, 26.5
im, p = new(W, H)
ell(p, W, H, cx, cy + 0.5, 12.5, 7.0, lambda x, y, ex, ey: tone(ROBE, ex, ey))                       # the mantle
ell(p, W, H, cx, cy + 0.5, 12.5, 7.0, lambda x, y, ex, ey: VIO[0] if ey < -0.55 and ex * ex + ey * ey > 0.62 else None)   # the mantle's violet front edge
for y in range(int(cy + 1), int(cy + 7.5)):                                                          # the gold stripe down the back
    put(p, W, H, cx - 0.5, y, GOLD[1] if y < cy + 5 else GOLD[0])
ell(p, W, H, cx, cy - 4.2, 5.2, 2.6, lambda x, y, ex, ey: (VIO[2] if ey < -0.2 else VIO[1]) if ex * ex + ey * ey > 0.35 else None)   # the high collar
for side in (-1, 1):                                                                                 # the pauldrons
    sx, sy = cx + side * 10.0, cy - 0.8
    ell(p, W, H, sx, sy, 4.6, 4.2, lambda x, y, ex, ey: (GOLD[2] if ey < -0.1 and ex * side < 0.3 else GOLD[1] if ey < 0.5 else GOLD[0]) if ex * ex + ey * ey > 0.55 else tone(VIO, ex, ey))
    put(p, W, H, sx, sy, VIO[2] if side < 0 else VIO[1])
    for k in range(3):                                                                               # a bone horn, out and forward
        put(p, W, H, sx + side * (3.6 + k), sy - 2.6 - k * 0.9, BONE[2] if k == 2 else BONE[1] if k == 1 else BONE[0])
for (dx, dy, c) in ((-1, 0, GOLD[2]), (0, 0, GOLD[1]), (-1, 1, GOLD[1]), (0, 1, GOLD[0])): put(p, W, H, cx - 0.5 + dx, cy - 6.6 + dy, c)   # the clasp
put(p, W, H, cx - 1, cy - 5.6, (220, 40, 50)); put(p, W, H, cx, cy - 5.6, (160, 24, 36))             # its stone
outline(im); im.save(os.path.join(out, "mag_body.png"))

# ------------------------------------------------------------- head 26x26: a deep violet hood, small like the
# guardian's head, a gold crown of three points over its front, two red eyes burning in the dark face opening
W = H = 26; cx, cy = 13.0, 13.5
im, p = new(W, H)
ell(p, W, H, cx, cy, 5.6, 6.0, lambda x, y, ex, ey: tone(ROBE, ex, ey))
ell(p, W, H, cx, cy + 5.0, 1.8, 2.0, lambda x, y, ex, ey: ROBE[0])                                   # the hood's tip falling back
ell(p, W, H, cx, cy - 2.4, 3.4, 2.2, lambda x, y, ex, ey: (10, 6, 14) if ey > -0.4 else None)        # the face opening
put(p, W, H, cx - 2, cy - 2.2, EYE); put(p, W, H, cx + 1, cy - 2.2, EYE)
for dx in range(-4, 4):                                                                              # the crown's band
    put(p, W, H, cx - 0.5 + dx + 0.5, cy - 5.6, GOLD[2] if dx < 0 else GOLD[1])
for dx in (-4, -1, 2): put(p, W, H, cx + dx + 0.5, cy - 6.6, GOLD[2])                                # its points
for (x, y) in ((cx - 4.3, cy + 1), (cx + 4.3, cy + 1)): put(p, W, H, x, y, VIO[1])                    # a violet hem
outline(im); im.save(os.path.join(out, "mag_head.png"))

# ------------------------------------------------------------- upper arm 24x24: a wide sleeve, a gold band
W = H = 24
im, p = new(W, H)
ell(p, W, H, 11.0, 10.5, 4.6, 4.2, lambda x, y, ex, ey: GOLD[1] if abs(ey + 0.15) < 0.16 else tone(ROBE, ex, ey))
outline(im); im.save(os.path.join(out, "mag_upperarm.png"))

# ------------------------------------------------------------- forearm 24x24: the sleeve from the elbow (below) to a
# wide violet bell cuff with a gold rim at the wrist (above)
im, p = new(W, H)
for y in range(7, 19):
    t = (y - 7) / 11.0; c = 10.5 + (1 - t) * 0.8; r = 2.6 + (1 - t) * 0.4
    for x in range(W):
        ex = (x + .5 - c) / r
        if abs(ex) <= 1.0: p[x, y] = tone(ROBE, ex, 0.2) + (255,)
ell(p, W, H, 11.5, 5.4, 4.0, 2.6, lambda x, y, ex, ey: (GOLD[2] if ex < 0 else GOLD[1]) if ey < -0.35 else tone(VIO, ex, ey))   # the cuff
outline(im); im.save(os.path.join(out, "mag_forearm.png"))

# ------------------------------------------------------------- hand 24x24: a pale bony hand, dark nails
im, p = new(W, H)
ell(p, W, H, 16.0, 7.2, 2.6, 3.4, lambda x, y, ex, ey: tone(SKIN, ex, ey))
for x in (15, 17): put(p, W, H, x, 3.6, SKIN[0])
outline(im); im.save(os.path.join(out, "mag_hand.png"))

# ------------------------------------------------------------- leg 24x24: the robe's hem and a black boot with a gold toe
im, p = new(W, H)
ell(p, W, H, 15.0, 12.0, 3.4, 6.2, lambda x, y, ex, ey: (GOLD[1] if ey < -0.8 else tone(ROBE, ex, ey)))
outline(im); im.save(os.path.join(out, "mag_leg.png"))

# ------------------------------------------------------------- the corpse 100x84 (ppu 110): him fallen on his back,
# drawn from the same parts, the crown rolled off
dead = Image.new("RGBA", (100, 84), (0, 0, 0, 0))
b = Image.open(os.path.join(out, "mag_body.png")); hd = Image.open(os.path.join(out, "mag_head.png"))
ua = Image.open(os.path.join(out, "mag_upperarm.png")); fa = Image.open(os.path.join(out, "mag_forearm.png"))
hn = Image.open(os.path.join(out, "mag_hand.png")); lg = Image.open(os.path.join(out, "mag_leg.png"))
def at(img, cx, cy): dead.alpha_composite(img, (int(cx - img.width / 2), int(cy - img.height / 2)))
# lying spread on his back: legs below, the arms flung out, the head fallen to one side, the crown rolled away
at(lg.rotate(180), 43, 62); at(lg.rotate(165), 56, 62)
at(fa.rotate(70), 27, 42); at(hn.rotate(70), 18, 39); at(fa.rotate(-80), 73, 46); at(hn.rotate(-80), 82, 47)
at(b, 50, 46)
at(ua.rotate(20), 39, 46); at(ua.rotate(-20), 61, 46)
at(hd.rotate(25), 47, 33)
pd = dead.load()
for (x, y) in ((64, 22), (65, 22), (66, 22), (67, 22), (64, 21), (66, 21)): pd[x, y] = GOLD[1] + (255,)    # the crown on the floor
dead.save(os.path.join(out, "mag_dead.png"))

# ------------------------------------------------------------- a sheet: the parts, him put together, the guardian
names = ["mag_body", "mag_head", "mag_upperarm", "mag_forearm", "mag_hand", "mag_leg", "mag_dead"]
ims = [Image.open(os.path.join(out, n + ".png")).convert("RGBA") for n in names]
fig = Image.new("RGBA", (48, 48), (0, 0, 0, 0))
L = Image.open(os.path.join(out, "mag_leg.png"))
fig.alpha_composite(L, (6, 14)); fig.alpha_composite(L.transpose(Image.FLIP_LEFT_RIGHT), (18, 14))
fig.alpha_composite(ims[0], (0, 0))
for side in (-1, 1):
    ua_ = ims[2] if side > 0 else ims[2].transpose(Image.FLIP_LEFT_RIGHT)
    fa_ = ims[3] if side > 0 else ims[3].transpose(Image.FLIP_LEFT_RIGHT)
    hn_ = ims[4] if side > 0 else ims[4].transpose(Image.FLIP_LEFT_RIGHT)
    fig.alpha_composite(fa_, (12 + side * 10, 8)); fig.alpha_composite(hn_, (12 + side * 9 - (6 if side < 0 else -2), 2)); fig.alpha_composite(ua_, (13 + side * 10, 14))
fig.alpha_composite(ims[1], (11, 11))
row = ims + [fig]
gd = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "refspr")
for g in ("zombieGuardian_body", "zombieGuardian_PREVIEW"):
    f = os.path.join(gd, g + ".png")
    if os.path.exists(f): row.append(Image.open(f).convert("RGBA"))
sh = Image.new("RGBA", (sum(i.width + 4 for i in row), max(i.height for i in row)), (96, 90, 90, 255)); x = 0
for i in row: sh.alpha_composite(i, (x, 0)); x += i.width + 4
sh.resize((sh.width * 6, sh.height * 6), Image.NEAREST).save(os.path.join(out, "mag2_sheet.png"))
print("archmage v2")
