# The bookcase seen from above, as the game sees everything (the user: we look down on the TOP of a tall cupboard,
# the cultists stand before its front like before any bookcase): its thick wooden top with a carved cornice all round,
# lengthwise planks, darker corner posts, and on top what one leaves on a bookcase - a stack of books, a candle burning,
# a skull, a rolled scroll. Drawn like the sanctum's candles and candle stands: a dark outline and a soft dark rim.
# Its back is the top of the picture (it stands with its back to the wall), its front the bottom. 52x18 like before.
#   usage: python shelf_art.py <out dir>
import sys, os, math
from PIL import Image
out = sys.argv[1]; os.makedirs(out, exist_ok=True)
OL = (16, 10, 8, 255)
WOOD = [(48, 30, 20), (68, 44, 28), (92, 60, 38), (118, 80, 50), (142, 100, 64)]
BOOK = [((120, 26, 30), (84, 16, 20)), ((40, 64, 110), (26, 42, 76)), ((48, 92, 52), (30, 62, 34))]
PAGE = (214, 204, 176)
BONE = [(150, 140, 120), (196, 188, 166), (228, 222, 204)]
WAX = [(186, 176, 150), (224, 216, 194)]
W, H = 52, 18
im = Image.new("RGBA", (W, H), (0, 0, 0, 0)); p = im.load()
def put(x, y, c):
    if 0 <= x < W and 0 <= y < H: p[x, y] = c + (255,) if len(c) == 3 else c
def h2(x, y, s): return (math.sin(x * 12.9898 + y * 78.233 + s * 37.719) * 43758.5453) % 1.0
x0, x1, y0, y1 = 1, W - 2, 1, H - 2
# the top board: the cornice round the edge (lit on the back and left, shaded at the front and right), planks inside
for y in range(y0, y1 + 1):
    for x in range(x0, x1 + 1):
        edge = min(x - x0, y - y0, x1 - x, y1 - y)
        if edge == 0: c = WOOD[4] if (y == y0 or x == x0) and not (y == y1 or x == x1) else WOOD[0]
        elif edge == 1: c = WOOD[3] if (y == y0 + 1 or x == x0 + 1) else WOOD[1]
        else:
            c = WOOD[2]
            if y in (6, 11): c = WOOD[1]                                      # the seams between the planks
            elif h2(x, y, 1) < 0.08: c = WOOD[1]                              # grain
            elif h2(x, y, 2) < 0.05: c = WOOD[3]
        put(x, y, c)
for (cx, cy) in ((x0 + 2, y0 + 2), (x1 - 2, y0 + 2), (x0 + 2, y1 - 2), (x1 - 2, y1 - 2)):   # the corner posts' tops
    for dx in (0, 1):
        for dy in (0, 1): put(cx + dx - (1 if cx > W / 2 else 0), cy + dy - (1 if cy > H / 2 else 0), WOOD[1])
# a stack of books lying on it (left)
for i, (bx, by, bw, bh) in enumerate(((6, 4, 10, 6), (7, 5, 9, 5), (8, 7, 8, 4))):
    col, dark = BOOK[i % 3]
    for y in range(by, by + bh):
        for x in range(bx, bx + bw):
            c = dark if x == bx or y == by + bh - 1 else col
            if x == bx + bw - 1 and y < by + bh - 1: c = PAGE                 # the pages' edge
            put(x, y, c)
put(10, 8, (210, 170, 70)); put(11, 8, (210, 170, 70))                            # a clasp
# a candle burning in a dish (middle right)
for (dx, dy) in ((-2, 0), (-1, -1), (0, -1), (1, -1), (2, 0), (-1, 1), (0, 1), (1, 1), (-2, 1), (2, 1)): put(31 + dx, 9 + dy, WOOD[0] if dy == 1 else (90, 86, 80))
for (dx, dy) in ((0, 0), (1, 0), (0, 1), (1, 1)): put(31 + dx - 1 + 1, 8 + dy, WAX[1] if dx == 0 else WAX[0])
put(31, 7, (255, 210, 120)); put(31, 6, (255, 150, 60))
# a skull (right)
for y in range(5, 11):
    for x in range(38, 44):
        ex, ey = (x - 40.5) / 2.8, (y - 7.8) / 3.0
        if ex * ex + ey * ey <= 1: put(x, y, BONE[2] if ex + ey < -0.3 else BONE[1])
put(39, 8, (40, 30, 28)); put(41, 8, (40, 30, 28)); put(40, 9, (70, 56, 50))
for x in (39, 40, 41): put(x, 11, BONE[0])
# a rolled scroll lying across the front
for x in range(18, 27): put(x, 13, PAGE if x not in (18, 26) else BONE[0]); put(x, 14, (176, 166, 140))
put(22, 13, (150, 30, 30))                                                        # its seal
# outline and the soft dark rim (like the candles)
ring = [(x, y) for y in range(H) for x in range(W) if p[x, y][3] == 0 and any(0 <= x + a < W and 0 <= y + b < H and p[x + a, y + b][3] == 255 for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
for q in ring: p[q] = OL
im.save(os.path.join(out, "occ_shelf.png"))
im.resize((W * 8, H * 8), Image.NEAREST).save(os.path.join(out, "occ_shelf_x8.png"))
print("bookcase")
