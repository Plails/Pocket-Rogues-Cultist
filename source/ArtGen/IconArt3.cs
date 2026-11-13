using System;
using System.Collections.Generic;
using System.IO;

namespace ArtGen
{
    /// Version 3: as close to the game's own icons as possible. Some are built on the game's own icon shapes
    /// (recoloured and changed), the rest are drawn the same way: ONE big bold object filling the cell, large clean
    /// colour areas, a bright rim along the lit edge, black outline, very few small details.
    public static partial class IconArt
    {
        public static string GameIcons;   // folder with the game's icons (ClassForge\gamedata\icons)

        static readonly Ramp BloodR = new Ramp("#3a0604", "#6e0c08", "#a3150f", "#d42a1f", "#ff5a45", "#ffb2a0");
        static readonly Ramp IronR = new Ramp("#2c2c34", "#5c5c66", "#8d8d94", "#b9b9bd", "#e2e2e0", "#fbf8f0");
        static readonly Ramp WoodR = new Ramp("#3e1f06", "#6b3a0c", "#9a5a1e", "#c98636", "#e9b25e");
        static readonly Ramp LeatherR = new Ramp("#2a120a", "#4e2416", "#764026", "#a0603a", "#c98a5c");
        static readonly Ramp PoisonR = new Ramp("#163a0a", "#2f7314", "#55b522", "#8fe63c", "#d6ff9a");
        static readonly Ramp GoldR = new Ramp("#6a3604", "#a56323", "#d99d3d", "#ecd04c", "#fff2a8", "#fffade");
        static readonly Ramp SkinR = new Ramp("#5a2e1c", "#8e5233", "#bf7d52", "#e0a874", "#f6cf9a");
        static readonly Ramp FireR = new Ramp("#8a2306", "#c9480a", "#e7660c", "#ffab13", "#ffdf93", "#fffade");

        static double Lum(C c) => (0.299 * c.r + 0.587 * c.g + 0.114 * c.b) / 255.0;

        /// a game icon centred on the 34x34 cell
        static Img Tpl(string name)
        {
            var src = Img.Load(Path.Combine(GameIcons, name + ".png"));
            var o = new Img(S, S);
            o.Draw(src, (S - src.W) / 2, (S - src.H) / 2);
            return o;
        }
        /// recolours by brightness onto a ramp; black lines stay black; f may pick a ramp per pixel
        static Img Recolor(Img src, Func<int, int, Ramp> rampAt, double gamma = 1.0)
        {
            var o = new Img(src.W, src.H);
            for (int y = 0; y < src.H; y++)
                for (int x = 0; x < src.W; x++)
                {
                    var c = src.Get(x, y);
                    if (c.a < 10) continue;
                    double l = Lum(c);
                    if (l < 0.09) { o.Put(x, y, new C(c.r, c.g, c.b, c.a)); continue; }
                    var r = rampAt(x, y);
                    double t = Math.Pow(Math.Clamp((l - 0.09) / 0.91, 0, 1), gamma);
                    o.Put(x, y, r.At(t).A(c.a));
                }
            return o;
        }
        static Canvas FromImg(Img im)
        {
            var c = new Canvas();
            for (int i = 0; i < im.P.Length; i++) if (im.P[i].a > 0) { c.img.P[i] = im.P[i]; c.id[i] = 500; }
            return c;
        }
        static bool[] Dilate(bool[] m, int r)
        {
            var o = New();
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                {
                    if (!m[y * S + x]) continue;
                    for (int dy = -r; dy <= r; dy++) for (int dx = -r; dx <= r; dx++) if (Math.Abs(dx) + Math.Abs(dy) <= r + (r > 1 ? 1 : 0)) Set(o, x + dx, y + dy);
                }
            return o;
        }
        static bool[] And(bool[] a, bool[] b) { var m = New(); for (int i = 0; i < m.Length; i++) m[i] = a[i] && b[i]; return m; }
        /// a bright rim along the lit (top-left) edge of a mask, like the game's white edge lines
        static void Rim(Canvas c, bool[] m, C col)
        {
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                {
                    if (!m[y * S + x]) continue;
                    bool up = y == 0 || !m[(y - 1) * S + x], left = x == 0 || !m[y * S + x - 1];
                    if (up && left) c.img.Put(x, y, col);
                }
        }

