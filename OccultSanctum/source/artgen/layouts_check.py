# quick check of the newer sanctum shapes (same rectangles as Layout.cs): whole (every tile reachable from the gate), gate column ok, ascii
from PIL import Image
L = [("Wide", 21, 12, [(0,0,21,12)], [], 0, 10), ("Rhomb", 19, 19, [(0,0,19,19)], [], 8, 9),
 ("FourPillars", 17, 17, [(0,0,17,17)], [(3,3,2,2),(12,3,2,2),(3,12,2,2),(12,12,2,2)], 0, 8),
 ("Star", 21, 21, [(5,5,11,11),(8,0,5,21),(0,8,21,5)], [], 0, 10),
 ("Arch", 21, 18, [(0,0,21,6),(0,6,6,12),(15,6,6,12),(6,12,9,6)], [], 0, 10),
 ("Steps", 17, 24, [(0,0,9,8),(4,8,9,8),(8,16,9,8)], [], 0, 4),
 ("Galleries", 19, 19, [(4,0,11,19),(0,2,2,15),(17,2,2,15),(2,5,2,2),(2,12,2,2),(15,5,2,2),(15,12,2,2)], [], 0, 9),
 ("TwoHalls", 21, 15, [(0,0,9,15),(12,0,9,15),(9,3,3,3),(9,10,3,3)], [], 0, 4),
 ("Apse", 15, 18, [(0,0,15,13),(2,13,11,2),(4,15,7,2),(6,17,3,1)], [], 0, 7),
 ("Catacomb", 17, 25, [(6,0,5,25),(0,4,6,5),(11,4,6,5),(0,14,6,5),(11,14,6,5),(3,21,11,4)], [], 0, 8),
 ("Court", 19, 19, [(0,0,19,19)], [(4,4,4,1),(11,4,4,1),(4,14,4,1),(11,14,4,1),(4,5,1,3),(4,11,1,3),(14,5,1,3),(14,11,1,3)], 0, 9),
 ("Horseshoe", 19, 17, [(0,0,19,17)], [(5,5,9,8)], 0, 9),
 ("Cells", 21, 19, [(4,0,13,13),(4,15,3,4),(9,15,3,4),(14,15,3,4),(5,13,1,2),(10,13,1,2),(15,13,1,2)], [], 0, 10),
 ("CrossNave", 19, 23, [(5,0,9,23),(0,13,19,5)], [(7,4,1,1),(11,4,1,1),(7,8,1,1),(11,8,1,1)], 0, 9)]
S = 6; sheet = []
for name, w, h, rects, holes, cut, gx in L:
    fl = [[False]*h for _ in range(w)]
    for ri,(rx,ry,rw,rh) in enumerate(rects):
        for x in range(rx,rx+rw):
            for y in range(ry,ry+rh):
                if 0<=x<w and 0<=y<h:
                    if cut and ri==0 and min(x-rx,rx+rw-1-x)+min(y-ry,ry+rh-1-y) < cut: continue
                    fl[x][y]=True
    for (rx,ry,rw,rh) in holes:
        for x in range(rx,rx+rw):
            for y in range(ry,ry+rh): fl[x][y]=False
    allc = sum(fl[x][y] for x in range(w) for y in range(h))
    seen={(gx,0)} if fl[gx][0] else set(); st=list(seen)
    while st:
        x,y=st.pop()
        for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)):
            q=(x+dx,y+dy)
            if 0<=q[0]<w and 0<=q[1]<h and fl[q[0]][q[1]] and q not in seen: seen.add(q); st.append(q)
    top=0
    while top+1<h and fl[gx][top+1]: top+=1
    print(f"{name:12s} tiles {allc:3d} reach {len(seen):3d} {'OK' if len(seen)==allc else 'BROKEN'} altar row {top}")
    im = Image.new("RGB",(w*S+2,h*S+2),(20,18,24))
    for x in range(w):
        for y in range(h):
            if fl[x][y]: im.paste((150,140,160) if x!=gx else (150,60,60),(1+x*S,1+(h-1-y)*S,1+x*S+S-1,1+(h-1-y)*S+S-1))
    sheet.append(im)
W=sum(i.width+8 for i in sheet); H=max(i.height for i in sheet)
c=Image.new("RGB",(W,H),(50,50,60)); x=0
for i in sheet: c.paste(i,(x,0)); x+=i.width+8
c.save("layouts_new.png")
