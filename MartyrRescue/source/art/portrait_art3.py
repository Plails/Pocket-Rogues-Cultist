# the rescued Cultist's unlock art, in the style of the game's class arts (the hunter's): a waist-up figure, dark
# outline, every part shaded in 5-6 tones lit from the upper left, dark overall, small bright highlights.
# A deep sackcloth cowl with the face lost in shadow (red eyes), a ragged sackcloth mantle on the shoulders, bare
# lashed chest and arms, leather-wrapped forearms, a studded club held across the body, the folded whip at the hip,
# rope belt, dark skirt.
import sys, math, random
import numpy as np
from PIL import Image, ImageDraw

W, H = 112, 178
random.seed(7); rng = np.random.default_rng(7)
img = np.zeros((H, W, 4), np.uint8)
owner = np.full((H, W), -1, np.int32)
LIGHT = np.array([-0.55, -0.83])
BAYER = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]) / 16.0

def ramp(*hexes): return [tuple(int(h[i:i + 2], 16) for i in (0, 2, 4)) + (255,) for h in hexes]
SKIN = ramp('3a1d12', '5e2f1c', '8a4a2c', 'b0683e', 'd28c58', 'efb487')
SACK = ramp('231a13', '3e3023', '5e4c37', '7f6a4e', 'a38d6b', 'c9b591')
MANT = ramp('211a15', '382d24', '574738', '7a6650', '9d8a6e', 'bfae8e')
WOOD = ramp('1e130c', '3b2414', '5c3a1f', '7f532d', 'a2703f', 'c49461')
LEATH = ramp('140e0b', '261a14', '3a2a20', '553e2f', '735642')
IRON = ramp('1b1d22', '33373f', '555b66', '858c98', 'c2c8d0', 'ffffff')
ROPE = ramp('3a2a10', '6a4e1e', '9a7630', 'c7a04c', 'e6c977')
CLOTH = ramp('0d1016', '161b24', '222a36', '313b4a', '45526a')
OUT = (14, 9, 9, 255)
BLOOD = (196, 18, 14, 255); BLOOD_D = (110, 8, 6, 255); BLOOD_L = (240, 70, 50, 255)

def mask_poly(pts):
    m = Image.new('L', (W, H), 0); ImageDraw.Draw(m).polygon(pts, fill=255); return np.array(m) > 0
def mask_ell(box):
    m = Image.new('L', (W, H), 0); ImageDraw.Draw(m).ellipse(box, fill=255); return np.array(m) > 0
def mask_line(pts, w):
    m = Image.new('L', (W, H), 0); ImageDraw.Draw(m).line(pts, fill=255, width=w, joint='curve'); return np.array(m) > 0

def edge_depth(m, n=4):
    d = np.zeros(m.shape, np.int32); cur = m.copy()
    for k in range(n):
        sh = cur.copy()
        sh[1:, :] &= cur[:-1, :]; sh[:-1, :] &= cur[1:, :]; sh[:, 1:] &= cur[:, :-1]; sh[:, :-1] &= cur[:, 1:]
        d += sh; cur = sh
    return d

part_id = 0
SKIN_IDS = set()
def paint(m, pal, rough=0.0, curve=1.0, bias=0.0, cx=None, cy=None, stripes=None):
    """shade a part: light from the upper left over a rounded body, darker edges, a little cloth noise"""
    global part_id
    ys, xs = np.nonzero(m)
    if len(xs) == 0: return
    cx = xs.mean() if cx is None else cx; cy = ys.mean() if cy is None else cy
    rx = max(4.0, (xs.max() - xs.min()) / 2.0); ry = max(4.0, (ys.max() - ys.min()) / 2.0)
    dep = edge_depth(m, 4)
    nx = (xs - cx) / rx; ny = (ys - cy) / ry
    lit = -(nx * LIGHT[0] + ny * LIGHT[1]) * 0.62 * curve + 0.5 + bias
    lit += (dep[ys, xs] - 2.0) * 0.07
    if rough: lit += rng.normal(0, rough, len(xs))
    if stripes is not None:
        a, per = stripes; t = (xs * math.cos(a) + ys * math.sin(a)) % per
        lit += np.where(t < 1.0, -0.28, np.where(t < 2.0, 0.08, 0.0))
    lit += (BAYER[ys % 4, xs % 4] - 0.5) * 0.05
    idx = np.clip((lit * (len(pal) - 0.01)).astype(int), 0, len(pal) - 1)
    for i, c in enumerate(pal):
        sel = idx == i
        img[ys[sel], xs[sel]] = c
    owner[ys, xs] = part_id; part_id += 1
    if pal is SKIN: SKIN_IDS.add(part_id - 1)

