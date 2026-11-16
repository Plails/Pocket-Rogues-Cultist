# The cult's elder (Верховный культист): the game's ogre shaman's parts (its shapes kept) dressed as a high cultist -
# a black hood with bone horns through it and a red veil hanging in front, a black robe under a deep red mantle with
# a gold edge, black sleeves with red cuffs, pale hands; and a new great staff: a black shaft with gold bands, a horned
# skull on top holding a red glowing orb.   usage: python elder_art.py <ogre parts dir> <cult art dir> <out dir>
import sys, os, math
from PIL import Image
src, cult, out = sys.argv[1], sys.argv[2], sys.argv[3]
os.makedirs(out, exist_ok=True)
CLOAK = [(8, 7, 10), (16, 14, 19), (25, 22, 29), (36, 32, 41), (50, 45, 56), (64, 58, 72)]
TRIM = [(74, 14, 20), (110, 22, 28), (146, 32, 34), (176, 52, 44)]
MANTLE = [(48, 8, 14), (70, 12, 20), (96, 18, 26), (124, 26, 32), (150, 36, 38)]
GOLD = [(96, 70, 22), (150, 112, 36), (198, 156, 58), (232, 200, 104)]
PALE = [(58, 54, 52), (88, 82, 78), (122, 114, 108), (154, 146, 138)]
BONE = [(96, 88, 72), (150, 140, 118), (196, 188, 164), (228, 222, 202)]
lum = lambda p: .3 * p[0] + .59 * p[1] + .11 * p[2]
def kind(p):
    r, g, b = p[:3]
    if g > r + 10 and g > b + 10: return "skin"
    if r > 150 and g > 150 and b > 140: return "white"
    if abs(r - g) < 14 and abs(g - b) < 16 and lum(p) > 50: return "grey"
    if r > g + 12 and g >= b - 4: return "brown"
    return "other"
def ramp(r, t): return r[min(len(r) - 1, max(0, round(t * (len(r) - 1) + .2)))]

