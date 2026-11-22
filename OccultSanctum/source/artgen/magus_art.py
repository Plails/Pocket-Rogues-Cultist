# The cult's second champion, the magus (Верховный маг культа): an elite cultist, the cultist's own goblin-wizard
# rig and moves, every picture new and drawn at TWICE the cultist's parts (the mod gives them twice the pixels per
# unit, Art.Part2x) - so at the champion's size his pixels are as big as the soldiers' and the game's.
# The cultists' style: black robe, red trim - and the elite's finery: skull pauldrons with small horns, a high red
# collar behind the head, a gold chain across the chest with a blood-red gem, a crowned hood with three bone horns and
# glowing red eyes under it, wide sleeves with red cuffs and gold bands, pale hands with a red glow about them.
# Seen from above like everything in the game: his FRONT is the TOP of each picture.
#   usage: python magus_art.py <out dir>
import sys, os, math
from PIL import Image
out = sys.argv[1]; os.makedirs(out, exist_ok=True)
OL = (10, 6, 10, 255)
ROBE = [(12, 10, 14), (22, 18, 26), (34, 28, 40), (48, 40, 56)]
RED = [(96, 14, 22), (146, 26, 32), (196, 46, 44), (236, 84, 70)]
GOLD = [(110, 80, 34), (160, 122, 52), (208, 170, 84), (240, 214, 140)]
BONE = [(140, 130, 112), (190, 182, 160), (226, 220, 202)]
SKIN = [(150, 138, 140), (190, 180, 182), (220, 214, 216)]
GLOW = (255, 90, 70)
def h2(x, y, s): return (math.sin(x * 12.9898 + y * 78.233 + s * 37.719) * 43758.5453) % 1.0
def new(w, h): im = Image.new("RGBA", (w, h), (0, 0, 0, 0)); return im, im.load()
def tone(r, ex, ey, bias=0.0):
    s = ex * 0.6 + ey * 0.8 + bias                     # light from the upper left
    i = 3 if s < -0.55 else 2 if s < -0.1 else 1 if s < 0.45 else 0
    return r[min(i, len(r) - 1)]
def ell(p, w, h, cx, cy, rx, ry, fn):
    for y in range(h):
        for x in range(w):
            ex, ey = (x - cx) / rx, (y - cy) / ry
            if ex * ex + ey * ey <= 1: c = fn(x, y, ex, ey); p[x, y] = c + (255,) if len(c) == 3 else c
def put(p, w, h, x, y, c):
    x, y = int(round(x)), int(round(y))
    if 0 <= x < w and 0 <= y < h: p[x, y] = c + (255,) if len(c) == 3 else c
