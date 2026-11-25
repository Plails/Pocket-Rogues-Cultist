"""The folded whip, SMALL, as the user wants it (after his photo): held by the handle in the fist (the wrist strap off
its butt), the braided lash coiled in two loops AHEAD of the handle, the tip with its popper hanging out of them.
Laid like the clubs: the handle's butt (the fist) down right, the coil up left (forward)."""
import sys, os, math
from PIL import Image

OUT = sys.argv[1]
W = H = 18
OUTL = (20, 12, 9, 255)


def ring(px, cx, cy, a, b, ang, cols):
    ca, sa = math.cos(math.radians(ang)), math.sin(math.radians(ang))
    for i in range(720):
        t = math.radians(i * 0.5)
        u, v = a * math.cos(t), b * math.sin(t)
        x, y = int(math.floor(cx + u * ca - v * sa)), int(math.floor(cy + u * sa + v * ca))
        if 0 <= x < W and 0 <= y < H: px[x, y] = cols[int(i / 45) % len(cols)]


def outline(im):
    px = im.load(); add = []
    for y in range(H):
        for x in range(W):
            if px[x, y][3]: continue
            if any(0 <= x + dx < W and 0 <= y + dy < H and px[x + dx, y + dy][3] and px[x + dx, y + dy] != OUTL for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                add.append((x, y))
    for x, y in add: px[x, y] = OUTL


def draw(braid, light, dark, stud, strap, coil=True):
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0)); px = im.load()
    if coil:
        ring(px, 6.0, 6.0, 4.6, 3.4, 45, braid)                   # the outer loop, long along the handle
        ring(px, 6.8, 5.2, 3.2, 2.2, 45, braid[1:] + braid[:1])  # the inner loop
        for i, (x, y) in enumerate([(3, 10), (3, 11), (2, 12)]): px[x, y] = braid[i % 2]   # the tip hanging out
        px[2, 13] = (176, 156, 126, 255)                          # its popper
    # the handle: 2 wide, from the coil (9,9) to the butt (15,15)
    for i in range(7):
        x, y = 9 + i, 9 + i
        px[x, y] = dark
        if x - 1 >= 0: px[x - 1, y] = light
    px[10, 11] = dark
    px[11, 11] = stud; px[13, 13] = stud
    px[15, 15] = dark; px[14, 15] = dark
    # the wrist strap off the butt
    for x, y in ((15, 16), (16, 16), (16, 17)): px[x, y] = strap
    outline(im)
    return im


os.makedirs(OUT, exist_ok=True)
BROWN = ([(124, 82, 54, 255), (80, 50, 33, 255), (102, 66, 44, 255)], (110, 70, 48, 255), (58, 34, 24, 255), (206, 196, 168, 255), (44, 30, 26, 255))
RED = ([(156, 44, 36, 255), (94, 22, 20, 255), (124, 30, 27, 255)], (100, 32, 34, 255), (44, 14, 18, 255), (214, 172, 64, 255), (60, 16, 18, 255))
grip = "14.5,3.5"                                                 # the fist near the butt (from the bottom left)
items = [("leather_coil", draw(*BROWN)), ("ritualScourge_coil", draw(*RED)),
         ("leather_handle", draw(*BROWN, coil=False)), ("ritualScourge_handle", draw(*RED, coil=False))]
sc = 12
sheet = Image.new("RGBA", (len(items) * (W * sc + 14), H * sc), (60, 60, 70, 255))
for k, (name, im) in enumerate(items):
    im.save(os.path.join(OUT, name + ".png")); open(os.path.join(OUT, name + ".grip"), "w").write(grip)
    sheet.alpha_composite(im.resize((W * sc, H * sc), Image.NEAREST), (k * (W * sc + 14), 0))
sheet.save(os.path.join(OUT, "_preview.png"))
print("done")
