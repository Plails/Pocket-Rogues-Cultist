using UnityEngine;

namespace FlagellantMod
{
    /// What the Cultist's skills look like (all but Bloody Strike and Blood Rain): built from the game's own effects
    /// (GameFx) so they read like the original classes' skills. Positions only - the logic is in SkillLogic.
    public static class FV
    {
        public static readonly Color Poison = new Color(0.5f, 1f, 0.3f, 1f);
        public static readonly Color Blood = new Color(1f, 0.16f, 0.12f, 1f);
        public static readonly Color DarkBlood = new Color(0.65f, 0.04f, 0.06f, 1f);
        public static readonly Color Holy = new Color(1f, 0.86f, 0.45f, 1f);
        public static readonly Color Ember = new Color(1f, 0.62f, 0.2f, 1f);

        static Vector2 Dir(float rot) => new Vector2(Mathf.Cos(rot * Mathf.Deg2Rad), Mathf.Sin(rot * Mathf.Deg2Rad));

        // ---------------- Poison Lash: the lash itself (no clouds or extra particles - the game shows the poison on its own)
        public static void LashSwing(Vector2 me, float rot, float reach, int k, Transform follow = null) { }
        public static void LashHit(Vector2 tp) { }

        /// which lash he swings: the look of the flail in his hands (like the hunter's bolts follow the bolts he carries);
        /// without one of the Cultist's flails it is a plain brown leather whip
        public static string WhipStyle(Character ch)
        {
            try
            {
                var n = ch != null && ch.Weapon != null ? ch.Weapon.Name : null;
                if (n != null && n.StartsWith("Flg_"))
                {
                    var id = n.Substring(4);
                    if (Art.Get("whip", id + "_seg", 42f) != null) return id;                // every Cultist flail has its own lash
                }
            }
            catch { }
            return "leather";
        }

        /// ONE whip for both blows: it shoots out of the hand with the attack, cracks forward, then lashes back;
        /// returns when its tip cracks (x = first blow, y = second) so the hits land exactly on the cracks
        public static Vector2 LashWhip(Character ch, float reach)
        {
            var style = WhipStyle(ch);
            var kind = Kind(style);
            var seg = Art.Get("whip", style + "_seg", 42f);
            float D = TipDelay(kind);
            var times = new Vector2(kind.s1 + D, kind.s1 + kind.s2 + D);
            if (ch == null) return times;
            var go = new GameObject("Flagellant_whip");
            var w = go.AddComponent<FxWhip>();
            w.hero = ch; w.style = style; w.reach = reach; w.dir = 1f;
            return times;
        }

        /// how far the lash reaches (the hits of Poison Lash go as far as the whip does)
        public static float WhipLength(Character ch, float reach) => reach * Kind(WhipStyle(ch)).len;

        /// how each weapon's lash moves and looks: length, the strike (s1) and the lash back (s2), how much the tip
        /// lags behind (the snap), tails, trail colour and width
        struct WhipKind
        {
            public float len, s1, s2, lag, spread, trailW; public int tails; public Color trail;
            public WhipKind(float len, float s1, float s2, float lag, int tails, float spread, Color trail, float trailW)
            { this.len = len; this.s1 = s1; this.s2 = s2; this.lag = lag; this.tails = tails; this.spread = spread; this.trail = trail; this.trailW = trailW; }
        }
        /// how much later the tip follows the handle (the bend of the lash); the tip cracks this long after the handle stops
        static float TipDelay(WhipKind k) => k.s1 * 0.9f;

        static WhipKind Kind(string style)
        {
            switch (style)
            {
                case "rustyFlail": return new WhipKind(1.72f, 0.09f, 0.15f, 0.0025f, 1, 0f, new Color(0.95f, 0.55f, 0.25f, 0.85f), 0.07f);
                case "ritualScourge": return new WhipKind(2.0f, 0.075f, 0.12f, 0.003f, 5, 9f, new Color(1f, 0.32f, 0.28f, 0.85f), 0.03f);
                case "inquisitorFlail": return new WhipKind(1.9f, 0.09f, 0.14f, 0.0025f, 1, 0f, new Color(1f, 0.72f, 0.25f, 0.9f), 0.08f);
                case "devastator": return new WhipKind(2.25f, 0.11f, 0.17f, 0.0022f, 1, 0f, new Color(0.8f, 0.08f, 0.06f, 0.9f), 0.11f);
                case "pilgrimFlail": return new WhipKind(2.0f, 0.08f, 0.13f, 0.0025f, 1, 0f, new Color(0.95f, 0.88f, 0.7f, 0.85f), 0.05f);
                case "penitentChain": return new WhipKind(1.9f, 0.085f, 0.14f, 0.0025f, 1, 0f, new Color(0.88f, 0.88f, 0.95f, 0.85f), 0.06f);
                case "thornStar": return new WhipKind(1.95f, 0.09f, 0.14f, 0.0025f, 1, 0f, new Color(0.55f, 0.9f, 0.4f, 0.85f), 0.07f);
                case "martyrChain": return new WhipKind(2.0f, 0.095f, 0.15f, 0.0024f, 1, 0f, new Color(1f, 0.92f, 0.6f, 0.9f), 0.08f);
                case "doomBell": return new WhipKind(2.1f, 0.1f, 0.16f, 0.0023f, 1, 0f, new Color(0.95f, 0.7f, 0.35f, 0.9f), 0.09f);
                default: return new WhipKind(2.2f, 0.08f, 0.13f, 0.0025f, 1, 0f, new Color(1f, 1f, 1f, 0.9f), 0.05f);
            }
        }

