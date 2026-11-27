using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CultistRescue
{
    /// the berserker mercenary dressed as the Cultist: the class's own pictures (sackcloth hood, bare lashed shoulders,
    /// wrapped forearms, a bloody hand, rag-wrapped legs), a club in the right hand and the folded whip in the left.
    /// Each picture is set on a canvas of the replaced part's size with the same pivot, so the game's animations move it.
    public class Dress : MonoBehaviour
    {
        readonly Dictionary<SpriteRenderer, string> parts = new Dictionary<SpriteRenderer, string>();
        static readonly Dictionary<(Sprite, string), Sprite> made = new Dictionary<(Sprite, string), Sprite>();
        static readonly Dictionary<string, Texture2D> art = new Dictionary<string, Texture2D>();
        static bool logged;

        void Start()
        {
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr == null) continue;
                switch (sr.name)
                {
                    case "Head": parts[sr] = "hood"; break;
                    case "Body": parts[sr] = "torso"; break;
                    case "UpperArm_R": case "UpperArm_L": parts[sr] = "upperArm"; break;
                    case "Arm_R": case "Arm_L": parts[sr] = "forearm"; break;
                    case "Hand_R": parts[sr] = "hand"; break;
                    case "Hand_L": parts[sr] = "hand_blood"; break;
                    case "Leg_R": case "Leg_L": parts[sr] = "leg"; break;
                    case "HandEquip_R": parts[sr] = "#club"; break;
                    case "HandEquip_L": parts[sr] = "#coil"; break;
                }
            }
            if (!logged)
            {
                logged = true;
                RescuePlugin.Log.LogInfo("Cultist Rescue: dressing " + string.Join(", ", GetComponentsInChildren<SpriteRenderer>(true).Select(r => r.name + "=" + (r.sprite != null ? r.sprite.name + "@" + r.sprite.pixelsPerUnit : "-"))));
            }
            Apply();
        }

        void LateUpdate() { Apply(); }

        void Apply()
        {
            foreach (var kv in parts)
            {
                var sr = kv.Key;
                if (sr == null) continue;
                var cur = sr.sprite;
                if (cur != null && cur.name.StartsWith("CR_")) continue;
                Sprite mine;
                if (kv.Value == "#club") mine = Pics.Get("weapons/rustyFlail_equip.png", Ppu(cur, 42f), new Vector2(16f, 16f));
                else if (kv.Value == "#coil") mine = Pics.Get("weapons/leather_coil.png", Ppu(cur, 42f), Pics.Grip("weapons/leather_coil.grip", new Vector2(12.5f, 9f)));
                else mine = Fit(cur, kv.Value);
                if (mine != null) sr.sprite = mine;
            }
        }

        /// the weapons are drawn for the hero's scale (42 px a unit); if this body is drawn finer or coarser, the same ratio
        static float Ppu(Sprite like, float hero) => 42f;

        static Texture2D Art(string name)
        {
            if (art.TryGetValue(name, out var t)) return t;
            t = Pics.Tex("parts/" + name + ".png");
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
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "CR_" + name };
                tex.SetPixels32(px); tex.Apply(false, true);
                Vector2 piv = orig.pivot + new Vector2(padX, padY);
                s = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(piv.x / W, piv.y / H), orig.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                s.name = "CR_" + name; s.hideFlags = HideFlags.HideAndDontSave;
            }
            catch (Exception e) { RescuePlugin.Log.LogWarning("Cultist Rescue look " + name + ": " + e.Message); s = null; }
            made[(orig, name)] = s;
            return s;
        }
    }

    /// a line said over his head (the game's own floating text)
    public static class Talk
    {
        public static void Say(Transform who, string text)
        {
            try { if (who != null) PopupTextManager.Instance.CreatePopupText_WorldPosition(who.position + Vector3.up * 1.1f, text, new Color(1f, 0.8f, 0.45f, 1f)); } catch { }
        }
    }
}