def dots(pts, col):
    for x, y in pts:
        if 0 <= x < W and 0 <= y < H: img[y, x] = col

def line_px(x0, y0, x1, y1):
    n = int(max(abs(x1 - x0), abs(y1 - y0))) + 1
    return [(round(x0 + (x1 - x0) * i / max(1, n - 1)), round(y0 + (y1 - y0) * i / max(1, n - 1))) for i in range(n)]

def lash(x0, y0, x1, y1):
    for i, (x, y) in enumerate(line_px(x0, y0, x1, y1)):
        if owner[y, x] not in SKIN_IDS: continue
        img[y, x] = BLOOD; img[min(H - 1, y + 1), x] = BLOOD_D
        if i % 3 == 0: img[max(0, y - 1), x] = BLOOD_L
    if owner[y1 + 2, x1] in SKIN_IDS: img[y1 + 2, x1] = BLOOD; img[y1 + 3, x1] = BLOOD_D

DK = -0.12                                   # the game's arts are dark: most of a part sits in its lower tones
# the Cultist standing full length (our own drawing): one arm raised high with the flail on its chains, the other
# hand bloody and open, a blood-soaked sack hood, bare lashed body, ochre sash with a hanging knot, a ragged dark
# skirt, rag-wrapped legs. Uses paint()/mask_*() from the header part.
BLOODY = ramp('3a0604', '6e0a06', 'a4120c', 'd42418', 'f2523a', 'ff9a7a')
SPIKE = ramp('16161a', '2e3036', '4c4f58', '7c808c', 'b4b8c2')
# legs, wrapped in rags, bare feet
for (x0, x1) in ((40, 50), (60, 70)):
    paint(mask_poly([(x0, 140), (x1, 140), (x1 - 1, 170), (x0 + 1, 170)]), LEATH, rough=0.03, curve=1.0, bias=DK + 0.1, stripes=(0.35, 4))
    paint(mask_poly([(x0 - 1, 168), (x1 + 3, 168), (x1 + 4, 175), (x0 - 2, 175)]), SKIN, rough=0.02, curve=1.0, bias=DK - 0.05)
# ragged dark skirt from the waist, torn into long tongues
skirt = mask_poly([(38, 112), (72, 112), (80, 136), (86, 158), (80, 152), (76, 162), (70, 150), (64, 160), (58, 148), (52, 162),
                   (46, 150), (40, 160), (36, 148), (28, 158), (32, 136)])
paint(skirt, CLOTH, rough=0.03, curve=0.9, bias=DK - 0.08, cx=50, cy=124)
for (x0, y0, x1, y1) in ((44, 116, 40, 150), (54, 116, 52, 154), (64, 116, 66, 152), (72, 118, 78, 148)):
    for i, (x, y) in enumerate(line_px(x0, y0, x1, y1)):
        if owner[y, x] >= 0: img[y, x] = CLOTH[0]
        if owner[y, x + 1] >= 0 and i % 2: img[y, x + 1] = CLOTH[4]
# the bare torso
paint(mask_poly([(40, 72), (70, 72), (74, 86), (70, 100), (68, 114), (42, 114), (40, 100), (36, 86)]), SKIN, rough=0.03, curve=1.25, bias=DK, cx=48, cy=88)
for y in range(84, 112):
    if owner[y, 55] >= 0 and y % 3: img[y, 55] = SKIN[1]
for (x0, x1) in ((41, 54), (57, 70)):
    for x in range(x0, x1):
        y = int(92 + 3 * math.sin((x - x0) / (x1 - x0) * math.pi))
        if owner[y, x] >= 0: img[y, x] = SKIN[0]
        if owner[y + 1, x] >= 0: img[y + 1, x] = SKIN[3]
