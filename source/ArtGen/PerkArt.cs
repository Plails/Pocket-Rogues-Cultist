using System;
using System.Collections.Generic;
using System.IO;

namespace ArtGen
{
    /// The Cultist's two class perk pictures, drawn like the game's icons (one big object, black outline, lit top-left)
    public static partial class IconArt
    {
        /// Sinful Vessel: the bare chest of a penitent; the left half still in a cracked iron breastplate that is
        /// breaking away, the bare right half covered in lash wounds with blood running down (after MRX's sketch)
        public static Img PerkVessel()
        {
            var c = new Canvas();
            // the torso: neck, wide shoulders, narrowing to the waist
            var torso = New();
            Capsule(17, 1.0, 17, 5, 3.6, torso);                                                          // neck
            Poly(torso, 13, 3.5, 21, 3.5, 28, 7.5, 6, 7.5);                                               // trapezius slopes
            Poly(torso, 2.5, 10, 7, 6.5, 27, 6.5, 31.5, 10, 29, 19, 24.5, 33.5, 9.5, 33.5, 5, 19);
            Circle(5.8, 11, 3.8, torso); Circle(28.2, 11, 3.8, torso);                                   // shoulders
            c.Part(torso, SkinR, 2);
            Rim(c, torso, SkinR[4]);
            // muscles: chest line, belly lines in a darker skin tone
            for (int x = 19; x <= 27; x++) c.Px(x, 15 + (x > 23 ? 1 : 0), SkinR[1]);
            c.Px(18, 18, SkinR[1]); c.Px(18, 19, SkinR[1]); c.Px(18, 22, SkinR[1]); c.Px(18, 23, SkinR[1]);
            for (int x = 19; x <= 23; x++) { c.Px(x, 21, SkinR[1]); c.Px(x, 26, SkinR[1]); }
            c.Px(22, 12, SkinR[4]); c.Px(23, 12, SkinR[4]);                                              // light on the chest
            // the breastplate on his left half (the viewer's left), broken along a jagged edge
            var plate = Poly(null, 1.5, 10, 7, 5.5, 15.5, 5.5, 14, 9, 17, 12, 13.5, 15.5, 16.5, 19, 13, 23, 15.5, 27, 12.5, 30.5, 13.5, 34, 9, 34, 4.5, 19);
            Circle(5.8, 11, 3.8, plate);
            plate = And(plate, torso);
            c.Part(plate, IronR, 2);
            Rim(c, plate, IronR[5]);
            for (int y = 9; y < 31; y += 7) { c.Px(6, y, IronR[1]); c.Px(7, y, IronR[4]); }                 // rivets
            c.Part(Capsule(5, 22, 10.5, 25.5, 0.5), IronR, 0, false, 0);                                 // a crack
            c.Part(Capsule(9, 13, 12, 11, 0.5), IronR, 0, false, 0);
            // a shard of the plate falling away
            var shard = Poly(null, 19, 28.5, 23.5, 27, 22, 31.5);
            c.Part(shard, IronR, 1);
            // lash wounds across the bare half, blood running down from them
            foreach (var (x0, y0, x1, y1) in new[] { (17.5, 10.5, 27.5, 7.5), (16.5, 16.5, 28.5, 13.0), (17.0, 23.5, 26.5, 20.0) })
                c.Part(Capsule(x0, y0, x1, y1, 0.85), BloodR, 1, false, 3);
            c.Part(Taper(Pts(25.5, 8.5, 25.8, 11.5, 25.5, 13.5), 1.6, 1.0), BloodR, 1, false, 2);
            c.Part(Taper(Pts(20.0, 15.5, 20.3, 19.5, 20.0, 22.0), 1.8, 1.1), BloodR, 1, false, 2);
            c.Part(Taper(Pts(24.0, 21.5, 24.3, 26.5, 23.8, 30.5), 1.8, 1.0), BloodR, 1, false, 2);
            Drop(c, 28.5, 18.5, 0.9, BloodR);
            // blood splashed on the iron too
            c.Px(12, 16, BloodR[3]); c.Px(13, 17, BloodR[2]); c.Px(11, 17, BloodR[2]); c.Px(14, 25, BloodR[3]);
            return c.Done();
        }

        /// Blood Feast: a golden chalice overflowing with blood, standing in a wide pool of blood, drops flying up
        public static Img PerkFeast()
        {
            var c = new Canvas();
            // the pool on the ground
            var pool = Ellipse(17, 27.5, 15.5, 5.3, 0);
            Ellipse(6.5, 25.5, 4.5, 3.2, -15, pool); Ellipse(27.5, 29.5, 5.5, 3.4, 10, pool);
            c.Part(pool, BloodR, 2);
            Rim(c, pool, BloodR[4]);
            c.Part(Ellipse(9.5, 26, 3, 0.9, -8), BloodR, 0, false, 4);                                   // shine on the pool
            c.Part(Ellipse(25, 29.5, 2.2, 0.7, 6), BloodR, 0, false, 4);
            // the chalice: foot, stem with a knot, the cup
            c.Part(Ellipse(17, 26.5, 6.2, 2.0, 0), GoldR, 1);
            c.Part(Capsule(17, 18, 17, 25.5, 1.6), GoldR, 1);
            c.Part(Ellipse(17, 20.5, 2.6, 1.6, 0), GoldR, 1);
            var cup = Poly(null, 7, 6.5, 27, 6.5, 25.5, 12, 21.5, 16.5, 17, 18, 12.5, 16.5, 8.5, 12);
            c.Part(cup, GoldR, 2);
            Rim(c, cup, GoldR[5]);
            c.Part(Capsule(10.5, 12.5, 14.5, 15.5, 0.5), GoldR, 0, false, 1);                             // engraved band
            c.Px(12, 9, GoldR[5]); c.Px(11, 10, GoldR[5]);
            // the gold lip of the cup, blood filling it to the brim and running over one side
            c.Part(Ellipse(17, 6.5, 10.2, 2.6, 0), GoldR, 1);
            c.Part(Ellipse(17, 6.6, 8.4, 1.6, 0), BloodR, 0, false, 2);
            c.Part(Ellipse(14.5, 6.2, 3.0, 0.6, 0), BloodR, 0, false, 4);
            c.Part(Taper(Pts(23.5, 7.0, 24.3, 10.5, 23.6, 15.0, 22.0, 18.0), 2.8, 1.4), BloodR, 1);
            c.Part(Taper(Pts(10.5, 8.0, 10.0, 10.5, 10.6, 12.5), 2.0, 1.2), BloodR, 1);
            // a few drops splashing up from the pool
            Drop(c, 30.0, 19.5, 1.0, BloodR);
            Drop(c, 4.0, 18.5, 0.9, BloodR);
            return c.Done();
        }

        public static void RunPerks(string root)
        {
            string outDir = Path.Combine(root, "art", "perks");
            Directory.CreateDirectory(outDir);
            var list = new (string id, Func<Img> f)[] { ("perk_vessel", PerkVessel), ("perk_feast", PerkFeast) };
            var sheet = new Img(list.Length * (34 * 4 + 8) + 8, 34 * 4 + 16);
            for (int i = 0; i < sheet.P.Length; i++) sheet.P[i] = C.Hex("#3c3a34");
            int x = 8;
            foreach (var (id, f) in list) { var im = f(); im.Save(Path.Combine(outDir, id + ".png")); sheet.Draw(im.Scale(4), x, 8); x += 34 * 4 + 8; }
            sheet.Save(Path.Combine(root, "preview", "perks.png"));
            Console.WriteLine("perks ok");
        }
    }
}
