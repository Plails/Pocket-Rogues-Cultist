using System;
using System.IO;

namespace ArtGen
{
    /// Five more flails for the Cultist (between the first four in strength), drawn like the first ones:
    /// a 24x24 icon and a 32x32 in-hand sprite, head top-left, handle bottom-right.
    public static partial class FlagArt
    {
        static readonly Ramp Bronze = new Ramp("#1e1206", "#3e260c", "#6a4214", "#93601e", "#bd8432", "#e0b060", "#f6d998");
        static readonly Ramp Thorn = new Ramp("#0c140a", "#1a2a12", "#2c421c", "#3f5c26", "#587a34", "#7a9c4a");
        static readonly Ramp Hemp = new Ramp("#2a2014", "#4a3a24", "#6e5a3a", "#937c55", "#b8a174", "#d8c69a");

        static Img Studded(int size, double cx, double cy, double r, Ramp metal, int seed)
        {
            var c = Layer(size, size);
            c.FillCircle(cx, cy, r, metal[3]);
            Shade(c, metal, seed);
            for (int i = 0; i < 4; i++) { double a = i * Math.PI / 2 + 0.6; c.Put((int)Math.Round(cx + Math.Cos(a) * r * 0.55), (int)Math.Round(cy + Math.Sin(a) * r * 0.55), metal[metal.N - 1]); }
            return c;
        }

        /// Pilgrim's Flail: a plain wooden haft, a hemp cord and a studded iron ball
        public static (Img icon, Img equip) PilgrimFlail()
        {
            var ic = new Img(24, 24);
            Handle(ic, 13, 13, 22, 22, Hemp, 2.4);
            ic.Line(17, 17, 18, 18, FP.Leather[3], 3);                                        // cord binding
            Chain(ic, 12, 12, 9, 9, Hemp, 0.6);
            Stamp(ic, Studded(24, 6.5, 6.5, 3.6, FP.Iron, 71));
            Finish(ic);
            var eq = new Img(32, 32);
            Handle(eq, 16, 16, 24, 24, Hemp, 1.4);
            Chain(eq, 15, 15, 11.5, 11.5, Hemp);
            Stamp(eq, Studded(32, 9.5, 9.5, 2.4, FP.Iron, 72));
            Finish(eq);
            return (ic, eq);
        }

        /// Penitent's Chain: a bare iron chain from a leather grip, ending in a hooked weight that tears the flesh
        public static (Img icon, Img equip) PenitentChain()
        {
            Img Hook(int size, double cx, double cy, double s)
            {
                var c = Layer(size, size);
                c.FillCircle(cx, cy, s, FP.Iron[3]);
                c.Line(cx - s * 0.2, cy - s * 0.2, cx - s * 1.6, cy - s * 0.3, FP.Iron[4], 1.2);   // the hook
                c.Line(cx - s * 1.6, cy - s * 0.3, cx - s * 1.5, cy + s * 0.7, FP.Iron[4], 1.0);
                Shade(c, FP.Iron, 81);
                c.Put((int)(cx - s * 1.5), (int)(cy + s * 0.7), FP.Blood[6]);
                return c;
            }
            var ic = new Img(24, 24);
            Handle(ic, 15, 15, 22, 22, FP.Leather, 2.6);
            ic.Line(14.5, 14.5, 16, 16, FP.Iron[4], 3);
            Chain(ic, 14, 14, 8, 8, FP.Iron, 0.8);
            Chain(ic, 13, 15, 7.5, 9, FP.Iron, 0.4);
            Stamp(ic, Hook(24, 6.5, 6.5, 2.6));
            Finish(ic);
            var eq = new Img(32, 32);
            Handle(eq, 17, 17, 24, 24, FP.Leather, 1.6);
            Chain(eq, 16, 16, 11, 11, FP.Iron, 0.5);
            Stamp(eq, Hook(32, 9.5, 9.5, 1.8));
            Finish(eq);
            return (ic, eq);
        }

        /// Thorned Morningstar: a ball of dark bronze grown over with thorns, on a vine-wrapped haft
        public static (Img icon, Img equip) ThornStar()
        {
            var ic = new Img(24, 24);
            Handle(ic, 13, 13, 22, 22, FP.Wood, 2.6);
            for (int k = 0; k < 4; k++) ic.Put(14 + k * 2, 15 + k * 2, Thorn[4]);              // the vine around the haft
            Chain(ic, 12.5, 12.5, 9.5, 9.5, FP.Iron, 0.4);
            Stamp(ic, SpikedBall(24, 7, 7, 4.2, Bronze, 11, 2.2, 91));
            foreach (var (x, y) in new[] { (4, 9), (9, 4), (6, 5), (8, 9) }) ic.Put(x, y, Thorn[4]);
            Finish(ic);
            var eq = new Img(32, 32);
            Handle(eq, 16, 16, 25, 25, FP.Wood, 1.6);
            Chain(eq, 15, 15, 12, 12, FP.Iron);
            Stamp(eq, SpikedBall(32, 9.5, 9.5, 2.8, Bronze, 9, 1.6, 92));
            eq.Put(8, 9, Thorn[4]); eq.Put(10, 8, Thorn[4]);
            Finish(eq);
            return (ic, eq);
        }

