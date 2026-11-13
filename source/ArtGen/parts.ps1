param([string]$Tpl, [string]$Out, [string]$Prev)
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @"
using System;
using System.Drawing;
using System.Collections.Generic;
public static class Recolor
{
    static Color H(string h) { return ColorTranslator.FromHtml(h); }
    public static int Thr = 38; static bool IsOutline(Color c) { return c.A > 0 && (c.R + c.G + c.B) / 3 < Thr; }
    static double Lum(Color c) { return 0.3 * c.R + 0.59 * c.G + 0.11 * c.B; }

    /// keeps the game's silhouette, outline and light/shadow; swaps the colours for a ramp
    public static Bitmap Map(Bitmap src, string[] ramp, Func<int, int, string[]> region)
    {
        var dst = new Bitmap(src.Width, src.Height);
        double min = 999, max = -1;
        for (int y = 0; y < src.Height; y++) for (int x = 0; x < src.Width; x++)
        { var c = src.GetPixel(x, y); if (c.A == 0 || IsOutline(c)) continue; double l = Lum(c); if (l < min) min = l; if (l > max) max = l; }
        if (max <= min) max = min + 1;
        for (int y = 0; y < src.Height; y++) for (int x = 0; x < src.Width; x++)
        {
            var c = src.GetPixel(x, y);
            if (c.A == 0) { dst.SetPixel(x, y, Color.Transparent); continue; }
            if (IsOutline(c)) { dst.SetPixel(x, y, Color.FromArgb(c.A, 0x14, 0x0c, 0x0c)); continue; }
            var r = region != null ? (region(x, y) ?? ramp) : ramp;
            double t = (Lum(c) - min) / (max - min);
            int i = (int)Math.Round(t * (r.Length - 1));
            var n = H(r[Math.Max(0, Math.Min(r.Length - 1, i))]);
            dst.SetPixel(x, y, Color.FromArgb(c.A, n));
        }
        return dst;
    }

    public static void Dot(Bitmap b, int x, int y, string hex)
    {
        if (x < 0 || y < 0 || x >= b.Width || y >= b.Height) return;
        var c = b.GetPixel(x, y);
        if (c.A == 0 || IsOutline(c)) return;          // details only inside the part, never on the outline
        b.SetPixel(x, y, Color.FromArgb(c.A, H(hex)));
    }

    public static Rectangle Bounds(Bitmap b)
    {
        int x0 = b.Width, y0 = b.Height, x1 = -1, y1 = -1;
        for (int y = 0; y < b.Height; y++) for (int x = 0; x < b.Width; x++) if (b.GetPixel(x, y).A > 0) { x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); }
        return x1 < 0 ? Rectangle.Empty : Rectangle.FromLTRB(x0, y0, x1 + 1, y1 + 1);
    }
}
"@

$Skin   = @("#a8664a", "#cd8a64", "#e8ae86", "#f6cfab")
$Sack   = @("#3f3126", "#564535", "#6f5b46", "#8a7459", "#a48d6f")
$Leath  = @("#1a110c", "#2a1b12", "#3b2618", "#573925")
$Blood  = @("#4a0903", "#7a1010", "#ae1e1c", "#d8140e")
$Rag    = @("#262b29", "#343b38", "#455049", "#58645c")
$red = "#c41410"; $dark = "#5e0a0a"; $ochre = "#b08530"; $ochreL = "#d3ac52"

function T($n) { [System.Drawing.Bitmap]::FromFile((Get-ChildItem $Tpl -Filter "*$n*" | Select-Object -First 1).FullName) }

# torso: the Berserker's default vest silhouette -> bare lashed shoulders, sackcloth mantle round the neck, a rope
$v = T "Body_Armor__Characters_def_BodyBerserker"
$torso = [Recolor]::Map($v, $Skin, [Func[int,int,string[]]]{ param($x, $y) if ([Math]::Abs($x - 12) -le 5 -and $y -le 7) { $Sack } else { $null } })
foreach ($p in @(@(3,6),@(4,7),@(5,8),@(20,6),@(21,7),@(19,8),@(6,4),@(18,4))) { [Recolor]::Dot($torso, $p[0], $p[1], $red) }
foreach ($p in @(@(4,9),@(21,9))) { [Recolor]::Dot($torso, $p[0], $p[1], $dark) }
foreach ($p in @(@(16,9),@(17,10),@(18,11))) { [Recolor]::Dot($torso, $p[0], $p[1], $ochre) }
$torso.Save("$Out\torso.png")

