using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace OccultSanctum
{
    /// A monster shut in one of the sanctum's cages. It is a real monster of the game, but while the cage stands it is
    /// not counted among the floor's monsters (so its mind does not run), it does not move, nothing can hurt it and it
    /// does not bump the bars. When the cage is broken it is let out: counted, awake, hurtable, free to walk.
    public class Prisoner : MonoBehaviour
    {
        public static readonly HashSet<Creature> Caged = new HashSet<Creature>();
        Container cage;
        Mob mob;
        AIController ai;
        Rigidbody2D rb;
        RigidbodyType2D bodyWas;
        readonly List<(Collider2D, Collider2D)> ignored = new List<(Collider2D, Collider2D)>();
        Vector2 spot;
        bool handedOver;                     // taken out by a cultist: no longer this cage's, not let loose either

        public Container CageOf => cage;
        public Mob MobOf => mob;

        /// the prisoner shut in this cage, if any
        public static Prisoner In(Container c) => c == null ? null : FindObjectsOfType<Prisoner>().FirstOrDefault(p => p.cage == c && p.mob != null && p.mob.alive);

        /// a cultist takes him out: from now on he is led about (still harmless and unharmed)
        public Led TakeOut()
        {
            var l = new Led { mob = mob, ai = ai, rb = rb, was = bodyWas };
            l.ign.AddRange(ignored);
            handedOver = true;
            Destroy(this);
            return l;
        }

        /// a led one shut in a cage: he stays there like any prisoner (out and fighting if the cage is broken)
        public static void Keep(Led l, Container c)
        {
            if (l == null || !l.Ok || c == null) return;
            var p = l.mob.gameObject.AddComponent<Prisoner>();
            p.cage = c; p.mob = l.mob; p.ai = l.ai; p.rb = l.rb; p.bodyWas = l.was; p.spot = c.transform.position;
            foreach (var mc in l.mob.GetComponentsInChildren<Collider2D>(true))
                foreach (var cc in c.GetComponentsInChildren<Collider2D>(true))
                    if (mc != null && cc != null) { Physics2D.IgnoreCollision(mc, cc, true); p.ignored.Add((mc, cc)); }
            Caged.Add(l.mob);
            l.Given();
        }

        static readonly Dictionary<string, string[]> Kinds = new Dictionary<string, string[]>
        {
            { "skel", new[] { "Skeleton", "Skeleton_Swordsman", "SkeletonWarrior" } },
            { "rot", new[] { "Zombie_Mutant", "ZombieMutant" } },
            { "zomb", new[] { "ZombiePrisoner", "Zombie_Prisoner", "Zombie", "Zombie_Fighter", "ZombieFighter", "Zombie_Swordsman", "ZombieSwordsman", "Zombie_Mutant" } },
        };

        static Dictionary<string, GameObject> mobs;
        /// a monster prefab of the game by one of its names, else the first whose name contains all the given words
        public static GameObject FindMob(string[] names, params string[] words)
        {
            MobPrefab("skel");
            foreach (var n in names) if (mobs.TryGetValue(n, out var g)) return g;
            return mobs.Where(kv => words.All(w => kv.Key.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0)).Select(kv => kv.Value).FirstOrDefault();
        }

        public static GameObject PrefabOf(string kind) => MobPrefab(kind);

        static GameObject MobPrefab(string kind)
        {
            if (mobs == null)
            {
                mobs = new Dictionary<string, GameObject>();
                try { foreach (var m in Resources.LoadAll<Mob>("")) if (m != null && !mobs.ContainsKey(m.name)) mobs[m.name] = m.gameObject; }
                catch (Exception e) { SanctumPlugin.Log.LogWarning("Mobs: " + e.Message); }
            }
            foreach (var n in Kinds[kind]) if (mobs.TryGetValue(n, out var g)) return g;
            // not under those names: any zombie / skeleton of the game
            string w = kind == "skel" ? "Skeleton" : "Zombie";
            return mobs.Where(kv => kv.Key.StartsWith(w, StringComparison.OrdinalIgnoreCase)).Select(kv => kv.Value).FirstOrDefault();
        }

        public static void Shut(string kind, Vector2 pos, Container cage)
        {
            var prefab = MobPrefab(kind);
            if (prefab == null) { SanctumPlugin.Log.LogWarning("Sanctum: no monster for a " + kind + " cage"); return; }
            var go = Builder.Spawn(prefab, pos, UnityEngine.Random.Range(0f, 360f));
            if (go == null) return;
            Builder.Own(go);
            var mob = go.GetComponent<Mob>();
            if (mob == null) { UnityEngine.Object.Destroy(go); return; }
            mob.lvl = Mathf.Max(1, Generation.GetRandomMobLvl() + 1);
            mob.Init();
            var p = go.AddComponent<Prisoner>();
            p.cage = cage; p.mob = mob; p.spot = pos;
            p.ai = go.GetComponent<AIController>();
            if (p.ai != null)
            {
                p.ai._enableAI = false;
                // a monster puts itself on the floor's list when it appears: not while it is shut in
                var gm = GameManager.Instance;
                if (gm != null && gm.mobsAI.Remove(p.ai))
                {
                    if (gm.mobsGo.Remove(go)) gm.mobsCount = Mathf.Max(0, gm.mobsCount - 1);
                    gm.mobsGoAlt.Remove(go);
                }
            }
            p.rb = go.GetComponent<Rigidbody2D>();
            if (p.rb != null) { p.bodyWas = p.rb.bodyType; p.rb.velocity = Vector2.zero; p.rb.bodyType = RigidbodyType2D.Kinematic; }
            foreach (var mc in go.GetComponentsInChildren<Collider2D>(true))
                foreach (var cc in cage.GetComponentsInChildren<Collider2D>(true))
                    if (mc != null && cc != null) { Physics2D.IgnoreCollision(mc, cc, true); p.ignored.Add((mc, cc)); }
            Caged.Add(mob);
            SanctumPlugin.Log.LogInfo($"Sanctum: {go.name} (lvl {mob.lvl}) shut in a cage at {pos}");
        }

        /// a skeleton led about by a cultist: out of the cage behind him, into a cage, out again. While led it does not
        /// fight and takes no harm; let loose (its keeper saw the hero, or fell, or its cage broke) it fights.
        public class Led
        {
            public Mob mob; public AIController ai; public Rigidbody2D rb; public RigidbodyType2D was; bool loose;
            public readonly List<(Collider2D, Collider2D)> ign = new List<(Collider2D, Collider2D)>();
            /// handed over to a cage (a Prisoner again): no longer led
            public void Given() { loose = true; }
            /// put to death by his keeper
            public void Kill()
            {
                if (mob == null || loose) return;
                loose = true;
                Caged.Remove(mob);
                if (rb != null) rb.bodyType = was;
                try { mob.Debug_Kill(); } catch (Exception e) { SanctumPlugin.Log.LogWarning("Sanctum execution: " + e.Message); }
            }
            /// stands him before his keeper (facing him)
            public void Before(Transform keeper)
            {
                if (!Ok || keeper == null) return;
                Vector2 np = (Vector2)keeper.position + (Vector2)keeper.right * 0.85f;
                if (rb != null) rb.position = np; else mob.transform.position = new Vector3(np.x, np.y, mob.transform.position.z);
                Vector2 f = (Vector2)keeper.position - np;
                mob.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(f.y, f.x) * Mathf.Rad2Deg);
                Ritual.MoveAnim(mob, false);
            }
            public bool Inside;
            public bool Ok => mob != null && mob.alive && !loose;

            public void Follow(Transform keeper)
            {
                if (!Ok || keeper == null) return;
                Vector2 to = (Vector2)keeper.position - (Vector2)keeper.right * 0.8f, at = mob.transform.position, d = to - at;
                bool moving = d.sqrMagnitude > 0.02f;
                Vector2 np = moving ? at + d.normalized * Mathf.Min(d.magnitude, 1.7f * Time.deltaTime) : at;
                if (rb != null) rb.position = np; else mob.transform.position = new Vector3(np.x, np.y, mob.transform.position.z);
                Vector2 f = (Vector2)keeper.position - np;
                if (f.sqrMagnitude > 0.01f) mob.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(f.y, f.x) * Mathf.Rad2Deg);
                Ritual.MoveAnim(mob, moving);
            }

            public void PutIn(Container cage)
            {
                if (!Ok || cage == null) return;
                foreach (var mc in mob.GetComponentsInChildren<Collider2D>(true))
                    foreach (var cc in cage.GetComponentsInChildren<Collider2D>(true))
                        if (mc != null && cc != null) { Physics2D.IgnoreCollision(mc, cc, true); ign.Add((mc, cc)); }
                Vector2 p = cage.transform.position;
                if (rb != null) rb.position = p; else mob.transform.position = new Vector3(p.x, p.y, mob.transform.position.z);
                Ritual.MoveAnim(mob, false);
                Inside = true;
            }

            public void TakeOut(Transform keeper) { Inside = false; }

            public void Loose()
            {
                if (loose || mob == null) return;
                loose = true;
                Caged.Remove(mob);
                foreach (var (a, b) in ign) if (a != null && b != null) Physics2D.IgnoreCollision(a, b, false);
                if (rb != null) rb.bodyType = was;
                if (ai != null) { ai._enableAI = true; try { GameManager.Instance.AddMob(ai); } catch { } }
                Hostile(ai);
                SanctumPlugin.Log.LogInfo("Sanctum: a led skeleton is let loose");
            }

            public static Led Make(GameObject go)
            {
                var l = new Led { mob = go.GetComponent<Mob>(), ai = go.GetComponent<AIController>(), rb = go.GetComponent<Rigidbody2D>() };
                if (l.ai != null)
                {
                    l.ai._enableAI = false;
                    var gm = GameManager.Instance;
                    if (gm != null && gm.mobsAI.Remove(l.ai)) { if (gm.mobsGo.Remove(go)) gm.mobsCount = Mathf.Max(0, gm.mobsCount - 1); gm.mobsGoAlt.Remove(go); }
                }
                if (l.rb != null) { l.was = l.rb.bodyType; l.rb.velocity = Vector2.zero; l.rb.bodyType = RigidbodyType2D.Kinematic; }
                Caged.Add(l.mob);
                return l;
            }
        }

        /// a skeleton (or another prisoner kind) to be led about by a cultist
        public static Led Lead(string kind, Vector2 pos)
        {
            var prefab = MobPrefab(kind);
            if (prefab == null) return null;
            var go = Builder.Spawn(prefab, pos, 0f);
            if (go == null) return null;
            Builder.Own(go);
            var mob = go.GetComponent<Mob>();
            if (mob == null) { UnityEngine.Object.Destroy(go); return null; }
            mob.lvl = Mathf.Max(1, Generation.GetRandomMobLvl() + 1);
            mob.Init();
            return Led.Make(go);
        }

        void FixedUpdate()
        {
            if (mob == null) { Destroy(this); return; }
            if (cage == null || !cage.gameObject.activeInHierarchy || cage.hp <= 0f) { Release(); return; }
            // it stays in its cage
            if (rb != null) { rb.velocity = Vector2.zero; rb.position = spot; }
            else mob.transform.position = new Vector3(spot.x, spot.y, mob.transform.position.z);
        }

        void Release()
        {
            Caged.Remove(mob);
            foreach (var (a, b) in ignored) if (a != null && b != null) Physics2D.IgnoreCollision(a, b, false);
            if (rb != null) rb.bodyType = bodyWas;
            if (ai != null)
            {
                ai._enableAI = true;
                try { GameManager.Instance.AddMob(ai); } catch (Exception e) { SanctumPlugin.Log.LogWarning("Sanctum prisoner: " + e.Message); }
            }
            try { GameManager.Instance.Recalculate_AI_Range(); } catch { }
            Hostile(ai);
            SanctumPlugin.Log.LogInfo("Sanctum: " + mob.name + " is out of its cage");
            Destroy(this);
        }

        void OnDestroy() { if (mob != null && !handedOver) Caged.Remove(mob); }

        /// out of the cage, a prisoner hates his jailers: the game's own "madness" side - he fights the cultists (and
        /// they him) as well as the hero
        public static void Hostile(AIController ai)
        {
            if (ai == null) return;
            try
            {
                ai.ChangeFaction(global::c.Factions.Madness);
                var gm = GameManager.Instance;
                if (gm != null)
                    foreach (var o in gm.mobsAI.ToList())
                        if (o != null && o != ai && (o.transform.position - ai.transform.position).sqrMagnitude < 100f) o.OverlapTargets();
            }
            catch (Exception e) { SanctumPlugin.Log.LogWarning("Sanctum prisoner side: " + e.Message); }
        }
    }

    /// nothing hurts a monster still shut in its cage
    [HarmonyPatch(typeof(Creature), nameof(Creature.SetDamageRPC))]
    internal static class CagedDamagePatch
    {
        [HarmonyPriority(Priority.High)]
        static bool Prefix(Creature __instance)
        {
            try { return Prisoner.Caged.Count == 0 || !Prisoner.Caged.Contains(__instance); }
            catch { return true; }
        }
    }
}