for y in (100, 106):
    for x in list(range(48, 54)) + list(range(57, 63)):
        if owner[y, x] >= 0: img[y, x] = SKIN[1]
# the sash: an ochre band and a knot whose ends hang down the skirt
paint(mask_poly([(37, 108), (73, 106), (74, 115), (36, 117)]), ROPE, rough=0.03, curve=0.9, bias=DK, stripes=(2.6, 4))
paint(mask_ell([38, 107, 48, 117]), ROPE, curve=1.2, bias=DK)
paint(mask_poly([(39, 115), (45, 115), (40, 140), (34, 138)]), ROPE, rough=0.03, curve=0.9, bias=DK - 0.05, stripes=(1.3, 4))
paint(mask_poly([(44, 115), (49, 116), (48, 134), (43, 133)]), ROPE, rough=0.03, curve=0.9, bias=DK - 0.1, stripes=(1.3, 4))
# the lowered arm (our left): out from the shoulder, bent up, the hand open and red with blood
paint(mask_poly([(32, 74), (44, 74), (40, 92), (30, 100), (24, 94)]), SKIN, rough=0.03, curve=1.3, bias=DK)
paint(mask_poly([(24, 92), (32, 98), (24, 110), (16, 104)]), LEATH, rough=0.03, curve=1.0, bias=DK + 0.1, stripes=(0.6, 4))
hand = mask_poly([(14, 100), (24, 104), (22, 112), (14, 114), (8, 110), (4, 102), (8, 98)])
for (fx0, fy0, fx1, fy1) in ((8, 100, 3, 92), (11, 99, 8, 89), (15, 100, 14, 90), (19, 102, 21, 93), (8, 108, 2, 112)):
    hand |= mask_line([(fx0, fy0), (fx1, fy1)], 3)
paint(hand, BLOODY, rough=0.04, curve=1.2, bias=0.05, cx=13, cy=104)
for (px, py) in ((6, 86), (20, 88), (2, 98), (12, 84), (24, 96)):                 # drops flying off it
    dots([(px, py)], BLOODY[4]); dots([(px, py + 1)], BLOODY[2])
# the raised arm (our right): shoulder, upper arm up and out, forearm straight up, the fist on the flail's handle
paint(mask_poly([(66, 72), (78, 70), (84, 54), (78, 48), (70, 56)]), SKIN, rough=0.03, curve=1.3, bias=DK)
paint(mask_poly([(76, 52), (86, 52), (86, 28), (78, 28)]), LEATH, rough=0.03, curve=1.0, bias=DK + 0.1, stripes=(1.5, 4))
for sy in (34, 42):                                                                # spikes on the bracer
    paint(mask_poly([(86, sy), (92, sy + 2), (86, sy + 4)]), SPIKE, curve=1.0)
paint(mask_line([(82, 30), (83, 4)], 3), WOOD, curve=0.9, bias=DK)              # the handle
paint(mask_ell([79, 0, 87, 6]) & ~mask_ell([81, 2, 85, 4]), SPIKE, curve=1.2)    # its iron ring
paint(mask_poly([(76, 20), (88, 20), (90, 28), (86, 32), (78, 32), (75, 27)]), SKIN, rough=0.02, curve=1.3, bias=DK)
for y in (24, 28):
    for x in range(77, 89):
        if owner[y, x] >= 0: img[y, x] = SKIN[1]
# the chains from the ring and the spiked balls swinging on them
def chain(pts):
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        for i, (x, y) in enumerate(line_px(x0, y0, x1, y1)):
            if 0 <= x < W and 0 <= y < H:
                img[y, x] = SPIKE[3] if i % 3 == 0 else SPIKE[1]
                if i % 3 == 0 and y + 1 < H: img[y + 1, x] = SPIKE[0]
                owner[y, x] = 9000
