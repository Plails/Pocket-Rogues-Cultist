using System;
using System.Collections.Generic;
using System.IO;

namespace ArtGen
{
    /// Version 4: every icon is an own drawing (nothing copied from the game), drawn the way the game's icons are:
    /// one big bold object filling the cell, clean colour areas, a bright rim on the lit edge, black outline.
    public static partial class IconArt
    {
        /// Blood Rain: one big homing drop of blood with a curved tail, two small drops following
        public static Img V4BloodRain()
        {
            var c = new Canvas();
            var drop = Taper(Curve(3.5, 3.5, 8, 9, 13.5, 14.5, 18.5, 19), 1.0, 13.5);
            Circle(21, 21.5, 8.6, drop);
            c.Part(drop, BloodR, 2);
            Rim(c, drop, BloodR[5]);
            c.Part(Ellipse(17.5, 18.5, 1.6, 2.8, 45), BloodR, 0, false, 5);                               // the wet shine
            c.Px(20, 16, BloodR[5]);
            foreach (var (x, y, r) in new[] { (29.5, 8.0, 2.0), (8.0, 29.0, 1.7) })
            {
                var d = Taper(Pts(x - 3.5, y - 3.5, x, y), 0.8, r * 2);
                Circle(x + 0.4, y + 0.4, r, d);
                c.Part(d, BloodR, 1);
                Rim(c, d, BloodR[5]);
            }
            return c.Done();
        }

        /// Torment: a thick whirl of blood torn from the enemies, turning into a burning core
        public static Img V4Torment()
        {
            var c = new Canvas();
            for (int k = 0; k < 3; k++)
            {
                var pts = new List<(double x, double y)>();
                for (int i = 0; i <= 30; i++)
                {
                    double t = i / 30.0, a = k * 2 * Math.PI / 3 + t * 4.2, rr = 15.6 - t * 10.5;
                    pts.Add((17 + Math.Cos(a) * rr, 17 + Math.Sin(a) * rr));
                }
                var arm = Taper(pts, 2.0, 6.2);
                c.Part(arm, BloodR, 2);
                Rim(c, arm, BloodR[5]);
            }
            var core = Circle(17, 17, 5.8);
            c.Part(core, FireR, 2);
            c.Part(Circle(16, 16, 3.0), FireR, 0, false, 4);
            c.Px(15, 14, FireR[5]); c.Px(14, 15, FireR[5]);
            return c.Done();
        }

        /// Divine Punishment: a great four-pointed holy star of wrath with a drop of his blood at its heart
        public static Img V4DivinePunishment()
        {
            var c = new Canvas();
            double cx = 17, cy = 17;
            var back = Poly(null, cx, cy - 11, cx + 3, cy - 3, cx + 11, cy, cx + 3, cy + 3, cx, cy + 11, cx - 3, cy + 3, cx - 11, cy, cx - 3, cy - 3);
            // the small star turned 45 degrees behind
            var diag = New();
            for (int i = 0; i < 4; i++)
            {
                double a = Math.PI / 4 + i * Math.PI / 2, ca = Math.Cos(a), sa = Math.Sin(a), pa = a + Math.PI / 2;
                Poly(diag, cx + Math.Cos(pa) * 2.6, cy + Math.Sin(pa) * 2.6, cx + ca * 13, cy + sa * 13, cx - Math.Cos(pa) * 2.6, cy - Math.Sin(pa) * 2.6);
            }
            c.Part(diag, GoldR, 1, false, 2);
            var star = Poly(null, cx, cy - 16.5, cx + 4.2, cy - 4.2, cx + 16.5, cy, cx + 4.2, cy + 4.2, cx, cy + 16.5, cx - 4.2, cy + 4.2, cx - 16.5, cy, cx - 4.2, cy - 4.2);
            c.Part(star, GoldR, 2);
            Rim(c, star, GoldR[5]);
            // the drop of blood in the middle
            var d = Circle(cx, cy + 1.8, 4.6);
            Poly(d, cx - 3.6, cy + 0.2, cx, cy - 7.2, cx + 3.6, cy + 0.2);
            c.Part(d, BloodR, 2);
            Rim(c, d, BloodR[5]);
            c.Px((int)cx - 2, (int)cy, BloodR[5]);
            return c.Done();
        }

