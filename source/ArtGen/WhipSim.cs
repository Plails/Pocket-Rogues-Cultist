using System;
using System.IO;

namespace ArtGen
{
    /// dev check: the lash of the mod's FxWhip at several moments (same maths as the mod); the hero faces up (north),
    /// so his right side is on the right of the picture; white dot = the hand, red dot = a crack (hit) moment
    public static class WhipSim
    {
        static double EO(double x) { x = Math.Clamp(x, 0, 1); return 1 - (1 - x) * (1 - x) * (1 - x); }
        static double EOB(double x, double s) { x = Math.Clamp(x, 0, 1) - 1; return 1 + (s + 1) * x * x * x + s * x * x; }
        public static void Run(string root)
        {
            var kinds = new (string n, double len, double s1, double s2)[] { ("leather", 2.2, 0.08, 0.13), ("devastator", 2.25, 0.11, 0.17) };
            double[] times = { 0, 0.02, 0.04, 0.06, 0.08, 0.10, 0.12, 0.14, 0.16, 0.18, 0.20, 0.23, 0.26, 0.29, 0.32 };
            int cw = 170; double sc = 30, reach = 1.2, from = -75, to = 35, back = -40, retract = 0.16, amp = 10, hz = 4.5, over = 1.7, bendPow = 1.4, facing = 90;
            var img = new Img(times.Length * cw, kinds.Length * cw);
            for (int i = 0; i < img.P.Length; i++) img.P[i] = C.Hex("#28261f");
            for (int r = 0; r < kinds.Length; r++)
            {
                var k = kinds[r];
                double D = k.s1 * 0.9, c1 = k.s1, c2 = k.s1 + k.s2;
                for (int ci = 0; ci < times.Length; ci++)
                {
                    double t = times[ci], ox = ci * cw + cw / 2.0, oy = r * cw + cw - 12;
                    double unroll = 0.3 + 0.7 * EO(t / (k.s1 * 0.7));
                    double wave = amp * (Math.Exp(-Math.Max(0, t - c1) * 9) * (t >= c1 * 0.5 ? 1 : 0.4) + (t > c2 ? Math.Exp(-(t - c2) * 9) : 0));
                    double len = reach * k.len; int n = 20;
                    double bk = Math.Clamp((t - (c2 + D)) / retract, 0, 1);
                    double L = len * unroll * (1 - 0.8 * (bk < 0.5 ? 4 * bk * bk * bk : 1 - Math.Pow(-2 * bk + 2, 3) / 2)), step = L / n, px = 0, py = 0;
                    var col = new C(220, 160, 90, (int)(255 * (1 - bk * bk * 0.8)));
                    for (int i = 0; i < n; i++)
                    {
                        double f = i / (double)n, u = t - D * Math.Pow(f, bendPow), o = over * f;
                        double ang = u <= k.s1 ? from + (to - from) * EOB(u / k.s1, o) : to + (back - to) * EOB((u - k.s1) / k.s2, o * 0.8);
                        double a = (facing + ang + wave * f * Math.Sin((f * 1.6 - t * hz) * Math.PI * 2)) * Math.PI / 180;
                        double nx = px + Math.Cos(a) * step, ny = py + Math.Sin(a) * step;
                        img.Line(ox + px * sc, oy - py * sc, ox + nx * sc, oy - ny * sc, col, 2);
                        px = nx; py = ny;
                    }
                    img.FillCircle(ox + px * sc, oy - py * sc, 2, C.Hex("#7cff4a"));
                    img.FillCircle(ox, oy, 3, C.Hex("#ffffff"));
                    if (Math.Abs(t - (c1 + D)) < 0.011 || Math.Abs(t - (c2 + D)) < 0.011) img.FillCircle(ci * cw + 8, r * cw + 8, 5, C.Hex("#ff3030"));
                }
            }
            img.Save(Path.Combine(root, "preview", "whip_sim.png"));
            Console.WriteLine("sim ok");
        }
    }
}
