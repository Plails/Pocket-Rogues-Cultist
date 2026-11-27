# ---------------------------------------------------------------- outline: dark around the figure, dark seams inside
a = img[:, :, 3] > 0
edge = np.zeros_like(a)
edge[1:, :] |= a[:-1, :] & ~a[1:, :]; edge[:-1, :] |= a[1:, :] & ~a[:-1, :]
edge[:, 1:] |= a[:, :-1] & ~a[:, 1:]; edge[:, :-1] |= a[:, 1:] & ~a[:, :-1]
img[edge] = OUT
for y in range(1, H - 1):
    for x in range(1, W - 1):
        o = owner[y, x]
        if o < 0: continue
        for dx, dy in ((1, 0), (0, 1), (-1, 0), (0, -1)):
            p = owner[y + dy, x + dx]
            if p >= 0 and p > o:
                c = img[y, x].astype(int); img[y, x] = (int(c[0] * 0.3), int(c[1] * 0.3), int(c[2] * 0.3), 255)
                y2, x2 = y - dy, x - dx
                if owner[y2, x2] == o:
                    c = img[y2, x2].astype(int); img[y2, x2] = (int(c[0] * 0.7), int(c[1] * 0.7), int(c[2] * 0.7), 255)
                break

out = Image.fromarray(img, 'RGBA')
for f in sys.argv[1:]: out.save(f)
