using System;
using System.Collections.Generic;

namespace ArtGen
{
    public static class FP
    {
        public static readonly C Out = C.Hex("#110a0b");
        // the class colours (frame / rim of the skill icons): #f20905 #ae1e1c #5e0a0a #4a0903
        public static readonly C F1 = C.Hex("#f20905"), F2 = C.Hex("#ae1e1c"), F3 = C.Hex("#5e0a0a"), F4 = C.Hex("#4a0903");
        public static readonly Ramp Blood = new Ramp("#2a0406", "#4a0903", "#5e0a0a", "#8a1414", "#ae1e1c", "#d8140e", "#f20905", "#ff6a4a");
        public static readonly Ramp Skin = new Ramp("#4a2618", "#7a4430", "#a8664a", "#cd8a64", "#e8ae86", "#f6cfab");
        public static readonly Ramp Sack = new Ramp("#1a130f", "#2b2019", "#3f3126", "#564535", "#6f5b46", "#8a7459", "#a48d6f");
        public static readonly Ramp Rag = new Ramp("#0f1111", "#1a1d1c", "#262b29", "#343b38", "#455049", "#58645c");
        public static readonly Ramp Ochre = new Ramp("#2e1d07", "#5a3c10", "#86601c", "#b08530", "#d3ac52", "#ecd38a");
        public static readonly Ramp Leather = new Ramp("#140d0a", "#24170f", "#3b2618", "#573925", "#744f33", "#946a47");
        public static readonly Ramp Rust = new Ramp("#1c0f09", "#3a1d10", "#5c2f17", "#82451f", "#a8632c", "#c98a4e", "#e2b27a");
        public static readonly Ramp Iron = new Ramp("#121318", "#22242b", "#363a43", "#4e535e", "#6c727e", "#959ba6", "#c3c8cf");
        public static readonly Ramp Wood = new Ramp("#1c120c", "#33211a", "#4f3424", "#6c4a31", "#8a6142", "#a87d56");
        public static readonly Ramp Gold = new Ramp("#2e1f06", "#5c400e", "#8c6418", "#b98c26", "#ddb648", "#f6e08e");
        public static readonly Ramp Bone = new Ramp("#2d2620", "#51463b", "#7a6c5c", "#a69580", "#cdbea6", "#ece2cc");
        public static readonly Ramp Poison = new Ramp("#0e2410", "#1d4a1a", "#2f7426", "#4ea43a", "#86d45a", "#c4f29a");
        public static readonly Ramp Holy = new Ramp("#3a2a06", "#8a6a12", "#d8b23a", "#f4dc78", "#fff6c8");
        public static readonly Ramp Fire = new Ramp("#2a0606", "#6e1410", "#b02a14", "#e05a1a", "#f8a03a", "#ffe08a");
    }

    /// The Flagellant: skill icons (34x34 like the game's), weapons (24x24 icons + 32x32 in-hand), the guild building
    /// icon (24x24), the body parts (game rig sizes) and blood effects.
    public static partial class FlagArt
    {
        static readonly C W = new C(255, 255, 255);
        const double TAU = Math.PI * 2;

        // ---------------------------------------------------------------- helpers
        static void Shade(Img c, Ramp r, int seed, double lightX = -0.6, double lightY = -0.8, double noise = 0.35)
        {
            // recolour every opaque non-outline pixel of c by a ramp: light from the top-left, a bit of grain
            int x0 = c.W, y0 = c.H, x1 = 0, y1 = 0;
            for (int y = 0; y < c.H; y++) for (int x = 0; x < c.W; x++) if (c.Get(x, y).a > 0) { x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); }
            double cx = (x0 + x1) / 2.0, cy = (y0 + y1) / 2.0, rx = Math.Max(1, (x1 - x0) / 2.0), ry = Math.Max(1, (y1 - y0) / 2.0);
            for (int y = 0; y < c.H; y++)
                for (int x = 0; x < c.W; x++)
                {
                    if (c.Get(x, y).a == 0) continue;
                    double dx = (x - cx) / rx, dy = (y - cy) / ry;
                    double l = 0.55 - 0.35 * (dx * lightX + dy * lightY) * -1 + (Noise.Fbm(x * 0.6, y * 0.6, seed, 2) - 0.5) * noise;
                    // edge darkening
                    bool edge = c.Get(x - 1, y).a == 0 || c.Get(x + 1, y).a == 0 || c.Get(x, y - 1).a == 0 || c.Get(x, y + 1).a == 0;
                    if (edge) l -= 0.18;
                    c.Put(x, y, r.At(Math.Clamp(l, 0, 0.999)));
                }
        }

        static Img Layer(int w, int h) => new Img(w, h);

        static void Stamp(Img dst, Img src) { for (int i = 0; i < dst.P.Length; i++) if (src.P[i].a > 0) dst.P[i] = src.P[i]; }

        static List<(double, double)> Pts(params double[] v) { var l = new List<(double, double)>(); for (int i = 0; i + 1 < v.Length; i += 2) l.Add((v[i], v[i + 1])); return l; }