        /// Martyr's Fetters: a heavy iron shackle chain and a gilded cross for a weight
        public static (Img icon, Img equip) MartyrChain()
        {
            Img Cross(int size, double cx, double cy, double s)
            {
                var c = Layer(size, size);
                c.Line(cx, cy - s * 1.4, cx, cy + s * 1.4, FP.Gold[3], s * 0.9);
                c.Line(cx - s * 0.95, cy - s * 0.35, cx + s * 0.95, cy - s * 0.35, FP.Gold[3], s * 0.9);
                Shade(c, FP.Gold, 101);
                c.Put((int)cx, (int)(cy - s * 0.35), FP.Blood[6]);
                return c;
            }
            var ic = new Img(24, 24);
            Handle(ic, 14, 14, 22, 22, FP.Iron, 2.8);
            ic.FillCircle(19, 19, 2.2, FP.Iron[2]); ic.FillCircle(19, 19, 1.0, C.Clear);       // the shackle ring
            Chain(ic, 13.5, 13.5, 9, 9, FP.Iron, 0.3);
            Chain(ic, 14.5, 12.5, 10, 8, FP.Iron, 0.3);
            Stamp(ic, Cross(24, 6.5, 6.5, 3.0));
            Finish(ic);
            var eq = new Img(32, 32);
            Handle(eq, 16, 16, 25, 25, FP.Iron, 1.8);
            Chain(eq, 15, 15, 12, 12, FP.Iron);
            Stamp(eq, Cross(32, 9.5, 9.5, 2.1));
            Finish(eq);
            return (ic, eq);
        }

        /// Doomsday Bell: a bronze bell on a chain, its toll stuns
        public static (Img icon, Img equip) DoomBell()
        {
            Img Bell(int size, double cx, double cy, double s, int seed)
            {
                var c = Layer(size, size);
                c.FillPoly(Pts(cx - s * 0.55, cy - s * 0.9, cx + s * 0.55, cy - s * 0.9, cx + s * 1.1, cy + s * 0.9, cx - s * 1.1, cy + s * 0.9), Bronze[3]);
                c.FillCircle(cx, cy - s * 0.9, s * 0.55, Bronze[3]);
                Shade(c, Bronze, seed);
                for (int x = (int)(cx - s * 1.1); x <= cx + s * 1.1; x++) if (c.Get(x, (int)(cy + s * 0.6)).a > 0) c.Put(x, (int)(cy + s * 0.6), Bronze[5]);
                c.Put((int)cx, (int)(cy + s * 1.2), FP.Iron[2]);                                  // the clapper
                return c;
            }
            var ic = new Img(24, 24);
            Handle(ic, 13, 13, 22, 22, FP.Wood, 2.6);
            ic.Line(20, 20, 21, 21, Bronze[4], 3);
            Chain(ic, 12.5, 12.5, 9.5, 9.5, Bronze, 0.4);
            var b = Bell(24, 7, 6.5, 3.6, 111);
            // the bell hangs turned towards the head end
            Stamp(ic, b);
            Finish(ic);
            var eq = new Img(32, 32);
            Handle(eq, 16, 16, 25, 25, FP.Wood, 1.6);
            Chain(eq, 15, 15, 12, 12, Bronze);
            Stamp(eq, Bell(32, 9.5, 9.5, 2.5, 112));
            Finish(eq);
            return (ic, eq);
        }

        public static void RunNewFlails(string root)
        {
            string weap = Path.Combine(root, "art", "weapons");
            Directory.CreateDirectory(weap);
            var list = new (string id, Func<(Img, Img)> f)[] { ("pilgrimFlail", PilgrimFlail), ("penitentChain", PenitentChain), ("thornStar", ThornStar), ("martyrChain", MartyrChain), ("doomBell", DoomBell) };
            var sheet = new Img(list.Length * 140, 140);
            for (int i = 0; i < sheet.P.Length; i++) sheet.P[i] = C.Hex("#3c3a34");
            int x = 4;
            foreach (var (id, f) in list)
            {
                var (ic, eq) = f();
                ic.Save(Path.Combine(weap, id + "_icon.png")); eq.Save(Path.Combine(weap, id + "_equip.png"));
                sheet.Draw(ic.Scale(4), x, 4); sheet.Draw(eq.Scale(1), x + 100, 100);
                x += 140;
            }
            sheet.Save(Path.Combine(root, "preview", "new_flails.png"));
            Console.WriteLine("new flails ok");
        }
    }
}
