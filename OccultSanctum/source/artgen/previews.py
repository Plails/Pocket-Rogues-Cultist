# The bestiary pictures of the cultist and the winged devil, put together from their parts with the goblin prefab's
# own offsets (px at 38 ppu) and the parts' pivots, drawn back to front like the game does (hands under arms under
# the body under the head).  usage: python previews.py <art dir>
import sys, os
from PIL import Image

art = sys.argv[1]
A = lambda n: Image.open(os.path.join(art, n + ".png")).convert("RGBA")
U = 38.0


def compose(prefix, head_piv, fore_piv, wings=False):
    W = H = 44
    cx, cy = W / 2, H / 2 + 2
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))

    def put(img, ox, oy, piv, flip=False):
        if flip: img = img.transpose(Image.FLIP_LEFT_RIGHT); piv = (1 - piv[0], piv[1])
        x = cx + ox - piv[0] * img.width
        y = cy - oy - (1 - piv[1]) * img.height
        im.alpha_composite(img, (int(round(x)), int(round(y))))
    ua = {"R": (0.17, -0.01), "L": (-0.18, -0.01)}
    fa = {"R": (0.06, 0.07), "L": (-0.03, 0.05)}
    hd = {"R": (-0.04, 0.09), "L": (0.04, 0.09)}
    if wings:
        put(A("devil_wing_l_0"), -0.2 * U - 0, -0.05 * U, (21 / 22, 0.56))
        put(A("devil_wing_r_0"), 0.2 * U, -0.05 * U, (1 / 22, 0.56))
    for s in ("L", "R"):          # hands, then forearms, then upper arms
        u = (ua[s][0] * U, ua[s][1] * U); f = (u[0] + fa[s][0] * U, u[1] + fa[s][1] * U); h = (f[0] + hd[s][0] * U, f[1] + hd[s][1] * U)
        put(A(prefix + "hand"), h[0], h[1], (0.667, 0.667), s == "L")
        put(A(prefix + "forearm"), f[0], f[1], fore_piv, s == "L")
        put(A(prefix + "upperarm"), u[0], u[1], (0.5, 0.5), s == "L")
    put(A(prefix + "body"), 0, 0, (0.5, 0.5417))
    put(A(prefix + "head"), 0, 0, head_piv)
    return im


compose("cult_", (0.5, 0.333), (0.4167, 0.4167)).save(os.path.join(art, "cult_preview.png"))
compose("devil_", (0.5, 0.333), (0.5, 0.4167), wings=True).save(os.path.join(art, "devil_preview.png"))
print("previews made")
