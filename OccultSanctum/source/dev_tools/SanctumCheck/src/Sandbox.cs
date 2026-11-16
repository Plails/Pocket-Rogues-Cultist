using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace SanctumCheck
{
    /// Test sandbox: every PlayerPrefs write goes to memory only (reads fall through to the real save).
    /// FileBasedPrefs is built on PlayerPrefs, so the real save is never changed while this is active.
    public static class Sandbox
    {
        public static readonly Dictionary<string, object> Over = new Dictionary<string, object>();
        public static readonly HashSet<string> Deleted = new HashSet<string>();
        public static int Writes;
        public static bool Verified;

        public static void Install(Harmony h)
        {
            var t = typeof(PlayerPrefs);
            void P(string name, Type[] args, string prefix) =>
                h.Patch(AccessTools.Method(t, name, args), prefix: new HarmonyMethod(typeof(Sandbox), prefix));
            P("SetInt", new[] { typeof(string), typeof(int) }, nameof(SetInt));
            P("SetFloat", new[] { typeof(string), typeof(float) }, nameof(SetFloat));
            P("SetString", new[] { typeof(string), typeof(string) }, nameof(SetString));
            P("GetInt", new[] { typeof(string), typeof(int) }, nameof(GetInt2));
            P("GetInt", new[] { typeof(string) }, nameof(GetInt1));
            P("GetFloat", new[] { typeof(string), typeof(float) }, nameof(GetFloat2));
            P("GetFloat", new[] { typeof(string) }, nameof(GetFloat1));
            P("GetString", new[] { typeof(string), typeof(string) }, nameof(GetString2));
            P("GetString", new[] { typeof(string) }, nameof(GetString1));
            P("HasKey", new[] { typeof(string) }, nameof(HasKey));
            P("DeleteKey", new[] { typeof(string) }, nameof(DeleteKey));
            P("DeleteAll", Type.EmptyTypes, nameof(Block));
            P("Save", Type.EmptyTypes, nameof(Block));
            // no achievements / leaderboards from a test
            TryPatch(h, AccessTools.Method(AccessTools.TypeByName("SteamStatsAndAchievements"),"UnlockAchievement", new[] { typeof(string) }));
            TryPatch(h, AccessTools.Method(typeof(GameManager), "SendSteamScore"));
            TryPatch(h, AccessTools.Method(typeof(GameManager), "SaveLeaderboards"), nameof(BlockBool));
        }

        static void TryPatch(Harmony h, System.Reflection.MethodInfo m, string prefix = nameof(Block))
        {
            if (m == null) return;
            try { h.Patch(m, prefix: new HarmonyMethod(typeof(Sandbox), prefix)); } catch (Exception e) { Debug.LogWarning("Sandbox patch " + m.Name + ": " + e.Message); }
        }

        static void Put(string k, object v) { if (k == null) return; Over[k] = v; Deleted.Remove(k); Writes++; }
        static bool SetInt(string key, int value) { Put(key, value); return false; }
        static bool SetFloat(string key, float value) { Put(key, value); return false; }
        static bool SetString(string key, string value) { Put(key, value ?? ""); return false; }
        static bool Block() => false;
        static bool BlockBool(ref bool __result) { __result = false; return false; }

        static bool Get<T>(string key, T def, ref T result)
        {
            if (key == null) return true;
            if (Deleted.Contains(key)) { result = def; return false; }
            if (Over.TryGetValue(key, out var v)) { result = v is T tv ? tv : def; return false; }
            return true;
        }
        static bool GetInt2(string key, int defaultValue, ref int __result) => Get(key, defaultValue, ref __result);
        static bool GetInt1(string key, ref int __result) => Get(key, 0, ref __result);
        static bool GetFloat2(string key, float defaultValue, ref float __result) => Get(key, defaultValue, ref __result);
        static bool GetFloat1(string key, ref float __result) => Get(key, 0f, ref __result);
        static bool GetString2(string key, string defaultValue, ref string __result) => Get(key, defaultValue, ref __result);
        static bool GetString1(string key, ref string __result) => Get(key, "", ref __result);
        static bool HasKey(string key, ref bool __result)
        {
            if (key == null) return true;
            if (Deleted.Contains(key)) { __result = false; return false; }
            if (Over.ContainsKey(key)) { __result = true; return false; }
            return true;
        }
        static bool DeleteKey(string key) { if (key != null) { Over.Remove(key); Deleted.Add(key); } return false; }

        /// real registry value names of the game (read through reflection: netstandard has no Registry API)
        public static string[] RegistryNames()
        {
            var reg = Type.GetType("Microsoft.Win32.Registry, mscorlib");
            var cu = reg.GetField("CurrentUser").GetValue(null);
            var sub = cu.GetType().GetMethod("OpenSubKey", new[] { typeof(string) })
                .Invoke(cu, new object[] { @"Software\" + Application.companyName + @"\" + Application.productName });
            if (sub == null) return new string[0];
            return (string[])sub.GetType().GetMethod("GetValueNames").Invoke(sub, null);
        }

        /// write a probe key and check it did NOT reach the registry
        public static bool Verify(out string why)
        {
            string key = "SanctumSandboxProbe_" + DateTime.Now.Ticks;
            PlayerPrefs.SetInt(key, 7);
            PlayerPrefs.Save();
            bool inMem = PlayerPrefs.GetInt(key, 0) == 7;
            string[] names;
            try { names = RegistryNames(); }
            catch (Exception e) { why = "registry read failed: " + e.Message; return false; }
            bool leaked = names.Any(n => n.StartsWith(key, StringComparison.Ordinal));
            why = $"inMemory={inMem} leaked={leaked} regValues={names.Length}";
            Verified = inMem && !leaked && names.Length > 50;
            return Verified;
        }
    }
}

