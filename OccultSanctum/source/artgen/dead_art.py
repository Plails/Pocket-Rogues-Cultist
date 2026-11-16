# Individual corpses for the sanctum's monsters, made the game's way: each starts from the game's own corpse of the
# monster it is built on (the goblin wizard, the goblin assassin, the orc crossbowman - a body sprawled on the floor
# seen from above, with the game's outline and soft shadow), the base monster's weapons taken away, recoloured,
# and its own things laid on it:
#   cult_dead   - the black robe with its red hem, pale hands, the staff gone, his cursed sphere gone out beside him
#   devil_dead  - red all over, the hat turned into bone horns, the knives gone, the bat wings spread flat under him
#   xbow_dead   - the hooded human archer's corpse: grey hood and cloak, leather, a red sash, the crossbow beside him
#   spear_dead  - the same man (the spearman is dressed like the crossbowman), his spear by him
# usage: python dead_art.py <game corpses dir (goblin_ref)> <art dir (reads parts, writes corpses)>
import sys, os, math
from PIL import Image

ref, art = sys.argv[1], sys.argv[2]
lum = lambda p: .3 * p[0] + .59 * p[1] + .11 * p[2]
CLOAK = [(14, 12, 17), (24, 21, 28), (36, 32, 41), (50, 45, 56), (66, 60, 74), (84, 77, 92)]
TRIM = [(74, 14, 20), (110, 22, 28), (146, 32, 34), (176, 52, 44)]
PALE = [(58, 54, 52), (88, 82, 78), (122, 114, 108), (154, 146, 138)]
SKIN = [(46, 8, 10), (78, 14, 16), (112, 24, 22), (146, 38, 30), (178, 58, 40)]
HORN = [(40, 34, 28), (88, 78, 64), (150, 138, 116), (200, 190, 168)]
GREY = [(22, 22, 25), (36, 36, 40), (52, 51, 56), (70, 69, 74), (90, 88, 93), (112, 110, 114)]
LEATHER = [(20, 13, 9), (34, 23, 16), (50, 35, 24), (68, 48, 33), (88, 64, 44)]
SASH = [(70, 12, 14), (104, 20, 22), (140, 32, 30)]
HALO = (0, 0, 0, 60)


def pick(r, t): return r[min(len(r) - 1, max(0, round(t * (len(r) - 1) + .3)))]


def load_main(name, pad):
    """the game's corpse, only its biggest piece (the body: the dropped weapons are separate pieces), no shadow,
    on a canvas padded by pad = (left, top, right, bottom) so the body stays at the same place relative to the middle"""
    im = Image.open(os.path.join(ref, name + ".png")).convert("RGBA")
    w, h = im.size; px = im.load()
    seen, comps = set(), []
    for y in range(h):
        for x in range(w):
            if px[x, y][3] > 200 and (x, y) not in seen:
                st, c = [(x, y)], []; seen.add((x, y))
                while st:
                    a, b = st.pop(); c.append((a, b))
                    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                        q = (a + dx, b + dy)
                        if 0 <= q[0] < w and 0 <= q[1] < h and q not in seen and px[q][3] > 200: seen.add(q); st.append(q)
                comps.append(c)
    keep = set(max(comps, key=len))
    l, t, r, b = pad
    out = Image.new("RGBA", (w + l + r, h + t + b), (0, 0, 0, 0)); op = out.load()
    for (x, y) in keep: op[x + l, y + t] = px[x, y][:3] + (255,)
    return out