        /// The lash as in the user's reference (a game whip): it shoots out of the hand together with the attack and
        /// cracks forward almost at once, a wave runs along it so it bends like a living whip, it lashes back for the
        /// second blow, every crack flashes and throws sparks, then it is drawn back in. A bright line follows the tip.
        public class FxWhip : MonoBehaviour
        {
            public Character hero; public string style = "leather"; public float reach = 1.6f, dir = 1f;
            // the swing starts on his right side and goes across to the left (the strike), then lashes back to the right
            const float FromAng = -75f, ToAng = 35f, BackAng = -40f, Retract = 0.16f, WaveAmp = 10f, WaveHz = 4.5f;
            const float Overshoot = 1.7f, BendPow = 1.4f;
            WhipKind kind;
            readonly System.Collections.Generic.List<(SpriteRenderer[] segs, SpriteRenderer tip, float angOff, float len)> lashes = new System.Collections.Generic.List<(SpriteRenderer[], SpriteRenderer, float, float)>();
            float t; int cracked; float done;
            SpriteRenderer heroSr, held; static int hiding;

            void Start()
            {
                kind = Kind(style);
                var seg = Art.Get("whip", style + "_seg", 42f);
                var tip = Art.Get("whip", style + "_tip", 42f);
                if (seg == null || hero == null) { Destroy(gameObject); return; }
                try { heroSr = hero.heroSprite; } catch { }
                // the flail in his hand turns into the lash: it is hidden while the lash is out
                try { held = hero.RightHandSprite; if (held != null && style != "leather") { hiding++; held.enabled = false; } else held = null; } catch { held = null; }
                int tails = kind.tails;
                for (int l = 0; l < tails; l++)
                {
                    float off = tails > 1 ? (l - (tails - 1) / 2f) * kind.spread : 0f;
                    float len = reach * kind.len * (tails > 1 ? 1f - Mathf.Abs(off) / (kind.spread * tails) * 0.5f : 1f);
                    int n = Mathf.Clamp(Mathf.RoundToInt(len * 42f / (seg.rect.width - 2f)), 4, 28);
                    var segs = new SpriteRenderer[n];
                    for (int i = 0; i < n; i++) segs[i] = Make(seg, 31);
                    var tp = tip != null ? Make(tip, 32) : null;
                    lashes.Add((segs, tp, off, len));
                }
                done = kind.s1 + kind.s2 + TipDelay(kind) + Retract;
                Update();
                // the trail is added only once the tip stands at its place (else it draws a line from far away)
                for (int l = 0; l < lashes.Count; l++)
                    if (lashes[l].tip != null && (lashes.Count == 1 || l == lashes.Count / 2)) AddTrail(lashes[l].tip.gameObject);
            }

            SpriteRenderer Make(Sprite s, int order)
            {
                var g = new GameObject("piece"); g.transform.SetParent(transform, false);
                var r = g.AddComponent<SpriteRenderer>(); r.sprite = s;
                if (heroSr != null) { r.sortingLayerID = heroSr.sortingLayerID; r.sortingOrder = heroSr.sortingOrder + order; }
                if (GameFx.PreviewLayer >= 0) g.layer = GameFx.PreviewLayer;
                return r;
            }

            void AddTrail(GameObject g)
            {
                try
                {
                    var tr = g.AddComponent<TrailRenderer>();
                    tr.time = 0.14f; tr.minVertexDistance = 0.02f; tr.widthMultiplier = kind.trailW;
                    tr.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
                    var sh = Shader.Find("Sprites/Default");
                    if (sh != null) tr.material = new Material(sh);
                    tr.startColor = kind.trail; tr.endColor = new Color(kind.trail.r, kind.trail.g, kind.trail.b, 0f);
                    if (heroSr != null) { tr.sortingLayerID = heroSr.sortingLayerID; tr.sortingOrder = heroSr.sortingOrder + 30; }
                    tr.Clear();
                }
                catch { }
            }

            /// the angle of a piece of the lash (f = 0 at the hand, 1 at the tip): every piece makes the same swing, but
            /// later the further it is from the hand, so the lash bends in an arc behind the handle; the outer pieces
            /// overshoot and come back, so it curls into an S right before the tip cracks
            float Ang(float t, float f)
            {
                float u = t - TipDelay(kind) * Mathf.Pow(f, BendPow);
                float o = Overshoot * f;
                if (u <= kind.s1) return Mathf.LerpUnclamped(FromAng, ToAng, EaseOutBack(u / kind.s1, o));
                return Mathf.LerpUnclamped(ToAng, BackAng, EaseOutBack((u - kind.s1) / kind.s2, o * 0.8f));
            }

            static float EaseOutBack(float x, float s) { x = Mathf.Clamp01(x) - 1f; return 1f + (s + 1f) * x * x * x + s * x * x; }
            static float EaseOut(float x) { x = Mathf.Clamp01(x); return 1f - (1f - x) * (1f - x) * (1f - x); }
            static float EaseInOut(float x) { x = Mathf.Clamp01(x); return x < 0.5f ? 4f * x * x * x : 1f - Mathf.Pow(-2f * x + 2f, 3f) / 2f; }

