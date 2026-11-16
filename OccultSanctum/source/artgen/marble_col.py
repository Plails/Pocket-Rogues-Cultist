# occ_column redrawn: a round column of dirty white marble seen from above - pale stone, grey veins, grime in the
# hollows, a hairline crack or two, a black 1 px outline.   usage: python marble_col.py <art dir>
import sys, os, math, random
from PIL import Image
out = sys.argv[1]; random.seed(11)
OL = (0, 0, 0, 255)
R = [(46, 43, 41), (84, 79, 73), (124, 118, 108), (158, 152, 141), (186, 181, 170), (208, 204, 193), (226, 223, 214), (238, 236, 229)]
def h2(x, y, s): return (math.sin(x * 12.9898 + y * 78.233 + s * 37.719) * 43758.5453) % 1.0
def vnoise(x, y, s):
    x0, y0 = math.floor(x), math.floor(y); fx, fy = x - x0, y - y0
    a, b, c, d = h2(x0, y0, s), h2(x0 + 1, y0, s), h2(x0, y0 + 1, s), h2(x0 + 1, y0 + 1, s)
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy
def col(i, x, y, tint=None, amt=0.0):
    i = max(0, min(len(R) - 1, i)); c = R[i]; d = (h2(x, y, 5) - .5) * 8
    c = tuple(max(0, min(255, int(v + d))) for v in c)
    if tint: c = tuple(int(v * (1 - amt) + t * amt) for v, t in zip(c, tint))
    return c + (255,)
S = 34; cx = cy = 17
im = Image.new("RGBA", (S, S), (0, 0, 0, 0)); px = im.load()
GRIME = (96, 84, 66)
for y in range(S):
    for x in range(S):
        r = math.hypot(x + .5 - cx, y + .5 - cy)
        if r > 14.5: continue
        # light from the upper left: a rounded top with a lit side and a shaded side
        lx, ly = (x + .5 - cx) / 14.5, (y + .5 - cy) / 14.5
        lit = 0.62 - 0.38 * (lx + ly) / 1.41 - 0.25 * (r / 14.5) ** 3
        i = int(round(lit * 7))
        if r > 13.3: i = min(i, 2)                         # the rim
        elif 9.2 < r < 10.6: i -= 2                        # the groove round the top
        elif r <= 9.2: i += 1                              # the round top, a shade lighter
        v = math.sin(x * .55 + y * .23 + math.sin(y * .31 + 2) * 2.4) + math.sin(x * .17 - y * .44 + 1.3) * .8
        tint, amt = None, 0.0
        if abs(v) < .1: i -= 1; tint, amt = (120, 126, 134), .25          # grey veins
        n = vnoise(x / 5, y / 5, 3)
        if n > .66: tint, amt = GRIME, .22 + (n - .66)                   # dirt
        if r > 11.5 and (lx + ly) > .3: tint, amt = GRIME, max(amt, .2)  # grime on the shaded rim
        px[x, y] = col(i, x, y, tint, amt)
# a couple of hairline cracks, not many
for (sx, sy, ang, ln) in ((23, 9, 2.2, 7), (9, 22, -.5, 5)):
    x, y = sx, sy
    for k in range(ln):
        xi, yi = int(x), int(y)
        if px[xi, yi][3]:
            px[xi, yi] = col(1, xi, yi)
            if px[xi + 1, yi][3] and math.hypot(xi + 1.5 - cx, yi + .5 - cy) < 13.3: px[xi + 1, yi] = col(6, xi + 1, yi)
        ang += random.uniform(-.6, .6); x += math.cos(ang); y += math.sin(ang)
# outline
ring = [(x, y) for y in range(S) for x in range(S) if px[x, y][3] == 0 and any(0 <= x + dx < S and 0 <= y + dy < S and px[x + dx, y + dy][3] > 0 and px[x + dx, y + dy] != OL for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
for q in ring: px[q] = OL
im.save(os.path.join(out, "occ_column.png"))
im.resize((S * 8, S * 8), Image.NEAREST).save(os.path.join(out, "..", "column_preview.png") if False else os.path.join(os.path.dirname(os.path.abspath(__file__)), "column_preview.png"))
print("column")