        // ------------------------------------------------------------------ icons
        /// Bloody Strike: a big spiked flail ball soaked in blood, on a short chain and a banded handle
        public static Img V3BloodStrike()
        {
            var c = new Canvas();
            c.Part(Capsule(3, 31, 13.5, 20.5, 2.3), WoodR, 1);
            foreach (var t in new[] { 0.25, 0.7 }) c.Part(Capsule(3 + 10.5 * t - 1.6, 31 - 10.5 * t - 1.6, 3 + 10.5 * t + 1.6, 31 - 10.5 * t + 1.6, 1.0), GoldR, 1);
            c.Part(Ellipse(15, 18.6, 2.3, 1.4, -45), IronR, 1);
            c.Part(Ellipse(17, 16.6, 1.7, 0.9, 45), IronR, 1);
            double cx = 22, cy = 12, r = 8.0;
            var sp = New();
            for (int i = 0; i < 8; i++)
            {
                double a = i * Math.PI / 4, ca = Math.Cos(a), sa = Math.Sin(a), pa = a + Math.PI / 2;
                double bx = cx + ca * (r - 0.8), by = cy + sa * (r - 0.8), tx = cx + ca * (r + 3.3), ty = cy + sa * (r + 3.3);
                Poly(sp, bx + Math.Cos(pa) * 1.8, by + Math.Sin(pa) * 1.8, tx, ty, bx - Math.Cos(pa) * 1.8, by - Math.Sin(pa) * 1.8);
            }
            c.Part(sp, IronR, 1, false);
            var ball = Circle(cx, cy, r);
            c.Part(ball, IronR, 2);
            // blood over the lower right half, running down in drips
            var bl = New();
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++) if (ball[y * S + x] && (y - cy) > -(x - cx) * 0.9 + 1.0) bl[y * S + x] = true;
            Capsule(26, 17, 26, 22.5, 1.0, bl); Capsule(21, 18, 21, 21.5, 0.9, bl);
            c.Part(bl, BloodR, 1, false, 3);
            Rim(c, ball, IronR[5]);
            c.Px(19, 7, IronR[5]); c.Px(20, 7, IronR[5]); c.Px(19, 8, IronR[4]);
            return c.Done();
        }

        /// Poison Lash: a grip, an iron chain and a thick leather lash curling in a swing, its cracker dripping poison
        public static Img V3PoisonLash()
        {
            var c = new Canvas();
            c.Part(Capsule(3.5, 30.5, 9.5, 24.5, 2.6), LeatherR, 2);
            c.Part(Circle(2.8, 31.2, 2.0), IronR, 1);
            c.Part(Capsule(8.2, 26.8, 11.2, 23.8, 2.7), IronR, 1);
            for (int k = 0; k < 3; k++) c.Part(Ellipse(12.8 + k * 2.4, 21.2 - k * 2.4, k % 2 == 0 ? 2.6 : 2.0, k % 2 == 0 ? 1.6 : 0.9, -45), IronR, 1);
            var lash = Taper(Curve(18.6, 14.4, 21, 8.5, 26, 4.8, 30.5, 6.0, 31.2, 9.5), 4.2, 2.6);
            c.Part(lash, LeatherR, 2);
            Rim(c, lash, LeatherR[4]);
            var tail2 = Taper(Curve(18.6, 14.4, 23.5, 16.5, 27, 20, 28.5, 24), 3.2, 2.0);
            c.Part(tail2, LeatherR, 1);
            foreach (var (x, y, r) in new[] { (30.8, 11.5, 2.9), (28.6, 25.6, 2.6) })
            {
                var k = Circle(x, y, r);
                c.Part(k, PoisonR, 2);
                Rim(c, k, PoisonR[4]);
            }
            c.Part(Capsule(30.6, 14.5, 30.6, 17.5, 1.0), PoisonR, 1, false, 3);
            c.Part(Capsule(28.4, 28.5, 28.4, 31.5, 1.0), PoisonR, 1, false, 3);
            foreach (var (x, y) in new[] { (32, 21), (24, 30), (25, 2) }) c.Part(Circle(x + 0.5, y + 0.5, 1.1), PoisonR, 1);
            return c.Done();
        }

