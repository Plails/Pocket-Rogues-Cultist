using System;
using System.Linq;
using UnityEngine;

namespace OccultSanctum
{
    /// The sanctum's first enemy: a cultist in a black cloak trimmed with red, a goblin's build and size, throwing
    /// cursed spheres from his bare hands. He is the game's goblin wizard (its size, its moves, its mind, its magic
    /// bolt) dressed in our pictures - the wizard's own parts recoloured (black hood and robe, red hem, red cuffs,
    /// pale hands), its staff taken away, its bolt turned into a dark red sphere; the corpse and the name are his.
    public static class Cultist
    {
        static GameObject prefab;
        static bool looked;

        static GameObject Prefab()
        {
            if (!looked)
            {
                looked = true;
                prefab = Prisoner.FindMob(new[] { "GoblinWizard1", "GoblinWizard", "Goblin_Wizard1", "GoblinWizard2" }, "Goblin", "Wiz")
                      ?? Prisoner.FindMob(new[] { "GoblinWizard2" }, "Goblin", "Mage");
                SanctumPlugin.Log.LogInfo("Sanctum: the cultist is made from " + (prefab != null ? prefab.name : "nothing (no goblin found)"));
            }
            return prefab;
        }

        public static GameObject PrefabForBestiary() => Prefab();
        public static void DressForBestiary(GameObject go, Mob mob) => Dress(go, mob);

        public static Mob Spawn(Vector2 pos)
        {
            var p = Prefab();
            if (p == null) return null;
            var go = Builder.Spawn(p, pos, UnityEngine.Random.Range(0f, 360f));
            if (go == null) return null;
            Builder.Own(go);
            foreach (var rs in go.GetComponentsInChildren<RandomSprite>(true)) UnityEngine.Object.DestroyImmediate(rs);
            var mob = go.GetComponent<Mob>();
            if (mob == null) { UnityEngine.Object.Destroy(go); return null; }
            mob.lvl = Mathf.Max(1, Generation.GetRandomMobLvl() + 1);
            mob.Init();
            Dress(go, mob);
            Sizer.Fit(go, Sizer.Cultist);                                             // as big as the hero (the user)
            var ai = go.GetComponent<AIController>();
            if (ai != null) try { GameManager.Instance.AddMob(ai); } catch { }
            return mob;
        }

        static void Dress(GameObject go, Mob mob)
        {
            go.name = "Cultist";
            if (go.GetComponent<CultistTag>() == null) go.AddComponent<CultistTag>();
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
            {
                string n = sr.gameObject.name;
                if (n.StartsWith("HandEquip", StringComparison.Ordinal)) { sr.sprite = null; sr.enabled = false; continue; }   // no staff: bare hands
                string art = n == "Body" ? "cult_body" : n == "Head" ? "cult_head" : n.StartsWith("UpperArm", StringComparison.Ordinal) ? "cult_upperarm"
                           : n.StartsWith("Arm_", StringComparison.Ordinal) ? "cult_forearm" : n.StartsWith("Hand_", StringComparison.Ordinal) ? "cult_hand"
                           : n.StartsWith("Leg_", StringComparison.Ordinal) ? "cult_leg" : n == "Hero_warrior" ? "cult_preview" : null;
                if (art == null) continue;
                var s = Art.Part(art, sr.sprite);
                if (s != null) sr.sprite = s;
            }
            try
            {
                var refDead = mob.deadBodySprites != null && mob.deadBodySprites.Length > 0 ? mob.deadBodySprites[0] : null;
                var dead = Art.Part("cult_dead", refDead, 36f);
                if (dead != null) mob.deadBodySprites = new[] { dead };
            }
            catch { }
            try { mob.creatureLocalizedName = "Sanctum/Cultist"; } catch { }
            mob.creatureID = Bestiary.CultistId;
            Human.Apply(mob);                                                      // a man's voice, red blood                                    // his kills count as a cultist's in the bestiary
            mob.customPreviewSprite = Snapshot.Of(go, mob.creatureID, Art.Pivoted("cult_preview", new Vector2(0.5f, 0.5f), 36f));   // taken from himself (the user: the hand-made ones were crooked)
        }
    }

    public class CultistTag : MonoBehaviour { }

    /// the wizard's bolt, thrown by a cultist, becomes a cursed sphere: our picture, a dark red glow
    [HarmonyLib.HarmonyPatch]
    internal static class CultistBoltPatch
    {
        static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            foreach (var m in typeof(ArrowController).GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
                if (m.Name == "Init" && m.GetParameters().Any(p => p.Name == "source")) yield return m;
        }

        static readonly Color Cursed = new Color(0.85f, 0.18f, 0.28f, 1f);

        static void Postfix(ArrowController __instance, UnityEngine.Object source)
        {
            try
            {
                var comp = source as Component;
                if (comp == null || comp.GetComponentInParent<CultistTag>() == null && comp.GetComponentInParent<ElderTag>() == null && comp.GetComponentInParent<MagusTag>() == null) return;
                // the archmage's sphere: the cultist's, only wider (the user) - a bigger sphere that strikes a wider path
                if (comp.GetComponentInParent<MagusTag>() != null) __instance.transform.localScale = __instance.transform.localScale * Magus.BoltWidth;
                foreach (var sr in __instance.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    string n = ((sr.sprite != null ? sr.sprite.name : "") + "|" + sr.gameObject.name).ToLowerInvariant();
                    if (n.Contains("light") || n.Contains("glow") || n.Contains("shadow")) { sr.color = new Color(Cursed.r, Cursed.g, Cursed.b, sr.color.a); continue; }   // its glow: just red
                    var s = Art.Part("cult_orb", sr.sprite, 24f);
                    if (s != null) sr.sprite = s;
                    sr.color = Color.white;
                }
                foreach (var ps in __instance.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main = ps.main;
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.55f, 0.05f, 0.12f, 1f), new Color(0.35f, 0.05f, 0.35f, 1f));
                }
                foreach (var tr in __instance.GetComponentsInChildren<TrailRenderer>(true)) { tr.startColor = Cursed; tr.endColor = new Color(0.3f, 0.02f, 0.1f, 0f); }
                foreach (var lr in __instance.GetComponentsInChildren<LineRenderer>(true)) { lr.startColor = Cursed; lr.endColor = Cursed; }
            }
            catch { }
        }
    }
}
