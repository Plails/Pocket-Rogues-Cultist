using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CultistRescue
{
    /// the Occult Sanctum's side: after a sanctum is built, maybe one of its cages gets the Cultist instead of its monster
    public static class Rescue
    {
        static Type prisonerT;
        static MethodInfo prisonerIn;
        static PropertyInfo prisonerMob;
        static FieldInfo cagedF;

        public static void PatchSanctum(Harmony h)
        {
            try
            {
                var builder = AccessTools.TypeByName("OccultSanctum.SanctumBuilder");
                var build = builder != null ? AccessTools.Method(builder, "Build") : null;
                prisonerT = AccessTools.TypeByName("OccultSanctum.Prisoner");
                if (prisonerT != null)
                {
                    prisonerIn = AccessTools.Method(prisonerT, "In", new[] { typeof(Container) });
                    prisonerMob = AccessTools.Property(prisonerT, "MobOf");
                    cagedF = AccessTools.Field(prisonerT, "Caged");
                }
                if (build == null) { RescuePlugin.Log.LogWarning("Cultist Rescue: the Occult Sanctum is not installed - nobody to rescue"); return; }
                h.Patch(build, postfix: new HarmonyMethod(typeof(Rescue), nameof(AfterBuild)));
                RescuePlugin.Log.LogInfo("Cultist Rescue: watching the Occult Sanctum's cages");
            }
            catch (Exception e) { RescuePlugin.Log.LogError("Cultist Rescue: sanctum patch: " + e); }
        }

        /// a monster still shut in a sanctum cage (they do not count among the floor's foes)
        public static bool IsCaged(Creature c)
        {
            try { return cagedF?.GetValue(null) is HashSet<Creature> set && set.Contains(c); } catch { return false; }
        }

        static void AfterBuild()
        {
            try
            {
                if (GameManager.Instance != null && GameManager.Instance.IsConnected) return;
                if (Unlock.IsOpen() && !RescuePlugin.CfgEvenIfUnlocked.Value) return;
                float roll = UnityEngine.Random.value * 100f;
                RescuePlugin.Log.LogInfo("Cultist Rescue: a sanctum - roll " + roll.ToString("0.0") + " of " + RescuePlugin.CfgChance.Value + "%");
                if (roll >= RescuePlugin.CfgChance.Value) return;
                Runner.I?.StartCoroutine(PutHimIn());
            }
            catch (Exception e) { RescuePlugin.Log.LogError("Cultist Rescue: " + e); }
        }

        static IEnumerator PutHimIn()
        {
            yield return new WaitForSeconds(0.5f);
            var cages = UnityEngine.Object.FindObjectsOfType<Container>().Where(c => c != null && c.name == "Sanctum_occ_cage" && c.hp > 0f && c.gameObject.activeInHierarchy).ToList();
            if (cages.Count == 0) { RescuePlugin.Log.LogWarning("Cultist Rescue: no cages in this sanctum"); yield break; }
            // a cage with a monster in it is taken (its monster goes), an empty one if there is no such cage
            var withMob = cages.Where(c => PrisonerIn(c) != null).ToList();
            var cage = (withMob.Count > 0 ? withMob : cages)[UnityEngine.Random.Range(0, (withMob.Count > 0 ? withMob : cages).Count)];
            var old = PrisonerIn(cage);
            if (old != null)
            {
                var m = prisonerMob?.GetValue(old) as Mob;
                if (m != null) UnityEngine.Object.Destroy(m.gameObject);
                UnityEngine.Object.Destroy(old);
            }
            yield return null;
            Captive.Make(cage);
        }

        static Component PrisonerIn(Container c)
        {
            try { return prisonerIn?.Invoke(null, new object[] { c }) as Component; } catch { return null; }
        }
    }

    /// the Cultist in his cage: the game's own berserker mercenary dressed as the Cultist. While the cage stands he is
    /// asleep, cannot be hurt and nobody counts him; when it breaks he joins the hero and fights at his side; when the
    /// sanctum is cleared he thanks the hero and the class opens
    public class Captive : MonoBehaviour
    {
        public static Captive Current;
        Container cage; Mob mob; AIController ai; Rigidbody2D rb; RigidbodyType2D bodyWas;
        readonly List<(Collider2D, Collider2D)> ignored = new List<(Collider2D, Collider2D)>();
        Vector2 spot;
        public bool Caged { get; private set; } = true;
        bool done; float nextCheck;

        static GameObject prefab;
        static GameObject Prefab()
        {
            if (prefab != null) return prefab;
            try { prefab = Resources.LoadAll<Mob>("").Where(m => m != null && m.name == "Mercenary_Berserk").Select(m => m.gameObject).FirstOrDefault(); }
            catch (Exception e) { RescuePlugin.Log.LogWarning("Cultist Rescue: " + e.Message); }
            if (prefab == null) RescuePlugin.Log.LogWarning("Cultist Rescue: Mercenary_Berserk not found");
            return prefab;
        }

        public static void Make(Container cage)
        {
            var p = Prefab();
            if (p == null || cage == null) return;
            Vector2 pos = cage.transform.position;
            GameObject go;
            try { go = CustomPhotonNetwork.Instantiate(p, pos, Quaternion.identity); }
            catch (Exception e) { RescuePlugin.Log.LogWarning("Cultist Rescue: spawn: " + e.Message); return; }
            if (go == null) return;
            go.name = "CultistRescue_Captive";
            // not a mercenary for hire: no hiring, no saving with the floor, no "hire me" button
            foreach (var mc in go.GetComponentsInChildren<MercenaryController>(true)) UnityEngine.Object.DestroyImmediate(mc);
            foreach (var so in go.GetComponentsInChildren<SavingObject>(true)) UnityEngine.Object.DestroyImmediate(so);
            foreach (var uo in go.GetComponentsInChildren<UsableObject>(true)) uo.gameObject.SetActive(false);
            var mob = go.GetComponent<Mob>();
            if (mob == null) { UnityEngine.Object.Destroy(go); return; }
            try
            {
                int deep = Mathf.Max(1, PlayerPrefs.GetInt("curDeep", 1));
                mob.lvl = Mathf.Max(5, Mathf.RoundToInt(deep * 2f / 5f) * 5);
                mob.initialed = false; mob.autoSetScale = false; mob.autoSetLvl = false;
                mob.Init();
                mob.Armor += mob.lvl;
                mob.HP_max += mob.lvl * 1.5f;
                mob.HP_cur = mob.HP_max; mob.ST_cur = mob.ST_max;
            }
            catch (Exception e) { RescuePlugin.Log.LogWarning("Cultist Rescue: init: " + e.Message); }
            var c = go.AddComponent<Captive>();
            c.cage = cage; c.mob = mob; c.spot = pos;
            c.ai = go.GetComponent<AIController>();
            if (c.ai != null)
            {
                c.ai._enableAI = false;
                var gm = GameManager.Instance;
                if (gm != null && gm.mobsAI.Remove(c.ai))
                {
                    if (gm.mobsGo.Remove(go)) gm.mobsCount = Mathf.Max(0, gm.mobsCount - 1);
                    gm.mobsGoAlt.Remove(go);
                }
            }
            c.rb = go.GetComponent<Rigidbody2D>();
            if (c.rb != null) { c.bodyWas = c.rb.bodyType; c.rb.velocity = Vector2.zero; c.rb.bodyType = RigidbodyType2D.Kinematic; }
            foreach (var mc in go.GetComponentsInChildren<Collider2D>(true))
                foreach (var cc in cage.GetComponentsInChildren<Collider2D>(true))
                    if (mc != null && cc != null) { Physics2D.IgnoreCollision(mc, cc, true); c.ignored.Add((mc, cc)); }
            mob.lockAsTarget = false;                                                         // nobody goes for him in the cage
            go.AddComponent<Dress>();
            Current = c;
            RescuePlugin.Log.LogInfo("Cultist Rescue: the Cultist (lvl " + mob.lvl + ", HP " + mob.HP_max + ") sits in a cage at " + pos);
        }

        void FixedUpdate()
        {
            if (mob == null) { Destroy(this); return; }
            if (!Caged) return;
            if (cage == null || !cage.gameObject.activeInHierarchy || cage.hp <= 0f) { Release(); return; }
            if (rb != null) { rb.velocity = Vector2.zero; rb.position = spot; }
            else mob.transform.position = new Vector3(spot.x, spot.y, mob.transform.position.z);
        }

        /// out of the cage: he is the hero's companion from now on
        void Release()
        {
            Caged = false;
            foreach (var (a, b) in ignored) if (a != null && b != null) Physics2D.IgnoreCollision(a, b, false);
            if (rb != null) { rb.bodyType = bodyWas; try { rb.useAutoMass = true; } catch { } }
            try
            {
                var pc = GameManager.Instance?.Player_Mine()?.ctrl as PlayerController;
                gameObject.layer = LayerMask.NameToLayer("Pets");
                if (ai != null) ai._enableAI = true;
                mob.lockAsTarget = true;
                var pet = GetComponent<MobPet>();
                if (pet != null) pet.SetCompanionType(MobPet.companionTypes.mercenary);
                if (pc != null) pc.AddPet(gameObject, temp: true, useAutoposition: false);
                try { GameManager.Instance.Recalculate_AI_Range(); } catch { }
            }
            catch (Exception e) { RescuePlugin.Log.LogWarning("Cultist Rescue: release: " + e.Message); }
            Talk.Say(transform, Unlock.Russian() ? "Свобода! Я с тобой!" : "Free! I'm with you!");
            RescuePlugin.Log.LogInfo("Cultist Rescue: the Cultist is out of his cage and fights for the hero");
        }

        void Update()
        {
            if (Caged || done || mob == null) return;
            if (!mob.alive)
            {
                done = true;
                RescuePlugin.Log.LogInfo("Cultist Rescue: the Cultist fell - no rescue this time");
                return;
            }
            if (Time.time < nextCheck) return;
            nextCheck = Time.time + 0.5f;
            if (!Cleared()) return;
            done = true;
            RescuePlugin.Log.LogInfo("Cultist Rescue: the sanctum is cleared - the Cultist speaks");
            Dialogue.Show(OnDialogueEnd);
        }

        void OnDialogueEnd()
        {
            Unlock.Open();
            StartCoroutine(Leave());
        }

        static Transform Exit()
        {
            try
            {
                var t = AccessTools.TypeByName("OccultSanctum.Gate");
                var g = t != null ? UnityEngine.Object.FindObjectOfType(t) as Component : null;
                return g != null ? g.transform : null;
            }
            catch { return null; }
        }

        static readonly int moveHash = Animator.StringToHash("Move");
        static PropertyInfo animProp;
        void MoveAnim(bool on)
        {
            try
            {
                if (animProp == null) animProp = AccessTools.Property(typeof(Controller), "anim");
                var an = mob != null && mob.ctrl != null && animProp != null ? animProp.GetValue(mob.ctrl) as Animator : null;
                if (an != null) an.SetBool(moveHash, on);
            }
            catch { }
        }

        /// (the user) after his words he runs off to the sanctum's way out and is gone there
        IEnumerator Leave()
        {
            // no longer the hero's companion: nobody leads him, his own mind is off, he only runs
            try
            {
                var pc = GameManager.Instance?.Player_Mine()?.ctrl;
                var pets = pc != null ? Traverse.Create(pc).Field("Pets").GetValue<List<GameObject>>() : null;
                pets?.Remove(gameObject);
            }
            catch { }
            if (ai != null) { ai.followTarget = null; ai._enableAI = false; }
            var exit = Exit();
            var path = ai != null ? ai._ai : null;
            yield return new WaitForSeconds(0.4f);
            if (exit != null)
            {
                Vector2 to = exit.position;
                float until = Time.time + 14f; Vector2 last = transform.position; float stuckSince = Time.time;
                if (rb != null) rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                while (Time.time < until && ((Vector2)transform.position - to).sqrMagnitude > 1.2f)
                {
                    if (path != null) { path.canMove = true; path.enableRotation = false; path.maxSpeed = 3.2f; path.destination = new Vector3(to.x, to.y, 0f); }
                    Vector2 v = path != null ? (Vector2)path.velocity : Vector2.zero;
                    if (v.sqrMagnitude < 0.05f) v = to - (Vector2)transform.position;
                    // no path found or stuck: straight on towards the way out
                    if (((Vector2)transform.position - last).sqrMagnitude > 0.01f) { last = transform.position; stuckSince = Time.time; }
                    else if (Time.time - stuckSince > 0.8f && rb != null) rb.velocity = (to - (Vector2)transform.position).normalized * 3f;
                    if (v.sqrMagnitude > 0.0001f)
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0f, 0f, Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg), 540f * Time.deltaTime);
                    MoveAnim(true);
                    yield return null;
                }
                if (path != null) path.canMove = false;
                MoveAnim(false);
                RescuePlugin.Log.LogInfo("Cultist Rescue: the Cultist reached the way out");
            }
            else { RescuePlugin.Log.LogWarning("Cultist Rescue: no way out found - he goes where he stands"); yield return new WaitForSeconds(1f); }
            var srs = GetComponentsInChildren<SpriteRenderer>(true);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.8f)
            {
                foreach (var sr in srs) if (sr != null) sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1f - t);
                yield return null;
            }
            gameObject.SetActive(false);                                                     // the hero's pet list drops him itself
            Destroy(gameObject, 0.5f);
        }

        /// every foe of the hero on the floor is dead (monsters still shut in cages do not count)
        static bool Cleared()
        {
            var gm = GameManager.Instance;
            var me = gm?.Player_Mine();
            if (gm?.mobsGo == null || me == null) return false;
            foreach (var m in gm.mobsGo)
            {
                if (m == null || !m.activeInHierarchy) continue;
                var cr = m.GetComponent<Creature>();
                if (cr != null && cr.alive && cr.faction != me.faction && !Rescue.IsCaged(cr) && cr.GetComponent<Captive>() == null) return false;
            }
            return true;
        }

        void OnDestroy() { if (Current == this) Current = null; }

        public static bool Protects(Creature c) => Current != null && Current.Caged && Current.mob == c;
    }

    /// nothing hurts him while he is in his cage
    [HarmonyPatch(typeof(Creature), nameof(Creature.SetDamageRPC))]
    internal static class CaptiveDamagePatch
    {
        [HarmonyPriority(Priority.High)]
        static bool Prefix(Creature __instance)
        {
            try { return !Captive.Protects(__instance); } catch { return true; }
        }
    }
}
