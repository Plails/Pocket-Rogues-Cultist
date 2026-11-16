# The cultist's body parts = the game's goblin wizard's parts (its shapes kept exactly) recoloured: the hood and robe
# black with a red hem, the sleeves black with red cuffs, pale hands, black boots; the corpse likewise. Plus the
# cursed sphere he throws.  usage: python cultist_from_goblin.py <goblin parts dir> <out dir>
import sys, os, math, random
from PIL import Image

src, out = sys.argv[1], sys.argv[2]
CLOAK = [(8, 7, 10), (16, 14, 19), (25, 22, 29), (36, 32, 41), (50, 45, 56), (64, 58, 72)]
TRIM = [(74, 14, 20), (110, 22, 28), (146, 32, 34), (176, 52, 44)]
PALE = [(58, 54, 52), (88, 82, 78), (122, 114, 108), (154, 146, 138)]
lum = lambda p: .3 * p[0] + .59 * p[1] + .11 * p[2]


def recolor(name, outname, trim_where=None, ramp=CLOAK, skin_to=None):
    im = Image.open(os.path.join(src, name + ".png")).convert("RGBA")
    w, h = im.size
    px = im.load()
    body = [(x, y) for y in range(h) for x in range(w) if px[x, y][3] > 200 and lum(px[x, y]) > 22]
    if not body: im.save(os.path.join(out, outname + ".png")); return
    ls = sorted(lum(px[x, y]) for x, y in body); lo, hi = ls[0], ls[-1]
    solid = lambda x, y: 0 <= x < w and 0 <= y < h and px[x, y][3] > 200 and lum(px[x, y]) > 22
    clear = lambda x, y: not (0 <= x < w and 0 <= y < h) or px[x, y][3] < 100
    # the outer outline: dark pixels touching the outside (the inner dark lines of the drawing are not edges)
    outer = {(x, y) for y in range(h) for x in range(w) if px[x, y][3] > 200 and lum(px[x, y]) <= 22
             and any(clear(x + dx, y + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))}
    new = {}
    for (x, y) in body:
        p = px[x, y]
        t = (lum(p) - lo) / max(1, hi - lo)
        greenish = p[1] > p[0] + 12 and p[1] > p[2] + 8                       # the goblin's own skin
        r = skin_to if (skin_to and greenish) else ramp
        c = r[min(len(r) - 1, max(0, round(t * (len(r) - 1) + .3)))]
        edge = any(clear(x + dx, y + dy) or (x + dx, y + dy) in outer for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
        if trim_where and edge and not (skin_to and greenish) and trim_where(x, y, w, h):
            c = TRIM[min(3, max(0, round(t * 3 + .5)))]
        new[(x, y)] = c + (p[3],)
    for k, v in new.items(): px[k[0], k[1]] = v
    im.save(os.path.join(out, outname + ".png"))


os.makedirs(out, exist_ok=True)
lower = lambda x, y, w, h: y >= h * 0.5           # (pictures are stored top row first: the lower half = the back/hem)
recolor("goblinWiz1_parts_Body", "cult_body", lower)
recolor("goblinWiz1_parts_Head", "cult_head", lambda x, y, w, h: y <= h * 0.45)   # red trim round the hood's opening
recolor("goblinWiz1_parts_UpperArm", "cult_upperarm")
recolor("goblinWiz1_parts_ForeArm", "cult_forearm", lambda x, y, w, h: y <= h * 0.45)   # cuffs at the hand end (top)
recolor("goblinWiz1_parts_Hand", "cult_hand", None, PALE)
recolor("goblinBow_parts_leg", "cult_leg")
recolor("goblinWiz1_deadBody_0", "cult_dead", None, CLOAK, PALE)
recolor("goblinWiz1_preview", "cult_preview", lower, CLOAK, PALE)

# the cursed sphere: a black core in a ring of dark red and violet light (drawn without the game's lighting)
n = 16
im = Image.new("RGBA", (n, n), (0, 0, 0, 0)); px = im.load(); c = n / 2
for y in range(n):
    for x in range(n):
        d = math.hypot(x + .5 - c, y + .5 - c)
        a = math.atan2(y + .5 - c, x + .5 - c)
        if d > 7: continue
        if d > 5.6: col = (120, 20, 40, 150)
        elif d > 4.2: col = (200, 40, 50, 255) if math.sin(a * 3) > -.2 else (120, 30, 110, 255)
        elif d > 2.6: col = (70, 10, 30, 255)
        else: col = (12, 4, 10, 255)
        px[x, y] = col
px[int(c - 2), int(c - 3)] = (255, 120, 120, 255)
im.save(os.path.join(out, "cult_orb.png"))
print("cultist recoloured")
