using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace FlagellantMod
{
    /// The Flagellant's attacks are his own: the game plays the animation, spends stamina and handles charging;
    /// the hit zones, effects and damage are done here, damage counted by the game's own formula from the skill parts.
    internal static class FLogic
    {
        public static bool Internal;

        public static readonly Color BloodCol = new Color(0.86f, 0.06f, 0.05f, 0.95f);
        public static readonly Color DarkBlood = new Color(0.5f, 0.03f, 0.03f, 0.95f);
        public static readonly Color FireCol = new Color(1f, 0.62f, 0.16f, 0.95f);
        public static readonly Color HolyCol = new Color(1f, 0.9f, 0.5f, 0.95f);
        public static readonly Color PoisonCol = new Color(0.45f, 0.85f, 0.3f, 0.95f);

        public static Character Hero(object src) =>
            Flag.Active && src is PlayerController pc && pc._creature is Character ch && ch.charClass == CharacterClasses.BERSERKER ? ch : null;

        public static Character HeroSelf(Creature c) =>
            Flag.Active && c is Character ch && ch.charClass == CharacterClasses.BERSERKER && ch.ctrl is PlayerController ? ch : null;

        public static void Popup(Vector3 at, string text, Color col)
        {
            try { PopupTextManager.Instance.CreatePopupText_WorldPosition(at, text, col); } catch { }
        }

        public static int L(int lvl) => Mathf.Clamp(lvl, 1, 50);

        public static float Weapon(Character ch)
        {
            try { return Mathf.Max(1f, (float)ch.DamageFull); } catch { return 10f; }
        }

        public static float MissingPct(Character ch)
        {
            try { return Mathf.Clamp01(1f - ch.HP_cur / Mathf.Max(1f, ch.HP_max)) * 100f; } catch { return 0f; }
        }

        /// pays health for a skill; never kills the hero
        public static float Pay(Character ch, float amount)
        {
            try
            {
                amount = Mathf.Min(amount, ch.HP_cur - 1f);
                if (amount <= 0f) return 0f;
                ch.HP_cur -= amount;
                Popup(ch.transform.position + Vector3.up * 0.5f, "-" + Mathf.CeilToInt(amount), BloodCol);
                return amount;
            }
            catch { return 0f; }
        }

        public static void Hit(Creature t, DamageStruct d, Controller src)
        {
            if (t == null || !t.alive) return;
            Internal = true;
            try { t.SetDamage(d, 1f, false, src); }
            catch (Exception e) { FlagellantPlugin.Log.LogWarning("Hit: " + e.Message); }
            finally { Internal = false; }
        }

        public static Debuff Copy(Debuff d, float value, float time)
        {
            if (d == null) return null;
            var n = UnityEngine.Object.Instantiate(d);
            n.value = value; n.time = time;
            try { n.timeReal = time; } catch { }
            return n;
        }

        public static void Stun(Character ch, Creature t, float time)
        {
            if (t == null || !t.alive) return;
            try
            {
                var b = ch.stuning != null ? ch.stuning : FlagSkills.D("Stuning");
                var s = Copy(b, b != null ? b.value : 1f, time);
                if (s != null) t.AddEffect(s);
            }
            catch { }
        }

        public static bool IsEnemy(Character ch, Creature cr) =>
            cr != null && !(cr is Character) && cr.alive && cr.gameObject.activeInHierarchy &&
            cr.faction != ch.faction && cr.faction != c.Factions.Human && cr.faction != c.Factions.Environment;

        static int Mask(Character ch)
        {
            try { int m = ch.ctrl.targets; if (m != 0) return m; } catch { }
            return Physics2D.DefaultRaycastLayers;
        }

        public static float BodyRadius(Creature cr)
        {
            try { if (cr != null && cr.coll != null) return cr.coll.radius * Mathf.Abs(cr.transform.lossyScale.x); } catch { }
            return 0.3f;
        }

        /// enemies whose BODY is within r of a point (the collider must be the creature's own)
        public static List<Creature> Enemies(Character ch, Vector2 at, float r)
        {
            var res = new List<Creature>();
            foreach (var col in Physics2D.OverlapCircleAll(at, r, Mask(ch)))
            {
                if (col == null || !col.gameObject.activeInHierarchy || !col.TryGetComponent<Creature>(out var cr)) continue;
                if (!IsEnemy(ch, cr) || res.Contains(cr)) continue;
                if (((Vector2)cr.transform.position - at).magnitude > r + BodyRadius(cr) + 0.1f) continue;
                res.Add(cr);
            }
            return res;
        }

        static void HitContainers(Character ch, Controller src, Vector2 at, float r)
        {
            foreach (var col in Physics2D.OverlapCircleAll(at, r, Mask(ch)))
                if (col != null && col.TryGetComponent<Container>(out var box))
                    try { if (!src.ObstacleToTarget(col.transform.position, checkDestroyable: false)) box.SetDamage(ch.attackDamageContainers); } catch { }
        }

        public static float Facing(Character ch) => Mathf.Atan2(ch.transform.right.y, ch.transform.right.x) * Mathf.Rad2Deg;

        static float RangePct(SkillLvl sl, int lvl)
        {
            var inj = sl?.damageInjectors?.FirstOrDefault(i => i != null && i.injectType == SkillDamageInjectContainer.InjectTypes.MultiplyRange && i.CheckLvl(lvl));
            return inj != null ? inj.GetResult(lvl) : 100f;
        }

        static float RangeMod(Character ch) => ch.rangeMod + ch.art_rangeMod * 0.01f;

        /// the circle of the game's melee damage zone (rangeMod 1.2, attackDistanceMod 0.5, offsetWithRange)
        public struct Zone { public Vector2 center; public float radius, reach; }

        static Zone GameZone(Character ch, float pct)
        {
            float num = 1.2f * pct * 0.01f * RangeMod(ch);
            float collR = 0.3f;
            try { if (ch.coll != null) collR = ch.coll.radius; } catch { }
            float off = (collR / 2f + ch.attackDistance * 0.5f) * (1f + num * 0.5f);
            var z = new Zone { center = (Vector2)ch.transform.position + (Vector2)ch.transform.right * off, radius = Mathf.Max(0.3f, ch.attackRange * num) };
            z.reach = Mathf.Max(0.8f, off + z.radius);
            return z;
        }

        /// an area around the hero: "area 500%" = 5 times the melee radius
        static float AreaRadius(Character ch, float pct) => Mathf.Max(1f, ch.attackRange * 1.2f * pct * 0.01f * RangeMod(ch));

        static List<Creature> InZone(Character ch, Controller src, Zone z)
        {
            Vector2 me = ch.transform.position;
            var res = new List<Creature>();
            foreach (var cr in Enemies(ch, z.center, z.radius))
            {
                try { if (src.ObstacleToTarget(cr.transform.position)) continue; } catch { }
                res.Add(cr);
            }
            HitContainers(ch, src, z.center, z.radius);
            return res.OrderBy(t => ((Vector2)t.transform.position - me).sqrMagnitude).ToList();
        }

        /// everyone the lash reaches: in front of him (a 150 degree sector) as far as the whip is long, in sight
        static List<Creature> WhipTargets(Character ch, Controller src, float len)
        {
            Vector2 me = ch.transform.position, fwd = ch.transform.right;
            var res = new List<Creature>();
            foreach (var cr in Enemies(ch, me, len + 0.2f))
            {
                Vector2 d = (Vector2)cr.transform.position - me;
                if (d.sqrMagnitude > 0.04f && Vector2.Angle(fwd, d) > 75f) continue;
                try { if (src.ObstacleToTarget(cr.transform.position)) continue; } catch { }
                res.Add(cr);
            }
            HitContainers(ch, src, me + fwd * (len * 0.5f), len * 0.5f);
            return res.OrderBy(t => ((Vector2)t.transform.position - me).sqrMagnitude).ToList();
        }

        /// the Flagellant's own damage multipliers: Religious Ecstasy and Blood Cleansing
        public static float SelfMult(Character ch) => FlagRuntime.DamageMult(ch);

        /// one hit with the game's own damage maths for this skill level
        static bool Strike(Character ch, Controller src, SkillLvl sl, int lvl, DamageStruct baseDmg, Creature t, float mult, float critAdd = 0f)
        {
            if (t == null || !t.alive || t.Immortal) return false;
            try { if (src.CheckAttackMiss(t)) return false; } catch { }
            var pre = new DamageStruct(baseDmg);
            float ignore = 0f, crit = 1f; bool power = false;
            if (sl.damageInjectors != null)
                foreach (var inj in sl.damageInjectors)
                    if (Works(inj)) SE_DamageZone.ActivateInjector_BeforeDamage(inj, null, src, lvl, ref pre, ref ignore, ref crit, ref power);
            pre.AddDamageType(sl.mainDamageType, pre.GetDamageType(c.DamageTypes._none));
            pre.Multiply(c.DamageTypes._none, 0f);
            try { ch.UpdateDamage(ref pre); } catch { }
            var dmg = new DamageStruct(pre);
            if (sl.damageInjectors != null)
                foreach (var inj in sl.damageInjectors)
                    if (Works(inj)) SE_DamageZone.ActivateInjector_BeforeDamage(inj, t, src, lvl, ref dmg, ref ignore, ref crit, ref power);
            mult *= SelfMult(ch);                                   // (Inquisition is added for every blow in InquisitionPatch)
            if (Math.Abs(mult - 1f) > 0.001f) dmg = dmg * mult;
            List<ItemEffectWeapon> effects = null;
            try
            {
                if (ch.Weapon != null)
                {
                    effects = new List<ItemEffectWeapon>(ch.Weapon.effects);
                    if (ch.ShieldWeapon != null) effects.AddRange(ch.ShieldWeapon.effects);
                }
            }
            catch { }
            bool was = t.alive;
            Internal = true;
            try { t.SetDamage(dmg, crit + ch.CriticalChance + critAdd, false, src, power, false, null, effects, ignore); }
            catch (Exception e) { FlagellantPlugin.Log.LogWarning("Strike: " + e.Message); }
            finally { Internal = false; }
            if (t != null && t.alive && sl.damageInjectors != null)
                foreach (var inj in sl.damageInjectors)
                    if (Works(inj)) try { SE_DamageZone.ActivateInjector_AfterDamage(inj, t, src, lvl, ref dmg, ref ignore); } catch { }
            return was && (t == null || !t.alive);
        }

        /// the skill's working parts (display-only lines under the description are skipped)
        static bool Works(SkillDamageInjectContainer inj) =>
            inj != null && inj.injectType != SkillDamageInjectContainer.InjectTypes.NONE && inj.mainType == SkillDamageInjectContainer.InjectGlobalType.Standart;

        // ------------------------------------------------------------------ effects
        public static SpriteRenderer Fx(string art, Vector2 pos, float rot, float scale, Color col, float life, Character hero, int order = 30,
            float grow = 1.08f, Transform follow = null, Vector2 drift = default)
        {
            var sr = Art.MakeSprite(art, pos, scale, hero, order);
            if (sr == null) return null;
            sr.color = col;
            sr.transform.rotation = Quaternion.Euler(0f, 0f, rot);
            var f = sr.gameObject.AddComponent<FxLife>();
            f.life = life; f.grow = grow; f.follow = follow; f.vel = drift;
            return sr;
        }

        public static void Shake(float t, float s) { try { CameraController.Instance?.ShakeAll(t, s); } catch { } }

        public static void Splash(Character ch, Vector2 at, int n, float speed, float size = 1.2f)
        {
            for (int i = 0; i < n; i++)
            {
                var v = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(speed * 0.4f, speed);
                Fx("blood_drop", at, 0f, size * UnityEngine.Random.Range(0.7f, 1.2f), Color.white, UnityEngine.Random.Range(0.25f, 0.5f), ch, 33, 0.6f, null, v);
            }
        }

        static void Sound(AudioClip clip, Vector2 at) { try { if (clip != null) SFXController.Instance?.PlaySFX(clip, at); } catch { } }

        // ------------------------------------------------------------------ dispatch
        public static void Attack(string id, bool charged, SkillLvl sl, DamageStruct dmg, Controller src, int lvl)
        {
            var ch = Hero(src);
            if (ch == null) return;
            var rt = FlagRuntime.Instance;
            switch (id)
            {
                case "bloodStrike": BloodStrike(ch, src, sl, dmg, lvl); break;
                case "poisonLash": if (rt != null) rt.StartCoroutine(PoisonLash(ch, src, sl, dmg, lvl)); break;
                case "sinnerStrike": if (rt != null) rt.StartCoroutine(SinnerStrike(ch, src, sl, dmg, lvl)); break;
                case "bloodRain": BloodRain(ch, src, sl, dmg, lvl); break;
                case "martyrHand": if (charged) MartyrHand(ch, src, sl, dmg, lvl); break;
                case "penanceHammer": if (rt != null) rt.StartCoroutine(PenanceHammer(ch, src, sl, dmg, lvl)); break;
                case "divinePunishment": if (charged) DivinePunishment(ch, src, sl, dmg, lvl); break;
                case "torment": if (charged && rt != null) rt.StartCoroutine(Torment(ch, src, sl, dmg, lvl)); break;
                case "suffering": Ecstasy(ch, lvl); break;
                case "quickSnap": if (rt != null) rt.StartCoroutine(QuickSnap(ch, src, sl, dmg, lvl)); break;
                case "bloodCleansing": if (charged) Cleansing(ch, lvl); break;
            }
        }

        // ------------------------------------------------------------------ main
        static void BloodStrike(Character ch, Controller src, SkillLvl sl, DamageStruct dmg, int lvl)
        {
            var z = GameZone(ch, RangePct(sl, lvl));
            float rot = Facing(ch);
            Vector2 me = ch.transform.position;
            Fx("arc_main", me, rot, z.reach / 1.29f, BloodCol, 0.24f, ch, 30, 1.08f, ch.transform);
            Fx("arc_main", me, rot - 6f, z.reach / 1.29f * 0.8f, DarkBlood, 0.2f, ch, 29, 1.04f, ch.transform);
            for (int i = 0; i < 5; i++)
            {
                Vector2 p = me + (Vector2)(Quaternion.Euler(0, 0, rot + UnityEngine.Random.Range(-50f, 50f)) * Vector3.right) * UnityEngine.Random.Range(0.6f, z.reach);
                Fx("blood_drop", p, 0f, 1.2f, Color.white, UnityEngine.Random.Range(0.3f, 0.5f), ch, 32, 0.7f, null, UnityEngine.Random.insideUnitCircle * 0.8f);
            }
            foreach (var t in InZone(ch, src, z))
            {
                Vector2 tp = t.transform.position;
                Strike(ch, src, sl, lvl, dmg, t, 1f);
                Fx("blood_splat", tp, UnityEngine.Random.Range(0f, 360f), 0.7f, Color.white, 0.35f, ch, 44, 1.3f);
            }
        }

        static IEnumerator PoisonLash(Character ch, Controller src, SkillLvl sl, DamageStruct dmg, int lvl)
        {
            int l = L(lvl);
            GameFx.Slash(src, RangePct(sl, lvl));                                 // the game's own weapon trail
            // ONE whip comes out of his hand and lashes there and back; each blow lands exactly when its tip cracks
            float reach0 = GameZone(ch, RangePct(sl, lvl)).reach;
            var snaps = FV.LashWhip(ch, reach0);
            float whip = FV.WhipLength(ch, reach0);                               // the hits go as far as the lash reaches
            float waited = 0f;
            for (int k = 0; k < 2 && ch != null && ch.alive; k++)
            {
                float at = k == 0 ? snaps.x : snaps.y;
                if (at > waited) { yield return new WaitForSeconds(at - waited); waited = at; }
                if (ch == null || !ch.alive) yield break;
                foreach (var t in WhipTargets(ch, src, whip))
                {
                    bool undead = t is Mob mu && mu.mobClass == Mob.MobClassess.undead;
                    Strike(ch, src, sl, lvl, dmg, t, l >= 25 && undead ? 1.4f : 1f);
                }
            }
        }
        /// three blows: each knocks the target far back and carries the hero to the next target
        static IEnumerator SinnerStrike(Character ch, Controller src, SkillLvl sl, DamageStruct dmg, int lvl)
        {
            int l = L(lvl);
            var done = new HashSet<Creature>();
            var rb = ch.GetComponent<Rigidbody2D>();
            int mask = 0;
            try { mask = src.obstaclesLayer; } catch { }
            for (int k = 0; k < 3 && ch != null && ch.alive; k++)
            {
                Vector2 me = ch.transform.position;
                Creature target;
                if (k == 0)
                {
                    var z = GameZone(ch, RangePct(sl, lvl));
                    GameFx.Slash(src, RangePct(sl, lvl));
                    target = InZone(ch, src, z).FirstOrDefault();
                    if (target == null) yield break;
                }
                else
                {
                    // the next sinner: the nearest enemy not hit yet, within 4.5 units and in sight
                    target = Enemies(ch, me, 4.5f).Where(t => !done.Contains(t)).OrderBy(t => ((Vector2)t.transform.position - me).sqrMagnitude)
                        .FirstOrDefault(t => { try { return !src.ObstacleToTarget(t.transform.position); } catch { return true; } });
                    if (target == null) yield break;
                    Vector2 tp0 = target.transform.position;
                    Vector2 to = tp0 + (me - tp0).normalized * (BodyRadius(target) + 0.45f);
                    if (Physics2D.OverlapCircle(to, 0.25f, mask) == null)
                    {
                        // he vanishes in a dark beam and lands in dust next to the next sinner; red lightning runs along the way
                        FV.SinnerVanish(me);
                        FV.SinnerStreak(me, to, k);                                // the blood streak along his way
                        if (rb != null) { rb.position = to; rb.velocity = Vector2.zero; }
                        ch.transform.position = new Vector3(to.x, to.y, ch.transform.position.z);
                        FV.SinnerAppear(to);
                        me = to;
                    }
                }
                done.Add(target);
                {
                    // one, two, three: a sharp blood streak cut through the sinner, alternating sides (there and back)
                    Vector2 tq = target.transform.position, fwd = (tq - me).sqrMagnitude > 0.0001f ? (tq - me).normalized : (Vector2)ch.transform.right;
                    Vector2 side = new Vector2(-fwd.y, fwd.x) * (k % 2 == 0 ? 0.45f : -0.45f);
                    FV.SinnerStreak(tq - fwd * 0.55f + side, tq + fwd * 0.55f - side, k);
                }
                Vector2 tp = target.transform.position;
                float rot = Mathf.Atan2(tp.y - me.y, tp.x - me.x) * Mathf.Rad2Deg;
                float crit = l >= 25 && target is Mob mb && mb.Armor >= 8 ? 15f : 0f;
                Strike(ch, src, sl, lvl, dmg, target, 1f, crit);
                FV.SinnerHit(me, tp, k == 2);
                Shake(0.1f, 0.12f);
                try { if (target != null && target.alive) target.ctrl?.PushMeFromPoint(me, 10f); } catch { }
                yield return new WaitForSeconds(0.17f);
            }
        }

        /// Hammer of Penance: the spiked ball goes round over his head and comes down on the ground in front of him;
        /// everyone in the circle of the blow is hit (armour partly ignored by the skill part), a chance to stun
        static IEnumerator PenanceHammer(Character ch, Controller src, SkillLvl sl, DamageStruct dmg, int lvl)
        {
            int l = L(lvl);
            float stunChance = l >= 25 ? 0.15f : l >= 12 ? 0.07f : 0f;
            yield return FV.HammerSwing(ch, GameZone(ch, RangePct(sl, lvl)).center);
            if (ch == null || !ch.alive) yield break;
            var z = GameZone(ch, RangePct(sl, lvl));                             // where he faces when the ball lands
            FV.HammerImpact(ch, z.center, z.radius);
            Shake(0.16f, 0.2f);
            foreach (var t in InZone(ch, src, z))
            {
                Vector2 tp = t.transform.position;
                Strike(ch, src, sl, lvl, dmg, t, 1f);
                if (t != null && t.alive && UnityEngine.Random.value < stunChance) Stun(ch, t, 1.2f);
                FV.HammerHit(tp);
            }
        }

        // ------------------------------------------------------------------ special
        static void BloodRain(Character ch, Controller src, SkillLvl sl, DamageStruct dmg, int lvl)
        {
            Pay(ch, ch.HP_max * 0.05f);
            int l = L(lvl);
            Vector2 me = ch.transform.position;
            var go = new GameObject("Flagellant_BloodRain");
            go.transform.position = me + (Vector2)ch.transform.right * 0.5f;
            var b = go.AddComponent<BloodBolt>();
            b.ch = ch; b.src = src; b.sl = sl; b.lvl = lvl; b.baseDmg = new DamageStruct(dmg);
            b.maxTargets = 1 + (l >= 25 ? 3 : l >= 12 ? 2 : 1);
            b.pierceBonus = l >= 25 ? 0.3f : 0f;
            b.dir = ch.transform.right;
            Splash(ch, me, 4, 1.2f, 1f);
        }

        /// the bolt: homes in on the nearest enemy, pierces, flies fast; it may pierce the same enemy again:
        /// after passing through it flies on a moment, turns back and strikes again (every pierce counts)
        internal class BloodBolt : MonoBehaviour
        {
            public Character ch; public Controller src; public SkillLvl sl; public int lvl; public DamageStruct baseDmg;
            public int maxTargets = 2; public float pierceBonus; public Vector2 dir;
            const float Again = 0.3f;                                   // a pierced enemy can be pierced again after this
            readonly Dictionary<Creature, float> lastHit = new Dictionary<Creature, float>();
            int hits;
            SpriteRenderer sr; float life = 3.5f, trail; int mask;
            bool Ready(Creature t) => !lastHit.TryGetValue(t, out var at) || Time.time - at >= Again;

            void Start()
            {
                sr = Art.MakeSprite("blood_bolt", transform.position, 1.3f, ch, 40);
                if (sr != null) sr.transform.SetParent(transform, true);
                try { mask = src.obstaclesLayer; } catch { }
            }

            void Update()
            {
                float dt = Time.deltaTime;
                life -= dt;
                if (life <= 0f || ch == null) { Pop(); return; }
                Vector2 p = transform.position;
                var target = Enemies(ch, p, 7f).Where(Ready).OrderBy(t => ((Vector2)t.transform.position - p).sqrMagnitude).FirstOrDefault();
                if (target != null)
                {
                    Vector2 want = ((Vector2)target.transform.position - p).normalized;
                    float turn = Vector2.Dot(dir, want) < 0f ? 14f : 7f;    // a tight U-turn back to a target behind
                    dir = Vector3.RotateTowards(dir, want, turn * dt, 0f);
                }
                dir = dir.sqrMagnitude < 0.01f ? Vector2.right : dir.normalized;
                p += dir * 13f * dt;
                transform.position = p;
                transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                trail += dt;
                if (trail > 0.03f) { trail = 0f; Fx("blood_drop", p - dir * 0.3f, 0f, 0.9f, new Color(1f, 1f, 1f, 0.8f), 0.25f, ch, 38, 0.5f); }
                if (mask != 0 && Physics2D.OverlapCircle(p, 0.1f, mask) != null) { Pop(); return; }
                foreach (var t in Enemies(ch, p, 0.3f))
                {
                    if (!Ready(t)) continue;
                    lastHit[t] = Time.time;
                    hits++;
                    float mult = 1f + pierceBonus * (hits - 1);
                    Strike(ch, src, sl, lvl, baseDmg, t, mult);
                    Fx("blood_splat", t.transform.position, UnityEngine.Random.Range(0f, 360f), 0.6f, Color.white, 0.3f, ch, 44, 1.3f);
                    if (hits >= maxTargets) { Pop(); return; }
                }
            }

            void Pop()
            {
                if (ch != null) Splash(ch, transform.position, 5, 1.5f, 1f);
                Destroy(gameObject);
            }
        }

        /// a powerful blow in a triangle in front; the lower the health, the stronger
        static void MartyrHand(Character ch, Controller src, SkillLvl sl, DamageStruct dmg, int lvl)
        {
            int l = L(lvl);
            Vector2 me = ch.transform.position, fwd = ch.transform.right;
            float R = GameZone(ch, RangePct(sl, lvl)).reach * 1.15f;
            float rot = Facing(ch);
            Fx("wedge", me + fwd * (R * 0.5f), rot, R / 2.67f, FireCol, 0.3f, ch, 30, 1.05f);   // only the triangle, no swing
            Shake(0.12f, 0.12f);
            float per = FlagSkills.At(FlagSkills.T_Martyr, l);                 // 1% .. 2% per 1% missing health (table)
            float mult = 1f + MissingPct(ch) * per * 0.01f;
            if (mult > 1.3f) Popup(me + Vector2.up * 0.7f, "+" + Mathf.RoundToInt((mult - 1f) * 100f) + "%", FireCol);
            var targets = Enemies(ch, me, R + 0.5f).Where(t =>
            {
                Vector2 d = (Vector2)t.transform.position - me;
                float along = Vector2.Dot(d, fwd), side = Mathf.Abs(d.x * fwd.y - d.y * fwd.x);
                return along > -0.2f && along <= R + BodyRadius(t) && side <= along * 0.38f + BodyRadius(t) + 0.15f;
            }).ToList();
            HitContainers(ch, src, me + fwd * (R * 0.5f), R * 0.4f);
            foreach (var t in targets)
            {
                try { if (src.ObstacleToTarget(t.transform.position)) continue; } catch { }
                Vector2 tp = t.transform.position;
                bool low = ch.HP_cur < ch.HP_max * 0.3f;
                if (Strike(ch, src, sl, lvl, dmg, t, mult))
                {
                    if (l >= 12 && low) { try { ch.AddHP(l >= 25 ? 3f : 2f); } catch { } }
                    if (l >= 25) FlagRuntime.AddRegen(3f, 5f);
                }
                Fx("fist_burst", tp, 0f, 0.9f, FireCol, 0.25f, ch, 44, 1.5f);
                Splash(ch, tp, 3, 1.5f);
            }
        }

        /// the wrath of heaven on everyone around, paid with blood for each enemy
        static void DivinePunishment(Character ch, Controller src, SkillLvl sl, DamageStruct dmg, int lvl)
        {
            int l = L(lvl);
            Vector2 me = ch.transform.position;
            float R = AreaRadius(ch, RangePct(sl, lvl)) * 0.8f;                 // a little smaller (the user asked; hits too)
            float costPct = FlagSkills.At(FlagSkills.T_WrathCost, l);
            float stunChance = l >= 25 ? 0.1f : l >= 12 ? 0.05f : 0f;
            FV.Wrath(me, R);                                                    // the circles are back (the user), only smaller
            Shake(0.25f, 0.25f);
            Sound(PrefabsContainerManager.Instance?.sfx_explosion, me);
            int n = 0;
            foreach (var t in Enemies(ch, me, R).OrderBy(t => ((Vector2)t.transform.position - me).sqrMagnitude))
            {
                Vector2 tp = t.transform.position;
                FV.WrathHit(tp);
                Strike(ch, src, sl, lvl, dmg, t, 1f);
                if (t != null && t.alive && UnityEngine.Random.value < stunChance) Stun(ch, t, 1.2f);
                Pay(ch, ch.HP_max * costPct * 0.01f);
                n++;
            }

        }

        // ------------------------------------------------------------------ power
        /// blood of the enemies flows to him, then a great explosion
        static IEnumerator Torment(Character ch, Controller src, SkillLvl sl, DamageStruct dmg, int lvl)
        {
            Vector2 me = ch.transform.position;
            float boom = AreaRadius(ch, RangePct(sl, lvl));
            float pull = AreaRadius(ch, 750f);
            // every enemy in the pull radius gives one unit of blood: +3% damage and +3% explosion radius each
            var donors = Enemies(ch, me, pull);
            int blood = donors.Count;
            float gain = 1f + 0.03f * blood;
            foreach (var t in donors)
            {
                Vector2 tp = t.transform.position;
                for (int i = 0; i < 4; i++)
                {
                    Vector2 from = tp + UnityEngine.Random.insideUnitCircle * 0.3f;
                    float time = UnityEngine.Random.Range(0.3f, 0.4f);
                    Fx("blood_drop", from, 0f, 1.3f, Color.white, time, ch, 40, 0.8f, null, (me - from) / time);
                }
            }
            for (int i = 0; i < 10; i++)
            {
                Vector2 from = me + UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(0.6f, 1.2f);
                Fx("blood_drop", from, 0f, 1.2f, Color.white, 0.3f, ch, 40, 0.6f, null, (me - from) / 0.3f);
            }
            Fx("aura", me, 0f, 0.6f, BloodCol, 0.35f, ch, 35, 1.8f, ch.transform);
            yield return new WaitForSeconds(0.35f);
            if (ch == null || !ch.alive) yield break;
            me = ch.transform.position;
            boom *= gain;
            if (blood > 0) Popup(me + Vector2.up * 0.9f, "+" + (blood * 3) + "%", BloodCol);
            Fx("blood_splat", me, UnityEngine.Random.Range(0f, 360f), boom / 1.33f * 0.8f, Color.white, 0.6f, ch, 28, 1.3f);
            Fx("flame_burst", me, 0f, boom / 0.66f * 0.5f, new Color(1f, 0.8f, 0.6f, 0.95f), 0.4f, ch, 36, 1.6f);
            Fx("shockwave", me, 0f, 0.5f, BloodCol, 0.4f, ch, 33, boom * 1.3f);
            Splash(ch, me, 14, 4f, 1.4f);
            Shake(0.3f, 0.35f);
            Sound(PrefabsContainerManager.Instance?.sfx_explosion, me);
            HitContainers(ch, src, me, boom);
            foreach (var t in Enemies(ch, me, boom)) Strike(ch, src, sl, lvl, dmg, t, gain);
        }

        /// (the self-flogging whip, FV.SelfLash, was removed again at the user's request: the old look is back)
        static void Ecstasy(Character ch, int lvl)
        {
            FlagRuntime.StartEcstasy(ch, L(lvl), 15f);
            Vector2 me = ch.transform.position;
            // the FIRST look of the skill is back (the user): a spray of his blood and a crimson burst on him
            // (the game-style FV.Ecstasy is no longer used)
            Splash(ch, me, 16, 3f, 1.3f);
            Fx("fist_burst", me, 0f, 1.2f, BloodCol, 0.35f, ch, 34, 1.6f);
            Popup(me + Vector2.up * 0.8f, Texts.Get("FLG/Ecstasy"), BloodCol);
            Shake(0.1f, 0.1f);
        }

        // ------------------------------------------------------------------ Quick Snap
        public static int SnapLvl;
        static readonly List<float> snapStacks = new List<float>();             // when each stack runs out
        public const float SnapStackTime = 4f;
        /// the attack speed bonus of Quick Snap right now (per stack, at most 2 stacks)
        public static float SnapSpeedMult()
        {
            snapStacks.RemoveAll(u => u <= Time.time);
            if (snapStacks.Count == 0) return 1f;
            return 1f + FlagSkills.At(FlagSkills.T_SnapSpeed, SnapLvl) * 0.01f * Mathf.Min(2, snapStacks.Count);
        }
        static void AddSnapStack(int lvl)
        {
            snapStacks.RemoveAll(u => u <= Time.time);
            SnapLvl = lvl;
            if (snapStacks.Count >= 2) snapStacks.RemoveAt(0);                  // a new hit refreshes the oldest stack
            snapStacks.Add(Time.time + SnapStackTime);
        }

        /// the lash shoots out straight ahead, far; its tip strikes the first enemy on the line (at the full reach: a crit)
        static IEnumerator QuickSnap(Character ch, Controller src, SkillLvl sl, DamageStruct dmg, int lvl)
        {
            int l = L(lvl);
            Vector2 hand = ch.transform.position;
            try { if (ch.RightHandSprite != null) hand = ch.RightHandSprite.transform.position; } catch { }
            Vector2 fwd = ch.transform.right;
            float max = FV.WhipLength(ch, GameZone(ch, RangePct(sl, lvl)).reach) * 1.25f;
            // a wall stops the lash
            float reach = max;
            try
            {
                int wall = src.obstaclesLayer;
                if (wall != 0) { var hit = Physics2D.Raycast(hand, fwd, max, wall); if (hit.collider != null) reach = Mathf.Max(0.3f, hit.distance); }
            }
            catch { }
            // the first enemy on the line
            Creature target = null; float along = reach;
            foreach (var t in Enemies(ch, hand + fwd * (reach * 0.5f), reach * 0.5f + 0.6f))
            {
                Vector2 d = (Vector2)t.transform.position - hand;
                float a = Vector2.Dot(d, fwd), side = Mathf.Abs(d.x * fwd.y - d.y * fwd.x);
                if (a < 0f || a > reach + BodyRadius(t) || side > BodyRadius(t) + 0.22f) continue;
                if (a < along || target == null) { target = t; along = Mathf.Min(a, reach); }
            }
            float len = target != null ? Mathf.Max(0.3f, along - BodyRadius(target) * 0.5f) : reach;
            float arrive = FV.Snap(ch, len);
            if (arrive > 0f) yield return new WaitForSeconds(arrive);
            HitContainers(ch, src, hand + fwd * len, 0.35f);
            if (target == null || ch == null || !ch.alive || !target.alive) yield break;
            bool far = along >= max * 0.75f;                                  // struck at (nearly) the full reach
            Strike(ch, src, sl, lvl, dmg, target, 1f, far ? 1000f : 0f);
            AddSnapStack(l);
        }

        static void Cleansing(Character ch, int lvl)
        {
            int l = L(lvl);
            Pay(ch, ch.HP_max * 0.1f);
            int removed = 0;
            try
            {
                var types = ch.buffsList.OfType<Debuff>().Where(d => d != null).Select(d => d.debuffType).Distinct().ToList();
                foreach (var tp in types) { try { ch.RemoveEffect(tp); removed++; } catch { } }
                ch.BleedCur = 0f; ch.PoisonCur = 0f; ch.PertificationCur = 0f;
            }
            catch (Exception e) { FlagellantPlugin.Log.LogWarning("Cleansing: " + e.Message); }
            float resist = l >= 25 ? 100f : 4f * l;
            FlagRuntime.StartResist(ch, resist, 30f);
            if (l >= 25 && removed > 0) FlagRuntime.StartCleanseBonus(0.4f * removed, 15f);
            Vector2 me = ch.transform.position, fwd = ch.transform.right;
            FV.Cleansing(me, fwd, ch.transform);
            Popup(me + Vector2.up * 0.8f, Texts.Get("FLG/Cleansed") + (removed > 0 ? " x" + removed : ""), HolyCol);
        }
    }

    // ---------------------------------------------------------------------- patches
    [HarmonyPatch(typeof(SkillLvl), nameof(SkillLvl.Activate))]
    internal static class SkillActivatePatch
    {
        static bool Prefix(SkillLvl __instance, DamageStruct damage, Controller source, int lvl)
        {
            if (!Flag.Active || __instance == null || !FlagSkills.LvlToId.TryGetValue(__instance, out var info)) return true;
            if (FLogic.Hero(source) == null) return true;
            try { FLogic.Attack(info.id, info.charged, __instance, damage, source, lvl); }
            catch (Exception e) { FlagellantPlugin.Log.LogWarning("Skill " + info.id + ": " + e); }
            return false;
        }
    }

    /// The Berserker's attacks swap hands every blow (right, then left). The Cultist with one weapon (or none) strikes
    /// with his right hand only, like the Warrior; only with a weapon in each hand he swaps like the Berserker.
    [HarmonyPatch(typeof(Controller), nameof(Controller.ChangeRandomAnimation))]
    internal static class RightHandOnlyPatch
    {
        static bool Prefix(Controller __instance)
        {
            try
            {
                var ch = FLogic.Hero(__instance);
                if (ch == null || ch.ShieldWeapon != null) return true;
                var co = Traverse.Create(__instance).Field("tempRandomAttack").GetValue<Coroutine>();
                if (co != null) __instance.StopCoroutine(co);                     // a hand swap already under way is stopped
                __instance.anim?.SetFloat("randomAttack", 0f);
                Traverse.Create(__instance).Field("randPrev").SetValue(0);
                return false;
            }
            catch { return true; }
        }
    }

    /// Religious Ecstasy from lvl 12: less damage taken the more health is missing
    [HarmonyPatch(typeof(Creature), nameof(Creature.SetDamageRPC))]
    internal static class HeroDefensePatch
    {
        static void Prefix(Creature __instance, ref DamageStruct damageStruct)
        {
            try
            {
                var ch = FLogic.HeroSelf(__instance);
                if (ch == null || FLogic.Internal) return;
                float k = FlagRuntime.DamageTakenMult(ch);
                if (k < 0.999f) damageStruct = damageStruct * k;
            }
            catch { }
        }
    }

    /// a short-lived effect: grows, fades, may follow the hero or drift
    public class FxLife : MonoBehaviour
    {
        public float life = 0.3f, grow = 1f;
        public Transform follow;
        public Vector2 vel;
        float t; SpriteRenderer sr; Color c0; Vector3 s0; Vector3 offset;
        void Start()
        {
            sr = GetComponent<SpriteRenderer>(); c0 = sr != null ? sr.color : Color.white; s0 = transform.localScale;
            if (follow != null) offset = transform.position - follow.position;
        }
        void LateUpdate()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / life);
            float e = 1f - (1f - k) * (1f - k);
            transform.localScale = s0 * Mathf.Lerp(1f, grow, e);
            if (follow != null) transform.position = follow.position + offset;
            if (vel != Vector2.zero) transform.position += (Vector3)(vel * Time.deltaTime);
            if (sr != null) sr.color = new Color(c0.r, c0.g, c0.b, c0.a * (1f - k * k));
            if (t >= life) Destroy(gameObject);
        }
    }
}
