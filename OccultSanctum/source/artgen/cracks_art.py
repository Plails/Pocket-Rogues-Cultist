# a few cracks and chips on a wall tile: the breakable stretch of wall in front of the sanctum's secret room (used
# only where the scene has no breakable wall of its own) - it must not stand out: thin dark cracks, two chipped
# spots, nothing more.  usage: python cracks_art.py <art dir>
import sys, os
from PIL import Image
out = sys.argv[1]
im = Image.new("RGBA", (24, 24), (0, 0, 0, 0)); px = im.load()
D, M, L = (8, 7, 10, 210), (20, 18, 24, 150), (120, 112, 128, 70)
crack1 = [(5, 3), (6, 4), (6, 5), (7, 6), (8, 7), (8, 8), (9, 9), (10, 10), (10, 11), (9, 12), (9, 13), (10, 14), (11, 15)]
crack2 = [(10, 11), (11, 11), (12, 12), (13, 12), (14, 13), (15, 13)]
crack3 = [(16, 17), (17, 18), (17, 19), (18, 20), (19, 20)]
for (x, y) in crack1 + crack2 + crack3: px[x, y] = D
for (x, y) in [(7, 5), (9, 8), (11, 14), (13, 13), (18, 19)]: px[x, y] = M
for (x, y) in [(4, 3), (5, 2), (17, 17), (15, 12)]: px[x, y] = L          # chipped edges catch a little light
im.save(os.path.join(out, "occ_cracks.png"))
print("cracks drawn")
