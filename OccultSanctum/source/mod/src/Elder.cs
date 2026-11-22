using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OccultSanctum
{
    /// The cult's elder (Верховный культист): an elite cultist - a man dressed like the cultists (their pictures) but gilded
    /// for his rank and a little bigger than the crossbowman, with a great staff topped by a horned skull holding a red
    /// orb. Under the clothes he is the game's ogre shaman (its moves, its mind and its spells - the three spheres), its
    /// spheres turned cursed red like the cultists'.
    /// In the hall he stands on the pentagram and now and then preaches: the others gather before him and listen.
    public static class Elder
    {
        public const string Id = "OccultElder";
        static GameObject prefab;
        static bool looked;

        static GameObject Prefab()
        {
            if (!looked)
            {
                looked = true;
                prefab = Prisoner.FindMob(new[] { "OgrMage", "Ogr_Mage", "OgreMage", "OgrShaman" }, "Ogr", "Mage");
                SanctumPlugin.Log.LogInfo("Sanctum: the elder is made from " + (prefab != null ? prefab.name : "nothing (no ogre shaman found)"));
            }
            return prefab;
        }

        public static GameObject PrefabForBestiary() => Prefab();
        public static void DressForBestiary(GameObject go, Mob mob) => Dress(go, mob);

        public static Mob Spawn(Vector2 pos)
        {
            var p = Prefab();
            if (p == null) return null;
            var go = Builder.Spawn(p, pos, UnityEngine.Random.Range(0f, 360f));
            if (go == null) return null;
            Builder.Own(go);
            foreach (var rs in go.GetComponentsInChildren<RandomSprite>(true)) UnityEngine.Object.DestroyImmediate(rs);
            var mob = go.GetComponent<Mob>();
            if (mob == null) { UnityEngine.Object.Destroy(go); return null; }
            mob.lvl = Mathf.Max(1, Generation.GetRandomMobLvl() + 1);
            mob.Init();
            Dress(go, mob);
            Sizer.Fit(go, Sizer.Elder);
            var ai = go.GetComponent<AIController>();
            if (ai != null) try { GameManager.Instance.AddMob(ai); } catch { }
            SanctumPlugin.Log.LogInfo("Sanctum: the elder stands at " + pos);
            return mob;
        }

        const float PartPpu = 26f;                        // the cultist's body and hood at the ogre's own scale (the whole frame is made smaller)

        static Vector2 Piv(Sprite like) => like != null && like.rect.width > 0 ? new Vector2(like.pivot.x / like.rect.width, like.pivot.y / like.rect.height) : new Vector2(0.5f, 0.5f);

        static void Dress(GameObject go, Mob mob)
        {
            go.name = "CultElder";
            if (go.GetComponent<ElderTag>() == null) go.AddComponent<ElderTag>();
            SpriteRenderer headSr = null; Transform body = null;
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
            {
                string n = sr.gameObject.name;
                if (n == "HandEquip_R") { sr.sprite = null; sr.enabled = false; continue; }              // the shaman's little thing in the other hand: gone
                if (n == "HandEquip_L")                                                                    // the great staff
                {
                    var st = Art.Part("elder_staff", sr.sprite); if (st != null) sr.sprite = st;
                    continue;
                }
                if (n == "Head") { headSr = sr; continue; }
                // a man's parts: the cultist's pictures (gilded), at a man's size - not the ogre's
                // the arms keep the ogre's own shapes (so the joints meet), in black sleeves with gold cuffs and pale hands
                string arm = n.StartsWith("UpperArm", StringComparison.Ordinal) ? "elder_oupperarm" : n.StartsWith("Arm_", StringComparison.Ordinal) ? "elder_oforearm"
                           : n.StartsWith("Hand_", StringComparison.Ordinal) ? "elder_ohand" : n.StartsWith("Leg_", StringComparison.Ordinal) ? "elder_oleg" : null;
                if (arm != null) { var a = Art.Part(arm, sr.sprite); if (a != null) sr.sprite = a; continue; }
                string art = n == "Body" ? "elder_body" : null;
                if (art == null) continue;
                var s = Art.Pivoted(art, Piv(sr.sprite), PartPpu);
                if (s != null) sr.sprite = s;
                if (n == "Body") body = sr.transform;
            }
            // his head (the hood) sits on his shoulders like a man's - not out in front like the ogre's big head
            if (headSr != null)
            {
                var hd = Art.Pivoted("elder_head_small", new Vector2(0.5f, 0.5f), PartPpu);   // a little smaller (the user; elder_head_small.py)
                if (body != null && hd != null)
                {
                    var hood = new GameObject("Hood");
                    hood.transform.SetParent(body, false);
                    hood.transform.localPosition = new Vector3(0f, 0.14f, 0f);
                    var hs = hood.AddComponent<SpriteRenderer>();
                    hs.sprite = hd; hs.sortingLayerID = headSr.sortingLayerID; hs.sortingOrder = headSr.sortingOrder; hs.sharedMaterial = headSr.sharedMaterial;
                    headSr.sprite = null; headSr.enabled = false;
                }
                else if (hd != null) headSr.sprite = hd;
            }
            foreach (var lr in go.GetComponentsInChildren<LineRenderer>(true)) { var c = new Color(0.06f, 0.05f, 0.08f, 1f); lr.startColor = c; lr.endColor = c; }   // the legs drawn by lines: black
            try
            {
                var dead = Art.Pivoted("elder_dead", new Vector2(0.5f, 0.5f), 26f);   // a little bigger (the user)
                if (dead != null) mob.deadBodySprites = new[] { dead };
            }
            catch { }
            try { mob.creatureLocalizedName = "Sanctum/Elder"; } catch { }
            mob.creatureID = Id;
            Human.Apply(mob);                                                      // a man's voice, red blood
            mob.customPreviewSprite = Snapshot.Of(go, mob.creatureID, Art.Pivoted("elder_preview", new Vector2(0.5f, 0.5f), 36f));   // taken from himself (the user: the hand-made ones were crooked)
        }
    }

    public class ElderTag : MonoBehaviour { }

    /// the elder's sermon: most of those about the hall leave what they are doing, gather in a half ring before him and
    /// look at him; those on the benches turn to him. When he is done they go back to their business.
    public static class Sermon
    {
        public static Activity Speaker;
        public static bool On;
        static Vector2 at;
        static readonly Dictionary<Activity, Vector2> spots = new Dictionary<Activity, Vector2>();
        static readonly HashSet<Activity> sitters = new HashSet<Activity>();

        public static Vector2 At => at;

        /// is this one listening; where he stands (a sitter: his seat - he only turns)
        public static bool Hears(Activity a, out Vector2 spot, out bool seated)
        {
            spot = default; seated = false;
            if (!On || Speaker == null) return false;
            if (sitters.Contains(a)) { seated = true; return true; }
            return spots.TryGetValue(a, out spot);
        }

        public static void Begin(Activity speaker, Vector2 center, Vector2 face)
        {
            End();
            Speaker = speaker; at = center; On = true;
            var near = UnityEngine.Object.FindObjectsOfType<Activity>()
                .Where(a => a != null && a != speaker && a.What != Activity.Kind.Execute && a.What != Activity.Kind.Preach && !(a.What == Activity.Kind.Follow && a.Leader == speaker.transform)
                            && a.GetComponent<SpearmanTag>() == null && a.GetComponent<Marksman>() == null && ((Vector2)a.transform.position - center).sqrMagnitude < 18f * 18f)
                .OrderBy(a => ((Vector2)a.transform.position - center).sqrMagnitude).ToList();
            // the places before him: half rings, nearest first, each one free of walls and things
            var free = new List<Vector2>();
            float fa = Mathf.Atan2(face.y, face.x);
            foreach (float r in new[] { 2.6f, 3.5f, 4.4f, 5.3f, 6.2f })
            {
                int n = Mathf.Max(3, Mathf.FloorToInt(r * 2.4f / 1.05f));
                for (int i = 0; i < n; i++)
                {
                    float a = fa - 1.2f + 2.4f * (i + .5f) / n;
                    Vector2 p = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                    if (GameManager.Instance.ObstacleToTarget(center, p)) continue;
                    if (Physics2D.OverlapCircleAll(p, 0.35f, 35651840).Any(c => c != null && !c.isTrigger && c.GetComponentInParent<Creature>() == null)) continue;
                    free.Add(p);
                }
            }
            int come = 0, turn = 0;
            // the soldiers decide by groups (a patrol comes as a whole or not at all); few of them come
            var groupComes = new Dictionary<Activity, bool>();
            bool Soldier(Activity x) => x.GetComponent<SpearmanTag>() != null || x.GetComponent<Marksman>() != null;
            foreach (var a in near)
            {
                if (a.What == Activity.Kind.Sit) { sitters.Add(a); turn++; continue; }
                if (Soldier(a))
                {
                    if (a.What == Activity.Kind.Train || a.What == Activity.Kind.Shoot) { if (UnityEngine.Random.value > 0.1f) continue; }
                    else
                    {
                        var lead = a.What == Activity.Kind.Follow && a.Leader != null ? a.Leader.GetComponent<Activity>() : a;
                        if (lead == null) lead = a;
                        if (!groupComes.TryGetValue(lead, out bool yes)) groupComes[lead] = yes = UnityEngine.Random.value < 0.25f;
                        if (!yes) continue;
                    }
                }
                else if (UnityEngine.Random.value < 0.15f) continue;               // not quite every cultist either
                if (free.Count == 0) break;
                Vector2 me = a.transform.position;
                var best = free.OrderBy(p => (p - center).sqrMagnitude * 0.6f + (p - me).sqrMagnitude * 0.15f).First();
                free.Remove(best); spots[a] = best; come++;
            }
            SanctumPlugin.Log.LogInfo($"Sanctum: the elder preaches - {come} come to listen, {turn} turn to him from the benches");
        }

        public static void End()
        {
            On = false; Speaker = null; spots.Clear(); sitters.Clear();
        }
    }
}