def outline(im):
    w, h = im.size; p = im.load()
    ring = [(x, y) for y in range(h) for x in range(w) if p[x, y][3] == 0 and any(0 <= x + a < w and 0 <= y + b < h and p[x + a, y + b][3] == 255 for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for q in ring: p[q] = OL
def glow(im, col, a=90):
    w, h = im.size; p = im.load()
    ring = [(x, y) for y in range(h) for x in range(w) if p[x, y][3] == 0 and any(0 <= x + dx < w and 0 <= y + dy < h and p[x + dx, y + dy] == OL for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for q in ring: p[q] = col + (a,)

# ---------------------------------------------------------------- body 36x36 (the cultist's 24x24, one and a half
# times: at the champion's size his pixels come out as big as the soldiers' and the general's)
W = H = 36
body, p = new(W, H); c = W / 2 - .5
ell(p, W, H, c, 22.0, 10.2, 6.4, lambda x, y, ex, ey: tone(ROBE, ex, ey, 0.3))                       # the cape falling behind (the back)
ell(p, W, H, c, 18.8, 11.4, 6.1, lambda x, y, ex, ey: RED[1] if ex * ex + ey * ey > 0.84 else tone(ROBE, ex, ey))   # the mantle, red-hemmed
for y in range(21, 27):                                                                              # the cape's folds
    for x in (round(c - 4.5), round(c + 4.5)):
        if p[x, y][3]: p[x, y] = ROBE[0] + (255,)
ell(p, W, H, c, 21.0, 5.6, 2.8, lambda x, y, ex, ey: RED[2] if ey < -0.2 else RED[1] if ex < 0 else RED[0])   # the high collar behind the head
for x in range(round(c - 7), round(c + 8)):                                                          # the gold chain across the chest
    put(p, W, H, x, 14 + round(0.03 * (x - c) ** 2), GOLD[2] if x % 2 else GOLD[1])
put(p, W, H, c, 15, GLOW); put(p, W, H, c + 1, 15, RED[2]); put(p, W, H, c, 16, RED[1])                 # the blood gem
for side in (-1, 1):                                                                                 # the skull pauldrons
    sx, sy = c + side * 9.5, 18.0
    ell(p, W, H, sx, sy, 3.9, 3.4, lambda x, y, ex, ey: tone(BONE, ex, ey))
    put(p, W, H, sx - 1, sy - 0.4, ROBE[0]); put(p, W, H, sx + 1, sy - 0.4, ROBE[0])                 # eye holes
    put(p, W, H, sx, sy + 1.2, ROBE[1])
    for k in range(3):                                                                               # a small horn curling out
        put(p, W, H, sx + side * (3 + k * 0.7), sy - 2.5 - k * 0.8, BONE[1] if k < 2 else BONE[2])
outline(body); body.save(os.path.join(out, "mag_body.png"))

# ---------------------------------------------------------------- head 18x18 (12x12): a crowned hood, eyes aglow
W = H = 18
head, p = new(W, H); c = W / 2 - .5
ell(p, W, H, c, 9.2, 6.3, 6.5, lambda x, y, ex, ey: (RED[2] if ey < 0 else RED[1]) if ex * ex + ey * ey > 0.78 and ey < 0.35 else tone(ROBE, ex, ey))
ell(p, W, H, c, 6.6, 3.4, 2.3, lambda x, y, ex, ey: ROBE[0])                                          # the dark of the hood's opening
put(p, W, H, c - 1, 6.2, GLOW); put(p, W, H, c + 1, 6.2, GLOW)                                        # the eyes
for x in range(round(c - 5), round(c + 6)):                                                          # the gold circlet round the brow
    put(p, W, H, x, 3 + round(0.12 * (x - c) ** 2), GOLD[2] if x < c else GOLD[1])
for dx, n in ((-4, 3), (0, 4), (4, 3)):                                                              # three bone horns rising from it
    y0 = 3 + round(0.12 * dx * dx)
    for k in range(n): put(p, W, H, c + dx + (dx / 4.0) * k * 0.5, y0 - 1 - k, BONE[2] if k >= n - 2 else BONE[1])
outline(head); head.save(os.path.join(out, "mag_head.png"))

# ---------------------------------------------------------------- arms 18x18 each (the cultist's 12x12)
def limb_img(name, draw):
    im, q = new(18, 18); draw(q); outline(im); im.save(os.path.join(out, name + ".png")); return im
limb_img("mag_upperarm", lambda q: (ell(q, 18, 18, 8.3, 8.3, 4.0, 4.2, lambda x, y, ex, ey: tone(ROBE, ex, ey)),
                                     [put(q, 18, 18, x, 7, GOLD[2] if x % 2 else GOLD[1]) for x in range(5, 12)]))
def fore(q):
    for y in range(1, 17):                                                                           # a wide sleeve flaring toward the wrist (the top)
        t = (16 - y) / 15.0; half = 2.4 + 2.0 * t
        for x in range(18):
            e = (x - 7.5) / half
            if abs(e) <= 1: q[x, y] = (RED[2] if y <= 2 else RED[1] if y == 3 else GOLD[2] if y == 6 else tone(ROBE, e, 0.0)) + (255,)
limb_img("mag_forearm", fore)
def hand(q):
    ell(q, 18, 18, 12.0, 5.3, 2.7, 3.1, lambda x, y, ex, ey: tone(SKIN, ex, ey))
    for k in range(3): put(q, 18, 18, 10.5 + k * 1.3, 2, SKIN[1])                                     # the fingers
    put(q, 18, 18, 11, 7, RED[2])                                                                    # a ring
limb_img("mag_hand", hand)
hd = Image.open(os.path.join(out, "mag_hand.png")); glow(hd, (230, 40, 40), 110); hd.save(os.path.join(out, "mag_hand.png"))   # red light about the hand
limb_img("mag_leg", lambda q: ell(q, 18, 18, 11.3, 9.0, 3.5, 5.5, lambda x, y, ex, ey: RED[1] if ey > 0.7 else tone(ROBE, ex, ey)))

# ---------------------------------------------------------------- the corpse, in the game's corpse style (flat tones,
# each part with its own outline, a soft dark rim): on his back, arms out, the crowned hood fallen back
W, H = 44, 46
final = Image.new("RGBA", (W, H), (0, 0, 0, 0)); im, p = new(W, H)
def layer():
    global im, p
    outline(im); final.alpha_composite(im); im, p = new(W, H)
def limb(x0, y0, x1, y1, w, r):
    dx, dy = x1 - x0, y1 - y0; L = math.hypot(dx, dy)
    for y in range(H):
        for x in range(W):
            t = max(0.0, min(1.0, ((x - x0) * dx + (y - y0) * dy) / (L * L)))
            d = math.hypot(x - (x0 + dx * t), y - (y0 + dy * t))
            if d <= w:
                s = ((x - x0 - dx * t) * -dy + (y - y0 - dy * t) * dx) / (L * w)
                p[x, y] = (r[2] if s < -0.45 else r[0] if s > 0.45 else r[1]) + (255,)
cx = 22
limb(cx - 3, 32, cx - 6, 41, 2.6, ROBE); layer(); limb(cx + 3, 32, cx + 6, 41, 2.6, ROBE); layer()   # the robe's skirt, legs apart
limb(cx - 8, 15, cx - 15, 24, 2.6, ROBE); layer(); limb(cx + 8, 15, cx + 15, 22, 2.6, ROBE); layer()   # sleeves
ell(p, W, H, cx - 16.5, 26, 2.2, 2.2, lambda x, y, ex, ey: tone(SKIN, ex, ey)); layer()
ell(p, W, H, cx + 16.5, 24, 2.2, 2.2, lambda x, y, ex, ey: tone(SKIN, ex, ey)); layer()
for y in range(12, 36):                                                                              # the robe, red hem
    half = 7 if y < 26 else 7 + (y - 26) * 0.35
    for x in range(int(cx - half), int(cx + half) + 1):
        e = (x - cx) / half
        p[x, y] = (RED[1] if abs(e) > 0.82 or y >= 34 else ROBE[3] if e < -0.5 else ROBE[1] if e < 0.5 else ROBE[0]) + (255,)
for x in range(cx - 6, cx + 7): put(p, W, H, x, 16 + abs(x - cx) // 3, GOLD[2] if x % 2 else GOLD[1])   # the chain
put(p, W, H, cx, 18, RED[3]); put(p, W, H, cx, 19, RED[1])
layer()
for side in (-1, 1): ell(p, W, H, cx + side * 8.5, 13, 3.6, 3.0, lambda x, y, ex, ey: tone(BONE, ex, ey)); layer()
ell(p, W, H, cx, 7, 4.6, 4.8, lambda x, y, ex, ey: RED[1] if ex * ex + ey * ey > 0.75 else ROBE[1]); layer()   # the hood
ell(p, W, H, cx, 7.5, 2.6, 2.8, lambda x, y, ex, ey: tone(SKIN, ex, ey)); put(p, W, H, cx - 1, 7, (60, 40, 44)); put(p, W, H, cx + 1, 7, (60, 40, 44)); layer()   # the face, eyes shut
for x in range(cx + 6, cx + 13): put(p, W, H, x, 3 + (x - cx - 6) // 3, GOLD[2] if x % 2 else GOLD[1])   # the circlet, fallen off
for k in range(3): put(p, W, H, cx + 8 + k * 2, 2 + (k % 2), BONE[2])
layer()
q = final.load()
rim = [(x, y) for y in range(H) for x in range(W) if q[x, y][3] == 0 and any(0 <= x + a < W and 0 <= y + b < H and q[x + a, y + b] == OL for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
for r in rim: q[r] = (0, 0, 0, 70)
final.save(os.path.join(out, "mag_dead.png"))

# preview sheet
names = ["mag_body", "mag_head", "mag_upperarm", "mag_forearm", "mag_hand", "mag_leg", "mag_dead"]
ims = [Image.open(os.path.join(out, n + ".png")).convert("RGBA") for n in names]
sh = Image.new("RGBA", (sum(i.width + 6 for i in ims), max(i.height for i in ims)), (90, 80, 80, 255)); x = 0
for i in ims: sh.alpha_composite(i, (x, 0)); x += i.width + 6
sh.resize((sh.width * 6, sh.height * 6), Image.NEAREST).save(os.path.join(out, "mag_sheet.png"))
print("magus")
