using System;
using System.Collections.Generic;
using System.IO;

namespace ArtGen
{
    /// Skill icons drawn the way the game's own icons are: 34x34, bold readable shapes, 4-5 tones per material
    /// lit from the top-left, a black line between parts and a black outline around everything, a few bright glints.
    public static partial class IconArt
    {
        const int S = 34;
        static readonly C Black = C.Hex("#020202");

        // materials, dark -> light (the game's own palette where it has one)
        static readonly Ramp Iron = new Ramp("#3a3a44", "#6b6b78", "#9d9d9d", "#c0c0c0", "#fbf8f0");
        static readonly Ramp Wood = new Ramp("#4a2608", "#763d06", "#a56323", "#d99d3d");
        static readonly Ramp Leather = new Ramp("#2e140c", "#55291a", "#7e4228", "#a8643c");
        static readonly Ramp Blood = new Ramp("#4a0903", "#7a0d08", "#ae1e1c", "#e02a20", "#ff7a62");
        static readonly Ramp Poison = new Ramp("#1d460e", "#36821a", "#62c82a", "#b8ff6e");
        static readonly Ramp Gold = new Ramp("#763d06", "#d99d3d", "#ecd04c", "#fffade");
        static readonly Ramp Skin = new Ramp("#6b3a26", "#a0603f", "#d29f70", "#eebf74");
        static readonly Ramp Fire = new Ramp("#a3330a", "#e7660c", "#ffab13", "#ffdf93");
        static readonly Ramp Dark = new Ramp("#1a1818", "#1a1818", "#2a2424", "#3a3030");

        // ------------------------------------------------------------------ canvas with parts
        internal class Canvas
        {
            public Img img = new Img(S, S);
            public int[] id = new int[S * S];       // which part owns the pixel (0 = empty)
            int next = 1;

            /// paints one part: bevel shading (light top-left, shadow bottom-right) and a black line where it lies over older parts
            public void Part(bool[] m, Ramp r, int shade = 1, bool contour = true, int baseTone = -1)
            {
                int n = r.N, mid = baseTone >= 0 ? baseTone : Math.Max(1, n - 3), hi = n - 2, top = n - 1, lo = Math.Max(0, mid - 1);
                int me = next++;
                if (contour)
                    for (int y = 0; y < S; y++)
                        for (int x = 0; x < S; x++)
                        {
                            if (m[y * S + x] || id[y * S + x] == 0) continue;
                            if (M(m, x - 1, y) || M(m, x + 1, y) || M(m, x, y - 1) || M(m, x, y + 1)) img.Put(x, y, Black);
                        }
                for (int y = 0; y < S; y++)
                    for (int x = 0; x < S; x++)
                    {
                        if (!m[y * S + x]) continue;
                        bool tl = !M(m, x - 1, y) || !M(m, x, y - 1), br = !M(m, x + 1, y) || !M(m, x, y + 1);
                        bool tip = !M(m, x - 1, y) && !M(m, x, y - 1);
                        C c = r[mid];
                        if (shade > 0)
                        {
                            if (tip) c = r[top];
                            else if (tl && !br) c = r[hi];
                            else if (br && !tl) c = r[lo];
                            else if (br && tl) c = r[mid];
                            // second ring of shadow on wide shapes
                            else if (shade > 1 && (!M(m, x + 2, y) || !M(m, x, y + 2))) c = r[Math.Max(0, mid - 1)];
                        }
                        img.Put(x, y, c);
                        id[y * S + x] = me;
                    }
            }
            static bool M(bool[] m, int x, int y) => x >= 0 && y >= 0 && x < S && y < S && m[y * S + x];
            public void Px(int x, int y, C c) { if (x >= 0 && y >= 0 && x < S && y < S && img.Get(x, y).a > 0) img.Put(x, y, c); }
            public void PxAny(int x, int y, C c) { if (x >= 0 && y >= 0 && x < S && y < S) { img.Put(x, y, c); if (id[y * S + x] == 0) id[y * S + x] = 999; } }
            public Img Done() { img.Outline(Black); return img; }
        }