chain([(82, 4), (74, 6), (66, 10), (60, 16)])
chain([(85, 4), (94, 8), (100, 16), (102, 24)])
chain([(83, 5), (80, 14), (74, 22), (70, 30)])
def ball(cx_, cy_, r):
    m = mask_ell([cx_ - r, cy_ - r, cx_ + r, cy_ + r])
    for k in range(8):
        a = k * math.pi / 4
        m |= mask_poly([(cx_ + math.cos(a - 0.35) * r * 0.8, cy_ + math.sin(a - 0.35) * r * 0.8), (cx_ + math.cos(a) * (r + 3.2), cy_ + math.sin(a) * (r + 3.2)),
                        (cx_ + math.cos(a + 0.35) * r * 0.8, cy_ + math.sin(a + 0.35) * r * 0.8)])
    paint(m, SPIKE, curve=1.4, bias=0.02, cx=cx_, cy=cy_)
    for k in range(5):                                                             # blood on it
        x = int(cx_ + random.uniform(-r, r)); y = int(cy_ + random.uniform(-r, r))
        if owner[y, x] >= 0: img[y, x] = BLOODY[2]
    dots([(cx_ - 1, cy_ - 2)], SPIKE[4])
ball(57, 20, 4); ball(103, 29, 4); ball(68, 34, 3.5)
# the sack hood, soaked with blood down the front
hood = mask_poly([(52, 36), (60, 38), (66, 46), (68, 58), (70, 70), (62, 76), (50, 76), (40, 72), (40, 58), (44, 44)])
paint(hood, SACK, rough=0.04, curve=1.35, bias=DK - 0.1, cx=54, cy=52)
for (x0, y0, x1, y1) in ((50, 48, 48, 72), (54, 46, 55, 74), (58, 50, 60, 70), (46, 56, 44, 66)):
    for i, (x, y) in enumerate(line_px(x0, y0, x1, y1)):
        if owner[y, x] >= 0: img[y, x] = BLOODY[1] if i % 4 else BLOODY[2]
        if owner[y, x + 1] >= 0 and i % 3 == 0: img[y, x + 1] = BLOODY[0]
for ex in (48, 58):                                                                # dark eye holes, a red glint
    paint(mask_ell([ex - 3, 54, ex + 3, 59]), ramp('070505', '120b0a', '1e1411'), curve=-1.0)
    dots([(ex, 56)], (235, 70, 40, 255)); dots([(ex, 57)], (130, 24, 16, 255))
dots([(52, 66), (53, 66), (54, 66), (55, 66), (56, 66)], SACK[0])                   # the mouth slit
for y in range(40, 54, 3):
    if owner[y, 53] >= 0: img[y, 53] = SACK[0]
# lash wounds on the body and the arms
for (x0, y0, x1, y1) in ((42, 78, 54, 86), (58, 80, 70, 88), (40, 92, 52, 98), (60, 94, 70, 100), (44, 102, 54, 106),
                         (34, 78, 42, 86), (70, 60, 78, 66), (28, 86, 36, 92)):
    lash(x0, y0, x1, y1)

# ---------------------------------------------------------------- outline: dark around the figure, dark seams inside
a = img[:, :, 3] > 0
edge = np.zeros_like(a)
edge[1:, :] |= a[:-1, :] & ~a[1:, :]; edge[:-1, :] |= a[1:, :] & ~a[:-1, :]
edge[:, 1:] |= a[:, :-1] & ~a[:, 1:]; edge[:, :-1] |= a[:, 1:] & ~a[:, :-1]
img[edge] = OUT
for y in range(1, H - 1):
    for x in range(1, W - 1):
        o = owner[y, x]
        if o < 0: continue
        for dx, dy in ((1, 0), (0, 1), (-1, 0), (0, -1)):
            p = owner[y + dy, x + dx]
            if p >= 0 and p > o:
                c = img[y, x].astype(int); img[y, x] = (int(c[0] * 0.3), int(c[1] * 0.3), int(c[2] * 0.3), 255)
                y2, x2 = y - dy, x - dx
                if owner[y2, x2] == o:
                    c = img[y2, x2].astype(int); img[y2, x2] = (int(c[0] * 0.7), int(c[1] * 0.7), int(c[2] * 0.7), 255)
                break

out = Image.fromarray(img, 'RGBA')
for f in sys.argv[1:]: out.save(f)