        /// Religious Ecstasy: a hooded penitent in a blood-red glow, his scourge raised over his own back
        public static Img V4Ecstasy()
        {
            var c = new Canvas();
            var body = New();
            Circle(13.5, 9.5, 4.4, body);                                                                // hood
            Poly(body, 10.5, 7.5, 14.5, 2.5, 17, 8);                                                      // hood point
            Poly(body, 6.5, 15, 20.5, 15, 25, 33, 2.5, 33);                                               // robe
            Capsule(8.5, 15.5, 4.5, 23.5, 2.1, body);                                                     // left arm hanging
            Capsule(19, 15.5, 24.5, 10.5, 2.1, body);                                                     // right arm up
            Capsule(24.5, 10.5, 24.5, 5.5, 1.9, body);
            var glow = Minus(Dilate(body, 1), body);
            c.Part(glow, BloodR, 0, false, 4);
            c.Part(body, Dark, 0, false, 0);
            // a blood-red rim of light on the lit side of the figure
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                    if (body[y * S + x] && ((x > 0 && !body[y * S + x - 1]) || (y > 0 && !body[(y - 1) * S + x]))) c.img.Put(x, y, BloodR[2]);
            c.Px(12, 10, BloodR[3]); c.Px(15, 10, BloodR[3]);                                           // eyes in the hood
            // the scourge: grip in the raised hand, three tails falling over his back
            c.Part(Capsule(24.5, 5, 27, 2, 1.3), WoodR, 1);
            foreach (var tl in new[] { Curve(27, 2, 30, 7, 30.5, 14), Curve(27, 2, 32, 5, 32.5, 10), Curve(27, 2, 28, 8, 26.5, 15) })
            {
                var m = Taper(tl, 1.8, 1.1);
                c.Part(m, LeatherR, 1);
            }
            foreach (var (x, y, s) in new[] { (30.5, 19.5, 1.2), (27.5, 22.0, 1.0), (32.0, 25.5, 0.9) }) Drop(c, x, y, s, BloodR);
            return c.Done();
        }

        /// Blood Cleansing: the outstretched hand, an own dagger drawn across the wrist, blood running down
        public static Img V4BloodCleansing()
        {
            var c = new Canvas();
            c.Part(Capsule(14.5, 34, 14.5, 19, 4.3), SkinR, 2);
            c.Part(Ellipse(14.5, 14.5, 5.3, 4.7, 0), SkinR, 2);
            double[] fx = { 10.4, 13.3, 16.2, 19.0 }, ft = { 4.6, 2.2, 2.6, 5.2 };
            for (int i = 0; i < 4; i++) c.Part(Capsule(fx[i], 11, fx[i], ft[i], 1.3), SkinR, 1);
            c.Part(Capsule(9, 16, 5.8, 11.5, 1.4), SkinR, 1);
            c.Part(Capsule(10.5, 23, 18.5, 21, 0.9), BloodR, 1, false, 4);                              // the cut
            c.Part(Taper(Pts(11.5, 23, 11, 27.5, 11.5, 31.5), 2.8, 1.8), BloodR, 1);
            c.Part(Taper(Pts(16.5, 22.5, 17, 26, 16.5, 29), 2.2, 1.4), BloodR, 1);
            Drop(c, 8.5, 32.0, 1.0, BloodR);
            // the dagger: blade from the guard down to the tip across the cut, grip and pommel up right
            var blade = Poly(null, 23.5, 13.2, 25.3, 15.0, 9.0, 27.6, 7.0, 27.8, 7.4, 25.6);
            c.Part(blade, IronR, 1);
            Rim(c, blade, IronR[5]);
            for (int i = 0; i < 6; i++) c.Px(20 - i * 2, 16 + (int)Math.Round(i * 1.55), IronR[4]);          // the fuller
            c.Px(8, 26, BloodR[3]); c.Px(9, 26, BloodR[2]); c.Px(10, 25, BloodR[3]);                       // blood on the tip
            c.Part(Capsule(21.5, 12.2, 26.5, 17.2, 1.2), GoldR, 1);                                         // guard
            c.Part(Capsule(25.5, 13.5, 29.5, 9.5, 1.5), LeatherR, 1);                                       // grip
            var pm = Circle(30.5, 8.5, 1.9); c.Part(pm, GoldR, 1); Rim(c, pm, GoldR[5]);                    // pommel
            return c.Done();
        }

        public static void Run4(string root)
        {
            string outDir = Path.Combine(root, "art", "icons_v4");
            Directory.CreateDirectory(outDir);
            var list = new (string id, Func<Img> f)[]
            {
                ("bloodStrike", V3BloodStrike), ("poisonLash", V3PoisonLash), ("sinnerStrike", V3SinnerStrike),
                ("bloodRain", V4BloodRain), ("martyrHand", V3MartyrHand), ("divinePunishment", V4DivinePunishment),
                ("torment", V4Torment), ("suffering", V4Ecstasy), ("bloodCleansing", V4BloodCleansing),
            };
            var sheet = new Img(list.Length * (34 * 3 + 6) + 6, 34 * 3 + 12);
            for (int i = 0; i < sheet.P.Length; i++) sheet.P[i] = C.Hex("#3c3a34");
            int x = 6;
            foreach (var (id, f) in list) { var im = f(); im.Save(Path.Combine(outDir, id + ".png")); sheet.Draw(im.Scale(3), x, 6); x += 34 * 3 + 6; }
            sheet.Save(Path.Combine(root, "preview", "icons_v4.png"));
            Console.WriteLine("icons_v4 ok");
        }
    }
}
