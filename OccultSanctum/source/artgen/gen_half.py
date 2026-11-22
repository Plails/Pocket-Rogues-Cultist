# The general's pictures at the rig's own size (half of general_art.py's): his pixels were half the size of everyone
# else's (the user). Each 2x2 block becomes one pixel: the outline if the block holds two outline pixels or more,
# else the commonest colour among its opaque pixels (transparent if fewer than two are opaque).
#   usage: python gen_half.py <dir with the 2x pictures> <out dir>
import sys, os
from collections import Counter
from PIL import Image
src, out = sys.argv[1], sys.argv[2]; os.makedirs(out, exist_ok=True)
def dark(c): return c[0] + c[1] + c[2] < 60
for n in ("gen_body", "gen_head", "gen_upperarm", "gen_forearm", "gen_hand", "gen_leg", "gen_mace"):
    im = Image.open(os.path.join(src, n + ".png")).convert("RGBA"); w, h = im.size; p = im.load()
    o = Image.new("RGBA", ((w + 1) // 2, (h + 1) // 2), (0, 0, 0, 0)); q = o.load()
    for y in range(0, h, 2):
        for x in range(0, w, 2):
            px = [p[x + a, y + b] for a in (0, 1) for b in (0, 1) if x + a < w and y + b < h]
            op = [c for c in px if c[3] > 128]
            if len(op) < 2: continue
            ol = [c for c in op if dark(c)]
            if len(ol) >= 2 and len(op) - len(ol) <= 2 and len(ol) >= len(op) - len(ol): q[x // 2, y // 2] = ol[0]
            else:
                inner = [c for c in op if not dark(c)] or op
                q[x // 2, y // 2] = Counter(inner).most_common(1)[0][0]
    # a clean one-pixel outline round the halved shape (the 2x outline was too thin to survive halving everywhere)
    W2, H2 = o.size
    big = Image.new("RGBA", (W2 + 2, H2 + 2), (0, 0, 0, 0)); big.alpha_composite(o, (1, 1)); q = big.load()
    ring = [(x, y) for y in range(H2 + 2) for x in range(W2 + 2) if q[x, y][3] == 0 and any(0 <= x + a < W2 + 2 and 0 <= y + b < H2 + 2 and q[x + a, y + b][3] > 0 and not dark(q[x + a, y + b]) for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for r in ring: q[r] = (8, 6, 8, 255)
    big.crop((1, 1, W2 + 1, H2 + 1)).save(os.path.join(out, n + ".png"))
print("halved")
