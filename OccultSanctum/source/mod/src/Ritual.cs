using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace OccultSanctum
{
    /// The cultists' ritual. In a sanctum with the great circle, once only five of them are left standing they break
    /// off the fight and WALK to the circle (by the game's own path-finding, round the furniture), each to his place
    /// on its ring; when they all stand there, a portal opens in its middle and winged devils come out of it one by
    /// one, every two seconds - two, plus one for every cultist in the ring - while the cultists stand chanting.
    /// Then they take up the fight again. If every one of them is cut down first, the ritual fails.
    public class Ritual : MonoBehaviour
    {
        public Vector2 Center;
        public readonly List<Mob> Cultists = new List<Mob>();
        enum St { Waiting, Gathering, Chanting, Summoning, Done }
        St st = St.Waiting;
        float t0, nextSpawn;
        int toSpawn;
        readonly Dictionary<Mob, Vector2> slot = new Dictionary<Mob, Vector2>();
        SpriteRenderer portal;
        const float Radius = 2.4f;            // the slots lie on the circle's outer ring (r 2.55 tiles)
        const float WalkSpeed = 3.2f;
        const float PortalSize = 0.5f;        // the portal (2.3 tiles drawn) shown at ~1.2 tiles: a little bigger than a devil
        const float SpawnEvery = 2f;

        public static bool Running;          // the ritual has begun: the cultists leave whatever they were doing
        static int moveHash = int.MinValue;
        static System.Reflection.PropertyInfo animProp;
        public static void MoveAnim(Creature c, bool on)
        {
            try
            {
                if (moveHash == int.MinValue) { var f = AccessTools.Field(typeof(AIController), "s_animMove"); moveHash = f != null ? (int)f.GetValue(null) : Animator.StringToHash("Move"); }
                if (animProp == null) animProp = AccessTools.Property(typeof(Controller), "anim");
                var an = c != null && c.ctrl != null && animProp != null ? animProp.GetValue(c.ctrl) as Animator : null;
                if (an != null) an.SetBool(moveHash, on);
            }
            catch { }
        }

        // the summoning pose: the game's own "WizHeal" animation (a wizard spreading his arms for a heal), held with
        // the arms wide open while the portal is open; its shot (WizEvent) is swallowed for the casters
        public static readonly HashSet<Controller> Casters = new HashSet<Controller>();
        static readonly int wizHeal = Animator.StringToHash("WizHeal");
        readonly Dictionary<Mob, float> castSince = new Dictionary<Mob, float>();
        readonly HashSet<Mob> held = new HashSet<Mob>();

        static Animator AnimOf(Mob m) { try { return m != null && m.ctrl != null ? m.ctrl.anim : null; } catch { return null; } }

        void Cast(Mob m)
        {
            var an = AnimOf(m);
            if (an == null || held.Contains(m)) return;
            if (m.ctrl != null) Casters.Add(m.ctrl);
            if (!castSince.TryGetValue(m, out float since) || Time.time - since > 2f)
            {
                an.speed = 1f;
                an.SetTrigger(wizHeal);                                                 // (again, if the first one did not take)
                castSince[m] = Time.time;
                return;
            }
            try
            {
                var ci = an.GetCurrentAnimatorClipInfo(0);
                bool inHeal = ci.Length > 0 && ci[0].clip != null && ci[0].clip.name.IndexOf("izHeal", StringComparison.OrdinalIgnoreCase) >= 0;
                if (inHeal && an.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.45f) { an.speed = 0f; held.Add(m); }   // arms wide open: hold it
            }
            catch { }
        }

        void Uncast(Mob m)
        {
            var an = AnimOf(m);
            if (an != null) an.speed = 1f;
            held.Remove(m); castSince.Remove(m);
            if (m != null && m.ctrl != null) Casters.Remove(m.ctrl);
        }

        List<Mob> Alive() => Cultists.Where(m => m != null && m.alive && m.gameObject.activeInHierarchy).ToList();

        void FixedUpdate()
        {
            try { Tick(); } catch (Exception e) { SanctumPlugin.Log.LogError("Sanctum ritual: " + e); Finish(); }
        }

        void Tick()
        {
            if (st == St.Done) { FadePortal(); return; }
            var alive = Alive();
            if (st == St.Waiting)
            {
                if (Cultists.Count > 5 && alive.Count <= 5 && alive.Count > 0) Begin(alive);
                return;
            }
            if (alive.Count == 0 && st != St.Summoning) { SanctumPlugin.Log.LogInfo("Sanctum ritual: every cultist is dead, it fails"); Finish(); return; }
            float dt = Time.fixedDeltaTime;
            bool all = true;
            foreach (var m in alive)
            {
                if (!slot.TryGetValue(m, out var to)) continue;
                var ai = m.GetComponent<AIController>();
                var path = ai != null ? ai._ai : null;
                var rb = m.GetComponent<Rigidbody2D>();
                Vector2 at = m.transform.position, d = to - at;
                bool arrived = d.magnitude < 0.3f;
                if (!arrived)                                                      // walking to his place (late ones keep walking)
                {
                    all = false;
                    if (path != null && ((Behaviour)(object)path).enabled)
                    {
                        // the game's own path-finding takes him round tables, cages and candles
                        path.canMove = true;
                        path.maxSpeed = WalkSpeed;
                        path.destination = new Vector3(to.x, to.y, 0f);
                        Vector2 v = path.velocity;
                        if (v.sqrMagnitude > 0.04f) m.transform.rotation = Quaternion.RotateTowards(m.transform.rotation, Quaternion.Euler(0f, 0f, Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg), 480f * Time.deltaTime);
                    }
                    else
                    {
                        Vector2 v = d.normalized * WalkSpeed;
                        if (rb != null) rb.velocity = v;
                        m.transform.rotation = Quaternion.RotateTowards(m.transform.rotation, Quaternion.Euler(0f, 0f, Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg), 480f * Time.deltaTime);
                    }
                    MoveAnim(m, true);
                }
                else
                {
                    Stop(m, path, rb);
                    Vector2 c = Center - at;                                               // face the circle's heart
                    m.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(c.y, c.x) * Mathf.Rad2Deg);
                    if (st != St.Gathering) Cast(m);                                       // standing in his place: arms spread, summoning
                }
            }
            foreach (var m in Cultists) if (m != null && !m.alive && castSince.ContainsKey(m)) Uncast(m);   // the fallen let go
            if (st == St.Gathering)
            {
                // the chant begins when every one of them has come to his place, or after 5 s at most (the late ones
                // walk on to their places while it goes on)
                if (all || Time.time - t0 > 5f) { st = St.Chanting; t0 = Time.time; OpenPortal(); }
            }
            else if (st == St.Chanting)
            {
                float k = Mathf.Clamp01((Time.time - t0) / 1.5f);
                if (portal != null) { portal.transform.localScale = Vector3.one * PortalSize * Mathf.SmoothStep(0.05f, 1f, k); portal.transform.Rotate(0f, 0f, -120f * dt); }
                if (k >= 1f) { st = St.Summoning; toSpawn = 2 + alive.Count; nextSpawn = Time.time; }
            }
            else if (st == St.Summoning)
            {
                if (portal != null) portal.transform.Rotate(0f, 0f, -160f * dt);
                if (Time.time >= nextSpawn && toSpawn > 0)
                {
                    nextSpawn = Time.time + SpawnEvery; toSpawn--;                       // one devil at a time
                    var dv = Devil.Spawn(Center + UnityEngine.Random.insideUnitCircle * 0.2f);
                    if (dv != null) try { CameraController.Instance.Shake(GameManager.Instance.Player_Mine().GetComponent<PlayerController>().playerID, 0.12f, 0.15f); } catch { }
                }
                if (toSpawn <= 0) Finish();
            }
        }

        static void Stop(Mob m, Pathfinding.AIPath path, Rigidbody2D rb)
        {
            if (path != null) { path.canMove = false; path.destination = Vector3.positiveInfinity; }
            if (rb != null) rb.velocity = Vector2.zero;
            MoveAnim(m, false);
        }

        void Begin(List<Mob> alive)
        {
            st = St.Gathering; t0 = Time.time; Running = true;
            for (int i = 0; i < alive.Count; i++)
            {
                var m = alive[i];
                var ai = m.GetComponent<AIController>();
                if (ai != null) ai._enableAI = false;                                   // they break off the fight
                float a = -Mathf.PI / 2f + i * 2f * Mathf.PI / alive.Count;
                slot[m] = Center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Radius;
            }
            SanctumPlugin.Log.LogInfo("Sanctum ritual: " + alive.Count + " cultists walk to the circle");
        }

        void OpenPortal()
        {
            portal = Builder.Sr("occ_portal_0", Center, Builder.floorLayer, Builder.floorOrder + 6, null, transform);
            portal.transform.localScale = Vector3.one * 0.05f * PortalSize;
            portal.gameObject.AddComponent<Flicker>().Init(new[] { "occ_portal_0", "occ_portal_1", "occ_portal_2", "occ_portal_3" }, 0.1f, 0.02f, null);
            SanctumPlugin.Log.LogInfo("Sanctum ritual: they all stand round the circle, the portal opens");
        }

        void Finish()
        {
            foreach (var m in Cultists) if (m != null) Uncast(m);
            foreach (var m in Alive())
            {
                var ai = m.GetComponent<AIController>();
                if (ai != null)
                {
                    if (ai._ai != null) ai._ai.canMove = true;
                    ai._enableAI = true;                                                 // back to the fight
                }
                MoveAnim(m, false);
            }
            st = St.Done; t0 = Time.time; Running = false;
            SanctumPlugin.Log.LogInfo("Sanctum ritual: done");
        }

        void FadePortal()
        {
            if (portal == null) return;
            float k = 1f - (Time.time - t0) / 2.5f;
            if (k <= 0f) { Destroy(portal.gameObject); portal = null; return; }
            portal.transform.localScale = Vector3.one * k * PortalSize;
            portal.transform.Rotate(0f, 0f, -160f * Time.fixedDeltaTime);
        }

        void OnDestroy() { foreach (var m in Cultists) if (m != null && m.ctrl != null) Casters.Remove(m.ctrl); }
    }

    /// a cultist spreading his arms for the summoning does not shoot (the heal animation's event would fire a sphere)
    [HarmonyPatch(typeof(Controller), nameof(Controller.WizEvent))]
    internal static class RitualNoShotPatch
    {
        static bool Prefix(Controller __instance) => !Ritual.Casters.Contains(__instance);
    }
}
