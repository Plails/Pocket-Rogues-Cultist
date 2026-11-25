"""The Cultist's clubs with proper club handles (the user: "redraw the handles so they look normal - they are clubs").
The old pictures (weapons_v1) keep their heads; the thin shaft from the head down to the end is taken off and a thick
wooden handle is drawn in its place: dark outline, three tones of wood, a leather wrap near the end, an iron pommel.
The pictures lie diagonally: the head top-left, the handle's end bottom-right."""
import sys, os, math
from PIL import Image

SRC, OUT = sys.argv[1], sys.argv[2]
os.makedirs(OUT, exist_ok=True)
NAMES = ["devastator", "doomBell", "inquisitorFlail", "martyrChain", "penitentChain", "pilgrimFlail", "rustyFlail", "thornStar"]

OUTL = (26, 16, 14, 255)
WOOD = [(150, 98, 52, 255), (116, 72, 36, 255), (84, 50, 26, 255)]      # light, mid, dark
WRAP = [(124, 86, 58, 255), (52, 32, 22, 255)]                          # leather wrap
IRON = [(168, 170, 178, 255), (110, 112, 122, 255)]


def opaque(px, x, y, w, h):
    return 0 <= x < w and 0 <= y < h and px[x, y][3] > 100


def redo(path, half, wide=6):
    im = Image.open(path).convert("RGBA")
    w, h = im.size
    px = im.load()
    pts = [(x, y) for y in range(h) for x in range(w) if px[x, y][3] > 100]
    E = max(pts, key=lambda p: p[0] + p[1])          # the handle's end (bottom right)
    T = min(pts, key=lambda p: p[0] + p[1])          # the head's far tip (top left)
    # walk up the diagonal from the end: where the drawing grows wide, the head begins
    H = None
    for s in range(0, (E[0] + E[1]) - (T[0] + T[1])):
        cx, cy = E[0] - s / 2.0, E[1] - s / 2.0
        width = 0
        for k in range(-8, 9):
            x, y = int(round(cx + k * 0.5)), int(round(cy - k * 0.5))
            if opaque(px, x, y, w, h): width += 1
        if width > wide + half and s > 4:
            H = (cx, cy); break
    if H is None: H = ((E[0] + T[0]) / 2.0, (E[1] + T[1]) / 2.0)
    proj_H = H[0] + H[1]
    # take the old shaft off: every drawn pixel close to the diagonal below the head
    for (x, y) in pts:
        if x + y > proj_H + 1 and abs((x - y) - (E[0] - E[1])) <= 4:
            px[x, y] = (0, 0, 0, 0)
    # the new handle: bands across the diagonal (x - y = c + k)
    c = E[0] - E[1]
    start = int(proj_H) - 1
    end = E[0] + E[1]
    n = end - start
    for sum_ in range(start, end + 1):
        u = (sum_ - start) / max(1, n)             # 0 at the head, 1 at the end
        for k in range(-half - 1, half + 2):
            d = c + k
            if (sum_ + d) % 2: continue
            x, y = (sum_ + d) // 2, (sum_ - d) // 2
            if not (0 <= x < w and 0 <= y < h): continue
            if px[x, y][3] > 100 and sum_ < proj_H + 2: continue      # the head stays on top where they meet
            if abs(k) == half + 1: col = OUTL
            elif u > 0.86: col = IRON[0] if k < 0 else IRON[1]        # the pommel
            elif u > 0.6: col = WRAP[0] if (sum_ // 2) % 2 else WRAP[1]   # the leather wrap
            else: col = WOOD[0] if k < 0 else (WOOD[1] if k == 0 or half == 0 else WOOD[2])
            px[x, y] = col
    # the end cap
    for dx, dy in ((1, 1), (2, 2)):
        x, y = E[0] + dx - 1, E[1] + dy - 1
        if 0 <= x < w and 0 <= y < h and px[x, y][3] < 100: px[x, y] = OUTL
    im.save(os.path.join(OUT, os.path.basename(path)))
    return im


sheet = []
for n in NAMES:
    a = redo(os.path.join(SRC, n + "_equip.png"), 1)
    b = redo(os.path.join(SRC, n + "_icon.png"), 1, 10 if n == "martyrChain" else 6)
    sheet.append((n, Image.open(os.path.join(SRC, n + "_equip.png")).convert("RGBA"), a, b))
sc = 7
S = Image.new("RGBA", (len(sheet) * (32 * sc + 10), 3 * (32 * sc + 10)), (60, 60, 70, 255))
for i, (n, old, a, b) in enumerate(sheet):
    for j, im in enumerate((old, a, b)):
        S.alpha_composite(im.resize((32 * sc, 32 * sc), Image.NEAREST), (i * (32 * sc + 10), j * (32 * sc + 10)))
S.save(os.path.join(OUT, "_preview.png"))
print("done")
