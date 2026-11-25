using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OccultSanctum
{
    /// (the user, 2026-11-25: "put my animation Сфера_культист on the ordinary cultist when he shoots his sphere") an
    /// animation made in the user's animation editor (F6 in the guild), read from Anims/<name>.json next to this plugin or
    /// from the desktop folder the editor saves to; the newest file is read again whenever it changes
    public class UserAnim
    {
        public class Key { public float t, x, y, rot; }
        public float Length = 1f; public bool Smooth = true;
        public readonly List<(string bone, List<Key> keys)> Tracks = new List<(string, List<Key>)>();

        static readonly Dictionary<string, (DateTime when, UserAnim a)> cache = new Dictionary<string, (DateTime, UserAnim)>();

        static string FileOf(string name)
        {
            var mine = Path.Combine(SanctumPlugin.Dir, "Anims", name + ".json");
            var desk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Анимации Pocket Rogues", name + ".json");
            if (File.Exists(mine) && File.Exists(desk)) return File.GetLastWriteTimeUtc(desk) > File.GetLastWriteTimeUtc(mine) ? desk : mine;
            return File.Exists(mine) ? mine : File.Exists(desk) ? desk : null;
        }

        public static UserAnim Get(string name)
        {
            var f = FileOf(name);
            if (f == null) return null;
            var w = File.GetLastWriteTimeUtc(f);
            if (cache.TryGetValue(name, out var c) && c.when == w) return c.a;
            UserAnim a = null;
            try { a = Parse(File.ReadAllText(f)); } catch (Exception e) { SanctumPlugin.Log.LogWarning("Sanctum: animation " + name + ": " + e.Message); }
            cache[name] = (w, a);
            SanctumPlugin.Log.LogInfo("Sanctum: animation " + name + " read from " + f + ": " + (a == null ? "unreadable" : a.Tracks.Count + " parts, " + a.Tracks.Sum(t => t.keys.Count) + " keys"));
            return a;
        }

        /// where a part stands at time t (between its keys)
        public static Key Sample(List<Key> keys, float t, bool smooth)
        {
            if (keys.Count == 0) return null;
            if (t <= keys[0].t) return keys[0];
            var last = keys[keys.Count - 1];
            if (t >= last.t) return last;
            for (int i = 0; i < keys.Count - 1; i++)
            {
                var a = keys[i]; var b = keys[i + 1];
                if (t < a.t || t > b.t) continue;
                float k = b.t - a.t < 1e-4f ? 1f : (t - a.t) / (b.t - a.t);
                if (smooth) k = k * k * (3f - 2f * k);
                return new Key { t = t, x = Mathf.Lerp(a.x, b.x, k), y = Mathf.Lerp(a.y, b.y, k), rot = Mathf.LerpAngle(a.rot, b.rot, k) };
            }
            return last;
        }

        // ---- the editor's file (written by hand there; read here the same way)
        static UserAnim Parse(string json)
        {
            var root = new Reader(json).Value() as Dictionary<string, object>;
            if (root == null) return null;
            var a = new UserAnim();
            if (root.TryGetValue("length", out var l) && l is double ld) a.Length = Mathf.Max(0.05f, (float)ld);
            if (root.TryGetValue("smooth", out var sm) && sm is bool sb) a.Smooth = sb;
            if (root.TryGetValue("tracks", out var tv) && tv is List<object> tl)
                foreach (var o in tl)
                {
                    if (!(o is Dictionary<string, object> td) || !(td.TryGetValue("bone", out var bn) && bn is string bone)) continue;
                    var keys = new List<Key>();
                    if (td.TryGetValue("keys", out var kv) && kv is List<object> kl)
                        foreach (var ko in kl)
                            if (ko is Dictionary<string, object> kd)
                                keys.Add(new Key { t = N(kd, "t"), x = N(kd, "x"), y = N(kd, "y"), rot = N(kd, "rot") });
                    keys.Sort((p, q) => p.t.CompareTo(q.t));
                    if (keys.Count > 0) a.Tracks.Add((bone, keys));
                }
            return a;
        }
        static float N(Dictionary<string, object> d, string k) => d.TryGetValue(k, out var v) && v is double x ? (float)x : 0f;

        class Reader
        {
            readonly string s; int i;
            public Reader(string s) { this.s = s ?? ""; }
            void Ws() { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }
            public object Value()
            {
                Ws(); if (i >= s.Length) return null;
                char c = s[i];
                if (c == '{') { var d = new Dictionary<string, object>(); i++; while (true) { Ws(); if (i >= s.Length) break; if (s[i] == '}') { i++; break; } if (s[i] == ',') { i++; continue; } var k = Str(); Ws(); if (i < s.Length && s[i] == ':') i++; d[k] = Value(); } return d; }
                if (c == '[') { var l = new List<object>(); i++; while (true) { Ws(); if (i >= s.Length) break; if (s[i] == ']') { i++; break; } if (s[i] == ',') { i++; continue; } l.Add(Value()); } return l; }
                if (c == '"') return Str();
                if (string.CompareOrdinal(s, i, "true", 0, 4) == 0) { i += 4; return true; }
                if (string.CompareOrdinal(s, i, "false", 0, 5) == 0) { i += 5; return false; }
                if (string.CompareOrdinal(s, i, "null", 0, 4) == 0) { i += 4; return null; }
                int st = i; while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
                double.TryParse(s.Substring(st, i - st), NumberStyles.Float, CultureInfo.InvariantCulture, out var dd); return dd;
            }
            string Str()
            {
                var sb = new StringBuilder(); i++;
                while (i < s.Length && s[i] != '"') { if (s[i] == '\\' && i + 1 < s.Length) { i++; sb.Append(s[i] == 'n' ? '\n' : s[i]); } else sb.Append(s[i]); i++; }
                i++; return sb.ToString();
            }
        }
    }

    /// the user's animation laid over the cultist's own while he casts his sphere: the game's animation still runs (the
    /// sphere leaves at its own moment), the parts the user keyed are set after it, the user's animation stretched over
    /// the game's cast
    public class CastAnim : MonoBehaviour
    {
        public string AnimName = "Сфера_культист";
        Animator an;
        readonly Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
        static readonly HashSet<string> seen = new HashSet<string>();

        void Start() { an = GetComponent<Animator>() ?? GetComponentInChildren<Animator>(); }

        static bool IsCast(string n)
        {
            n = n.ToLowerInvariant();
            if (n.Contains("idle") || n.Contains("walk") || n.Contains("run") || n.Contains("death") || n.Contains("die") || n.Contains("hit") || n.Contains("stun") || n.Contains("heal") || n.Contains("special")) return false;   // not the ritual pose, not the beam
            return n.Contains("attack") || n.Contains("cast") || n.Contains("spell") || n.Contains("magic") || n.Contains("shoot") || n.Contains("dist") || n.Contains("wiz");
        }

        void LateUpdate()
        {
            if (an == null || !an.enabled) return;
            AnimatorClipInfo[] info;
            try { info = an.GetCurrentAnimatorClipInfo(0); } catch { return; }
            if (info == null || info.Length == 0 || info[0].clip == null) return;
            string clip = info[0].clip.name;
            if (seen.Add(clip)) SanctumPlugin.Log.LogInfo("Sanctum: a cultist plays the clip " + clip + (IsCast(clip) ? " (the sphere: the user's animation over it)" : ""));
            if (!IsCast(clip)) return;
            var ua = UserAnim.Get(AnimName);
            if (ua == null || ua.Tracks.Count == 0) return;
            float nt = an.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f;
            float t = Mathf.Clamp01(nt) * ua.Length;
            foreach (var (bone, keys) in ua.Tracks)
            {
                if (!bones.TryGetValue(bone, out var tr)) { tr = transform.Find(bone); bones[bone] = tr; }
                if (tr == null) continue;
                var k = UserAnim.Sample(keys, t, ua.Smooth);
                if (k == null) continue;
                tr.localPosition = new Vector3(k.x, k.y, tr.localPosition.z);
                tr.localRotation = Quaternion.Euler(0f, 0f, k.rot);
            }
        }
    }
}
