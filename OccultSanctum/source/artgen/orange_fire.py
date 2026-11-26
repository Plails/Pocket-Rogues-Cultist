# (0.41.2, the user) the brazier's fire and coals in the colours of an ordinary fire, not blood red:
# red pixels turn orange; the brighter a pixel, the more yellow (the brightest nearly white)
import sys, colorsys, os
from PIL import Image
src, dst = sys.argv[1], sys.argv[2]
pairs = [('occ_rfire_%d' % i, 'occ_ofire_%d' % i, True) for i in range(3)] + [('occ_brazier_red', 'occ_brazier_fire', False)]
for a, b, flame in pairs:
    im = Image.open(os.path.join(src, a + '.png')).convert('RGBA'); p = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, bl, al = p[x, y]
            if al == 0: continue
            h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, bl / 255)
            if s < 0.25 or not (h < 0.06 or h > 0.9): continue       # only the red parts (the iron stays as it is)
            if flame:
                nh = 0.035 + 0.085 * v ** 3                             # orange, only the brightest bits go yellow
                ns = max(0.55, min(1.0, s * 1.1)) * (1.0 - 0.3 * v ** 4)
                nv = min(1.0, v * 1.1 + 0.08)
            else:                                                       # coals: deep orange glowing in the bowl
                nh = 0.025 + 0.05 * v
                ns = min(1.0, s * 1.05)
                nv = min(1.0, v * 1.6 + 0.05)
            nr, ng, nb = colorsys.hsv_to_rgb(nh, ns, nv)
            p[x, y] = (int(nr * 255), int(ng * 255), int(nb * 255), al)
    for d in dst.split(';'):
        im.save(os.path.join(d, b + '.png'))
