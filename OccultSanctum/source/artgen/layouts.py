# a sheet of the sanctum's shapes (the same rectangles as Layout.cs): floor, walls, the carpet lane, the way back
import sys, os
from PIL import Image, ImageDraw
art, ref, out = sys.argv[1], sys.argv[2], sys.argv[3]
T = 24
# name, W, H, rects, holes, cut, round, gateX
L = [("Big hall", 15, 15, [(0,0,15,15)], [], 0, 0, 7), ("Gallery", 9, 25, [(0,0,9,25)], [], 0, 0, 4),
     ("Cross", 19, 21, [(6,0,7,21),(0,7,19,7)], [], 0, 0, 9), ("Two rooms", 15, 23, [(4,0,7,6),(6,6,3,4),(0,10,15,13)], [], 0, 0, 7),
     ("Octagon", 17, 17, [(0,0,17,17)], [], 4, 0, 8), ("Nave", 13, 21, [(0,0,13,21)], [], 0, 0, 6),
     ("Round hall", 19, 19, [], [], 0, 8.6, 9), ("Ring", 19, 19, [(0,3,19,16),(8,0,3,3)], [(6,8,7,6)], 0, 0, 9),
     ("Niches", 19, 17, [(2,0,15,17),(0,3,2,3),(0,9,2,3),(17,3,2,3),(17,9,2,3)], [], 0, 0, 9),
     ("L-hall", 19, 19, [(0,0,9,19),(9,10,10,9)], [], 0, 0, 4), ("T-hall", 21, 19, [(7,0,7,12),(0,12,21,7)], [], 0, 0, 10),
     ("Colonnade", 17, 19, [(0,0,17,19)], [(3,4,2,2),(12,4,2,2),(3,9,2,2),(12,9,2,2),(3,14,2,2),(12,14,2,2)], 0, 0, 8),
     ("Three rooms", 23, 19, [(7,0,9,19),(0,6,7,9),(16,6,7,9)], [(6,6,1,3),(6,12,1,3),(16,6,1,3),(16,12,1,3)], 0, 0, 11)]
big = Image.open(os.path.join(art, "occ_floor_big.png")).convert("RGBA")
wall = Image.open(os.path.join(ref, "TilesetCrypt24x24_walls.png")).convert("RGBA").crop((24, 24, 48, 48))
carpet = Image.open(os.path.join(art, "occ_carpet.png")).convert("RGBA")
gate = Image.open(os.path.join(art, "occ_gate.png")).convert("RGBA")
k = 0.4
tiles = []
for i, (name, w, h, rects, holes, cut, rnd, gx) in enumerate(L):
    # the entrance (as Layout.WithEntrance): every other shape an antechamber + corridor, else a corridor
    room = i % 2 == 0; roomH = 4 if room else 0; corr = 4 + i % 3; sh = roomH + corr
    rects = [(rx, ry + sh, rw, rh) for (rx, ry, rw, rh) in rects] + [(gx - 1, roomH, 3, corr + 1)] + ([(gx - 2, 0, 5, roomH)] if room else [])
    holes = [(rx, ry + sh, rw, rh) for (rx, ry, rw, rh) in holes]
    h += sh
    fl = [[False] * h for _ in range(w)]
    for ri, (rx, ry, rw, rh) in enumerate(rects):
        for x in range(rx, rx + rw):
            for y in range(ry, ry + rh):
                if cut and ri == 0 and min(x - rx, rx + rw - 1 - x) + min(y - ry, ry + rh - 1 - y) < cut: continue
                fl[x][y] = True
    if rnd:
        cx, cy = (w - 1) / 2, (h - sh - 1) / 2 + sh
        for x in range(w):
            for y in range(sh, h): fl[x][y] = (x - cx) ** 2 + (y - cy) ** 2 <= rnd * rnd or (abs(x - gx) <= 1 and y <= cy)
    for (rx, ry, rw, rh) in holes:
        for x in range(rx, rx + rw):
            for y in range(ry, ry + rh): fl[x][y] = False
    W, H = (w + 2) * T, (h + 3) * T
    im = Image.new("RGBA", (W, H), (0, 0, 0, 255))
    for x in range(w + 2):
        for y in range(h + 3): im.alpha_composite(wall, (x * T, y * T))
    top = 0
    while top + 1 < h and fl[gx][top + 1]: top += 1
    for x in range(w):
        for y in range(h):
            if fl[x][y]:
                im.paste(big.crop(((x % 30) * T, (y % 30) * T, (x % 30 + 1) * T, (y % 30 + 1) * T)), ((x + 1) * T, H - (y + 2) * T))
    for y in range(top - 1):
        if fl[gx][y]: im.alpha_composite(carpet, ((gx + 1) * T - 24, H - (y + 2) * T))
    im.alpha_composite(gate, ((gx + 1) * T + 12 - gate.width // 2, H - T - gate.height // 2))
    d = ImageDraw.Draw(im); d.text((6, 4), name, fill=(255, 220, 120, 255))
    tiles.append(im.resize((int(W * k), int(H * k)), Image.NEAREST))
rows = [tiles[:7], tiles[7:]]
SW = max(sum(t.width + 8 for t in r) for r in rows); SH = sum(max(t.height for t in r) + 8 for r in rows)
sheet = Image.new("RGBA", (SW, SH), (40, 40, 40, 255)); y = 0
for r in rows:
    x = 0
    for t in r: sheet.alpha_composite(t, (x, y)); x += t.width + 8
    y += max(t.height for t in r) + 8
sheet.save(out); print(sheet.size)
