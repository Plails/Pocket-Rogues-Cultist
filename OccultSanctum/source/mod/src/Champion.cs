using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OccultSanctum
{
    /// The cult's champion (Чемпион культа), after the user's concept: the game's guardsman warrior (its shapes, its moves
    /// and its mind) recoloured - black robe, steel pauldrons with pale spikes, a grey tabard, a pale bald head, dark
    /// sleeves and steel gauntlets, a big blackened morningstar; standing a head taller than the guardsman. He is of the
    /// game's "champion" rank (purple name, more loot) and much tougher, and goes the rounds of the whole sanctum with
    /// two soldiers behind him.
    public static class Champion
    {
        public const string Id = "OccultChampion";
        /// taken out for now (the user): he does not appear and is not in the bestiary
        public const bool Enabled = false;
        public const float Size = 1f, HpMult = 2.2f;          // (the guardsman is 0.85 in the game: the champion stands a head taller)
        static GameObject prefab;
        static bool looked;

        static GameObject Prefab()
        {
            if (!looked)
            {
                looked = true;
                prefab = Prisoner.FindMob(new[] { "Guardsman_Warrior", "GuardsmanWarrior" }, "Guardsman", "Warrior");
                SanctumPlugin.Log.LogInfo("Sanctum: the champion is made from " + (prefab != null ? prefab.name : "nothing (no ogre warrior found)"));
            }
            return prefab;
        }

        public static GameObject PrefabForBestiary() => Prefab();
        public static void DressForBestiary(GameObject go, Mob mob) { Dress(go, mob); mob.mobPriority = Mob.MobPriority.hero; }

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
            mob.mobPriority = Mob.MobPriority.hero;                                  // the game's champion rank: a purple name, more loot
            mob.lvl = Mathf.Max(1, Generation.GetRandomMobLvl() + 2);
            mob.Init();
            try { mob.HP_max = mob.HP_max * HpMult; mob.HP_cur = mob.HP_max; } catch { }
            go.transform.localScale = Vector3.one * Size;                             // (after Init: not the game's random size)
            Dress(go, mob);
            var ai = go.GetComponent<AIController>();
            if (ai != null) try { GameManager.Instance.AddMob(ai); } catch { }
            SanctumPlugin.Log.LogInfo("Sanctum: the champion stands at " + pos + " (HP " + mob.HP_max + ")");
            return mob;
        }

        static void Dress(GameObject go, Mob mob)
        {
            go.name = "CultChampion";
            if (go.GetComponent<ChampionTag>() == null) go.AddComponent<ChampionTag>();
            // the guardsman's own parts recoloured after the concept (same shapes and moves): black robe, steel pauldrons
            // with pale spikes, a grey tabard, a pale bald head, dark sleeves, steel gauntlets, a blackened morningstar
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
            {
                string n = sr.gameObject.name;
                if (n == "HandEquip_L") { sr.sprite = null; sr.enabled = false; continue; }               // no shield, nothing in the left hand
                if (n == "HandEquip_R")                                                                     // the morningstar, bigger
                {
                    var m = Art.Part("champ_g_mace", sr.sprite); if (m != null) sr.sprite = m;
                    sr.transform.localScale = sr.transform.localScale * 1.35f;
                    continue;
                }
                string art = n == "Body" ? "champ_g_body" : n == "Head" ? "champ_g_head" : n.StartsWith("UpperArm", StringComparison.Ordinal) ? "champ_g_upperarm"
                           : n.StartsWith("Arm_", StringComparison.Ordinal) ? "champ_g_forearm" : n.StartsWith("Hand_", StringComparison.Ordinal) ? "champ_g_hand"
                           : n.StartsWith("Leg_", StringComparison.Ordinal) ? "champ_g_leg" : n == "Hero_warrior" ? "champ_g_preview" : null;
                if (art == null) continue;
                var s = Art.Part(art, sr.sprite);
                if (s != null) sr.sprite = s;
            }
            try { var dead = Art.Pivoted("champ_dead", new Vector2(0.5f, 0.5f), 28f); if (dead != null) mob.deadBodySprites = new[] { dead }; } catch { }
            try { mob.creatureLocalizedName = "Sanctum/Champion"; } catch { }
            mob.creatureID = Id;
            Human.Apply(mob);
            mob.customPreviewSprite = Art.Pivoted("champ_g_preview", new Vector2(0.5f, 0.5f), 34f);
            var ai = go.GetComponent<AIController>();
            if (ai != null)
            {
                // a fighter with his mace: no shield blocks, no ramming runs or pushing charges, nothing thrown
                foreach (var bh in new[] { AIController.BEHAVIORS.USE_SHIELD, AIController.BEHAVIORS.RUN_ATTACK, AIController.BEHAVIORS.CUSTOM_RUN, AIController.BEHAVIORS.PUSH_ATTACK,
                                           AIController.BEHAVIORS.COMBAT_DISTANCE, AIController.BEHAVIORS.STRENGTH_DISTANCE, AIController.BEHAVIORS.FIRE_ON_DISTANCE, AIController.BEHAVIORS.MULTIARCH })
                    ai.RemoveBehavior(bh);
                if (!ai.HasBehavior(AIController.BEHAVIORS.COMBAT_MELEE)) ai.AddBehavior(AIController.BEHAVIORS.COMBAT_MELEE);
            }
        }

        /// the champion and his two soldiers, going the rounds of the whole sanctum: every side room and the hall
        public static void Place(Vector2 at, Vector2 heart, System.Random rnd)
        {
            var ch = Spawn(at);
            if (ch == null) return;
            var rooms = SideRoom.Rooms.Select(x => Room.W(x.r.xMin + x.r.width / 2f - .5f, x.r.yMin + x.r.height / 2f - .5f)).ToList();
            var pts = new List<Vector2>(rooms) { heart, heart + new Vector2(3f, 2f), heart + new Vector2(-3f, -2f) };
            var route = new List<Vector2>(); Vector2 cur = at;
            while (pts.Count > 0) { var nx = pts.OrderBy(p => (p - cur).sqrMagnitude).First(); route.Add(nx); pts.Remove(nx); cur = nx; }
            var lead = Activity.Give(ch, Activity.Kind.Patrol, heart, route.ToArray());
            if (lead == null) return;
            lead.Stay = true;
            // his escort: two spearmen, a spearman and a crossbowman, or two crossbowmen
            int kind = rnd.Next(3);
            for (int k = 1; k <= 2; k++)
            {
                bool spear = kind == 0 || (kind == 1 && k == 1);
                Vector2 p = at + new Vector2(k == 1 ? -0.9f : 0.9f, -0.8f);
                var m = spear ? Spearman.Spawn(p) : Crossbowman.Spawn(p);
                var f = Activity.Give(m, Activity.Kind.Follow, heart);
                if (f == null) continue;
                f.Leader = ch.transform; f.Slot = k; f.Stay = true;
                lead.Mates.Add(f);
            }
            SanctumPlugin.Log.LogInfo("Sanctum: the champion goes the rounds of " + route.Count + " places with " + lead.Mates.Count + " soldiers (" + (kind == 0 ? "two spearmen" : kind == 1 ? "a spearman and a crossbowman" : "two crossbowmen") + ")");
        }
    }

    public class ChampionTag : MonoBehaviour { }
}
