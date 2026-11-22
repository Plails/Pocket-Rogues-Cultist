using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OccultSanctum
{
    /// The bestiary picture of each of our people, taken from the dressed monster itself (the user: the hand-made
    /// pictures were crooked - weapons and arms hanging apart from the body). A copy of his pictures alone (no scripts)
    /// is stood in the game's own idle pose, turned to face up like the game's own bestiary pictures, and drawn once by
    /// a camera of our own into a picture, pixel for pixel. Taken once for each kind of monster.
    public static class Snapshot
    {
        const int Layer = 31;
        static readonly Dictionary<string, Sprite> made = new Dictionary<string, Sprite>();

        /// the picture of this kind of monster (taken now from `go` if not yet), or `fallback` if it cannot be taken
        public static Sprite Of(GameObject go, string id, Sprite fallback, float size = 1f) => Of(go, id, fallback, size, 90f, 0f);

        /// `face`: where his hands point in the picture (90 = up); `frame` > 0: drawn the way the rig's own game
        /// picture is (the figure tight in a frame of that width/height, turned and sized as the game shows that
        /// picture) - the spearman like the orc pikeman's picture (the user)
        public static Sprite Of(GameObject go, string id, Sprite fallback, float size, float face, float frame)
        {
            if (frame > 0f)
            {
                if (id != null && made.TryGetValue(id, out var f0) && f0 != null) return f0;
                Sprite f1 = null;
                try { f1 = Shoot(go, face, frame); }
                catch (Exception e) { SanctumPlugin.Log.LogWarning("Sanctum: no bestiary picture of " + id + ": " + e.Message); }
                if (f1 == null) return fallback;
                if (id != null) made[id] = f1;
                return f1;
            }
            // shown like the game's own (the zombie's, the rotfiend's): turned 135 in the list, head down in the
            // details, at full size and centred (the rigs' own sizes are for their old pictures: ours came out small
            // and off to a corner - the user's picture)
            try
            {
                var m = go != null ? go.GetComponent<Mob>() : null;
                if (m != null) { m.preview_bestiary_rot = 135; m.preview_bestiary_size = size; m.preview_bestiaryDet_rot = 180; m.preview_bestiaryDet_size = Mathf.Min(size, 1.3f); }
            }
            catch { }
            if (id != null && made.TryGetValue(id, out var s) && s != null) return s;
            try { s = Shoot(go, 90f, 0f); }
            catch (Exception e) { SanctumPlugin.Log.LogWarning("Sanctum: no bestiary picture of " + id + ": " + e.Message); s = null; }
            if (s == null) return fallback;
            if (id != null) made[id] = s;
            return s;
        }

        /// parts left out whole (legs, shadow, the blow's trail); "Hero_warrior" holds the whole body - only its own
        /// picture (the old preview) is left out
        static bool Skip(string n) => n.StartsWith("Leg", StringComparison.Ordinal) || n.IndexOf("shadow", StringComparison.OrdinalIgnoreCase) >= 0
                                      || n == "AttackTrail" || n == "RangeZone";
        static bool NoPicture(string n) => n == "Hero_warrior";

        static void Copy(Transform src, Transform dst, List<SpriteRenderer> out_)
        {
            foreach (Transform c in src)
            {
                if (!c.gameObject.activeSelf || Skip(c.name)) continue;
                var go = new GameObject(c.name); go.layer = Layer;
                var t = go.transform; t.SetParent(dst, false);
                t.localPosition = c.localPosition; t.localRotation = c.localRotation; t.localScale = c.localScale;
                var sr = c.GetComponent<SpriteRenderer>();
                if (sr != null && sr.enabled && sr.sprite != null && !NoPicture(c.name))
                {
                    var r = go.AddComponent<SpriteRenderer>();
                    r.sprite = sr.sprite; r.color = sr.color; r.flipX = sr.flipX; r.flipY = sr.flipY;
                    r.sharedMaterial = sr.sharedMaterial; r.sortingLayerID = sr.sortingLayerID; r.sortingOrder = sr.sortingOrder;
                    out_.Add(r);
                }
                Copy(c, t, out_);
            }
        }

        static Transform Find(Transform root, Func<string, bool> f) => root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => f(t.name));

        static Sprite Shoot(GameObject src, float face, float frame)
        {
            if (src == null) return null;
            var root = new GameObject("OS_Snapshot"); root.layer = Layer;
            Camera cam = null; RenderTexture rt = null; var was = RenderTexture.active;
            try
            {
                root.transform.position = new Vector3(-9000f, -9000f, 0f);
                var srs = new List<SpriteRenderer>();
                Copy(src.transform, root.transform, srs);
                if (srs.Count == 0) return null;
                // the game's idle pose
                var an0 = src.GetComponent<Animator>();
                if (an0 != null && an0.runtimeAnimatorController != null)
                {
                    var an = root.AddComponent<Animator>();
                    an.runtimeAnimatorController = an0.runtimeAnimatorController; an.avatar = an0.avatar;
                    an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    try { an.Rebind(); an.Update(0f); an.Update(0.15f); } catch { }
                    // (an animation may put the game's own pictures back: ours again)
                    foreach (var r in srs)
                    {
                        var orig = src.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(o => o.name == r.name && o.sprite != null);
                        if (orig != null) r.sprite = orig.sprite;
                    }
                    UnityEngine.Object.DestroyImmediate(an);
                }
                // face up, like the game's pictures: the hands (the weapon) above the shoulders, turned by a quarter only
                Transform sR = Find(root.transform, n => n == "UpperArm_R"), sL = Find(root.transform, n => n == "UpperArm_L");
                var hands = root.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Hand", StringComparison.Ordinal) || t.name.StartsWith("Arm_", StringComparison.Ordinal)).ToList();
                Vector2 mid = sR != null && sL != null ? (Vector2)(sR.position + sL.position) / 2f : (Vector2)root.transform.position;
                Vector2 fw = hands.Count > 0 ? (Vector2)hands.Aggregate(Vector3.zero, (a, t) => a + t.position) / hands.Count - mid : Vector2.right;
                if (fw.sqrMagnitude < 1e-6f) fw = Vector2.right;
                float ang = Mathf.Atan2(fw.y, fw.x) * Mathf.Rad2Deg;
                float turn = Mathf.Round((face - ang) / 90f) * 90f;
                root.transform.rotation = Quaternion.Euler(0f, 0f, turn);
                // one picture pixel = one pixel of his body
                var body = srs.FirstOrDefault(r => r.name == "Body") ?? srs[0];
                float px = 1f / body.sprite.pixelsPerUnit * Mathf.Abs(body.transform.lossyScale.x);
                if (px <= 1e-5f) return null;
                Bounds b = srs[0].bounds; foreach (var r in srs) b.Encapsulate(r.bounds);
                int tw = Mathf.Clamp(Mathf.CeilToInt(b.size.x / px) + 6, 8, 512), th = Mathf.Clamp(Mathf.CeilToInt(b.size.y / px) + 6, 8, 512);
                var cgo = new GameObject("OS_SnapshotCam");
                cgo.transform.SetParent(root.transform, true);
                cam = cgo.AddComponent<Camera>(); cam.enabled = false;
                cam.orthographic = true; cam.cullingMask = 1 << Layer;
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                cam.nearClipPlane = 0.01f; cam.farClipPlane = 100f;
                cgo.transform.position = new Vector3(b.center.x, b.center.y, -20f); cgo.transform.rotation = Quaternion.identity;
                rt = RenderTexture.GetTemporary(tw, th, 24, RenderTextureFormat.ARGB32); rt.filterMode = FilterMode.Point;
                cam.targetTexture = rt; cam.aspect = tw / (float)th; cam.orthographicSize = th * px / 2f;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(tw, th, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, tw, th), 0, 0); tex.Apply();
                RenderTexture.active = was;
                // crisp pixels, cut to what is drawn
                var c = tex.GetPixels(); int x0 = tw, y0 = th, x1 = -1, y1 = -1;
                for (int y = 0; y < th; y++) for (int x = 0; x < tw; x++)
                {
                    int i = y * tw + x; var p = c[i];
                    if (p.a >= 0.5f) { c[i] = new Color(Mathf.Clamp01(p.r / p.a), Mathf.Clamp01(p.g / p.a), Mathf.Clamp01(p.b / p.a), 1f); if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y; }
                    else c[i] = new Color(0f, 0f, 0f, 0f);
                }
                UnityEngine.Object.Destroy(tex);
                if (x1 < x0) return null;
                // centred on his body, not on all he holds (a long spear pushed the body off to a side - the user),
                // and the body the same size in every picture: about a third across, like the game's own
                Vector3 bc = body.bounds.center;
                int cx = Mathf.RoundToInt((bc.x - (b.center.x - tw * px / 2f)) / px), cy = Mathf.RoundToInt((bc.y - (b.center.y - th * px / 2f)) / px);
                float bodyPx = Mathf.Max(body.bounds.size.x, body.bounds.size.y) / px;
                int ext = Mathf.Max(Mathf.Max(cx - x0, x1 - cx), Mathf.Max(cy - y0, y1 - cy)) + 2;
                int w = Mathf.Max(2 * ext + 1, Mathf.CeilToInt(bodyPx / 0.34f)), h = w;
                int fw0 = x1 - x0 + 1, fh0 = y1 - y0 + 1;
                int ox = w / 2 - (cx - x0), oy = h / 2 - (cy - y0);
                if (frame > 0f)
                {
                    // like the game's picture: the figure tight, in the middle of a frame of the game's proportions
                    h = Mathf.Max(fh0, Mathf.CeilToInt(fw0 / frame)); w = Mathf.Max(fw0, Mathf.CeilToInt(h * frame));
                    ox = (w - fw0) / 2; oy = (h - fh0) / 2;
                }
                var outT = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                var o = new Color[w * h];
                for (int y = 0; y < fh0; y++) for (int x = 0; x < fw0; x++) o[(y + oy) * w + x + ox] = c[(y + y0) * tw + x + x0];
                outT.SetPixels(o); outT.Apply(false, false);
                outT.hideFlags = HideFlags.HideAndDontSave;
                var s = Sprite.Create(outT, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), Mathf.Max(w, h), 0, SpriteMeshType.FullRect);   // about one unit across, like the game's own
                s.hideFlags = HideFlags.HideAndDontSave; s.name = "OS_snap_" + src.name;
                SanctumPlugin.Log.LogInfo("Sanctum: bestiary picture of " + src.name + " taken (" + w + "x" + h + ", turned " + turn + ")");
                return s;
            }
            finally
            {
                RenderTexture.active = was;
                if (cam != null) cam.targetTexture = null;
                if (rt != null) RenderTexture.ReleaseTemporary(rt);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