# hood: the hair silhouette in sackcloth, a seam, blood smears
$h = T "Head_Head_Hair__Character_HeadAndHair_4"
[Recolor]::Thr = 14; $hood = [Recolor]::Map($h, $Sack, $null); [Recolor]::Thr = 38
$hb = [Recolor]::Bounds($hood); $cx = [int]($hb.X + $hb.Width / 2)
for ($y = $hb.Y + 1; $y -lt $hb.Bottom - 1; $y += 2) { [Recolor]::Dot($hood, $cx, $y, "#2b2019") }
[Recolor]::Dot($hood, $hb.X + 2, $hb.Y + 6, $red); [Recolor]::Dot($hood, $hb.X + 2, $hb.Y + 7, $dark); [Recolor]::Dot($hood, $hb.Right - 3, $hb.Y + 3, $dark)
$hood.Save("$Out\hood.png")

# upper arm: bare skin with one lash
$u = T "UpperArm_R__Characters_def_UpperR"
$up = [Recolor]::Map($u, $Skin, $null)
$ub = [Recolor]::Bounds($up)
[Recolor]::Dot($up, $ub.X + 4, $ub.Y + 5, $red); [Recolor]::Dot($up, $ub.X + 5, $ub.Y + 4, $red)
$up.Save("$Out\upperArm.png")

# forearm: black leather straps
$a = T "Arm_R__Characters_def_arm_R"
$fa = [Recolor]::Map($a, $Leath, $null)
for ($y = 2; $y -lt $fa.Height - 1; $y += 3) { for ($x = 1; $x -lt $fa.Width - 1; $x++) { [Recolor]::Dot($fa, $x, $y, "#6a4a30") } }
$fa.Save("$Out\forearm.png")

# hands: one red with blood, one in a dark glove
$hd = T "Hand_R__Characters_def_hand_R"
([Recolor]::Map($hd, $Blood, $null)).Save("$Out\hand_blood.png")
([Recolor]::Map($hd, $Leath, $null)).Save("$Out\hand.png")

# legs: dark rag wraps
$l = T "Leg_R__Characters_def_leg_R"
[Recolor]::Thr = 10; $leg = [Recolor]::Map($l, $Rag, $null); [Recolor]::Thr = 38
for ($y = 2; $y -lt $leg.Height - 1; $y += 3) { [Recolor]::Dot($leg, 2, $y, "#4d5753"); [Recolor]::Dot($leg, 3, $y, "#4d5753") }
$leg.Save("$Out\leg.png")

# preview: game parts (top) vs ours (bottom), x8
$pairs = @(@("Body_Armor__Characters_def_BodyBerserker", "torso"), @("Head_Head_Hair__Character_HeadAndHair_4", "hood"), @("UpperArm_R__Characters_def_UpperR", "upperArm"), @("Arm_R__Characters_def_arm_R", "forearm"), @("Hand_R__Characters_def_hand_R", "hand_blood"), @("Hand_R__Characters_def_hand_R", "hand"), @("Leg_R__Characters_def_leg_R", "leg"))
$s = 8; $W = 0; foreach ($p in $pairs) { $W += 26 * $s + 10 }
$bmp = New-Object System.Drawing.Bitmap $W, (2 * 26 * $s + 30)
$g = [System.Drawing.Graphics]::FromImage($bmp); $g.Clear([System.Drawing.Color]::FromArgb(255, 74, 64, 56)); $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
$x = 5
foreach ($p in $pairs) {
  $a1 = T $p[0]; $b1 = [System.Drawing.Bitmap]::FromFile("$Out\$($p[1]).png")
  $g.DrawImage($a1, $x, 5, $a1.Width * $s, $a1.Height * $s); $g.DrawImage($b1, $x, 26 * $s + 20, $b1.Width * $s, $b1.Height * $s)
  $x += 26 * $s + 10; $a1.Dispose(); $b1.Dispose()
}
$bmp.Save("$Prev\parts_vs_game.png"); $g.Dispose(); $bmp.Dispose()
"ok"
