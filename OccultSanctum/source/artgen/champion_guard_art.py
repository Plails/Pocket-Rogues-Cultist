# The champion on the game's guardsman warrior (its shapes and its moves kept - the user: "on someone, but beautiful"),
# recoloured after the user's concept: black robe, dark steel pauldrons with pale spikes, a grey tabard instead of the red
# trim, a pale bald head instead of the helmet, dark sleeves and steel gauntlets, his morningstar blackened.
#   usage: python champion_guard_art.py <guard parts dir> <out dir>
import sys, os, math
from PIL import Image
src, out = sys.argv[1], sys.argv[2]
os.makedirs(out, exist_ok=True)
OL = (0, 0, 0, 255)
lum = lambda p: .3 * p[0] + .59 * p[1] + .11 * p[2]
def outline(im):
    w, h = im.size; px = im.load()
    ring = [(x, y) for y in range(h) for x in range(w) if px[x, y][3] == 0 and any(0 <= x + a < w and 0 <= y + b < h and px[x + a, y + b][3] > 0 and px[x + a, y + b] != OL for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for q in ring: px[q] = OL
ROBE = [(12, 11, 14), (22, 20, 26), (34, 31, 39), (48, 44, 54)]
STEEL = [(34, 34, 40), (58, 58, 68), (92, 92, 104), (136, 136, 150)]
GREY = [(70, 70, 74), (104, 104, 108), (140, 140, 144)]
BONE = [(176, 168, 146), (214, 208, 190), (236, 232, 220)]
SKIN = [(118, 104, 94), (158, 144, 132), (196, 184, 172), (224, 216, 206)]
def ramp(r, t): return r[min(len(r) - 1, max(0, int(round(t * (len(r) - 1)))))]

def recolor(name, outname, rule):
    im = Image.open(os.path.join(src, name + ".png")).convert("RGBA"); w, h = im.size; px = im.load()
    pts = [(x, y) for y in range(h) for x in range(w) if px[x, y][3] > 0 and lum(px[x, y]) > 14]
    ls = sorted(lum(px[x, y]) for x, y in pts) or [0, 1]; lo, hi = ls[0], ls[-1]
    for x, y in pts:
        p = px[x, y]; t = (lum(p) - lo) / max(1, hi - lo)
        red = p[0] > p[1] + 30 and p[0] > p[2] + 20
        px[x, y] = rule(x, y, w, h, t, red) + (p[3],)
    return im

# the body: the wide plates on the shoulders stay steel (dark), the middle becomes the black robe, red becomes the grey tabard
def body_rule(x, y, w, h, t, red):
    if red: return ramp(GREY, t + .2)
    shoulder = abs(x - w / 2) > w * 0.2
    return ramp(STEEL, t * .9) if shoulder else ramp(ROBE, t)
b = recolor("GuardsmanWarrior_Body", "", body_rule); bp = b.load(); W, H = b.size
op = lambda X, Y: 0 <= X < W and 0 <= Y < H and bp[X, Y][3] > 0
for side in (-1, 1):                                                         # pale spikes growing out of the pauldrons' edges
    xs = range(W - 1, -1, -1) if side > 0 else range(W)
    for y0 in (H // 2 - 2, H // 2 + 2):                                       # two out of the side
        ex = next((x for x in xs if op(x, y0)), None)
        if ex is None: continue
        for k in range(1, 4): X = ex + side * k; Y = y0 - (k // 2)
        for k in range(1, 4):
            X, Y = ex + side * k, y0 - (k // 2)
            if 0 <= X < W and 0 <= Y < H: bp[X, Y] = BONE[1 if k < 3 else 2] + (255,)
    for xr in (0.33, 0.42):                                                  # two up from the top of the pauldron
        x0 = round(W / 2 + side * W * xr)
        ty = next((y for y in range(H) if op(x0, y)), None)
        if ty is None: continue
        for k in range(1, 4):
            X, Y = x0 + side * (k // 2), ty - k
            if 0 <= X < W and 0 <= Y < H: bp[X, Y] = BONE[1 if k < 3 else 2] + (255,)
outline(b); b.save(os.path.join(out, "champ_g_body.png"))

# the head: a pale bald head where the helmet was (the same size and place)
hd0 = Image.open(os.path.join(src, "GuardsmanWarrior_Head.png")).convert("RGBA"); hw, hh = hd0.size
hd = Image.new("RGBA", (hw, hh), (0, 0, 0, 0)); hp = hd.load(); cx, cy = hw / 2 - .5, hh / 2 - .5
for y in range(hh):
    for x in range(hw):
        ex, ey = (x - cx) / 6.2, (y - cy) / 7.2
        if ex * ex + ey * ey <= 1:
            l = 0.62 - 0.38 * (ex * 0.7 + ey * 0.7) - 0.2 * (ex * ex + ey * ey)
            hp[x, y] = ramp(SKIN, l) + (255,)
for x in (round(cx - 6.5), round(cx + 6.5)): hp[x, round(cy + 1)] = SKIN[1] + (255,); hp[x, round(cy + 2)] = SKIN[0] + (255,)   # ears
for x in range(round(cx - 3), round(cx + 4)): hp[x, round(cy - 5)] = SKIN[0] + (255,)   # the heavy brow at the front
hp[round(cx + 3), round(cy - 1)] = SKIN[0] + (255,); hp[round(cx + 3), round(cy)] = SKIN[0] + (255,); hp[round(cx + 2), round(cy + 1)] = SKIN[0] + (255,)   # a scar
outline(hd); hd.save(os.path.join(out, "champ_g_head.png"))

# arms: dark sleeves (the upper arm and forearm), steel gauntlets
for n, o, r in (("Guardsman_Warrior_UpperArm", "champ_g_upperarm", ROBE), ("GuardsmanWarrior_ForeArm", "champ_g_forearm", ROBE), ("GuardsmanWarrior_Hand", "champ_g_hand", STEEL), ("GuardsmanWarrior_Leg", "champ_g_leg", ROBE)):
    im = recolor(n, "", lambda x, y, w, h, t, red, r=r: ramp(GREY, t) if red else ramp(r, t * .9 + .1))
    im.save(os.path.join(out, o + ".png"))

# the morningstar blackened: a dark head with pale spike tips, a dark haft with a red-brown grip
def mace_rule(x, y, w, h, t, red):
    if red: return (60, 34, 22)
    head = x + (h - y) * 0 < w * 0.55 and y < h * 0.5
    return ramp(STEEL, t * 0.8) if not (t > 0.85 and head) else BONE[1]
recolor("GuardsmanWarrior_Weapon_1", "", mace_rule).save(os.path.join(out, "champ_g_mace.png"))

# the bestiary picture: the game's own preview recoloured, the bald head on it
pv = recolor("Guardsman_Warrior_preview", "", lambda x, y, w, h, t, red: ramp(GREY, t) if red else ramp(STEEL, t))
pv.alpha_composite(hd, (pv.width // 2 - hw // 2, pv.height // 2 - hh // 2 - 2))
pv.save(os.path.join(out, "champ_g_preview.png"))

sheet = Image.new("RGBA", (1500, 200), (70, 50, 40, 255)); x = 0
for n in ("champ_g_body", "champ_g_head", "champ_g_upperarm", "champ_g_forearm", "champ_g_hand", "champ_g_leg", "champ_g_mace", "champ_g_preview"):
    i = Image.open(os.path.join(out, n + ".png")); i = i.resize((i.width * 5, i.height * 5), Image.NEAREST); sheet.alpha_composite(i, (x, 0)); x += i.width + 10
sheet.save(os.path.join(os.path.dirname(os.path.abspath(__file__)), "champ_g_sheet.png"))
print("champion (guardsman) art")