        /// a flail head: a ball with spikes
        static Img SpikedBall(int size, double cx, double cy, double r, Ramp metal, int spikes, double spikeLen, int seed, bool blood = false)
        {
            var c = Layer(size, size);
            for (int i = 0; i < spikes; i++)
            {
                double a = i * TAU / spikes + 0.3;
                double bx = cx + Math.Cos(a) * r * 0.7, by = cy + Math.Sin(a) * r * 0.7;
                double tx = cx + Math.Cos(a) * (r + spikeLen), ty = cy + Math.Sin(a) * (r + spikeLen);
                double px = -Math.Sin(a) * r * 0.42, py = Math.Cos(a) * r * 0.42;
                c.FillPoly(Pts(bx + px, by + py, tx, ty, bx - px, by - py), metal[3]);
            }
            c.FillCircle(cx, cy, r, metal[3]);
            Shade(c, metal, seed);
            c.Put((int)(cx - r * 0.35), (int)(cy - r * 0.4), metal[metal.N - 1]);
            if (blood)
            {
                var rng = new Rng(seed);
                for (int k = 0; k < (int)(r * 3); k++)
                {
                    int x = (int)(cx + rng.R(-r, r)), y = (int)(cy + rng.R(-r * 0.2, r));
                    if (c.Get(x, y).a > 0) c.Put(x, y, FP.Blood[rng.I(2, 5)]);
                }
            }
            return c;
        }

        /// a chain between two points (alternating links)
        static void Chain(Img c, double x0, double y0, double x1, double y1, Ramp metal, double sag = 0)
        {
            double len = Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
            int n = Math.Max(2, (int)(len / 1.6));
            for (int i = 0; i <= n; i++)
            {
                double t = i / (double)n;
                double x = x0 + (x1 - x0) * t, y = y0 + (y1 - y0) * t + Math.Sin(t * Math.PI) * sag;
                c.Put((int)Math.Round(x), (int)Math.Round(y), i % 2 == 0 ? metal[4] : metal[2]);
            }
        }

        static void Handle(Img c, double x0, double y0, double x1, double y1, Ramp wood, double w = 1.6)
        {
            c.Line(x0, y0, x1, y1, wood[2], w);
            c.Line(x0 - 0.4, y0 - 0.4, x1 - 0.4, y1 - 0.4, wood[4], 0.8);
        }

        static Img Finish(Img c, bool outline = true)
        {
            if (outline) c.Outline(FP.Out);
            return c;
        }

        // ---------------------------------------------------------------- figure (for the "self" skills)
        /// a penitent seen from the front: hood, bare torso, rags; body filled with one ramp, rim in the class colours
        static Img Figure(int s, Ramp fill, bool armUp, bool fistToChest, int seed)
        {
            var c = Layer(s, s);
            double cx = s / 2.0;
            // legs + skirt
            c.FillPoly(Pts(cx - 7, 22, cx + 7, 22, cx + 9, 31, cx + 4, 30, cx + 2, 33, cx - 1, 30, cx - 4, 33, cx - 6, 30, cx - 9, 31), fill[2]);
            // torso
            c.FillPoly(Pts(cx - 6, 11, cx + 6, 11, cx + 7, 22, cx - 7, 22), fill[3]);
            // hood (pointed)
            c.FillPoly(Pts(cx - 4.5, 11, cx - 4, 5, cx, 1.5, cx + 4, 5, cx + 4.5, 11), fill[4]);
            // arms
            if (armUp) c.Line(cx + 6, 12, cx + 11, 2, fill[3], 2.6); else c.Line(cx + 6, 12, cx + 10, 21, fill[3], 2.6);
            if (fistToChest) { c.Line(cx - 6, 12, cx - 3, 18, fill[3], 2.6); c.FillCircle(cx - 1.5, 16, 2.4, fill[4]); }
            else c.Line(cx - 6, 12, cx - 10, 21, fill[3], 2.6);
            Shade(c, fill, seed, -0.4, -0.9, 0.25);
            // rim light in the class colours
            var rim = c.Clone();
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
                {
                    if (c.Get(x, y).a == 0) continue;
                    bool l = c.Get(x - 1, y).a == 0, u = c.Get(x, y - 1).a == 0, r = c.Get(x + 1, y).a == 0;
                    if (u || l) rim.Put(x, y, FP.F1); else if (r) rim.Put(x, y, FP.F2);
                }
            // dark hood opening
            rim.FillEllipse(cx, 7.5, 2.2, 2.2, FP.F4);
            return rim;
        }

        static Img Icon34() => new Img(34, 34);

        static void Drops(Img c, int seed, int n, double x0, double y0, double x1, double y1)
        {
            var r = new Rng(seed);
            for (int i = 0; i < n; i++)
            {
                double x = r.R(x0, x1), y = r.R(y0, y1), s = r.R(0.8, 1.6);
                c.FillCircle(x, y, s, FP.Blood[r.I(4, 7)]);
                c.Put((int)x, (int)(y - s - 0.5), FP.Blood[5]);
            }
        }