            void Update()
            {
                if (hero == null) { Destroy(gameObject); return; }
                t += Time.deltaTime;
                float facing = Mathf.Atan2(hero.transform.right.y, hero.transform.right.x) * Mathf.Rad2Deg;
                // it comes out of the hand that holds the weapon (and follows it through the attack animation)
                Vector2 hand = (Vector2)hero.transform.position + (Vector2)hero.transform.right * 0.15f;
                try { if (hero.RightHandSprite != null) hand = hero.RightHandSprite.transform.position; } catch { }
                float unroll = 0.3f + 0.7f * EaseOut(t / (kind.s1 * 0.7f));                    // shoots out of the hand
                // a wave running along the lash, strongest right after each crack, fading out
                float c1 = kind.s1, c2 = kind.s1 + kind.s2;
                float wave = WaveAmp * (Mathf.Exp(-Mathf.Max(0f, t - c1) * 9f) * (t >= c1 * 0.5f ? 1f : 0.4f) + (t > c2 ? Mathf.Exp(-(t - c2) * 9f) : 0f));
                foreach (var (segs, tip, angOff, len) in lashes)
                {
                    int n = segs.Length;
                    float D = TipDelay(kind);
                    float arrive = c2 + D;                                                        // the second crack, then it is drawn in
                    float back = Mathf.Clamp01((t - arrive) / Retract);
                    float L = len * unroll * (1f - 0.8f * EaseInOut(back)), step = L / n;
                    float alpha = 1f - back * back;
                    Vector2 p = hand;
                    for (int i = 0; i <= n; i++)
                    {
                        float f = i / (float)n;
                        float a = facing + angOff + dir * (Ang(t, f) + wave * f * Mathf.Sin((f * 1.6f - t * WaveHz) * Mathf.PI * 2f));
                        Vector2 d = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                        if (i < n)
                        {
                            var s = segs[i];
                            s.transform.position = new Vector3(p.x + d.x * step * 0.5f, p.y + d.y * step * 0.5f, 0f);
                            s.transform.rotation = Quaternion.Euler(0f, 0f, a);
                            s.color = new Color(1f, 1f, 1f, alpha);
                            p += d * step;
                        }
                        else if (tip != null)
                        {
                            tip.transform.position = new Vector3(p.x, p.y, 0f);
                            tip.transform.rotation = Quaternion.Euler(0f, 0f, a);
                            tip.color = new Color(1f, 1f, 1f, alpha);
                            if (angOff == lashes[lashes.Count / 2].angOff && cracked < 2 && t >= (cracked == 0 ? c1 : c2) + D) { cracked++; Crack(p, a); }
                        }
                    }
                }
                if (t >= done) Destroy(gameObject);
            }

            void OnDestroy() { try { if (held != null && --hiding <= 0) { hiding = 0; held.enabled = true; } } catch { } }

            /// the crack of the tip: a white star flash and a spray of sparks in the lash's colour
            void Crack(Vector2 at, float a)
            {
                var s = Art.Get("whip", "crack", 42f);
                if (s != null)
                {
                    var g = new GameObject("Flagellant_whipCrack");
                    g.transform.position = new Vector3(at.x, at.y, 0f);
                    g.transform.rotation = Quaternion.Euler(0f, 0f, a + 45f);
                    g.transform.localScale = Vector3.one * 1.3f;
                    var r = g.AddComponent<SpriteRenderer>(); r.sprite = s;
                    r.color = Color.Lerp(Color.white, kind.trail, 0.3f);
                    if (heroSr != null) { r.sortingLayerID = heroSr.sortingLayerID; r.sortingOrder = heroSr.sortingOrder + 33; }
                    if (GameFx.PreviewLayer >= 0) g.layer = GameFx.PreviewLayer;
                    var fl = g.AddComponent<FxLife>(); fl.life = 0.16f; fl.grow = 2.0f;
                }
                for (int k = 0; k < 7; k++)                                                        // sparks flying off the tip
                {
                    float sa = (a + Random.Range(-70f, 70f)) * Mathf.Deg2Rad;
                    var v = new Vector2(Mathf.Cos(sa), Mathf.Sin(sa)) * Random.Range(1.5f, 3.2f);
                    FLogic.Fx("spark", at, Random.Range(0f, 360f), Random.Range(0.6f, 1.0f), Color.Lerp(Color.white, kind.trail, 0.5f), Random.Range(0.12f, 0.22f), hero, 34, 0.6f, null, v);
                }
            }
        }        // ---------------- Sinner's Strike: he vanishes in a dark beam, lands in dust, each blow breaks armour
        // (no round glows here: only the beam and the dust of those game effects are kept)
        public static void SinnerVanish(Vector2 from) =>
            GameFx.Spawn("Hunter_Step_Beamup", from, 0f, 0.8f, DarkBlood, 1.5f, null, new[] { "Hunter_Step_Beamup", "DustParticle_OnAttack" });
        public static readonly Color Dust = new Color(0.62f, 0.42f, 0.38f, 1f);
        public static void SinnerAppear(Vector2 to) =>
            GameFx.Spawn("DashBlue", to, 0f, 0.8f, Dust, 1.8f, null, new[] { "DashBlue", "Dust" });
        public static void SinnerHit(Vector2 me, Vector2 tp, bool last)
        {
            float rot = Mathf.Atan2(tp.y - me.y, tp.x - me.x) * Mathf.Rad2Deg;
            GameFx.Spawn("pcm:particles_armorReduce_Directed", tp, rot, 1f, null, 1.5f);
        }

