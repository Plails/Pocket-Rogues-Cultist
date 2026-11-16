# occ_gate = the game's goblin pantry gate with its end posts and its upper beam recoloured to white marble
# (the lower planks keep their brown)
# usage: python gate_from_pantry.py <pantry gate png> <out png>
import sys
from PIL import Image
src, out = sys.argv[1], sys.argv[2]
im = Image.open(src).convert("RGBA")
w, h = im.size
WHITE = [(70, 66, 74), (104, 99, 108), (140, 135, 143), (176, 171, 176), (208, 203, 204), (234, 229, 226)]
lum = lambda p: .3 * p[0] + .59 * p[1] + .11 * p[2]
body = [im.getpixel((x, y)) for y in range(h) for x in range(w)]
ls = [lum(p) for p in body if p[3] > 200 and lum(p) > 22]
lo, hi = min(ls), max(ls)
px = im.load()
for y in range(h):
    for x in range(w):
        p = px[x, y]
        side = x <= 10 or x >= w - 11
        beam = not side and y <= 9                        # the upper beam (above the dark gap at row 10)
        if p[3] <= 200 or lum(p) <= (5 if beam else 22): continue   # the outline, the soft shadow stay
        if not side and not beam: continue                # the lower planks stay brown
        t = (lum(p) - lo) / max(1, hi - lo)
        c = WHITE[min(5, max(0, round(t * 5 + .6)))]
        px[x, y] = c + (p[3],)
im.save(out)
print("gate", im.size)
