using System;
using UnityEngine;

namespace OccultSanctum
{
    /// The cult's crossbowman: a deep grey hood and cowl, dark leather, a red sash, a big crossbow; he shoots bolts
    /// from afar. He is the game's orc crossbowman (an ordinary character's build, its aim, its bolts, its mind) dressed
    /// in our pictures - its own parts recoloured, a small hood drawn for the head, the crossbow darker and bigger, the
    /// orc's shield taken away; corpse and name are his.
    public static class Crossbowman
    {
        public const string Id = "OccultCrossbowman";
        static GameObject prefab;
        static bool looked;

        static GameObject Prefab()
        {
            if (!looked)
            {
                looked = true;
                prefab = Prisoner.FindMob(new[] { "OrkCrossbowman", "Ork_Crossbowman", "OrcCrossbowman" }, "Ork", "Crossbow")
                      ?? Prisoner.FindMob(new[] { "ZombieCrossbowman" }, "Crossbow");
                SanctumPlugin.Log.LogInfo("Sanctum: the crossbowman is made from " + (prefab != null ? prefab.name : "nothing (no crossbowman found)"));
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
            var ai = go.GetComponent<AIController>();
            if (ai != null)
            {
                // a pure marksman: no melee rush, no shield; he keeps his distance with the game's archer tactics and
                // only hits in close when the hero comes right up to him (see Marksman)
                ai.RemoveBehavior(AIController.BEHAVIORS.COMBAT_MELEE);
                ai.RemoveBehavior(AIController.BEHAVIORS.USE_SHIELD);
                ai.RemoveBehavior(AIController.BEHAVIORS.COMBAT_PIKEMAN);
                ai.AddBehavior(AIController.BEHAVIORS.COMBAT_DISTANCE);
                ai.forcedBattleTactics = true;
                go.AddComponent<Marksman>();
                try { GameManager.Instance.AddMob(ai); } catch { }
            }
            return mob;
        }

        static void Dress(GameObject go, Mob mob)
        {
            go.name = "HoodedCrossbowman";
            SpriteRenderer headSr = null; Transform body = null;
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
            {
                string n = sr.gameObject.name;
                if (n == "Shield") { sr.gameObject.SetActive(false); UnityEngine.Object.Destroy(sr); continue; }   // no shield at all (its animation cannot bring it back)
                if (n == "Head") { headSr = sr; continue; }
                if (n == "Crossbow") { var cb = Art.Part("xbow_crossbow", sr.sprite); if (cb != null) sr.sprite = cb; sr.transform.localScale = sr.transform.localScale * 1.3f; continue; }   // a big crossbow
                string art = n == "Body" ? "xbow_body" : n == "Head" ? "xbow_head" : n.StartsWith("UpperArm", StringComparison.Ordinal) ? "xbow_upperarm"
                           : n.StartsWith("ForeArm", StringComparison.Ordinal) || n.StartsWith("Arm_", StringComparison.Ordinal) ? "xbow_forearm"
                           : n.StartsWith("Hand_", StringComparison.Ordinal) ? "xbow_hand" : n.StartsWith("Leg_", StringComparison.Ordinal) ? "xbow_leg"
                           : n == "Hero_warrior" ? "xbow_preview" : null;
                if (art == null) continue;
                var s = Art.Part(art, sr.sprite);
                if (s != null) sr.sprite = s;
                if (n == "Body") body = sr.transform;
            }
            // the hood (the game's own hero hood, ~0.6 tiles) sits on the body, not on the orc's head: the orc's
            // shooting moves its head far forward, a hood on the shoulders stays put
            if (headSr != null)
            {
                var hd = Art.Pivoted("xbow_head", new Vector2(0.5f, 0.5f), 40f);
                if (body != null && hd != null)
                {
                    var hood = new GameObject("Hood");
                    hood.transform.SetParent(body, false);
                    // seen from above the head sits right on the shoulders: the middle of the body, a hair forward
                    hood.transform.localPosition = new Vector3(0f, 0.04f, 0f);
                    var hs = hood.AddComponent<SpriteRenderer>();
                    hs.sprite = hd; hs.sortingLayerID = headSr.sortingLayerID; hs.sortingOrder = headSr.sortingOrder; hs.sharedMaterial = headSr.sharedMaterial;
                    headSr.sprite = null;
                }
                else if (hd != null) headSr.sprite = hd;
            }
            try
            {
                // the game's hooded archer's corpse without its cloak (grey hood, sash, the crossbow beside him), shown at
                // that corpse's own 42 px per unit
                var dead = Art.Pivoted("xbow_dead", new Vector2(0.5f, 0.5f), 42f);
                if (dead != null) mob.deadBodySprites = new[] { dead };
            }
            catch { }
            try { mob.creatureLocalizedName = "Sanctum/Crossbowman"; } catch { }
            mob.creatureID = Id;
            Human.Apply(mob);                                                      // a man's voice, red blood
            mob.customPreviewSprite = Snapshot.Of(go, mob.creatureID, Art.Pivoted("xbow_preview", new Vector2(0.5f, 0.5f), 32f));   // taken from himself (the user: the hand-made ones were crooked)
        }
    }

    /// the crossbowman shoots from afar; when the hero comes right up to him (closer than ~2 tiles) he fights him in
    /// close, and when the hero steps back he is a marksman again
    public class Marksman : MonoBehaviour
    {
        AIController ai; float next; bool melee;
        void Awake() { ai = GetComponent<AIController>(); }
        void Update()
        {
            if (ai == null || Time.time < next) return;
            next = Time.time + 0.2f;
            var t = Patches.Target(ai);
            if (t == null) { if (melee) Set(false); return; }
            float d = Vector2.Distance(t.position, transform.position);
            if (!melee && d < 1.9f) Set(true);
            else if (melee && d > 2.8f) Set(false);
        }
        void Set(bool on)
        {
            melee = on;
            if (on) ai.AddBehavior(AIController.BEHAVIORS.COMBAT_MELEE); else ai.RemoveBehavior(AIController.BEHAVIORS.COMBAT_MELEE);
        }
    }
}