        /// one, two, three: a sharp blood streak cut in one quick stroke from a to b (the Berserker's own leap trail,
        /// in blood red, drawn by a short fast stroke - only a picture, the hero is not moved by it)
        public static void SinnerStreak(Vector2 a, Vector2 b, int k)
        {
            var go = GameFx.Spawn("BersTrail", a, 0f, 0.8f + 0.15f * k, Blood, 0.7f);
            if (go == null) return;
            var mv = go.AddComponent<FxStroke>();
            mv.from = a; mv.to = b; mv.time = 0.06f;
        }

        // ---------------- Martyr's Hand: only the triangle of the blow, burning orange, a heavy impact in it
        public static void MartyrBlow(Vector2 me, float rot, float R, Character hero)
        {
            // the triangle: a soft additive glow like the game's own area particles
            var go = new GameObject("Flagellant_martyrWedge");
            go.transform.position = me + Dir(rot) * (R * 0.5f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, rot);
            go.transform.localScale = Vector3.one * R;
            var w = go.AddComponent<SpriteRenderer>();
            w.sprite = GameFx.SoftWedge;
            var mat = GameFx.Additive; if (mat != null) w.sharedMaterial = mat;
            w.color = new Color(1f, 0.5f, 0.12f, 1f);
            try { var hs = hero != null ? hero.heroSprite : null; if (hs != null) { w.sortingLayerID = hs.sortingLayerID; w.sortingOrder = hs.sortingOrder + 30; } } catch { }
            if (GameFx.PreviewLayer >= 0) go.layer = GameFx.PreviewLayer;
            var f = go.AddComponent<FxLife>(); f.life = 0.4f; f.grow = 1.05f;
            GameFx.Spawn("DashBlue", me + Dir(rot) * (R * 0.35f), rot, 0.8f, Dust, 1.6f, null, new[] { "DashBlue", "Dust" });
        }
        public static void MartyrHit(Vector2 tp)
        {
            GameFx.Spawn("pcm:particles_dmg_bleed", tp, 0f, 1f, null, 1.5f);
        }

        // ---------------- Hammer of Penance: the spiked ball on its chain goes round over his head (from behind him),
        // then comes down in front of him; cracks in the ground, dust and stone chips where it lands
        public const float HammerSpin = 0.13f, HammerFall = 0.07f;
        /// the head and the chain of the hammer follow the flail in his hand (the same pieces as his lash);
        /// without one of the Cultist's flails it is a plain spiked mace on an iron chain
        static (Sprite head, Sprite link, float headScale) HammerLook(Character ch)
        {
            var style = WhipStyle(ch);
            if (style != "leather")
            {
                var head = Art.Get("whip", style + "_tip", 42f);
                var link = Art.Get("whip", style + "_seg", 42f);
                if (head != null && link != null) return (head, link, Mathf.Clamp(22f / Mathf.Max(head.rect.width, head.rect.height), 1.2f, 1.7f));
            }
            return (Art.Get("", "hammer_ball", 42f), Art.Get("whip", "penitentChain_seg", 42f), 1f);
        }