        /// Sinner's Strike: three bold blood slashes, one, two, three
        public static Img V3SinnerStrike()
        {
            var c = new Canvas();
            void Slash(List<(double x, double y)> p, double w)
            {
                var mid = p.Count / 2;
                var a = Taper(p.GetRange(0, mid + 1), 0.8, w);
                var b = Taper(p.GetRange(mid, p.Count - mid), w, 0.8);
                for (int i = 0; i < a.Length; i++) a[i] |= b[i];
                c.Part(a, BloodR, 2);
                // the bright core line of the cut
                var core = Taper(p.GetRange(mid / 2, p.Count - mid), 0.6, 0.6);
                for (int i = 0; i < core.Length; i++) if (core[i] && a[i]) { c.img.P[i] = BloodR[5]; }
            }
            Slash(Curve(13, 2, 18, 5, 23, 9, 26, 13), 3.6);
            Slash(Curve(7, 6, 14, 11, 21, 17, 25, 23), 4.8);
            Slash(Curve(2, 11, 8, 18, 14, 25, 17, 32), 6.0);
            foreach (var (x, y, s) in new[] { (29.0, 19.0, 1.3), (31.0, 27.0, 1.0), (24.0, 29.5, 1.1) }) Drop(c, x, y, s, BloodR);
            return c.Done();
        }

        /// Blood Rain: the game's own curved bolt shape, turned into a homing stream of blood, with drops
        public static Img V3BloodRain()
        {
            var src = Recolor(Tpl("Wzd_Str_Flame"), (x, y) => BloodR, 0.9);
            var c = FromImg(src);
            foreach (var (x, y, s) in new[] { (27.0, 26.0, 1.9), (31.0, 18.0, 1.3), (21.0, 30.5, 1.2) }) Drop(c, x, y, s, BloodR);
            return c.Done();
        }

        /// Martyr's Hand: a dark open hand in a burning glow (the way the game draws its rage icons), the triangle of the blow behind it
        public static Img V3MartyrHand()
        {
            var c = new Canvas();
            c.Part(Poly(null, 17, 17, 33, 0.5, 33, 22), FireR, 2);                                        // the triangle of the blow
            var h = New();
            Ellipse(13.5, 21.5, 6.0, 5.4, 0, h);
            double[] fx = { 8.6, 12.1, 15.6, 19.0 }, ft = { 9.0, 6.6, 7.0, 9.6 };
            for (int i = 0; i < 4; i++) Capsule(fx[i], 18, fx[i], ft[i], 1.45, h);
            Capsule(18.5, 23, 23, 18.5, 1.6, h);
            Capsule(11, 27, 9.5, 33, 3.4, h);
            c.Part(Minus(Dilate(h, 1), h), FireR, 0, false, 4);                                          // burning glow around the hand
            c.Part(Capsule(11, 27, 9.5, 33, 3.4), SkinR, 2);
            c.Part(Ellipse(13.5, 21.5, 6.0, 5.4, 0), SkinR, 2);
            for (int i = 0; i < 4; i++) c.Part(Capsule(fx[i], 18, fx[i], ft[i], 1.45), SkinR, 1);
            c.Part(Capsule(18.5, 23, 23, 18.5, 1.6), SkinR, 1);
            foreach (var (x, y) in new[] { (12, 21), (14, 24), (11, 24), (15, 21) }) c.Px(x, y, BloodR[3]);  // blood on the palm
            c.Px(13, 22, BloodR[2]);
            return c.Done();
        }
        /// Divine Punishment: the game's cross, turned into a cross of blood in holy gold, with sparks of wrath
        public static Img V3DivinePunishment()
        {
            var t = Tpl("Wzd_Spec_Heal");
            var o = new Img(S, S);
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                {
                    var p = t.Get(x, y); if (p.a < 10) continue;
                    double l = Lum(p);
                    bool red = p.r > p.g + 40;
                    if (l < 0.09) o.Put(x, y, p);
                    else if (red) o.Put(x, y, BloodR.At(Math.Clamp((l - 0.15) / 0.6, 0, 1) * 0.75));
                    else o.Put(x, y, GoldR.At(Math.Clamp(l, 0, 1) * 0.8));
                }
            var c = FromImg(o);
            foreach (var (x, y) in new[] { (2, 3), (31, 3), (1, 15), (32, 15) })
            {
                c.PxAny(x, y, GoldR[5]); c.PxAny(x - 1, y, GoldR[3]); c.PxAny(x + 1, y, GoldR[3]); c.PxAny(x, y - 1, GoldR[3]); c.PxAny(x, y + 1, GoldR[3]);
            }
            return c.Done();
        }

        /// Torment: the game's power wave turned into a vortex of blood around a burning core
        public static Img V3Torment()
        {
            var src = Recolor(Tpl("Wzd_Str_PowerWave"), (x, y) => (x - 16.5) * (x - 16.5) + (y - 16.5) * (y - 16.5) < 30 ? FireR : BloodR, 2.4);
            return FromImg(src).Done();
        }

