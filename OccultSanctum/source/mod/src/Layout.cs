using System;
using System.Collections.Generic;
using UnityEngine;

namespace OccultSanctum
{
    /// The shape of the sanctum for one visit, like the game's lairs that differ every time: halls, galleries, a
    /// cross, two or three rooms, an octagon, a round hall, a ring round a solid core, a hall with niches, L and T
    /// shapes, a hall with blocks of wall standing in it. Tile coordinates, (0, 0) at the lower left, y up. The way
    /// back is always in the south wall at GateX; the carpet runs north from it to the altar. Any shape may come
    /// mirrored left to right.
    public class Layout
    {
        public string Name;
        public int W, H;
        public List<RectInt> Rects = new List<RectInt>();
        public List<RectInt> Holes = new List<RectInt>();   // solid wall standing inside the floor
        public int Cut;                 // corners cut this deep (octagon)
        public float Round;             // > 0: a round hall of this radius, centred
        public RectInt Entry;           // the part the hero comes into
        public int GateX;               // the column of the way back, the carpet and the altar
        public Vector2? Circle;         // where a ritual circle fits (7 x 7 free), if anywhere
        public List<RectInt> Side = new List<RectInt>();    // the small side rooms (the complex), furnished each by its own use
        bool[,] fl;

        public bool Floor(int x, int y) => x >= 0 && y >= 0 && x < W && y < H && fl[x, y];

        Layout Done()
        {
            fl = new bool[W, H];
            for (int ri = 0; ri < Rects.Count; ri++)
            {
                var r = Rects[ri];
                for (int x = r.xMin; x < r.xMax; x++) for (int y = r.yMin; y < r.yMax; y++)
                    if (x >= 0 && y >= 0 && x < W && y < H)
                    {
                        if (Cut > 0 && ri == 0)                                     // only the hall's corners are cut
                        {
                            int dx = Mathf.Min(x - r.xMin, r.xMax - 1 - x), dy = Mathf.Min(y - r.yMin, r.yMax - 1 - y);
                            if (dx + dy < Cut) continue;
                        }
                        fl[x, y] = true;
                    }
            }
            if (Round > 0)
            {
                float cx = (W - 1) / 2f, cy = (H - ShiftY - 1) / 2f + ShiftY;
                for (int x = 0; x < W; x++) for (int y = ShiftY; y < H; y++)
                    fl[x, y] = (x - cx) * (x - cx) + (y - cy) * (y - cy) <= Round * Round || (Mathf.Abs(x - GateX) <= 1 && y <= cy);
            }
            foreach (var r in Holes)
                for (int x = r.xMin; x < r.xMax; x++) for (int y = r.yMin; y < r.yMax; y++)
                    if (x >= 0 && y >= 0 && x < W && y < H) fl[x, y] = false;
            return this;
        }

        public int ShiftY;              // how far the hall was moved up to make room for the entrance

        /// like the game's rat hole: the hero does not come straight into the hall but into a small antechamber
        /// and/or a narrow corridor first, so its monsters do not set on him the moment he arrives
        Layout WithEntrance(System.Random rnd)
        {
            bool room = rnd.Next(2) == 0;
            int roomH = room ? 4 : 0, corr = rnd.Next(4, 7), shift = roomH + corr;
            var m = new Layout { Name = Name + (room ? " + прихожая и коридор" : " + коридор"), W = W, H = H + shift, Cut = Cut, Round = Round, GateX = GateX, ShiftY = shift };
            foreach (var r in Rects) m.Rects.Add(new RectInt(r.xMin, r.yMin + shift, r.width, r.height));
            foreach (var r in Holes) m.Holes.Add(new RectInt(r.xMin, r.yMin + shift, r.width, r.height));
            foreach (var r in Side) m.Side.Add(new RectInt(r.xMin, r.yMin + shift, r.width, r.height));
            if (Circle.HasValue) m.Circle = new Vector2(Circle.Value.x, Circle.Value.y + shift);
            var hall = new RectInt(GateX - 1, roomH, 3, corr + 1);                 // the corridor, reaching into the hall
            m.Rects.Add(hall);
            if (room) { var ante = new RectInt(GateX - 2, 0, 5, roomH); m.Rects.Add(ante); m.Entry = ante; }
            else m.Entry = new RectInt(GateX - 1, 0, 3, corr);
            return m.Done();
        }

