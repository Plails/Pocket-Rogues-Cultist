# The hooded crossbowman = the game's orc crossbowman's parts (ordinary character proportions: head 12x12, body
# 24x12, arms 12x12, 32 ppu) recoloured: a grey hood and cowl, dark brown leather, a red sash, dark gloves; the head
# drawn anew as a small hood seen from above; the crossbow kept, a little darker; the orc's shield is not used.
# Plus the bestiary picture and the corpse.  usage: python xbow_art.py <parts dir> <out dir>
import sys, os
from PIL import Image

src, out = sys.argv[1], sys.argv[2]
GREY = [(22, 22, 25), (36, 36, 40), (52, 51, 56), (70, 69, 74), (90, 88, 93), (112, 110, 114)]
LEATHER = [(20, 13, 9), (34, 23, 16), (50, 35, 24), (68, 48, 33), (88, 64, 44)]
SASH = [(70, 12, 14), (104, 20, 22), (140, 32, 30)]
GLOVE = [(14, 12, 12), (26, 22, 22), (40, 34, 32), (54, 46, 42)]
lum = lambda p: .3 * p[0] + .59 * p[1] + .11 * p[2]
OL = (0, 0, 0, 255)


def recolor(name, ramp, fn=None):
    im = Image.open(os.path.join(src, name + ".png")).convert("RGBA")
    w, h = im.size; px = im.load()
    body = [(x, y) for y in range(h) for x in range(w) if px[x, y][3] > 200 and lum(px[x, y]) > 20]
    ls = sorted(lum(px[x, y]) for x, y in body); lo, hi = (ls[0], ls[-1]) if ls else (0, 1)
    for (x, y) in body:
        p = px[x, y]; t = (lum(p) - lo) / max(1, hi - lo)
        r = fn(x, y, w, h, p) if fn else ramp
        px[x, y] = r[min(len(r) - 1, max(0, round(t * (len(r) - 1) + .3)))] + (p[3],)
    return im


os.makedirs(out, exist_ok=True)
# body 24 x 12 (shoulders seen from above, front = top): the grey cowl over the shoulders, leather at the back,
# a red sash across
def body_ramp(x, y, w, h, p):
    if abs((y - h * 0.62) - (x - w / 2) * 0.3) < 0.7: return SASH
    return GREY if y < h * 0.55 else LEATHER
recolor("ork_archer_Body", None, body_ramp).save(os.path.join(out, "xbow_body.png"))
recolor("ork_archer_UpperArm", None, lambda x, y, w, h, p: GREY if y < h * 0.4 else LEATHER).save(os.path.join(out, "xbow_upperarm.png"))
recolor("ork_archer_ForeArm", LEATHER).save(os.path.join(out, "xbow_forearm.png"))
recolor("ork_archer_Hand", GLOVE).save(os.path.join(out, "xbow_hand.png"))
recolor("ork_archer_Leg", LEATHER).save(os.path.join(out, "xbow_leg.png"))
# the crossbow: its own colours, just a shade darker
cb = Image.open(os.path.join(src, "ork_archer_Crossbow.png")).convert("RGBA"); cp = cb.load()
for y in range(cb.height):
    for x in range(cb.width):
        p = cp[x, y]
        if p[3]: cp[x, y] = (int(p[0] * .8), int(p[1] * .75), int(p[2] * .75), p[3])
cb.save(os.path.join(out, "xbow_crossbow.png"))

# the head: the game's own hero hood (Head_2.1_hood, 24 x 24) recoloured neutral grey - the game's shape and the way
# heroes wear it; the mod shows it at ~0.55 tiles on the orc's body (44 px per unit)
recolor("Head_2.1_hood", GREY).save(os.path.join(out, "xbow_head.png"))

# the corpse: the orc's own, recoloured like the rest
dead = recolor("ork_archer_deadBody_0", None, lambda x, y, w, h, p: LEATHER if p[0] > p[2] + 10 else GREY)
dead.save(os.path.join(out, "xbow_dead.png"))

# the bestiary picture, from the parts with the prefab's offsets (32 px per unit, y up) and pivots
U = 32.0
A = lambda n: Image.open(os.path.join(out, n + ".png")).convert("RGBA")
C = Image.new("RGBA", (40, 40), (0, 0, 0, 0)); ox0, oy0 = 20, 22
def put(img, ox, oy, piv=(0.5, 0.5), sc=1.0):
    if sc != 1.0: img = img.resize((int(img.width * sc), int(img.height * sc)), Image.NEAREST)
    C.alpha_composite(img, (int(round(ox0 + ox - piv[0] * img.width)), int(round(oy0 - oy - (1 - piv[1]) * img.height))))
body = (0, -0.09 * U)
put(A("xbow_leg"), -0.16 * U, -0.02 * U); put(A("xbow_leg"), 0.16 * U, -0.02 * U)
ur = (body[0] + 0.22 * U, body[1]); fr = (ur[0] + 0.20 * U, ur[1] + 0.09 * U); hr = (fr[0] - 0.03 * U, fr[1] + 0.14 * U); bw = (hr[0] - 0.01 * U, hr[1] + 0.04 * U)
ul = (body[0] - 0.22 * U, body[1]); fl = (ul[0] - 0.20 * U, ul[1] + 0.09 * U); hl = (fl[0] + 0.10 * U, fl[1] + 0.19 * U)
put(A("xbow_crossbow"), *bw, sc=1.3)
for (h, f, u) in ((hl, fl, ul), (hr, fr, ur)):
    put(A("xbow_hand"), *h); put(A("xbow_forearm"), *f, piv=(0.667, 0.333)); put(A("xbow_upperarm"), *u, piv=(0.417, 0.333))
put(A("xbow_body"), *body)
put(A("xbow_head"), body[0], body[1] + 0.04 * U, piv=(0.5, 0.5), sc=U / 40.0)
C.save(os.path.join(out, "xbow_preview.png"))
print("crossbowman drawn (orc base)")
