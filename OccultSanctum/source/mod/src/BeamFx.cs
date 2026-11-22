using System.Collections.Generic;
using UnityEngine;

namespace OccultSanctum
{
    /// Glowing beams, flares and sparks drawn the game's way (its additive particle material where it can be found,
    /// else the plain sprite one): used by the elder's blood beam and by the clash with a hero's breath.
    public static class Glow
    {
        static Material additive, plain;
        static Texture2D beamTex;
        static Sprite dot;

        public static Material Additive
        {
            get
            {
                if (additive != null) return additive;
                try
                {
                    foreach (var r in Resources.FindObjectsOfTypeAll<ParticleSystemRenderer>())
                        if (r != null && r.sharedMaterial != null && r.sharedMaterial.shader != null && r.sharedMaterial.shader.name == "Mobile/Particles/Additive")
                        { additive = new Material(r.sharedMaterial.shader) { name = "Sanctum_Additive" }; break; }
                }
                catch { }
                return additive ?? Plain;
            }
        }

        public static Material Plain
        {
            get
            {
                if (plain != null) return plain;
                var sh = Shader.Find("Sprites/Default");
                if (sh != null) plain = new Material(sh) { name = "Sanctum_Plain" };
                return plain;
            }
        }

        /// a beam strip: bright in the middle across its width, soft at the edges, with flowing streaks along it
        public static Texture2D BeamTex
        {
            get
            {
                if (beamTex != null) return beamTex;
                const int W = 64, H = 16;
                beamTex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Repeat, name = "Sanctum_beam" };
                var px = new Color32[W * H];
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        float v = Mathf.Abs((y + 0.5f) / H - 0.5f) * 2f;                 // 0 in the middle, 1 at the edge
                        float across = Mathf.Pow(Mathf.Clamp01(1f - v), 1.6f);
                        float streak = 0.75f + 0.25f * Mathf.Sin(x / (float)W * Mathf.PI * 6f + y * 0.9f) * Mathf.Sin(x / (float)W * Mathf.PI * 2f);
                        byte a = (byte)(255 * Mathf.Clamp01(across * streak));
                        px[y * W + x] = new Color32(255, 255, 255, a);
                    }
                beamTex.SetPixels32(px); beamTex.Apply();
                return beamTex;
            }
        }

        /// a soft round glow, 1 unit across
        public static Sprite Dot
        {
            get
            {
                if (dot != null) return dot;
                const int N = 64;
                var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "Sanctum_dot" };
                var px = new Color32[N * N];
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(N / 2f, N / 2f)) / (N / 2f);
                        px[y * N + x] = new Color32(255, 255, 255, (byte)(255 * Mathf.Pow(Mathf.Clamp01(1f - d), 2f)));
                    }
                t.SetPixels32(px); t.Apply();
                dot = Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
                dot.hideFlags = HideFlags.HideAndDontSave;
                return dot;
            }
        }

        public static SpriteRenderer Flare(Transform parent, SpriteRenderer like, int order)
        {
            var g = new GameObject("SanctumFlare");
            if (parent != null) g.transform.SetParent(parent, false);
            var sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = Dot; sr.sharedMaterial = Additive;
            if (like != null) { sr.sortingLayerID = like.sortingLayerID; sr.sortingOrder = like.sortingOrder + order; }
            return sr;
        }
    }

    /// one glowing beam: a wide dim halo, a bright body and a white-hot core, flowing from its start to its end,
    /// with a flare at both ends
    public class Beam
    {
        readonly LineRenderer halo, body, core;
        readonly SpriteRenderer startFlare, endFlare;
        readonly GameObject root;
        Color cHalo, cBody, cCore;
        float flow;
        public float EndScale = 1f;                                 // the flare at the end (small in a clash: the knot is there)

        public Beam(string name, SpriteRenderer like, Color halo, Color body, Color core)
        {
            root = new GameObject(name);
            cHalo = halo; cBody = body; cCore = core;
            this.halo = Line("halo", like, 40, Glow.Additive);
            this.body = Line("body", like, 41, Glow.Additive);
            this.core = Line("core", like, 42, Glow.Plain);
            startFlare = Glow.Flare(root.transform, like, 43);
            endFlare = Glow.Flare(root.transform, like, 43);
            Show(false);
        }

        LineRenderer Line(string n, SpriteRenderer like, int order, Material mat)
        {
            var g = new GameObject(n); g.transform.SetParent(root.transform, false);
            var lr = g.AddComponent<LineRenderer>();
            lr.useWorldSpace = true; lr.positionCount = 2; lr.numCapVertices = 4;
            lr.textureMode = LineTextureMode.Tile;
            if (mat != null) { lr.material = new Material(mat); lr.material.mainTexture = Glow.BeamTex; }
            if (like != null) { lr.sortingLayerID = like.sortingLayerID; lr.sortingOrder = like.sortingOrder + order; }
            return lr;
        }

        public void Show(bool on) { if (root != null) root.SetActive(on); }

        /// draws the beam from a to b; width scales it (1 = full), heat 0..1 makes it brighter
        public void Set(Vector2 a, Vector2 b, float width, float heat = 0f)
        {
            if (root == null) return;
            flow += Time.deltaTime * 6f;
            float flick = 1f + 0.12f * Mathf.Sin(Time.time * 37f) + 0.08f * Mathf.Sin(Time.time * 23f + 1.3f);
            float len = Vector2.Distance(a, b);
            void Put(LineRenderer lr, float w, Color c, float tile)
            {
                lr.SetPosition(0, new Vector3(a.x, a.y, 0f)); lr.SetPosition(1, new Vector3(b.x, b.y, 0f));
                lr.widthMultiplier = w * width * flick;
                lr.startColor = c; lr.endColor = new Color(c.r, c.g, c.b, c.a * 0.85f);
                if (lr.material != null) { lr.material.mainTextureScale = new Vector2(Mathf.Max(1f, len / tile), 1f); lr.material.mainTextureOffset = new Vector2(-flow / tile, 0f); }
            }
            Put(halo, 0.62f, new Color(cHalo.r, cHalo.g, cHalo.b, Mathf.Clamp01(cHalo.a + heat * 0.25f)), 1.4f);
            Put(body, 0.3f, cBody, 0.9f);
            Put(core, 0.1f + heat * 0.05f, cCore, 0.6f);
            startFlare.transform.position = new Vector3(a.x, a.y, 0f); startFlare.transform.localScale = Vector3.one * (0.55f + 0.1f * Mathf.Sin(Time.time * 19f)) * width;
            startFlare.color = cBody;
            endFlare.transform.position = new Vector3(b.x, b.y, 0f); endFlare.transform.localScale = Vector3.one * (0.7f + heat * 0.6f + 0.15f * Mathf.Sin(Time.time * 27f)) * width * EndScale;
            endFlare.color = Color.Lerp(cBody, cCore, 0.4f);
        }

        public void Destroy() { if (root != null) Object.Destroy(root); }
    }

    /// short-lived glowing sparks and bursts
    public class Sparks : MonoBehaviour
    {
        class P { public SpriteRenderer sr; public Vector2 v; public float life, max, size; }
        readonly List<P> ps = new List<P>();
        static Sparks inst;

        static Sparks I { get { if (inst == null) inst = new GameObject("SanctumSparks").AddComponent<Sparks>(); return inst; } }

        public static void Burst(Vector2 at, Color c, int n, float speed, float size, SpriteRenderer like, float life = 0.45f)
        {
            var me = I;
            for (int i = 0; i < n; i++)
            {
                var sr = Glow.Flare(me.transform, like, 45);
                sr.transform.position = at; sr.color = c;
                var p = new P { sr = sr, v = Random.insideUnitCircle.normalized * Random.Range(speed * 0.3f, speed), life = Random.Range(life * 0.6f, life), size = size * Random.Range(0.6f, 1.2f) };
                p.max = p.life; me.ps.Add(p);
            }
        }

        /// a ring of light growing out from a point
        public static void Ring(Vector2 at, Color c, float radius, SpriteRenderer like)
        {
            for (int i = 0; i < 24; i++)
            {
                float a = i * 15f * Mathf.Deg2Rad; var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var sr = Glow.Flare(I.transform, like, 44); sr.transform.position = at; sr.color = c;
                I.ps.Add(new P { sr = sr, v = d * radius / 0.4f, life = 0.4f, max = 0.4f, size = 0.45f });
            }
        }

        void Update()
        {
            for (int i = ps.Count - 1; i >= 0; i--)
            {
                var p = ps[i];
                p.life -= Time.deltaTime;
                if (p.sr == null || p.life <= 0f) { if (p.sr != null) Destroy(p.sr.gameObject); ps.RemoveAt(i); continue; }
                p.sr.transform.position += (Vector3)(p.v * Time.deltaTime);
                p.v *= 1f - 3f * Time.deltaTime;
                float k = p.life / p.max;
                p.sr.transform.localScale = Vector3.one * p.size * (0.4f + 0.6f * k);
                var c = p.sr.color; c.a = k; p.sr.color = c;
            }
        }
    }
}