        Layout Mirror()
        {
            var m = new Layout { Name = Name, W = W, H = H, Cut = Cut, Round = Round, GateX = W - 1 - GateX };
            foreach (var r in Rects) m.Rects.Add(new RectInt(W - r.xMax, r.yMin, r.width, r.height));
            foreach (var r in Holes) m.Holes.Add(new RectInt(W - r.xMax, r.yMin, r.width, r.height));
            foreach (var r in Side) m.Side.Add(new RectInt(W - r.xMax, r.yMin, r.width, r.height));
            m.Entry = new RectInt(W - Entry.xMax, Entry.yMin, Entry.width, Entry.height);
            if (Circle.HasValue) m.Circle = new Vector2(W - 1 - Circle.Value.x, Circle.Value.y);
            return m.Done();
        }

        /// the highest floor tile of the gate's column reachable straight up from the gate (the altar stands there)
        public int TopAt(int x) { int y = 0; while (Floor(x, y + 1)) y++; return y; }

        static RectInt R(int x, int y, int w, int h) => new RectInt(x, y, w, h);

        static System.Random Rn;
        static int N(int a, int b) => Rn.Next(a, b + 1);           // a..b inclusive
        static int Odd(int a, int b) { int v = N(a, b); return v % 2 == 0 ? v + 1 : v; }

        static Layout Hall(string name, int w, int h, int cut = 0)
            => new Layout { Name = name, W = w, H = h, Rects = { R(0, 0, w, h) }, Cut = cut, Entry = R(0, 0, w, h), GateX = w / 2, Circle = w >= 9 && h >= 9 ? new Vector2(w / 2, h / 2) : (Vector2?)null };

