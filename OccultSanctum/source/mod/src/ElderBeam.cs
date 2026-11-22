using System.Collections;
using UnityEngine;

namespace OccultSanctum
{
    /// The elder's blood beam (the user): now and then, with a foe in sight 2.5..8.5 tiles away, he stops, gathers a
    /// red glow at his hand and pours out a beam of blood for about three seconds; it follows the foe slowly (one can
    /// step out of it), stops at walls and burns everyone of another side in it.
    ///
    /// The clash (the user, "like the magic duel"): if a hero breathes back at him meanwhile - the mage's dark flame or
    /// the necromancer's cursed breath, facing him - the two streams meet and fight: the point where they meet is
    /// pushed back and forth between them. Whoever it reaches is blown back and hurt badly; if the hero lets go of his
    /// breath the elder's beam drives it into him; after a while neither wins and both are thrown back.
    /// takes the dark wizard's animation events (his giant-beam clip calls them) and does nothing with them
    public class WizEvents : MonoBehaviour
    {
        public void AttackEvent_Melee() { } public void AttackEvent_Dist() { } public void AttackEvent_Dist3() { }
        public void BeamSetActive_True() { } public void BeamSetActive_False() { }
        public void Event_EnableShield() { } public void Event_DisableShield() { } public void Event_SpawnFireRandom() { } public void Event_SpawnFirePrefabForward() { }
        public void RandomTeleportation() { } public void ChangeState_DestroyWall() { } public void ChangeState_SpawnRandomDirSphere(int n) { }
        public void ShakeScene(float v) { } public void ShakeSceneLong(float v) { } public void SpawnDustForward() { } public void SpawnDustAll() { }
        public void SpawnDust(int i) { } public void PushEveryone() { } public void PushFront() { } public void DamageFront(float d) { }
        public void ChangeRigidType(int t) { } public void StartFight() { } public void PlayHitSFX() { } public void PlayerSFX_Footstep() { }
    }

    public class ElderBeam : MonoBehaviour
    {
        public const int WallMask = 35651840;                     // the game's own walls mask (GameManager.ObstacleToTarget)
        const float Range = 9f, Min = 2.5f, Max = 8.5f;
        // the caster's beam colours (red blood by default; the archmage's is violet - his rank's colour) and who he is
        public Color BHalo = new Color(0.55f, 0f, 0.03f, 0.45f), BBody = new Color(0.95f, 0.06f, 0.1f, 0.95f), BCore = new Color(1f, 0.78f, 0.75f, 1f);
        public string Who = "the elder";
        public float Width = 1f;                                  // the beam's thickness (the archmage is big: a thick beam)
        static readonly Color FHalo = new Color(1f, 0.32f, 0.03f, 0.45f), FBody = new Color(1f, 0.58f, 0.1f, 0.95f), FCore = new Color(1f, 0.96f, 0.72f, 1f);
        static readonly Color NHalo = new Color(0.15f, 0.75f, 0.25f, 0.45f), NBody = new Color(0.45f, 1f, 0.35f, 0.95f), NCore = new Color(0.9f, 1f, 0.85f, 1f);

        Mob mob; AIController ai; Rigidbody2D rb; Animator an; SpriteRenderer like;
        Beam beam, theirs;
        float next;
        bool busy;

