using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OccultSanctum
{
    /// The cult's second champion, the archmage (Архимаг культа), after the user: an elite cultist, a mage, as big as
    /// the general, finely dressed in the cultists' own style (magus_art.py: skull pauldrons, a gold chain with a blood
    /// gem, a crowned and horned hood with red eyes, wide sleeves, glowing hands). He is the cultist's own rig and mind
    /// (the game's goblin wizard: its moves, its bolt) with every picture new at twice the cultist's pixels, so at his
    /// size his pixels are as big as the soldiers'. For now he fights only with the cultist's attack, only wider: his
    /// cursed spheres are big (Magus.BoltWidth). The game's champion rank (purple name, more loot), much tougher; goes
    /// the rounds of the sanctum with two cultists behind him.
    public static class Magus
    {
        public const string Id = "OccultMagus";
        public const float HpMult = 2.0f, BoltWidth = 2.2f;
        static GameObject prefab;
        static bool looked;

        static GameObject Prefab()
        {
            if (!looked)
            {
                looked = true;
                prefab = Prisoner.FindMob(new[] { "GoblinWizard1", "GoblinWizard", "Goblin_Wizard1", "GoblinWizard2" }, "Goblin", "Wiz")
                      ?? Prisoner.FindMob(new[] { "GoblinWizard2" }, "Goblin", "Mage");
                SanctumPlugin.Log.LogInfo("Sanctum: the archmage's rig is " + (prefab != null ? prefab.name : "nothing (no goblin wizard found)"));
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
            Dress(go, mob);
            go.transform.localScale = Vector3.one;                                    // (after Init: not the game's random size)
            Sizer.FitChampion(go);                                                    // as big as the game's champions, always the same (the user)
            // his laser (the user): now and then a thick violet beam instead of his spheres; a mage's dark flame or a
            // necromancer's cursed breath turned on it starts the magic duel - the beams push each other back and forth
            var lz = go.AddComponent<ElderBeam>();
            lz.Who = "the archmage"; lz.Width = 1.5f; lz.WizardAnim = true;
            lz.BHalo = new Color(0.36f, 0.08f, 0.55f, 0.5f); lz.BBody = new Color(0.72f, 0.3f, 1f, 0.95f); lz.BCore = new Color(0.97f, 0.88f, 1f, 1f);
            var ai = go.GetComponent<AIController>();
            if (ai != null) try { GameManager.Instance.AddMob(ai); } catch { }
            SanctumPlugin.Log.LogInfo("Sanctum: the archmage stands at " + pos + " (HP " + mob.HP_max + ")");
            return mob;
        }

        static void Dress(GameObject go, Mob mob)
        {
            go.name = "CultArchmage";
            if (go.GetComponent<MagusTag>() == null) go.AddComponent<MagusTag>();
            if (go.GetComponent<SteadyLegs>() == null) go.AddComponent<SteadyLegs>();
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
            {
                string n = sr.gameObject.name;
                if (n.StartsWith("HandEquip", StringComparison.Ordinal)) { sr.sprite = null; sr.enabled = false; continue; }   // bare hands, like the cultists
                // (the user, 2026-11-21: the old archmage back - the elder parts recoloured, magus_from_elder.py)
                string art = n == "Body" ? "mag_body" : n == "Head" ? "mag_head" : n.StartsWith("UpperArm", StringComparison.Ordinal) ? "mag_upperarm"
                           : n.StartsWith("Arm_", StringComparison.Ordinal) ? "mag_forearm" : n.StartsWith("Hand_", StringComparison.Ordinal) ? "mag_hand"
                           : n.StartsWith("Leg_", StringComparison.Ordinal) ? "mag_leg" : null;
                if (art == null) continue;
                var s = Art.PartK(art, sr.sprite, 2f);                             // the old archmage (the user, 2026-11-21: "верни старого")
                if (s != null) { sr.sprite = s; sr.color = Color.white; }
            }
            foreach (var lr in go.GetComponentsInChildren<LineRenderer>(true)) { var c = new Color(0.07f, 0.05f, 0.08f, 1f); lr.startColor = c; lr.endColor = c; }
            try { var dead = Art.Pivoted("mag_dead", new Vector2(0.5f, 0.5f), 110f); if (dead != null) mob.deadBodySprites = new[] { dead }; } catch { }
            try { mob.creatureLocalizedName = "Sanctum/Magus"; } catch { }
            mob.creatureID = Id;
            Human.Apply(mob);                                                      // a man's voice, red blood
            mob.customPreviewSprite = Snapshot.Of(go, mob.creatureID, null, 1.7f);  // big, like the general's (with the bosses)
        }

        /// the archmage and two cultists, going the rounds of the sanctum: the side rooms and the hall
        public static void Place(Vector2 at, Vector2 heart, System.Random rnd)
        {
            var m = Spawn(at);
            if (m == null) return;
            var rooms = SideRoom.Rooms.Select(x => Room.W(x.r.xMin + x.r.width / 2f - .5f, x.r.yMin + x.r.height / 2f - .5f)).OrderBy(_ => rnd.Next()).ToList();
            var pts = new List<Vector2>(rooms) { heart + new Vector2(-3f, 2f), heart + new Vector2(3f, -2f) };
            var route = new List<Vector2>(); Vector2 cur = at;
            while (pts.Count > 0) { var nx = pts.OrderBy(p => (p - cur).sqrMagnitude).First(); route.Add(nx); pts.Remove(nx); cur = nx; }
            route.Reverse();                                                          // (the other way round from the general)
            var lead = Activity.Give(m, Activity.Kind.Patrol, heart, route.ToArray());
            if (lead == null) return;
            lead.Stay = true;
            for (int k = 1; k <= 2; k++)
            {
                var c = Cultist.Spawn(at + new Vector2(k == 1 ? -0.9f : 0.9f, -0.8f));
                var f = Activity.Give(c, Activity.Kind.Follow, heart);
                if (f == null) continue;
                f.Leader = m.transform; f.Slot = k; f.Stay = true;
                lead.Mates.Add(f);
            }
            SanctumPlugin.Log.LogInfo("Sanctum: the archmage goes the rounds of " + route.Count + " places with " + lead.Mates.Count + " cultists");
        }
    }

    public class MagusTag : MonoBehaviour { }

    /// (the user: the archmage's legs twitched) his legs step only while he really moves: the walking flag of his
    /// animator follows how far he went in the last moment, so nothing switching it on and off frame by frame can
    /// make the legs jerk while he stands
    public class SteadyLegs : MonoBehaviour
    {
        Mob mob; Animator an; Vector2 last; float speed;
        void Start() { mob = GetComponent<Mob>(); an = GetComponent<Animator>(); last = transform.position; }
        void LateUpdate()
        {
            if (mob == null || !mob.alive || Time.deltaTime <= 0f) return;
            Vector2 now = transform.position;
            float v = (now - last).magnitude / Time.deltaTime; last = now;
            speed = Mathf.Lerp(speed, v, Mathf.Clamp01(Time.deltaTime * 8f));
            bool moving = speed > 0.35f;
            Ritual.MoveAnim(mob, moving);
            if (!moving && an != null) { try { an.SetFloat("Move_F", 0f); an.SetFloat("Move_H", 0f); } catch { } }
        }
    }
}