        static readonly Func<Layout>[] Makers =
        {
            () => Hall("Большой зал", Odd(13, 19), Odd(13, 19)),
            () => { int h = N(21, 29); return new Layout { Name = "Длинная галерея", W = 9, H = h, Rects = { R(0, 0, 9, h) }, Entry = R(0, 0, 9, h), GateX = 4, Circle = new Vector2(4, h / 2) }; },
            () => new Layout { Name = "Крест", W = 19, H = 21, Rects = { R(6, 0, 7, 21), R(0, 7, 19, 7) }, Entry = R(6, 0, 7, 7), GateX = 9, Circle = new Vector2(9, 10) },
            () => new Layout { Name = "Две комнаты", W = 15, H = 23, Rects = { R(4, 0, 7, 6), R(6, 6, 3, 4), R(0, 10, 15, 13) }, Entry = R(4, 0, 7, 6), GateX = 7, Circle = new Vector2(7, 16) },
            () => new Layout { Name = "Восьмиугольный зал", W = 17, H = 17, Rects = { R(0, 0, 17, 17) }, Cut = 4, Entry = R(4, 0, 9, 9), GateX = 8, Circle = new Vector2(8, 8) },
            () => Hall("Неф", 13, N(19, 25)),
            // new shapes
            () => new Layout { Name = "Круглый зал", W = 19, H = 19, Round = 8.6f, Entry = R(6, 0, 7, 7), GateX = 9, Circle = new Vector2(9, 9) },
            () => new Layout { Name = "Кольцо", W = 19, H = 19, Rects = { R(0, 3, 19, 16), R(8, 0, 3, 3) }, Holes = { R(6, 8, 7, 6) }, Entry = R(0, 3, 19, 5), GateX = 9 },
            () => new Layout { Name = "Зал с нишами", W = 19, H = 17, Rects = { R(2, 0, 15, 17), R(0, 3, 2, 3), R(0, 9, 2, 3), R(17, 3, 2, 3), R(17, 9, 2, 3), R(7, 17 - 1, 5, 1) }, Entry = R(2, 0, 15, 17), GateX = 9, Circle = new Vector2(9, 8) },
            () => new Layout { Name = "Г-образный зал", W = 19, H = 19, Rects = { R(0, 0, 9, 19), R(9, 10, 10, 9) }, Entry = R(0, 0, 9, 10), GateX = 4, Circle = new Vector2(13, 14) },
            () => new Layout { Name = "Т-образный зал", W = 21, H = 19, Rects = { R(7, 0, 7, 12), R(0, 12, 21, 7) }, Entry = R(7, 0, 7, 12), GateX = 10, Circle = new Vector2(10, 15) },
            () => new Layout { Name = "Колоннада", W = 17, H = 19, Rects = { R(0, 0, 17, 19) }, Holes = { R(3, 4, 2, 2), R(12, 4, 2, 2), R(3, 9, 2, 2), R(12, 9, 2, 2), R(3, 14, 2, 2), R(12, 14, 2, 2) }, Entry = R(0, 0, 17, 19), GateX = 8, Circle = new Vector2(8, 10) },
            () => new Layout { Name = "Три комнаты", W = 23, H = 19, Rects = { R(7, 0, 9, 19), R(0, 6, 7, 9), R(16, 6, 7, 9) }, Holes = { R(6, 6, 1, 3), R(6, 12, 1, 3), R(16, 6, 1, 3), R(16, 12, 1, 3) }, Entry = R(7, 0, 9, 19), GateX = 11, Circle = new Vector2(11, 10) },
            // more shapes
            () => Hall("Широкий зал", Odd(19, 25), N(11, 13)),
            () => Hall("Ромбовидный зал", 19, 19, 8),
            () => new Layout { Name = "Зал четырёх столпов", W = 17, H = 17, Rects = { R(0, 0, 17, 17) }, Holes = { R(3, 3, 2, 2), R(12, 3, 2, 2), R(3, 12, 2, 2), R(12, 12, 2, 2) }, Entry = R(0, 0, 17, 17), GateX = 8, Circle = new Vector2(8, 8) },
            () => new Layout { Name = "Звезда", W = 21, H = 21, Rects = { R(5, 5, 11, 11), R(8, 0, 5, 21), R(0, 8, 21, 5) }, Entry = R(8, 0, 5, 6), GateX = 10, Circle = new Vector2(10, 10) },
            () => new Layout { Name = "Арка", W = 21, H = 18, Rects = { R(0, 0, 21, 6), R(0, 6, 6, 12), R(15, 6, 6, 12), R(6, 12, 9, 6) }, Entry = R(0, 0, 21, 6), GateX = 10 },
            () => new Layout { Name = "Ступени", W = 17, H = 24, Rects = { R(0, 0, 9, 8), R(4, 8, 9, 8), R(8, 16, 9, 8) }, Entry = R(0, 0, 9, 8), GateX = 4 },
            () => new Layout { Name = "Зал с галереями", W = 19, H = 19, Rects = { R(4, 0, 11, 19), R(0, 2, 2, 15), R(17, 2, 2, 15), R(2, 5, 2, 2), R(2, 12, 2, 2), R(15, 5, 2, 2), R(15, 12, 2, 2) }, Entry = R(4, 0, 11, 19), GateX = 9, Circle = new Vector2(9, 9) },
            () => new Layout { Name = "Два зала", W = 21, H = 15, Rects = { R(0, 0, 9, 15), R(12, 0, 9, 15), R(9, 3, 3, 3), R(9, 10, 3, 3) }, Entry = R(0, 0, 9, 15), GateX = 4, Circle = new Vector2(16, 7) },
            () => new Layout { Name = "Зал с апсидой", W = 15, H = 18, Rects = { R(0, 0, 15, 13), R(2, 13, 11, 2), R(4, 15, 7, 2), R(6, 17, 3, 1) }, Entry = R(0, 0, 15, 13), GateX = 7, Circle = new Vector2(7, 7) },
            () => new Layout { Name = "Катакомба", W = 17, H = 25, Rects = { R(6, 0, 5, 25), R(0, 4, 6, 5), R(11, 4, 6, 5), R(0, 14, 6, 5), R(11, 14, 6, 5), R(3, 21, 11, 4) }, Entry = R(6, 0, 5, 4), GateX = 8 },
            () => new Layout { Name = "Внутренний двор", W = 19, H = 19, Rects = { R(0, 0, 19, 19) }, Holes = { R(4, 4, 4, 1), R(11, 4, 4, 1), R(4, 14, 4, 1), R(11, 14, 4, 1), R(4, 5, 1, 3), R(4, 11, 1, 3), R(14, 5, 1, 3), R(14, 11, 1, 3) }, Entry = R(0, 0, 19, 4), GateX = 9, Circle = new Vector2(9, 9) },
            () => new Layout { Name = "Подкова", W = 19, H = 17, Rects = { R(0, 0, 19, 17) }, Holes = { R(5, 5, 9, 8) }, Entry = R(0, 0, 19, 5), GateX = 9 },
            () => new Layout { Name = "Зал и кельи", W = 21, H = 19, Rects = { R(4, 0, 13, 13), R(4, 15, 3, 4), R(9, 15, 3, 4), R(14, 15, 3, 4), R(4, 13, 2, 2), R(9, 13, 2, 2), R(14, 13, 2, 2) }, Entry = R(4, 0, 13, 13), GateX = 10, Circle = new Vector2(10, 6) },
            () => new Layout { Name = "Крестовый неф", W = 19, H = 23, Rects = { R(5, 0, 9, 23), R(0, 13, 19, 5) }, Holes = { R(7, 4, 1, 1), R(11, 4, 1, 1), R(7, 8, 1, 1), R(11, 8, 1, 1) }, Entry = R(5, 0, 9, 12), GateX = 9, Circle = new Vector2(9, 15) },
        };

