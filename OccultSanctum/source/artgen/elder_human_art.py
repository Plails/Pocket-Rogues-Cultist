# The cult's elder as a man: the cultist's own pictures (same shapes, same black hood and robe - the user likes the
# cultist) with the marks of his rank - the red hems turned gold, a deep red mantle over the shoulders, a gold sign on
# the breast - drawn bigger in the game (a little bigger than the crossbowman). His great staff stays.
#   usage: python elder_human_art.py <cult art dir> <elder art dir (has elder_staff.png)> <out dir>
import sys, os
from PIL import Image
cult, eld, out = sys.argv[1], sys.argv[2], sys.argv[3]
os.makedirs(out, exist_ok=True)
TRIM = {(74, 14, 20): 0, (110, 22, 28): 1, (146, 32, 34): 2, (176, 52, 44): 3}
GOLD = [(110, 80, 26), (156, 118, 40), (200, 160, 62), (234, 204, 110)]
MANTLE = [(46, 8, 14), (66, 12, 20), (88, 16, 24), (112, 24, 30), (136, 32, 36)]
lum = lambda p: .3 * p[0] + .59 * p[1] + .11 * p[2]

def load(n): return Image.open(os.path.join(cult, n + ".png")).convert("RGBA")

def gild(im, mantle_rows=None):
    w, h = im.size; px = im.load()
    for y in range(h):
        for x in range(w):
            p = px[x, y]
            if p[3] < 100: continue
            k = TRIM.get(p[:3])
            if k is not None: px[x, y] = GOLD[k] + (p[3],); continue
            # the mantle: the robe's dark cloth on the shoulders turned deep red (the outline stays black)
            if mantle_rows and mantle_rows(x, y, w, h) and 12 < lum(p) < 90 and abs(p[0] - p[2]) < 20:
                t = min(1, max(0, (lum(p) - 12) / 50))
                px[x, y] = MANTLE[min(4, round(t * 4))] + (p[3],)
    return im

head = gild(load("cult_head")); head.save(os.path.join(out, "elder_head.png"))
body = gild(load("cult_body"), lambda x, y, w, h: y < h * 0.5)
bp = body.load(); cx = body.size[0] // 2
for (dx, dy, c) in ((0, 0, GOLD[3]), (-1, 0, GOLD[1]), (1, 0, GOLD[1]), (0, 1, GOLD[2]), (0, -1, GOLD[2])):   # the gold sign on his breast
    x, y = cx + dx - 1, body.size[1] // 2 + dy - 1
    if bp[x, y][3] > 100: bp[x, y] = c + (255,)
body.save(os.path.join(out, "elder_body.png"))
gild(load("cult_upperarm"), lambda x, y, w, h: True).save(os.path.join(out, "elder_upperarm.png"))   # sleeves under the mantle: red
gild(load("cult_forearm")).save(os.path.join(out, "elder_forearm.png"))                             # gold cuffs
load("cult_hand").save(os.path.join(out, "elder_hand.png"))
load("cult_leg").save(os.path.join(out, "elder_leg.png"))
staff = Image.open(os.path.join(eld, "elder_staff.png")).convert("RGBA")
staff.save(os.path.join(out, "elder_staff.png"))

# the bestiary picture: the cultist's own, gilded and mantled, the great staff beside him
pv = gild(load("cult_preview"), lambda x, y, w, h: y < h * 0.5)
P = Image.new("RGBA", (pv.width + 10, pv.height + 6), (0, 0, 0, 0))
P.alpha_composite(staff, (0, 4))
P.alpha_composite(pv, (8, 6))
P.save(os.path.join(out, "elder_preview.png"))

# the corpse: the cultist's, gilded, with the staff dropped beside him
cd = gild(load("cult_dead"))
D = Image.new("RGBA", (cd.width + 14, cd.height + 6), (0, 0, 0, 0))
D.alpha_composite(staff.rotate(-25, resample=Image.NEAREST, expand=True), (cd.width - 12, 0))
D.alpha_composite(cd, (0, 5))
D.save(os.path.join(out, "elder_dead.png"))

sheet = Image.new("RGBA", (1400, 260), (70, 50, 40, 255)); x = 0
for n in ("elder_head", "elder_body", "elder_upperarm", "elder_forearm", "elder_hand", "elder_staff", "elder_preview", "elder_dead"):
    i = Image.open(os.path.join(out, n + ".png")).convert("RGBA"); i = i.resize((i.width * 5, i.height * 5), Image.NEAREST)
    sheet.alpha_composite(i, (x, 0)); x += i.width + 10
sheet.save(os.path.join(os.path.dirname(os.path.abspath(__file__)), "elder_sheet.png"))
print("elder (man) art")