        // ---------------------------------------------------------------- skill icons
        public static Img BloodStrike()
        {
            var c = Icon34();
            // the crimson swing behind
            for (int y = 0; y < 34; y++) for (int x = 0; x < 34; x++)
                {
                    double dx = x - 9, dy = y - 25, d = Math.Sqrt(dx * dx + dy * dy), a = Math.Atan2(-dy, dx);
                    if (a < 0.05 || a > 1.65 || d < 15 || d > 23) continue;
                    double t = a / 1.65;
                    c.Put(x, y, FP.Blood.At(0.3 + 0.65 * (1 - Math.Abs(d - 19) / 4.5) * (0.5 + 0.5 * t)));
                }
            // handle + chain + ball
            Handle(c, 4, 31, 11, 22, FP.Wood, 2.2);
            Chain(c, 11, 22, 19, 13, FP.Iron, 1.5);
            Stamp(c, SpikedBall(34, 23, 10, 4.6, FP.Iron, 8, 2.6, 3, true));
            Drops(c, 7, 6, 18, 18, 32, 31);
            return Finish(c);
        }

        /// a scourge: a bound grip and leather tails whipping out, every tail ending in a cracker dripping poison
        public static Img PoisonLash()
        {
            var c = Icon34();
            double gx = 8, gy = 26;                                       // where the tails leave the grip
            var tips = new List<(double x, double y)>();
            double[][] tails = { new[] { -1.25, 21.0, 1.0 }, new[] { -0.88, 24.0, -1.0 }, new[] { -0.52, 25.0, 1.0 }, new[] { -0.18, 22.0, -1.0 } };
            foreach (var tl in tails)
            {
                double a0 = tl[0], len = tl[1], curl = tl[2];
                double px = gx, py = gy;
                for (double t = 0; t <= 1.0001; t += 0.02)
                {
                    double a = a0 + Math.Sin(t * Math.PI * 2) * 0.12 * curl;
                    double x = gx + Math.Cos(a) * len * t, y = gy + Math.Sin(a) * len * t;
                    c.Line(px, py, x, y, FP.Leather[t < 0.45 ? 4 : 3], Math.Max(0.7, 1.9 * (1 - t * 0.65)));
                    px = x; py = y;
                }
                tips.Add((px, py));
            }
            // the grip, bound in ochre, toward the bottom-left corner
            Handle(c, 2, 32, gx + 1, gy - 1, FP.Leather, 3);
            c.Line(gx - 1.5, gy + 1.5, gx + 1.5, gy - 1.5, FP.Ochre[3], 3.4);
            c.Put((int)gx, (int)gy, FP.Ochre[5]);
            Finish(c);
            // the crackers: a knot at every tip, glowing with poison pixels and a drip
            var r = new Rng(31);
            foreach (var (x, y) in tips)
            {
                c.FillCircle(x, y, 1.6, FP.Poison[2]);
                c.Put((int)Math.Round(x), (int)Math.Round(y), FP.Poison[5]);
                for (int k = 0; k < 3; k++) c.Put((int)Math.Round(x + r.R(-2.2, 2.2)), (int)Math.Round(y + r.R(-2.2, 2.2)), FP.Poison[r.I(3, 6)]);
                c.Put((int)Math.Round(x), (int)Math.Round(y) + 2, FP.Poison[4]);
                c.Put((int)Math.Round(x), (int)Math.Round(y) + 3, FP.Poison[3]);
            }
            // class rim along the upper edges of the leather
            for (int y = 1; y < 33; y++) for (int x = 1; x < 33; x++) if (c.Get(x, y).Equals(FP.Out) && c.Get(x, y + 1).a > 0 && !c.Get(x, y + 1).Equals(FP.Out) && c.Get(x, y - 1).a == 0) c.Put(x, y, FP.F4);
            return c;
        }

        public static Img SinnerStrike()
        {
            var c = Icon34();
            // three afterimages of the flail ball, left to right, the last one solid
            for (int k = 0; k < 3; k++)
            {
                double x = 7 + k * 9, y = 24 - k * 7;
                var ball = SpikedBall(34, x, y, 3.2 + k * 0.5, k == 2 ? FP.Iron : FP.Blood, 7, 2, 20 + k, k == 2);
                if (k < 2) for (int i = 0; i < ball.P.Length; i++) if (ball.P[i].a > 0) ball.P[i] = ball.P[i].A(110 + k * 60);
                for (int i = 0; i < c.P.Length; i++) if (ball.P[i].a > 0) c.P[i] = ball.P[i];
                // impact star
                c.Line(x - 5, y + 5, x - 2, y + 2, FP.F1, 1);
            }
            // teleport streaks
            c.Line(4, 31, 12, 26, FP.F2, 1); c.Line(13, 28, 21, 21, FP.F2, 1); c.Line(22, 21, 28, 14, FP.F1, 1);
            Finish(c);
            return c;
        }

