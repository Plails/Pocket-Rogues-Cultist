# The cult's champion (Чемпион культа), after the user's concept: a big man in a black robe with a grey tabard down his
# front, huge black pauldrons with long pale spikes, a pale bald head, dark sleeves with steel bracers, and a huge
# two-handed mace - a long dark haft with a black spiked star at its end. Seen from above like everything in the game
# (the figure faces UP in the picture). Arms keep the ogre rig's shapes (elder_o* recoloured: steel instead of gold).
#   usage: python champion_art.py <elder art3 dir (elder_o*.png)> <out dir>
import sys, os, math
from PIL import Image
src, out = sys.argv[1], sys.argv[2]
os.makedirs(out, exist_ok=True)
OL = (0, 0, 0, 255)
def h2(x, y, s): return (math.sin(x * 12.9898 + y * 78.233 + s * 37.719) * 43758.5453) % 1.0
def g(c, x, y, s=1, k=5): d = (h2(x, y, s) - .5) * 2 * k; return tuple(max(0, min(255, int(v + d))) for v in c[:3]) + (255,)
def outline(im):
    w, h = im.size; px = im.load()
    ring = [(x, y) for y in range(h) for x in range(w) if px[x, y][3] == 0 and any(0 <= x + a < w and 0 <= y + b < h and px[x + a, y + b][3] > 0 and px[x + a, y + b] != OL for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for q in ring: px[q] = OL
ROBE = [(10, 9, 12), (18, 16, 21), (28, 25, 32), (40, 36, 45)]
TABARD = [(70, 70, 74), (100, 100, 104), (132, 132, 136)]
STEEL = [(40, 40, 46), (70, 70, 80), (110, 110, 122), (160, 160, 172)]
BONE = [(150, 140, 122), (196, 188, 166), (228, 222, 204)]
SKIN = [(120, 106, 96), (160, 146, 134), (196, 184, 172), (222, 214, 204)]

# ---------------------------------------------------------------- body: robe, tabard, spiked pauldrons
W = 36
im = Image.new("RGBA", (W, W), (0, 0, 0, 0)); px = im.load(); c = W / 2 - .5
for y in range(W):
    for x in range(W):
        ex, ey = (x - c) / 11.5, (y - c - 1) / 7.5
        if ex * ex + ey * ey <= 1:
            l = 0.55 - 0.35 * (ex * 0.6 + ey * 0.8)
            px[x, y] = g(ROBE[max(0, min(3, int(l * 4)))], x, y, 1, 3)
for y in range(int(c - 4), int(c + 8)):                                     # the grey tabard down his front (the top of the picture is his front)
    for x in range(int(c - 2), int(c + 3)):
        if px[x, y][3]: px[x, y] = g(TABARD[1 if x < c + 1 else 0] if y > c - 3 else TABARD[2], x, y, 2, 4)
for x in range(int(c - 2), int(c + 3)): px[x, int(c + 2)] = STEEL[1] + (255,)   # a belt across
for side in (-1, 1):                                                        # the pauldrons: big black domes with long pale spikes
    pcx, pcy = c + side * 9.5, c - 1
    for y in range(W):
        for x in range(W):
            d = math.hypot(x - pcx, y - pcy)
            if d <= 5.2:
                l = 0.65 - 0.3 * ((x - pcx) * -side + (y - pcy)) / 5 - 0.25 * (d / 5.2) ** 2
                px[x, y] = g(STEEL[max(0, min(2, int(l * 3)))] if d > 4.2 else ROBE[max(1, min(3, int(l * 4)))], x, y, 3, 4)
    for ang in (-60, 0, 55):                                                # three spikes outward
        a = math.radians(ang + (0 if side > 0 else 180))
        for k in range(5):
            x, y = round(pcx + math.cos(a) * (4.5 + k)), round(pcy + math.sin(a) * (4.5 + k))
            if 0 <= x < W and 0 <= y < W: px[x, y] = (BONE[2] if k > 2 else BONE[1]) + (255,)
outline(im); im.save(os.path.join(out, "champ_body.png")); body = im

# ---------------------------------------------------------------- head: pale, bald, seen from above
H = 13
im = Image.new("RGBA", (H, H), (0, 0, 0, 0)); px = im.load(); c = H / 2 - .5
for y in range(H):
    for x in range(H):
        ex, ey = (x - c) / 5.0, (y - c) / 5.6
        if ex * ex + ey * ey <= 1:
            l = 0.62 - 0.4 * (ex * 0.7 + ey * 0.7) - 0.2 * (ex * ex + ey * ey)
            px[x, y] = g(SKIN[max(0, min(3, int(l * 4)))], x, y, 4, 4)
for x in (round(c - 5), round(c + 5)): px[x, round(c + 0.5)] = SKIN[1] + (255,)   # ears
for x in range(round(c - 2), round(c + 3)): px[x, round(c - 4)] = SKIN[0] + (255,)   # the brow at the front
px[round(c + 2), round(c - 1)] = SKIN[0] + (255,); px[round(c + 2), round(c)] = SKIN[0] + (255,)   # an old scar
outline(im); im.save(os.path.join(out, "champ_head.png"))

# ---------------------------------------------------------------- arms: the ogre shapes, dark sleeves, steel bracers
GOLDS = {(110, 80, 26): 0, (156, 118, 40): 1, (200, 160, 62): 2, (234, 204, 110): 3}
for n in ("upperarm", "forearm", "hand", "leg"):
    a = Image.open(os.path.join(src, "elder_o" + n + ".png")).convert("RGBA"); p = a.load()
    for y in range(a.height):
        for x in range(a.width):
            q = p[x, y]
            if q[3] == 0: continue
            k = GOLDS.get(q[:3])
            if k is not None: p[x, y] = STEEL[k] + (q[3],)
            elif q[0] > q[1] + 30: p[x, y] = ROBE[2] + (q[3],)              # the red sleeves under the mantle: black here
    a.save(os.path.join(out, "champ_o" + n + ".png"))

# ---------------------------------------------------------------- the huge two-handed mace (diagonal: the grip at the lower left, the head at the upper right)
M = 44
im = Image.new("RGBA", (M, M), (0, 0, 0, 0)); px = im.load()
x0, y0, x1, y1 = 3, M - 4, M - 12, 11
L = math.hypot(x1 - x0, y1 - y0); dx, dy = (x1 - x0) / L, (y1 - y0) / L; nx, ny = -dy, dx
if nx + ny > 0: nx, ny = -nx, -ny
for i in range(int(L * 2)):
    t = i / 2; x, y = x0 + dx * t, y0 + dy * t
    grip = t < 9
    a, b = ((52, 30, 20), (84, 52, 32)) if grip else ((26, 24, 30), (54, 52, 60))
    if abs(t - 9) < .6 or abs(t - 22) < .6: a, b = STEEL[1], STEEL[3]
    for (xx, yy, cc) in ((x, y, a), (x + nx * .9, y + ny * .9, b)):
        X, Y = round(xx), round(yy)
        if 0 <= X < M and 0 <= Y < M: px[X, Y] = cc + (255,)
hx, hy = x1 + dx * 2, y1 + dy * 2                                            # the spiked star
for y in range(M):
    for x in range(M):
        d = math.hypot(x - hx, y - hy)
        if d <= 5.0:
            l = 0.6 - 0.3 * ((x - hx) * nx + (y - hy) * ny) / -5
            px[x, y] = g(STEEL[max(0, min(2, int(l * 3)))] if d < 4 else STEEL[0], x, y, 6, 4)
for k in range(8):
    a = k * math.pi / 4 + 0.2
    for j in range(5):
        X, Y = round(hx + math.cos(a) * (4.5 + j)), round(hy + math.sin(a) * (4.5 + j))
        if 0 <= X < M and 0 <= Y < M: px[X, Y] = (STEEL[2] if j < 3 else STEEL[3]) + (255,)
outline(im); im.save(os.path.join(out, "champ_mace.png")); mace = im
open(os.path.join(out, "champ_mace.grip"), "w").write("%.3f,%.3f" % ((x0 + dx * 6 + .5) / M, (M - (y0 + dy * 6) - .5) / M))   # the grip: a third up the haft

# ---------------------------------------------------------------- preview (bestiary) and corpse
hd = Image.open(os.path.join(out, "champ_head.png"))
P = Image.new("RGBA", (56, 56), (0, 0, 0, 0))
P.alpha_composite(mace, (14, 0)); P.alpha_composite(body, (13, 20)); P.alpha_composite(hd, (21, 22))
P.save(os.path.join(out, "champ_preview.png"))
D = Image.new("RGBA", (62, 46), (0, 0, 0, 0))
bd = body.rotate(100, resample=Image.NEAREST, expand=True); D.alpha_composite(bd, (10, 6))
D.alpha_composite(hd.rotate(80, resample=Image.NEAREST, expand=True), (4, 16))
D.alpha_composite(mace.rotate(-30, resample=Image.NEAREST, expand=True).crop((0, 0, 60, 40)), (2, 4))
dp = D.load()
for (x, y) in [(30, 30), (31, 31), (29, 32), (33, 30), (28, 29)]: dp[x, y] = (110, 10, 14, 255)   # blood
D.save(os.path.join(out, "champ_dead.png"))

sheet = Image.new("RGBA", (1500, 340), (60, 44, 36, 255)); x = 0
for n in ("champ_body", "champ_head", "champ_oupperarm", "champ_oforearm", "champ_ohand", "champ_mace", "champ_preview", "champ_dead"):
    i = Image.open(os.path.join(out, n + ".png")); i = i.resize((i.width * 5, i.height * 5), Image.NEAREST); sheet.alpha_composite(i, (x, 0)); x += i.width + 10
sheet.save(os.path.join(os.path.dirname(os.path.abspath(__file__)), "champ_sheet.png"))
print("champion art")

# ---------------------------------------------------------------- parts of his own rig (he is drawn and moved by ChampionRig, not by a game body)
# the mace upright (its head at the top: forward), the grip near its bottom
MW, MH = 15, 44
im = Image.new("RGBA", (MW, MH), (0, 0, 0, 0)); px = im.load(); cx = MW // 2
for y in range(11, MH - 1):
    grip = y > MH - 13
    a, b = ((52, 30, 20), (84, 52, 32)) if grip else ((26, 24, 30), (58, 56, 64))
    if y in (MH - 13, 22): a, b = STEEL[1], STEEL[3]
    px[cx, y] = a + (255,); px[cx - 1, y] = b + (255,)
px[cx, MH - 1] = STEEL[2] + (255,); px[cx - 1, MH - 1] = STEEL[2] + (255,)
hy = 6
for y in range(MH):
    for x in range(MW):
        d = math.hypot(x - cx + .5, y - hy)
        if d <= 4.2: px[x, y] = g(STEEL[2] if (x - cx + y - hy) < -1 else STEEL[1] if d < 3.4 else STEEL[0], x, y, 7, 4)
for k in range(8):
    a = k * math.pi / 4
    for j in range(3):
        X, Y = round(cx - .5 + math.cos(a) * (4.3 + j)), round(hy + math.sin(a) * (4.3 + j))
        if 0 <= X < MW and 0 <= Y < MH: px[X, Y] = (STEEL[2] if j < 2 else STEEL[3]) + (255,)
outline(im); im.save(os.path.join(out, "champ_mace_v.png"))
# a piece of sleeve, a fist, a boot (all small, laid along his arms / under him)
im = Image.new("RGBA", (6, 6), (0, 0, 0, 0)); px = im.load()
for y in range(1, 5):
    for x in range(1, 5): px[x, y] = g(ROBE[3] if y == 1 else ROBE[2], x, y, 8, 3)
outline(im); im.save(os.path.join(out, "champ_sleeve.png"))
im = Image.new("RGBA", (7, 7), (0, 0, 0, 0)); px = im.load()
for y in range(1, 6):
    for x in range(1, 6):
        if math.hypot(x - 3, y - 3) <= 2.4: px[x, y] = (STEEL[1] if x + y > 6 else STEEL[2]) + (255,)   # steel gauntlets
outline(im); im.save(os.path.join(out, "champ_hand.png"))
im = Image.new("RGBA", (8, 10), (0, 0, 0, 0)); px = im.load()
for y in range(1, 9):
    for x in range(1, 7):
        if ((x - 3.5) / 2.6) ** 2 + ((y - 4.5) / 3.8) ** 2 <= 1: px[x, y] = g(ROBE[1] if y > 3 else ROBE[2], x, y, 9, 3)
outline(im); im.save(os.path.join(out, "champ_foot.png"))
print("rig parts")