        public static System.Collections.IEnumerator HammerSwing(Character ch, Vector2 target)
        {
            const float ChainLen = 0.72f;                                    // from his fist to the ball while it whirls
            var (headSp, linkSp, headScale) = HammerLook(ch);
            if (headSp == null) yield break;
            SpriteRenderer hs = null;
            try { hs = ch.heroSprite; } catch { }
            var root = new GameObject("Flagellant_hammer");
            SpriteRenderer Piece(Sprite sp, int order)
            {
                var g = new GameObject("piece"); g.transform.SetParent(root.transform, false);
                var r = g.AddComponent<SpriteRenderer>(); r.sprite = sp;
                if (hs != null) { r.sortingLayerID = hs.sortingLayerID; r.sortingOrder = hs.sortingOrder + order; }
                if (GameFx.PreviewLayer >= 0) g.layer = GameFx.PreviewLayer;
                return r;
            }
            var ball = Piece(headSp, 34);
            var links = new System.Collections.Generic.List<SpriteRenderer>();
            float linkLen = linkSp != null ? Mathf.Max(0.08f, (linkSp.rect.width - 2f) / 42f) : 0.15f;
            SpriteRenderer held = null;                                       // the flail in his hand becomes the whirling hammer
            try { held = ch.RightHandSprite; if (held != null && held.enabled) held.enabled = false; else held = null; } catch { held = null; }
            float t = 0f, lastA = 0f;
            while (t < HammerSpin + HammerFall && ch != null && root != null)
            {
                t += Time.deltaTime;
                Vector2 hand = ch.transform.position;
                try { if (ch.RightHandSprite != null) hand = ch.RightHandSprite.transform.position; } catch { }
                float face = Mathf.Atan2(ch.transform.right.y, ch.transform.right.x) * Mathf.Rad2Deg;
                Vector2 p; float s;
                if (t < HammerSpin)
                {
                    // one wide turn round his fist on the full chain: from behind him, past his right side, over to the front
                    float k = t / HammerSpin;
                    lastA = face + 180f - 360f * (k * k * (3f - 2f * k));
                    p = hand + Dir(lastA) * ChainLen; s = 1.2f;
                }
                else
                {
                    // and down on the ground in front of him (a little smaller as it falls from above)
                    float k = Mathf.Clamp01((t - HammerSpin) / HammerFall);
                    p = Vector2.Lerp(hand + Dir(lastA) * ChainLen, target, k * k); s = Mathf.Lerp(1.2f, 1f, k);
                }
                ball.transform.position = new Vector3(p.x, p.y, 0f);
                ball.transform.localScale = Vector3.one * headScale * s;
                ball.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(p.y - hand.y, p.x - hand.x) * Mathf.Rad2Deg - 90f);
                // the chain: links laid from the fist to the ball
                if (linkSp != null)
                {
                    Vector2 d = p - hand; float dist = d.magnitude;
                    int n = Mathf.Clamp(Mathf.CeilToInt(dist / linkLen), 1, 24);
                    while (links.Count < n) links.Add(Piece(linkSp, 33));
                    float rot = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                    for (int i = 0; i < links.Count; i++)
                    {
                        links[i].enabled = i < n;
                        if (i >= n) continue;
                        Vector2 q = hand + d * ((i + 0.5f) / n);
                        links[i].transform.position = new Vector3(q.x, q.y, 0f);
                        links[i].transform.rotation = Quaternion.Euler(0f, 0f, rot);
                        links[i].transform.localScale = new Vector3(dist / n / linkLen, 1f, 1f);
                    }
                }
                yield return null;
            }
            // the ball rests a moment in the dent and fades; the chain is drawn back into his hand
            foreach (var lk in links) if (lk != null) Object.Destroy(lk.gameObject);
            if (ball != null) { ball.transform.SetParent(null, true); var f = ball.gameObject.AddComponent<FxLife>(); f.life = 0.22f; f.grow = 1f; }
            if (root != null) Object.Destroy(root);
            try { if (held != null) held.enabled = true; } catch { }
        }