        public static Img BloodRain()
        {
            var c = Icon34();
            var r = new Rng(5);
            for (int k = 0; k < 4; k++)
            {
                double sx = 4 + k * 6, sy = 3 + (k % 2) * 4;
                double ex = sx + 9 + r.R(-1, 2), ey = sy + 18 + r.R(-2, 2);
                // curved homing trail
                for (double t = 0; t <= 1; t += 0.02)
                {
                    double x = sx + (ex - sx) * t + Math.Sin(t * Math.PI) * 3, y = sy + (ey - sy) * t;
                    c.Blend((int)x, (int)y, FP.Blood[3 + (int)(t * 3)], 0.4 + 0.6 * t);
                }
                // the clot: a drop pointing down the trail
                c.FillCircle(ex, ey, 2.4, FP.Blood[5]);
                c.FillPoly(Pts(ex - 2, ey - 0.5, ex + 2, ey - 0.5, ex - 1.5, ey - 5), FP.Blood[5]);
                c.Put((int)ex - 1, (int)ey - 1, FP.Blood[7]);
            }
            Finish(c);
            return c;
        }

        public static Img MartyrHand()
        {
            var c = Icon34();
            // the yellow-orange swing triangle in front
            for (int y = 0; y < 34; y++) for (int x = 0; x < 34; x++)
                {
                    double dx = x - 6, dy = y - 27;
                    if (dx < 0 || -dy < 0) continue;
                    double a = Math.Atan2(-dy, dx), d = Math.Sqrt(dx * dx + dy * dy);
                    if (a < 0.15 || a > 1.4 || d > 28) continue;
                    double t = d / 28;
                    if (t < 0.45) continue;
                    c.Put(x, y, (t > 0.92 ? FP.Fire[5] : FP.Fire.At(0.4 + t * 0.5)).A((int)(120 + 135 * t)));
                }
            // the bloody clawed hand (palm + five fingers), in the class reds
            var h = Layer(34, 34);
            h.FillEllipse(12, 22, 5.5, 6.5, FP.Blood[4]);
            double[][] f = { new[] { 7.0, 18, 4, 11 }, new[] { 10.0, 16, 8.5, 7 }, new[] { 13.0, 16, 13.5, 6 }, new[] { 16.0, 17, 18, 9 }, new[] { 17.0, 22, 21, 19 } };
            foreach (var g in f) { h.Line(g[0], g[1], g[2], g[3], FP.Blood[4], 2.2); h.Put((int)g[2], (int)g[3], FP.Bone[5]); }
            h.Line(9, 28, 15, 33, FP.Blood[3], 4);       // wrist
            Shade(h, FP.Blood, 9, -0.5, -0.8, 0.3);
            Stamp(c, h);
            Drops(c, 13, 4, 6, 27, 18, 33);
            Finish(c);
            return c;
        }

        public static Img DivinePunishment()
        {
            var c = Icon34();
            // the beam of wrath from above
            for (int y = 0; y < 34; y++) for (int x = 10; x < 24; x++)
                {
                    double t = Math.Abs(x - 17) / 7.0;
                    if (t > 1) continue;
                    c.Put(x, y, FP.Holy.At(0.95 - t * 0.7).A((int)(90 + 140 * (1 - t))));
                }
            var fig = Figure(34, FP.Blood, false, true, 4);
            Stamp(c, fig);
            // shock rings at the fist
            c.Ring(15, 16, 5, 5.9, FP.Holy[3].A(220), true);
            c.Ring(15, 16, 8, 8.8, FP.Holy[2].A(150), true);
            Finish(c);
            return c;
        }

        public static Img Torment()
        {
            var c = Icon34();
            var r = new Rng(17);
            // blood streams pulled into the centre
            for (int k = 0; k < 7; k++)
            {
                double a = k * TAU / 7 + 0.2;
                for (double t = 0; t <= 1; t += 0.03)
                {
                    double d = 16 - t * 9, aa = a + t * 1.1;
                    c.Blend((int)(17 + Math.Cos(aa) * d), (int)(17 + Math.Sin(aa) * d), FP.Blood[3 + (int)(t * 3)], 0.5 + t * 0.5);
                }
            }
            // the burst: fire core in a blood shell
            for (int y = 0; y < 34; y++) for (int x = 0; x < 34; x++)
                {
                    double dx = x - 17, dy = y - 17, d = Math.Sqrt(dx * dx + dy * dy), a = Math.Atan2(dy, dx);
                    double edge = 8 + Math.Sin(a * 7) * 1.6 + (Noise.Value(a * 2, 0, 3) - 0.5) * 2;
                    if (d > edge) continue;
                    double t = d / edge;
                    c.Put(x, y, t < 0.45 ? FP.Fire.At(0.95 - t) : FP.Blood.At(0.95 - (t - 0.45) * 1.3));
                }
            Finish(c);
            return c;
        }