        /// Religious Ecstasy: the game's rage figure as a dark penitent in a blood glow, lashing himself with a scourge
        public static Img V3Ecstasy()
        {
            var t = Tpl("Brs_Spec_BuffVamp");
            // the figure (mid tones) goes dark, its light rim becomes blood red
            var o = new Img(S, S);
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                {
                    var p = t.Get(x, y); if (p.a < 10) continue;
                    double l = Lum(p);
                    if (l < 0.09) o.Put(x, y, p);
                    else if (l < 0.2) o.Put(x, y, C.Hex("#1a1414"));
                    else o.Put(x, y, BloodR.At(Math.Clamp((l - 0.3) / 0.6, 0, 1) * 0.6 + 0.35));
                }
            var c = FromImg(o);
            // the scourge in the raised right hand, tails falling over his back
            c.Part(Capsule(25.5, 3, 28.5, 0.8, 1.2), WoodR, 1);
            foreach (var tl in new[] { Curve(25.5, 3, 29.5, 8, 30.5, 14), Curve(25.5, 3, 31.5, 6, 32.5, 10), Curve(25.5, 3, 27, 9, 26, 15) })
                c.Part(Taper(tl, 1.6, 1.0), LeatherR, 1);
            foreach (var (x, y, s) in new[] { (30.5, 19.0, 1.1), (27.0, 21.0, 0.9), (32.0, 24.0, 0.8) }) Drop(c, x, y, s, BloodR);
            return c.Done();
        }

        /// Blood Cleansing: an outstretched hand, the game's own dagger drawn across the wrist, blood running down
        public static Img V3BloodCleansing()
        {
            var c = new Canvas();
            c.Part(Capsule(15, 34, 15, 19, 4.2), SkinR, 2);
            var h = Ellipse(15, 14.5, 5.2, 4.6, 0);
            c.Part(h, SkinR, 2);
            double[] fx = { 11.0, 13.8, 16.6, 19.2 }, ft = { 4.5, 2.2, 2.6, 5.0 };
            for (int i = 0; i < 4; i++) c.Part(Capsule(fx[i], 11, fx[i], ft[i], 1.25), SkinR, 1);
            c.Part(Capsule(9.5, 16, 6.2, 11.5, 1.35), SkinR, 1);
            // the cut and the blood
            c.Part(Capsule(11, 23, 19, 21, 0.9), BloodR, 1, false, 4);
            c.Part(Taper(Pts(12, 23, 11.5, 27, 12, 31), 2.6, 1.8), BloodR, 1);
            c.Part(Taper(Pts(17, 22.5, 17.5, 26, 17, 28.5), 2.2, 1.4), BloodR, 1);
            Drop(c, 9.0, 32.0, 1.0, BloodR);
            // the game's dagger, its blade across the cut
            var d = Img.Load(Path.Combine(GameIcons, "w_hunter1IronDagger.png")).Scale(1);
            var big = new Img(S, S);
            for (int y = 0; y < d.H; y++) for (int x = 0; x < d.W; x++) { var p = d.Get(x, y); if (p.a > 10) big.Put(x + 6, y + 6, p); }
            for (int i = 0; i < big.P.Length; i++) if (big.P[i].a > 10) { c.img.P[i] = big.P[i]; c.id[i] = 600; }
            return c.Done();
        }

        public static void Run3(string root, string gameIcons)
        {
            GameIcons = gameIcons;
            string outDir = Path.Combine(root, "art", "icons_v3");
            Directory.CreateDirectory(outDir);
            var list = new (string id, Func<Img> f)[]
            {
                ("bloodStrike", V3BloodStrike), ("poisonLash", V3PoisonLash), ("sinnerStrike", V3SinnerStrike),
                ("bloodRain", V3BloodRain), ("martyrHand", V3MartyrHand), ("divinePunishment", V3DivinePunishment),
                ("torment", V3Torment), ("suffering", V3Ecstasy), ("bloodCleansing", V3BloodCleansing),
            };
            var sheet = new Img(list.Length * (34 * 3 + 6) + 6, 34 * 3 + 12);
            for (int i = 0; i < sheet.P.Length; i++) sheet.P[i] = C.Hex("#3c3a34");
            int x = 6;
            foreach (var (id, f) in list) { var im = f(); im.Save(Path.Combine(outDir, id + ".png")); sheet.Draw(im.Scale(3), x, 6); x += 34 * 3 + 6; }
            sheet.Save(Path.Combine(root, "preview", "icons_v3.png"));
            Console.WriteLine("icons_v3 ok");
        }
    }
}
