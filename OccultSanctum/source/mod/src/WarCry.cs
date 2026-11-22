using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace OccultSanctum
{
    /// The general's war cry (the user): the very thing the game's zombie guardian does, with a cast one can see - in a
    /// fight, every 12 seconds, if he has friends within 6 tiles in sight, he stops, raises his mace high in both hands
    /// while a red glow gathers about him, then brings it down: a red ring runs out from him and every friend it reaches
    /// gets the guardian's buff (the game's "Altar_Rage", rage) for 10 seconds - a red glow flickers on each of them
    /// for as long as it lasts - and turns on his foe.
    public class WarCry : MonoBehaviour
    {
        const float Radius = 6f, Every = 12f, Lasts = 10f, Cast = 1.0f;
        static readonly Color Red = new Color(1f, 0.12f, 0.08f, 1f), Ember = new Color(1f, 0.35f, 0.12f, 1f);
        static Buff rage; static bool looked;
        Mob mob; AIController ai; Rigidbody2D rb; SpriteRenderer like;
        float next; bool casting;

        /// the zombie guardian's own buff made to last 10 seconds (a copy; the game's stays as it is); if the game has
        /// not loaded it here, the same buff made anew
        public static Buff Rage()
        {
            if (looked) return rage;
            looked = true;
            const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;
            try
            {
                var game = Resources.FindObjectsOfTypeAll<Buff>().FirstOrDefault(b => b != null && b.name == "Altar_Rage");
                if (game != null) rage = Object.Instantiate(game);
                else
                {
                    rage = ScriptableObject.CreateInstance<Buff>();
                    rage.buffType = BuffTypes.Rage;
                    typeof(Buff).GetField("_value", F)?.SetValue(rage, 20f);
                    var other = Resources.FindObjectsOfTypeAll<Buff>().FirstOrDefault(b => b != null && b != rage && b.buffType == BuffTypes.Rage);
                    if (other != null) typeof(Buff).GetField("_icon", F)?.SetValue(rage, typeof(Buff).GetField("_icon", F)?.GetValue(other));
                }
                rage.name = "Altar_Rage";
                typeof(Buff).GetField("_time", F)?.SetValue(rage, Lasts);
                typeof(Buff).GetField("_timeReal", F)?.SetValue(rage, Lasts);
                rage.hideFlags = HideFlags.HideAndDontSave;
            }
            catch (System.Exception e) { SanctumPlugin.Log.LogWarning("Sanctum: no rage buff: " + e.Message); rage = null; }
            SanctumPlugin.Log.LogInfo("Sanctum: the general's war cry gives " + (rage != null ? rage.name + " (" + rage.buffType + ", " + rage.time + " s)" : "nothing"));
            return rage;
        }

        void Start()
        {
            mob = GetComponent<Mob>(); ai = GetComponent<AIController>(); rb = GetComponent<Rigidbody2D>();
            foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name == "Body") { like = t.GetComponent<SpriteRenderer>(); break; }
            next = Time.time + 3f;
        }

        void Update()
        {
            if (casting || mob == null || ai == null || !mob.alive || Time.time < next) return;
            var t = ai._target;
            if (t == null || !t.gameObject.activeInHierarchy) return;
            next = Time.time + 1f;
            if (Rage() == null || Friends().Length == 0) return;
            next = Time.time + Every;
            StartCoroutine(Cry(t));
        }

        Creature[] Friends() => Physics2D.OverlapCircleAll(transform.position, Radius)
            .Select(c => c != null ? c.GetComponent<Creature>() : null)
            .Where(c => c != null && c != mob && c.alive && c.faction == mob.faction && !Prisoner.Caged.Contains(c) && c.gameObject.activeInHierarchy
                        && !Physics2D.Linecast(transform.position, c.transform.position, ElderBeam.WallMask))
            .Distinct().ToArray();

        /// the name of the clip the animator plays now (or is going into)
        static string Clip(Animator an)
        {
            try
            {
                var n = an.GetNextAnimatorClipInfo(0); if (n != null && n.Length > 0 && n[0].clip != null) return n[0].clip.name;
                var c = an.GetCurrentAnimatorClipInfo(0); if (c != null && c.Length > 0 && c[0].clip != null) return c[0].clip.name;
            }
            catch { }
            return "";
        }

        static float Norm(Animator an)
        {
            try { return an.IsInTransition(0) ? an.GetNextAnimatorStateInfo(0).normalizedTime : an.GetCurrentAnimatorStateInfo(0).normalizedTime; } catch { return 1f; }
        }

        /// the cry, by the game's own move: his two-handed power blow (the mace swung up over his head in both hands
        /// and brought down), held at the top while the red glow gathers, then brought down - the ring goes out
        /// as the mace strikes the floor
        IEnumerator Cry(Transform foe)
        {
            casting = true;
            ai._enableAI = false;
            var an = GetComponent<Animator>();
            float speedWas = an != null ? an.speed : 1f;
            if (rb != null) rb.velocity = Vector2.zero;
            if (foe != null) { Vector2 d = foe.position - transform.position; transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg); }
            bool anim = false;
            if (an != null)
            {
                an.speed = 1f;
                try { Ritual.MoveAnim(mob, false); } catch { }
                try { an.ResetTrigger("Attack"); an.SetTrigger("AttackStr"); } catch { }
                for (float w = 0f; w < 0.3f && !anim; w += Time.deltaTime) { yield return null; anim = Clip(an).IndexOf("attackPower", System.StringComparison.OrdinalIgnoreCase) >= 0; }
            }
            var glow = Glow.Flare(null, like, 2); glow.color = new Color(Red.r, Red.g, Red.b, 0.5f);
            // up goes the mace; at the top it is held while the glow gathers, embers rise into it
            bool held = false;
            for (float c = 0f; c < Cast; c += Time.deltaTime)
            {
                if (mob == null || !mob.alive) break;
                if (rb != null) rb.velocity = Vector2.zero;
                if (anim && !held && Norm(an) >= 0.38f) { held = true; an.speed = 0.02f; }
                glow.transform.position = transform.position;
                glow.transform.localScale = Vector3.one * (1.2f + 1.8f * c / Cast + 0.15f * Mathf.Sin(Time.time * 25f)) * transform.lossyScale.x;
                if (Random.value < 0.5f) Sparks.Burst((Vector2)transform.position + Random.insideUnitCircle.normalized * Random.Range(1.2f, 2f), Ember, 1, -2.2f, 0.25f, like, 0.45f);
                yield return null;
            }
            // down it comes: the cry goes out as the mace strikes
            if (an != null) an.speed = 1.3f;
            if (anim) for (float w = 0f; w < 0.6f && Norm(an) < 0.62f; w += Time.deltaTime) { if (rb != null) rb.velocity = Vector2.zero; yield return null; }
            if (an != null) an.speed = speedWas > 0.05f ? speedWas : 1f;
            if (glow != null) Destroy(glow.gameObject);
            if (mob != null && mob.alive)
            {
                Sparks.Ring(transform.position, Red, Radius * 0.55f, like);
                Sparks.Burst(transform.position, Ember, 18, 3.5f, 0.45f, like, 0.55f);
                try { CameraController.Instance?.ShakeAll(0.25f, 0.22f); } catch { }
                var friends = Friends();
                foreach (var f in friends)
                {
                    try { f.AddEffect(Rage()); } catch { }
                    var fa = f.GetComponent<AIController>();
                    if (fa != null && foe != null) fa._target = foe;
                    RageGlow.On(f, Lasts, like);
                }
                SanctumPlugin.Log.LogInfo("Sanctum: the general's war cry (" + (anim ? "power blow" : "no clip") + "): rage on " + friends.Length + " friends for " + Lasts + " s");
                for (float w = 0f; w < 0.35f; w += Time.deltaTime) yield return null;   // the follow-through of the blow
                ai._enableAI = true;
            }
            casting = false;
        }

        void OnDisable()
        {
            var an = GetComponent<Animator>();
            if (casting && an != null) an.speed = 1f;
            casting = false;
            if (ai != null && mob != null && mob.alive) ai._enableAI = true;
        }
    }

    /// the red flicker on one in the general's rage, for as long as it lasts
    public class RageGlow : MonoBehaviour
    {
        SpriteRenderer sr; float until;

        public static void On(Creature c, float secs, SpriteRenderer like)
        {
            var g = c.GetComponent<RageGlow>() ?? c.gameObject.AddComponent<RageGlow>();
            if (g.sr == null) { g.sr = Glow.Flare(c.transform, like, 1); g.sr.transform.localPosition = Vector3.zero; }
            g.until = Time.time + secs; g.sr.gameObject.SetActive(true);
        }

        void Update()
        {
            if (sr == null) return;
            var cr = GetComponent<Creature>();
            if (Time.time > until || cr == null || !cr.alive) { sr.gameObject.SetActive(false); return; }
            float left = until - Time.time, fade = Mathf.Clamp01(left / 0.8f);
            sr.color = new Color(1f, 0.1f, 0.06f, (0.35f + 0.2f * Mathf.Sin(Time.time * 9f)) * fade);
            sr.transform.localScale = Vector3.one * (1.5f + 0.12f * Mathf.Sin(Time.time * 7f)) / Mathf.Max(0.2f, transform.lossyScale.x);
            if (Random.value < 4f * Time.deltaTime) Sparks.Burst((Vector2)transform.position + Random.insideUnitCircle * 0.35f, new Color(1f, 0.3f, 0.1f, 1f), 1, 0.8f, 0.16f, sr, 0.5f);
        }
    }
}
