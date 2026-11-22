# The elder's hood a little smaller (the user), its pixels the same size: one row and one column taken out where the
# picture repeats itself most (the row/column most like its neighbour), so the shape and the trim stay as they were.
#   usage: python elder_head_small.py <elder_head.png> <out png>
import sys
from PIL import Image
im = Image.open(sys.argv[1]).convert("RGBA")
def diff(a, b): return sum(abs(a[i] - b[i]) for i in range(4))
w, h = im.size; p = im.load()
y0 = min(range(2, h - 2), key=lambda y: sum(diff(p[x, y], p[x, y - 1]) for x in range(w)))
o = Image.new("RGBA", (w, h - 1)); o.paste(im.crop((0, 0, w, y0)), (0, 0)); o.paste(im.crop((0, y0 + 1, w, h)), (0, y0)); im = o
w, h = im.size; p = im.load()
x0 = min(range(2, w - 2), key=lambda x: sum(diff(p[x, y], p[x - 1, y]) for y in range(h)))
o = Image.new("RGBA", (w - 1, h)); o.paste(im.crop((0, 0, x0, h)), (0, 0)); o.paste(im.crop((x0 + 1, 0, w, h)), (x0, 0))
o.save(sys.argv[2]); print("row", y0, "col", x0, o.size)
