# replaces gate() in artgen.py with v5: dark marble portico
import re
p = 'Occult/artgen/artgen.py'
s = open(p, encoding='utf-8').read()
a = s.index('MARBLE = [hx(c)'); b = s.index('def hole():')
new = '''MARBLE = [hx(c) for c in ("#151418", "#222026", "#302d34", "#403c44", "#524d56", "#666069", "#7c767e", "#938c94")]


def marble(x, y, base, seed=200):
    """dark marble with paler veins: a shade of the ramp, lighter along thin wandering lines"""
    v = math.sin(x * .55 + y * .21 + math.sin(y * .37 + seed) * 2.2) + math.sin(x * .13 - y * .48 + seed * .7) * .8
    i = base + (1 if abs(v) < .14 else 0)
    return grain(MARBLE, i, x, y, seed, .25)


def gate():
    """v5: a dark marble portico, like a ruined temple's door frame seen from above: square pillars with carved
    capitals at both ends, an entablature with a row of dentils between them, and a stepped pediment in the middle
    with a red sigil sunk in it"""
    im = Img(GW, GH)
    cx = GW / 2
    # the entablature: a cornice band, a frieze with a thin red inlay, a row of dentils
    im.rect(9, 6, GW - 9, 16, lambda x, y: MARBLE[6] if y == 6 else MARBLE[1] if y == 15 else marble(x, y, 4 if y < 9 else 3))
    for x in range(10, GW - 10): im.put(x, 10, RED[3] if x % 4 else RED[4])
    for x in range(10, GW - 10, 3): im.rect(x, 12, x + 2, 15, lambda xx, yy: MARBLE[5] if yy == 12 else marble(xx, yy, 3, 205))
    # the pillars: square capitals, bevelled, a carved boss in the middle
    for px in (6, GW - 6):
        im.rect(px - 5, 2, px + 5, 21, lambda x, y, px=px: MARBLE[7] if (y == 2 or x == px - 5) else MARBLE[1] if (y >= 20 or x == px + 4) else marble(x, y, 4, 207))
        im.rect(px - 3, 4, px + 3, 19, lambda x, y, px=px: MARBLE[2] if (y == 4 or x == px - 3) else marble(x, y, 3, 208))
        im.disc(px, 11.5, 2.4, lambda x, y: marble(x, y, 5, 209)); im.disc(px - .7, 10.8, 1, MARBLE[7])
        for (kx, ky) in ((px - 3, 5), (px + 2, 5), (px - 3, 18), (px + 2, 18)): im.put(kx, ky, MARBLE[6])
    # the pediment: stepped up in the middle
    for k, (w, y0) in enumerate(((16, 4), (12, 2), (8, 0))):
        im.rect(cx - w / 2, y0, cx + w / 2, 17 - k, lambda x, y, y0=y0: MARBLE[7] if y == y0 else marble(x, y, 4 + (k == 2), 210 + k))
    im.rect(cx - 8, 16, cx + 8, 18, MARBLE[1])
    # the sigil: a ring with an inverted star, glowing dull red
    im.disc(cx, 9.5, 4.2, MARBLE[1])
    im.ring(cx, 9.5, 3.0, 3.9, RED[4])
    pts = [(cx + math.cos(math.pi / 2 + k * 2 * math.pi / 5) * 2.8, 9.5 + math.sin(math.pi / 2 + k * 2 * math.pi / 5) * 2.8) for k in range(5)]
    for k in range(5):
        a_, b_ = pts[(k * 2) % 5], pts[(k * 2 + 2) % 5]
        im.line(a_[0], a_[1], b_[0], b_[1], RED[5])
    return im.finish()


'''
s = s[:a] + new + s[b:]
open(p, 'w', encoding='utf-8').write(s)
print('gate v5 in')
