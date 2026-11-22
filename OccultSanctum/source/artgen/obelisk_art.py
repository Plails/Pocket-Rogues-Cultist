# The sanctuary's centrepiece, as something one can tell from above (the user could not make out the obelisk - a tall
# pillar seen from straight above is only a square): a blood crystal on a round stone pedestal - a cluster of red
# crystal shards, faceted (lit faces bright, shaded faces deep), a pale glint on each, rising from a carved stone bowl
# on its plinth. 26x26 like the old picture (its red glow and the mod's sizes stay).
#   usage: python obelisk_art.py <out dir>
import sys, os, math
from PIL import Image
out = sys.argv[1]; os.makedirs(out, exist_ok=True)
OL = (14, 8, 10, 255)
STONE = [(52, 48, 52), (74, 70, 74), (98, 94, 96), (124, 120, 120)]
RED = [(70, 6, 12), (120, 14, 20), (176, 28, 30), (226, 60, 52), (255, 160, 140)]
W = H = 26
im = Image.new("RGBA", (W, H), (0, 0, 0, 0)); p = im.load()
c = W / 2 - .5
def put(x, y, col):
    x, y = int(round(x)), int(round(y))
    if 0 <= x < W and 0 <= y < H: p[x, y] = col + (255,)
# the plinth: a round stone base, its rim lit at the upper left, carved notches round it
for y in range(H):
    for x in range(W):
        ex, ey = (x - c) / 11.6, (y - c) / 11.6
        r = ex * ex + ey * ey
        if r <= 1:
            s = ex * 0.6 + ey * 0.8
            col = STONE[3] if r > 0.78 and s < -0.2 else STONE[0] if r > 0.78 and s > 0.3 else STONE[2] if r > 0.78 else STONE[1]
            if 0.55 < r <= 0.62: col = STONE[0]                                   # the groove between rim and bowl
            p[x, y] = col + (255,)
for a in range(0, 360, 45):                                                     # notches in the rim
    put(c + math.cos(math.radians(a)) * 10.4, c + math.sin(math.radians(a)) * 10.4, STONE[0])
# the shards: a big one in the middle, smaller ones leaning out round it; each a diamond with a lit and a shaded half
def shard(cx, cy, ang, ln, wd):
    a = math.radians(ang); ux, uy = math.cos(a), math.sin(a); vx, vy = -uy, ux
    for y in range(H):
        for x in range(W):
            dx, dy = x - cx, y - cy
            t = dx * ux + dy * uy; s = dx * vx + dy * vy
            if -ln * 0.35 <= t <= ln and abs(s) <= wd * (1 - max(0, t) / ln if t > 0 else 1 + t / (ln * 0.35)):
                lit = s * (vx * -0.6 + vy * -0.8) > 0                         # the half toward the light
                col = RED[3] if lit else RED[1]
                if abs(s) < 0.6: col = RED[2]                                   # the ridge
                if t > ln * 0.7: col = RED[4] if lit else RED[3]                # the bright tip
                p[x, y] = col + (255,)
# a dark socket in the stone where the cluster grows from
for y in range(H):
    for x in range(W):
        ex, ey = (x - c) / 5.5, (y - (c + 3)) / 3.6
        if ex * ex + ey * ey <= 1: p[x, y] = (30, 22, 26, 255)
# the cluster rises from the socket, leaning up and out (like the game's things, their tops toward the top of the picture)
for ang, ln, wd, dx, dy in ((-150, 6, 1.8, -2.5, 3.5), (-30, 7, 2.0, 2.5, 3.0), (-125, 8, 2.2, -1.5, 3.0), (-60, 8, 2.2, 1.5, 3.0)):
    shard(c + dx, c + dy, ang, ln, wd)
shard(c - 0.3, c + 3.5, -92, 13, 3.2)                                            # the great shard, over the rest
put(c - 1, c - 6, RED[4]); put(c - 1, c - 7, (255, 220, 210)); put(c - 6, c - 1, RED[4])   # glints
# outline
ring = [(x, y) for y in range(H) for x in range(W) if p[x, y][3] == 0 and any(0 <= x + a < W and 0 <= y + b < H and p[x + a, y + b][3] > 0 and p[x + a, y + b] != OL for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
for q in ring: p[q] = OL
im.save(os.path.join(out, "occ_obelisk.png"))
im.resize((W * 8, H * 8), Image.NEAREST).save(os.path.join(out, "occ_obelisk_x8.png"))
print("crystal")
