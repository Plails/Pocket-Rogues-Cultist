# The cult's spearman: dressed exactly like the crossbowman (the same grey hood, leathers and red sash - his parts
# xbow_*), only with a spear instead of the crossbow. The spear = the game's orc pikeman's pike (48 x 12, its point
# to the right) recoloured: a dark shaft, a blackened iron point, a strip of red cloth tied under the point.
# Plus his bestiary picture and his corpse (the crossbowman's corpse with the spear lying by him instead).
# usage: python spear_art.py <game sprites dir (goblin_ref)> <art dir (reads xbow_*, writes spear_*)>
import sys, os
from PIL import Image

ref, art = sys.argv[1], sys.argv[2]
lum = lambda p: .3 * p[0] + .59 * p[1] + .11 * p[2]
SHAFT = [(16, 10, 8), (28, 18, 12), (42, 28, 18), (58, 40, 26)]
IRON = [(20, 20, 26), (40, 40, 48), (66, 66, 76), (100, 100, 112), (140, 138, 150)]
RED = [(74, 14, 20), (110, 22, 28), (146, 32, 34)]
pick = lambda r, t: r[min(len(r) - 1, max(0, round(t * (len(r) - 1) + .3)))]

sp = Image.open(os.path.join(ref, "ork_pikeman_Pike.png")).convert("RGBA")
w, h = sp.size; px = sp.load()
for y in range(h):
    for x in range(w):
        p = px[x, y]
        if p[3] < 200 or lum(p) <= 16: continue
        grey = abs(p[0] - p[1]) < 14 and abs(p[1] - p[2]) < 14
        t = min(1, lum(p) / 140)
        px[x, y] = pick(IRON if grey else SHAFT, t) + (p[3],)
# a strip of red cloth tied round the shaft just below the point, its ends hanging off to one side
for (x, y, k) in ((35, 5, 1), (35, 6, 2), (35, 7, 1), (34, 8, 1), (33, 9, 0), (36, 8, 2), (36, 9, 1)):
    px[x, y] = RED[k] + (255,)
for (x, y) in ((34, 9), (32, 9), (33, 10), (37, 9), (36, 10)):
    if px[x, y][3] < 200: px[x, y] = (0, 0, 0, 255)
sp.save(os.path.join(art, "spear_pike.png"))

A = lambda n: Image.open(os.path.join(art, n + ".png")).convert("RGBA")

# the bestiary picture: the crossbowman's figure (same parts, same offsets as in xbow_art.py), the spear held upright
U = 32.0
C = Image.new("RGBA", (40, 56), (0, 0, 0, 0)); ox0, oy0 = 20, 30
def put(img, ox, oy, piv=(0.5, 0.5), sc=1.0):
    if sc != 1.0: img = img.resize((int(img.width * sc), int(img.height * sc)), Image.NEAREST)
    C.alpha_composite(img, (int(round(ox0 + ox - piv[0] * img.width)), int(round(oy0 - oy - (1 - piv[1]) * img.height))))
body = (0, -0.09 * U)
put(A("xbow_leg"), -0.16 * U, -0.02 * U); put(A("xbow_leg"), 0.16 * U, -0.02 * U)
ur = (body[0] + 0.22 * U, body[1]); fr = (ur[0] + 0.20 * U, ur[1] + 0.09 * U); hr = (fr[0] - 0.03 * U, fr[1] + 0.14 * U)
ul = (body[0] - 0.22 * U, body[1]); fl = (ul[0] - 0.20 * U, ul[1] + 0.09 * U); hl = (fl[0] + 0.10 * U, fl[1] + 0.19 * U)
put(A("spear_pike").rotate(90, Image.NEAREST, expand=True), hr[0] + 1, hr[1] + 6)     # upright, point up, in the right hand
for (hh, f, u) in ((hl, fl, ul), (hr, fr, ur)):
    put(A("xbow_hand"), *hh); put(A("xbow_forearm"), *f, piv=(0.667, 0.333)); put(A("xbow_upperarm"), *u, piv=(0.417, 0.333))
put(A("xbow_body"), *body)
put(A("xbow_head"), body[0], body[1] + 0.04 * U, piv=(0.5, 0.5), sc=U / 40.0)
C.save(os.path.join(art, "spear_preview.png"))

# (his corpse is drawn in dead_art.py, from the crossbowman's: run this first)
print("spear drawn: pike", sp.size, "preview", C.size)