        void Start()
        {
            mob = GetComponent<Mob>(); ai = GetComponent<AIController>(); rb = GetComponent<Rigidbody2D>(); an = GetComponent<Animator>();
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true)) if (sr.gameObject.name == "Body") { like = sr; break; }
            next = Time.time + Random.Range(4f, 7f);
        }

        void OnDestroy() { beam?.Destroy(); theirs?.Destroy(); Release(); Lights(false); }
        void OnDisable() { EndPose(); }

        // ---- the onlookers (the user): while the beams fight, every other monster about leaves off fighting, steps
        // back out of the beams' way (both burn whoever is in them) and stands watching the duel
        class Onlooker { public AIController ai; public Rigidbody2D rb; public Animator an; public bool wasOn, actOn, pathOn, sits; public Activity act; public RigidbodyConstraints2D cons; public Vector2 spot; public bool hasSpot; public float pickAt, dbg; }
        readonly System.Collections.Generic.List<Onlooker> crowd = new System.Collections.Generic.List<Onlooker>();

        void Gather(Controller foe)
        {
            Release();
            foreach (var o in Object.FindObjectsOfType<AIController>())
            {
                if (o == null || o == ai || o == foe || o._creature == null || !o._creature.alive) continue;
                if (Vector2.Distance(o.transform.position, transform.position) > 16f && Vector2.Distance(o.transform.position, foe.transform.position) > 16f) continue;
                if (o._creature is Mob mb && Prisoner.Caged.Contains(mb)) continue;
                var r = o.GetComponent<Rigidbody2D>();
                var w = new Onlooker { ai = o, rb = r, an = o.GetComponent<Animator>(), wasOn = o._enableAI, act = o.GetComponent<Activity>() };
                if (r != null) w.cons = r.constraints;
                if (w.act != null) { w.actOn = w.act.enabled; w.sits = w.act.What == Activity.Kind.Sit || w.act.What == Activity.Kind.Sleep; if (!w.sits) w.act.enabled = false; }
                try { if (o._ai != null) { w.pathOn = o._ai.canMove; o._ai.canMove = false; } } catch { }
                if (r != null && !w.sits) r.constraints = RigidbodyConstraints2D.FreezeRotation;
                crowd.Add(w);
                o._enableAI = false;
            }
            // (the sandbox test: in a narrow lane they walked into the big archmage and into each other, and the physics
            // pushed them back - so while they step aside, their bodies pass through the fighters' and each other's)
            var bodies = new System.Collections.Generic.List<Collider2D>();
            bodies.AddRange(GetComponentsInChildren<Collider2D>());
            if (foe != null) bodies.AddRange(foe.GetComponentsInChildren<Collider2D>());
            foreach (var w in crowd) if (w.ai != null) bodies.AddRange(w.ai.GetComponentsInChildren<Collider2D>());
            foreach (var w in crowd)
            {
                if (w.ai == null) continue;
                foreach (var mine in w.ai.GetComponentsInChildren<Collider2D>())
                    foreach (var other in bodies)
                    {
                        if (mine == null || other == null || mine == other || mine.isTrigger || other.isTrigger || other.transform.IsChildOf(w.ai.transform)) continue;
                        try { Physics2D.IgnoreCollision(mine, other, true); ignored.Add(new System.Collections.Generic.KeyValuePair<Collider2D, Collider2D>(mine, other)); } catch { }
                    }
            }
            SanctumPlugin.Log.LogInfo("Sanctum: " + crowd.Count + " monsters stop to watch the beam duel");
        }

        /// the distance to the first real wall along a way (bodies, triggers and props do not count); -1: none
        public static float WallAt(Vector2 from, Vector2 dir, float dist, float radius = 0f)
        {
            float best = -1f;
            var hits = radius > 0f ? Physics2D.CircleCastAll(from, radius, dir, dist, WallMask) : Physics2D.RaycastAll(from, dir, dist, WallMask);
            foreach (var h in hits)
            {
                if (h.collider == null || h.collider.isTrigger) continue;
                if (h.collider.GetComponentInParent<Creature>() != null || h.collider.GetComponentInParent<Container>() != null) continue;
                if (best < 0f || h.distance < best) best = h.distance;
            }
            return best;
        }

        /// how well a place keeps out of the duel: 1 = 4.5 tiles off the beams and 4 off either fighter
        static float Safety(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a; float k = ab.sqrMagnitude > 0.001f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
            float line = Vector2.Distance(p, a + ab * k) / 4.5f, fa = Vector2.Distance(p, a) / 4f, fb = Vector2.Distance(p, b) / 4f;
            return Mathf.Min(1f, Mathf.Min(line, Mathf.Min(fa, fb)));
        }

        static Vector2 PickSpot(Vector2 me, Vector2 a, Vector2 b)
        {
            Vector2 best = me; float bs = Safety(me, a, b), bl = 0f;
            if (bs >= 1f) return me;
            for (int i = 0; i < 24; i++)
            {
                Vector2 d = Quaternion.Euler(0f, 0f, i * 15f) * Vector2.right;
                float wd = WallAt(me, d, 9f, 0.3f);
                float reach = wd >= 0f ? wd - 0.2f : 9f;
                for (float r = 1f; r <= reach; r += 0.75f)
                {
                    Vector2 p = me + d * r;
                    float sc = Safety(p, a, b) - r * 0.012f;                              // (of two as good, the nearer)
                    if (sc > bs + 0.02f) { bs = sc; best = p; bl = r; }
                }
            }
            return best;
        }

        void Watch(Vector2 a, Vector2 b, Vector2 P)
        {
            foreach (var w in crowd)
            {
                if (w.ai == null || w.ai._creature == null || !w.ai._creature.alive) continue;
                Vector2 me = w.ai.transform.position;
                if (w.sits && Safety(me, a, b) * 4.5f > 1.6f) { Look(w, P - me, false); continue; }   // (one on a bench gets up only if the beam is on him)
                if (w.sits && w.act != null) { w.act.enabled = false; w.sits = false; if (w.rb != null) w.rb.constraints = RigidbodyConstraints2D.FreezeRotation; }
                if (!w.hasSpot || Time.time > w.pickAt) { w.spot = PickSpot(me, a, b); w.hasSpot = true; w.pickAt = Time.time + 0.8f + Random.value * 0.4f; }
                Vector2 go = w.spot - me;
                bool moving = go.magnitude > 0.2f;
                if (w.rb != null) { w.rb.velocity = Vector2.zero; if (moving) Step(w.rb, go.normalized * Mathf.Min(go.magnitude, 3.4f * Time.deltaTime)); }
                else if (moving) w.ai.transform.position += (Vector3)(go.normalized * Mathf.Min(go.magnitude, 3f * Time.deltaTime));
                Look(w, moving ? go : P - me, moving);
                if (Debug && Time.time > w.dbg) { w.dbg = Time.time + 1f; SanctumPlugin.Log.LogInfo($"Sanctum dbg onlooker {w.ai.name}: at {me} safe {Safety(me, a, b):0.00} spot {w.spot} safe {Safety(w.spot, a, b):0.00} moving {moving} rb {(w.rb != null ? w.rb.bodyType + "/" + w.rb.constraints + "/sim " + w.rb.simulated : "none")} wall-to-spot {WallAt(me, go.sqrMagnitude > 0.0001f ? go.normalized : Vector2.up, go.magnitude + 0.35f):0.00}"); }
            }
        }
        public static bool Debug;

        static void Look(Onlooker w, Vector2 look, bool moving)
        {
            float want = Mathf.Atan2(look.y, look.x) * Mathf.Rad2Deg;
            w.ai.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.MoveTowardsAngle(w.ai.transform.eulerAngles.z, want, 400f * Time.deltaTime));
            try { Ritual.MoveAnim(w.ai._creature, moving); w.an?.SetFloat("MoveSpeed", moving ? 1f : 0f); } catch { }
        }

        /// a step that slides along a wall instead of stopping at it
        static void Step(Rigidbody2D r, Vector2 d)
        {
            foreach (float turn in new[] { 0f, 50f, -50f, 90f, -90f })
            {
                Vector2 v = (Vector2)(Quaternion.Euler(0f, 0f, turn) * d);
                if (WallAt(r.position, v.normalized, v.magnitude + 0.35f) >= 0f) continue;
                Vector2 np = r.position + v; r.position = np; r.transform.position = new Vector3(np.x, np.y, r.transform.position.z); return;
            }
        }

        readonly System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<Collider2D, Collider2D>> ignored = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<Collider2D, Collider2D>>();

        void Release()
        {
            foreach (var kv in ignored) if (kv.Key != null && kv.Value != null) try { Physics2D.IgnoreCollision(kv.Key, kv.Value, false); } catch { }
            ignored.Clear();
            foreach (var w in crowd)
            {
                if (w.ai == null) continue;
                if (w.rb != null) { w.rb.velocity = Vector2.zero; w.rb.constraints = w.cons; }
                try { Ritual.MoveAnim(w.ai._creature, false); w.an?.SetFloat("MoveSpeed", 0f); } catch { }
                if (w.act != null) w.act.enabled = w.actOn;
                try { if (w.ai._ai != null) w.ai._ai.canMove = w.pathOn; } catch { }
                if (w.ai._creature != null && w.ai._creature.alive) w.ai._enableAI = w.wasOn;
            }
            crowd.Clear();
        }

        /// a beam of the duel burns whoever stands in it, but the two fighters
        void Sear(Vector2 a, Vector2 b, Controller by, Controller foe)
        {
            float dmg = 0f;
            try { dmg = (float)mob.Damage * 0.35f; } catch { dmg = 5f; }
            Vector2 d = b - a; if (d.sqrMagnitude < 0.01f) return;
            burnt.Clear();
            foreach (var h in Physics2D.CircleCastAll(a, 0.3f * Width, d.normalized, d.magnitude))
            {
                var c = h.collider != null ? h.collider.GetComponentInParent<Creature>() : null;
                if (c == null || c == mob || !c.alive || !burnt.Add(c) || (foe != null && c == foe._creature) || (c is Mob mb && Prisoner.Caged.Contains(mb))) continue;
                try { c.SetDamage(dmg * 2f, 1f, false, by); Hits++; } catch { }
                Sparks.Burst(c.transform.position, BBody, 3, 2.2f, 0.25f, like, 0.35f);
            }
        }

        // (the user: the beam must come out of his hands) between his two hands, a little before them
        Transform handL, handR; bool handsLooked;
        Vector2 Hand
        {
            get
            {
                if (!handsLooked)
                {
                    handsLooked = true;
                    foreach (var t in GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name.StartsWith("Hand_L", System.StringComparison.Ordinal) && handL == null) handL = t;
                        if (t.name.StartsWith("Hand_R", System.StringComparison.Ordinal) && handR == null) handR = t;
                    }
                }
                if (handL != null && handR != null)
                    return ((Vector2)handL.position + (Vector2)handR.position) * 0.5f + (Vector2)transform.right * 0.12f * transform.lossyScale.x;
                return (Vector2)transform.position + (Vector2)transform.right * 0.55f * transform.lossyScale.x;
            }
        }

        // ---- the cast (the user: like the fire breath): while the beam pours he breathes it as the mage breathes his dark
        // flame - the game's "WizSpecial" breath pose of his own animator (a bool on the goblins' and the hero's
        // animators, a trigger on some); without it, his own cast (WizHeal) held still
        public bool WizardAnim;
        bool holding, breathing; float poseAt; int mode = -1;               // 0 his cast held, 1 the breath (bool), 2 the breath (trigger)

        int Mode()
        {
            if (mode >= 0) return mode;
            mode = 0;
            try { foreach (var p in an.parameters) if (p.name == "WizSpecial") mode = p.type == AnimatorControllerParameterType.Bool ? 1 : p.type == AnimatorControllerParameterType.Trigger ? 2 : 0; } catch { }
            SanctumPlugin.Log.LogInfo("Sanctum: " + Who + " pours his beam " + (mode == 1 ? "with the fire breath's pose (WizSpecial)" : mode == 2 ? "with the breath's trigger (WizSpecial)" : "with his own cast held (no breath in his animator)"));
            return mode;
        }

        void CastPose()
        {
            holding = false; breathing = false; poseAt = Time.time;
            if (an == null) return;
            try
            {
                an.speed = 1f;
                int m = WizardAnim ? Mode() : 0;
                if (m == 1) { an.SetBool("WizSpecial", true); breathing = true; }
                else if (m == 2) { an.SetTrigger("WizSpecial"); breathing = true; }
                else an.SetTrigger("WizHeal");
            }
            catch { }
        }

        /// called every frame of the cast: the breath kept on; his own cast stands still once its pose is reached
        void KeepPose()
        {
            if (an == null) return;
            try
            {
                if (breathing) { if (mode == 1) an.SetBool("WizSpecial", true); return; }
                if (holding) return;
                var st = an.GetCurrentAnimatorStateInfo(0);
                if (Time.time - poseAt > 0.1f && (st.normalizedTime >= 0.4f || Time.time - poseAt > 0.9f)) { an.speed = 0f; holding = true; }
            }
            catch { }
        }

        void EndPose()
        {
            if (an == null) return;
            try
            {
                an.speed = 1f; holding = false;
                if (breathing && mode == 1) an.SetBool("WizSpecial", false);
                breathing = false;
            }
            catch { }
        }

        float nextAnswer, nextSeen;
        void Update()
        {
            if (busy || mob == null || ai == null || !mob.alive) return;
            if (Time.time > nextAnswer)
            {
                var b = Breather(transform.position, transform.position);
                if (b != null) { StartCoroutine(Answer(b)); return; }
            }
            if (Time.time > nextSeen)
            {
                nextSeen = Time.time + 2f;
                foreach (var c in Object.FindObjectsOfType<Controller>())
                    if (c != null && !(c is AIController) && Vector2.Distance(c.transform.position, transform.position) < 15f)
                    {
                        bool fl = false, nf = false, wz = false;
                        try { fl = c.darkFlame != null && c.darkFlame.activeInHierarchy; nf = c.necroFire != null && c.necroFire.activeInHierarchy; var an = c.GetComponent<Animator>() ?? c.GetComponentInChildren<Animator>(); wz = HasWiz(an) && an.GetBool("WizSpecial"); } catch { }
                        if (fl || nf || wz) SanctumPlugin.Log.LogInfo("Sanctum: near " + Who + ": hero " + c.name + " flame=" + fl + " necro=" + nf + " anim=" + wz);
                    }
            }
            if (Time.time < next) return;
            var t = ai._target;
            if (t == null || !t.gameObject.activeInHierarchy) return;
            var cr = t.GetComponent<Creature>();
            if (cr == null || !cr.alive || cr.faction == mob.faction) return;
            float d = Vector2.Distance(transform.position, t.position);
            if (d < Min || d > Max || WallAt(transform.position, ((Vector2)t.position - (Vector2)transform.position).normalized, d) >= 0f) { next = Time.time + 0.5f; return; }
            StartCoroutine(Cast(t));
        }

        void Face(Vector2 dir, float maxDeg)
        {
            float want = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float now = transform.eulerAngles.z;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.MoveTowardsAngle(now, want, maxDeg));
        }

        /// the dark wizard's giant beam turns as fast as he does (the user): his own speed_rotate, his own easing
        static float wizRot = -1f;
        static float WizRot()
        {
            if (wizRot >= 0f) return wizRot;
            wizRot = 10f;                                                                       // the game's default
            try
            {
                foreach (var w in Resources.LoadAll<BossAI_M_DarkWizard>("")) if (w != null) { wizRot = w.speed_rotate; break; }
            }
            catch { }
            SanctumPlugin.Log.LogInfo("Sanctum: the dark wizard's beam turn speed " + wizRot);
            return wizRot;
        }

        void Aim(Vector2 dir)
        {
            float want = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(WizRot() * 0.0015f), Time.deltaTime / 0.02f);   // per physics step -> per frame
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, 0f, want), k);
        }

        /// nothing but walls between (the two fighters' own colliders do not count)
        string blocker = "";
        bool Clear(Vector2 a, Vector2 b, Transform other)
        {
            foreach (var h in Physics2D.LinecastAll(a, b, WallMask))
            {
                if (h.collider == null) continue;
                var tr = h.collider.transform;
                if (tr.IsChildOf(transform) || (other != null && tr.IsChildOf(other))) continue;
                if (h.collider.GetComponentInParent<Creature>() != null) continue;
                // (the user: "no clash: wall" with no wall there - the beam itself got through) triggers, props and
                // things on the floor are no wall: only what stops the beam stops the clash
                if (h.collider.isTrigger || h.collider.GetComponentInParent<Container>() != null) continue;
                blocker = h.collider.name + " (layer " + h.collider.gameObject.layer + ")";
                return false;
            }
            return true;
        }

        void Hold()
        {
            KeepPose();
            ai._enableAI = false;
            if (rb != null) rb.velocity = Vector2.zero;
            try { an?.SetFloat("MoveSpeed", 0f); an?.SetFloat("StrafeSpeed", 0f); } catch { }
        }

        public void CastAt(Transform target) { if (!busy) StartCoroutine(Cast(target)); }
        public bool Busy => busy;

        IEnumerator Cast(Transform target)
        {
            busy = true;
            if (beam == null) beam = new Beam("ElderBeam", like, BHalo, BBody, BCore);
            CastPose();
            // gathering: a red glow grows at his hand
            var orb = Glow.Flare(null, like, 43); orb.color = BBody;
            for (float c = 0f; c < 0.75f; c += Time.deltaTime)
            {
                if (!Alive()) { Destroy(orb.gameObject); yield return End(); yield break; }
                Hold();
                if (target != null) Face((Vector2)target.position - (Vector2)transform.position, 360f * Time.deltaTime);
                orb.transform.position = Hand; orb.transform.localScale = Vector3.one * (0.2f + c * 1.1f + 0.08f * Mathf.Sin(Time.time * 30f));
                if (Random.value < 0.3f) Sparks.Burst(Hand + Random.insideUnitCircle * 0.6f, BBody, 1, -1.2f, 0.18f, like, 0.3f);
                yield return null;
            }
            Destroy(orb.gameObject);
            Sparks.Burst(Hand, BBody, 10, 2.5f, 0.3f, like);
            Sparks.Ring(Hand, BBody, 1.2f * Width, like);
            Shake(0.15f, 0.12f);
            beam.Show(true);
            Lights(true);
            float tick = 0f, ring = 0f;
            for (float b = 0f; b < 3.2f; b += Time.deltaTime)
            {
                if (!Alive()) break;
                Hold();
                if (target != null) Aim((Vector2)target.position - (Vector2)transform.position);   // follows like the dark wizard's beam
                Vector2 a = Hand, dir = transform.right;
                float wd = WallAt(a, dir, Range);                                                // (through every body: only walls stop it)
                Vector2 e = a + dir * (wd >= 0f ? wd : Range);
                float grow = Mathf.Clamp01(b / 0.15f) * Mathf.Clamp01((3.2f - b) / 0.2f);
                Vector2 tip = Vector2.Lerp(a, e, Mathf.Clamp01(b / 0.12f));
                beam.Set(a, tip, grow * Width, 0.35f + 0.25f * Mathf.Sin(Time.time * 17f));
                Shine(a, tip, grow, b);
                if (Random.value < 0.6f) Sparks.Burst(tip, BBody, 2, 2.6f, 0.24f, like, 0.32f);
                if (Random.value < 0.35f) Sparks.Burst(Vector2.Lerp(a, tip, Random.value), BCore, 1, 0.5f, 0.13f, like, 0.35f);
                ring -= Time.deltaTime;
                if (ring <= 0f && b > 0.12f) { ring = 0.32f; Sparks.Ring(tip, BBody, 0.7f * Width, like); }
                // a hero breathing back at him: the clash
                var foe = Breather(a, e);
                if (foe != null) { Lights(false); yield return Clash(foe); break; }
                tick -= Time.deltaTime;
                if (tick <= 0f) { tick = 0.25f; Burn(a, dir, Vector2.Distance(a, e)); }
                yield return null;
            }
            yield return End();
        }

        /// a hero breathes at him: he turns, a short flash at his hand, and his beam meets the breath head on
        IEnumerator Answer(Controller foe)
        {
            busy = true;
            if (beam == null) beam = new Beam("ElderBeam", like, BHalo, BBody, BCore);
            CastPose();
            for (float c = 0f; c < 0.2f && Alive() && foe != null; c += Time.deltaTime)
            {
                Hold(); Face((Vector2)foe.transform.position - (Vector2)transform.position, 900f * Time.deltaTime);
                yield return null;
            }
            Sparks.Burst(Hand, BBody, 10, 2.5f, 0.3f, like);
            beam.Show(true);
            if (Alive() && foe != null && Breath(foe) != 0) yield return Clash(foe);
            nextAnswer = Time.time + 2.5f;
            yield return End();
        }

        bool Alive() => mob != null && mob.alive && gameObject.activeInHierarchy;

        // the plain beam's lights: one at his hands, a blazing one where it strikes
        SpriteRenderer handFl, tipFl, tipCore;
        void Lights(bool on)
        {
            if (on)
            {
                if (handFl == null) handFl = Glow.Flare(null, like, 44);
                if (tipFl == null) tipFl = Glow.Flare(null, like, 44);
                if (tipCore == null) tipCore = Glow.Flare(null, like, 45);
                handFl.color = BBody; tipFl.color = BBody; tipCore.color = new Color(1f, 1f, 1f, 0.85f);
                return;
            }
            if (handFl != null) Destroy(handFl.gameObject);
            if (tipFl != null) Destroy(tipFl.gameObject);
            if (tipCore != null) Destroy(tipCore.gameObject);
            handFl = tipFl = tipCore = null;
        }

        void Shine(Vector2 a, Vector2 tip, float grow, float t)
        {
            float pulse = 1f + 0.18f * Mathf.Sin(Time.time * 23f) + 0.1f * Mathf.Sin(Time.time * 37f);
            if (handFl != null) { handFl.transform.position = a; handFl.transform.localScale = Vector3.one * 0.5f * Width * grow * pulse; }
            if (tipFl != null) { tipFl.transform.position = tip; tipFl.transform.localScale = Vector3.one * 0.62f * Width * grow * pulse; }
            if (tipCore != null) { tipCore.transform.position = tip; tipCore.transform.localScale = Vector3.one * 0.26f * Width * grow * pulse; }
        }

        IEnumerator End()
        {
            Lights(false);
            EndPose();
            beam?.Show(false); theirs?.Show(false);
            if (ai != null && mob != null && mob.alive) ai._enableAI = true;
            next = Time.time + Random.Range(8f, 11f);
            busy = false;
            yield break;
        }

        public static int Hits;
        public static int Clashes, LastOutcome; public bool InClash;                                 // (the test counts the burns)
        readonly System.Collections.Generic.HashSet<Creature> burnt = new System.Collections.Generic.HashSet<Creature>();

        void Burn(Vector2 a, Vector2 dir, float len)
        {
            float dmg = 0f;
            try { dmg = (float)mob.Damage * 0.35f; } catch { dmg = 5f; }
            burnt.Clear();
            foreach (var h in Physics2D.CircleCastAll(a, 0.3f * Width, dir, len))
            {
                var c = h.collider != null ? h.collider.GetComponentInParent<Creature>() : null;
                if (c == null || c == mob || !c.alive || !burnt.Add(c) || (c is Mob mb && Prisoner.Caged.Contains(mb))) continue;
                bool monster = !(c is Character);
                float d = monster ? (float)mob.Damage * 0.7f + c.HP_max * 0.07f : dmg;
                try { c.SetDamage(d, 1f, false, ai); Hits++; } catch { }
                if (monster && c.ctrl != null)
                {
                    Vector2 cp = c.transform.position; float k = Mathf.Max(0f, Vector2.Dot(cp - a, dir));
                    try { c.ctrl.PushMeFromPoint(a + dir * k - Vector2.Perpendicular(dir) * 0.01f, 1.3f); } catch { }
                }
                Sparks.Burst(c.transform.position, BBody, 4, 2.4f, 0.26f, like, 0.35f);
                Sparks.Burst(c.transform.position, BCore, 2, 1.2f, 0.18f, like, 0.25f);
            }
        }

        /// a hero within reach who breathes his dark flame or cursed breath at the elder right now
        Controller Breather(Vector2 a, Vector2 e)
        {
            foreach (var c in Object.FindObjectsOfType<Controller>())
            {
                if (c == null || c is AIController || c._creature == null || !c._creature.alive || c._creature.faction == mob.faction) continue;
                if (Breath(c) == 0) continue;
                Vector2 p = c.transform.position, me = transform.position, to = me - p;
                string why = to.magnitude > Range + 4.5f ? "far" : Vector2.Angle(c.transform.right, to) > 60f ? "not facing" : !(Clear(p, me, c.transform) || Clear(Hand, p, c.transform) || Clear(p + (Vector2)c.transform.right * 0.4f, Hand, c.transform)) ? "wall " + blocker : null;
                if (why == null) return c;
                if (Time.time > nextWhy) { nextWhy = Time.time + 1f; SanctumPlugin.Log.LogInfo("Sanctum: a hero breathes near " + Who + " but no clash: " + why); }
            }
            return null;
        }
        float nextWhy;

        static readonly System.Collections.Generic.Dictionary<RuntimeAnimatorController, bool> wizParam = new System.Collections.Generic.Dictionary<RuntimeAnimatorController, bool>();
        static bool HasWiz(Animator an)
        {
            if (an == null || an.runtimeAnimatorController == null) return false;
            var k = an.runtimeAnimatorController;
            if (!wizParam.TryGetValue(k, out var has)) { has = false; foreach (var p in an.parameters) if (p.name == "WizSpecial" && p.type == AnimatorControllerParameterType.Bool) has = true; wizParam[k] = has; }
            return has;
        }

        /// 1 = the mage's dark flame, 2 = the necromancer's cursed breath, 0 = none
        static int Breath(Controller c)
        {
            try
            {
                if (c.darkFlame != null && c.darkFlame.activeInHierarchy) return 1;
                if (c.necroFire != null && c.necroFire.activeInHierarchy) return 2;
                // (the user breathed and nothing clashed) the hero's own animator holds "WizSpecial" all the while he
                // breathes - the mage's flame and the necromancer's breath both - whatever the flame object does
                var an = c.GetComponent<Animator>() ?? c.GetComponentInChildren<Animator>();
                if (HasWiz(an) && an.GetBool("WizSpecial")) return c.necroFire != null && c.darkFlame == null ? 2 : 1;
            }
            catch { }
            return 0;
        }

        IEnumerator Clash(Controller foe)
        {
            int kind = Breath(foe);
            theirs?.Destroy();
            theirs = kind == 2 ? new Beam("HeroBreath", like, NHalo, NBody, NCore) : new Beam("HeroBreath", like, FHalo, FBody, FCore);
            Color theirC = kind == 2 ? NBody : FBody;
            theirs.Show(true);
            // (the user: like the anime beam clash) a ball of raging power where the streams meet, waves out of it
            beam.EndScale = 0.35f; theirs.EndScale = 0.35f;
            var knot = Glow.Flare(null, like, 45); var knot2 = Glow.Flare(null, like, 46);
            float wave = 0f;
            Sparks.Ring(Vector2.Lerp(Hand, (Vector2)foe.transform.position, 0.5f), Color.white, 2.2f, like);
            Shake(0.25f, 0.25f);
            Gather(foe);
            float sear = 0f;
            SanctumPlugin.Log.LogInfo("Sanctum: " + Who + "'s beam clashes with a hero's " + (kind == 2 ? "cursed breath" : "dark flame"));
            float t = 0.5f, seed = Random.value * 100f, shake = 0f, let = 0f;
            int outcome = 0;                                                                   // 1 = the hero is hit, -1 = the elder is hit, 2 = a draw
            Clashes++; InClash = true; LastOutcome = 0;
            for (float T = 0f; ; T += Time.deltaTime)
            {
                if (!Alive() || foe == null || foe._creature == null || !foe._creature.alive) break;
                Hold();
                // (the user: it came out crooked) the two lock eyes: each turned square on the other, so the hero's own
                // flame and both beams lie on one straight line
                Vector2 to = (Vector2)foe.transform.position - (Vector2)transform.position;
                Face(to, 720f * Time.deltaTime);
                if (Breath(foe) != 0) { float ang = Mathf.Atan2(-to.y, -to.x) * Mathf.Rad2Deg; foe.transform.rotation = Quaternion.Euler(0f, 0f, ang); }
                Vector2 dirL = to.sqrMagnitude > 0.0001f ? to.normalized : (Vector2)transform.right;
                Vector2 me = Hand, pp = (Vector2)foe.transform.position - dirL * 0.45f;
                bool breathing = Breath(foe) != 0;
                bool aligned = Vector2.Angle(foe.transform.right, -to) < 70f && to.magnitude < Range + 6f && (Clear(transform.position, foe.transform.position, foe.transform) || Clear(Hand, foe.transform.position, foe.transform));
                if (breathing && !aligned) let += Time.deltaTime; else let = 0f;
                if (let > 0.6f) { outcome = 2; break; }                                        // he turned away or a wall came between: it breaks off
                // the meeting point is pushed back and forth: two waves and a wandering push, the hero a hair stronger;
                // when he lets go of his breath, the beam drives it into him
                float push = 0.24f * Mathf.Sin(T * 6.3f) + 0.15f * Mathf.Sin(T * 2.2f + seed) + (Mathf.PerlinNoise(T * 0.8f, seed) - 0.5f) * 0.5f - 0.03f;
                if (Breath(foe) == 0) push = 0.9f;
                float was = t;
                t = Mathf.Clamp01(t + push * Time.deltaTime);
                // (the user: "they go back and forth a little") whoever the point is pushed towards gives ground, the
                // other leans in after it; it turns back - so do they
                Vector2 way = to.sqrMagnitude > 0.001f ? to.normalized : (Vector2)transform.right;
                float dt = (t - was) * 1.6f;
                Slide(rb, way * dt);
                Slide(foe.GetComponent<Rigidbody2D>(), way * dt * 0.7f);
                Vector2 P = Vector2.Lerp(me, pp, t);
                float heat = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(T * 6.3f));
                beam.Set(me, P, 1.1f * Width, heat);
                if (Breath(foe) != 0) theirs.Set(pp, P, 1.1f * Width, heat); else theirs.Show(false);
                float pulse = 1f + 0.35f * Mathf.Sin(Time.time * 24f) + 0.2f * Mathf.Sin(Time.time * 41f);
                knot.transform.position = P; knot.transform.localScale = Vector3.one * (0.75f + heat * 0.35f) * pulse;
                knot.color = Color.Lerp(BBody, theirC, 0.5f + 0.5f * Mathf.Sin(Time.time * 9f));
                knot2.transform.position = P; knot2.transform.localScale = Vector3.one * (0.35f + heat * 0.15f) * pulse; knot2.color = new Color(1f, 1f, 1f, 0.8f);
                Watch(me, pp, P);
                sear -= Time.deltaTime;
                if (sear <= 0f) { sear = 0.25f; Sear(me, P, ai, foe); if (Breath(foe) != 0) Sear(pp, P, foe, foe); }
                wave -= Time.deltaTime;
                if (wave <= 0f) { wave = 0.45f; Sparks.Ring(P, Random.value < 0.5f ? BBody : theirC, 1.3f + heat, like); Sparks.Burst(P, Color.white, 4, 4f, 0.2f, like, 0.3f); }
                if (Random.value < 0.7f) Sparks.Burst(P, Random.value < 0.5f ? BBody : theirC, 2, 3.2f, 0.28f, like, 0.4f);
                shake -= Time.deltaTime;
                if (shake <= 0f) { shake = 0.25f; Shake(0.08f, 0.06f); }
                if (t >= 0.97f) { outcome = 1; break; }
                if (t <= 0.03f) { outcome = -1; break; }
                if (T > 7f) { outcome = 2; break; }
                yield return null;
            }
            InClash = false; LastOutcome = outcome;
            SanctumPlugin.Log.LogInfo("Sanctum: the beam duel ends: " + (outcome == 1 ? "the hero is hit" : outcome == -1 ? Who + " is hit" : "a draw"));
            theirs.Show(false);
            if (knot != null) Destroy(knot.gameObject);
            if (knot2 != null) Destroy(knot2.gameObject);
            beam.EndScale = 1f;
            Release();
            if (foe != null && foe._creature != null && Alive())
            {
                Vector2 me = transform.position, hp = foe.transform.position;
                if (outcome == 1)                                                              // the hero is hit
                {
                    Blast(hp, BBody);
                    try { foe._creature.SetDamage((float)mob.Damage * 3f, 1f, false, ai); } catch { }
                    try { foe.PushMeFromPoint(me, 2f); } catch { }
                }
                else if (outcome == -1)                                                        // the elder is hit and staggers
                {
                    Blast(me, theirC);
                    try { mob.SetDamage(mob.HP_max * 0.15f, 1f, false, foe); } catch { }
                    try { ai.PushMeFromPoint(hp, 2f); } catch { }
                    beam.Show(false);
                    for (float s = 0f; s < 1.5f && Alive(); s += Time.deltaTime) { Hold(); yield return null; }
                }
                else if (outcome == 2)                                                         // nobody wins: both are thrown back
                {
                    Vector2 P = Vector2.Lerp(me, hp, t);
                    Blast(P, Color.Lerp(BBody, theirC, 0.5f));
                    try { foe.PushMeFromPoint(P, 1.4f); } catch { }
                    try { ai.PushMeFromPoint(P, 1.4f); } catch { }
                }
            }
        }

        /// moves a body a little, unless a wall is in the way
        static void Slide(Rigidbody2D r, Vector2 d)
        {
            if (r == null || d.sqrMagnitude < 1e-8f) return;
            if (WallAt(r.position, d.normalized, d.magnitude + 0.35f) >= 0f) return;
            r.position += d;
        }

        void Blast(Vector2 at, Color c)
        {
            Sparks.Burst(at, c, 26, 5f, 0.4f, like, 0.6f);
            Sparks.Burst(at, Color.white, 8, 2f, 0.6f, like, 0.35f);
            Sparks.Ring(at, c, 1.8f, like);
            Shake(0.3f, 0.35f);
        }

        static void Shake(float t, float s) { try { CameraController.Instance?.ShakeAll(t, s); } catch { } }
    }
}