        // ------------------------------------------------------------------ masks
        static bool[] New() => new bool[S * S];
        static void Set(bool[] m, int x, int y) { if (x >= 0 && y >= 0 && x < S && y < S) m[y * S + x] = true; }
        static bool[] Capsule(double x0, double y0, double x1, double y1, double r, bool[] m = null)
        {
            m ??= New();
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    double px = x + 0.5, py = y + 0.5, dx = x1 - x0, dy = y1 - y0, l2 = dx * dx + dy * dy;
                    double t = l2 > 0 ? Math.Clamp(((px - x0) * dx + (py - y0) * dy) / l2, 0, 1) : 0;
                    double ex = x0 + dx * t - px, ey = y0 + dy * t - py;
                    if (ex * ex + ey * ey <= r * r) m[y * S + x] = true;
                }
            return m;
        }
        /// a stroke that tapers from width w0 to w1 along a polyline (curves, tails, slashes)
        static bool[] Taper(List<(double x, double y)> pts, double w0, double w1, bool[] m = null)
        {
            m ??= New();
            double total = 0; for (int i = 1; i < pts.Count; i++) total += Dist(pts[i - 1], pts[i]);
            double acc = 0;
            for (int i = 1; i < pts.Count; i++)
            {
                double seg = Dist(pts[i - 1], pts[i]);
                int steps = Math.Max(2, (int)(seg * 3));
                for (int k = 0; k <= steps; k++)
                {
                    double t = k / (double)steps, f = (acc + seg * t) / total;
                    double x = pts[i - 1].x + (pts[i].x - pts[i - 1].x) * t, y = pts[i - 1].y + (pts[i].y - pts[i - 1].y) * t;
                    double r = (w0 + (w1 - w0) * f) / 2;
                    Circle(x, y, Math.Max(0.45, r), m);
                }
                acc += seg;
            }
            return m;
        }
        static double Dist((double x, double y) a, (double x, double y) b) => Math.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));
        static bool[] Circle(double cx, double cy, double r, bool[] m = null)
        {
            m ??= New();
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++) { double dx = x + 0.5 - cx, dy = y + 0.5 - cy; if (dx * dx + dy * dy <= r * r) m[y * S + x] = true; }
            return m;
        }
        static bool[] Ellipse(double cx, double cy, double rx, double ry, double rotDeg, bool[] m = null)
        {
            m ??= New();
            double a = rotDeg * Math.PI / 180, ca = Math.Cos(a), sa = Math.Sin(a);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    double dx = x + 0.5 - cx, dy = y + 0.5 - cy, u = dx * ca + dy * sa, v = -dx * sa + dy * ca;
                    if (u * u / (rx * rx) + v * v / (ry * ry) <= 1) m[y * S + x] = true;
                }
            return m;
        }
        static bool[] Poly(bool[] m, params double[] xy)
        {
            m ??= New();
            var pts = new List<(double, double)>();
            for (int i = 0; i + 1 < xy.Length; i += 2) pts.Add((xy[i], xy[i + 1]));
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++) if (Img.InPoly(pts, x + 0.5, y + 0.5)) m[y * S + x] = true;
            return m;
        }
        static bool[] Minus(bool[] a, bool[] b) { var m = New(); for (int i = 0; i < m.Length; i++) m[i] = a[i] && !b[i]; return m; }
        static List<(double x, double y)> Pts(params double[] xy) { var l = new List<(double, double)>(); for (int i = 0; i + 1 < xy.Length; i += 2) l.Add((xy[i], xy[i + 1])); return l; }
        /// a smooth curve through control points (Catmull-Rom), for tails and arcs
        static List<(double x, double y)> Curve(params double[] xy)
        {
            var p = Pts(xy); var o = new List<(double x, double y)>();
            for (int i = 0; i < p.Count - 1; i++)
            {
                var p0 = p[Math.Max(0, i - 1)]; var p1 = p[i]; var p2 = p[i + 1]; var p3 = p[Math.Min(p.Count - 1, i + 2)];
                for (int k = 0; k < 8; k++)
                {
                    double t = k / 8.0, t2 = t * t, t3 = t2 * t;
                    double x = 0.5 * (2 * p1.Item1 + (-p0.Item1 + p2.Item1) * t + (2 * p0.Item1 - 5 * p1.Item1 + 4 * p2.Item1 - p3.Item1) * t2 + (-p0.Item1 + 3 * p1.Item1 - 3 * p2.Item1 + p3.Item1) * t3);
                    double y = 0.5 * (2 * p1.Item2 + (-p0.Item2 + p2.Item2) * t + (2 * p0.Item2 - 5 * p1.Item2 + 4 * p2.Item2 - p3.Item2) * t2 + (-p0.Item2 + 3 * p1.Item2 - 3 * p2.Item2 + p3.Item2) * t3);
                    o.Add((x, y));
                }
            }
            o.Add(p[p.Count - 1]);
            return o;
        }

        // ------------------------------------------------------------------ shared pieces
        static void SpikedBall(Canvas c, double cx, double cy, double r)
        {
            var sp = New();
            for (int i = 0; i < 8; i++)
            {
                double a = i * Math.PI / 4 + Math.PI / 8, ca = Math.Cos(a), sa = Math.Sin(a), pa = a + Math.PI / 2;
                double bx = cx + ca * (r - 0.5), by = cy + sa * (r - 0.5), tx = cx + ca * (r + 2.6), ty = cy + sa * (r + 2.6);
                Poly(sp, bx + Math.Cos(pa) * 1.3, by + Math.Sin(pa) * 1.3, tx, ty, bx - Math.Cos(pa) * 1.3, by - Math.Sin(pa) * 1.3);
            }
            c.Part(sp, Iron, 1, false);
            c.Part(Circle(cx, cy, r), Iron, 2);
            c.Px((int)(cx - r * 0.4), (int)(cy - r * 0.45), Iron[4]);
        }
        static void Chain(Canvas c, List<(double x, double y)> path, Ramp r)
        {
            double total = 0; for (int i = 1; i < path.Count; i++) total += Dist(path[i - 1], path[i]);
            int n = Math.Max(2, (int)(total / 2.6));
            for (int k = 0; k <= n; k++)
            {
                double target = total * k / n, acc = 0;
                for (int i = 1; i < path.Count; i++)
                {
                    double seg = Dist(path[i - 1], path[i]);
                    if (acc + seg >= target || i == path.Count - 1)
                    {
                        double t = seg > 0 ? Math.Clamp((target - acc) / seg, 0, 1) : 0;
                        double x = path[i - 1].x + (path[i].x - path[i - 1].x) * t, y = path[i - 1].y + (path[i].y - path[i - 1].y) * t;
                        double ang = Math.Atan2(path[i].y - path[i - 1].y, path[i].x - path[i - 1].x) * 180 / Math.PI;
                        var link = Ellipse(x, y, k % 2 == 0 ? 1.9 : 1.6, k % 2 == 0 ? 1.15 : 0.8, ang);
                        c.Part(link, r, 1);
                        break;
                    }
                    acc += seg;
                }
            }
        }
        static void Drop(Canvas c, double x, double y, double s, Ramp r)
        {
            var m = Circle(x, y + s * 0.4, s);
            Poly(m, x - s * 0.8, y + s * 0.1, x, y - s * 1.6, x + s * 0.8, y + s * 0.1);
            c.Part(m, r, 1);
        }

        // ------------------------------------------------------------------ the nine icons
        /// Bloody Strike: the sinner's flail swung in a blood-red arc
        public static Img BloodStrike()
        {
            var c = new Canvas();
            c.Part(Taper(Curve(4, 17, 9, 7, 19, 3, 29.5, 7.5), 3.0, 6.5), Blood, 1, false);                    // the blood arc of the swing
            c.Part(Capsule(4.5, 30, 13, 21.5, 1.6), Wood, 1);                                               // handle
            c.Part(Capsule(3.5, 31, 6, 28.5, 1.9), Leather, 1);                                             // grip end
            Chain(c, Pts(13, 21.5, 16, 18.5, 18.5, 16.5), Iron);
            SpikedBall(c, 22.5, 13, 4.6);
            foreach (var (x, y, s) in new[] { (28.0, 18.0, 1.4), (30.5, 23.5, 1.1), (25.5, 24.0, 0.9) }) Drop(c, x, y, s, Blood);
            return c.Done();
        }

        /// Poison Lash: a handle, an iron chain and leather tails cracking twice, each cracker dripping poison
        public static Img PoisonLash()
        {
            var c = new Canvas();
            c.Part(Taper(Curve(3, 17, 5, 9, 11, 4, 18, 2.5), 1.0, 2.4), Poison, 1, false);                 // the lash's swing line
            c.Part(Capsule(4, 30.5, 9, 25.5, 2.1), Leather, 2);                                             // wrapped grip
            c.Part(Circle(3.2, 31.3, 1.7), Iron, 1);                                                        // pommel
            c.Part(Capsule(8.6, 26, 10.6, 24, 2.4), Iron, 1);                                               // iron cap
            // the chain: big iron links
            for (int k = 0; k < 4; k++)
            {
                double x = 12 + k * 2.1, y = 22.4 - k * 2.1;
                c.Part(Ellipse(x, y, k % 2 == 0 ? 2.1 : 1.7, k % 2 == 0 ? 1.3 : 0.75, -45), Iron, 1);
            }
            // three leather tails fanning out, each ending in a cracker soaked in poison
            var tails = new[] { Curve(19, 15.5, 20, 10, 23, 6.5, 27.5, 5), Curve(19, 15.5, 23, 13, 27, 12.5, 30.5, 14), Curve(19, 15.5, 21.5, 19, 24.5, 21.5, 27, 25.5) };
            foreach (var t in tails) c.Part(Taper(t, 2.9, 1.5), Leather, 1);
            foreach (var t in tails)
            {
                var e = t[t.Count - 1];
                c.Part(Circle(e.x, e.y, 2.1), Poison, 1);
                c.PxAny((int)e.x, (int)e.y + 2, Poison[2]);                                                // a drip of poison
                c.PxAny((int)e.x, (int)e.y + 3, Poison[1]);
            }
            foreach (var (x, y) in new[] { (31, 4), (25, 2), (32, 18), (30, 29), (22, 26) }) c.PxAny(x, y, Poison[3]);
            return c.Done();
        }

        /// Sinner's Strike: the flail head and three sharp blood streaks - one, two, three
        public static Img SinnerStrike()
        {
            var c = new Canvas();
            void Slash(double x0, double y0, double x1, double y1, double w)
            {
                var pts = Pts(x0, y0, (x0 + x1) / 2, (y0 + y1) / 2, x1, y1);
                var m = Taper(pts, 0.6, w);
                var m2 = Taper(Pts(x1, y1, x1 + (x1 - x0) * 0.25, y1 + (y1 - y0) * 0.25), w, 0.6);
                for (int i = 0; i < m.Length; i++) m[i] |= m2[i];
                c.Part(m, Blood, 1);
            }
            Slash(14, 3.5, 27, 9.5, 3.0);
            Slash(12, 10, 29, 17, 3.9);
            Slash(10, 16.5, 30, 26, 4.8);
            c.Part(Capsule(3, 31, 8, 26, 1.6), Wood, 1);
            Chain(c, Pts(8, 26, 10, 23.5), Iron);
            SpikedBall(c, 11.5, 20, 3.6);
            return c.Done();
        }

        /// Blood Rain: drops of his own blood homing in on the enemy
        public static Img BloodRain()
        {
            var c = new Canvas();
            void Clot(double x0, double y0, double x1, double y1, double cx, double cy, double s)
            {
                c.Part(Taper(Curve(x0, y0, cx, cy, x1, y1), 1.2, s * 1.8), Blood, 1, false);
                var m = Ellipse(x1, y1, s * 1.35, s, Math.Atan2(y1 - cy, x1 - cx) * 180 / Math.PI);
                c.Part(m, Blood, 1, true, 3);
                c.Px((int)(x1 - s * 0.5), (int)(y1 - s * 0.5), Blood[4]);
            }
            Clot(3, 6, 22, 9, 12, 3, 2.9);
            Clot(3, 16, 25, 19.5, 14, 12.5, 3.3);
            Clot(4, 27, 21, 29, 12, 25, 2.8);
            foreach (var (x, y) in new[] { (29.0, 13.0), (30.0, 24.0), (27.0, 4.0) }) Drop(c, x, y, 1.0, Blood);
            return c.Done();
        }

        /// an open hand: palm, then every finger and the thumb as its own piece (black lines between them, like the game draws hands)
        static void Hand(Canvas c, double cx, double cy, double ang, Ramp r, bool bloody, double scale = 1)
        {
            double a = ang * Math.PI / 180, ca = Math.Cos(a), sa = Math.Sin(a);
            (double, double) P(double u, double v) => (cx + (u * ca - v * sa) * scale, cy + (u * sa + v * ca) * scale);
            c.Part(Ellipse(cx, cy, 4.4 * scale, 4.3 * scale, ang), r, 2);
            double[] fv = { -3.4, -1.15, 1.15, 3.4 }, fl = { 4.6, 6.0, 5.6, 4.2 };
            for (int i = 0; i < 4; i++)
            {
                var b = P(3.2, fv[i]); var t = P(3.2 + fl[i], fv[i] * 1.1);
                c.Part(Capsule(b.Item1, b.Item2, t.Item1, t.Item2, 0.95 * scale), r, 1);
            }
            var tb = P(0.5, -3.8); var tt = P(3.0, -7.4);
            c.Part(Capsule(tb.Item1, tb.Item2, tt.Item1, tt.Item2, 1.1 * scale), r, 1);
            if (bloody) { var b0 = P(-2.5, 1); c.Px((int)b0.Item1, (int)b0.Item2, Blood[2]); var b1 = P(-1, 2.5); c.Px((int)b1.Item1, (int)b1.Item2, Blood[3]); var b2 = P(0.5, -0.5); c.Px((int)b2.Item1, (int)b2.Item2, Blood[2]); }
        }

        /// Martyr's Hand: a bloodied open hand driving a burning triangle of force forward
        public static Img MartyrHand()
        {
            var c = new Canvas();
            c.Part(Poly(null, 12, 22, 32, 3, 32, 14, 23, 31), Fire, 2);                                   // the triangle of the blow
            c.Part(Poly(null, 14, 21, 29, 8, 28, 16), Fire, 1, false, 3);
            c.Part(Capsule(2, 32, 7, 27, 2.6), Skin, 1);                                                    // wrist
            Hand(c, 11, 22, -45, Skin, true);
            return c.Done();
        }

        /// Divine Punishment: a fist on the chest and the wrath of heaven striking down in gold and blood
        public static Img DivinePunishment()
        {
            var c = new Canvas();
            var rays = New();
            for (int i = 0; i < 8; i++)
            {
                double a = i * Math.PI / 4, ca = Math.Cos(a), sa = Math.Sin(a), pa = a + Math.PI / 2, L = i % 2 == 0 ? 15.5 : 11;
                Poly(rays, 17 + Math.Cos(pa) * 2.2, 15 + Math.Sin(pa) * 2.2, 17 + ca * L, 15 + sa * L, 17 - Math.Cos(pa) * 2.2, 15 - Math.Sin(pa) * 2.2);
            }
            c.Part(rays, Gold, 1, false);
            c.Part(Circle(17, 15, 6.5), Gold, 2);
            var cross = Capsule(17, 7, 17, 24, 1.8); Capsule(11.5, 13, 22.5, 13, 1.8, cross);               // a blood-red cross of wrath
            c.Part(cross, Blood, 1);
            // the fist: back of the hand, then four knuckles and the thumb as separate pieces
            c.Part(Capsule(2, 31.5, 6, 28.5, 2.7), Skin, 1);
            c.Part(Ellipse(9.5, 26.5, 4.6, 3.8, -20), Skin, 2);
            for (int i = 0; i < 4; i++) c.Part(Ellipse(6.3 + i * 2.35, 22.6 - i * 0.75, 1.3, 1.5, -20), Skin, 1);
            c.Part(Capsule(8, 28.5, 12.5, 26.5, 1.15), Skin, 1);
            foreach (var (x, y) in new[] { (27.0, 26.0), (30.0, 21.0) }) Drop(c, x, y, 1.1, Blood);
            return c.Done();
        }

        /// Torment: the blood of the enemies twisted into a vortex around a burning core
        public static Img Torment()
        {
            var c = new Canvas();
            for (int k = 0; k < 4; k++)
            {
                var pts = new List<(double x, double y)>();
                for (int i = 0; i <= 24; i++)
                {
                    double t = i / 24.0, a = k * Math.PI / 2 + t * 3.0, rr = 15 - t * 11;
                    pts.Add((17 + Math.Cos(a) * rr, 17 + Math.Sin(a) * rr));
                }
                c.Part(Taper(pts, 1.4, 4.4), Blood, 1);
            }
            c.Part(Circle(17, 17, 5), Fire, 2);
            c.Part(Circle(16.5, 16.5, 2.4), Fire, 1, false, 3);
            c.Px(15, 15, C.Hex("#fffade"));
            return c.Done();
        }

        /// Religious Ecstasy: the penitent lashing his own back (like the game's own rage icons: a dark figure in a blood-red glow)
        public static Img Ecstasy()
        {
            var c = new Canvas();
            var body = Circle(15, 9, 3.6);                                                                 // hooded head
            Poly(body, 12, 7, 15, 3, 18, 7);
            Poly(body, 9, 13, 21, 13, 22, 31, 8, 31);                                                       // robe
            Capsule(10, 14, 5, 21, 1.9, body); Capsule(5, 21, 7, 26, 1.7, body);                            // left arm down
            Capsule(20, 14, 25, 10, 1.9, body); Capsule(25, 10, 24, 4, 1.7, body);                          // right arm up over the shoulder
            var glow = New();
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                if (!body[y * S + x]) for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) { int xx = x + dx, yy = y + dy; if (xx >= 0 && yy >= 0 && xx < S && yy < S && body[yy * S + xx]) glow[y * S + x] = true; }
            c.Part(glow, Blood, 0, false, 3);                                                               // the red glow around him
            c.Part(body, Dark, 0, false, 0);
            // the scourge: tails whipping down over his back
            c.Part(Capsule(24, 4, 26, 2, 1.2), Wood, 1);
            foreach (var t in new[] { Curve(24.5, 3.5, 28, 9, 28.5, 16), Curve(24.5, 3.5, 30, 7, 31.5, 12), Curve(24.5, 3.5, 26.5, 10, 25, 17) })
                c.Part(Taper(t, 1.4, 0.9), Leather, 1);
            foreach (var (x, y) in new[] { (29.0, 21.0), (31.0, 26.0), (26.0, 24.0), (4.0, 6.0), (7.0, 3.0) }) Drop(c, x, y, 0.9, Blood);
            for (int i = 0; i < 4; i++) c.PxAny(13 + i * 2, 18 + (i % 2) * 3, Blood[2]);                    // wounds on the back
            return c.Done();
        }

        /// Blood Cleansing: a dagger drawn across the outstretched arm, blood running down
        public static Img BloodCleansing()
        {
            var c = new Canvas();
            c.Part(Capsule(14, 33, 15, 20, 3.4), Skin, 2);                                                  // forearm
            Hand(c, 15.5, 14, -90, Skin, false, 0.95);
            // blood from the cut
            c.Part(Taper(Pts(13, 23, 12.5, 27, 13, 31), 2.6, 1.6), Blood, 1);
            c.Part(Taper(Pts(16.5, 23.5, 17, 26.5, 16.5, 29), 2.0, 1.2), Blood, 1);
            Drop(c, 10.5, 32, 0.9, Blood);
            // the dagger across the wrist: blade lower-left, hilt upper-right
            c.Part(Poly(null, 8, 25, 22.5, 18.5, 23.5, 20.5, 9.5, 26.5), Iron, 1);                         // blade
            c.Part(Capsule(22, 16.5, 25.5, 22.5, 1.1), Gold, 1);                                            // guard
            c.Part(Capsule(25, 19, 30, 16.5, 1.3), Leather, 1);                                             // grip
            c.Part(Circle(30.8, 16, 1.5), Gold, 1);                                                         // pommel
            c.Px(9, 25, Blood[3]); c.Px(10, 25, Blood[2]);
            // a small holy glint of cleansing
            foreach (var (x, y) in new[] { (5, 6), (4, 6), (6, 6), (5, 5), (5, 7) }) c.PxAny(x, y, C.Hex("#ffdf93"));
            c.PxAny(5, 6, C.Hex("#fffade"));
            return c.Done();
        }

        public static void Run(string root)
        {
            string outDir = Path.Combine(root, "art", "icons_v2");
            Directory.CreateDirectory(outDir);
            var list = new (string id, Func<Img> f)[]
            {
                ("bloodStrike", BloodStrike), ("poisonLash", PoisonLash), ("sinnerStrike", SinnerStrike),
                ("bloodRain", BloodRain), ("martyrHand", MartyrHand), ("divinePunishment", DivinePunishment),
                ("torment", Torment), ("suffering", Ecstasy), ("bloodCleansing", BloodCleansing),
            };
            var sheet = new Img(list.Length * 34 * 4 + 8, 34 * 4 + 8);
            for (int i = 0; i < sheet.P.Length; i++) sheet.P[i] = C.Hex("#3c3a34");
            int x = 4;
            foreach (var (id, f) in list) { var im = f(); im.Save(Path.Combine(outDir, id + ".png")); sheet.Draw(im.Scale(4), x, 4); x += 34 * 4; }
            sheet.Save(Path.Combine(root, "preview", "icons_v2.png"));
            Console.WriteLine("icons_v2 ok");
        }
    }
}