        public static Img Ecstasy()
        {
            var c = Icon34();
            var fig = Figure(34, FP.Blood, true, false, 8);
            // the flail over the shoulder, striking the back
            Stamp(c, fig);
            c.Line(28, 2, 31, 9, FP.Iron[4], 1);
            Stamp(c, SpikedBall(34, 30, 11, 1.8, FP.Iron, 6, 1.4, 31));
            // blood spray all around
            var r = new Rng(21);
            for (int i = 0; i < 16; i++)
            {
                double a = r.R(0, TAU), d = r.R(11, 16);
                c.FillCircle(17 + Math.Cos(a) * d, 17 + Math.Sin(a) * d, r.R(0.6, 1.3), FP.Blood[r.I(4, 8)]);
            }
            Finish(c);
            // inner glow (the body heats red)
            for (int y = 0; y < 34; y++) for (int x = 0; x < 34; x++) if (c.Get(x, y).Equals(FP.Blood[2]) || c.Get(x, y).Equals(FP.Blood[3])) c.Put(x, y, FP.Blood[4]);
            return c;
        }

        public static Img BloodCleansing()
        {
            var c = Icon34();
            // the hand in the foreground: forearm from the bottom-left corner, open palm, fingers spread up
            var hand = Layer(34, 34);
            hand.FillPoly(Pts(0, 34, 0, 26, 10, 19, 15, 23, 6, 34), FP.Skin[3]);            // forearm
            hand.FillEllipse(14.5, 17.5, 6.2, 5.6, FP.Skin[3]);                               // palm
            double[][] fingers = { new[] { 10.0, 14, 7, 6 }, new[] { 13.0, 13, 12, 3 }, new[] { 16.0, 13, 17, 3.5 }, new[] { 19.0, 15, 22, 7 } };
            foreach (var f in fingers) hand.Line(f[0], f[1], f[2], f[3], FP.Skin[3], 2.6);
            hand.Line(20, 19, 25, 17, FP.Skin[3], 2.6);                                     // thumb
            Shade(hand, FP.Skin, 2, -0.4, -0.85, 0.2);
            Stamp(c, hand);
            // the cut across the wrist and the blood running down the arm
            c.Line(5, 25, 14, 21, FP.Blood[6], 1.2);
            c.Line(5, 25, 14, 21, FP.Blood[7], 0.4);
            for (int k = 0; k < 4; k++) { int x = 6 + k * 2; c.Line(x, 25 - k, x - 1, 29 - k + (k % 2) * 2, FP.Blood[4 + (k % 2)], 0.8); }
            Drops(c, 3, 4, 2, 30, 12, 34);
            // the dagger drawn across the wrist: hilt top-right, blade down onto the cut
            c.Line(31, 3, 26, 8, FP.Leather[4], 2.4);                                        // grip
            c.Line(23.5, 6, 28, 10.5, FP.Gold[4], 1.2);                                      // guard
            c.Line(25.5, 8.5, 12, 22, FP.Iron[4], 2.1);                                      // blade
            c.Line(25.5, 8.5, 12, 22, FP.Iron[6], 0.6);                                      // edge glint
            c.Put(31, 3, FP.Gold[5]);
            Finish(c);
            // blood on the blade tip, one cleansing sparkle
            c.Put(12, 22, FP.Blood[6]); c.Put(13, 21, FP.Blood[5]);
            c.Put(29, 26, W); c.Put(28, 26, FP.Holy[3]); c.Put(30, 26, FP.Holy[3]); c.Put(29, 25, FP.Holy[3]); c.Put(29, 27, FP.Holy[3]);
            return c;
        }

        // ---------------------------------------------------------------- weapons (head top-left, handle bottom-right)
        public static (Img icon, Img equip) RustyFlail()
        {
            var ic = new Img(24, 24);
            Handle(ic, 13, 13, 22, 22, FP.Wood, 2.6);
            ic.Line(12.5, 12.5, 14.5, 14.5, FP.Rust[4], 3);     // iron cap
            Chain(ic, 12, 12, 9, 9, FP.Rust, 0.5);
            Stamp(ic, SpikedBall(24, 6.5, 6.5, 3.8, FP.Rust, 8, 2, 41));
            Finish(ic);
            var eq = new Img(32, 32);
            Handle(eq, 16, 16, 24, 24, FP.Wood, 1.4);
            Chain(eq, 15, 15, 11, 11, FP.Rust);
            Stamp(eq, SpikedBall(32, 9, 9, 2.4, FP.Rust, 6, 1.4, 42));
            Finish(eq);
            return (ic, eq);
        }

