using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OccultSanctum
{
    /// What a monster of the sanctum is busy with while it has not yet noticed the hero - so there is always something
    /// to watch: a cultist reads at a lectern or prays with his arms spread, or goes from one thing to another and
    /// studies it (touches it, passes his hands over it, takes notes); a crossbowman shoots bolts into a target from a
    /// few paces off; spearmen thrust at dummies or go the rounds of the store rooms in twos, threes and fours; they sit
    /// on the benches, get up, stroll about and sit down again. The moment the monster has a target (it saw the hero,
    /// or was hit) it drops all that and fights as usual.
    public class Activity : MonoBehaviour
    {
        public enum Kind { Read, Pray, Shoot, Patrol, Guard, Train, Sit, Wander, Escort, Execute, Shuffle, Follow, Inspect, Study, Preach }
        public Kind What;
        public Vector2 Look;                 // what he faces (the lectern, the statue, the target, the door); Wander: the room's middle
        public Vector2[] Route;              // the points he walks between (Inspect / Study: the things he looks at)
        public bool Stay;                    // a soldier who keeps his post (not taken into a patrol)
        public float Radius = 2.5f;          // Wander: how far from the middle
        public Container Cage;
        public Container[] Cages;            // Execute: the cages he takes his prisoners from
        public Transform Leader;             // Follow: the one he walks his round with
        public int Slot = 1;                 // Follow: his place behind the leader
        public bool StartStanding;           // Sit: on his feet at first, sits down later
        public readonly List<Activity> Mates = new List<Activity>();   // Patrol: those walking with him
        public float Heading;                // the way he walks (his mates keep their places by it)

        AIController ai; Mob mob; Animator an; Rigidbody2D rb; RigidbodyType2D bodyWas;
        SpriteRenderer book;
        float next, hp0, waitTill, offs, walkSince, restAng, glance, glanceAt, lagSince = -1f; int leg; bool held, posed;
        Vector2? facePt; Vector2 lastTo = new Vector2(9999f, 9999f);
        static readonly int wizHeal = Animator.StringToHash("WizHeal");
        const int ObstacleMask = 35651840;   // the game's own mask for "something in the way"

        public static Activity Give(Mob m, Kind k, Vector2 look, params Vector2[] route)
        {
            if (m == null) return null;
            var a = m.gameObject.AddComponent<Activity>();
            a.What = k; a.Look = look; a.Route = route;
            return a;
        }

        void Start()
        {
            ai = GetComponent<AIController>(); mob = GetComponent<Mob>(); rb = GetComponent<Rigidbody2D>();
            if (rb != null) bodyWas = rb.bodyType;
            try { an = mob != null && mob.ctrl != null ? mob.ctrl.anim : null; } catch { }
            hp0 = mob != null ? mob.HP_cur : 0f;
            next = Time.time + UnityEngine.Random.Range(0.5f, 2.5f);
            Heading = transform.eulerAngles.z;
            home = transform.position;
            // nobody moves the prisoners about any more: he looks at them instead
            if (What == Kind.Shuffle || What == Kind.Escort)
            {
                Route = Cages?.Where(c => c != null).Select(c => (Vector2)c.transform.position).ToArray();
                What = Kind.Inspect;
            }
            // a cultist does not just stand: he studies the things about him
            bool cult = GetComponent<CultistTag>() != null;
            if (cult && (What == Kind.Inspect || What == Kind.Guard))
            {
                if (What == Kind.Guard || Route == null || Route.Length == 0) Route = Sights(transform.position, 8f);
                What = Kind.Study;
            }
            if ((What == Kind.Study || What == Kind.Inspect) && (Route == null || Route.Length == 0)) { What = Kind.Wander; Look = transform.position; }
            if (What == Kind.Pray && mob != null && mob.ctrl != null) Ritual.Casters.Add(mob.ctrl);
            if (What == Kind.Sit) SitStart();
            if (What == Kind.Shoot) shotSpot = ShotSpot();
            if (What == Kind.Follow && Leader != null)
            {
                var la = Leader.GetComponent<Activity>();
                var others = new List<Component> { Leader };
                if (la != null) others.AddRange(la.Mates.Where(m => m != null && m != this).Cast<Component>());
                foreach (var o in others)
                    foreach (var oc in o.GetComponentsInChildren<Collider2D>(true))
                        foreach (var mc in GetComponentsInChildren<Collider2D>(true))
                            if (oc != null && mc != null && !oc.isTrigger && !mc.isTrigger) Physics2D.IgnoreCollision(mc, oc, true);
            }
            // he turns himself (smoothly), neither the path-finder nor a bump spins him
            try { if (ai != null && ai._ai != null) ai._ai.enableRotation = false; if (rb != null) rb.constraints = RigidbodyConstraints2D.FreezeRotation; } catch { }
        }

        /// the things worth a look near a point: cages, lecterns, statues, the altar, shelves, tables, racks...
        static readonly string[] SightNames = { "cage", "lectern", "statue", "altar", "sacrifice", "shelf", "obelisk", "table_", "rack", "skullpile", "brazier_red", "dummy" };
        public static Vector2[] Sights(Vector2 at, float r)
        {
            return FindObjectsOfType<Container>()
                .Where(c => c != null && c.hp > 0f && c.name.StartsWith("Sanctum_occ_", StringComparison.Ordinal) && SightNames.Any(n => c.name.Substring(12).StartsWith(n, StringComparison.Ordinal))
                            && ((Vector2)c.transform.position - at).sqrMagnitude < r * r)
                .OrderBy(_ => UnityEngine.Random.value).Take(5).Select(c => (Vector2)c.transform.position).ToArray();
        }

        Prisoner.Led Led;
        Vector2 Look2;

        // ---------------------------------------------------------------- the sermon
        Vector2 home; bool listening, returning;

        /// he listens to the elder: walks to his place before him and looks at him (a sitter just turns to him);
        /// true while that goes on
        bool Listen()
        {
            if (What == Kind.Preach || !Sermon.Hears(this, out var spot, out bool seated))
            {
                if (listening)
                {
                    // the sermon is over: back to his business (to his own place first)
                    listening = false; step = 0; offs = 0f; facePt = null;
                    if (What == Kind.Shoot) atSpot = false;
                    else if (What == Kind.Read || What == Kind.Pray || What == Kind.Guard || What == Kind.Train) returning = true;
                }
                return false;
            }
            if (!listening)
            {
                listening = true;
                if (an != null) an.speed = 1f; held = false; posed = false;
                if (What != Kind.Pray && mob != null && mob.ctrl != null) Ritual.Casters.Remove(mob.ctrl);
                if (What == Kind.Study && step == 2) SetPos(basePos);
            }
            Vector2 sp = Sermon.Speaker != null ? (Vector2)Sermon.Speaker.transform.position : Sermon.At;
            if (seated)
            {
                if (What == Kind.Sit && sitSt == 2) { Halt(); SetPos(SeatPos(bench, seat)); Turn(Ang(sp - (Vector2)transform.position), 140f); return true; }
                return false;                                                               // on his feet: he goes on with his strolling
            }
            if (Walk(spot, 1.25f)) { Halt(); offs = 0f; Face(sp); }
            return true;
        }

        // the elder on the pentagram: he waits there, looking over the hall, and now and then preaches - turning from one
        // side of his flock to the other, raising his staff, spreading his arms
        bool onSpot; float preachAt, sermonEnd, gestureAt, gestureT, swing; int gesture;
        Transform armL, armR; Quaternion armL0, armR0; bool armsKnown;

        /// his mates lag behind (or are still on their way to him): he waits for them a while, looking about
        bool MatesLag()
        {
            // he stops when one of them is far behind and goes on only when they are all close again (not at the edge
            // of one distance, which made him stop and go every moment)
            float worst = 0f;
            foreach (var m in Mates) if (m != null && m.What == Kind.Follow) worst = Mathf.Max(worst, ((Vector2)m.transform.position - (Vector2)transform.position).sqrMagnitude);
            if (lagSince < 0f && worst > 4.5f * 4.5f) { lagSince = Time.time; restAng = transform.eulerAngles.z; }
            if (lagSince >= 0f)
            {
                if (worst < 2.4f * 2.4f || Time.time - lagSince > 25f) { lagSince = -1f; return false; }
                Halt(); LookAbout(); return true;
            }
            return false;
        }

        // the elder's rounds after a sermon: with his retinue he looks over the stores and goes to see the others at work
        // (a reader, one at prayer, one studying, the copyists, the executioner), then back to the pentagram
        bool touring; readonly List<(Vector2 go, Vector2 look)> tour = new List<(Vector2, Vector2)>(); int tourLeg;
        static readonly string[] Stores = { "warehouse", "store", "pantry", "research", "prison", "cell", "scriptorium", "alchemy", "armory", "summoning", "library" };

        bool StartTour()
        {
            tour.Clear();
            foreach (var x in SideRoom.Rooms.Where(x => Stores.Contains(x.role)).OrderBy(_ => UnityEngine.Random.value).Take(UnityEngine.Random.Range(1, 3)))
            {
                Vector2 c = Room.W(x.r.xMin + x.r.width / 2f - .5f, x.r.yMin + x.r.height / 2f - .5f);
                tour.Add((c, c + UnityEngine.Random.insideUnitCircle.normalized * 2f));
            }
            foreach (var a in FindObjectsOfType<Activity>().Where(a => a != null && a != this && a.GetComponent<CultistTag>() != null
                         && (a.What == Kind.Read || a.What == Kind.Pray || a.What == Kind.Study || a.What == Kind.Sit || a.What == Kind.Execute))
                         .OrderBy(_ => UnityEngine.Random.value).Take(3))
            {
                Vector2 p = a.transform.position, d = (Vector2)transform.position - p;
                tour.Add((p + (d.sqrMagnitude > 0.01f ? d.normalized : Vector2.down) * 1.8f, p));
            }
            foreach (var c in FindObjectsOfType<Container>().Where(c => c != null && c.hp > 0f && c.name == "Sanctum_occ_cage").OrderBy(_ => UnityEngine.Random.value).Take(1))
            {
                Vector2 p = c.transform.position, d = (Vector2)transform.position - p;
                tour.Add((p + (d.sqrMagnitude > 0.01f ? d.normalized : Vector2.down) * 1.9f, p));
            }
            if (tour.Count == 0) return false;
            // nearest first, then on from one to the next
            var left = new List<(Vector2 go, Vector2 look)>(tour); tour.Clear(); Vector2 at = transform.position;
            while (left.Count > 0) { var nx = left.OrderBy(q => (q.go - at).sqrMagnitude).First(); tour.Add(nx); left.Remove(nx); at = nx.go; }
            touring = true; tourLeg = 0;
            SanctumPlugin.Log.LogInfo("Sanctum: the elder goes his rounds (" + tour.Count + " stops)");
            return true;
        }

        void Tour()
        {
            if (Waiting()) return;
            if (MatesLag()) return;
            if (tourLeg >= tour.Count) { touring = false; Pause(3f, 6f); return; }       // done: a short rest, then the next round
            var (go, look) = tour[tourLeg];
            if (Walk(go, 1.1f)) { Face(look); Pause(4f, 7f); tourLeg++; }
        }

        Vector2 FaceDir { get { Vector2 d = Look - Route[0]; return d.sqrMagnitude > 0.01f ? d.normalized : Vector2.down; } }

        void Arms(float kl, float kr)
        {
            if (!armsKnown)
            {
                armsKnown = true;
                foreach (var t in GetComponentsInChildren<Transform>(true)) { if (t.name == "UpperArm_L") armL = t; else if (t.name == "UpperArm_R") armR = t; }
                if (armL != null) armL0 = armL.localRotation;
                if (armR != null) armR0 = armR.localRotation;
            }
            if (armL != null) armL.localRotation = armL0 * Quaternion.Euler(0f, 0f, 40f * kl);      // the staff raised
            if (armR != null) armR.localRotation = armR0 * Quaternion.Euler(0f, 0f, -45f * kr);     // the other hand thrown open
        }

        void Preach()
        {
            if (touring) { Tour(); return; }
            if (Waiting()) return;                                                          // a while where he stopped, looking about
            if (!StartTour())                                                               // nothing to see: a stroll near where he is
            {
                Vector2 p = (Route != null && Route.Length > 0 ? Route[0] : (Vector2)transform.position) + UnityEngine.Random.insideUnitCircle * 3f;
                tour.Clear(); tour.Add((p, p + UnityEngine.Random.insideUnitCircle.normalized * 2f)); touring = true; tourLeg = 0;
            }
        }

        static float Ang(Vector2 d) => Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;

        void Face(Vector2 p)
        {
            facePt = p;
            Vector2 d = p - (Vector2)transform.position;
            if (d.sqrMagnitude > 0.01f) Turn(Ang(d) + offs);
        }

        /// turns towards an angle at a steady pace (no snapping)
        void Turn(float deg, float rate = 420f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0f, 0f, deg), rate * Time.deltaTime);
        }

        void SetPos(Vector2 p)
        {
            if (rb != null) { rb.velocity = Vector2.zero; rb.position = p; }
            transform.position = new Vector3(p.x, p.y, transform.position.z);
        }

        void Halt() { Moving = false; if (ai != null && ai._ai != null) ai._ai.canMove = false; if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic) rb.velocity = Vector2.zero; Ritual.MoveAnim(mob, false); }
        /// walking now (his mates keep pace with him), and how fast
        public bool Moving; public float Pace = 1.3f; bool followStill;

        void LateUpdate()
        {
            try { Tick(); } catch (Exception e) { SanctumPlugin.Log.LogWarning("Sanctum activity: " + e.Message); Stop(); }
        }

        /// walks towards a point by the game's path-finding; true once there (or when he cannot get there at all)
        bool Walk(Vector2 to, float speed = 1.3f)
        {
            var path = ai._ai;
            if ((to - lastTo).sqrMagnitude > 0.05f) { lastTo = to; walkSince = Time.time; }
            facePt = null;
            bool there = ((Vector2)transform.position - to).sqrMagnitude < 0.15f
                         || path != null && path.canMove && Time.time - walkSince > 0.6f && !path.pathPending && path.reachedEndOfPath && ((Vector2)transform.position - to).sqrMagnitude < 2.5f;
            if (there || Time.time - walkSince > 10f) { Halt(); walkSince = Time.time; return true; }
            if (path != null) { path.canMove = true; path.enableRotation = false; path.maxSpeed = speed; path.destination = new Vector3(to.x, to.y, 0f); }
            Moving = true; Pace = speed;
            Vector2 v = path != null ? (Vector2)path.velocity : Vector2.zero;
            if (v.sqrMagnitude < 0.04f) v = to - (Vector2)transform.position;               // slow at a corner: towards where he goes
            offs = 0f;
            if (v.sqrMagnitude > 0.0001f) { Heading = Ang(v); Turn(Heading); }
            Ritual.MoveAnim(mob, true);                                                     // walking until he is there: no flicker
            return false;
        }

        /// waiting: stands still and looks about him now and then (look = false: just stands)
        bool Waiting(bool look = true)
        {
            if (Time.time >= waitTill) return false;
            Halt();
            if (look) LookAbout();
            return true;
        }

        void LookAbout()
        {
            if (Time.time > glanceAt)
            {
                glanceAt = Time.time + UnityEngine.Random.Range(1.2f, 2.8f);
                glance = UnityEngine.Random.value < 0.35f ? 0f : UnityEngine.Random.Range(-45f, 45f);
            }
            Turn(restAng + glance, 100f);
        }

        void Pause(float a, float b)
        {
            waitTill = Time.time + UnityEngine.Random.Range(a, b);
            restAng = facePt.HasValue ? Ang(facePt.Value - (Vector2)transform.position) + offs : transform.eulerAngles.z;
            glance = 0f; glanceAt = Time.time + UnityEngine.Random.Range(0.8f, 2f);
        }

        /// something in the way between two points (the given container itself does not count)
        static bool Blocked(Vector2 a, Vector2 b, Container self)
        {
            foreach (var h in Physics2D.RaycastAll(a, b - a, Vector2.Distance(a, b), ObstacleMask))
            {
                if (h.collider == null || h.collider.isTrigger) continue;
                if (self != null && h.collider.GetComponentInParent<Container>() == self) continue;
                return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- the crossbowman at his target
        Vector2 shotSpot; bool atSpot;

        /// a spot some paces off the target with a clear line to it: he shoots from there, not from right beside it
        Vector2 ShotSpot()
        {
            Vector2 t = Look, me = transform.position, dir = (me - t).sqrMagnitude > 0.01f ? (me - t).normalized : Vector2.down;
            Container tc = null;
            foreach (var c in Physics2D.OverlapCircleAll(t, 0.4f)) { var k = c != null ? c.GetComponentInParent<Container>() : null; if (k != null) { tc = k; break; } }
            foreach (float d in new[] { 5f, 4.5f, 4f, 3.5f, 3f })
                foreach (float a in new[] { 0f, 20f, -20f, 40f, -40f, 60f, -60f })
                {
                    Vector2 v = Quaternion.Euler(0f, 0f, a) * dir, p = t + v * d;
                    if (Blocked(t + v * 0.8f, p, tc)) continue;
                    if (Physics2D.OverlapCircleAll(p, 0.35f, ObstacleMask).Any(c => c != null && !c.isTrigger)) continue;
                    return p;
                }
            return me;
        }

        // ---------------------------------------------------------------- the cultist studying things
        int sub; float subAt; Vector2 basePos;

        void Study()
        {
            if (Waiting()) return;
            if (Route == null || Route.Length == 0) { What = Kind.Wander; Look = transform.position; return; }
            var pt = Route[leg % Route.Length];
            if (step == 0)
            {
                // a place by the thing, on his side of it (a little to one side or the other: several of them stand round it)
                Vector2 d = (Vector2)transform.position - pt;
                float a = (d.sqrMagnitude > 0.01f ? Mathf.Atan2(d.y, d.x) : -Mathf.PI / 2f) + UnityEngine.Random.Range(-0.8f, 0.8f);
                Look2 = pt + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * UnityEngine.Random.Range(1.0f, 1.3f);
                step = 1;
            }
            if (step == 1)
            {
                if (Walk(Look2, 1.0f)) { step = 2; sub = UnityEngine.Random.Range(0, 3); subAt = Time.time; basePos = transform.position; }
                return;
            }
            Halt();
            Face(pt);
            float t = Time.time - subAt; bool done = false;
            switch (sub)
            {
                case 0:                                                                     // he looks it over from one side to the other (he stands still: no sliding to and fro)
                    offs = Mathf.Sin(t * 1.6f) * 24f;
                    done = t > 3.6f;
                    if (done) offs = 0f;
                    break;
                case 1:                                                                     // passes his hands over it (the spell gesture)
                    if (!posed && an != null) { an.speed = 1f; an.SetTrigger(wizHeal); if (mob.ctrl != null) Ritual.Casters.Add(mob.ctrl); posed = true; }
                    done = t > 2.4f;
                    if (done) { posed = false; if (mob.ctrl != null) Ritual.Casters.Remove(mob.ctrl); }
                    break;
                default:                                                                    // bends over it and looks close, now this side, now that
                    // he studies it closely, now this side, now that (standing where he is)
                    offs = ((int)(t / 1.6f)) % 2 == 0 ? -12f : 12f;
                    done = t > 5.2f;
                    if (done) offs = 0f;
                    break;
            }
            if (done) { step = 0; leg += 1 + (Route.Length > 2 ? UnityEngine.Random.Range(0, 2) : 0); Pause(1.5f, 3.5f); }
        }

        // ---------------------------------------------------------------- sitting on the benches
        static readonly Dictionary<Container, Activity[]> taken = new Dictionary<Container, Activity[]>();
        Container bench; int seat = -1, sitSt = 4, strolls; float sitUntil, slideAt; Vector2 slideFrom, approach, strollTo;

        static Container[] Benches(Vector2 at, float r) => FindObjectsOfType<Container>()
            .Where(c => c != null && c.hp > 0f && c.gameObject.activeInHierarchy && c.name.StartsWith("Sanctum_occ_bench", StringComparison.Ordinal) && ((Vector2)c.transform.position - at).sqrMagnitude < r * r).ToArray();

        static Vector2 SeatPos(Container b, int i) => (Vector2)b.transform.position + (Vector2)b.transform.right * (i == 0 ? -0.45f : 0.45f);

        /// which way one sits on it: towards the table beside it, else towards the room
        Vector2 SeatFace(Container b)
        {
            Vector2 up = b.transform.up, p = b.transform.position, to = Look - p;
            foreach (var c in Physics2D.OverlapCircleAll(p, 1.6f))
            {
                var k = c != null ? c.GetComponentInParent<Container>() : null;
                if (k != null && k.name.StartsWith("Sanctum_occ_table", StringComparison.Ordinal)) { to = (Vector2)k.transform.position - p; break; }
            }
            return Vector2.Dot(up, to) >= 0f ? up : -up;
        }

        /// where he steps off to: behind the bench, else at its end, else in front
        Vector2 Approach(Container b, int i)
        {
            Vector2 s = SeatPos(b, i), f = SeatFace(b), side = (Vector2)b.transform.right * (i == 0 ? -1f : 1f);
            foreach (var p in new[] { s - f * 0.85f, s + side * 1.25f, s + f * 0.85f })
                if (!Blocked(s, p, b) && !Physics2D.OverlapCircleAll(p, 0.25f, ObstacleMask).Any(c => c != null && !c.isTrigger && c.GetComponentInParent<Container>() != b)) return p;
            return s - f * 0.85f;
        }

        bool Reserve(Container b, int i)
        {
            if (b == null) return false;
            if (!taken.TryGetValue(b, out var arr)) taken[b] = arr = new Activity[2];
            if (arr[i] != null && arr[i] != this) return false;
            arr[i] = this; bench = b; seat = i;
            return true;
        }

        void FreeSeat()
        {
            if (bench != null && seat >= 0 && taken.TryGetValue(bench, out var arr) && arr[seat] == this) arr[seat] = null;
            bench = null; seat = -1;
        }

        void Body(bool sitting)
        {
            if (rb == null) return;
            rb.velocity = Vector2.zero;
            rb.bodyType = sitting ? RigidbodyType2D.Kinematic : bodyWas;
        }

        void SitStart()
        {
            // the benches neither push him off nor get pushed about
            foreach (var b in Benches(transform.position, 12f))
            {
                foreach (var brb in b.GetComponentsInChildren<Rigidbody2D>(true)) { brb.velocity = Vector2.zero; brb.bodyType = RigidbodyType2D.Static; }
                foreach (var bc in b.GetComponentsInChildren<Collider2D>(true))
                    foreach (var mc in GetComponentsInChildren<Collider2D>(true))
                        if (bc != null && mc != null) Physics2D.IgnoreCollision(mc, bc, true);
            }
            if (!StartStanding)
            {
                Vector2 me = transform.position;
                foreach (var b in Benches(me, 1.6f).OrderBy(b => ((Vector2)b.transform.position - me).sqrMagnitude))
                {
                    foreach (int i in new[] { 0, 1 }.OrderBy(i => (SeatPos(b, i) - me).sqrMagnitude))
                        if (Reserve(b, i)) break;
                    if (bench != null) break;
                }
                if (bench != null)
                {
                    Body(true); SetPos(SeatPos(bench, seat));
                    Vector2 f = SeatFace(bench);
                    transform.rotation = Quaternion.Euler(0f, 0f, Ang(f));
                    restAng = Ang(f); sitSt = 2; sitUntil = Time.time + UnityEngine.Random.Range(8f, 25f);
                    return;
                }
            }
            OnFeet();
        }

        void OnFeet()
        {
            sitSt = 4; strolls = UnityEngine.Random.Range(1, 3);
            strollTo = StrollPoint();
            Pause(1.5f, 3f);
        }

        Vector2 StrollPoint()
        {
            Vector2 me = transform.position;
            for (int i = 0; i < 6; i++)
            {
                Vector2 p = me + UnityEngine.Random.insideUnitCircle * 2.2f;
                if (!Blocked(me, p, null) && !Physics2D.OverlapCircleAll(p, 0.3f, ObstacleMask).Any(c => c != null && !c.isTrigger)) return p;
            }
            return me;
        }

        void Sit()
        {
            bool benchOk = bench != null && bench.hp > 0f && bench.gameObject.activeInHierarchy;
            switch (sitSt)
            {
                case 0:                                                                     // to the seat
                    if (!benchOk) { FreeSeat(); OnFeet(); return; }
                    if (Walk(approach, 1.0f)) { sitSt = 1; slideAt = Time.time; slideFrom = transform.position; Body(true); }
                    return;
                case 1:                                                                     // he sits down
                {
                    if (!benchOk) { Body(false); FreeSeat(); OnFeet(); return; }
                    float k = Mathf.Clamp01((Time.time - slideAt) / 0.45f);
                    Halt();
                    SetPos(Vector2.Lerp(slideFrom, SeatPos(bench, seat), k));
                    Turn(Ang(SeatFace(bench)), 360f);
                    if (k >= 1f) { sitSt = 2; restAng = Ang(SeatFace(bench)); glance = 0f; sitUntil = Time.time + UnityEngine.Random.Range(10f, 25f); }
                    return;
                }
                case 2:                                                                     // sitting, looking about now and then
                    if (!benchOk) { Body(false); FreeSeat(); OnFeet(); return; }
                    Halt();
                    SetPos(SeatPos(bench, seat));
                    LookAbout();
                    if (Time.time > sitUntil)
                    {
                        if (UnityEngine.Random.value < 0.6f) { sitSt = 3; slideAt = Time.time; slideFrom = transform.position; approach = Approach(bench, seat); }
                        else sitUntil = Time.time + UnityEngine.Random.Range(6f, 14f);
                    }
                    return;
                case 3:                                                                     // he gets up
                {
                    float k = Mathf.Clamp01((Time.time - slideAt) / 0.45f);
                    Halt();
                    SetPos(Vector2.Lerp(slideFrom, approach, k));
                    if (k >= 1f) { Body(false); FreeSeat(); OnFeet(); }
                    return;
                }
                default:                                                                    // on his feet: a stroll or two, then a free seat
                    if (Waiting()) return;
                    if (strolls > 0) { if (Walk(strollTo, 1.0f)) { strolls--; strollTo = StrollPoint(); Pause(2f, 5f); } return; }
                    foreach (var b in Benches(transform.position, 8f).OrderBy(_ => UnityEngine.Random.value))
                    {
                        foreach (int i in new[] { 0, 1 }) if (Reserve(b, i)) break;
                        if (bench != null) break;
                    }
                    if (bench != null) { approach = Approach(bench, seat); sitSt = 0; }
                    else { strolls = 1; strollTo = StrollPoint(); Pause(3f, 6f); }
                    return;
            }
        }

        // ---------------------------------------------------------------- the executioner
        Container from; int step; float stepAt;

        /// where to stand before a cage: on the side towards the room (Look is the room's middle)
        Vector2 Front(Container c)
        {
            Vector2 p = c.transform.position, d = Look - p;
            return p + (d.sqrMagnitude > 0.01f ? d.normalized : Vector2.down) * 1.25f;
        }

        static bool Whole(Container c) => c != null && c.hp > 0f && c.gameObject.activeInHierarchy;

        /// he takes a prisoner out of a cage, walks him to the block (Route[0]), strikes him down, waits, and fetches
        /// the next - until the cages are empty; then he studies them
        void Execute()
        {
            if (Waiting(false)) { if (Led != null && Led.Ok) Led.Before(transform); return; }
            if (step == 0)
            {
                from = Cages?.Where(Whole).FirstOrDefault(c => Prisoner.In(c) != null);
                if (from == null) { What = Kind.Inspect; step = 0; leg = 0; Route = Cages?.Where(Whole).Select(c => (Vector2)c.transform.position).ToArray(); if (Route == null || Route.Length == 0) { What = Kind.Wander; Look = transform.position; } return; }
                step = 1;
            }
            if (step == 1) { if (!Whole(from)) { step = 0; return; } if (Walk(Front(from), 1.1f)) { Face(from.transform.position); var p = Prisoner.In(from); if (p == null) { step = 0; return; } Led = p.TakeOut(); step = 2; Pause(0.8f, 1.2f); } return; }
            if (step == 2) { if (Led == null || !Led.Ok) { step = 0; return; } if (Walk(Route[0], 1.0f)) { if (Route.Length > 1) Face(Route[1]); step = 3; Pause(0.6f, 1f); Led.Before(transform); } else Led.Follow(transform); return; }
            if (step == 3)                                                                  // the blow
            {
                if (Route.Length > 1) Face(Route[1]);
                Led.Before(transform);
                try { if (GetComponent<SpearmanTag>() != null) mob.ctrl.Attack(27); else { if (an != null) { an.speed = 1f; an.SetTrigger(wizHeal); } if (mob.ctrl != null) Ritual.Casters.Add(mob.ctrl); } } catch { }
                step = 4; stepAt = Time.time + 0.55f; return;
            }
            if (step == 4 && Time.time >= stepAt)
            {
                Led.Kill(); Led = null;
                if (mob.ctrl != null && GetComponent<CultistTag>() != null) Ritual.Casters.Remove(mob.ctrl);
                step = 0; Pause(5f, 9f);
            }
        }

        // ----------------------------------------------------------------
        void Tick()
        {
            if (ai == null || mob == null || !mob.alive) { Stop(); return; }
            if (Patches.Target(ai) != null || mob.HP_cur < hp0 - 0.01f || Ritual.Running && GetComponent<CultistTag>() != null) { Stop(); return; }   // he has seen the hero / was hit / the ritual calls
            if (Listen()) return;                                                           // the elder preaches: he listens
            if (returning) { if (Walk(home, 1.2f)) { returning = false; } return; }
            switch (What)
            {
                case Kind.Preach: Preach(); return;
                case Kind.Patrol:
                {
                    if (Route == null || Route.Length == 0 || Waiting()) return;
                    if (MatesLag()) return;
                    if (Walk(Route[leg % Route.Length])) { leg++; Pause(0.7f, 2f); }                // a short look round, and on
                    return;
                }
                case Kind.Wander:
                    if (Waiting()) return;
                    if (Route == null || Route.Length == 0) Route = new[] { Look + UnityEngine.Random.insideUnitCircle * Radius };
                    if (Walk(Route[0], 1.0f)) { Route = null; Pause(1.5f, 4.5f); }
                    return;
                case Kind.Sit: Sit(); return;
                case Kind.Study: Study(); return;
                case Kind.Execute: Execute(); return;
                case Kind.Follow:
                {
                    var la = Leader != null ? Leader.GetComponent<Activity>() : null;
                    if (Leader == null || la == null) { What = Kind.Wander; Look = transform.position; Route = null; return; }   // his mate fights or fell: he looks about
                    // his place in the file, by the way the leader walks (not by where he glances)
                    Vector2 off = Slot == 1 ? new Vector2(-0.9f, 0.7f) : Slot == 2 ? new Vector2(-0.9f, -0.7f) : Slot == 3 ? new Vector2(-1.8f, 0f) : new Vector2(-1.8f, Slot % 2 == 0 ? 0.8f : -0.8f);
                    Quaternion q = Quaternion.Euler(0f, 0f, la.Heading);
                    Vector2 to = (Vector2)Leader.position + (Vector2)(q * off);
                    float far = ((Vector2)transform.position - to).sqrMagnitude;
                    if (la.Moving)
                    {
                        // the leader walks: he keeps walking with him - at the leader's pace once in his place, faster to catch up
                        var path = ai._ai;
                        float sp = far > 16f ? 1.8f : far > 0.6f ? 1.5f : la.Pace;
                        if (path != null) { path.canMove = true; path.enableRotation = false; path.maxSpeed = sp; path.destination = new Vector3(to.x, to.y, 0f); }
                        Vector2 v = path != null ? (Vector2)path.velocity : Vector2.zero;
                        Turn(v.sqrMagnitude > 0.04f ? Ang(v) : la.Heading);
                        Ritual.MoveAnim(mob, true); Moving = true; followStill = false;
                        return;
                    }
                    // the leader stands: he comes to his place and stands there (and stays, unless pushed well off it)
                    if (followStill && far < 1.0f) { Halt(); Turn(la.Heading, 200f); return; }
                    followStill = false;
                    if (Walk(to, far > 16f ? 1.8f : 1.3f)) { followStill = true; Turn(la.Heading, 200f); }
                    return;
                }
                case Kind.Inspect:
                {
                    // he goes from one thing to another - a cage, a reader, a statue - stops and looks at it a while
                    if (Route == null || Route.Length == 0) { What = Kind.Wander; Look = transform.position; return; }
                    if (Waiting()) return;
                    var pt = Route[leg % Route.Length];
                    if (step == 0) { Vector2 d = (Vector2)transform.position - pt; Look2 = pt + (d.sqrMagnitude > 0.01f ? d.normalized : Vector2.down) * 1.3f; step = 1; }
                    if (Walk(Look2, 1.1f)) { Face(pt); step = 0; leg += 1 + UnityEngine.Random.Range(0, Route.Length > 2 ? 2 : 1); Pause(3f, 6f); }
                    return;
                }
                case Kind.Shoot:
                    if (!atSpot) { if (Walk(shotSpot, 1.1f)) atSpot = true; return; }        // a few paces off the target first
                    break;
            }
            Halt();
            Face(Look);
            if (What == Kind.Read && Time.time > next)
            {
                next = Time.time + UnityEngine.Random.Range(2.5f, 5f);                  // he turns a page, glances up
                offs = UnityEngine.Random.Range(-14f, 14f);
            }
            else if (What == Kind.Pray && an != null)
            {
                if (!posed) { an.speed = 1f; an.SetTrigger(wizHeal); posed = true; next = Time.time + 2f; }
                else if (!held)
                {
                    var ci = an.GetCurrentAnimatorClipInfo(0);
                    bool inHeal = ci.Length > 0 && ci[0].clip != null && ci[0].clip.name.IndexOf("izHeal", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (inHeal && an.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.45f) { an.speed = 0f; held = true; }
                    else if (Time.time > next) posed = false;                           // the trigger did not take: again
                }
            }
            else if (What == Kind.Train && Time.time > next)
            {
                next = Time.time + UnityEngine.Random.Range(2f, 3.5f);                  // a thrust at the dummy
                mob.ctrl.Attack(27);
            }
            else if (What == Kind.Shoot && Time.time > next)
            {
                next = Time.time + UnityEngine.Random.Range(3f, 5.5f);                  // a bolt into the target now and then
                try { SFXController.Instance.PlaySFX(mob.BowSound, transform.position); } catch { }
                mob.ctrl.Attack(9);
            }
            else if (What == Kind.Guard && Time.time > next)
            {
                next = Time.time + UnityEngine.Random.Range(2.5f, 5f);                  // the guard looks this way and that
                offs = UnityEngine.Random.Range(-35f, 35f);
            }
        }

        void Stop()
        {
            if (What == Kind.Preach) { if (Sermon.Speaker == this) Sermon.End(); if (armsKnown) Arms(0f, 0f); if (onSpot) Body(false); }
            if (an != null) an.speed = 1f;
            if (book != null) Destroy(book.gameObject);
            if (Led != null) Led.Loose();                                               // the prisoner he led fights too
            if (sitSt >= 0 && sitSt <= 3 && rb != null) Body(false);                    // up from the bench to fight
            FreeSeat();
            if (mob != null && mob.ctrl != null) Ritual.Casters.Remove(mob.ctrl);
            if (ai != null && ai._ai != null) { ai._ai.canMove = true; }
            Ritual.MoveAnim(mob, false);
            Destroy(this);
        }

        void OnDestroy() { if (Led != null && mob != null && !mob.alive) Led.Loose(); FreeSeat(); if (Sermon.Speaker == this) Sermon.End(); }
    }

    /// while busy, the game's idle walk (back to his spawn spot, a random stroll) is skipped - it pulled him the other
    /// way every tick, which made him jerk; he still looks for the hero as usual
    [HarmonyLib.HarmonyPatch(typeof(AIController), nameof(AIController.Walk))]
    internal static class ActivityNoIdleWalkPatch
    {
        static bool Prefix(AIController __instance, float distance)
        {
            try
            {
                if (__instance == null || __instance.GetComponent<Activity>() == null) return true;
                if (distance < 45f) __instance.UpdateTarget();
                return Patches.Target(__instance) != null;                                  // he saw the hero: the game goes on as usual
            }
            catch { return true; }
        }
    }
}
