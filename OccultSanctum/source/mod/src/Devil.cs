using System;
using UnityEngine;

namespace OccultSanctum
{
    /// A small winged devil, called up through the portal of the cultists' ritual: quick, fights in close with its
    /// claws. It is the game's goblin assassin (its size, speed, moves and mind) made red all over with small horns,
    /// its daggers taken away, a pair of bat wings flapping on its back, smaller than a goblin.
    public static class Devil
    {
        static GameObject prefab;
        static bool looked;

        static GameObject Prefab()
        {
            if (!looked)
            {
                looked = true;
                prefab = Prisoner.FindMob(new[] { "GoblinAssassin", "Goblin_Assassin", "goblinAssassin" }, "Goblin", "Assassin")
                      ?? Prisoner.FindMob(new[] { "GoblinThief" }, "Goblin", "Thief");
                SanctumPlugin.Log.LogInfo("Sanctum: the devil is made from " + (prefab != null ? prefab.name : "nothing (no goblin found)"));
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
            if (ai != null) try { GameManager.Instance.AddMob(ai); } catch { }
            return mob;
        }

        static void Dress(GameObject go, Mob mob)
        {
            go.name = "WingedDevil";
            if (go.GetComponent<DevilTag>() == null) go.AddComponent<DevilTag>();
            go.transform.localScale = go.transform.localScale * 0.8f;          // smaller than a goblin
            Transform body = null; SpriteRenderer bodySr = null;
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
            {
                string n = sr.gameObject.name;
                if (n.StartsWith("HandEquip", StringComparison.Ordinal)) { sr.sprite = null; sr.enabled = false; continue; }   // no daggers: claws
                string art = n == "Body" ? "devil_body" : n == "Head" ? "devil_head" : n.StartsWith("UpperArm", StringComparison.Ordinal) ? "devil_upperarm"
                           : n.StartsWith("Arm_", StringComparison.Ordinal) ? "devil_forearm" : n.StartsWith("Hand_", StringComparison.Ordinal) ? "devil_hand" : null;
                if (n == "Hero_warrior") { sr.sprite = null; continue; }
                if (art == null) continue;
                var s = Art.Part(art, sr.sprite);
                if (s != null) sr.sprite = s;
                if (n == "Body") { body = sr.transform; bodySr = sr; }
            }
            // the wings: on the back, behind the body, flapping
            if (body != null)
                foreach (var (side, x) in new[] { ("l", -0.2f), ("r", 0.2f) })
                {
                    var w = new GameObject("Wing_" + side.ToUpperInvariant());
                    w.transform.SetParent(body, false);
                    w.transform.localPosition = new Vector3(x, -0.05f, 0f);
                    var sr = w.AddComponent<SpriteRenderer>();
                    sr.sortingLayerID = bodySr.sortingLayerID; sr.sortingOrder = bodySr.sortingOrder - 2; sr.sharedMaterial = bodySr.sharedMaterial;
                    var pivot = side == "l" ? new Vector2(21f / 22f, 0.56f) : new Vector2(1f / 22f, 0.56f);
                    var f0 = Art.Pivoted("devil_wing_" + side + "_0", pivot, bodySr.sprite != null ? bodySr.sprite.pixelsPerUnit : 38f);
                    var f1 = Art.Pivoted("devil_wing_" + side + "_1", pivot, bodySr.sprite != null ? bodySr.sprite.pixelsPerUnit : 38f);
                    sr.sprite = f0;
                    w.AddComponent<Flap>().Init(f0, f1);
                }
            try
            {
                var refDead = mob.deadBodySprites != null && mob.deadBodySprites.Length > 0 ? mob.deadBodySprites[0] : null;
                var dead = Art.Part("devil_dead", refDead, 36f);
                if (dead != null) mob.deadBodySprites = new[] { dead };
            }
            catch { }
            try { mob.creatureLocalizedName = "Sanctum/Devil"; } catch { }
            mob.creatureID = Bestiary.DevilId;
            mob.customPreviewSprite = Art.Pivoted("devil_preview", new Vector2(0.5f, 0.5f), 36f);
        }
    }

    public class DevilTag : MonoBehaviour { }

    /// the wings beat
    public class Flap : MonoBehaviour
    {
        Sprite a, b; SpriteRenderer sr; float t;
        public void Init(Sprite a, Sprite b) { this.a = a; this.b = b; sr = GetComponent<SpriteRenderer>(); t = UnityEngine.Random.value; }
        void Update()
        {
            t += Time.deltaTime;
            sr.sprite = (t % 0.22f) < 0.11f ? a : b;
        }
    }
}
