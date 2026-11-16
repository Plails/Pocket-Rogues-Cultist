p = 'Occult/artgen/artgen.py'
s = open(p, encoding='utf-8').read()
a = s.index('def gate():'); b = s.index('def hole():')
new = '''def gate():
    """v6: as narrow and plain as the game's goblin pantry gate (40 px): two bulky rounded posts of dark marble
    at the ends and a low slab between them with a small red sigil"""
    im = Img(GW, GH)
    cx = GW / 2
    # the slab between the posts, sagging a hair, with joints and a dull red sigil in the middle
    for x in range(7, GW - 7):
        sag = int(round(math.sin((x - 7) / (GW - 14) * math.pi) * 1.0))
        for y in range(8 + sag, 15 + sag):
            t = y - (8 + sag)
            c = MARBLE[6] if t == 0 else MARBLE[1] if t == 6 else marble(x, y, 4 if t < 3 else 3)
            if (x - 7) % 6 == 0 and 0 < t < 6: c = MARBLE[2]
            im.put(x, y, c)
    im.disc(cx, 11.5, 2.6, MARBLE[0]); im.ring(cx, 11.5, 1.6, 2.6, RED[4]); im.put(cx - .5, 11, RED[5])
    # the posts: rough rounded lumps of marble, lit from the top-left, a small red stone set in each
    for px in (5.5, GW - 5.5):
        im.disc(px, 11.5, 5.6, lambda x, y: marble(x, y, 3, 230))
        im.disc(px - .7, 10.6, 4.4, lambda x, y: marble(x, y, 4, 231))
        im.disc(px - 1.5, 9.4, 2.2, lambda x, y: marble(x, y, 6, 232))
        im.disc(px + .6, 16.0, 3.8, lambda x, y: marble(x, y, 2, 233))
        im.put(px, 12, RED[4]); im.put(px + 1, 12, RED[3])
    return im.finish()


'''
s = s[:a] + new + s[b:]
s = s.replace('GW, GH = 50, 22', 'GW, GH = 40, 22')
open(p, 'w', encoding='utf-8').write(s)
print('gate v6 in')
