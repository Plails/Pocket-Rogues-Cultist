using System;
using System.IO;

namespace ArtGen
{
    /// The lash of Poison Lash, piece by piece (a segment repeated along the whip and the tip), one look per weapon:
    /// leather (no flail), rustyFlail, ritualScourge, inquisitorFlail, devastator. Drawn at the heroes' pixel size
    /// (42 px per unit in the game), lying along +x, black outline, poison soaking the tips.
    public static class WhipArt
    {
        static readonly C K = C.Hex("#120a08");
        static readonly C[] Leather = { C.Hex("#3b1d10"), C.Hex("#6a3420"), C.Hex("#94532e"), C.Hex("#bd7a47") };
        static readonly C[] Rust = { C.Hex("#4a230c"), C.Hex("#7d3c14"), C.Hex("#ad5e24"), C.Hex("#d88a48") };
        static readonly C[] Steel = { C.Hex("#34343c"), C.Hex("#5e5e68"), C.Hex("#9a9aa2"), C.Hex("#d6d6d8") };
        static readonly C[] Gold = { C.Hex("#6a3604"), C.Hex("#a56323"), C.Hex("#d9a33d"), C.Hex("#f4dc6a") };
        static readonly C[] DarkIron = { C.Hex("#1c1a1e"), C.Hex("#35323a"), C.Hex("#55515c"), C.Hex("#7d7886") };
        static readonly C[] Blood = { C.Hex("#4a0903"), C.Hex("#8a120c"), C.Hex("#c8241a"), C.Hex("#f0503c") };
        static readonly C[] Poison = { C.Hex("#2f7314"), C.Hex("#55b522"), C.Hex("#8fe63c"), C.Hex("#d6ff9a") };

        static Img Done(Img im) { im.Outline(K); return im; }

        /// a rope piece: thickness 1 or 2, light on top
        static Img Rope(C[] r, int thick)
        {
            var im = new Img(8, thick + 2);
            for (int x = 0; x < 8; x++)
            {
                if (thick == 1) im.Put(x, 1, (x % 3 == 0) ? r[1] : r[2]);
                else { im.Put(x, 1, (x % 3 == 1) ? r[2] : r[3]); im.Put(x, 2, (x % 3 == 2) ? r[0] : r[1]); }
            }
            return Done(im);
        }

        /// a chain piece: an oval ring lying flat and a short bar of the next ring standing on edge
        static Img Link(C[] r, bool big, C[] stain = null)
        {
            // a flat ring (o) with its hole and the next ring standing on edge (=), drawn pixel by pixel
            string[] rows = big
                ? new[] { ".ooooooo...", "oo.....oo..", "o.......o==", "oo.....oo..", ".ooooooo..." }
                : new[] { ".ooooo..", "o.....o=", ".ooooo.." };
            if (!big) rows = new[] { ".ooooo..", "o.....o.", "o.....o=", "o.....o.", ".ooooo.." };
            var im = new Img(rows[0].Length + 2, rows.Length + 2);
            for (int y = 0; y < rows.Length; y++)
                for (int x = 0; x < rows[y].Length; x++)
                {
                    char ch = rows[y][x];
                    if (ch == 'o') im.Put(x + 1, y + 1, y == 0 ? r[3] : y == rows.Length - 1 ? r[0] : x < rows[y].Length / 2 ? r[2] : r[1]);
                    else if (ch == '=') im.Put(x + 1, y + 1, r[2]);
                }
            if (stain != null) { im.Put(4, rows.Length, stain[2]); im.Put(5, rows.Length, stain[1]); im.Put(7, 1, stain[2]); }
            return Done(im);
        }

        static void Spikes(Img im, double cx, double cy, double r, double len, C[] ramp)
        {
            for (int i = 0; i < 8; i++)
            {
                double a = i * Math.PI / 4;
                for (double s = r - 0.5; s <= r + len; s += 0.4)
                {
                    int x = (int)Math.Round(cx + Math.Cos(a) * s), y = (int)Math.Round(cy + Math.Sin(a) * s);
                    im.Put(x, y, s > r + len - 1 ? ramp[3] : ramp[2]);
                }
            }
        }
        static void Ball(Img im, double cx, double cy, double r, C[] ramp)
        {
            for (int y = 0; y < im.H; y++)
                for (int x = 0; x < im.W; x++)
                {
                    double dx = x - cx, dy = y - cy, d = Math.Sqrt(dx * dx + dy * dy);
                    if (d > r) continue;
                    double lit = (-dx - dy) / (r * 1.4);
                    im.Put(x, y, ramp[Math.Clamp((int)Math.Round(1.5 + lit * 1.6), 0, 3)]);
                }
            im.Put((int)(cx - r * 0.45), (int)(cy - r * 0.45), C.Hex("#fbf8f0"));
        }
        static void Poisoned(Img im, params (int x, int y)[] pts)
        {
            int i = 0;
            foreach (var (x, y) in pts) im.Put(x, y, Poison[i++ % 2 == 0 ? 2 : 3]);
        }

