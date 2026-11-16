# The elder's staff, in the game's own manner (like its goblin and ogre staffs): a shaft two pixels thick, lit on one
# side, dark old wood with two dull gold rings and an iron foot; on top a small bone skull with two horns rising from
# it and a red gem held between them, a bright speck on the gem; a thick black outline.
# Drawn along the same line as the ogre shaman's staff, so it sits in the hand the same way.
#   usage: python elder_staff2.py <ogre parts dir> <out dir>
import sys, os, math
from PIL import Image
src, out = sys.argv[1], sys.argv[2]
st = Image.open(os.path.join(src, "OgrMage_Stuff.png")).convert("RGBA"); W, H = st.size; sp = st.load()
op = [(x, y) for y in range(H) for x in range(W) if sp[x, y][3] > 100]
bot = max(op, key=lambda q: q[1] - q[0] * .3); top = min(op, key=lambda q: q[1] - q[0] * .3)
WOOD = [(52, 28, 18), (78, 44, 26), (106, 62, 36), (132, 82, 48)]
GOLD = [(120, 88, 34), (170, 130, 52), (208, 170, 80)]
IRON = [(56, 56, 64), (92, 92, 102)]
BONE = [(112, 102, 86), (156, 146, 124), (196, 188, 166)]
RED = [(90, 14, 22), (160, 26, 34), (214, 58, 58), (255, 190, 170)]
im = Image.new("RGBA", (W, H), (0, 0, 0, 0)); px = im.load()
def put(x, y, c):
    x, y = int(round(x)), int(round(y))
    if 0 <= x < W and 0 <= y < H: px[x, y] = c + (255,)
x0, y0 = bot; x1, y1 = top
L = math.hypot(x1 - x0, y1 - y0); dx, dy = (x1 - x0) / L, (y1 - y0) / L; L -= 3.5   # room for the head inside the picture
nx, ny = -dy, dx                                        # the shaft's lit side (towards the upper left)
if nx + ny > 0: nx, ny = -nx, -ny
HEAD = 6.5
n = int((L - HEAD) * 2)
for k in range(n + 1):                                  # the shaft, two pixels thick
    i = k / 2; x, y = x0 + dx * i, y0 + dy * i
    ring = abs(i - (L - HEAD) * .35) < .8 or abs(i - (L - HEAD) * .8) < .8
    foot = i < 1.6
    a, b = (IRON[0], IRON[1]) if foot else (GOLD[0], GOLD[2]) if ring else (WOOD[1], WOOD[3] if k % 7 else WOOD[2])
    put(x, y, a); put(x + nx * .9, y + ny * .9, b)
# the skull at the top of the shaft (from above: a small round top, two dark eyes towards the front)
sx, sy = x0 + dx * (L - HEAD + 1.2), y0 + dy * (L - HEAD + 1.2)
for yy in range(H):
    for xx in range(W):
        u, v = xx + .5 - sx, yy + .5 - sy
        if u * u + v * v <= 2.1 ** 2:
            lit = -(u * nx + v * ny) * -1
            px[xx, yy] = BONE[2 if (u * nx + v * ny) > .3 else 1 if (u * nx + v * ny) > -.8 else 0] + (255,)
put(sx + dx * .2 - nx * .9, sy + dy * .2 - ny * .9, (20, 12, 10)); put(sx + dx * .2 + nx * .2, sy + dy * .2 + ny * .2, (20, 12, 10))
# two horns curving up and out from the skull's sides
for s in (-1, 1):
    for k in range(6):
        a = k / 5
        hx = sx + nx * s * (1.6 + a * 1.0) + dx * (a * 2.6)
        hy = sy + ny * s * (1.6 + a * 1.0) + dy * (a * 2.6)
        put(hx, hy, BONE[1] if k < 4 else BONE[2])
# the red gem between the horns, with its bright speck
gx, gy = sx + dx * 2.8, sy + dy * 2.8
for yy in range(H):
    for xx in range(W):
        u, v = xx + .5 - gx, yy + .5 - gy
        d = math.hypot(u, v)
        if d <= 1.6: px[xx, yy] = (RED[2] if (u * nx + v * ny) > 0 else RED[1]) + (255,)
put(gx + nx * .7, gy + ny * .7, RED[3])
# the game's thick black outline
OL = (0, 0, 0, 255)
ring = [(x, y) for y in range(H) for x in range(W) if px[x, y][3] == 0 and any(0 <= x + a < W and 0 <= y + b < H and px[x + a, y + b][3] > 0 and px[x + a, y + b] != OL for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
for q in ring: px[q] = OL
os.makedirs(out, exist_ok=True)
im.save(os.path.join(out, "elder_staff.png"))
im.resize((W * 10, H * 10), Image.NEAREST).save(os.path.join(os.path.dirname(os.path.abspath(__file__)), "staff2_preview.png"))
print("staff")
