# The cultists' beds and a cultist asleep in one (the user: the old beddings looked awful; let them sleep, breathing
# smoothly like the game's own animations). Seen from above, the head end at the TOP of the picture, 22x44 px at the
# mod's 24 px a tile (one tile across, two long):
#   occ_bedroll       - an empty bed: a dark wooden frame with a headboard and corner posts, a linen mattress, a pillow,
#                       a dark red wool blanket with a turned-down edge and a stripe
#   cult_sleep        - the sleeper's head on the pillow: face up in his black hood with the red rim, eyes shut
#   cult_sleep_blanket- the blanket drawn up over him to the chest, the shape of his body under it, a pale hand on it
#                       (a picture of its own: the mod makes it rise and fall as he breathes)
#   occ_z             - a small "z" that drifts up from a sleeper
#   usage: python bed_sleep_art.py <out dir>
import sys, os, math
from PIL import Image
out = sys.argv[1]; os.makedirs(out, exist_ok=True)
OL = (10, 8, 10, 255)
WOOD = [(40, 26, 18), (58, 38, 26), (78, 52, 34), (98, 66, 42)]
LINEN = [(120, 112, 100), (150, 142, 128), (178, 170, 154), (204, 198, 184)]
WOOL = [(52, 12, 16), (74, 18, 22), (98, 26, 28), (124, 38, 36)]
STRIPE = (150, 120, 70)
ROBE = [(14, 12, 16), (24, 20, 26), (36, 30, 38)]
RED = [(110, 18, 22), (160, 30, 30)]
SKIN = [(150, 136, 126), (188, 176, 164), (214, 204, 192)]
def h2(x, y, s): return (math.sin(x * 12.9898 + y * 78.233 + s * 37.719) * 43758.5453) % 1.0
def g(c, x, y, s=1, k=3): d = (h2(x, y, s) - .5) * 2 * k; return tuple(max(0, min(255, int(v + d))) for v in c[:3]) + (255,)
W, H = 22, 44
def canvas(): im = Image.new("RGBA", (W, H), (0, 0, 0, 0)); return im, im.load()
def outline(im):
    w, h = im.size; p = im.load()
    ring = [(x, y) for y in range(h) for x in range(w) if p[x, y][3] == 0 and any(0 <= x + a < w and 0 <= y + b < h and p[x + a, y + b][3] > 0 and p[x + a, y + b] != OL for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for q in ring: p[q] = OL

# ---------------------------------------------------------------- the empty bed
bed, b = canvas()
for y in range(1, H - 1):
    for x in range(1, W - 1):
        frame = x <= 2 or x >= W - 3 or y <= 4 or y >= H - 3
        if frame:
            if y <= 4: c = WOOD[3] if y == 1 else WOOD[2] if y < 4 else WOOD[0]          # the headboard
            else: c = WOOD[2] if x <= 1 or y == H - 3 else WOOD[1] if x <= 2 or x >= W - 3 else WOOD[1]
            if x == W - 2 or y == H - 2: c = WOOD[0]
            b[x, y] = g(c, x, y, 1, 3)
        else:
            b[x, y] = g(LINEN[2] if x < 6 else LINEN[1], x, y, 2, 3)                      # the mattress
for (x, y) in ((1, 1), (W - 2, 1), (1, H - 2), (W - 2, H - 2), (2, 1), (W - 3, 1)): b[x, y] = g(WOOD[3], x, y, 3, 2)   # the posts
for x in range(3, W - 3, 3): b[x, 2] = WOOD[0] + (255,)                                     # the headboard's planks
# the pillow
for y in range(6, 12):
    for x in range(4, W - 4):
        ex, ey = (x - (W / 2 - .5)) / 7.2, (y - 8.5) / 3.4
        if ex * ex + ey * ey <= 1: b[x, y] = g(LINEN[3] if ey < 0.3 and ex < 0.4 else LINEN[2], x, y, 4, 3)
for x in range(6, W - 6): b[x, 9] = g(LINEN[1], x, 9, 5, 2)                                 # its crease
# the blanket: over the lower part, its edge turned down (lighter), a pale stripe across
for y in range(21, H - 3):
    for x in range(3, W - 3):
        if y <= 22: c = WOOL[3]
        elif y == 23: c = WOOL[0]
        elif y in (34, 35): c = STRIPE
        else: c = WOOL[2] if x < 7 else WOOL[1] if x < W - 6 else WOOL[0]
        b[x, y] = g(c, x, y, 6, 4)
for y in range(24, H - 3, 5): b[W - 5, y] = WOOL[0] + (255,)                               # folds
outline(bed); bed.save(os.path.join(out, "occ_bedroll.png"))

# ---------------------------------------------------------------- the sleeper's head on the pillow
hd, p = canvas()
cx, cy = W / 2 - .5, 9.0
for y in range(H):
    for x in range(W):
        ex, ey = (x - cx) / 5.4, (y - cy) / 5.0
        r = ex * ex + ey * ey
        if r <= 1:
            fx, fy = (x - cx) / 3.0, (y - (cy + 0.8)) / 3.2
            if fx * fx + fy * fy <= 1: p[x, y] = g(SKIN[2] if fx < 0.2 and fy < 0.2 else SKIN[1], x, y, 7, 3)   # the face, up
            elif r > 0.72: p[x, y] = g(RED[1] if ey > -0.5 else RED[0], x, y, 8, 3)                        # the hood's red rim
            else: p[x, y] = g(ROBE[2] if ex < 0 else ROBE[1], x, y, 9, 3)                                 # the hood
for x in (int(cx - 1.5), int(cx + 1.5)): p[x, int(cy + 0.5)] = SKIN[0] + (255,)                          # eyes shut
p[int(cx), int(cy + 2.5)] = (120, 92, 88, 255)                                                          # the mouth
outline(hd); hd.save(os.path.join(out, "cult_sleep.png"))

# ---------------------------------------------------------------- the blanket over him
bl, q = canvas()
for y in range(13, H - 3):
    for x in range(3, W - 3):
        # the shape of the body under the wool: shoulders, then narrowing to the feet; the ridge lit from the left
        t = (y - 13) / (H - 16)
        half = 7.5 - 2.5 * max(0, t - 0.35)
        d = (x - (W / 2 - .5)) / half
        if abs(d) <= 1.0:
            lift = 1 - d * d
            c = WOOL[3] if y <= 14 else WOOL[3] if lift > 0.75 and d < 0 else WOOL[2] if lift > 0.4 else WOOL[1] if d < 0 else WOOL[0]
        else:
            c = WOOL[1] if x < 7 else WOOL[0]                                                 # the blanket's flat edges
        if y in (13, 14): c = WOOL[3] if y == 13 else WOOL[0]                                  # the turned-down edge at the chest
        if y in (34, 35): c = STRIPE if abs(d) <= 1.0 or True else c
        q[x, y] = g(c, x, y, 10, 4)
for y in range(16, 36, 4): q[int(W / 2 + 4), y] = WOOL[0] + (255,)                           # folds
for (x, y, c) in ((5, 16, SKIN[1]), (6, 16, SKIN[2]), (5, 17, SKIN[1]), (6, 17, SKIN[1]), (4, 15, ROBE[1]), (5, 15, ROBE[1]), (4, 14, ROBE[1])):   # a pale hand over the edge
    q[x, y] = c + (255,)
outline(bl); bl.save(os.path.join(out, "cult_sleep_blanket.png"))

# ---------------------------------------------------------------- a "z"
z = Image.new("RGBA", (7, 7), (0, 0, 0, 0)); zp = z.load()
for (x, y) in ((1, 1), (2, 1), (3, 1), (4, 1), (5, 1), (4, 2), (3, 3), (2, 4), (1, 5), (2, 5), (3, 5), (4, 5), (5, 5)): zp[x, y] = (226, 222, 236, 255)
outline(z); z.save(os.path.join(out, "occ_z.png"))

# preview: empty bed, and with him in it
pv = Image.new("RGBA", (W * 2 + 8, H), (70, 56, 50, 255))
pv.alpha_composite(bed, (0, 0)); pv.alpha_composite(bed, (W + 8, 0)); pv.alpha_composite(bl, (W + 8, 0)); pv.alpha_composite(hd, (W + 8, 0))
pv.resize((pv.width * 6, pv.height * 6), Image.NEAREST).save(os.path.join(out, "bed_preview.png"))
print("beds")
