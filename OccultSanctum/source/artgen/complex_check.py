# a port of Layout.Complex (Layout.cs) to look at a few complexes: whole? how many rooms? a picture of six of them
import random
from PIL import Image
def gen(seed):
    Rn = random.Random(seed)
    N = lambda a, b: Rn.randint(a, b)
    def Odd(a, b):
        v = N(a, b); return v + 1 if v % 2 == 0 else v
    hw, hh, ew, eh, sp = Odd(11, 15), N(11, 13), Odd(7, 9), N(5, 6), N(3, 5)
    W = Odd(hw + 2 * N(15, 18), hw + 2 * 19); gx = W // 2
    H = eh + sp + hh + N(10, 14)
    entry = (gx - ew // 2, 0, ew, eh); hall = (gx - hw // 2, eh + sp, hw, hh)
    rects = [entry, (gx - 1, eh, 3, sp), hall]; side = []
    taken = [[False] * H for _ in range(W)]
    def take(r, pad):
        x0, y0, w, h = r
        for x in range(x0 - pad, x0 + w + pad):
            for y in range(y0 - pad, y0 + h + pad):
                if 0 <= x < W and 0 <= y < H: taken[x][y] = True
    def free(r):
        x0, y0, w, h = r
        if x0 < 1 or y0 < 0 or x0 + w > W - 1 or y0 + h > H - 1: return False
        return not any(taken[x][y] for x in range(x0, x0 + w) for y in range(y0, y0 + h))
    for r in rects: take(r, 1)
    anchors = [entry, hall, hall]; want = N(5, 8)
    for _ in range(200):
        if len(side) >= want: break
        a = Rn.choice(anchors); s = "wensw e"[Rn.randrange(7)]
        if s == ' ': s = 'w' if Rn.randrange(2) == 0 else 'e'
        if s in 'ns' and a == entry: continue
        big = Rn.randrange(4) == 0
        rw = N(8, 11) if big else N(4, 8); rh = N(7, 9) if big else N(4, 7); ln = N(4, 9); cw = 3 if Rn.randrange(3) == 0 else 2
        ax, ay, aw, ah = a
        if s in 'we':
            if ah - 2 < cw: continue
            cy = N(ay + 1, ay + ah - 1 - cw)
            cor = (ax - ln, cy, ln, cw) if s == 'w' else (ax + aw, cy, ln, cw)
            ry = cy - N(0, rh - cw)
            room = (cor[0] - rw, ry, rw, rh) if s == 'w' else (cor[0] + ln, ry, rw, rh)
        else:
            if aw - 2 < cw: continue
            cx = N(ax + 1, ax + aw - 1 - cw)
            if abs(cx - gx) <= 2 or abs(cx + cw - 1 - gx) <= 2: continue
            rx = cx - N(0, rw - cw)
            if s == 'n': cor = (cx, ay + ah, cw, ln); room = (rx, ay + ah + ln, rw, rh)
            else: cor = (cx, ay - ln, cw, ln); room = (rx, ay - ln - rh, rw, rh)
        cx0, cy0, cww, chh = cor
        if s == 'n': sides = (cx0 - 1, cy0 + 1, cww + 2, chh - 1)
        elif s == 's': sides = (cx0 - 1, cy0, cww + 2, chh - 1)
        elif s == 'w': sides = (cx0, cy0 - 1, cww - 1, chh + 2)
        else: sides = (cx0 + 1, cy0 - 1, cww - 1, chh + 2)
        pad = (room[0] - 1, room[1] - 1, room[2] + 2, room[3] + 2)
        if not free(pad) or (sides[2] > 0 and sides[3] > 0 and not free(sides)): continue
        rects += [cor, room]; side.append(room); take(room, 1); take(cor, 1); anchors.append(room)
    fl = [[False] * H for _ in range(W)]
    for (x0, y0, w, h) in rects:
        for x in range(x0, x0 + w):
            for y in range(y0, y0 + h):
                if 0 <= x < W and 0 <= y < H: fl[x][y] = True
    allc = sum(fl[x][y] for x in range(W) for y in range(H))
    seen = {(gx, 0)}; st = [(gx, 0)]
    while st:
        x, y = st.pop()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            q = (x + dx, y + dy)
            if 0 <= q[0] < W and 0 <= q[1] < H and fl[q[0]][q[1]] and q not in seen: seen.add(q); st.append(q)
    return W, H, fl, side, len(seen) == allc, gx
S = 5; ims = []
for seed in range(6):
    W, H, fl, side, ok, gx = gen(seed)
    print(seed, W, H, "rooms", len(side), "whole" if ok else "BROKEN")
    im = Image.new("RGB", (W * S, H * S), (18, 16, 22))
    for x in range(W):
        for y in range(H):
            if fl[x][y]: im.paste((150, 140, 160), (x * S, (H - 1 - y) * S, x * S + S - 1, (H - 1 - y) * S + S - 1))
    for (x0, y0, w, h) in side:
        for x in range(x0, x0 + w):
            for y in range(y0, y0 + h): im.paste((120, 150, 120), (x * S, (H - 1 - y) * S, x * S + S - 1, (H - 1 - y) * S + S - 1))
    ims.append(im)
c = Image.new("RGB", (sum(i.width + 10 for i in ims), max(i.height for i in ims)), (60, 60, 70)); x = 0
for i in ims: c.paste(i, (x, 0)); x += i.width + 10
c.save("complex_new.png")
