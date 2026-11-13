using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace FlagellantMod
{
    /// The game's own visual effects for the Cultist's skills: the particle prefabs the original classes use
    /// (shockwaves, glow rings, flashes, dust, light beams, heal bursts), the shared hit effects of the game
    /// (poison, bleeding, armour break, explosion, curse) and the weapon slash trail - so his attacks look
    /// as if the game's own author made them. Colours are tinted where the skill needs its own colour.
    internal static class GameFx
    {
        const BindingFlags F = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static Dictionary<string, GameObject> prefabs;
        static Material additive;
        /// dev preview: when >= 0 every spawned effect goes to this layer
        public static int PreviewLayer = -1;

        static void Init()
        {
            if (prefabs != null) return;
            prefabs = new Dictionary<string, GameObject>();
            try
            {
                var db = (Resources.Load("SkillsDB") as GameObject)?.GetComponent<SkillsDataBase>();
                if (db == null) return;
                var all = new[] { db.skills_warrior, db.skills_archer, db.skills_wizard, db.skills_hunter, FlagSkills.OriginalOf(db), db.skills_necrom };
                foreach (var data in all.Where(d => d != null))
                    foreach (var h in data.skills_Speed.Concat(data.skills_Power).Concat(data.skills_Special))
                        if (h?.skill != null)
                            foreach (var l in FlagSkills.AllLvls(h.skill))
                                foreach (var ec in l.effectsList ?? new List<SkillEffectContainer>())
                                    if (ec?.effect != null)
                                        for (var t = ec.effect.GetType(); t != null && t != typeof(ScriptableObject); t = t.BaseType)
                                            foreach (var f in t.GetFields(F | BindingFlags.DeclaredOnly))
                                                if (f.GetValue(ec.effect) is GameObject g && g != null && !prefabs.ContainsKey(g.name)) prefabs[g.name] = g;
                FlagellantPlugin.Log.LogInfo("Game effects found: " + prefabs.Count);
            }
            catch (Exception e) { FlagellantPlugin.Log.LogWarning("GameFx init: " + e.Message); }
        }

        /// a game prefab by name; "pcm:field" = the shared effects of the game (PrefabsContainerManager, in a run)
        public static GameObject Get(string name)
        {
            Init();
            if (name.StartsWith("pcm:"))
            {
                try
                {
                    var pcm = PrefabsContainerManager.Instance;
                    if (pcm == null) return null;
                    var v = typeof(PrefabsContainerManager).GetField(name.Substring(4), F)?.GetValue(pcm);
                    if (v is GameObject go) return go;
                    if (v is Component c) return c.gameObject;
                }
                catch { }
                return null;
            }
            return prefabs.TryGetValue(name, out var p) ? p : null;
        }

        /// the game's additive particle material (soft glow), for our own shapes drawn the game's way
        public static Material Additive
        {
            get
            {
                if (additive != null) return additive;
                Init();
                foreach (var p in prefabs.Values)
                    foreach (var r in p.GetComponentsInChildren<ParticleSystemRenderer>(true))
                        if (r.sharedMaterial != null && r.sharedMaterial.shader != null && r.sharedMaterial.shader.name == "Mobile/Particles/Additive")
                        { additive = new Material(r.sharedMaterial.shader) { name = "Flagellant_Additive" }; return additive; }
                return null;
            }
        }

        static Sprite softWedge;
        /// a soft glowing triangle, apex at the left, 1 unit long, 0.76 wide (smooth like the game's area particles)
        public static Sprite SoftWedge
        {
            get
            {
                if (softWedge != null) return softWedge;
                const int W = 96, H = 72;
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "Flagellant_softWedge" };
                var px = new Color32[W * H];
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        float u = (x + 0.5f) / W, v = ((y + 0.5f) / H - 0.5f) * 2f;          // u 0..1 along, v -1..1 across
                        float half = u * 0.98f;                                             // the triangle widens to the base
                        float edge = Mathf.Clamp01((half - Mathf.Abs(v)) / 0.22f);           // soft sides
                        float tail = Mathf.Clamp01(u / 0.15f) * Mathf.Clamp01((1f - u) / 0.12f);
                        float core = Mathf.Lerp(0.25f, 1f, u);                               // brighter towards the base
                        float a = edge * tail * core;
                        px[y * W + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(255f * a * a));
                    }
                tex.SetPixels32(px); tex.Apply(false, true);
                tex.hideFlags = HideFlags.HideAndDontSave;
                softWedge = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), W, 0, SpriteMeshType.FullRect);
                softWedge.hideFlags = HideFlags.HideAndDontSave;
                return softWedge;
            }
        }

        static Color Tint(Color orig, Color t) => new Color(t.r, t.g, t.b, orig.a * t.a);

        static void Recolor(ParticleSystem ps, Color t)
        {
            var m = ps.main;
            var sc = m.startColor;
            switch (sc.mode)
            {
                case ParticleSystemGradientMode.Color: m.startColor = new ParticleSystem.MinMaxGradient(Tint(sc.color, t)); break;
                case ParticleSystemGradientMode.TwoColors: m.startColor = new ParticleSystem.MinMaxGradient(Tint(sc.colorMin, t), Tint(sc.colorMax, t)); break;
                default: m.startColor = new ParticleSystem.MinMaxGradient(t); break;
            }
            try { var col = ps.colorOverLifetime; if (col.enabled) { col.enabled = false; } } catch { }
        }

        /// spawns a game effect; scale grows the particles too; tint recolours it; destroyed after life seconds
        public static GameObject Spawn(string name, Vector2 pos, float rotZ = 0f, float scale = 1f, Color? tint = null, float life = 2.5f, Transform follow = null, string[] only = null)
        {
            var pf = Get(name);
            if (pf == null) return null;
            GameObject go = null;
            try
            {
                // a picture-only copy: made while the prefab is switched off, then every script, collider and body is
                // removed (projectile prefabs like the ball lightning must not fly or hit anything)
                bool was = pf.activeSelf;
                if (was) pf.SetActive(false);
                try { go = UnityEngine.Object.Instantiate(pf, new Vector3(pos.x, pos.y, 0f), Quaternion.Euler(0f, 0f, rotZ)); }
                finally { if (was) pf.SetActive(true); }
                foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true)) try { UnityEngine.Object.DestroyImmediate(mb); } catch { }
                foreach (var col in go.GetComponentsInChildren<Collider2D>(true)) try { UnityEngine.Object.DestroyImmediate(col); } catch { }
                foreach (var body in go.GetComponentsInChildren<Rigidbody2D>(true)) try { UnityEngine.Object.DestroyImmediate(body); } catch { }
                foreach (var a in go.GetComponentsInChildren<AudioSource>(true)) a.enabled = false;
                go.name = "Flagellant_fx_" + pf.name;
                if (follow != null) go.transform.SetParent(follow, true);
                go.transform.localScale = pf.transform.localScale * scale;
                foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var m = ps.main;
                    m.scalingMode = ParticleSystemScalingMode.Hierarchy;
                    m.loop = false;
                    if (only != null && !only.Contains(ps.name))
                    {
                        // the root holds the others: it is only silenced, children are switched off
                        if (ps.gameObject == go) { var em = ps.emission; em.enabled = false; var pr = ps.GetComponent<ParticleSystemRenderer>(); if (pr != null) pr.enabled = false; }
                        else ps.gameObject.SetActive(false);
                        continue;
                    }
                    if (tint.HasValue) Recolor(ps, tint.Value);
                }
                foreach (var trl in go.GetComponentsInChildren<TrailRenderer>(true))
                {
                    if (tint.HasValue) { var t = tint.Value; trl.startColor = new Color(t.r, t.g, t.b, trl.startColor.a); trl.endColor = new Color(t.r, t.g, t.b, 0f); }
                    trl.widthMultiplier *= scale;
                    trl.Clear();
                }
                if (PreviewLayer >= 0) foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = PreviewLayer;
                go.SetActive(true);
                foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(false)) { ps.Clear(true); ps.Play(true); }
                UnityEngine.Object.Destroy(go, life);
            }
            catch (Exception e) { FlagellantPlugin.Log.LogWarning("GameFx " + name + ": " + e.Message); }
            return go;
        }

        /// the game's weapon slash trail, placed where the game puts it for a melee skill with this area
        public static void Slash(Controller src, float rangePct)
        {
            try
            {
                var tr = src.GetAttackTrail;
                var cr = src._creature as Character;
                if (tr == null || cr == null) return;
                float num = 1.2f * rangePct * 0.01f * (cr.rangeMod + cr.art_rangeMod * 0.01f);
                float collR = cr.coll != null ? cr.coll.radius : 0.3f;
                float off = (collR / 2f + cr.attackDistance * 0.5f) * (1f + num * 0.5f) * 1.4f / 1.5f;
                tr.position = (Vector2)cr.transform.position + (Vector2)cr.transform.right * off;
                tr.localRotation = Quaternion.Euler(0f, 0f, -90f);
                tr.localScale = Vector2.one * cr.attackRange * num * 1.5f;
            }
            catch { }
        }
    }
}
