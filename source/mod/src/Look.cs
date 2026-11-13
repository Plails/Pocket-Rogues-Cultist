using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace FlagellantMod
{
    /// The penitent look: sackcloth hood, bare lashed shoulders, wrapped forearms, a bloody hand, rag-wrapped legs,
    /// ochre sash and ragged skirt. Drawn for the game's own body rig (same part sizes and pivots), so every
    /// animation moves it. It is only the class's "no armour" look: equipped armour and helmets are drawn as usual.
    internal class FlagLook : MonoBehaviour
    {
        Character ch;
        readonly Dictionary<SpriteRenderer, string> parts = new Dictionary<SpriteRenderer, string>();
        readonly List<SpriteRenderer> hidden = new List<SpriteRenderer>();
        SpriteRenderer pelvis;
        bool tinted;
        float rescan;
        static readonly Dictionary<(Sprite, string), Sprite> made = new Dictionary<(Sprite, string), Sprite>();
        static readonly Dictionary<string, Texture2D> art = new Dictionary<string, Texture2D>();

        void Start()
        {
            ch = GetComponent<Character>();
            Scan();
        }

        void Scan()
        {
            parts.Clear(); hidden.Clear(); pelvis = null;
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr == null) continue;
                switch (sr.name.TrimStart('_'))
                {
                    case "Head_Hair": parts[sr] = "hood"; break;
                    case "Body_Armor": parts[sr] = "torso"; break;
                    case "UpperArm_R": case "UpperArm_L": parts[sr] = "upperArm"; break;
                    case "Arm_R": case "Arm_L": parts[sr] = "forearm"; break;
                    case "Hand_R": parts[sr] = "hand"; break;
                    case "Hand_L": parts[sr] = "hand_blood"; break;
                    case "Leg_R": case "Leg_L": parts[sr] = "leg"; break;
                }
            }
        }

        static Texture2D Art(string name)
        {
            if (art.TryGetValue(name, out var t)) return t;
            var path = Path.Combine(FlagellantPlugin.Dir, "parts", name + ".png");
            if (!File.Exists(path)) { art[name] = null; return null; }
            t = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            ImageConversion.LoadImage(t, File.ReadAllBytes(path));
            t.hideFlags = HideFlags.HideAndDontSave;
            art[name] = t;
            return t;
        }

        /// our picture on a canvas of the original sprite's size, centred where the original's pixels are, same pivot
        static Sprite Fit(Sprite orig, string name)
        {
            if (orig == null) return null;
            if (made.TryGetValue((orig, name), out var s)) return s;
            var a = Art(name);
            if (a == null) return null;
            try
            {
                var rect = orig.rect;
                int W = Mathf.Max(a.width, Mathf.RoundToInt(rect.width)), H = Mathf.Max(a.height, Mathf.RoundToInt(rect.height));
                Vector2 tightCenter;
                try { var tr = orig.textureRect; var off = orig.textureRectOffset; tightCenter = off + tr.size / 2f; }
                catch { tightCenter = rect.size / 2f; }
                if (a.width >= rect.width * 0.9f && a.height >= rect.height * 0.9f) tightCenter = rect.size / 2f;
                // the canvas grows only if our picture is bigger than the original rect
                float padX = (W - rect.width) / 2f, padY = (H - rect.height) / 2f;
                int ox = Mathf.RoundToInt(tightCenter.x + padX - a.width / 2f), oy = Mathf.RoundToInt(tightCenter.y + padY - a.height / 2f);
                var px = new Color32[W * H];
                var src = a.GetPixels32();
                for (int y = 0; y < a.height; y++)
                    for (int x = 0; x < a.width; x++)
                    {
                        int tx = ox + x, ty = oy + y;
                        if (tx < 0 || ty < 0 || tx >= W || ty >= H) continue;
                        px[ty * W + tx] = src[y * a.width + x];
                    }
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "Flg_" + name };
                tex.SetPixels32(px); tex.Apply(false, true);
                Vector2 piv = orig.pivot + new Vector2(padX, padY);
                s = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(piv.x / W, piv.y / H), orig.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                s.name = "Flg_" + name; s.hideFlags = HideFlags.HideAndDontSave;
            }
            catch (Exception e) { FlagellantPlugin.Log.LogWarning("Look " + name + ": " + e.Message); s = null; }
            made[(orig, name)] = s;
            if (s != null) made[(s, name)] = s;
            return s;
        }

        static readonly Dictionary<SpriteRenderer, Sprite> origOf = new Dictionary<SpriteRenderer, Sprite>();

        void LateUpdate()
        {
            if (ch == null) return;
            if (Time.time > rescan) { rescan = Time.time + 3f; if (parts.Keys.Any(k => k == null)) Scan(); }
            // like every hero: ours is only the "no armour" look of the class - the game's default parts are swapped,
            // equipped armour and helmets are drawn by the game as usual
            foreach (var kv in parts)
            {
                var sr = kv.Key;
                if (sr == null) continue;
                var cur = sr.sprite;
                if (cur == null || cur.name.StartsWith("Flg_") || !IsDefault(cur)) continue;
                var mine = Fit(cur, kv.Value);
                if (mine != null) sr.sprite = mine;
            }

            // Religious Ecstasy: the body turns crimson and pulses
            bool ecstasy = FlagRuntime.EcstasyActive;
            if (ecstasy || tinted)
            {
                float p = ecstasy ? 0.55f + Mathf.Sin(Time.time * 9f) * 0.2f : 0f;
                var col = Color.Lerp(Color.white, new Color(1f, 0.25f, 0.2f, 1f), p);
                foreach (var sr in parts.Keys) if (sr != null) sr.color = new Color(col.r, col.g, col.b, sr.color.a);
                var head = transform.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r => r.name == "Head");
                if (head != null) head.color = new Color(col.r, col.g, col.b, head.color.a);
                tinted = ecstasy;
            }
        }

        /// the class's own "no armour" sprites (default body, hair, bare arms, hands, legs)
        static bool IsDefault(Sprite s) => s.name.StartsWith("Characters_def") || s.name.StartsWith("Character_HeadAndHair");

        void OnDestroy()
        {
            foreach (var kv in parts) if (kv.Key != null) kv.Key.color = Color.white;
        }
    }
}
