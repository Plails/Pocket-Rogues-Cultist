# The cult's general (Чемпион культа), drawn whole by hand after the user's concept, for the game's two-handed rig
# (the skeleton axeman's bones and moves: idle, walk, the two-handed overhead blow and the power blow). Every picture is
# new: a big man in a black robe with a grey tabard, dark steel pauldrons with long pale spikes, a pale bald scarred
# head, dark sleeves with steel bracers, iron gauntlets, black boots, and a huge two-handed morningstar.
# Seen from above like everything in the game: the figure's FRONT is the TOP of the picture. All pictures are drawn
# at twice the rig's own size (the mod gives them twice its pixels-per-unit), so they stand exactly where the rig's
# parts stand, with finer detail.
#   usage: python general_art.py <out dir> <dir with Merc_Berserk_Parts_UpperArm/ForeArm/Hand.png (their shapes are followed)>
import sys, os, math
from PIL import Image, ImageFilter
out = sys.argv[1]; os.makedirs(out, exist_ok=True)
OL = (8, 6, 8, 255)
def h2(x, y, s): return (math.sin(x * 12.9898 + y * 78.233 + s * 37.719) * 43758.5453) % 1.0
def g(c, x, y, s=1, k=4): d = (h2(x, y, s) - .5) * 2 * k; return tuple(max(0, min(255, int(v + d))) for v in c[:3]) + (255,)
def ramp(r, t): return r[max(0, min(len(r) - 1, int(t * len(r))))]
def outline(im):
    w, h = im.size; px = im.load()
    ring = [(x, y) for y in range(h) for x in range(w) if px[x, y][3] == 0 and any(0 <= x + a < w and 0 <= y + b < h and px[x + a, y + b][3] > 0 and px[x + a, y + b] != OL for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for q in ring: px[q] = OL
ROBE = [(12, 10, 14), (20, 18, 24), (30, 27, 35), (42, 38, 48)]
TAB = [(72, 72, 76), (100, 100, 104), (130, 130, 134)]
STEEL = [(34, 34, 40), (56, 56, 64), (86, 86, 98), (124, 124, 138), (168, 168, 180)]
BONE = [(150, 140, 122), (198, 190, 168), (232, 226, 208)]
SKIN = [(112, 98, 90), (150, 136, 126), (188, 176, 164), (218, 210, 200)]
WOOD = [(36, 24, 18), (54, 36, 26), (74, 50, 36)]
LEATHER = [(60, 30, 22), (86, 44, 30)]

def blank(w, h): im = Image.new("RGBA", (w, h), (0, 0, 0, 0)); return im, im.load()
def ell(px, w, h, cx, cy, rx, ry, col_fn):
    for y in range(h):
        for x in range(w):
            ex, ey = (x - cx) / rx, (y - cy) / ry
            if ex * ex + ey * ey <= 1: px[x, y] = col_fn(x, y, ex, ey)
def spike(px, w, h, x0, y0, ang, n, base=1):
    a = math.radians(ang)
    for k in range(n):
        x, y = x0 + math.cos(a) * k, y0 + math.sin(a) * k
        wdt = base if k < n - 2 else 0
        for o in range(-wdt, wdt + 1):
            X, Y = round(x - math.sin(a) * o), round(y + math.cos(a) * o)
            if 0 <= X < w and 0 <= Y < h: px[X, Y] = (BONE[2] if k >= n - 2 else BONE[1] if o == 0 else BONE[0]) + (255,)

# ---------------------------------------------------------------- body 64x32 (the rig's 24x12 is the middle 48x24;
# the margin is for the spikes - the picture is centred on the rig's pivot, so it stands just where the rig's body does)
W, H = 64, 32
body, bp = blank(W, H); c = W / 2 - .5; my = 16.0
# the robe over the back and shoulders (a wide flattened oval, the back - bottom of the picture - fuller)
ell(bp, W, H, c, my + 1.0, 19.0, 9.4, lambda x, y, ex, ey: g(ramp(ROBE, 0.62 - 0.32 * (ex * 0.5 + ey * 0.7) - 0.2 * (ex * ex + ey * ey)), x, y, 1, 3))
for y in range(int(my), int(my) + 10):                                     # folds of the robe on the back
    for x in (round(c - 6), round(c + 6)):
        if bp[x, y][3]: bp[x, y] = ROBE[0] + (255,)
# the grey tabard down the front (the top of the picture), a steel gorget around the neck
for y in range(int(my) - 9, int(my) + 1):
    for x in range(round(c - 4), round(c + 5)):
        if bp[x, y][3] or y >= my - 9: bp[x, y] = g(TAB[2] if y < my - 7 else TAB[1] if x < c + 2 else TAB[0], x, y, 2, 3)
ell(bp, W, H, c, my - 1.0, 6.4, 4.4, lambda x, y, ex, ey: g(ramp(STEEL, 0.7 - 0.4 * (ex * 0.5 + ey * 0.8)), x, y, 3, 3) if ex * ex + ey * ey > 0.42 else bp[x, y])
ell(bp, W, H, c, my + 5.5, 5.2, 3.0, lambda x, y, ex, ey: g(ramp(ROBE, 0.6 - 0.3 * ey), x, y, 4, 3))   # the hood thrown back
# the pauldrons: domed plates of blackened steel with a bright rim and rivets, long pale spikes
for side in (-1, 1):
    pcx, pcy = c + side * 14.0, my - 0.5
    def pf(x, y, ex, ey, side=side):
        r = ex * ex + ey * ey
        if r > 0.74: return g(STEEL[3] if ey < 0.2 else STEEL[2], x, y, 5, 3)          # the rolled rim
        l = 0.42 - 0.22 * (ex * -side * 0.5 + ey * 0.8) - 0.25 * r
        return g(ramp(STEEL[:3], l), x, y, 5, 3)
    ell(bp, W, H, pcx, pcy, 7.4, 6.4, pf)
    for k in (-1, 0, 1):                                                    # rivets
        X, Y = round(pcx + side * 3.0), round(pcy + k * 2.6)
        bp[X, Y] = STEEL[4] + (255,)
    for ang, n in ((-40, 8), (-5, 10), (30, 8)):                            # spikes outward (fanned toward the front)
        a = ang if side > 0 else 180 - ang
        spike(bp, W, H, pcx + side * 6.0, pcy + ang / 20.0, a, n)
    spike(bp, W, H, pcx + side * 1.5, pcy - 5.5, -90 + side * 25, 6, 0)     # one up from the top of the plate
outline(body); body.save(os.path.join(out, "gen_body.png"))

# ---------------------------------------------------------------- head 24x24 (12x12): pale, bald, scarred
W = H = 24
head, hp = blank(W, H); c = W / 2 - .5; cy = 10.0
ell(hp, W, H, c, cy, 6.6, 7.6, lambda x, y, ex, ey: g(ramp(SKIN, 0.78 - 0.38 * (ex * 0.55 + ey * 0.75) - 0.3 * (ex * ex + ey * ey)), x, y, 6, 4))
for s in (-1, 1):                                                           # ears
    for dy in (0, 1, 2): X = round(c + s * 6.8); hp[X, round(cy + dy)] = SKIN[1 if dy < 2 else 0] + (255,)
for x in range(round(c - 4), round(c + 5)): hp[x, round(cy - 6)] = SKIN[0] + (255,)   # the heavy brow at the front
hp[round(c - 2), round(cy - 7)] = SKIN[1] + (255,); hp[round(c + 2), round(cy - 7)] = SKIN[1] + (255,)
for k in range(6): hp[round(c + 2 + k * 0.4), round(cy - 4 + k)] = (120, 70, 66, 255)  # an old scar across the scalp
for k in range(3): hp[round(c - 3), round(cy + 3 + k)] = SKIN[1] + (255,)             # a crease at the back of the skull
outline(head); head.save(os.path.join(out, "gen_head.png"))

# ---------------------------------------------------------------- arms: the shapes of the game's own arms for this
# two-handed rig (the two-handed berserker's: a slanted upper arm, a forearm from the elbow up to the wrist, a fist
# round the haft - the user: hold it like the game's two-handed fighters), at twice their size, each with its own
# pivot (the mod sets them), filled with his sleeves, bracers and gauntlets
REF = sys.argv[2] if len(sys.argv) > 2 else None
def silhouette(name):
    # twice the size with a smooth edge, one pixel fine (a plain doubling left steps two pixels high - the user: his
    # pixels looked bigger than the game's)
    im = Image.open(os.path.join(REF, name + ".png")).convert("RGBA")
    al = im.getchannel("A").point(lambda v: 255 if v > 0 else 0).resize((im.width * 2, im.height * 2), Image.NEAREST).filter(ImageFilter.GaussianBlur(1.1)); p = al.load()
    return al.size, [[p[x, y] >= 120 for x in range(al.width)] for y in range(al.height)]
def arm(name, src, fill):
    (w, h), m = silhouette(src); im, p = blank(w, h)
    xs = [x for y in range(h) for x in range(w) if m[y][x]]; ys = [y for y in range(h) for x in range(w) if m[y][x]]
    cx, cy = (min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2; rx, ry = max(1, (max(xs) - min(xs)) / 2), max(1, (max(ys) - min(ys)) / 2)
    for y in range(h):
        for x in range(w):
            if m[y][x]: p[x, y] = fill(x, y, (x - cx) / rx, (y - cy) / ry)
    # a one-pixel light rim on the lit (upper) edge and a dark one below, like the game's own parts
    for y in range(h):
        for x in range(w):
            if not m[y][x]: continue
            up = y == 0 or not m[y - 1][x]; dn = y == h - 1 or not m[y + 1][x]
            c = p[x, y]
            if up: p[x, y] = tuple(min(255, v + 22) for v in c[:3]) + (255,)
            elif dn: p[x, y] = tuple(max(0, v - 12) for v in c[:3]) + (255,)
    outline(im); im.save(os.path.join(out, name + ".png"))
arm("gen_upperarm", "Merc_Berserk_Parts_UpperArm", lambda x, y, ex, ey: g(ramp(ROBE, 0.8 - 0.4 * (ex * 0.6 + ey * 0.4)), x, y, 7, 3))
def fore(x, y, ex, ey):                                                     # the elbow is at the bottom, the wrist at the top
    if ey < -0.05: return g(ramp(STEEL[:4], 0.66 - 0.4 * ex - (0.25 if ey < -0.8 else 0)), x, y, 8, 3)   # the steel bracer at the wrist
    return g(ramp(ROBE, 0.75 - 0.4 * ex), x, y, 9, 3)
arm("gen_forearm", "Merc_Berserk_Parts_ForeArm", fore)
arm("gen_hand", "Merc_Berserk_Parts_Hand", lambda x, y, ex, ey: g(STEEL[2] if ey < -0.5 and (x % 3) else ramp(STEEL[:3], 0.55 - 0.35 * (ex * 0.6 + ey * 0.6)), x, y, 11, 3))
# legs 24x24 (12x12 at ppu 38): black boots with a steel toe
def limb(name, fill, rx, ry, cy=12.0):
    im, p = blank(24, 24); ell(p, 24, 24, 11.5, cy, rx, ry, fill); outline(im); im.save(os.path.join(out, name + ".png"))
limb("gen_leg", lambda x, y, ex, ey: g(ramp(ROBE, 0.6 - 0.35 * ey) if ey > -0.45 else STEEL[2], x, y, 12, 3), 4.0, 6.0)

# ---------------------------------------------------------------- the two-handed morningstar 96x46 (48x23)
# held where the rig holds its two-handed weapons: the grip at (0.6875, 0.3913) of the picture; the head at the far
# (left) end, like the rig's own axe blade, the pommel at the right
W, H = 96, 46
mace, mp = blank(W, H)
gy = round(H - 1 - 0.3913 * H)                                              # the haft's line (picture rows go down)
gx = round(0.6875 * W)
for x in range(14, 93):                                                     # the haft: dark wood, a leather-wound grip, steel bands
    for o in (-1, 0, 1):
        col = WOOD[2] if o == -1 else WOOD[1] if o == 0 else WOOD[0]
        if gx - 9 <= x <= gx + 9 or gx - 53 <= x <= gx - 37: col = LEATHER[1] if o < 1 and (x // 2) % 2 else LEATHER[0]   # wound where the two hands hold it
        if x in (gx - 11, gx - 10, gx + 11, gx - 55, gx - 35, gx - 24): col = STEEL[3] if o < 1 else STEEL[1]
        mp[x, gy + o] = col + (255,)
for o in (-2, -1, 0, 1, 2):                                                 # the pommel
    for x in (91, 92, 93): mp[x, gy + o] = (STEEL[3] if o < 0 else STEEL[1]) + (255,)
mp[94, gy] = STEEL[1] + (255,)
for x in range(16, 22):                                                     # the steel collar under the head
    for o in (-2, -1, 0, 1, 2): mp[x, gy + o] = g(STEEL[3] if o < 0 else STEEL[2], x, gy + o, 13, 3)
hcx, hcy, R = 11.0, gy, 8.6                                                  # the head: a black iron ball
ell(mp, W, H, hcx, hcy, R, R, lambda x, y, ex, ey: g(ramp(STEEL, 0.55 - 0.42 * (ex * 0.6 + ey * 0.8) - 0.25 * (ex * ex + ey * ey)) if ex * ex + ey * ey < 0.8 else STEEL[0], x, y, 14, 4))
for k in range(10):                                                         # ten long spikes, pale tips
    a = k * 36 + 18
    spike(mp, W, H, hcx + math.cos(math.radians(a)) * (R - 1), hcy + math.sin(math.radians(a)) * (R - 1), a, 6 if k % 2 else 5)
for a in (0, 120, 240): mp[round(hcx + math.cos(math.radians(a)) * 3), round(hcy + math.sin(math.radians(a)) * 3)] = BONE[2] + (255,)   # studs
outline(mace); mace.save(os.path.join(out, "gen_mace.png"))

# ---------------------------------------------------------------- the bestiary picture and the corpse
def place(dst, im, cx, cy): dst.alpha_composite(im, (round(cx - im.width / 2), round(cy - im.height / 2)))
pv = Image.new("RGBA", (130, 90), (0, 0, 0, 0))
place(pv, mace, 65 - (gx - 48), 26)
for s in (-1, 1): place(pv, Image.open(os.path.join(out, "gen_hand.png")), 65 + s * 8, 26 + 3)
place(pv, body, 65, 52); place(pv, head, 65, 48)
pv = pv.crop(pv.getbbox()); pv.save(os.path.join(out, "gen_preview.png"))
dead = Image.new("RGBA", (72, 72), (0, 0, 0, 0)); dp = dead.load()
ell(dp, 72, 72, 36, 40, 26, 17, lambda x, y, ex, ey: (92 + int(h2(x, y, 20) * 20), 10, 12, 210) if h2(x // 3, y // 3, 21) > 0.25 or ex * ex + ey * ey < 0.5 else (0, 0, 0, 0))
place(dead, body.rotate(-20, expand=True), 36, 40); place(dead, head.rotate(-20, expand=True), 30, 34)
place(dead, mace.rotate(35, expand=True), 38, 30)
dead.save(os.path.join(out, "gen_dead.png"))

sheet = Image.new("RGBA", (1700, 260), (82, 62, 50, 255)); x = 0
for n in ("gen_body", "gen_head", "gen_upperarm", "gen_forearm", "gen_hand", "gen_leg", "gen_mace", "gen_preview", "gen_dead"):
    i = Image.open(os.path.join(out, n + ".png")); k = 4 if i.width < 60 else 2
    i = i.resize((i.width * k, i.height * k), Image.NEAREST); sheet.alpha_composite(i, (x, 0)); x += i.width + 10
sheet.save(os.path.join(os.path.dirname(os.path.abspath(__file__)), "gen_sheet.png"))
print("general art")
