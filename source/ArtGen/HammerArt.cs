using System;
using System.IO;

namespace ArtGen
{
    /// Hammer of Penance: the skill icon (34x34, same style as the first Cultist icons), the ball of the blow
    /// (art\hammer_ball.png) and the cracks it leaves in the floor (art\hammer_crater.png)
    public static partial class FlagArt
    {
        public static void RunHammer(string root)
        {
            string art = Path.Combine(root, "art"), icons = Path.Combine(art, "icons"), prev = Path.Combine(root, "preview");
            foreach (var d in new[] { art, icons, prev }) Directory.CreateDirectory(d);
            var ic = PenanceHammer(); ic.Save(Path.Combine(icons, "penanceHammer.png"));
            var ball = HammerBall(); ball.Save(Path.Combine(art, "hammer_ball.png"));
            var cr = HammerCrater(); cr.Save(Path.Combine(art, "hammer_crater.png"));
            // preview: the icon x6, ball x6, crater x4 on the game's dark floor colour
            var p = new Img(34 * 6 + 24 * 6 + 48 * 4 + 40, 48 * 4 + 20);
            for (int i = 0; i < p.P.Length; i++) p.P[i] = C.Hex("#28261f");
            Blit(p, ic, 10, 10, 6); Blit(p, ball, 34 * 6 + 20, 10, 6); Blit(p, cr, 34 * 6 + 24 * 6 + 30, 10, 4);
            p.Save(Path.Combine(prev, "hammer.png"));
            Console.WriteLine("hammer ok");
        }

        static void Blit(Img dst, Img src, int ox, int oy, int k)
        {
            for (int y = 0; y < src.H * k; y++)
                for (int x = 0; x < src.W * k; x++)
                {
                    var c = src.Get(x / k, y / k);
                    if (c.a == 0) continue;
                    var b = dst.Get(ox + x, oy + y);
                    double a = c.a / 255.0;
                    dst.Put(ox + x, oy + y, new C((int)(c.r * a + b.r * (1 - a)), (int)(c.g * a + b.g * (1 - a)), (int)(c.b * a + b.b * (1 - a))));
                }
        }

        /// the icon: the chain whirls round in a crimson arc and the big spiked ball smashes into cracked ground
        public static Img PenanceHammer()
        {
            var c = Icon34();
            var r = new Rng(41);
            // the whirl over the head: a crimson swoosh over the top, faint on the left, bright on the right where it turns down
            for (int y = 0; y < 34; y++) for (int x = 0; x < 34; x++)
                {
                    double dx = (x - 17) / 14.0, dy = (y - 14) / 11.0, d = Math.Sqrt(dx * dx + dy * dy);
                    double a = Math.Atan2(dy, dx);                                // top = -pi/2
                    if (a > 0.35 || a < -Math.PI + 0.25) continue;
                    double t = (a + Math.PI - 0.25) / (Math.PI + 0.1);            // 0 at the left .. 1 at the right
                    double w = 0.06 + 0.1 * t;
                    if (Math.Abs(d - 1) > w) continue;
                    c.Put(x, y, FP.Blood.At(0.3 + 0.65 * t).A((int)(90 + 165 * t)));
                }
            // the ground slab under the blow, broken by cracks
            var g = Layer(34, 34);
            g.FillEllipse(18, 29.5, 15, 4, FP.Sack[3]);
            Shade(g, FP.Sack, 5, -0.3, -0.9, 0.3);
            double gx = 20, gy = 29.5;
            for (int k = 0; k < 6; k++)
            {
                double a = k * Math.PI / 3 + r.R(-0.2, 0.2), px = gx, py = gy;
                for (int s = 1; s <= 3; s++)
                {
                    double nx = gx + Math.Cos(a) * 4.5 * s + r.R(-0.7, 0.7), ny = gy + Math.Sin(a) * 1.3 * s + r.R(-0.3, 0.3);
                    if (g.Get((int)Math.Round(nx), (int)Math.Round(ny)).a == 0) break;
                    g.Line(px, py, nx, ny, FP.Sack[0], 1);
                    px = nx; py = ny;
                }
            }
            Stamp(c, g);
            // handle up at the top-left, a heavy chain down to the ball
            Handle(c, 3, 3, 9, 10, FP.Wood, 2.6);
            c.Line(7.5, 8.5, 10, 11, FP.Ochre[3], 3);
            for (double t = 0; t <= 1.001; t += 0.1)
            {
                double x = 10 + (19 - 10) * t, y = 11 + (20 - 11) * t - Math.Sin(t * Math.PI) * 1.2;
                c.FillCircle(x, y, 1.1, ((int)(t * 10)) % 2 == 0 ? FP.Iron[4] : FP.Iron[2]);
            }
            // the ball: big, bloody, smashing into the slab
            Stamp(c, SpikedBall(34, 21, 24, 5.8, FP.Iron, 9, 3.0, 12, true));
            // stone chips and blood flying out of the blow
            foreach (var (x, y) in new[] { (8, 25), (11, 22), (31, 22), (33, 26), (6, 28), (29, 19) }) c.Put(x, y, FP.Sack[5]);
            Drops(c, 17, 4, 4, 18, 33, 27);
            return Finish(c);
        }

        /// the ball of the blow as it flies (24x24): heavy iron, long spikes, blood on it
        public static Img HammerBall()
        {
            var c = SpikedBall(24, 12, 12, 5.5, FP.Iron, 9, 4.2, 7, true);
            return Finish(c);
        }

        /// cracks in the floor where it lands (48x48, soft dark, fades out in the game)
        public static Img HammerCrater()
        {
            var c = new Img(48, 48);
            var r = new Rng(77);
            // a dark dent
            for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++)
                {
                    double d = Math.Sqrt((x - 24) * (x - 24) + (y - 24) * (y - 24)) / 9.0;
                    if (d < 1) c.Put(x, y, new C(20, 14, 12, (int)(150 * (1 - d * d))));
                }
            // cracks running out of it, branching
            for (int k = 0; k < 9; k++)
            {
                double a = k * Math.PI * 2 / 9 + r.R(-0.25, 0.25), px = 24 + Math.Cos(a) * 4, py = 24 + Math.Sin(a) * 4;
                double len = r.R(13, 21);
                int steps = 6;
                for (int s = 1; s <= steps; s++)
                {
                    a += r.R(-0.35, 0.35);
                    double nx = px + Math.Cos(a) * len / steps, ny = py + Math.Sin(a) * len / steps;
                    c.Line(px, py, nx, ny, new C(16, 10, 9, 230 - s * 20), s < 3 ? 1.5 : 1.0);
                    if (s == 3 && r.R(0, 1) < 0.6)
                    {
                        double b = a + (r.R(0, 1) < 0.5 ? 0.7 : -0.7);
                        c.Line(nx, ny, nx + Math.Cos(b) * 5, ny + Math.Sin(b) * 5, new C(16, 10, 9, 150), 0.9);
                    }
                    px = nx; py = ny;
                }
            }
            // a few blood specks
            for (int k = 0; k < 10; k++) { int x = (int)r.R(14, 34), y = (int)r.R(14, 34); c.Put(x, y, FP.Blood[r.I(2, 5)].A(220)); }
            return c;
        }
    }
}
