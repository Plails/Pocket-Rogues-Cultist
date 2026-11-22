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
        public enum Kind { Read, Pray, Shoot, Patrol, Guard, Train, Sit, Wander, Escort, Execute, Shuffle, Follow, Inspect, Study, Preach, Work, Sleep }
        public Kind What;
        public Vector2 Look;                 // what he faces (the lectern, the statue, the target, the door); Wander: the room's middle
        public Vector2[] Route;              // the points he walks between (Inspect / Study: the things he looks at)
        public bool Stay;                    // a soldier who keeps his post (not taken into a patrol)
        public float Radius = 2.5f;          // Wander: how far from the middle
        public Container Cage;
        public Container Bed;                // Sleep: the bed he sleeps in
        public Container[] Cages;            // Execute: the cages he takes his prisoners from
        public Transform Leader;             // Follow: the one he walks his round with
        public int Slot = 1;                 // Follow: his place behind the leader
        public bool StartStanding;           // Sit: on his feet at first, sits down later
        public readonly List<Activity> Mates = new List<Activity>();   // Patrol: those walking with him
        public float Heading;                // the way he walks (his mates keep their places by it)

        AIController ai; Mob mob; Animator an; Rigidbody2D rb; RigidbodyType2D bodyWas; RigidbodyConstraints2D consWas;
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
            if (rb != null) { bodyWas = rb.bodyType; consWas = rb.constraints; }
            try { an = mob != null && mob.ctrl != null ? mob.ctrl.anim : null; } catch { }
            hp0 = mob != null ? mob.HP_cur : 0f;
            next = Time.time + UnityEngine.Random.Range(0.5f, 2.5f);
            Heading = transform.eulerAngles.z;
            // one set down inside a table or a bench (they are fixed now: they no longer get shoved off him) steps out
            // beside it first - a reader stood in the middle of his desk (the user)
            if (What != Kind.Sit && What != Kind.Sleep && Physics2D.OverlapCircleAll(transform.position, 0.3f).Any(c => c != null && !c.isTrigger && c.GetComponentInParent<Container>() != null && c.GetComponentInParent<Mob>() == null))
            {
                var free = SpotBy(transform.position, 0.7f, 1.0f, 1f); Claimed = null;
                SetPos(free);
            }
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
            if (What == Kind.Sleep) SleepStart();
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
            // the busy ones never push one another (crowding round a thing they jostled and jerked)
            All.RemoveWhere(a => a == null);
            foreach (var o in All)
                foreach (var oc in o.GetComponentsInChildren<Collider2D>(true))
                    foreach (var mc in GetComponentsInChildren<Collider2D>(true))
                        if (oc != null && mc != null && !oc.isTrigger && !mc.isTrigger) Physics2D.IgnoreCollision(mc, oc, true);
            All.Add(this);
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
        public static readonly HashSet<Activity> All = new HashSet<Activity>();

        /// a place to stand by a thing, on his side of it, not where another already stands (they spread round it)
        Vector2 SpotBy(Vector2 pt, float r0, float r1, float spread)
        {
            Vector2 d = (Vector2)transform.position - pt;
            float a0 = d.sqrMagnitude > 0.01f ? Mathf.Atan2(d.y, d.x) : -Mathf.PI / 2f;
            Vector2 best = pt + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * r0;
            bool first = true;
            for (int i = 0; i < 27; i++)
            {
                // round the thing, farther out each round: a spot inside the (now fixed) table put him in it - the user
                float a = a0 + ((i % 9) == 0 ? 0f : (((i % 9) + 1) / 2) * 0.7f * ((i % 9) % 2 == 0 ? 1f : -1f)) + UnityEngine.Random.Range(-spread, spread) * 0.3f;
                Vector2 p = pt + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (UnityEngine.Random.Range(r0, r1) + 0.35f * (i / 9));
                if (Physics2D.OverlapCircleAll(p, 0.34f).Any(c => c != null && !c.isTrigger && c.GetComponentInParent<Container>() != null)) continue;   // not in a table, a bench, a crate
                if (first) { best = p; first = false; }
                bool near = false;
                foreach (var o in All)
                    if (o != null && o != this && (((Vector2)o.transform.position - p).sqrMagnitude < 0.9f * 0.9f || o.Claimed.HasValue && (o.Claimed.Value - p).sqrMagnitude < 0.9f * 0.9f)) { near = true; break; }
                if (near || Physics2D.OverlapCircleAll(p, 0.3f, ObstacleMask).Any(c => c != null && !c.isTrigger)) continue;
                best = p; break;
            }
            Claimed = best;
            return best;
        }
        public Vector2? Claimed;

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

        void Halt() { Moving = false; if (ai != null && ai._ai != null) ai._ai.canMove = false; if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic) { rb.velocity = Vector2.zero; rb.constraints = RigidbodyConstraints2D.FreezeAll; } Ritual.MoveAnim(mob, false); }
        void Free() { if (rb != null) rb.constraints = RigidbodyConstraints2D.FreezeRotation; }
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
            Free();
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
            Turn(restAng, 100f);                                                        // he stands still: no glancing about (the legs stepped at every turn)
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
                Look2 = SpotBy(pt, 1.0f, 1.3f, 0.8f);
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
                    offs = 0f;
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
                    offs = 0f;
                    done = t > 5.2f;
                    if (done) offs = 0f;
                    break;
            }
            if (done) { step = 0; leg += 1 + (Route.Length > 2 ? UnityEngine.Random.Range(0, 2) : 0); Pause(1.5f, 3.5f); }
        }

        // ---------------------------------------------------------------- sitting on the benches
        static readonly Dictionary<Container, Activity[]> taken = new Dictionary<Container, Activity[]>();
        bool turned;
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
            Above(sitting);
            if (rb == null) return;
            rb.velocity = Vector2.zero;
            rb.bodyType = sitting ? RigidbodyType2D.Kinematic : bodyWas;
        }

        /// one sitting is drawn whole over his bench (his arms are drawn below his body, and the bench came between:
        /// the body sat on the bench, the arms under it - the user's picture)
        UnityEngine.Rendering.SortingGroup above;
        void Above(bool on)
        {
            try
            {
                if (!on) { if (above != null) above.enabled = false; return; }
                // over the bench AND over the table he sits at: the topmost of their pictures by layer, then by order
                // (the first try took the bench's highest order on whatever layer - he ended up under it, the user)
                var near = Physics2D.OverlapCircleAll(transform.position, 1.6f).Select(c => c != null ? c.GetComponentInParent<Container>() : null).Where(c => c != null).Distinct().ToList();
                if (bench != null && !near.Contains(bench)) near.Add(bench);
                var srs = near.SelectMany(c => c.GetComponentsInChildren<SpriteRenderer>(true)).Where(r => r != null && r.sprite != null && r.enabled).ToList();
                var mine = GetComponentsInChildren<SpriteRenderer>(true).Where(r => r != null && !(above != null && false)).ToList();
                srs.AddRange(mine);
                if (srs.Count == 0) return;
                var top = srs.OrderByDescending(r => SortingLayer.GetLayerValueFromID(r.sortingLayerID)).ThenByDescending(r => r.sortingOrder).First();
                int layer = top.sortingLayerID;
                int order = srs.Where(r => r.sortingLayerID == layer).Max(r => r.sortingOrder) + 2;
                if (above == null) above = GetComponent<UnityEngine.Rendering.SortingGroup>() ?? gameObject.AddComponent<UnityEngine.Rendering.SortingGroup>();
                above.sortingLayerID = layer; above.sortingOrder = Mathf.Min(order, 390); above.enabled = true;   // (under the cages' bars, 400)
            }
            catch { }
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
                    if (Walk(approach, 1.0f)) { sitSt = 1; turned = false; }
                    return;
                case 1:                                                                     // he sits down
                {
                    // (the user: they were dragged onto the bench) he comes up to it, turns his back to it, steps back
                    // onto the seat and only then sits
                    if (!benchOk) { Body(false); FreeSeat(); OnFeet(); return; }
                    Halt();
                    float want = Ang(SeatFace(bench));
                    if (!turned)
                    {
                        Turn(want, 420f);
                        if (Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.z, want)) < 10f) { turned = true; slideAt = Time.time; slideFrom = transform.position; }
                        return;
                    }
                    Vector2 to = SeatPos(bench, seat);
                    float dur = Mathf.Max(0.3f, Vector2.Distance(slideFrom, to) / 1.1f);
                    float k = Mathf.Clamp01((Time.time - slideAt) / dur);
                    SetPos(Vector2.Lerp(slideFrom, to, k));
                    Turn(want, 360f);
                    Ritual.MoveAnim(mob, k < 1f);                                               // a step back, legs moving
                    if (k >= 1f) { Body(true); sitSt = 2; restAng = want; glance = 0f; sitUntil = Time.time + UnityEngine.Random.Range(10f, 25f); }
                    return;
                }
                case 2:                                                                     // sitting, looking about now and then
                    if (!benchOk) { Body(false); FreeSeat(); OnFeet(); return; }
                    Halt();
                    SetPos(SeatPos(bench, seat));
                    LookAbout();
                    if (Time.time > sitUntil)
                    {
                        if (UnityEngine.Random.value < 0.6f) { sitSt = 3; slideAt = Time.time; slideFrom = transform.position; approach = Approach(bench, seat); Body(false); }
                        else sitUntil = Time.time + UnityEngine.Random.Range(6f, 14f);
                    }
                    return;
                case 3:                                                                     // he gets up and steps away from the bench, then turns where he goes
                {
                    float dur = Mathf.Max(0.3f, Vector2.Distance(slideFrom, approach) / 1.1f);
                    float k = Mathf.Clamp01((Time.time - slideAt) / dur);
                    Halt();
                    SetPos(Vector2.Lerp(slideFrom, approach, k));
                    Vector2 fwd = approach - slideFrom;
                    if (fwd.sqrMagnitude > 0.0001f) Turn(Ang(fwd), 360f);
                    Ritual.MoveAnim(mob, k < 1f);
                    if (k >= 1f) { FreeSeat(); OnFeet(); }
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
            if (What == Kind.Sleep) { SleepTick(); return; }                                // asleep: he hears no sermon
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
                        Free();
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
                    if (step == 0) { Look2 = SpotBy(pt, 1.2f, 1.5f, 0.6f); step = 1; }
                    if (Walk(Look2, 1.1f)) { Face(pt); step = 0; leg += 1 + UnityEngine.Random.Range(0, Route.Length > 2 ? 2 : 1); Pause(3f, 6f); }
                    return;
                }
                case Kind.Shoot:
                    if (Broken()) { Leave(); return; }
                    if (!atSpot) { if (Walk(shotSpot, 1.1f)) { atSpot = true; pin = transform.position; } return; }   // a few paces off the target first
                    break;
                case Kind.Train:
                    if (Broken()) { Leave(); return; }
                    if (!pinned) { pinned = true; pin = transform.position; }
                    break;
                case Kind.Work:
                    if (!pinned) { if (Walk(SpotFor(Look, 0.95f), 1.0f)) { pinned = true; pin = transform.position; } return; }
                    break;
            }
            Halt();
            if ((What == Kind.Train || What == Kind.Work) && pinned || What == Kind.Shoot && atSpot) SetPos(pin);   // his blows do not carry him off his place
            Face(Look);
            if (hitAt > 0f && Time.time >= hitAt) hitAt = 0f;                         // nothing of ours rocks the tables (the user)
            if (What == Kind.Read && Time.time > next)
            {
                next = Time.time + UnityEngine.Random.Range(2.5f, 5f);                  // he reads, standing still
                offs = 0f;
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
                next = Time.time + UnityEngine.Random.Range(1.5f, 2.4f);                // a thrust at the dummy: it rocks under the blow
                mob.ctrl.Attack(27); hitAt = Time.time + 0.3f;
            }
            else if (What == Kind.Shoot && Time.time > next)
            {
                next = Time.time + UnityEngine.Random.Range(3f, 5f);                    // a bolt into the target: it shakes when it strikes
                try { SFXController.Instance.PlaySFX(mob.BowSound, transform.position); } catch { }
                mob.ctrl.Attack(9); hitAt = Time.time + 0.45f;
            }
            else if (What == Kind.Work && Time.time > next)
            {
                next = Time.time + 0.95f;                                               // the cleaver comes down on the block
                hitAt = Time.time + 0.62f;
            }
            else if (What == Kind.Guard && Time.time > next)
            {
                next = Time.time + UnityEngine.Random.Range(2.5f, 5f);                  // the guard stands still
                offs = 0f;
            }
        }

        bool pinned; Vector2 pin; float hitAt;

        /// the dummy or target he trains at is broken (the user: they went on striking at nothing like blocks)
        Container drill; bool drillSeen;
        bool Broken()
        {
            if (!drillSeen) { drill = Thing(Look); drillSeen = true; return false; }
            return drillSeen && (drill == null || drill.hp <= 0f || !drill.gameObject.activeInHierarchy);
        }

        /// ...so he goes off to do something else: falls in behind a patrol going the rounds near by, or walks a round
        /// of his own past the things of the sanctum
        void Leave()
        {
            pinned = false; atSpot = false; hitAt = 0f; Stay = false; Claimed = null;
            Free();
            Activity lead = null; float best = 20f * 20f;
            foreach (var o in All)
            {
                if (o == null || o == this || o.What != Kind.Patrol || o.Leader != null || o.Stay || o.Mates.Count >= 3) continue;
                float d = ((Vector2)o.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (d < best) { best = d; lead = o; }
            }
            if (lead != null)
            {
                What = Kind.Follow; Leader = lead.transform; Slot = lead.Mates.Count + 1; Route = null; lead.Mates.Add(this);
                SanctumPlugin.Log.LogInfo("Sanctum: " + name + " broke what he trained at - joins a patrol");
                return;
            }
            var pts = Sights(transform.position, 16f).ToList();
            if (pts.Count >= 2) { pts.Insert(0, home); What = Kind.Patrol; Route = pts.ToArray(); leg = 1; }
            else { What = Kind.Wander; Look = home; Radius = 3f; Route = null; }
            SanctumPlugin.Log.LogInfo("Sanctum: " + name + " broke what he trained at - goes on " + What);
        }

        /// the thing standing at a point (a dummy, a target, a table)
        static Container Thing(Vector2 at)
        {
            foreach (var c in Physics2D.OverlapCircleAll(at, 0.7f)) { var k = c != null ? c.GetComponentInParent<Container>() : null; if (k != null && k.name.StartsWith("Sanctum_occ_", StringComparison.Ordinal)) return k; }
            return null;
        }

        /// a free place right by a thing, on his side
        Vector2 SpotFor(Vector2 at, float r) => Claimed ?? SpotBy(at, r, r + 0.15f, 0.4f);

        // ---------------------------------------------------------------- small movements while standing at his business
        Transform uR, fR, uL, fL, hd; Quaternion uR0, fR0, uL0, fL0, hd0; bool bonesKnown, touched;
        float gUR, gFR, gUL, gFL, gH, gPhase = -1f, gNext; int gKind;

        void Bones()
        {
            if (bonesKnown) return;
            bonesKnown = true;
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                switch (t.name)
                {
                    case "UpperArm_R": uR = t; break;
                    case "UpperArm_L": uL = t; break;
                    case "Arm_R": case "ForeArm_R": if (fR == null) fR = t; break;
                    case "Arm_L": case "ForeArm_L": if (fL == null) fL = t; break;
                    case "Head": if (hd == null) hd = t; break;
                }
            }
            if (uR != null) uR0 = uR.localRotation;
            if (fR != null) fR0 = fR.localRotation;
            if (uL != null) uL0 = uL.localRotation;
            if (fL != null) fL0 = fL.localRotation;
            if (hd != null) hd0 = hd.localRotation;
        }

        void Pose(float ur, float fr, float ul, float fl, float h, float k = 14f)
        {
            Bones();
            float a = Mathf.Clamp01(k * Time.deltaTime);
            gUR = Mathf.Lerp(gUR, ur, a); gFR = Mathf.Lerp(gFR, fr, a); gUL = Mathf.Lerp(gUL, ul, a); gFL = Mathf.Lerp(gFL, fl, a); gH = Mathf.Lerp(gH, h, a);
            if (uR != null) uR.localRotation = uR0 * Quaternion.Euler(0f, 0f, gUR);
            if (fR != null) fR.localRotation = fR0 * Quaternion.Euler(0f, 0f, gFR);
            if (uL != null) uL.localRotation = uL0 * Quaternion.Euler(0f, 0f, gUL);
            if (fL != null) fL.localRotation = fL0 * Quaternion.Euler(0f, 0f, gFL);
            if (hd != null) hd.localRotation = hd0 * Quaternion.Euler(0f, 0f, gH);
            touched = true;
        }

        void Rest()
        {
            if (uR != null) uR.localRotation = uR0;
            if (fR != null) fR.localRotation = fR0;
            if (uL != null) uL.localRotation = uL0;
            if (fL != null) fL.localRotation = fL0;
            if (hd != null) hd.localRotation = hd0;
            gUR = gFR = gUL = gFL = gH = 0f; touched = false;
        }

        void Release()
        {
            if (!touched) return;
            Pose(0f, 0f, 0f, 0f, 0f, 18f);
            if (Mathf.Abs(gUR) + Mathf.Abs(gFR) + Mathf.Abs(gUL) + Mathf.Abs(gFL) + Mathf.Abs(gH) < 0.6f) Rest();
        }

        /// what his hands and head do while he stands at his business (so one sees what he is doing): a reader turns
        /// the pages and nods over them, the one sitting at a table eats and drinks, the butcher raises his cleaver and
        /// brings it down, the one standing about breathes, glances, and gestures now and then
        void Gesture()
        {
            if (Moving || listening || returning || posed || held || What == Kind.Study || What == Kind.Pray || What == Kind.Train || What == Kind.Shoot || What == Kind.Preach) { Release(); return; }
            float now = Time.time;
            switch (What)
            {
                case Kind.Read:
                {
                    if (gPhase < 0f && now > gNext) { gPhase = 0f; gNext = now + UnityEngine.Random.Range(3f, 5.5f); }
                    float ur = 0f, fr = 0f;
                    if (gPhase >= 0f) { gPhase += Time.deltaTime; float k = Mathf.Sin(Mathf.PI * Mathf.Clamp01(gPhase / 0.9f)); ur = 30f * k; fr = -35f * k; if (gPhase > 0.9f) gPhase = -1f; }   // a page turned
                    Pose(ur + 3f * Mathf.Sin(now * 1.1f), fr, -4f * Mathf.Sin(now * 0.9f), 0f, 7f * Mathf.Sin(now * 0.7f));                    // he reads along the lines
                    return;
                }
                case Kind.Work:
                {
                    if (!pinned) { Release(); return; }
                    float t = (now % 0.95f) / 0.95f;                                      // the cleaver: up slowly, down hard
                    float up = t < 0.62f ? Mathf.SmoothStep(0f, 1f, t / 0.62f) : t < 0.72f ? 1f - (t - 0.62f) / 0.1f : 0f;
                    Pose(-50f * up, 45f * up, 8f, -20f, -4f * up, 30f);                  // the other hand holds the meat
                    return;
                }
                case Kind.Sit:
                {
                    if (sitSt != 2) { Release(); return; }
                    if (gPhase < 0f && now > gNext) { gPhase = 0f; gNext = now + UnityEngine.Random.Range(4f, 8f); }
                    float ur = 0f, fr = 0f;
                    if (gPhase >= 0f) { gPhase += Time.deltaTime; float k = Mathf.Sin(Mathf.PI * Mathf.Clamp01(gPhase / 1.6f)); ur = 25f * k; fr = -70f * k; if (gPhase > 1.6f) gPhase = -1f; }   // a bite, a drink
                    Pose(ur, fr, 0f, 0f, 4f * Mathf.Sin(now * 0.8f));
                    return;
                }
                case Kind.Guard: case Kind.Follow: case Kind.Wander: case Kind.Inspect: case Kind.Patrol: case Kind.Execute:
                {
                    if (What == Kind.Execute && step >= 3) { Release(); return; }
                    if (gPhase < 0f && now > gNext) { gPhase = 0f; gNext = now + UnityEngine.Random.Range(4f, 9f); gKind = UnityEngine.Random.Range(0, 3); }
                    float ur = 0f, fr = 0f, ul = 0f, fl = 0f;
                    if (gPhase >= 0f)
                    {
                        gPhase += Time.deltaTime; float k = Mathf.Sin(Mathf.PI * Mathf.Clamp01(gPhase / 1.4f));
                        if (gKind == 0) { ur = 20f * k; fr = -30f * k; }                   // a hand raised as he speaks
                        else if (gKind == 1) { ul = -18f * k; ur = 18f * k; }              // a shrug, arms out
                        else { fr = -25f * k; fl = 25f * k; }                              // hands brought together
                        if (gPhase > 1.4f) gPhase = -1f;
                    }
                    Pose(ur + 3f * Mathf.Sin(now * 1.3f), fr, ul - 3f * Mathf.Sin(now * 1.3f + 0.6f), fl, 5f * Mathf.Sin(now * 0.6f), 10f);   // breathing, a glance
                    return;
                }
                default: Release(); return;
            }
        }

        // ---------------------------------------------------------------- asleep in his bed
        // (the user: let them sleep, as smoothly as the game's own animations) - his figure is put away and he lies in
        // the bed face up in his hood, the blanket drawn up to his chest; the blanket rises and falls slowly as he
        // breathes, his head stirs with it, now and then a "z" drifts up and fades. He wakes when he is hit, when
        // the hero comes right up to him, or when the ritual calls - and is up at once.
        readonly List<Renderer> asleepHid = new List<Renderer>();
        GameObject sleepGo; Transform sHead, sBlanket; float breathPh, zAt;
        readonly List<SpriteRenderer> zs = new List<SpriteRenderer>(); readonly List<float> zBorn = new List<float>(); readonly List<Vector3> zFrom = new List<Vector3>();

        void SleepStart()
        {
            var bsr = Bed != null ? Bed.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r => r.sprite != null) : null;
            if (bsr == null) { What = Kind.Wander; Look = transform.position; return; }
            if (rb != null) { rb.velocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Kinematic; }
            foreach (var bc in Bed.GetComponentsInChildren<Collider2D>(true))
                foreach (var mc in GetComponentsInChildren<Collider2D>(true))
                    if (bc != null && mc != null) Physics2D.IgnoreCollision(mc, bc, true);
            SetPos(Bed.transform.position);
            transform.rotation = Quaternion.Euler(0f, 0f, Bed.transform.eulerAngles.z + 90f);   // (along the bed, for when he gets up)
            Moving = false; if (ai != null && ai._ai != null) ai._ai.canMove = false;
            asleepHid.Clear();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                if (r != null && r.enabled && (r is SpriteRenderer || r is LineRenderer)) { r.enabled = false; asleepHid.Add(r); }
            sleepGo = new GameObject("OS_Asleep");
            sleepGo.transform.SetParent(Bed.transform, false);
            SpriteRenderer Layer(string art, int up, string name)
            {
                var go = new GameObject(name); go.transform.SetParent(sleepGo.transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Art.Get(art); sr.sortingLayerID = bsr.sortingLayerID; sr.sortingOrder = bsr.sortingOrder + up; sr.sharedMaterial = bsr.sharedMaterial;
                return sr;
            }
            sBlanket = Layer("cult_sleep_blanket", 2, "Blanket").transform;
            sHead = Layer("cult_sleep", 1, "Head").transform;
            breathPh = UnityEngine.Random.value * 10f;
            zAt = Time.time + UnityEngine.Random.Range(1f, 4f);
        }

        void SleepTick()
        {
            if (sleepGo == null || Bed == null || Bed.hp <= 0f) { Stop(); return; }
            if (rb != null) rb.velocity = Vector2.zero;
            // the breath: slow in, slower out (about one every 3.6 s), the blanket swelling across the chest
            float t = (Time.time + breathPh) / 3.6f, f = t - Mathf.Floor(t);
            float k = f < 0.42f ? Mathf.SmoothStep(0f, 1f, f / 0.42f) : Mathf.SmoothStep(1f, 0f, (f - 0.42f) / 0.58f);
            sBlanket.localScale = new Vector3(1f + 0.06f * k, 1f + 0.012f * k, 1f);
            sBlanket.localPosition = new Vector3(0f, 0.01f * k, 0f);
            sHead.localPosition = new Vector3(0f, 0.012f * k, 0f);
            sHead.localRotation = Quaternion.Euler(0f, 0f, 3f * Mathf.Sin((Time.time + breathPh) * 0.21f));   // he stirs a little in his sleep
            // a "z" now and then, drifting up from his head and fading
            if (Time.time >= zAt)
            {
                zAt = Time.time + UnityEngine.Random.Range(2.6f, 4.2f);
                var go = new GameObject("z"); go.transform.SetParent(sleepGo.transform, false);
                var sr = go.AddComponent<SpriteRenderer>(); var hsr = sHead.GetComponent<SpriteRenderer>();
                sr.sprite = Art.Get("occ_z"); sr.sortingLayerID = hsr.sortingLayerID; sr.sortingOrder = hsr.sortingOrder + 5; sr.sharedMaterial = hsr.sharedMaterial;
                var from = sleepGo.transform.TransformPoint(new Vector3(0.15f, 0.55f, 0f));
                go.transform.position = from; go.transform.rotation = Quaternion.identity;
                zs.Add(sr); zBorn.Add(Time.time); zFrom.Add(from);
            }
            for (int i = zs.Count - 1; i >= 0; i--)
            {
                float a = (Time.time - zBorn[i]) / 2.2f;
                if (zs[i] == null || a >= 1f) { if (zs[i] != null) Destroy(zs[i].gameObject); zs.RemoveAt(i); zBorn.RemoveAt(i); zFrom.RemoveAt(i); continue; }
                zs[i].transform.position = zFrom[i] + new Vector3(0.18f * Mathf.Sin(a * 5f), 0.55f * a, 0f);
                zs[i].transform.localScale = Vector3.one * (0.55f + 0.45f * a);
                zs[i].color = new Color(1f, 1f, 1f, a < 0.2f ? a / 0.2f : 1f - (a - 0.2f) / 0.8f);
            }
        }

        void SleepEnd()
        {
            foreach (var r in asleepHid) if (r != null) r.enabled = true;
            asleepHid.Clear();
            if (sleepGo != null) Destroy(sleepGo);
            sleepGo = null; zs.Clear(); zBorn.Clear(); zFrom.Clear();
            if (rb != null) rb.bodyType = bodyWas;
        }

        void Stop()
        {
            if (What == Kind.Sleep) SleepEnd();
            if (touched) Rest();
            if (rb != null) rb.constraints = consWas;
            Above(false);
            All.Remove(this);
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

        void OnDestroy() { if (Led != null && mob != null && !mob.alive) Led.Loose(); FreeSeat(); if (Sermon.Speaker == this) Sermon.End(); if (sleepGo != null) SleepEnd(); }
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
                var act = __instance != null ? __instance.GetComponent<Activity>() : null;
                if (act == null) return true;
                if (act.What == Activity.Kind.Sleep && distance > 2.5f) return false;        // a sleeper notices the hero only right by his bed
                if (distance < 45f) __instance.UpdateTarget();
                return Patches.Target(__instance) != null;                                  // he saw the hero: the game goes on as usual
            }
            catch { return true; }
        }
    }
}