        public static int Count => Makers.Length;

        public static Layout Pick(System.Random rnd)
        {
            Rn = rnd;
            for (int tries = 0; tries < 20; tries++)
            {
                // most visits: a complex of rooms and corridors; else one of the great halls
                var l = (rnd.Next(100) < 60 ? Complex() : Makers[rnd.Next(Makers.Length)]()).Done();
                if (!l.Floor(l.GateX, 0) || !l.Connected()) { Debug.LogWarning("Sanctum layout " + l.Name + " is not whole, another one"); continue; }
                if (rnd.Next(2) == 1) l = l.Mirror();
                return l.WithEntrance(rnd);
            }
            return Hall("Большой зал", 15, 15).Done().WithEntrance(rnd);
        }

        /// a complex: an entrance room, a short way north into the great hall (the altar at its far end, the circle
        /// in its middle), and small rooms all round them, each reached by its own corridor - narrow or wide, short
        /// or long - some of them off other small rooms
        static Layout Complex()
        {
            int hw = Odd(11, 15), hh = N(11, 13), ew = Odd(7, 9), eh = N(5, 6), sp = N(3, 5);
            int W = Odd(hw + 2 * N(15, 18), hw + 2 * 19), gx = W / 2;
            int H = eh + sp + hh + N(10, 14);
            var l = new Layout { Name = "Комплекс комнат", W = W, H = H, GateX = gx };
            var entry = R(gx - ew / 2, 0, ew, eh);
            var hall = R(gx - hw / 2, eh + sp, hw, hh);
            l.Rects.Add(entry); l.Rects.Add(R(gx - 1, eh, 3, sp)); l.Rects.Add(hall);
            l.Entry = entry; l.Circle = new Vector2(gx, hall.yMin + hh / 2);
            var taken = new bool[W, H];
            void Take(RectInt r, int pad) { for (int x = r.xMin - pad; x < r.xMax + pad; x++) for (int y = r.yMin - pad; y < r.yMax + pad; y++) if (x >= 0 && y >= 0 && x < W && y < H) taken[x, y] = true; }
            bool Free(RectInt r) { if (r.xMin < 1 || r.yMin < 0 || r.xMax > W - 1 || r.yMax > H - 1) return false; for (int x = r.xMin; x < r.xMax; x++) for (int y = r.yMin; y < r.yMax; y++) if (taken[x, y]) return false; return true; }
            foreach (var r in l.Rects) Take(r, 1);
            var anchors = new List<RectInt> { entry, hall, hall };
            int want = N(5, 8);
            for (int tries = 0; tries < 200 && l.Side.Count < want; tries++)
            {
                var a = anchors[Rn.Next(anchors.Count)];
                char s = "wensw e"[Rn.Next(7)];
                if (s == ' ') s = Rn.Next(2) == 0 ? 'w' : 'e';
                if ((s == 'n' || s == 's') && a.Equals(entry)) continue;
                bool big = Rn.Next(4) == 0;                                          // now and then a big room: a store, a training hall
                int rw = big ? N(8, 11) : N(4, 8), rh = big ? N(7, 9) : N(4, 7), len = N(4, 9), cw = Rn.Next(3) == 0 ? 3 : 2;   // corridors two or three tiles wide, never one
                RectInt cor, room;
                if (s == 'w' || s == 'e')
                {
                    if (a.height - 2 < cw) continue;
                    int cy = N(a.yMin + 1, a.yMax - 1 - cw);
                    cor = s == 'w' ? R(a.xMin - len, cy, len, cw) : R(a.xMax, cy, len, cw);
                    int ry = cy - N(0, rh - cw);
                    room = s == 'w' ? R(cor.xMin - rw, ry, rw, rh) : R(cor.xMax, ry, rw, rh);
                }
                else
                {
                    if (a.width - 2 < cw) continue;
                    int cx = N(a.xMin + 1, a.xMax - 1 - cw);
                    if (Mathf.Abs(cx - gx) <= 2 || Mathf.Abs(cx + cw - 1 - gx) <= 2) continue;   // nothing straight north of the altar or south into the way in
                    int rx = cx - N(0, rw - cw);
                    if (s == 'n') { cor = R(cx, a.yMax, cw, len); room = R(rx, cor.yMax, rw, rh); }
                    else { cor = R(cx, a.yMin - len, cw, len); room = R(rx, cor.yMin - rh, rw, rh); }
                }
                // the corridor runs through rock (its sides free), the room has rock all round it
                var corSides = s == 'n' ? R(cor.xMin - 1, cor.yMin + 1, cor.width + 2, cor.height - 1) : s == 's' ? R(cor.xMin - 1, cor.yMin, cor.width + 2, cor.height - 1) : R(cor.xMin, cor.yMin - 1, cor.width, cor.height + 2);
                if (s == 'w') corSides = R(cor.xMin, cor.yMin - 1, cor.width - 1, cor.height + 2);
                if (s == 'e') corSides = R(cor.xMin + 1, cor.yMin - 1, cor.width - 1, cor.height + 2);
                var roomPad = R(room.xMin - 1, room.yMin - 1, room.width + 2, room.height + 2);
                if (!Free(roomPad) || (corSides.width > 0 && corSides.height > 0 && !Free(corSides))) continue;
                l.Rects.Add(cor); l.Rects.Add(room); l.Side.Add(room);
                Take(room, 1); Take(cor, 1);
                anchors.Add(room);
            }
            // loops: now and then a second corridor joins two side rooms that face each other across the rock
            int loops = 0;
            for (int i = 0; i < l.Side.Count && loops < 2; i++)
                for (int j = 0; j < l.Side.Count && loops < 2; j++)
                {
                    if (i == j || Rn.Next(2) == 0) continue;
                    RectInt a = l.Side[i], b = l.Side[j];
                    int y0 = Mathf.Max(a.yMin + 1, b.yMin + 1), y1 = Mathf.Min(a.yMax - 1, b.yMax - 1), gap = b.xMin - a.xMax;
                    if (gap >= 3 && gap <= 12 && y1 - y0 >= 2)
                    {
                        var cor = R(a.xMax, N(y0, y1 - 2), gap, 2);
                        if (Free(R(cor.xMin + 1, cor.yMin - 1, gap - 2, 4))) { l.Rects.Add(cor); Take(cor, 1); loops++; continue; }
                    }
                    int x0 = Mathf.Max(a.xMin + 1, b.xMin + 1), x1 = Mathf.Min(a.xMax - 1, b.xMax - 1), vgap = b.yMin - a.yMax;
                    if (vgap >= 3 && vgap <= 12 && x1 - x0 >= 2)
                    {
                        var cor = R(N(x0, x1 - 2), a.yMax, 2, vgap);
                        if (Free(R(cor.xMin - 1, cor.yMin + 1, 4, vgap - 2))) { l.Rects.Add(cor); Take(cor, 1); loops++; }
                    }
                }
            // the big rooms get pillars standing in them
            foreach (var r in l.Side)
                if (r.width >= 8 && r.height >= 7 && Rn.Next(2) == 0)
                    foreach (var (px, py) in new[] { (r.xMin + 2, r.yMin + 2), (r.xMax - 3, r.yMin + 2), (r.xMin + 2, r.yMax - 3), (r.xMax - 3, r.yMax - 3) })
                        l.Holes.Add(R(px, py, 1, 1));
            l.Name += " (" + l.Side.Count + " комнат" + (loops > 0 ? ", " + loops + " кольц." : "") + ")";
            return l;
        }

        /// every floor tile can be reached from the gate
        public bool Connected()
        {
            var seen = new bool[W, H]; var st = new Stack<Vector2Int>(); int n = 0, all = 0;
            for (int x = 0; x < W; x++) for (int y = 0; y < H; y++) if (fl[x, y]) all++;
            if (!Floor(GateX, 0)) return false;
            st.Push(new Vector2Int(GateX, 0)); seen[GateX, 0] = true;
            while (st.Count > 0)
            {
                var p = st.Pop(); n++;
                foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                {
                    var q = p + d;
                    if (Floor(q.x, q.y) && !seen[q.x, q.y]) { seen[q.x, q.y] = true; st.Push(q); }
                }
            }
            return n == all;
        }

        public static IEnumerable<Layout> All() { Rn = Rn ?? new System.Random(1); foreach (var m in Makers) yield return m().Done(); }
    }
}