        public static (Img icon, Img equip) RitualScourge()
        {
            // a cat-o'-tails: a thick bound grip and 5 curling knotted leather tails
            void Tails(Img c, double gx, double gy, double len, int n, double spread, double w0)
            {
                for (int k = 0; k < n; k++)
                {
                    double a = 3.93 + (k - (n - 1) / 2.0) * spread;
                    double curl = (k - (n - 1) / 2.0) * 0.9 + 0.6;
                    double px = gx, py = gy;
                    for (double t = 0; t <= 1.001; t += 0.04)
                    {
                        double aa = a + curl * t * t * 0.6;
                        double x = gx + Math.Cos(aa) * len * t, y = gy + Math.Sin(aa) * len * t;
                        c.Line(px, py, x, y, FP.Leather[t < 0.5 ? 4 : 3], Math.Max(0.6, w0 * (1 - t * 0.6)));
                        px = x; py = y;
                        if (Math.Abs(t - 0.52) < 0.021) c.FillCircle(x, y, 0.9, FP.Leather[5]);   // knot
                    }
                    c.Put((int)Math.Round(px), (int)Math.Round(py), FP.Iron[5]);                    // barb
                }
            }
            var ic = new Img(24, 24);
            Tails(ic, 12.5, 12.5, 11, 5, 0.3, 1.3);
            Handle(ic, 13, 13, 21, 21, FP.Leather, 2.6);
            ic.Line(12, 12, 15, 15, FP.Ochre[3], 3);           // binding
            ic.Put(13, 13, FP.Ochre[5]);
            Finish(ic);
            var eq = new Img(32, 32);
            Tails(eq, 16, 16, 9, 4, 0.3, 0.9);
            Handle(eq, 16, 16, 23, 23, FP.Leather, 1.8);
            eq.Line(15.5, 15.5, 17.5, 17.5, FP.Ochre[3], 2);
            Finish(eq);
            return (ic, eq);
        }

        static Img Censer(int size, double cx, double cy, double r, int seed)
        {
            var c = Layer(size, size);
            c.FillEllipse(cx, cy, r, r * 1.15, FP.Gold[3]);
            c.FillPoly(Pts(cx - r * 0.5, cy - r * 1.1, cx + r * 0.5, cy - r * 1.1, cx, cy - r * 1.6), FP.Gold[3]);   // lid spike
            Shade(c, FP.Iron, seed);
            // gilded bands + smoke holes
            for (int x = (int)(cx - r); x <= cx + r; x++) { var y = (int)cy; if (c.Get(x, y).a > 0) c.Put(x, y, FP.Gold[4]); }
            c.Put((int)(cx - r * 0.4), (int)(cy - r * 0.45), FP.Out); c.Put((int)(cx + r * 0.35), (int)(cy - r * 0.45), FP.Out);
            c.Put((int)(cx), (int)(cy + r * 0.5), FP.Fire[4]);
            return c;
        }

        public static (Img icon, Img equip) InquisitorFlail()
        {
            var ic = new Img(24, 24);
            Handle(ic, 13, 13, 22, 22, FP.Wood, 2.6);
            ic.Line(19, 19, 20, 20, FP.Gold[4], 3);
            Chain(ic, 13, 13, 9, 9, FP.Iron, 0.4);
            Chain(ic, 14, 12, 10, 8, FP.Iron, 0.4);
            Stamp(ic, Censer(24, 6.5, 6.5, 4, 51));
            // a wisp of incense
            ic.Put(2, 2, FP.Bone[4].A(160)); ic.Put(3, 1, FP.Bone[5].A(120));
            Finish(ic);
            var eq = new Img(32, 32);
            Handle(eq, 16, 16, 25, 25, FP.Wood, 1.6);
            Chain(eq, 15, 15, 12, 12, FP.Iron);
            Stamp(eq, Censer(32, 9.5, 9.5, 3, 52));
            Finish(eq);
            return (ic, eq);
        }

        public static (Img icon, Img equip) Devastator()
        {
            var ic = new Img(24, 24);
            Handle(ic, 13, 13, 22, 22, FP.Iron, 2.8);
            for (int k = 0; k < 3; k++) { int x = 15 + k * 2; ic.Put(x + 1, x - 1, FP.Iron[5]); }   // spikes on the haft
            Chain(ic, 13, 13, 10, 10, FP.Iron, 0.3);
            Stamp(ic, SpikedBall(24, 7, 7, 5, FP.Iron, 9, 2.4, 61, true));
            Drops(ic, 62, 3, 3, 9, 12, 14);
            Finish(ic);
            var eq = new Img(32, 32);
            Handle(eq, 16, 16, 26, 26, FP.Iron, 1.8);
            Chain(eq, 15, 15, 12, 12, FP.Iron);
            Stamp(eq, SpikedBall(32, 9, 9, 3.6, FP.Iron, 8, 1.8, 63, true));
            Finish(eq);
            return (ic, eq);
        }

        // ---------------------------------------------------------------- guild building icon (24x24)
        /// after the reference: a pale horned sacrificial skull, hollow eyes, a red drip
        public static Img Building()
        {
            var c = new Img(24, 24);
            // horns curving outwards and down
            for (double t = 0; t <= 1; t += 0.02)
            {
                double a = Math.PI * (1.05 + t * 0.85);
                double r = 5 + t * 4.5, w = 2.2 - 1.6 * t;
                double lx = 12 + Math.Cos(a) * r - 1, ly = 9 - Math.Sin(a) * r * 0.75;
                double rx = 24 - lx, ry = ly;
                c.FillCircle(lx, ly, w, FP.Bone[3]); c.FillCircle(rx, ry, w, FP.Bone[3]);
            }
            // the hooded skull
            c.FillEllipse(12, 11, 5.5, 6, FP.Bone[4]);
            c.FillPoly(Pts(8, 14, 16, 14, 14.5, 20, 9.5, 20), FP.Bone[4]);
            Shade(c, FP.Bone, 70, -0.4, -0.9, 0.25);
            // eye holes and nose
            c.FillEllipse(9.6, 12.3, 1.6, 1.8, FP.Out); c.FillEllipse(14.4, 12.3, 1.6, 1.8, FP.Out);
            c.Put(12, 15, FP.Out); c.Put(12, 16, FP.Out);
            // teeth
            for (int x = 10; x <= 14; x += 2) c.Put(x, 19, FP.Bone[1]);
            // the sacrificial blood
            c.Put(9, 14, FP.Blood[6]); c.Put(9, 15, FP.Blood[5]); c.Put(9, 16, FP.Blood[4]);
            c.Put(15, 14, FP.Blood[6]); c.Put(15, 15, FP.Blood[5]);
            Finish(c);
            return c;
        }

