# The cult's general drawn anew at the rig's OWN size (the skeleton axeman's two-handed rig, 42 px a unit): his pixels
# as big as the soldiers' and the game's champions' (the guardian's, the goblin champion's) - he is bigger, so there is
# more of him drawn, not finer pixels (the user). The game's way of drawing: flat colours, two or three tones a
# material lit from the upper left, a dark outline. Black robe, grey tabard, steel gorget, domed steel pauldrons with
# pale spikes, a pale bald scarred head, sleeves with steel bracers, iron gauntlets, black boots, a spiked morningstar.
# Seen from above: his FRONT is the TOP of each picture.
#   usage: python general_1x_art.py <out dir> <dir with Merc_Berserk_Parts_UpperArm/ForeArm/Hand.png>
import sys, os, math
from PIL import Image
out, ref = sys.argv[1], sys.argv[2]; os.makedirs(out, exist_ok=True)
OL = (10, 8, 12, 255)
ROBE = [(20, 18, 24), (34, 30, 40), (52, 46, 60)]
TAB = [(86, 86, 92), (114, 114, 120), (146, 146, 152)]
STEEL = [(58, 58, 70), (96, 96, 110), (144, 144, 160), (190, 190, 204)]
BONE = [(170, 160, 138), (226, 218, 196)]
SKIN = [(132, 116, 108), (176, 162, 150), (212, 202, 190)]
WOOD = [(52, 34, 22), (84, 56, 36)]
LEATHER = [(70, 36, 24), (104, 56, 36)]
def new(w, h): im = Image.new("RGBA", (w, h), (0, 0, 0, 0)); return im, im.load()
def tone(r, ex, ey):
    s = ex * 0.6 + ey * 0.8
    return r[min(len(r) - 1, 2)] if s < -0.4 else r[0] if s > 0.45 else r[1]
def ell(p, w, h, cx, cy, rx, ry, fn):
    for y in range(h):
        for x in range(w):
            ex, ey = (x - cx) / rx, (y - cy) / ry
            if ex * ex + ey * ey <= 1.0: c = fn(x, y, ex, ey); p[x, y] = c + (255,)
def put(p, w, h, x, y, c):
    x, y = int(round(x)), int(round(y))
    if 0 <= x < w and 0 <= y < h: p[x, y] = c + (255,)