def recolor(name, outname, rules, edge_rule=None):
    im = Image.open(os.path.join(src, name + ".png")).convert("RGBA"); w, h = im.size; px = im.load()
    pts = [(x, y) for y in range(h) for x in range(w) if px[x, y][3] > 200 and lum(px[x, y]) > 22]
    ls = sorted(lum(px[x, y]) for x, y in pts) or [0, 1]; lo, hi = ls[0], ls[-1]
    clear = lambda x, y: not (0 <= x < w and 0 <= y < h) or px[x, y][3] < 100 or lum(px[x, y]) <= 22
    new = {}
    for (x, y) in pts:
        p = px[x, y]; t = (lum(p) - lo) / max(1, hi - lo); k = kind(p)
        r = rules.get(k, rules.get("other", CLOAK))
        c = ramp(r, t)
        if edge_rule and any(clear(x + dx, y + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
            e = edge_rule(k, x, y, w, h)
            if e: c = ramp(e, t)
        new[(x, y)] = c + (p[3],)
    for k2, v in new.items(): px[k2] = v
    im.save(os.path.join(out, outname + ".png")); return im

recolor("OgrMage_Head", "elder_head", {"skin": CLOAK, "white": BONE, "brown": TRIM, "grey": CLOAK, "other": CLOAK},
        lambda k, x, y, w, h: TRIM if k == "skin" and y >= h * .55 else None)          # red edge round the hood's face side
recolor("OgrMage_Body", "elder_body", {"skin": CLOAK, "grey": MANTLE, "white": MANTLE, "brown": MANTLE, "other": CLOAK},
        lambda k, x, y, w, h: GOLD if k in ("grey", "white") else None)                # the mantle's gold edge
recolor("OgrMage_UpperArm", "elder_upperarm", {"skin": CLOAK, "other": CLOAK})
recolor("OgrMage_ForeArm", "elder_forearm", {"skin": CLOAK, "brown": CLOAK, "grey": CLOAK, "white": CLOAK, "other": CLOAK},
        lambda k, x, y, w, h: TRIM if y <= h * .5 else None)                           # red cuffs at the hand end
recolor("OgrMage_Hand", "elder_hand", {"skin": PALE, "other": PALE})
recolor("troll1_parts_Leg", "elder_leg", {"other": CLOAK, "grey": CLOAK, "skin": CLOAK, "brown": CLOAK})

# the great staff, along the same line as the shaman's (so it sits in the hand the same way)
st = Image.open(os.path.join(src, "OgrMage_Stuff.png")).convert("RGBA"); W, H = st.size; sp = st.load()
op = [(x, y) for y in range(H) for x in range(W) if sp[x, y][3] > 100]
bot = max(op, key=lambda q: q[1] - q[0] * .3); top = min(op, key=lambda q: q[1] - q[0] * .3)
im = Image.new("RGBA", (W, H), (0, 0, 0, 0)); px = im.load()
x0, y0 = bot; x1, y1 = top
L = math.hypot(x1 - x0, y1 - y0); dx, dy = (x1 - x0) / L, (y1 - y0) / L; nx, ny = -dy, dx
def put(x, y, c):
    x, y = int(round(x)), int(round(y))
    if 0 <= x < W and 0 <= y < H: px[x, y] = c + (255,) if len(c) == 3 else c
head = 6.0                                                                  # the top part: the skull and the orb
for i in range(int(L - head) + 1):                                          # the shaft: black wood, gold bands
    x, y = x0 + dx * i, y0 + dy * i
    band = i % 5 == 2
    put(x, y, GOLD[2] if band else (34, 24, 22)); put(x + nx * .7, y + ny * .7, GOLD[1] if band else (58, 42, 36))
cx, cy = x0 + dx * (L - head * .55), y0 + dy * (L - head * .55)
for yy in range(H):                                                         # the skull at the top
    for xx in range(W):
        u = (xx + .5 - cx); v = (yy + .5 - cy)
        if (u / 2.8) ** 2 + (v / 2.6) ** 2 <= 1: px[xx, yy] = ramp(BONE, .9 - .5 * ((u + v) / 4 + .5)) + (255,)
put(cx - 1, cy + .5, (16, 10, 8)); put(cx + 1, cy - .5, (16, 10, 8))       # its eyes
for s in (-1, 1):                                                           # two horns curling up from it
    for k in range(5):
        a = k / 4
        put(cx + nx * s * (2.4 + a * 1.6) + dx * (a * 3 - .5), cy + ny * s * (2.4 + a * 1.6) + dy * (a * 3 - .5), BONE[2] if k < 4 else BONE[3])
ox, oy = cx + dx * 2.6, cy + dy * 2.6                                       # the red orb between the horns
for yy in range(H):
    for xx in range(W):
        d = math.hypot(xx + .5 - ox, yy + .5 - oy)
        if d <= 2.1: px[xx, yy] = ((255, 150, 150) if d < .8 else (220, 40, 52) if d < 1.5 else (140, 16, 30)) + (255,)
OL = (0, 0, 0, 255)
ring = [(x, y) for y in range(H) for x in range(W) if px[x, y][3] == 0 and any(0 <= x + a < W and 0 <= y + b < H and px[x + a, y + b][3] > 0 and px[x + a, y + b] != OL for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
for q in ring: px[q] = OL
im.save(os.path.join(out, "elder_staff.png")); staff = im

# preview (bestiary): staff, body, arms and head put together like the rig, a bit enlarged
P = 40; pv = Image.new("RGBA", (P, P), (0, 0, 0, 0))
def paste(name, cxp, cyp, img=None):
    i = img or Image.open(os.path.join(out, name + ".png")).convert("RGBA")
    pv.alpha_composite(i, (int(cxp - i.width / 2), int(cyp - i.height / 2)))
paste("elder_staff", 11, 15, staff)
paste("elder_hand", 10, 14); paste("elder_upperarm", 13, 20); paste("elder_upperarm", 27, 20); paste("elder_hand", 30, 16)
paste("elder_body", 20, 23); paste("elder_head", 20, 15)
pv.save(os.path.join(out, "elder_preview.png"))

# the corpse: the cultist's corpse, larger, with the great staff dropped beside him
cd = Image.open(os.path.join(cult, "cult_dead.png")).convert("RGBA")
cd = cd.resize((int(cd.width * 1.35), int(cd.height * 1.35)), Image.NEAREST)
D = Image.new("RGBA", (cd.width + 12, cd.height + 6), (0, 0, 0, 0))
D.alpha_composite(staff.rotate(-25, resample=Image.NEAREST, expand=True), (cd.width - 14, 0))
D.alpha_composite(cd, (0, 4))
D.save(os.path.join(out, "elder_dead.png"))

sheet = Image.new("RGBA", (8 * 200, 8 * 42), (70, 50, 40, 255)); x = 0
for n in ("elder_head", "elder_body", "elder_upperarm", "elder_forearm", "elder_hand", "elder_staff", "elder_preview", "elder_dead"):
    i = Image.open(os.path.join(out, n + ".png")).convert("RGBA"); i = i.resize((i.width * 5, i.height * 5), Image.NEAREST)
    sheet.alpha_composite(i, (x, 0)); x += i.width + 10
sheet.save(os.path.join(os.path.dirname(os.path.abspath(__file__)), "elder_sheet.png"))
print("elder art")