        // ---------------------------------------------------------------- body parts (game rig, top-down, shoulders along x)
        /// the hood over the head (replaces the hair; head part 24x24, the head itself ~10x12 in the middle)
        public static Img Hood()
        {
            var c = new Img(24, 24);
            c.FillEllipse(12, 12, 6, 6.8, FP.Sack[4]);
            c.FillPoly(Pts(9.5, 5.6, 14.5, 5.6, 12, 3), FP.Sack[4]);       // the pointed back of the hood
            Shade(c, FP.Sack, 80, -0.5, -0.8, 0.45);
            // seam along the crown + stitches
            for (int y = 4; y <= 17; y++) c.Put(12, y, FP.Sack[1]);
            for (int y = 6; y <= 16; y += 3) { c.Put(11, y, FP.Sack[5]); c.Put(13, y, FP.Sack[5]); }
            // blood smears
            c.Put(9, 13, FP.Blood[4]); c.Put(9, 14, FP.Blood[3]); c.Put(10, 14, FP.Blood[4]); c.Put(15, 9, FP.Blood[3]);
            // the eye opening in front (bottom of the sprite = front): dark slit
            for (int x = 10; x <= 14; x++) c.Put(x, 18, FP.Out);
            Finish(c);
            return c;
        }

        /// the torso (replaces the body armour): bare shoulders with lash wounds, a sackcloth mantle round the neck
        public static Img Torso()
        {
            var c = new Img(29, 15);
            c.FillPoly(Pts(1, 6, 4, 2, 10, 1, 19, 1, 25, 2, 28, 6, 27, 11, 22, 13, 7, 13, 2, 11), FP.Skin[3]);
            Shade(c, FP.Skin, 90, -0.4, -0.9, 0.3);
            // lash wounds (diagonal red lines) and blood streaks
            var r = new Rng(91);
            for (int k = 0; k < 7; k++)
            {
                int x = r.I(3, 25), y = r.I(3, 10);
                c.Line(x, y, x + 3, y + 2, FP.Blood[5], 0.6);
                if (r.P(0.5)) c.Put(x + 1, y + 2, FP.Blood[4]);
            }
            // the mantle of the hood: a sackcloth collar around the neck (middle)
            var m = new Img(29, 15);
            m.FillEllipse(14.5, 6.5, 8.5, 5.5, FP.Sack[4]);
            for (int x = 6; x <= 23; x += 2) m.Put(x, 11 + (x % 4 == 0 ? 1 : 0), FP.Sack[4]);   // ragged edge
            Shade(m, FP.Sack, 92, -0.5, -0.8, 0.5);
            Stamp(c, m);
            c.Put(9, 9, FP.Blood[4]); c.Put(20, 5, FP.Blood[3]);
            // the rope over one shoulder
            c.Line(20, 2, 25, 10, FP.Ochre[3], 0.8);
            Finish(c);
            return c;
        }

        /// bare upper arm with a lash scar (16x16 like the game's)
        public static Img UpperArm()
        {
            var c = new Img(16, 16);
            c.FillPoly(Pts(4, 12, 6, 13, 12, 6, 12, 3, 9, 3, 3, 9), FP.Skin[3]);
            Shade(c, FP.Skin, 100, -0.5, -0.8, 0.3);
            c.Line(6, 9, 9, 6, FP.Blood[5], 0.6);
            Finish(c);
            return c;
        }

        /// forearm wrapped in black leather straps (7x10)
        public static Img Forearm()
        {
            var c = new Img(9, 12);
            c.FillEllipse(4.5, 6, 2.6, 5, FP.Leather[2]);
            Shade(c, FP.Leather, 110, -0.6, -0.6, 0.3);
            for (int y = 2; y <= 10; y += 2) { c.Put(3, y, FP.Leather[4]); c.Put(5, y + 1, FP.Leather[4]); }
            c.Put(4, 5, FP.Iron[5]);                                      // a rivet
            Finish(c);
            return c;
        }

        /// a hand: one red with blood, one wrapped
        public static Img Hand(bool bloody)
        {
            var c = new Img(9, 9);
            c.FillEllipse(4.5, 4.5, 2.6, 2.6, bloody ? FP.Blood[5] : FP.Skin[3]);
            Shade(c, bloody ? FP.Blood : FP.Skin, bloody ? 121 : 120, -0.6, -0.6, 0.3);
            if (bloody) { c.Put(3, 3, FP.Blood[7]); }
            else { c.Put(3, 5, FP.Leather[3]); c.Put(5, 5, FP.Leather[3]); }
            Finish(c);
            return c;
        }

