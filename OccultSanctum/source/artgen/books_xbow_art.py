# seen from straight above, in the sanctum's grainy dark style (1 px black outline):
#   occ_table_0 / occ_table_1 - a plank table with books lying on it: stacks of closed books in dark covers (their page
#                               edges showing), an open book with written pages, a candle stub (two arrangements)
#   occ_rack_xbow             - a crossbow rack against the wall, like the spear rack: a beam with two posts and three
#                               crossbows lying on it, stocks towards the room, bows across
# usage: python books_xbow_art.py <art dir>
import sys, os, math
from PIL import Image
out = sys.argv[1]
OL = (0, 0, 0, 255)
def h2(x, y, s): return (math.sin(x * 12.9898 + y * 78.233 + s * 37.719) * 43758.5453) % 1.0
def g(c, x, y, s=1, k=6): d = (h2(x, y, s) - .5) * 2 * k; return tuple(max(0, min(255, int(v + d))) for v in c[:3]) + (255,)
def outline(im):
    w, h = im.size; px = im.load()
    ring = [(x, y) for y in range(h) for x in range(w) if px[x, y][3] == 0 and any(0 <= x + dx < w and 0 <= y + dy < h and px[x + dx, y + dy][3] > 0 and px[x + dx, y + dy] != OL for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for q in ring: px[q] = OL
WOOD = [(26, 17, 10), (44, 30, 18), (60, 42, 26), (76, 54, 34), (92, 68, 44)]
COVERS = [[(58, 14, 18), (88, 22, 26), (112, 34, 34)], [(22, 28, 52), (34, 44, 78), (50, 62, 100)], [(46, 32, 20), (70, 50, 30), (92, 68, 42)], [(30, 44, 26), (44, 64, 38), (60, 84, 50)]]
PAGE = [(150, 138, 112), (186, 174, 146), (210, 200, 172)]
INK = (60, 46, 40)
GOLD = (176, 140, 60)
WAX = [(140, 130, 112), (196, 186, 164), (226, 218, 198)]

def table(arr, name):
    W, H = 48, 30
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0)); px = im.load()
    x0, y0, x1, y1 = 2, 3, 46, 27
    for y in range(y0, y1):
        for x in range(x0, x1):
            k = (y - y0) % 6
            i = 0 if k == 5 else (3 if ((y - y0) // 6) % 2 else 2) + (1 if k == 0 else 0)
            px[x, y] = g(WOOD[i], x, y, 1 + (y - y0) // 6, 5)
    def book(bx, by, w, h, cov, edge_bottom=True):
        for y in range(by, by + h):
            for x in range(bx, bx + w):
                side = x == bx or x == bx + w - 1 or y == by or y == by + h - 1
                px[x, y] = (cov[0] if side else cov[2] if (x - bx + y - by) < 3 else cov[1]) + (255,)
        for x in range(bx + 1, bx + w - 1): px[x, by + h - 1 if edge_bottom else by] = PAGE[1] + (255,)   # the page edge
        px[bx + w // 2, by + h // 2] = GOLD + (255,)                                                    # a gold mark on the cover
        for x in range(bx - 1, bx + w + 1):
            for y in (by - 1, by + h):
                if 0 <= x < W and 0 <= y < H and px[x, y][:3] not in [c for cv in COVERS for c in cv] and px[x, y][:3] != PAGE[1]: px[x, y] = OL
        for y in range(by, by + h):
            for x in (bx - 1, bx + w):
                if 0 <= x < W and px[x, y][:3] not in [c for cv in COVERS for c in cv]: px[x, y] = OL
    def open_book(bx, by):
        w, h = 14, 9
        for y in range(by, by + h):
            for x in range(bx, bx + w):
                px[x, y] = (COVERS[0][0] if (y in (by, by + h - 1) or x in (bx, bx + w - 1)) else PAGE[2] if x < bx + w // 2 else PAGE[1]) + (255,)
        for x in range(bx, bx + w): px[x, by - 1] = OL; px[x, by + h] = OL
        for y in range(by - 1, by + h + 1): px[bx - 1, y] = OL; px[bx + w, y] = OL
        mid = bx + w // 2
        for y in range(by + 1, by + h - 1): px[mid, y] = PAGE[0] + (255,)
        for y in range(by + 2, by + h - 2, 2):                                                          # lines of writing
            for x in list(range(bx + 2, mid - 1)) + list(range(mid + 2, bx + w - 2)):
                if h2(x, y, 9) > .3: px[x, y] = INK + (255,)
    def candle(cx, cy):
        for y in range(cy - 2, cy + 3):
            for x in range(cx - 2, cx + 3):
                d = math.hypot(x - cx, y - cy)
                if d <= 2.3: px[x, y] = (WAX[2] if d < 1 else WAX[1] if d < 1.8 else WAX[0]) + (255,)
        px[cx, cy] = (24, 18, 12, 255)
    if arr == 0:
        book(6, 7, 9, 6, COVERS[0]); book(7, 14, 8, 6, COVERS[1]); book(6, 20, 9, 5, COVERS[2])   # a pile by the edge
        open_book(19, 10)
        book(37, 7, 6, 8, COVERS[3]); candle(40, 21)
    else:
        open_book(6, 8)
        book(24, 7, 8, 6, COVERS[2]); book(25, 13, 7, 6, COVERS[0])
        book(35, 9, 7, 9, COVERS[1]); book(35, 19, 8, 5, COVERS[3])
        candle(15, 22)
    outline(im)
    im.save(os.path.join(out, name + ".png")); return im

def rack_xbow():
    W, H = 40, 14
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0)); px = im.load()
    for x in range(2, 38):
        for y in range(2, 5): px[x, y] = g(WOOD[3] if y == 2 else WOOD[2], x, y, 21, 5)
    for x0 in (1, 35):
        for x in range(x0, x0 + 4):
            for y in range(1, 6): px[x, y] = g(WOOD[4] if (x == x0 or y == 1) else WOOD[2], x, y, 22, 5)
    IRON = [(58, 58, 68), (100, 100, 112), (150, 150, 162)]
    for cx in (8, 19, 30):                                                  # three crossbows lying on the rack
        for y in range(4, 13): px[cx, y] = g(WOOD[3], cx, y, 23, 4); px[cx + 1, y] = g(WOOD[2], cx + 1, y, 24, 4)   # the stock, towards the room
        for k in range(-3, 5):                                              # the bow across, curved back at the tips
            bx = cx + k; by = 5 + (1 if abs(k - .5) > 2.5 else 0)
            px[bx, by] = (IRON[2] if k in (0, 1) else IRON[1]) + (255,)
        for k in range(-2, 4): px[cx + k, 7] = (190, 180, 150, 255) if abs(k - .5) > 1 else px[cx + k, 7]   # the string
        px[cx, 12] = IRON[0] + (255,); px[cx + 1, 12] = IRON[0] + (255,)   # the butt plate
    outline(im)
    im.save(os.path.join(out, "occ_rack_xbow.png")); return im

a = table(0, "occ_table_0"); b = table(1, "occ_table_1"); c = rack_xbow()
pv = Image.new("RGBA", (48 * 6 * 2 + 40 * 6 + 40, 30 * 6), (60, 40, 30, 255)); x = 0
for i in (a, b, c): j = i.resize((i.width * 6, i.height * 6), Image.NEAREST); pv.alpha_composite(j, (x, 0)); x += j.width + 20
pv.save(os.path.join(os.path.dirname(os.path.abspath(__file__)), "books_xbow_preview.png"))
print("done")
