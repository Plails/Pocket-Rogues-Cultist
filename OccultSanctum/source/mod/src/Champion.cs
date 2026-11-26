using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OccultSanctum
{
    /// The cult's general (Чемпион культа), after the user's concept, drawn whole by hand (general_art.py): a big man in a
    /// black robe with a grey tabard, blackened steel pauldrons with long pale spikes, a pale bald scarred head, dark
    /// sleeves with steel bracers, iron gauntlets, black boots and a huge two-handed spiked morningstar. Not a recoloured
    /// monster: every picture is new; only the bones and the moves are the game's two-handed rig (the one its
    /// two-handed fighters use - idle, walk, the overhead two-handed blow and the power blow), so he holds the mace in
    /// both hands and brings it down with both. Of the game's "champion" rank (purple name, more loot), much tougher,
    /// much bigger than the others (1.45); a man (voice, red blood, not undead). Goes the rounds of the whole sanctum with
    /// two soldiers behind him.
    public static class Champion
    {
        public const string Id = "OccultChampion";
        public const bool Enabled = true;
        public const float HpMult = 2.2f, DmgMult = 1.35f;                       // (his size: Sizer.General)
        static GameObject prefab;
        static bool looked;

        /// the game's two-handed rig: the skeleton axeman's bones and moves (his pictures all replaced)
        static GameObject Prefab()
        {
            if (!looked)
            {
                looked = true;
                prefab = Prisoner.FindMob(new[] { "SkeletonAxeman", "Skeleton_Axeman" }, "Axeman");
                SanctumPlugin.Log.LogInfo("Sanctum: the general's rig is " + (prefab != null ? prefab.name : "nothing (no two-handed rig found)"));
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
            try { mob.attackDamage = Math.Max(mob.attackDamage, OfficerDamage()) * DmgMult; } catch { }   // his blows hit harder (the user), and grow with the level like the goblin officer's (0.40.0)
            go.transform.localScale = Vector3.one;                                    // (after Init: not the game's random size)
            Sizer.FitChampion(go, Sizer.ChampionBody() * 0.9f);                       // (0.41.1, the user) the old size (the game's champions) back, a little smaller; the goblin-captain size was too small
            go.AddComponent<WarCry>();
            Dress(go, mob);
            var ai = go.GetComponent<AIController>();
            if (ai != null) try { GameManager.Instance.AddMob(ai); } catch { }
            SanctumPlugin.Log.LogInfo("Sanctum: the general stands at " + pos + " (HP " + mob.HP_max + ")");
            return mob;
        }

        /// (0.40.0, the user: "his attacks must grow with the level just like the goblin general's") the damage the game
        /// gives its goblin officer (a champion boss) here: his own base damage raised by the game's champion-boss
        /// formula at the champion-boss level of this floor
        static float OfficerDamage()
        {
            float baseDmg = 0f;
            try { foreach (var b in Resources.LoadAll<BossAI_H_GoblinOfficer>("")) { var cr = b != null ? b.GetComponent<Creature>() : null; if (cr != null && cr.attackDamage > 0f) { baseDmg = cr.attackDamage; break; } } } catch { }
            if (baseDmg <= 0f) baseDmg = 10f;
            int lvl = 1;
            try { lvl = Math.Max(1, BossAI.GetLvl(true)); } catch { }
            BossAI.GetLevelStatMods(lvl, true, out _, out float dm);
            float d = baseDmg + baseDmg * dm;
            SanctumPlugin.Log.LogInfo("Sanctum: the general hits like the goblin officer: base " + baseDmg + ", champion level " + lvl + " -> " + d);
            return d;
        }

        static void Dress(GameObject go, Mob mob)
        {
            go.name = "CultGeneral";
            if (go.GetComponent<ChampionTag>() == null) go.AddComponent<ChampionTag>();
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
            {
                string n = sr.gameObject.name;
                if (n == "HandEquip_L") { sr.sprite = null; continue; }                                   // both hands are on the mace
                if (n == "Hero_warrior") { var pv = Art.Pivoted("gen_preview", new Vector2(0.5f, 0.5f), 96f); if (pv != null) sr.sprite = pv; continue; }
                // the arms: the game's own arm shapes for this two-handed rig (its two-handed berserker's), each at its
                // own pivot, twice the pixels - so they sit on the joints and close round the haft like the game's own
                Sprite armS = n.StartsWith("UpperArm", StringComparison.Ordinal) ? Art.Pivoted("gen_upperarm", new Vector2(0.5f, 5f / 12f), 42f)
                            : n.StartsWith("Arm_", StringComparison.Ordinal) ? Art.Pivoted("gen_forearm", new Vector2(1f / 3f, 0.25f), 42f)
                            : n.StartsWith("Hand_", StringComparison.Ordinal) ? Art.Pivoted("gen_hand", new Vector2(0.5f, 0.4f), 42f) : null;
                if (armS != null) { sr.sprite = armS; sr.color = Color.white; continue; }
                string art = n == "Body" ? "gen_body" : n == "Head" ? "gen_head" : n == "HandEquip_R" ? "gen_mace"
                           : n.StartsWith("Leg_", StringComparison.Ordinal) ? "gen_leg" : null;
                if (art == null) continue;
                var s = Art.PartK(art, sr.sprite, 1f);                                // the rig's own size: his pixels as big as everyone's (the user)
                if (s != null) { sr.sprite = s; sr.color = Color.white; }
            }
            foreach (var lr in go.GetComponentsInChildren<LineRenderer>(true)) { var c = new Color(0.07f, 0.06f, 0.08f, 1f); lr.startColor = c; lr.endColor = c; }   // the legs drawn by lines: black, not bone
            // his corpse drawn anew like the game's corpses: on his back, arms flung out, the mace by him (gen_dead_art.py)
            try { var dead = Art.Pivoted("gen_dead", new Vector2(0.5f, 0.5f), 64f); if (dead != null) mob.deadBodySprites = new[] { dead }; } catch { }
            try { mob.creatureLocalizedName = "Sanctum/Champion"; } catch { }
            mob.creatureID = Id;
            Human.Apply(mob);
            try { var d = Human.DonorMob(); if (d != null) mob.mobClass = d.mobClass; } catch { }      // a man, not undead
            mob.customPreviewSprite = Snapshot.Of(go, mob.creatureID, Art.Pivoted("gen_preview", new Vector2(0.5f, 0.5f), 68f), 1.7f);   // big, like the game's bosses (the user)   // taken from himself (the user: the hand-made ones were crooked)
            var ai = go.GetComponent<AIController>();
            if (ai != null)
            {
                // he fights in close with his two-handed blows: no thrown things, no shooting
                foreach (var bh in new[] { AIController.BEHAVIORS.FIRE_ON_DISTANCE, AIController.BEHAVIORS.MULTIARCH, AIController.BEHAVIORS.MULTIARCH_HUNTER, AIController.BEHAVIORS.STRENGTH_DISTANCE })
                    ai.RemoveBehavior(bh);
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