        public static void HammerImpact(Character ch, Vector2 at, float r)
        {
            // cracks in the floor that slowly fade, the game's dust, a ring of stone chips flying out
            var cr = Art.MakeSprite("hammer_crater", at, Mathf.Max(0.8f, r * 0.95f) * 24f / 42f * 1.5f, ch, -2);
            if (cr != null) { cr.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)); var f = cr.gameObject.AddComponent<FxLife>(); f.life = 2.2f; f.grow = 1f; }
            GameFx.Spawn("DashBlue", at, 0f, 0.9f, Dust, 1.8f, null, new[] { "DashBlue", "Dust" });
            GameFx.Spawn("pcm:particles_armorReduce_Destr", at, 0f, 1f, null, 1.5f);
            for (int k = 0; k < 9; k++)
            {
                float a = k * 40f + Random.Range(-15f, 15f);
                FLogic.Fx("spark", at, Random.Range(0f, 360f), Random.Range(0.6f, 1f), Dust, Random.Range(0.25f, 0.4f), ch, 33, 0.7f, null, Dir(a) * Random.Range(1.6f, 3f));
            }
        }

        public static void HammerHit(Vector2 tp)
        {
            GameFx.Spawn("pcm:particles_armorReduce_Directed", tp, 90f, 1f, null, 1.5f);
        }

        // ---------------- Divine Punishment: a ring of holy light over the area, a beam from above on every enemy, cursed hits
        public static void Wrath(Vector2 me, float R, Transform follow = null)
        {
            GameFx.Spawn("RingAura_AntiUndead", me, 0f, R / 1.4f, Holy, 1.6f, follow);
            GameFx.Spawn("ShockwavePower", me, 0f, R * 0.45f, new Color(1f, 0.35f, 0.25f, 0.9f), 1.2f);
        }
        public static void WrathHit(Vector2 tp)
        {
            GameFx.Spawn("Hunter_Step_Beamup", tp, 0f, 1f, Holy, 1.6f);
            GameFx.Spawn("pcm:prefab_eff_curseMagic", tp, 0f, 1f, null, 2f);
        }

        // ---------------- Torment: blood is torn out of the enemies and gathers on him, then a great blood-and-fire blast
        public static void TormentDonor(Vector2 tp) => GameFx.Spawn("pcm:particles_dmg_bleed", tp, 0f, 1f, null, 1.5f);
        public static void TormentGather(Vector2 me, Transform follow = null) =>
            GameFx.Spawn("Flash_star_HunterSign", me, 0f, 0.35f, Blood, 0.8f, follow);
        public static void TormentBlast(Vector2 me, float R)
        {
            GameFx.Spawn("pcm:particles_explosion", me, 0f, Mathf.Max(1f, R * 0.5f), null, 2.5f);
            GameFx.Spawn("Explosion_Magic", me, 0f, R * 0.5f, Blood, 1.5f);
            GameFx.Spawn("ShockwavePower", me, 0f, R * 0.45f, Blood, 1.2f);
            GameFx.Spawn("pcm:prefab_dec_explosionTrace", me, 0f, 1f, null, 8f);
        }

        // ---------------- shared bits of the lashes
        static SpriteRenderer LashPiece(Transform parent, Sprite s, SpriteRenderer heroSr, int order)
        {
            var g = new GameObject("piece"); g.transform.SetParent(parent, false);
            var r = g.AddComponent<SpriteRenderer>(); r.sprite = s;
            if (heroSr != null) { r.sortingLayerID = heroSr.sortingLayerID; r.sortingOrder = heroSr.sortingOrder + order; }
            if (GameFx.PreviewLayer >= 0) g.layer = GameFx.PreviewLayer;
            return r;
        }

        static void LashTrail(GameObject g, WhipKind kind, SpriteRenderer heroSr, float time = 0.14f)
        {
            try
            {
                var tr = g.AddComponent<TrailRenderer>();
                tr.time = time; tr.minVertexDistance = 0.02f; tr.widthMultiplier = kind.trailW;
                tr.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
                var sh = Shader.Find("Sprites/Default");
                if (sh != null) tr.material = new Material(sh);
                tr.startColor = kind.trail; tr.endColor = new Color(kind.trail.r, kind.trail.g, kind.trail.b, 0f);
                if (heroSr != null) { tr.sortingLayerID = heroSr.sortingLayerID; tr.sortingOrder = heroSr.sortingOrder + 30; }
                tr.Clear();
            }
            catch { }
        }

        /// the crack of a lash tip: a white star flash and a few sparks in the lash's colour
        static void LashCrack(Vector2 at, float a, WhipKind kind, Character hero, SpriteRenderer heroSr, float size = 1.3f, int sparks = 7)
        {
            var s = Art.Get("whip", "crack", 42f);
            if (s != null)
            {
                var g = new GameObject("Flagellant_whipCrack");
                g.transform.position = new Vector3(at.x, at.y, 0f);
                g.transform.rotation = Quaternion.Euler(0f, 0f, a + 45f);
                g.transform.localScale = Vector3.one * size;
                var r = g.AddComponent<SpriteRenderer>(); r.sprite = s;
                r.color = Color.Lerp(Color.white, kind.trail, 0.3f);
                if (heroSr != null) { r.sortingLayerID = heroSr.sortingLayerID; r.sortingOrder = heroSr.sortingOrder + 33; }
                if (GameFx.PreviewLayer >= 0) g.layer = GameFx.PreviewLayer;
                var fl = g.AddComponent<FxLife>(); fl.life = 0.16f; fl.grow = 2.0f;
            }
            for (int k = 0; k < sparks; k++)
            {
                float sa = (a + Random.Range(-70f, 70f)) * Mathf.Deg2Rad;
                var v = new Vector2(Mathf.Cos(sa), Mathf.Sin(sa)) * Random.Range(1.5f, 3.2f);
                FLogic.Fx("spark", at, Random.Range(0f, 360f), Random.Range(0.6f, 1.0f), Color.Lerp(Color.white, kind.trail, 0.5f), Random.Range(0.12f, 0.22f), hero, 34, 0.6f, null, v);
            }
        }

        // ---------------- Quick Snap: the lash shoots out dead straight, cracks on the first enemy (or at full reach), comes back
        public const float SnapOut = 0.07f, SnapHold = 0.04f, SnapBack = 0.1f;
        /// starts the lash; returns when its tip arrives (the moment of the hit)
        public static float Snap(Character ch, float len)
        {
            if (ch == null) return 0f;
            var go = new GameObject("Flagellant_snap");
            var s = go.AddComponent<FxSnap>(); s.hero = ch; s.style = WhipStyle(ch); s.len = len;
            return SnapOut;
        }

        public class FxSnap : MonoBehaviour
        {
            public Character hero; public string style = "leather"; public float len = 3f;
            WhipKind kind; SpriteRenderer[] segs; SpriteRenderer tip, heroSr, held;
            float t, face; bool cracked;

            void Start()
            {
                kind = Kind(style);
                var seg = Art.Get("whip", style + "_seg", 42f);
                var tp = Art.Get("whip", style + "_tip", 42f);
                if (seg == null || hero == null) { Destroy(gameObject); return; }
                try { heroSr = hero.heroSprite; } catch { }
                try { held = hero.RightHandSprite; if (held != null && held.enabled && style != "leather") held.enabled = false; else held = null; } catch { held = null; }
                face = Mathf.Atan2(hero.transform.right.y, hero.transform.right.x) * Mathf.Rad2Deg;   // strictly straight: the line is fixed
                int n = Mathf.Clamp(Mathf.RoundToInt(len * 42f / Mathf.Max(2f, seg.rect.width - 2f)), 4, 48);
                segs = new SpriteRenderer[n];
                for (int i = 0; i < n; i++) segs[i] = LashPiece(transform, seg, heroSr, 31);
                tip = tp != null ? LashPiece(transform, tp, heroSr, 32) : null;
                Update();
                if (tip != null) LashTrail(tip.gameObject, kind, heroSr, 0.1f);
            }

            static float EaseOut(float x) { x = Mathf.Clamp01(x); return 1f - (1f - x) * (1f - x) * (1f - x); }

            void Update()
            {
                if (hero == null || segs == null) { Destroy(gameObject); return; }
                t += Time.deltaTime;
                Vector2 hand = hero.transform.position;
                try { if (hero.RightHandSprite != null) hand = hero.RightHandSprite.transform.position; } catch { }
                float L = t < SnapOut ? Mathf.Lerp(0.15f, len, EaseOut(t / SnapOut))
                    : t < SnapOut + SnapHold ? len
                    : len * (1f - 0.95f * EaseOut((t - SnapOut - SnapHold) / SnapBack));
                float alpha = t < SnapOut + SnapHold ? 1f : 1f - Mathf.Clamp01((t - SnapOut - SnapHold) / SnapBack);
                int n = segs.Length; float step = L / n;
                Vector2 p = hand;
                // a small ripple runs down the taut lash right after the crack
                float ripple = cracked ? 7f * Mathf.Exp(-(t - SnapOut) * 18f) : 2f;
                for (int i = 0; i <= n; i++)
                {
                    float f = i / (float)n;
                    float a = face + ripple * f * Mathf.Sin((f * 3f - t * 9f) * Mathf.PI * 2f);
                    Vector2 d = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                    if (i < n)
                    {
                        var s = segs[i];
                        s.transform.position = new Vector3(p.x + d.x * step * 0.5f, p.y + d.y * step * 0.5f, 0f);
                        s.transform.rotation = Quaternion.Euler(0f, 0f, a);
                        s.color = new Color(1f, 1f, 1f, alpha);
                        p += d * step;
                    }
                    else if (tip != null)
                    {
                        tip.transform.position = new Vector3(p.x, p.y, 0f);
                        tip.transform.rotation = Quaternion.Euler(0f, 0f, a);
                        tip.color = new Color(1f, 1f, 1f, alpha);
                    }
                }
                if (!cracked && t >= SnapOut) { cracked = true; LashCrack(p, face, kind, hero, heroSr, 1.1f, 5); }
                if (t >= SnapOut + SnapHold + SnapBack) Destroy(gameObject);
            }

            void OnDestroy() { try { if (held != null) held.enabled = true; } catch { } }
        }

        // ---------------- Religious Ecstasy: he flogs himself with his whip - three blows over his shoulders onto his back
        // (right, left, right). Each blow: the lash is thrown out in front of him and held a moment, then SNAPPED over
        // the shoulder in one jerk and lies across his back (a crack, blood), a moment later it is thrown out again.
        // (A swing of the tip round him looked like twirling the whip - the user wants blows.)
        public const float SelfRaise = 0.12f, SelfHold = 0.05f, SelfStrike = 0.05f, SelfRest = 0.11f, SelfBack = 0.12f;
        /// starts the self-flogging; returns when the first blow lands on him
        public static float SelfLash(Character ch)
        {
            if (ch == null) return 0f;
            var go = new GameObject("Flagellant_selfLash");
            var s = go.AddComponent<FxSelfLash>(); s.hero = ch; s.style = WhipStyle(ch);
            return SelfRaise + SelfHold + SelfStrike;
        }

        public class FxSelfLash : MonoBehaviour
        {
            public Character hero; public string style = "leather";
            const float Len = 0.95f;
            WhipKind kind; SpriteRenderer[] segs; SpriteRenderer tip, heroSr, held;
            float t, segW = 0.15f; int cracked;
            static float Blow => SelfRaise + SelfHold + SelfStrike + SelfRest;
            static float Hit => SelfRaise + SelfHold + SelfStrike;                  // when in a blow the lash lands

            void Start()
            {
                kind = Kind(style);
                var seg = Art.Get("whip", style + "_seg", 42f);
                var tp = Art.Get("whip", style + "_tip", 42f);
                if (seg == null || hero == null) { Destroy(gameObject); return; }
                try { heroSr = hero.heroSprite; } catch { }
                try { held = hero.RightHandSprite; if (held != null && held.enabled && style != "leather") held.enabled = false; else held = null; } catch { held = null; }
                segW = Mathf.Max(2f, seg.rect.width - 2f) / 42f;
                int n = Mathf.Clamp(Mathf.RoundToInt(Len / segW), 4, 14);
                segs = new SpriteRenderer[n];
                for (int i = 0; i < n; i++) segs[i] = LashPiece(transform, seg, heroSr, 31);
                tip = tp != null ? LashPiece(transform, tp, heroSr, 32) : null;
                Update();
            }

            const int Blows = 3;

            /// the tip of the lash and the bend point of its arc at a moment u of a blow (side -1 = over his right
            /// shoulder, +1 = over the left); same maths as ArtGen SelfLashSim (the frames were checked there)
            static (Vector2 tip, Vector2 ctrl) Pose(Vector2 me, Vector2 hand, float face, float u, float side, bool first)
            {
                Vector2 fwd = Dir(face), sd = Dir(face + side * 90f);
                Vector2 front = me + fwd * 0.95f + sd * 0.12f;                    // thrown out in front of him
                Vector2 back = me - fwd * 0.34f + sd * 0.06f;                     // lying across his back
                Vector2 shoulder = me + sd * 0.34f + fwd * 0.05f;                 // the lash goes over this shoulder
                Vector2 start = first ? hand + fwd * 0.25f : back;
                if (u < SelfRaise)
                {
                    float k = u / SelfRaise; k = 1f - (1f - k) * (1f - k);
                    Vector2 tip = Vector2.Lerp(start, front, k);
                    return (tip, Vector2.Lerp(first ? (hand + tip) * 0.5f : shoulder, (hand + tip) * 0.5f, k));
                }
                if (u < SelfRaise + SelfHold) return (front, (hand + front) * 0.5f + fwd * 0.04f);
                if (u < Hit)
                {
                    float k = (u - SelfRaise - SelfHold) / SelfStrike; k *= k;     // the jerk speeds up into the blow
                    return (Vector2.Lerp(front, back, k), Vector2.Lerp((hand + front) * 0.5f, shoulder, k));
                }
                return (back, shoulder);
            }

            static Vector2 Bez(Vector2 a, Vector2 c, Vector2 b, float t) { float m = 1f - t; return m * m * a + 2f * m * t * c + t * t * b; }

            void Update()
            {
                if (hero == null || segs == null) { Destroy(gameObject); return; }
                t += Time.deltaTime;
                Vector2 me = hero.transform.position;
                Vector2 hand = me;
                try { if (hero.RightHandSprite != null) hand = hero.RightHandSprite.transform.position; } catch { }
                float face = Mathf.Atan2(hero.transform.right.y, hero.transform.right.x) * Mathf.Rad2Deg;
                float end = Blows * Blow;
                int blow = Mathf.Min(Blows - 1, Mathf.FloorToInt(t / Blow));
                float u = Mathf.Min(t - blow * Blow, Blow);
                float side = blow % 2 == 0 ? -1f : 1f;                                // right shoulder, left, right
                float back = Mathf.Clamp01((t - end) / SelfBack);
                float alpha = 1f - back;
                var (tipP, ctrl) = Pose(me, hand, face, u, side, blow == 0);
                tipP = Vector2.Lerp(tipP, hand, back * 0.8f); ctrl = Vector2.Lerp(ctrl, hand, back * 0.8f);
                int n = segs.Length;
                Vector2 prev = hand;
                for (int i = 1; i <= n; i++)
                {
                    Vector2 q = Bez(hand, ctrl, tipP, i / (float)n);
                    Vector2 dd = q - prev; float seg = dd.magnitude;
                    float a = Mathf.Atan2(dd.y, dd.x) * Mathf.Rad2Deg;
                    var s = segs[i - 1];
                    s.transform.position = new Vector3((prev.x + q.x) * 0.5f, (prev.y + q.y) * 0.5f, 0f);
                    s.transform.rotation = Quaternion.Euler(0f, 0f, a);
                    s.transform.localScale = new Vector3(Mathf.Clamp(seg / Mathf.Max(0.01f, segW), 0.4f, 1.6f), 1f, 1f);
                    s.color = new Color(1f, 1f, 1f, alpha);
                    if (i == n && tip != null)
                    {
                        tip.transform.position = new Vector3(q.x, q.y, 0f);
                        tip.transform.rotation = Quaternion.Euler(0f, 0f, a);
                        tip.color = new Color(1f, 1f, 1f, alpha);
                    }
                    prev = q;
                }
                // every blow lands on his back: a crack, his blood spurts, the camera jolts a little
                if (cracked < Blows && t >= cracked * Blow + Hit)
                {
                    cracked++;
                    LashCrack(tipP, face + side * 175f, kind, hero, heroSr, 0.9f, 4);
                    GameFx.Spawn("pcm:particles_dmg_bleed", tipP, 0f, 0.9f, null, 1.2f);
                    FLogic.Splash(hero, tipP, 4, 1.4f, 0.9f);
                    FLogic.Shake(0.06f, 0.06f);
                }
                if (t >= end + SelfBack) Destroy(gameObject);
            }

            void OnDestroy() { try { if (held != null) held.enabled = true; } catch { } }
        }

        /// moves a trail picture from a to b in a moment (draws a sharp streak)
        public class FxStroke : MonoBehaviour
        {
            public Vector2 from, to; public float time = 0.06f;
            float t;
            void Update()
            {
                t += Time.deltaTime;
                Vector2 p = Vector2.Lerp(from, to, Mathf.Clamp01(t / Mathf.Max(0.01f, time)));
                transform.position = new Vector3(p.x, p.y, transform.position.z);
            }
        }

        // ---------------- Religious Ecstasy: a burst of his own blood and the game's "damage up" glow on him
        public static void Ecstasy(Vector2 me, Transform follow = null)
        {
            GameFx.Spawn("Flash_star_HunterSign", me, 0f, 0.6f, Blood, 0.9f, follow);
            GameFx.Spawn("pcm:particles_blood_onDie", me, 0f, 1f, null, 2f);
            GameFx.Spawn("pcm:particles_getBuff_dmgMod", me, 0f, 1f, null, 2.5f, follow);
        }

        // ---------------- Blood Cleansing: blood runs from the cut, a small holy flash of cleansing (a third of the old size)
        public static void Cleansing(Vector2 me, Vector2 fwd, Transform follow = null)
        {
            GameFx.Spawn("pcm:particles_dmg_bleed", me + fwd * 0.35f, 0f, 1f, null, 1.5f);
            GameFx.Spawn("Burst_rainbow_healGrass_12", me, 0f, 0.4f, new Color(1f, 0.3f, 0.25f, 1f), 2f, follow);
            GameFx.Spawn("Particles_Necr_OnSpawnSeal", me, 0f, 0.35f, Holy, 1.2f, follow);
        }
    }
}