        /// dark rag-wrapped leg (8x10)
        public static Img Leg()
        {
            var c = new Img(10, 12);
            c.FillEllipse(5, 6, 3, 5, FP.Rag[3]);
            Shade(c, FP.Rag, 130, -0.6, -0.6, 0.35);
            for (int y = 3; y <= 9; y += 3) { c.Put(3, y, FP.Rag[1]); c.Put(4, y, FP.Rag[1]); c.Put(5, y + 1, FP.Rag[5]); }
            Finish(c);
            return c;
        }

        /// pelvis: the ochre sash and the ragged dark skirt (15x9)
        public static Img Pelvis()
        {
            var c = new Img(19, 12);
            // ragged skirt
            c.FillPoly(Pts(1, 3, 18, 3, 18, 8, 16, 10, 14, 8, 12, 11, 9, 8, 7, 11, 5, 8, 2, 10, 1, 7), FP.Rag[3]);
            Shade(c, FP.Rag, 140, -0.5, -0.8, 0.4);
            // the sash
            for (int x = 1; x <= 17; x++) { c.Put(x, 3, FP.Ochre[4]); c.Put(x, 4, FP.Ochre[3]); c.Put(x, 5, FP.Ochre[2]); }
            c.Put(13, 6, FP.Ochre[3]); c.Put(13, 7, FP.Ochre[2]); c.Put(14, 8, FP.Ochre[2]);   // the hanging knot
            Finish(c);
            return c;
        }

        // ---------------------------------------------------------------- effects (white = tinted in the game)
        public static Img BloodDrop()
        {
            var c = new Img(8, 8);
            c.FillCircle(4, 4.8, 2, FP.Blood[5]);
            c.Put(4, 2, FP.Blood[5]); c.Put(3, 4, FP.Blood[7]);
            return c;
        }

        public static Img BloodSplat()
        {
            var c = new Img(32, 32);
            var r = new Rng(150);
            c.FillCircle(16, 16, 6, FP.Blood[4]);
            for (int i = 0; i < 14; i++)
            {
                double a = r.R(0, TAU), d = r.R(6, 14), s = r.R(1, 2.6);
                c.FillCircle(16 + Math.Cos(a) * d, 16 + Math.Sin(a) * d, s, FP.Blood[r.I(3, 6)]);
                c.Line(16, 16, 16 + Math.Cos(a) * d, 16 + Math.Sin(a) * d, FP.Blood[4], 1);
            }
            c.FillCircle(14, 14, 2, FP.Blood[6]);
            return c;
        }

        /// the homing blood clot (points to +x)
        public static Img BloodBolt()
        {
            var c = new Img(16, 10);
            for (int x = 0; x < 16; x++)
            {
                double t = x / 15.0, w = 0.6 + 3.4 * Math.Pow(t, 1.4);
                for (int y = 0; y < 10; y++)
                {
                    double dy = Math.Abs(y + 0.5 - 5);
                    if (dy > w) continue;
                    c.Put(x, y, FP.Blood.At(0.35 + 0.6 * t).A((int)(120 + 135 * t)));
                }
            }
            c.FillCircle(12, 5, 2.6, FP.Blood[6]);
            c.Put(13, 4, FP.Blood[7]);
            return c;
        }

        /// the martyr's triangle wedge (white, apex at the left = the hero, opening to +x)
        public static Img Wedge()
        {
            var c = new Img(64, 48);
            for (int y = 0; y < 48; y++)
                for (int x = 0; x < 64; x++)
                {
                    double t = x / 63.0, half = 2 + t * 21, dy = Math.Abs(y + 0.5 - 24);
                    if (dy > half) continue;
                    double edge = Math.Max(dy / half, t);
                    int a = (int)(40 + 200 * Math.Pow(edge, 3));
                    c.Put(x, y, W.A(Math.Min(255, a)));
                }
            return c;
        }

        /// a beam of light from the sky (vertical)
        public static Img Beam()
        {
            var c = new Img(16, 64);
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 16; x++)
                {
                    double t = Math.Abs(x + 0.5 - 8) / 8, v = y / 63.0;
                    if (t > 1) continue;
                    c.Put(x, y, W.A((int)(255 * (1 - t * t) * (0.3 + 0.7 * v))));
                }
            return c;
        }

        /// a ragged lash trail (S-curve, white)
        public static Img LashTrail()
        {
            var c = new Img(64, 24);
            for (double t = 0; t <= 1; t += 0.004)
            {
                double x = 4 + t * 56, y = 12 + Math.Sin(t * TAU * 1.0) * 7 * (1 - t * 0.3);
                double w = 0.6 + 2.2 * Math.Sin(t * Math.PI);
                c.FillCircle(x, y, w, W.A((int)(90 + 165 * t)), true);
            }
            return c;
        }
    }
}
