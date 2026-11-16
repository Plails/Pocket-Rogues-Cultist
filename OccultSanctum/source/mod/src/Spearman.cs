using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OccultSanctum
{
    /// The cult's spearman: dressed exactly like the crossbowman (deep grey hood, dark leathers, red sash - the same
    /// pictures), but he carries a long spear with a strip of red cloth under its point and fights in close with it.
    /// He is the game's orc pikeman (an ordinary character's build like the orc crossbowman, his own spear thrusts and
    /// his own mind) dressed in our pictures; any shield is taken away; corpse and name are his.
    public static class Spearman
    {
        public const string Id = "OccultSpearman";
        /// the spears in the spearmen's hands - the only ones there may be
        public static readonly HashSet<SpriteRenderer> Hands = new HashSet<SpriteRenderer>();
        static GameObject prefab;
        static bool looked;

        static GameObject Prefab()
        {
            if (!looked)
            {
                looked = true;
                prefab = Prisoner.FindMob(new[] { "OrkPikeman", "Ork_Pikeman", "Ork_pikeman", "OrcPikeman", "OrkSpearman" }, "Ork", "Pike")
                      ?? Prisoner.FindMob(new string[0], "Pike")
                      ?? Prisoner.FindMob(new[] { "ZombieSpearman" }, "Spear");
                SanctumPlugin.Log.LogInfo("Sanctum: the spearman is made from " + (prefab != null ? prefab.name : "nothing (no pikeman found)"));
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
            go.AddComponent<SpearmanTag>();
            if (Builder.root != null && Builder.root.GetComponent<SpearSweeper>() == null) Builder.root.gameObject.AddComponent<SpearSweeper>();
            var ai = go.GetComponent<AIController>();
            if (ai != null)
            {
                ai.RemoveBehavior(AIController.BEHAVIORS.USE_SHIELD);             // no shield: he fights with the spear
                // one spear, kept in his hands: he never throws spears (the orc pikeman's thrown pikes) - he thrusts
                foreach (var b in new[] { AIController.BEHAVIORS.COMBAT_DISTANCE, AIController.BEHAVIORS.COMBAT_DISTANCE_LIMIT, AIController.BEHAVIORS.STRENGTH_DISTANCE,
                                          AIController.BEHAVIORS.MULTIARCH, AIController.BEHAVIORS.MULTIARCH_HUNTER, AIController.BEHAVIORS.FIRE_ON_DISTANCE })
                    ai.RemoveBehavior(b);
                if (!ai.HasBehavior(AIController.BEHAVIORS.COMBAT_PIKEMAN)) ai.AddBehavior(AIController.BEHAVIORS.COMBAT_PIKEMAN);
                try { GameManager.Instance.AddMob(ai); } catch { }
            }
            return mob;
        }

        static bool IsSpear(SpriteRenderer sr)
        {
            // only the weapon itself: the part called "Pike" / "Spear", or a picture whose name ENDS so - the orc's
            // body parts are "ork_pikeman_Body", "ork_pikeman_UpperArm"... ("pikeman" holds "pike": they are no spears)
            string n = sr.gameObject.name, s = sr.sprite != null ? sr.sprite.name : "";
            return n.Equals("Pike", StringComparison.OrdinalIgnoreCase) || n.Equals("Spear", StringComparison.OrdinalIgnoreCase)
                || s.EndsWith("_Pike", StringComparison.OrdinalIgnoreCase) || s.EndsWith("_spear", StringComparison.OrdinalIgnoreCase);
        }

        static void Dress(GameObject go, Mob mob)
        {
            go.name = "HoodedSpearman";
            SpriteRenderer headSr = null; Transform body = null;
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
            {
                string n = sr.gameObject.name;
                if (n == "Shield" || (sr.sprite != null && sr.sprite.name.IndexOf("Shield", StringComparison.OrdinalIgnoreCase) >= 0))
                { sr.gameObject.SetActive(false); UnityEngine.Object.Destroy(sr); continue; }        // no shield at all
                if (n == "Head") { headSr = sr; continue; }
                if (IsSpear(sr)) { var sp = Art.Part("spear_pike", sr.sprite); if (sp != null) sr.sprite = sp; Hands.Add(sr); continue; }
                string art = n == "Body" ? "xbow_body" : n.StartsWith("UpperArm", StringComparison.Ordinal) ? "xbow_upperarm"
                           : n.StartsWith("ForeArm", StringComparison.Ordinal) || n.StartsWith("Arm_", StringComparison.Ordinal) ? "xbow_forearm"
                           : n.StartsWith("Hand_", StringComparison.Ordinal) ? "xbow_hand" : n.StartsWith("Leg_", StringComparison.Ordinal) ? "xbow_leg"
                           : n == "Hero_warrior" ? "spear_preview" : null;
                if (art == null) continue;
                var s = Art.Part(art, sr.sprite);
                if (s != null) sr.sprite = s;
                if (n == "Body") body = sr.transform;
            }
            // the hood on the shoulders, as on the crossbowman (the orc's head moves forward when he strikes)
            if (headSr != null)
            {
                var hd = Art.Pivoted("xbow_head", new Vector2(0.5f, 0.5f), 40f);
                if (body != null && hd != null)
                {
                    var hood = new GameObject("Hood");
                    hood.transform.SetParent(body, false);
                    hood.transform.localPosition = new Vector3(0f, 0.04f, 0f);
                    var hs = hood.AddComponent<SpriteRenderer>();
                    hs.sprite = hd; hs.sortingLayerID = headSr.sortingLayerID; hs.sortingOrder = headSr.sortingOrder; hs.sharedMaterial = headSr.sharedMaterial;
                    headSr.sprite = null;
                }
                else if (hd != null) headSr.sprite = hd;
            }
            try
            {
                var dead = Art.Pivoted("spear_dead", new Vector2(0.5f, 0.5f), 42f);   // the crossbowman's corpse, his spear by him
                if (dead != null) mob.deadBodySprites = new[] { dead };
            }
            catch { }
            try { mob.creatureLocalizedName = "Sanctum/Spearman"; } catch { }
            mob.creatureID = Id;
            Human.Apply(mob);                                                      // a man's voice, red blood
            mob.customPreviewSprite = Art.Pivoted("spear_preview", new Vector2(0.5f, 0.5f), 32f);
        }
    }

    public class SpearmanTag : MonoBehaviour { }

    /// a spearman has one spear, in his hands: any other copy of his spear's picture that turns up in the sanctum
    /// (copies the game makes of a monster's weapon) is taken away at once - and where it came from goes to the log
    public class SpearSweeper : MonoBehaviour
    {
        float next; int logged;
        void Update()
        {
            if (Time.time < next) return;
            next = Time.time + 0.15f;
            Spearman.Hands.RemoveWhere(h => h == null);
            var corpses = new System.Collections.Generic.List<SpriteRenderer>();
            foreach (var sr in FindObjectsOfType<SpriteRenderer>())
            {
                if (sr == null || sr.sprite == null) continue;
                string sn = sr.sprite.name;
                if (sn == "OS_spear_dead") { corpses.Add(sr); continue; }
                if (!sn.StartsWith("OS_spear", StringComparison.Ordinal) || sn == "OS_spear_preview" || Spearman.Hands.Contains(sr)) continue;
                if (logged++ < 5)
                {
                    string path = sr.gameObject.name; var t = sr.transform.parent;
                    while (t != null) { path = t.name + "/" + path; t = t.parent; }
                    SanctumPlugin.Log.LogInfo("Sanctum: an extra spear (" + path + ", " + string.Join(",", sr.GetComponents<Component>().Select(cm => cm.GetType().Name)) + ") taken away");
                }
                var cr = sr.GetComponentInParent<Creature>();
                if (cr != null) { sr.enabled = false; sr.sprite = null; }     // on a creature: just its picture
                else Destroy(sr.gameObject);
            }
            // one spearman leaves one corpse: a second one at the same spot goes
            for (int i = 0; i < corpses.Count; i++)
                for (int j = i + 1; j < corpses.Count; j++)
                    if (corpses[i] != null && corpses[j] != null && ((Vector2)corpses[i].transform.position - (Vector2)corpses[j].transform.position).sqrMagnitude < 0.36f)
                    {
                        if (logged++ < 8) SanctumPlugin.Log.LogInfo("Sanctum: a doubled spearman corpse taken away");
                        Destroy(corpses[j].gameObject); corpses[j] = null;
                    }
        }
    }

    /// whatever missile his attacks would let fly (the orc pikeman's thrown pikes) is gone at once: he has one spear
    [HarmonyLib.HarmonyPatch]
    internal static class SpearNoThrowPatch
    {
        static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            foreach (var m in typeof(ArrowController).GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
                if (m.Name == "Init" && System.Linq.Enumerable.Any(m.GetParameters(), p => p.Name == "source")) yield return m;
        }

        static void Postfix(ArrowController __instance, UnityEngine.Object source)
        {
            try
            {
                var comp = source as Component;
                if (comp == null || comp.GetComponentInParent<SpearmanTag>() == null) return;
                __instance.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(__instance.gameObject);
            }
            catch { }
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Controller), nameof(Controller.ThrowKnives))]
    internal static class SpearNoKnivesPatch
    {
        static bool Prefix(Controller __instance) { try { return __instance.GetComponent<SpearmanTag>() == null; } catch { return true; } }
    }
}
