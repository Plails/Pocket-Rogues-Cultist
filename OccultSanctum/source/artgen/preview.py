# A mock-up of the sanctum and of the gate on a crypt floor, from the mod's pictures and the game's own crypt tiles
# (layout = the same tile positions the mod uses).  usage: python preview.py <artdir> <refdir> <out.png>
import sys, os
from PIL import Image

art, ref, out = sys.argv[1], sys.argv[2], sys.argv[3]
A = lambda n: Image.open(os.path.join(art, n + ".png")).convert("RGBA")
R = lambda n: Image.open(os.path.join(ref, n + ".png")).convert("RGBA")
T = 24
walls = R("TilesetCrypt24x24_walls")
wall = walls.crop((24, 24, 48, 48))
cfloor = R("TilesetCrypt24x24_floor_0")
RS, M = 15, 2
W = H = (RS + 2 * M) * T


def put(img, n, tx, ty, rot=0):
    """centre of picture n at tile coords (tx, ty), y up (like the game); rot in 90 degree steps"""
    p = A(n)
    if rot: p = p.rotate(rot, expand=True)
    px = int(round((tx + M + .5) * T - p.width / 2)); py = int(round(H - (ty + M + .5) * T - p.height / 2))
    img.alpha_composite(p, (px, py))


room = Image.new("RGBA", (W, H), (0, 0, 0, 255))
for y in range(0, H, T):
    for x in range(0, W, T): room.alpha_composite(wall, (x, y))
room.alpha_composite(A("occ_floor"), (M * T, M * T))
put(room, "occ_circle_1", 7, 7)
put(room, "occ_altar", 7, 12.6)
for (x, y) in [(3, 3), (11, 3), (3, 11), (11, 11)]: put(room, "occ_column", x, y)
put(room, "occ_table_0", 0.9, 7, 90); put(room, "occ_table_1", 13.1, 7, -90)
for (x, y) in [(5.2, 12.4), (8.8, 12.4), (4.6, 5), (9.4, 5), (4.6, 9), (9.4, 9), (7, 3.9), (1, 13), (13, 13)]:
    put(room, "occ_candles", x, y); put(room, "occ_flames_0", x, y)
# the way back: the gate in the south wall, turned to face north into the room
put(room, "occ_gate", 7, -0.8, 180)
for k, (x, y) in zip(("skel", "zomb", "skel", "rot"), [(1.1, 4.2), (12.9, 4.2), (3.8, 13.4), (10.2, 13.4)]): put(room, "occ_cage_" + k, x, y)
for i, (x, y) in enumerate([(5.6, 2.2), (9.1, 6.6), (4.4, 7.4), (11.8, 11.9), (2.3, 12.0), (8.6, 1.6), (12.1, 6.1)]): put(room, "occ_books_%d" % (i % 4), x, y, (i * 67) % 360)

# the gate on a crypt floor: in a north wall, facing down into the room
gw, gh = 9 * T, 6 * T
g = Image.new("RGBA", (gw, gh), (0, 0, 0, 255))
for y in range(0, gh, T):
    for x in range(0, gw, T): g.alpha_composite(wall if y < 2 * T else cfloor, (x, y))
p = A("occ_gate"); q = A("occ_rubble")
g.alpha_composite(p, (gw // 2 - p.width // 2 - 3 * T, 2 * T - p.height // 2 - 7)); g.alpha_composite(p, (gw // 2 - p.width // 2 + 2 * T, 2 * T - p.height // 2 - 7)); g.alpha_composite(q, (gw // 2 - q.width // 2 + 2 * T, 2 * T - q.height // 2 - 9))

k = 2
sheet = Image.new("RGBA", ((W + gw + 30) * k, max(H, gh) * k + 20), (40, 40, 40, 255))
sheet.alpha_composite(room.resize((W * k, H * k), Image.NEAREST), (0, 0))
sheet.alpha_composite(g.resize((gw * k, gh * k), Image.NEAREST), ((W + 30) * k, 0))
sheet.save(out)
print("preview", sheet.size)
