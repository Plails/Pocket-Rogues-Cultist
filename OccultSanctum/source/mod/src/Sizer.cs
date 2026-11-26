using UnityEngine;

namespace OccultSanctum
{
    /// Sizes the sanctum's people against the player's hero (the user: cultists as big as the hero, the elder a little
    /// bigger, the general big next to the crossbowmen).
    /// Our people stand on different frames of the game (a goblin, an ogre, the two-handed rig), and those frames hold
    /// their shoulders differently (the goblin's and the ogre's stand far wider than the hero's), so the cultists and
    /// the elder are measured by what one sees: the width of the body picture (its opaque part) against the hero's.
    /// The general stands on the hero's own frame: him the shoulders measure exactly.
    public static class Sizer
    {
        public const float Cultist = 1.0f, Elder = 1.12f,   // (0.41.0, the user: a little bigger; were 0.92 / 1.03)
                                  General = 2.2f;           // (the general's body picture takes in his pauldron spikes)           // the general as big as the game's own champions (the zombie guardian's body is about twice the hero's)
        const float HeroSpanIfNone = 0.30f, HeroBodyIfNone = 0.5f;

        static Transform Bone(Transform root, string n)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == n) return t;
            return null;
        }

        static float Span(Transform root)
        {
            Transform r = Bone(root, "UpperArm_R"), l = Bone(root, "UpperArm_L");
            return r != null && l != null ? Vector2.Distance(r.position, l.position) : -1f;
        }

        static Character Hero()
        {
            try { var go = GameObject.FindGameObjectWithTag("Player"); return go != null ? go.GetComponent<Character>() : null; } catch { return null; }
        }

        public static float HeroSpan()
        {
            var ch = Hero();
            if (ch != null && ch.UpperArm_R != null && ch.UpperArm_L != null)
            {
                float s = Vector2.Distance(ch.UpperArm_R.transform.position, ch.UpperArm_L.transform.position);
                if (s > 0.05f && s < 3f) return s;
            }
            return HeroSpanIfNone;
        }

        /// the width in the world of what one sees of a sprite (its opaque columns, if its picture can be read)
        static float Seen(SpriteRenderer sr)
        {
            if (sr == null || sr.sprite == null || !sr.enabled) return 0f;
            var s = sr.sprite; var rc = s.textureRect;
            float px = rc.width;
            try
            {
                var tex = s.texture;
                if (tex != null && tex.isReadable)
                {
                    int x0 = (int)rc.x, y0 = (int)rc.y, w = (int)rc.width, h = (int)rc.height, lo = w, hi = -1;
                    var cols = tex.GetPixels(x0, y0, w, h);
                    for (int x = 0; x < w; x++) for (int y = 0; y < h; y++) if (cols[y * w + x].a > 0.15f) { if (x < lo) lo = x; if (x > hi) hi = x; break; }
                    if (hi >= lo) px = hi - lo + 1;
                }
            }
            catch { }
            return px / s.pixelsPerUnit * Mathf.Abs(sr.transform.lossyScale.x);
        }

        /// what one sees of the hero's body, wide: his body picture cannot be read (its blank margins made him seem
        /// half again too wide and the cultists came out twice his size - the user's picture), so it is taken from what
        /// the user's pictures show (a cultist of the game's own size, body 0.40, stands as wide as the hero) and
        /// follows the hero's own scale by his shoulders (those are measured exactly: 0.30..0.32)
        /// (0.38.1, the sandbox: with a mage hero his shoulders read 0.46 and every cultist came out half again too big,
        /// 0.54 instead of 0.36) - so the yardstick is the hero the user tuned the sizes on, the same for every class
        public static float HeroBody() => 0.39f;

        static SpriteRenderer BodyOf(GameObject go)
        {
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true)) if (sr.gameObject.name == "Body") return sr;
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true)) if (sr.sprite != null && sr.sprite.name.EndsWith("_Body", System.StringComparison.Ordinal)) return sr;   // (the lich's body piece has another name)
            return null;
        }

        /// makes the mob's body (what one sees of it) `times` the hero's
        public static void Fit(GameObject go, float times)
        {
            if (go == null) return;
            float me = Seen(BodyOf(go)), hero = HeroBody();
            if (me <= 0.01f) return;
            float k = Mathf.Clamp(hero * times / me, 0.5f, 3.6f);                      // (a wrong reading never makes a dwarf or a giant)
            go.transform.localScale = go.transform.localScale * k;
            SanctumPlugin.Log.LogInfo("Sanctum: " + go.name + " sized to " + times + "x the hero (body " + me.ToString("0.00") + " -> " + (me * k).ToString("0.00") + ", hero " + hero.ToString("0.00") + ")");
        }

        /// the same frame as the hero's: shoulders against shoulders
        public static void FitByShoulders(GameObject go, float times)
        {
            if (go == null) return;
            float me = Span(go.transform), hero = HeroSpan();
            if (me <= 0.01f) return;
            float k = hero * times / me;
            go.transform.localScale = go.transform.localScale * k;
            SanctumPlugin.Log.LogInfo("Sanctum: " + go.name + " sized to " + times + "x the hero (shoulders " + me.ToString("0.00") + " -> " + (me * k).ToString("0.00") + ", hero " + hero.ToString("0.00") + ")");
        }

        // ---- the champions (the user, 2026-11-19: the archmage and the general must not change size, and be as big as
        // something between the game's own champions - the skeleton giant and the zombie guardian). They no longer
        // follow the hero (a mage and a hunter stand differently wide, so the champions came out different every run):
        // their body is made as wide as the two champions' bodies are on average, in the world, and kept so.
        const float LevelGrowth = 1.25f;                                          // (the game grows its mobs 1.2 + 0.007 a level: a mid-level one)
        static float champBody = -1f;

        static float RefBody(MonoBehaviour boss)
        {
            if (boss == null) return -1f;
            SpriteRenderer best = null; float bw = 0f;
            foreach (var sr in boss.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr == null || sr.sprite == null) continue;
                bool body = sr.gameObject.name == "Body" || sr.sprite.name.ToLower().Contains("body");
                if (!body) continue;
                float w = sr.sprite.rect.width / sr.sprite.pixelsPerUnit * Mathf.Abs(sr.transform.lossyScale.x);
                if (w > bw) { bw = w; best = sr; }
            }
            if (best == null) return -1f;
            float seen = Seen(best);
            bool readable = false; try { readable = best.sprite.texture != null && best.sprite.texture.isReadable; } catch { }
            if (!readable) seen *= 0.85f;                                           // (an unread picture: its blank margins off, as the guardian's 28 of 34)
            SanctumPlugin.Log.LogInfo("Sanctum: champion yardstick " + boss.name + ": body " + best.sprite.name + " " + seen.ToString("0.000") + " (" + best.sprite.pixelsPerUnit + " ppu, one pixel " + (Mathf.Abs(best.transform.lossyScale.x) / best.sprite.pixelsPerUnit * LevelGrowth).ToString("0.0000") + ")");
            return seen;
        }

        /// how wide the body of a game champion is in the world, the skeleton giant's and the zombie guardian's on average
        public static float ChampionBody()
        {
            if (champBody > 0f) return champBody;
            float sum = 0f; int n = 0;
            try
            {
                foreach (var b in Resources.LoadAll<BossAI_H_SkeletonGiant>("")) { float w = RefBody(b); if (w > 0.05f) { sum += w; n++; break; } }
                foreach (var b in Resources.LoadAll<BossAI_H_ZombieGuardian>("")) { float w = RefBody(b); if (w > 0.05f) { sum += w; n++; break; } }
            }
            catch (System.Exception e) { SanctumPlugin.Log.LogWarning("Sanctum: champion yardstick: " + e.Message); }
            champBody = n > 0 ? sum / n * LevelGrowth : 1.2f;
            SanctumPlugin.Log.LogInfo("Sanctum: a champion's body is " + champBody.ToString("0.000") + " wide (" + n + " of the game's champions measured)");
            return champBody;
        }


        // (0.41.0, the user) the general as big as the game's goblin captain champion (BossAI_H_GoblinOfficer),
        // measured the same way; the old yardstick if the captain can't be read
        static float goblinBody = -1f;
        public static float GoblinCaptainBody()
        {
            if (goblinBody > 0f) return goblinBody;
            float w = -1f;
            try { foreach (var b in Resources.LoadAll<BossAI_H_GoblinOfficer>("")) { w = RefBody(b); if (w > 0.05f) break; } }
            catch (System.Exception e) { SanctumPlugin.Log.LogWarning("Sanctum: goblin captain yardstick: " + e.Message); }
            goblinBody = w > 0.05f ? w * LevelGrowth : ChampionBody();
            SanctumPlugin.Log.LogInfo("Sanctum: the goblin captain's body is " + goblinBody.ToString("0.000") + (w > 0.05f ? "" : " (not measured: the champions' size)"));
            return goblinBody;
        }
        /// a champion made as big as the game's champions, and kept that size
        public static void FitChampion(GameObject go, float body = -1f)
        {
            if (go == null) return;
            float me = Seen(BodyOf(go)), want = body > 0f ? body : ChampionBody();
            if (me <= 0.01f) return;
            float k = Mathf.Clamp(want / me, 0.4f, 4f);
            go.transform.localScale = go.transform.localScale * k;
            var lk = go.GetComponent<ScaleLock>() ?? go.AddComponent<ScaleLock>();
            lk.Size = go.transform.localScale;
            SanctumPlugin.Log.LogInfo("Sanctum: " + go.name + " sized as the game's champions (body " + me.ToString("0.00") + " -> " + (me * k).ToString("0.00") + ")");
        }
    }
}

namespace OccultSanctum
{
    /// keeps a champion the size he was given (nothing of the game's grows or shrinks him after)
    public class ScaleLock : UnityEngine.MonoBehaviour
    {
        public UnityEngine.Vector3 Size;
        void LateUpdate() { if (Size != UnityEngine.Vector3.zero && transform.localScale != Size) transform.localScale = Size; }
    }
}
