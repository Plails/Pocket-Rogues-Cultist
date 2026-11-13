using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace ArtGen
{
    public struct C
    {
        public byte r, g, b, a;
        public C(int r, int g, int b, int a = 255) { this.r = (byte)Math.Clamp(r, 0, 255); this.g = (byte)Math.Clamp(g, 0, 255); this.b = (byte)Math.Clamp(b, 0, 255); this.a = (byte)Math.Clamp(a, 0, 255); }
        public static C Hex(string h)
        {
            h = h.TrimStart('#');
            int v = Convert.ToInt32(h.Substring(0, 6), 16);
            int a = h.Length >= 8 ? Convert.ToInt32(h.Substring(6, 2), 16) : 255;
            return new C((v >> 16) & 255, (v >> 8) & 255, v & 255, a);
        }
        public C A(int a) => new C(r, g, b, a);
        public static C Lerp(C x, C y, double t) => new C((int)Math.Round(x.r + (y.r - x.r) * t), (int)Math.Round(x.g + (y.g - x.g) * t), (int)Math.Round(x.b + (y.b - x.b) * t), (int)Math.Round(x.a + (y.a - x.a) * t));
        public static readonly C Clear = new C(0, 0, 0, 0);
    }

    public class Ramp
    {
        public C[] c;
        public Ramp(params string[] hex) { c = new C[hex.Length]; for (int i = 0; i < hex.Length; i++) c[i] = C.Hex(hex[i]); }
        public C this[int i] => c[Math.Clamp(i, 0, c.Length - 1)];
        public int N => c.Length;
        /// t in 0..1 -> ramp color (quantized, pixel-art style)
        public C At(double t) => this[(int)Math.Floor(Math.Clamp(t, 0, 0.9999) * c.Length)];
    }

    public static class Pal
    {
        public static readonly C Outline = C.Hex("#16121c");
        // cool ruin stone of the warden
        public static readonly Ramp Stone = new Ramp("#1d1924", "#2d2a37", "#3f3b4b", "#54505f", "#6a6676", "#837f8e", "#9f9ba8", "#bcb8c3");
        // warm floor stone of the borderlands
        public static readonly Ramp Floor = new Ramp("#1f1a19", "#2e2725", "#3d3431", "#4d423d", "#5d514a", "#6e6158", "#807165", "#938373");
        public static readonly Ramp Rim = new Ramp("#17141a", "#221e26", "#2e2933", "#3b3540", "#48424e", "#57505d");
        public static readonly Ramp Moss = new Ramp("#23281f", "#2f3828", "#3c4832", "#4b583c", "#5c6a48");
        public static readonly Ramp Iron = new Ramp("#141419", "#25262e", "#383a45", "#4f525f", "#6b6f7e", "#8e93a3");
        public static readonly Ramp Bone = new Ramp("#2b2522", "#433a35", "#5e534b", "#7b6f64", "#998c7e", "#b9ab9a", "#d6c9b7");
        public static readonly Ramp Void = new Ramp("#1e0b38", "#35145f", "#53208f", "#7531c4", "#9a52ff", "#bd8bff", "#dcc0ff", "#f6eeff");
        public static readonly Ramp Cyan = new Ramp("#0d2a3a", "#145a72", "#1f8fae", "#3cc4e0", "#8ce9f6", "#e2fbff");
    }

    public class Rng
    {
        private uint s;
        public Rng(int seed) { s = (uint)seed * 2654435761u + 12345u; if (s == 0) s = 1; }
        public uint NextU() { s ^= s << 13; s ^= s >> 17; s ^= s << 5; return s; }
        public double D() => (NextU() & 0xFFFFFF) / (double)0x1000000;
        public double R(double a, double b) => a + (b - a) * D();
        public int I(int a, int bExcl) => a + (int)(D() * (bExcl - a));
        public bool P(double p) => D() < p;
    }

    public static class Noise
    {
        static double Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (double)0x1000000;
            }
        }
        static double Smooth(double t) => t * t * (3 - 2 * t);
        public static double Value(double x, double y, int seed)
        {
            int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y);
            double tx = Smooth(x - xi), ty = Smooth(y - yi);
            double a = Hash(xi, yi, seed), b = Hash(xi + 1, yi, seed), c = Hash(xi, yi + 1, seed), d = Hash(xi + 1, yi + 1, seed);
            return (a + (b - a) * tx) * (1 - ty) + (c + (d - c) * tx) * ty;
        }
        public static double Fbm(double x, double y, int seed, int oct = 3)
        {
            double sum = 0, amp = 0.5, f = 1, norm = 0;
            for (int i = 0; i < oct; i++) { sum += amp * Value(x * f, y * f, seed + i * 31); norm += amp; amp *= 0.5; f *= 2; }
            return sum / norm;
        }
        public static double White(int x, int y, int seed) => Hash(x, y, seed);
    }

    public class Img
    {
        public int W, H;
        public C[] P;
        public Img(int w, int h) { W = w; H = h; P = new C[w * h]; }
        public bool In(int x, int y) => x >= 0 && y >= 0 && x < W && y < H;
        public C Get(int x, int y) => In(x, y) ? P[y * W + x] : C.Clear;
        public void Put(int x, int y, C c) { if (In(x, y)) P[y * W + x] = c; }
        public void Blend(int x, int y, C c, double alpha = 1)
        {
            if (!In(x, y)) return;
            double sa = c.a / 255.0 * alpha;
            if (sa <= 0) return;
            C d = P[y * W + x];
            double da = d.a / 255.0;
            double oa = sa + da * (1 - sa);
            if (oa <= 0) { P[y * W + x] = C.Clear; return; }
            int r = (int)Math.Round((c.r * sa + d.r * da * (1 - sa)) / oa);
            int g = (int)Math.Round((c.g * sa + d.g * da * (1 - sa)) / oa);
            int b = (int)Math.Round((c.b * sa + d.b * da * (1 - sa)) / oa);
            P[y * W + x] = new C(r, g, b, (int)Math.Round(oa * 255));
        }
        public void FillCircle(double cx, double cy, double r, C c, bool blend = false)
        {
            for (int y = (int)Math.Floor(cy - r - 1); y <= (int)Math.Ceiling(cy + r + 1); y++)
                for (int x = (int)Math.Floor(cx - r - 1); x <= (int)Math.Ceiling(cx + r + 1); x++)
                {
                    double dx = x + 0.5 - cx, dy = y + 0.5 - cy;
                    if (dx * dx + dy * dy <= r * r) { if (blend) Blend(x, y, c); else Put(x, y, c); }
                }
        }
        public void FillEllipse(double cx, double cy, double rx, double ry, C c, bool blend = false)
        {
            for (int y = (int)Math.Floor(cy - ry - 1); y <= (int)Math.Ceiling(cy + ry + 1); y++)
                for (int x = (int)Math.Floor(cx - rx - 1); x <= (int)Math.Ceiling(cx + rx + 1); x++)
                {
                    double dx = (x + 0.5 - cx) / rx, dy = (y + 0.5 - cy) / ry;
                    if (dx * dx + dy * dy <= 1) { if (blend) Blend(x, y, c); else Put(x, y, c); }
                }
        }
        public void Ring(double cx, double cy, double r0, double r1, C c, bool blend = false)
        {
            for (int y = (int)Math.Floor(cy - r1 - 1); y <= (int)Math.Ceiling(cy + r1 + 1); y++)
                for (int x = (int)Math.Floor(cx - r1 - 1); x <= (int)Math.Ceiling(cx + r1 + 1); x++)
                {
                    double dx = x + 0.5 - cx, dy = y + 0.5 - cy, d = Math.Sqrt(dx * dx + dy * dy);
                    if (d >= r0 && d <= r1) { if (blend) Blend(x, y, c); else Put(x, y, c); }
                }
        }
        public void Line(double x0, double y0, double x1, double y1, C c, double w = 1, bool blend = false)
        {
            double len = Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
            int steps = Math.Max(1, (int)Math.Ceiling(len * 2));
            var done = new HashSet<int>();
            for (int i = 0; i <= steps; i++)
            {
                double t = i / (double)steps, x = x0 + (x1 - x0) * t, y = y0 + (y1 - y0) * t;
                if (w <= 1.01)
                {
                    int px = (int)Math.Floor(x), py = (int)Math.Floor(y);
                    if (done.Add(py * 100000 + px)) { if (blend) Blend(px, py, c); else Put(px, py, c); }
                }
                else
                {
                    double r = w / 2;
                    for (int yy = (int)Math.Floor(y - r); yy <= (int)Math.Ceiling(y + r); yy++)
                        for (int xx = (int)Math.Floor(x - r); xx <= (int)Math.Ceiling(x + r); xx++)
                        {
                            double dx = xx + 0.5 - x, dy = yy + 0.5 - y;
                            if (dx * dx + dy * dy <= r * r && done.Add(yy * 100000 + xx)) { if (blend) Blend(xx, yy, c); else Put(xx, yy, c); }
                        }
                }
            }
        }
        public static bool InPoly(List<(double x, double y)> pts, double px, double py)
        {
            bool inside = false;
            for (int i = 0, j = pts.Count - 1; i < pts.Count; j = i++)
            {
                if (((pts[i].y > py) != (pts[j].y > py)) && (px < (pts[j].x - pts[i].x) * (py - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x)) inside = !inside;
            }
            return inside;
        }
        public void FillPoly(List<(double x, double y)> pts, Func<int, int, C> color, bool blend = false)
        {
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (var p in pts) { minX = Math.Min(minX, p.x); minY = Math.Min(minY, p.y); maxX = Math.Max(maxX, p.x); maxY = Math.Max(maxY, p.y); }
            for (int y = (int)Math.Floor(minY); y <= (int)Math.Ceiling(maxY); y++)
                for (int x = (int)Math.Floor(minX); x <= (int)Math.Ceiling(maxX); x++)
                    if (InPoly(pts, x + 0.5, y + 0.5)) { var c = color(x, y); if (blend) Blend(x, y, c); else Put(x, y, c); }
        }
        public void FillPoly(List<(double x, double y)> pts, C c, bool blend = false) => FillPoly(pts, (x, y) => c, blend);

        /// 1px outline around opaque pixels (only on transparent pixels)
        public void Outline(C oc, int alphaThreshold = 100, bool diagonals = false)
        {
            var add = new List<int>();
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (P[y * W + x].a >= alphaThreshold) continue;
                    bool near = false;
                    for (int dy = -1; dy <= 1 && !near; dy++)
                        for (int dx = -1; dx <= 1 && !near; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            if (!diagonals && dx != 0 && dy != 0) continue;
                            if (Get(x + dx, y + dy).a >= alphaThreshold) near = true;
                        }
                    if (near) add.Add(y * W + x);
                }
            foreach (var i in add) P[i] = oc;
        }
        public Img Clone() { var n = new Img(W, H); Array.Copy(P, n.P, P.Length); return n; }
        public Img FlipX() { var n = new Img(W, H); for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) n.P[y * W + (W - 1 - x)] = P[y * W + x]; return n; }
        public void Draw(Img src, int ox, int oy)
        {
            for (int y = 0; y < src.H; y++) for (int x = 0; x < src.W; x++) { var c = src.P[y * src.W + x]; if (c.a > 0) Blend(ox + x, oy + y, c); }
        }
        public Img Scale(int k)
        {
            var n = new Img(W * k, H * k);
            for (int y = 0; y < n.H; y++) for (int x = 0; x < n.W; x++) n.P[y * n.W + x] = P[(y / k) * W + (x / k)];
            return n;
        }
        public Img OnBg(C bg)
        {
            var n = new Img(W, H);
            for (int i = 0; i < P.Length; i++) n.P[i] = bg;
            n.Draw(this, 0, 0);
            return n;
        }

        /// reads an 8-bit RGBA / RGB PNG (non-interlaced), as Unity and System.Drawing write them
        public static Img Load(string path)
        {
            var b = File.ReadAllBytes(path);
            int pos = 8, w = 0, h = 0, ct = 6;
            using var idat = new MemoryStream();
            while (pos < b.Length)
            {
                int len = (b[pos] << 24) | (b[pos + 1] << 16) | (b[pos + 2] << 8) | b[pos + 3];
                string type = System.Text.Encoding.ASCII.GetString(b, pos + 4, 4);
                if (type == "IHDR") { w = (b[pos + 8] << 24) | (b[pos + 9] << 16) | (b[pos + 10] << 8) | b[pos + 11]; h = (b[pos + 12] << 24) | (b[pos + 13] << 16) | (b[pos + 14] << 8) | b[pos + 15]; ct = b[pos + 17]; }
                else if (type == "IDAT") idat.Write(b, pos + 8, len);
                else if (type == "IEND") break;
                pos += 12 + len;
            }
            int bpp = ct == 6 ? 4 : 3, stride = w * bpp;
            idat.Position = 0;
            using var z = new ZLibStream(idat, CompressionMode.Decompress);
            using var raw = new MemoryStream(); z.CopyTo(raw);
            var d = raw.ToArray();
            var img = new Img(w, h);
            var prev = new byte[stride]; var cur = new byte[stride];
            int o = 0;
            for (int y = 0; y < h; y++)
            {
                int f = d[o++];
                for (int i = 0; i < stride; i++)
                {
                    int x = d[o++], a = i >= bpp ? cur[i - bpp] : 0, up = prev[i], c = i >= bpp ? prev[i - bpp] : 0;
                    int v = f switch { 1 => x + a, 2 => x + up, 3 => x + ((a + up) >> 1), 4 => x + Paeth(a, up, c), _ => x };
                    cur[i] = (byte)v;
                }
                for (int xx = 0; xx < w; xx++) img.P[y * w + xx] = new C(cur[xx * bpp], cur[xx * bpp + 1], cur[xx * bpp + 2], bpp == 4 ? cur[xx * bpp + 3] : 255);
                var t = prev; prev = cur; cur = t;
            }
            return img;
        }
        static int Paeth(int a, int b, int c) { int p = a + b - c, pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c); return pa <= pb && pa <= pc ? a : pb <= pc ? b : c; }

        // ---------------- PNG ----------------
        static readonly uint[] CrcTable = MakeCrc();
        static uint[] MakeCrc() { var t = new uint[256]; for (uint n = 0; n < 256; n++) { uint c = n; for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1; t[n] = c; } return t; }
        static uint Crc(byte[] buf, int off, int len) { uint c = 0xFFFFFFFFu; for (int i = off; i < off + len; i++) c = CrcTable[(c ^ buf[i]) & 0xFF] ^ (c >> 8); return c ^ 0xFFFFFFFFu; }
        static void Chunk(Stream s, string type, byte[] data)
        {
            var len = BitConverter.GetBytes(data.Length); Array.Reverse(len); s.Write(len, 0, 4);
            var buf = new byte[4 + data.Length];
            for (int i = 0; i < 4; i++) buf[i] = (byte)type[i];
            Array.Copy(data, 0, buf, 4, data.Length);
            s.Write(buf, 0, buf.Length);
            var crc = BitConverter.GetBytes(Crc(buf, 0, buf.Length)); Array.Reverse(crc); s.Write(crc, 0, 4);
        }
        public void Save(string path)
        {
            using var fs = File.Create(path);
            fs.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, 0, 8);
            var ihdr = new byte[13];
            var w = BitConverter.GetBytes(W); Array.Reverse(w); Array.Copy(w, 0, ihdr, 0, 4);
            var h = BitConverter.GetBytes(H); Array.Reverse(h); Array.Copy(h, 0, ihdr, 4, 4);
            ihdr[8] = 8; ihdr[9] = 6; ihdr[10] = 0; ihdr[11] = 0; ihdr[12] = 0;
            Chunk(fs, "IHDR", ihdr);
            var raw = new byte[(W * 4 + 1) * H];
            int o = 0;
            for (int y = 0; y < H; y++)
            {
                raw[o++] = 0;
                for (int x = 0; x < W; x++) { var c = P[y * W + x]; raw[o++] = c.r; raw[o++] = c.g; raw[o++] = c.b; raw[o++] = c.a; }
            }
            using var ms = new MemoryStream();
            using (var z = new ZLibStream(ms, CompressionLevel.Optimal, true)) z.Write(raw, 0, raw.Length);
            Chunk(fs, "IDAT", ms.ToArray());
            Chunk(fs, "IEND", new byte[0]);
        }
    }
}
