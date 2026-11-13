using System;
using System.Collections.Generic;
using System.IO;

namespace ArtGen
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string root = args.Length > 0 ? args[0] : ".";
            if (args.Length > 1 && args[1] == "icons2") { IconArt.Run(root); return; }
            if (args.Length > 1 && args[1] == "icons4") { IconArt.Run4(root); return; }
            if (args.Length > 1 && args[1] == "perks") { IconArt.RunPerks(root); return; }
            if (args.Length > 1 && args[1] == "whips") { WhipArt.Run(root); return; }
            if (args.Length > 1 && args[1] == "newflails") { FlagArt.RunNewFlails(root); return; }
            if (args.Length > 1 && args[1] == "whipsim") { WhipSim.Run(root); return; }
            if (args.Length > 1 && args[1] == "hammer") { FlagArt.RunHammer(root); return; }
            if (args.Length > 1 && args[1] == "snap") { FlagArt.RunSnap(root); return; }
            if (args.Length > 1 && args[1] == "selflash") { SelfLashSim.Run(root); return; }
            if (args.Length > 2 && args[1] == "icons3") { IconArt.Run3(root, args[2]); return; }
            string art = Path.Combine(root, "art"), icons = Path.Combine(art, "icons"), parts = Path.Combine(art, "parts"), weap = Path.Combine(art, "weapons"), prev = Path.Combine(root, "preview");
            foreach (var d in new[] { icons, parts, weap, prev }) Directory.CreateDirectory(d);
            var sheet = new List<(string label, Img img, int k)>();

            var skills = new (string id, Func<Img> f)[]
            {
                ("bloodStrike", FlagArt.BloodStrike), ("poisonLash", FlagArt.PoisonLash), ("sinnerStrike", FlagArt.SinnerStrike),
                ("bloodRain", FlagArt.BloodRain), ("martyrHand", FlagArt.MartyrHand), ("divinePunishment", FlagArt.DivinePunishment),
                ("torment", FlagArt.Torment), ("suffering", FlagArt.Ecstasy), ("bloodCleansing", FlagArt.BloodCleansing),
            };
            foreach (var (id, f) in skills) { var im = f(); im.Save(Path.Combine(icons, id + ".png")); sheet.Add((id, im, 4)); }

            var weapons = new (string id, Func<(Img, Img)> f)[]
            {
                ("rustyFlail", FlagArt.RustyFlail), ("ritualScourge", FlagArt.RitualScourge), ("inquisitorFlail", FlagArt.InquisitorFlail), ("devastator", FlagArt.Devastator),
            };
            foreach (var (id, f) in weapons)
            {
                var (ic, eq) = f();
                ic.Save(Path.Combine(weap, id + "_icon.png")); eq.Save(Path.Combine(weap, id + "_equip.png"));
                sheet.Add((id + " icon", ic, 5)); sheet.Add((id + " hand", eq, 4));
            }

            var b = FlagArt.Building(); b.Save(Path.Combine(art, "building.png")); sheet.Add(("building", b, 5));

            void P(string n, Img im) { Directory.CreateDirectory(Path.Combine(prev, "old_parts")); im.Save(Path.Combine(prev, "old_parts", n + ".png")); }   // the real parts come from parts.ps1 (game templates)
            P("hood", FlagArt.Hood()); P("torso", FlagArt.Torso()); P("upperArm", FlagArt.UpperArm()); P("forearm", FlagArt.Forearm());
            P("hand_blood", FlagArt.Hand(true)); P("hand", FlagArt.Hand(false)); P("leg", FlagArt.Leg()); P("pelvis", FlagArt.Pelvis());

            void F(string n, Img im) { im.Save(Path.Combine(art, n + ".png")); sheet.Add((n, im, 3)); }
            F("blood_drop", FlagArt.BloodDrop()); F("blood_splat", FlagArt.BloodSplat()); F("blood_bolt", FlagArt.BloodBolt());
            F("wedge", FlagArt.Wedge()); F("beam", FlagArt.Beam()); F("lash_trail", FlagArt.LashTrail());

            // the assembled penitent seen from above (like in the game): legs, pelvis, arms, torso, hood
            var doll = new Img(48, 48);
            void At(Img im, int cx, int cy) { doll.Draw(im, cx - im.W / 2, cy - im.H / 2); }
            At(FlagArt.Leg(), 17, 30); At(FlagArt.Leg(), 31, 30); At(FlagArt.Pelvis(), 24, 28);
            At(FlagArt.Forearm(), 12, 22); At(FlagArt.Forearm(), 36, 22); At(FlagArt.Hand(true), 11, 16); At(FlagArt.Hand(false), 37, 16);
            At(FlagArt.UpperArm().FlipX(), 15, 23); At(FlagArt.UpperArm(), 33, 23);
            At(FlagArt.Torso(), 24, 25); At(FlagArt.Hood(), 24, 24);
            sheet.Add(("assembled (top view)", doll, 6));

            int pad = 10, x = pad, y = pad, rowH = 0, maxW = 1500;
            var placed = new List<(int, int, Img, string)>();
            foreach (var (label, im, k) in sheet)
            {
                var sc = im.Scale(k);
                if (x + sc.W + pad > maxW) { x = pad; y += rowH + pad + 4; rowH = 0; }
                placed.Add((x, y, sc, label)); x += sc.W + pad; rowH = Math.Max(rowH, sc.H);
            }
            var outImg = new Img(maxW, y + rowH + pad);
            for (int i = 0; i < outImg.P.Length; i++) outImg.P[i] = C.Hex("#4a4038");
            foreach (var (px, py, im, _) in placed) outImg.Draw(im, px, py);
            outImg.Save(Path.Combine(prev, "sheet.png"));
            Console.WriteLine("ok " + sheet.Count);
        }
    }
}
