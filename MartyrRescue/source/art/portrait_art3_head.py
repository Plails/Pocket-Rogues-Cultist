# the rescued Cultist's unlock art, in the style of the game's class arts (the hunter's): a waist-up figure, dark
# outline, every part shaded in 5-6 tones lit from the upper left, dark overall, small bright highlights.
# A deep sackcloth cowl with the face lost in shadow (red eyes), a ragged sackcloth mantle on the shoulders, bare
# lashed chest and arms, leather-wrapped forearms, a studded club held across the body, the folded whip at the hip,
# rope belt, dark skirt.
import sys, math, random
import numpy as np
from PIL import Image, ImageDraw

W, H = 112, 150
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