def outline(im):
    w, h = im.size; p = im.load()
    ring = [(x, y) for y in range(h) for x in range(w) if p[x, y][3] == 0 and any(0 <= x + a < w and 0 <= y + b < h and p[x + a, y + b][3] == 255 and p[x + a, y + b] != OL for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for q in ring: p[q] = OL

# ---------------------------------------------------------------- body 32x16 (the rig's 24x12 in the middle)
W, H = 32, 16
im, p = new(W, H); c = W / 2 - .5
ell(p, W, H, c, 8.6, 9.6, 5.0, lambda x, y, ex, ey: tone(ROBE, ex, ey))                     # the robe over back and shoulders
ell(p, W, H, c, 11.2, 3.4, 1.8, lambda x, y, ex, ey: ROBE[0])                                 # the hood thrown back
for y in range(3, 9):                                                                         # the grey tabard down the front
    for x in range(int(c - 1.5), int(c + 2.5)): p[x, y] = (TAB[2] if x < c - 0.5 else TAB[1] if x < c + 1.5 else TAB[0]) + (255,)
ell(p, W, H, c, 6.6, 3.4, 2.4, lambda x, y, ex, ey: (STEEL[2] if ey < 0 else STEEL[1]) if ex * ex + ey * ey > 0.45 else (p[x, y][:3]))   # the steel gorget
for side in (-1, 1):                                                                          # the pauldrons
    sx, sy = c + side * 8.4, 7.6
    ell(p, W, H, sx, sy, 4.0, 3.6, lambda x, y, ex, ey: STEEL[3] if ex * ex + ey * ey > 0.7 and ey < -0.2 else tone(STEEL[:3], ex * -side * 0.3 + ex * 0.7, ey))
    put(p, W, H, sx + side * 1.5, sy, STEEL[3])                                               # a rivet
    for ang, n in ((-35, 3), (15, 3)):                                                        # pale spikes, out to the sides
        a = math.radians(ang if side > 0 else 180 - ang)
        for k in range(n): put(p, W, H, sx + side * 3.8 + math.cos(a) * k, sy + ang / 30.0 + math.sin(a) * k, BONE[1] if k == n - 1 else BONE[0])
    put(p, W, H, sx - side * 0.5, sy - 3.8, BONE[1]); put(p, W, H, sx - side * 0.5, sy - 4.6, BONE[1])   # one spike up
outline(im); im.save(os.path.join(out, "gen_body.png"))

# ---------------------------------------------------------------- head 12x12: pale, bald, scarred
W = H = 12
im, p = new(W, H)
ell(p, W, H, 5.5, 5.4, 3.7, 4.2, lambda x, y, ex, ey: tone(SKIN, ex, ey))
for x in (3, 4, 5, 6, 7, 8): put(p, W, H, x, 1.6, SKIN[0]) if 2 < x < 9 else None            # the heavy brow at the front
put(p, W, H, 6, 3, (128, 74, 70)); put(p, W, H, 7, 4, (128, 74, 70)); put(p, W, H, 7, 5, (128, 74, 70))   # the scar
put(p, W, H, 1.6, 5, SKIN[1]); put(p, W, H, 9.4, 5, SKIN[0])                                   # ears
outline(im); im.save(os.path.join(out, "gen_head.png"))

# ---------------------------------------------------------------- arms: the game's own arm shapes for this rig
def shaped(name, fill, outname):
    m = Image.open(os.path.join(ref, name + ".png")).convert("RGBA"); w, h = m.size; mp = m.load()
    im, p = new(w, h)
    xs = [x for y in range(h) for x in range(w) if mp[x, y][3] > 0]; ys = [y for y in range(h) for x in range(w) if mp[x, y][3] > 0]
    cx, cy = (min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2; rx, ry = max(1, (max(xs) - min(xs)) / 2), max(1, (max(ys) - min(ys)) / 2)
    for y in range(h):
        for x in range(w):
            if mp[x, y][3] > 0: p[x, y] = fill(x, y, (x - cx) / rx, (y - cy) / ry) + (255,)
    outline(im); im.save(os.path.join(out, outname + ".png"))
shaped("Merc_Berserk_Parts_UpperArm", lambda x, y, ex, ey: tone(ROBE, ex, ey), "gen_upperarm")
shaped("Merc_Berserk_Parts_ForeArm", lambda x, y, ex, ey: tone(STEEL[:3], ex, 0) if ey < -0.1 else tone(ROBE, ex, 0), "gen_forearm")   # the bracer at the wrist
shaped("Merc_Berserk_Parts_Hand", lambda x, y, ex, ey: tone(STEEL[:3], ex, ey), "gen_hand")

# ---------------------------------------------------------------- legs 12x12: black boots, a steel toe
W = H = 12
im, p = new(W, H)
ell(p, W, H, 6.0, 6.0, 2.8, 3.8, lambda x, y, ex, ey: STEEL[1] if ey < -0.55 else tone(ROBE, ex, ey))
outline(im); im.save(os.path.join(out, "gen_leg.png"))

# ---------------------------------------------------------------- the morningstar 48x23, held where the rig holds
# its two-handed weapon (the grip at (0.6875, 0.3913) of the picture), the head at the far (left) end
W, H = 48, 23
im, p = new(W, H)
gy = round(H - 1 - 0.3913 * H); gx = round(0.6875 * W)
for x in range(13, 46):
    wrap = gx - 3 <= x <= gx + 3 or gx - 17 <= x <= gx - 12
    put(p, W, H, x, gy, LEATHER[1] if wrap and x % 2 else LEATHER[0] if wrap else WOOD[1])
    put(p, W, H, x, gy + 1, LEATHER[0] if wrap else WOOD[0])
for y in (gy - 1, gy, gy + 1, gy + 2): put(p, W, H, 46, y, STEEL[1] if y < gy + 1 else STEEL[0])        # the pommel
# the head, finer (the user: the iron ball and its white blades prettier, the haft as it was): eight spikes, each a
# proper blade - two pixels at its root, one at its point, lit on one side, the points bright - behind a forged iron
# ball with a dark band and rivets round it, a bright glint, on a ribbed steel collar
bx, by = 7.0, gy + 0.5
SP = [(150, 140, 120), (196, 188, 166), (238, 232, 214)]
for a in range(0, 360, 45):
    r = math.radians(a + 22.5); ux, uy = math.cos(r), math.sin(r); vx, vy = -uy, ux
    for k10 in range(34, 70):
        k = k10 / 10.0
        wdt = 0.5 if k < 4.6 else 0.0
        for o in ((-wdt, 0.0, wdt) if wdt else (0.0,)):
            x, y = bx + ux * k + vx * o, by + uy * k + vy * o
            lit = o * (vx * -0.6 + vy * -0.8) >= 0
            col = SP[2] if k > 6.0 else SP[1] if lit else SP[0]
            put(p, W, H, x, y, col)
ell(p, W, H, bx, by, 3.9, 3.9, lambda x, y, ex, ey: tone(STEEL[:3], ex, ey) if ex * ex + ey * ey < 0.8 else STEEL[0])   # the ball, a dark rim
for a in range(0, 360, 20):                                                                                  # the band round it
    put(p, W, H, bx + math.cos(math.radians(a)) * 2.6, by + math.sin(math.radians(a)) * 2.6, STEEL[0])
for a in (45, 135, 225, 315): put(p, W, H, bx + math.cos(math.radians(a)) * 2.6, by + math.sin(math.radians(a)) * 2.6, STEEL[3])   # rivets
put(p, W, H, bx - 2, by - 2, STEEL[3]); put(p, W, H, bx - 1, by - 2, STEEL[3]); put(p, W, H, bx - 2, by - 1, STEEL[2])          # the glint
for x in (11, 12, 13):                                                                                       # the ribbed collar
    for y in (gy - 1, gy, gy + 1, gy + 2): put(p, W, H, x, y, STEEL[3] if y == gy - 1 else STEEL[1] if x == 12 else STEEL[2] if y < gy + 1 else STEEL[0])
outline(im); im.save(os.path.join(out, "gen_mace.png"))

names = ["gen_body", "gen_head", "gen_upperarm", "gen_forearm", "gen_hand", "gen_leg", "gen_mace"]
ims = [Image.open(os.path.join(out, n + ".png")).convert("RGBA") for n in names]
sh = Image.new("RGBA", (sum(i.width + 4 for i in ims), max(i.height for i in ims)), (90, 80, 80, 255)); x = 0
for i in ims: sh.alpha_composite(i, (x, 0)); x += i.width + 4
sh.resize((sh.width * 8, sh.height * 8), Image.NEAREST).save(os.path.join(out, "gen_sheet.png"))
print("general 1x")
