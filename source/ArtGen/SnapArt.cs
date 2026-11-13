using System;
using System.IO;

namespace ArtGen
{
    /// Quick Snap: the skill icon (34x34, same style as the first Cultist icons)
    public static partial class FlagArt
    {
        public static void RunSnap(string root)
        {
            string icons = Path.Combine(root, "art", "icons"), prev = Path.Combine(root, "preview");
            Directory.CreateDirectory(icons); Directory.CreateDirectory(prev);
            var ic = QuickSnap(); ic.Save(Path.Combine(icons, "quickSnap.png"));
            var p = new Img(34 * 6 + 20, 34 * 6 + 20);
            for (int i = 0; i < p.P.Length; i++) p.P[i] = C.Hex("#28261f");
            Blit(p, ic, 10, 10, 6);
            p.Save(Path.Combine(prev, "snap.png"));
            Console.WriteLine("snap ok");
        }

        /// a leather lash shot dead straight from the grip (bottom-left) to the top-right corner, a crack star at the tip,
        /// crimson speed streaks along it
        public static Img QuickSnap()
        {
            var c = Icon34();
            // the taut lash: a bit thicker at the grip, thin to the tip
            double x0 = 8, y0 = 26, x1 = 26, y1 = 8;
            for (double t = 0; t < 1; t += 0.02)
            {
                double xa = x0 + (x1 - x0) * t, ya = y0 + (y1 - y0) * t, xb = x0 + (x1 - x0) * (t + 0.02), yb = y0 + (y1 - y0) * (t + 0.02);
                c.Line(xa, ya, xb, yb, FP.Leather[t < 0.4 ? 5 : 4], Math.Max(0.7, 1.5 * (1 - t * 0.6)));
            }
            // the grip, bound in ochre
            Handle(c, 3, 31, x0, y0, FP.Wood, 2.6);
            c.Line(x0 - 1.2, y0 + 1.2, x0 + 0.8, y0 - 0.8, FP.Ochre[3], 3.2);
            c.Put((int)x0, (int)y0, FP.Ochre[5]);
            Finish(c);
            // speed streaks along the lash (no outline: they are motion, not objects)
            c.Line(5, 20, 15, 10, FP.F2.A(200), 1); c.Line(12, 28, 22, 18, FP.F1.A(210), 1);
            c.Line(15, 21, 21, 15, FP.F3.A(220), 1);
            // the crack at the tip: a white-gold 4-point star with a red halo
            double cx = 27.5, cy = 6.5;
            c.Ring(cx, cy, 3.5, 4.4, FP.F1.A(170), true);
            c.FillPoly(Pts(cx, cy - 6, cx + 1.3, cy - 1.3, cx + 6, cy, cx + 1.3, cy + 1.3, cx, cy + 6, cx - 1.3, cy + 1.3, cx - 6, cy, cx - 1.3, cy - 1.3), FP.Holy[3]);
            c.FillCircle(cx, cy, 1.4, FP.Holy[4]);
            c.Put((int)cx, (int)cy, new C(255, 255, 255));
            // sparks
            foreach (var (x, y) in new[] { (32, 12), (21, 3), (31, 2), (24, 12) }) c.Put(x, y, FP.Holy[4]);
            return c;
        }
    }
}
