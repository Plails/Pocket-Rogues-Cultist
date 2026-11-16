p = 'Occult/artgen/artgen.py'
s = open(p, encoding='utf-8').read()
a = s.index('def gate():'); b = s.index('def hole():')
new = '''def gate():
    """v7: real gates in the wall: a pair of closed doors of dark wood with iron bands, studs and ring handles and
    a red sigil painted across the join, in a doorway framed by two dark-marble pillars and a lintel, a marble
    threshold at the bottom (the floor side)"""
    im = Img(GW, GH)
    cx = GW / 2
    # the doors
    for x in range(11, GW - 11):
        for y in range(5, GH - 4):
            leaf = 0 if x < cx else 1
            plank = (x - 11) // 4
            c = grain(WOOD, 3 if plank % 2 else 2, x, y, 300 + plank, .45)
            if (x - 11) % 4 == 0: c = WOOD[1]
            if abs(x + .5 - cx) < 1: c = WOOD[0]                                         # the join
            im.put(x, y, c)
    for by in (9, 17, GH - 8):                                                          # iron bands with studs
        im.rect(11, by, GW - 11, by + 2, lambda x, y: IRON[3] if y == by else IRON[2])
        for x in range(13, GW - 12, 4): im.put(x, by, IRON[4])
    for hx_ in (cx - 4, cx + 3):                                                        # ring handles
        im.ring(hx_ + .5, 14, 1.3, 2.2, IRON[4]); im.put(hx_, 12, IRON[2])
    im.ring(cx, 22, 2.6, 3.5, RED[3]); im.put(cx - .5, 21.5, RED[4])                     # a red sigil across the join
    for k in range(3): im.put(cx - 1 + k, 18 + k, RED[2])
    # the lintel over the doorway
    im.rect(6, 0, GW - 6, 6, lambda x, y: MARBLE[7] if y == 0 else MARBLE[1] if y == 5 else marble(x, y, 4, 310))
    for x in range(9, GW - 9, 5): im.rect(x, 2, x + 3, 4, lambda xx, yy: marble(xx, yy, 5, 311))
    im.disc(cx, 3, 2.2, RED[3]); im.put(cx - .5, 2.5, RED[5])                           # a red stone in the keystone
    # the pillars: square, with a capital at the top and a wider base at the bottom
    for px in (5.5, GW - 5.5):
        im.rect(px - 4.5, 0, px + 4.5, GH - 2, lambda x, y, px=px: MARBLE[6] if x < px - 3.5 else MARBLE[1] if x > px + 3.5 else marble(x, y, 4, 312))
        im.rect(px - 5.5, 0, px + 5.5, 4, lambda x, y: MARBLE[7] if y == 0 else marble(x, y, 5, 313))      # capital
        im.rect(px - 5.5, GH - 6, px + 5.5, GH - 1, lambda x, y: MARBLE[2] if y >= GH - 2 else marble(x, y, 4, 314))  # base
        for y in range(7, GH - 7, 3): im.put(px - 2, y, MARBLE[3]); im.put(px + 2, y + 1, MARBLE[3])   # fluting
    # the threshold
    im.rect(10, GH - 4, GW - 10, GH, lambda x, y: MARBLE[6] if y == GH - 4 else marble(x, y, 3, 315))
    return im.finish()


'''
s = s[:a] + new + s[b:]
import re
s = re.sub(r'GW, GH = \d+, \d+[^\n]*', 'GW, GH = 56, 34           # a doorway 2.3 tiles wide; its threshold lies on the wall\'s lower edge', s, count=1)
open(p, 'w', encoding='utf-8').write(s)
print('gate v7 in')