        /// the cracker of a leather lash: a knot soaked in poison
        static Img Knot(C[] r)
        {
            var im = new Img(8, 8);
            for (int y = 1; y < 7; y++) for (int x = 1; x < 7; x++) { double dx = x - 3.5, dy = y - 3.5; if (dx * dx + dy * dy <= 6.5) im.Put(x, y, dy < 0 ? r[3] : r[1]); }
            im.Put(3, 3, r[2]); im.Put(4, 4, r[0]);
            Poisoned(im, (5, 2), (5, 5), (2, 5), (6, 4));
            return Done(im);
        }

        public static void Run(string root)
        {
            string dir = Path.Combine(root, "art", "whip");
            Directory.CreateDirectory(dir);
            void Save(string n, Img im) => im.Save(Path.Combine(dir, n + ".png"));

            Save("leather_seg", Rope(Leather, 2));
            Save("leather_tip", Knot(Leather));

            // the ritual scourge: pale braided cords with blood-red beads, each ending in a little barbed hook
            var cord = new Img(8, 3);
            for (int x = 0; x < 8; x++) cord.Put(x, 1, x == 3 ? C.Hex("#b0201a") : (x % 2 == 0 ? C.Hex("#efe2c4") : C.Hex("#c4ad86")));
            Save("ritualScourge_seg", Done(cord));
            var hook = new Img(9, 9);
            foreach (var (x, y) in new[] { (1, 4), (2, 4), (3, 4), (4, 4), (5, 3), (6, 2), (5, 5), (6, 6), (4, 5), (4, 3) }) hook.Put(x, y, (x + y) % 2 == 0 ? C.Hex("#c8241a") : C.Hex("#8a120c"));
            hook.Put(6, 2, C.Hex("#e0e0e0")); hook.Put(6, 6, C.Hex("#e0e0e0"));
            Poisoned(hook, (3, 3));
            Save("ritualScourge_tip", Done(hook));

            // the crack of the lash at its tip: a small white star of the snap
            var cr = new Img(9, 9);
            foreach (var (x, y) in new[] { (4, 0), (4, 1), (4, 2), (4, 3), (4, 4), (4, 5), (4, 6), (4, 7), (4, 8), (0, 4), (1, 4), (2, 4), (3, 4), (5, 4), (6, 4), (7, 4), (8, 4), (2, 2), (6, 2), (2, 6), (6, 6), (3, 3), (5, 3), (3, 5), (5, 5) })
                cr.Put(x, y, (Math.Abs(x - 4) + Math.Abs(y - 4) <= 1) ? C.Hex("#ffffff") : C.Hex("#fff3c8"));
            Save("crack", cr);

            Save("rustyFlail_seg", Link(Rust, false));
            var rt = new Img(13, 13); Spikes(rt, 6, 6, 3.6, 2.2, Rust); Ball(rt, 6, 6, 3.8, Rust); Poisoned(rt, (8, 8), (7, 9), (9, 6)); Save("rustyFlail_tip", Done(rt));

            Save("inquisitorFlail_seg", Link(Steel, false));
            var ce = new Img(13, 15);                                                                // a censer: gold cap, iron body with holes, gold foot
            for (int y = 2; y < 5; y++) for (int x = 4 - (y - 2); x <= 8 + (y - 2); x++) ce.Put(x, y, y == 2 ? Gold[3] : Gold[2]);
            for (int y = 5; y < 11; y++) for (int x = 2; x <= 10; x++) { double dx = (x - 6) / 4.6, dy = (y - 7.5) / 3.6; if (dx * dx + dy * dy <= 1) ce.Put(x, y, x < 6 ? Steel[3] : Steel[1]); }
            foreach (var (x, y) in new[] { (4, 7), (6, 8), (8, 7), (5, 9), (7, 9) }) ce.Put(x, y, C.Hex("#ffb13b"));   // glowing holes
            for (int x = 4; x <= 8; x++) ce.Put(x, 11, Gold[1]);
            ce.Put(6, 1, Gold[3]); ce.Put(6, 0, Gold[2]);
            Poisoned(ce, (9, 10), (3, 6));
            Save("inquisitorFlail_tip", Done(ce));

            Save("devastator_seg", Link(DarkIron, true, Blood));
            var dv = new Img(19, 19); Spikes(dv, 9, 9, 5.6, 3.2, DarkIron); Ball(dv, 9, 9, 5.8, DarkIron);
            for (int y = 9; y < 15; y++) for (int x = 6; x < 15; x++) { double dx = x - 9, dy = y - 9; if (dx * dx + dy * dy <= 30 && y - 9 > -(x - 9) * 0.6 + 1) dv.Put(x, y, (x + y) % 3 == 0 ? Blood[1] : Blood[2]); }
            Poisoned(dv, (13, 7), (12, 5), (5, 12));
            Save("devastator_tip", Done(dv));

            // ---- the five new flails
            var hemp = new[] { C.Hex("#4a3a24"), C.Hex("#6e5a3a"), C.Hex("#937c55"), C.Hex("#c8b184") };
            var bronze = new[] { C.Hex("#3e260c"), C.Hex("#6a4214"), C.Hex("#a8722a"), C.Hex("#e0b060") };
            var thorn = new[] { C.Hex("#1a2a12"), C.Hex("#2c421c"), C.Hex("#4a6a2c"), C.Hex("#7a9c4a") };

            Save("pilgrimFlail_seg", Rope(hemp, 2));
            var pb = new Img(11, 11); Ball(pb, 5, 5, 3.6, Steel); foreach (var (x, y) in new[] { (5, 2), (8, 5), (5, 8), (2, 5) }) pb.Put(x, y, Steel[3]); Poisoned(pb, (7, 7)); Save("pilgrimFlail_tip", Done(pb));

            Save("penitentChain_seg", Link(Steel, false));
            var hk = new Img(12, 11);
            for (int y = 2; y < 9; y++) for (int x = 1; x < 6; x++) { double dx = x - 3, dy = y - 5.5; if (dx * dx + dy * dy <= 7) hk.Put(x, y, dy < 0 ? Steel[3] : Steel[1]); }
            foreach (var (x, y) in new[] { (6, 5), (7, 5), (8, 4), (9, 3), (9, 2), (8, 6), (9, 7), (9, 8) }) hk.Put(x, y, Steel[2]);
            hk.Put(9, 2, Blood[3]); hk.Put(9, 8, Blood[3]); Poisoned(hk, (2, 7));
            Save("penitentChain_tip", Done(hk));

            var vine = new Img(8, 5);
            for (int x = 0; x < 8; x++) { vine.Put(x, 2, (x % 2 == 0) ? thorn[2] : thorn[3]); if (x == 2) vine.Put(x, 1, thorn[1]); if (x == 6) vine.Put(x, 3, thorn[1]); }
            Save("thornStar_seg", Done(vine));
            var ts = new Img(15, 15); Spikes(ts, 7, 7, 4.2, 2.6, bronze); Ball(ts, 7, 7, 4.4, bronze); foreach (var (x, y) in new[] { (5, 9), (9, 5), (7, 3) }) ts.Put(x, y, thorn[3]); Poisoned(ts, (9, 9)); Save("thornStar_tip", Done(ts));

            Save("martyrChain_seg", Link(Steel, true, null));
            var cx = new Img(11, 13);
            for (int y = 1; y < 12; y++) { cx.Put(4, y, Gold[2]); cx.Put(5, y, y < 6 ? Gold[3] : Gold[1]); }
            for (int x = 1; x < 9; x++) { cx.Put(x, 4, Gold[3]); cx.Put(x, 5, Gold[1]); }
            cx.Put(5, 4, Blood[3]); Poisoned(cx, (6, 10));
            Save("martyrChain_tip", Done(cx));

            Save("doomBell_seg", Link(bronze, false));
            var bl = new Img(13, 13);
            for (int y = 2; y < 11; y++) { int hw = 1 + (y - 2) / 2; for (int x = 6 - hw; x <= 6 + hw; x++) bl.Put(x, y, x < 6 ? bronze[3] : y > 8 ? bronze[0] : bronze[1]); }
            for (int x = 1; x <= 11; x++) bl.Put(x, 10, bronze[2]);
            bl.Put(6, 11, Steel[1]); bl.Put(6, 1, bronze[2]); Poisoned(bl, (9, 9));
            Save("doomBell_tip", Done(bl));

            // preview: each whip laid out
            var sheet = new Img(200, 10 * 26);
            for (int i = 0; i < sheet.P.Length; i++) sheet.P[i] = C.Hex("#3c3a34");
            int k = 0;
            foreach (var st in new[] { "leather", "rustyFlail", "ritualScourge", "inquisitorFlail", "devastator", "pilgrimFlail", "penitentChain", "thornStar", "martyrChain", "doomBell" })
            {
                var seg = Img.Load(Path.Combine(dir, st + "_seg.png")); var tip = Img.Load(Path.Combine(dir, st + "_tip.png"));
                int x0 = 4, y0 = k * 26 + 13;
                for (int s = 0; s < 9; s++) sheet.Draw(seg, x0 + s * (seg.W - 3), y0 - seg.H / 2);
                sheet.Draw(tip, x0 + 9 * (seg.W - 3), y0 - tip.H / 2);
                k++;
            }
            sheet.Scale(3).Save(Path.Combine(root, "preview", "whips.png"));
            Console.WriteLine("whips ok");
        }
    }
}