def recolor(im, choose):
    """choose(x, y, p, t, edge) -> a colour or None (keep); t = the pixel's brightness within the body, edge = on the
    outer rim (next to the outline)"""
    w, h = im.size; px = im.load()
    body = [(x, y) for y in range(h) for x in range(w) if px[x, y][3] > 200 and lum(px[x, y]) > 22]
    ls = sorted(lum(px[x, y]) for x, y in body); lo, hi = ls[0], ls[-1]
    clear = lambda x, y: not (0 <= x < w and 0 <= y < h) or px[x, y][3] < 100
    outer = {(x, y) for y in range(h) for x in range(w) if px[x, y][3] > 200 and lum(px[x, y]) <= 22
             and any(clear(x + dx, y + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))}
    new = {}
    for (x, y) in body:
        p = px[x, y]
        t = (lum(p) - lo) / max(1, hi - lo)
        edge = any(clear(x + dx, y + dy) or (x + dx, y + dy) in outer for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
        c = choose(x, y, p, t, edge)
        if c is not None: new[(x, y)] = c + (255,)
    for k, v in new.items(): px[k] = v


def part(name, scale=1.0, rot=0.0, dark=1.0):
    im = Image.open(os.path.join(art, name + ".png")).convert("RGBA")
    if scale != 1.0: im = im.resize((max(1, round(im.width * scale)), max(1, round(im.height * scale))), Image.NEAREST)
    if rot: im = im.rotate(rot, Image.NEAREST, expand=True)
    if dark != 1.0:
        px = im.load()
        for y in range(im.height):
            for x in range(im.width):
                p = px[x, y]
                if p[3]: px[x, y] = (int(p[0] * dark), int(p[1] * dark), int(p[2] * dark), p[3])
    return im


def under(im, layer, at):
    """lay a picture UNDER the body (wings on the floor beneath it)"""
    c = Image.new("RGBA", im.size, (0, 0, 0, 0)); c.alpha_composite(layer, at); c.alpha_composite(im); return c


def clear_disc(im, cx, cy, r):
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            if (x - cx) ** 2 + (y - cy) ** 2 <= r * r: px[x, y] = (0, 0, 0, 0)


def shadow(im):
    """the game's soft 1 px shadow round the whole corpse"""
    w, h = im.size; px = im.load()
    ring = [(x, y) for y in range(h) for x in range(w) if px[x, y][3] < 100 and any(
        0 <= x + dx < w and 0 <= y + dy < h and px[x + dx, y + dy][3] > 200 for dx in (-1, 0, 1) for dy in (-1, 0, 1))]
    for q in ring: px[q] = HALO
    return im


# ---- the cultist: the goblin wizard's corpse (a robed body), his staff taken away -------------------------------
cu = load_main("goblinWiz1_deadBody_0", (0, 0, 0, 0))
def cult(x, y, p, t, edge):
    if p[1] > p[0] + 12 and p[1] > p[2] + 8 and lum(p) > 60: return pick(PALE, t)      # the goblin's skin: pale hands, face
    if edge and y >= 24 and t > 0.25: return pick(TRIM, t * 0.8 + 0.2)                    # the red hem along the robe's skirt
    return pick(CLOAK, t)
recolor(cu, cult)
# his cursed sphere, gone out, rolled from his hand to where the staff lay
orb = part("cult_orb", 0.5, dark=0.5)
cu.alpha_composite(orb, (25, 7))
shadow(cu).save(os.path.join(art, "cult_dead.png"))

# ---- the devil: the goblin assassin's corpse, the knives gone, horns, wings flat on the floor --------------------
L, T = 8, 4
dv = load_main("goblinAssassin_deadBody_0", (L, T, L, 2 * T - T))           # (38 + 16) x (38 + 8): centre kept
def devil(x, y, p, t, edge):
    if y - T <= 6 and p[1] >= p[0] - 6 and p[1] > p[2] + 22: return pick(HORN, t)   # the hat's points -> bone horns
    return pick(SKIN, t)
recolor(dv, devil)
wl = part("devil_wing_l_0", 0.72, rot=-40, dark=0.62); wr = part("devil_wing_r_0", 0.72, rot=20, dark=0.62)   # one flung out, one crumpled
dv = under(dv, wl, (L + 17 - wl.width + 3, T + 13 - wl.height // 2))
dv = under(dv, wr, (L + 25 - 3, T + 12 - wr.height // 2 + 1))
shadow(dv).save(os.path.join(art, "devil_dead.png"))

# ---- the crossbowman: the game's hooded human archer's corpse (a man in a hood and cloak, sprawled on his back),
# his broken bow taken away; the hood and cloak deep grey as on the living one, leather, a red sash across the chest,
# and his big crossbow dropped beside him. (Shown at the archer corpse's own 42 px per unit.)
P = 4
Q = 10                                                                      # room at the sides for the crossbow
xb = load_main("Merc_Archer_DeadBody", (P + Q, P, P + Q, P))
def xbow(x, y, p, t, edge):
    x -= P + Q; y -= P
    if abs((y - 23) - (x - 29) * 0.55) < 1.1 and 20 <= x <= 37 and 16 <= y <= 31: return pick(SASH, t * 0.7 + 0.3)
    if p[1] > p[0] + 4 and p[1] >= p[2]: return pick(GREY, t * 0.82)                     # the green hood and cloak -> grey
    if p[0] > p[2] + 8: return pick(LEATHER, t)                                          # brown clothes -> dark leather
    return pick(GREY, t * 0.6)
recolor(xb, xbow)
# the spearman's corpse: the same man (he is dressed the same), his one spear lying by him
sp = part("spear_pike", 1.1, rot=152, dark=0.9)                      # its point out to the left
sb = under(xb.copy(), sp, (2, (xb.height - sp.height) // 2 + 6))
shadow(sb).save(os.path.join(art, "spear_dead.png"))
cb = part("xbow_crossbow", 1.25, rot=-100, dark=0.85)
xb = under(xb, cb, (1, P + 10))                                             # dropped beside him
shadow(xb).save(os.path.join(art, "xbow_dead.png"))

print("corpses drawn:", cu.size, dv.size, xb.size)
