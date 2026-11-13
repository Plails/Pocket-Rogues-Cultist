using System;
using System.IO;

namespace ArtGen
{
    /// dev check: Religious Ecstasy's self-flogging (same maths as FV.FxSelfLash), seen from above; the hero faces up,
    /// grey circle = his body, white dot = his right hand, red dot in the corner = a blow lands on his back
    public static class SelfLashSim
    {
        // raise the lash out in front, hold it a moment, snap it over the shoulder onto the back, leave it there a moment
        const double Raise = 0.12, Hold = 0.05, Strike = 0.05, Rest = 0.11, Back = 0.12;
        static double Blow => Raise + Hold + Strike + Rest;

        struct V { public double x, y; public V(double x, double y) { this.x = x; this.y = y; } public static V operator +(V a, V b) => new V(a.x + b.x, a.y + b.y); public static V operator -(V a, V b) => new V(a.x - b.x, a.y - b.y); public static V operator *(V a, double k) => new V(a.x * k, a.y * k); }
        static V L(V a, V b, double k) => a + (b - a) * k;
        static V Dir(double a) => new V(Math.Cos(a * Math.PI / 180), Math.Sin(a * Math.PI / 180));

        /// (tip, control point) of the lash for a moment u of a blow, relative to his centre
        static (V tip, V ctrl) Pose(V hand, double face, double u, double side, bool first)
        {
            V fwd = Dir(face), sd = Dir(face + side * 90);                     // side -1 = his right
            V front = fwd * 0.95 + sd * 0.12, back = fwd * -0.34 + sd * 0.06, shoulder = sd * 0.34 + fwd * 0.05;
            V start = first ? hand + fwd * 0.25 : back;
            if (u < Raise)
            {
                double k = u / Raise; k = 1 - (1 - k) * (1 - k);
                V tip = L(start, front, k);
                return (tip, L(first ? (hand + tip) * 0.5 : shoulder, (hand + tip) * 0.5, k));
            }
            if (u < Raise + Hold) { V tip = front; return (tip, (hand + tip) * 0.5 + fwd * 0.04); }
            if (u < Raise + Hold + Strike)
            {
                double k = (u - Raise - Hold) / Strike; k *= k;
                V tip = L(front, back, k);
                return (tip, L((hand + front) * 0.5, shoulder, k));
            }
            return (back, shoulder);
        }

        public static void Run(string root)
        {
            int blows = 3; double end = blows * Blow, face = 90;
            int frames = 32, cols = 16, cw = 90; double sc = 42;
            var img = new Img(cols * cw, (frames / cols) * cw);
            for (int i = 0; i < img.P.Length; i++) img.P[i] = C.Hex("#28261f");
            for (int fi = 0; fi < frames; fi++)
            {
                double t = fi * (end + Back) / (frames - 1);
                double ox = (fi % cols) * cw + cw / 2.0, oy = (fi / cols) * cw + cw / 2.0;
                img.FillCircle(ox, oy, 0.3 * sc, C.Hex("#55524a"));
                img.FillCircle(ox, oy - 0.3 * sc, 3, C.Hex("#8a8578"));
                V hand = Dir(face - 90) * 0.22 + Dir(face) * 0.1;
                int blow = Math.Min(blows - 1, (int)Math.Floor(t / Blow));
                double u = Math.Min(t - blow * Blow, Blow), side = blow % 2 == 0 ? -1 : 1;
                double back = Math.Clamp((t - end) / Back, 0, 1);
                var (tip, ctrl) = Pose(hand, face, u, side, blow == 0);
                tip = L(tip, hand, back * 0.8); ctrl = L(ctrl, hand, back * 0.8);
                V prev = hand;
                for (int i = 1; i <= 20; i++)
                {
                    double q = i / 20.0, m = 1 - q;
                    V p = hand * (m * m) + ctrl * (2 * m * q) + tip * (q * q);
                    img.Line(ox + prev.x * sc, oy - prev.y * sc, ox + p.x * sc, oy - p.y * sc, new C(220, 160, 90, (int)(255 * (1 - back))), 2);
                    prev = p;
                }
                img.FillCircle(ox + hand.x * sc, oy - hand.y * sc, 3, C.Hex("#ffffff"));
                double dt = (end + Back) / (frames - 1);
                for (int k = 0; k < blows; k++) if (t >= k * Blow + Raise + Hold + Strike && t < k * Blow + Raise + Hold + Strike + dt) img.FillCircle(ox - cw / 2.0 + 8, oy - cw / 2.0 + 8, 5, C.Hex("#ff3030"));
            }
            img.Save(Path.Combine(root, "preview", "selflash_sim.png"));
            Console.WriteLine("selflash ok");
        }
    }
}
