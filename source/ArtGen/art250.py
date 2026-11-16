# Cultist 2.5.0 art (MRX's list):
#  * the flails hang on CHAINS: each flail's picture (in the hand and its icon) is cut into its head and its handle;
#    the handle is made short, a chain is laid between it and the head, so the head swings further out; the fist holds
#    the handle near its end (its end just shows out of the fist) - written to <id>_equip.grip (pixels from bottom left)
#  * the Thorned Morningstar gets its own head (no longer like the Devastator): a bronze ball wound in green brambles
#  * three skill icons redrawn after MRX's references: Poison Lash (a cat-o'-nine-tails, green poison on the tails),
#    Blood Rain (falling blood drops with streaks), Quick Snap (a coiled bullwhip with its whole handle)
# The 2.4 pictures stay in art/weapons_v1 and art/icons_v5; this reads those and writes art/weapons, art/icons.
# usage: python art250.py <art dir>
import sys, os, math, shutil
from PIL import Image
art = sys.argv[1]
W0, I0 = os.path.join(art, "weapons_v1"), os.path.join(art, "icons_v5")
for src, dst in ((os.path.join(art, "weapons"), W0), (os.path.join(art, "icons"), I0)):
    if not os.path.isdir(dst): shutil.copytree(src, dst)
OL = (0, 0, 0, 255)
def h2(x, y, s): return (math.sin(x * 12.9898 + y * 78.233 + s * 37.719) * 43758.5453) % 1.0
def outline(im, only_new=None):
    w, h = im.size; px = im.load()
    ring = [(x, y) for y in range(h) for x in range(w) if px[x, y][3] == 0 and any(0 <= x + dx < w and 0 <= y + dy < h and px[x + dx, y + dy][3] > 0 and px[x + dx, y + dy][:3] != (0, 0, 0) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for q in ring: px[q] = OL

IRON = [(46, 46, 54), (86, 86, 98), (132, 132, 146), (176, 176, 188)]

def split(im):
    """head = the wide part at the top left; handle = the thin stick to the bottom right (by the anti-diagonals x+y)"""
    w, h = im.size; px = im.load()
    pts = [(x, y) for y in range(h) for x in range(w) if px[x, y][3] > 0]
    width = {}
    for x, y in pts: width[x + y] = width.get(x + y, 0) + 1
    smax = max(width); smin = min(width)
    big = max(range(smin, smax + 1), key=lambda s: width.get(s, 0))
    hw = max(width.get(s, 0) for s in range(smax - 9, smax + 1))               # how thick the handle is
    B = smax
    for s in range(big, smax + 1):
        if all(width.get(s + k, 0) <= hw for k in range(4)): B = s; break
    head = [(x, y) for x, y in pts if x + y < B]
    handle = [(x, y) for x, y in pts if x + y >= B]
    return head, handle, B, smax

def chained(name, src, canvas, keep_handle, chain_len, new_head=None, pad=1, ring=True):
    im = Image.open(src).convert("RGBA"); px = im.load()
    head, handle, B, smax = split(im)
    # the handle: only its last part (towards its end at the bottom right)
    hs = smax - 2 * keep_handle
    handle = [(x, y) for x, y in handle if x + y >= hs]
    end = max(handle, key=lambda q: (q[0] + q[1], -abs(q[0] - q[1])))
    top_c = [q for q in handle if q[0] + q[1] <= hs + 1]
    hx = round(sum(q[0] for q in top_c) / len(top_c)); hy = round(sum(q[1] for q in top_c) / len(top_c))
    # the head's lowest point on the diagonal and its centre line
    if new_head is not None:
        hd_img = new_head; hpx = hd_img.load()
        head_pts = [(x, y) for y in range(hd_img.height) for x in range(hd_img.width) if hpx[x, y][3] > 0]
        get = lambda x, y: hpx[x, y]
    else:
        head_pts = head; get = lambda x, y: px[x, y]
    hb = max(head_pts, key=lambda q: q[0] + q[1])
    out = Image.new("RGBA", (canvas, canvas), (0, 0, 0, 0)); op = out.load()
    ox, oy = canvas - 1 - pad - end[0], canvas - 1 - pad - end[1]
    for x, y in handle: op[x + ox, y + oy] = px[x, y]
    # the chain from the top of the handle up-left, then the head hangs at its end
    cx, cy = hx + ox, hy + oy
    for i in range(1, chain_len + 1, 2):                                       # round links, one after another
        x, y = cx - i, cy - i
        for (ddx, ddy, c) in ((0, -1, IRON[3]), (-1, 0, IRON[2]), (1, 0, IRON[1]), (0, 1, IRON[0])):
            X, Y = x + ddx, y + ddy
            if 0 <= X < canvas and 0 <= Y < canvas and op[X, Y][3] == 0: op[X, Y] = c + (255,)
    tx, ty = cx - chain_len - 1, cy - chain_len - 1                            # where the head is hung
    dx, dy = tx - hb[0], ty - hb[1]
    mnx = min(q[0] for q in head_pts) + dx; mny = min(q[1] for q in head_pts) + dy
    if mnx < 1 or mny < 1: s = max(1 - mnx, 1 - mny); dx += s; dy += s          # keep it on the picture (a shorter chain)
    for x, y in head_pts:
        X, Y = x + dx, y + dy
        if 0 <= X < canvas and 0 <= Y < canvas: op[X, Y] = get(x, y)
    if ring: outline(out)                                                      # (the pictures in the hand have no black outline)
    return out, (end[0] + ox, end[1] + oy)

def grip_of(end, canvas, back=2.0):
    """the fist a little up the handle from its end (Unity pivot: pixels from the bottom left)"""
    gx, gy = end[0] - back, end[1] - back
    return (gx + 0.5, canvas - (gy + 0.5))

# ---------------------------------------------------------------- the Thorned Morningstar's own head
def thorn_head(r):
    n = int(r * 2 + 7); c = n / 2 - 0.5
    im = Image.new("RGBA", (n, n), (0, 0, 0, 0)); px = im.load()
    BR = [(78, 46, 20), (122, 76, 34), (168, 112, 52), (206, 154, 84)]
    VINE = [(22, 52, 18), (40, 86, 28), (70, 124, 42)]
    for y in range(n):
        for x in range(n):
            d = math.hypot(x - c, y - c)
            if d <= r:
                l = 0.6 - 0.35 * ((x - c) + (y - c)) / (r * 1.4) - 0.2 * (d / r) ** 2
                px[x, y] = BR[max(0, min(3, int(l * 4)))] + (255,)
    # a bramble wound round it: a ring of dark green with thorns sticking out
    for k in range(int(r * 9)):
        a = k / (r * 9) * math.pi * 2
        rr = r * (0.75 + 0.18 * math.sin(a * 3))
        x, y = round(c + math.cos(a) * rr), round(c + math.sin(a) * rr)
        px[x, y] = VINE[1 + (k % 2)] + (255,)
    for k in range(7):                                                           # thorns: short, pale-tipped, all round
        a = k / 7 * math.pi * 2 + 0.3
        for j in range(2):
            x, y = round(c + math.cos(a) * (r + 1 + j)), round(c + math.sin(a) * (r + 1 + j))
            if 0 <= x < n and 0 <= y < n: px[x, y] = (VINE[2] if j == 0 else (200, 190, 140)) + (255,)
    px[round(c - r * 0.35), round(c - r * 0.35)] = BR[3] + (255,)
    return im

# ---------------------------------------------------------------- the flails (doomBell / martyrChain are gone; the scourge is a whip)
os.makedirs(os.path.join(art, "weapons"), exist_ok=True)
for fid in ("rustyFlail", "pilgrimFlail", "penitentChain", "inquisitorFlail", "thornStar", "devastator"):
    nh_e = thorn_head(4.5) if fid == "thornStar" else None
    nh_i = thorn_head(3.8) if fid == "thornStar" else None
    eq, end = chained(fid, os.path.join(W0, fid + "_equip.png"), 44, 6, 10, nh_e, ring=False)
    eq.save(os.path.join(art, "weapons", fid + "_equip.png"))
    g = grip_of(end, 44)
    open(os.path.join(art, "weapons", fid + "_equip.grip"), "w").write("%.1f,%.1f" % g)
    ic, _ = chained(fid, os.path.join(W0, fid + "_icon.png"), 24, 4, 6, nh_i, pad=1)
    ic.save(os.path.join(art, "weapons", fid + "_icon.png"))
# the scourge stays as it is; it too is held near the end of its handle
sc = Image.open(os.path.join(W0, "ritualScourge_equip.png")).convert("RGBA")
_, hd, _, _ = split(sc)
end = max(hd, key=lambda q: q[0] + q[1])
open(os.path.join(art, "weapons", "ritualScourge_equip.grip"), "w").write("%.1f,%.1f" % grip_of(end, sc.width))

# ---------------------------------------------------------------- skill icons (34 x 34, the game frames them)
def canvas34(): im = Image.new("RGBA", (34, 34), (0, 0, 0, 0)); return im, im.load()
def put(px, x, y, c):
    x, y = int(round(x)), int(round(y))
    if 0 <= x < 34 and 0 <= y < 34: px[x, y] = c + (255,) if len(c) == 3 else c
LEATHER = [(40, 24, 14), (70, 44, 24), (104, 68, 38), (140, 96, 56)]
TAN = [(90, 58, 30), (140, 96, 52), (186, 136, 78), (220, 178, 112)]
GREEN = [(30, 90, 24), (64, 160, 44), (130, 230, 80), (210, 255, 170)]

# Poison Lash: a cat-o'-nine-tails - a braided handle with a loop at the bottom left, seven thin tails fanning out to
# the upper right, each with a knot and a green drop of poison at its end
im, px = canvas34()
for i in range(9):                                                              # the braided handle
    x, y = 6 + i, 27 - i
    put(px, x, y, LEATHER[2 + (i % 2)]); put(px, x + 1, y, LEATHER[1]); put(px, x, y - 1, LEATHER[3] if i % 3 == 0 else LEATHER[2])
for a in range(0, 360, 30):                                                     # the loop at its end
    put(px, 4.5 + math.cos(math.radians(a)) * 2, 29.5 + math.sin(math.radians(a)) * 2, LEATHER[2])
for k in range(5):                                                              # the tails, a little wavy, apart
    ang = math.radians(-80 + k * 18)
    bx, by = 15, 18
    L = 14 + (2 - abs(k - 2))
    for t in range(L):
        f = t / L
        w = math.sin(f * math.pi * 1.6 + k) * 1.2 * f
        x = bx + math.cos(ang) * t * 1.0 - math.sin(ang) * w
        y = by + math.sin(ang) * t * 1.0 + math.cos(ang) * w
        put(px, x, y, LEATHER[1] if t % 3 else LEATHER[3])
        if t == L - 1:
            put(px, x, y, LEATHER[3])
            for (ddx, ddy, cc) in ((1, 0, GREEN[1]), (1, 1, GREEN[2]), (2, 1, GREEN[1]), (1, 2, GREEN[0])): put(px, x + ddx, y + ddy, cc)   # the poison
            put(px, x + 1, y + 1, GREEN[3])
for (x, y) in ((24, 9), (28, 15), (21, 5), (30, 21)): put(px, x, y, GREEN[2])  # drops flying off
outline(im); im.save(os.path.join(art, "icons", "poisonLash.png"))

# Blood Rain: three long blood drops falling (pointed at both ends, like the reference), thin streaks over them
im, px = canvas34()
RED = [(70, 4, 8), (140, 10, 14), (210, 24, 24), (255, 96, 86)]
def drop(cx, cy, hlen, hw):
    for y in range(-hlen, hlen + 1):
        f = 1 - abs(y) / (hlen + 0.5)
        half = hw * (f ** 0.9 if y > 0 else f ** 1.6)
        for x in range(-3, 4):
            if abs(x) <= half:
                c = RED[3] if (x <= -half + 1 and y < 0) else RED[2] if x <= 0 else RED[1]
                put(px, cx + x, cy + y, c)
for (cx, top, bot) in ((9, 2, 15), (22, 0, 10), (17, 12, 22)):              # streaks behind
    for y in range(top, bot): put(px, cx, y, RED[0] if y % 3 else RED[1])
for (cx, cy, hl, hw) in ((9, 21, 6, 2.6), (22, 16, 6, 2.6), (17, 27, 4, 2.0)): drop(cx, cy, hl, hw)
for (x, y) in ((4, 8), (28, 25), (13, 3), (27, 4)): put(px, x, y, RED[1])      # specks
outline(im); im.save(os.path.join(art, "icons", "bloodRain.png"))

# Quick Snap: a bullwhip coiled up - its whole handle (a knob at the end, braided, a knot where the lash begins),
# two loops of the lash and its thin end with a little crack
im, px = canvas34()
for i in range(11):                                                             # the handle, all of it
    x, y = 4 + i, 29 - i
    put(px, x, y, TAN[2] if i % 2 else TAN[1]); put(px, x + 1, y, TAN[1]); put(px, x, y - 1, TAN[3] if i % 2 == 0 else TAN[2])
for (ddx, ddy) in ((-1, 0), (0, 1), (-1, 1), (0, 0)): put(px, 3 + ddx, 30 + ddy, TAN[3])   # the knob at its end
put(px, 15, 18, (230, 220, 200)); put(px, 16, 18, (200, 190, 170)); put(px, 15, 17, (230, 220, 200))   # the knot (white cord)
for loop, (cx, cy, rx, ry) in enumerate(((20, 14, 9, 7), (19, 15, 6.5, 5))):    # two loops of the lash
    for k in range(80):
        a = k / 80 * math.pi * 2
        put(px, cx + math.cos(a) * rx, cy + math.sin(a) * ry, TAN[1] if (k + loop) % 4 else TAN[2])
for t in range(9):                                                              # the thin end, out to the right
    put(px, 29 + t * 0.4, 14 + t * 0.9, (60, 36, 20))
put(px, 31, 23, (255, 250, 210)); put(px, 32, 22, (255, 230, 150)); put(px, 30, 24, (255, 230, 150))   # the crack
outline(im); im.save(os.path.join(art, "icons", "quickSnap.png"))

# preview sheet
names = [("weapons", f + s) for f in ("rustyFlail", "pilgrimFlail", "penitentChain", "inquisitorFlail", "thornStar", "devastator") for s in ("_equip", "_icon")] + [("icons", n) for n in ("poisonLash", "bloodRain", "quickSnap")]
ims = [Image.open(os.path.join(art, a, n + ".png")) for a, n in names]
sh = Image.new("RGBA", (sum(i.width * 5 + 8 for i in ims), max(i.height * 5 for i in ims)), (60, 44, 36, 255)); x = 0
for i in ims: j = i.resize((i.width * 5, i.height * 5), Image.NEAREST); sh.alpha_composite(j, (x, 0)); x += j.width + 8
sh.save(os.path.join(os.path.dirname(art), "preview", "art250.png"))
print("art 2.5.0 done")
