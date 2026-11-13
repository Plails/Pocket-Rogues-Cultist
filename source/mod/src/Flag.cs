using System;
using System.Linq;
using System.Text.RegularExpressions;
using HarmonyLib;
using UnityEngine;

namespace FlagellantMod
{
    /// "Is the current hero the Flagellant?" The Flagellant runs on the Berserker's engine (class 4)
    /// but keeps all of its data under its own number (6), and has its own guild building ("flg").
    internal static class Flag
    {
        public const int BaseClass = 4;              // CharacterClasses.BERSERKER
        public const int OwnIndex = 6;
        public const string BuildingId = "flg";
        public const string BuildingKey = "bld_" + BuildingId;
        private const string StateKey = "FLG_isActive";
        private static bool loaded, active;

        public static bool Active
        {
            get
            {
                if (!loaded)
                {
                    loaded = true;
                    try { active = FileBasedPrefs.GetInt(StateKey, 0) == 1 && FileBasedPrefs.GetInt("curChar", 0) == BaseClass; }
                    catch { active = false; }
                    ApplyStatics();
                }
                return active;
            }
        }

        public static void Set(bool on)
        {
            loaded = true;
            if (active == on) return;
            active = on;
            try { FileBasedPrefs.SetInt(StateKey, on ? 1 : 0); } catch { }
            ApplySkills();
            FlagellantPlugin.Log.LogInfo("Flagellant " + (on ? "selected" : "off"));
        }

        /// the skill tree, the weapon lists and the building the game reads for the Berserker
        public static void ApplySkills()
        {
            ApplyStatics();
            if (FlagSkills.Data != null)
                foreach (var db in Resources.FindObjectsOfTypeAll<SkillsDataBase>())
                {
                    if (db == null) continue;
                    FlagSkills.RememberOriginal(db);
                    db.skills_berserk = Active ? FlagSkills.Data : FlagSkills.OriginalOf(db);
                    try { db.skills_berserk.Load(); } catch { }
                    try { (AccessTools.Field(typeof(SkillsDataBase), "cachedSkills")?.GetValue(db) as System.Collections.IDictionary)?.Clear(); } catch { }
                }
            try { Weapons.Apply(Active); } catch (Exception e) { FlagellantPlugin.Log.LogWarning("Weapons apply: " + e.Message); }
        }

        static void ApplyStatics()
        {
            try
            {
                if (CharacterStatsManager.CharactersBuidings != null && CharacterStatsManager.CharactersBuidings.Length > BaseClass)
                    CharacterStatsManager.CharactersBuidings[BaseClass] = active ? BuildingKey : "bld_bers";
                if (ChoseCharsManager.buildingNames != null && ChoseCharsManager.buildingNames.Length > BaseClass)
                    ChoseCharsManager.buildingNames[BaseClass] = active ? "ClassFlagellant" : "ClassBerserker";
            }
            catch { }
        }

        private static readonly Regex[] IndexKeys =
        {
            new Regex(@"^(curPoints|realLvl_|CurInventory_|CurLocation_|NeedLoading_ReturnToTheFortress_|NewShield|NewWeapon|pet|curPrice|towerBossLvlCounter)4$", RegexOptions.Compiled),
            new Regex(@"^(curSkill)4(_spd|_str|_spc)$", RegexOptions.Compiled),
            new Regex(@"^(CurClearedChar_|CurDeepChar_|CurScoreChar_|addClearedFloors_|raidBaseline_)4(_.*)$", RegexOptions.Compiled),
            new Regex(@"^()4(_raid.*)$", RegexOptions.Compiled),
            new Regex(@"^([A-Za-z]+)4(_hp|_dmg|_def)$", RegexOptions.Compiled),
        };

        private static int bypass;
        public static int BuildingScope;              // inside the character screen: the Berserker's building means ours
        public static T Raw<T>(Func<T> read)
        {
            bypass++;
            try { return read(); } finally { bypass--; }
        }

        public static string Remap(string key)
        {
            if (bypass > 0 || key == null || key.Length < 2 || key.StartsWith("FLG_") || !Active) return key;
            if (BuildingScope > 0 && key == "bld_bers") return BuildingKey;
            if (key.StartsWith("BERSERKER_")) return "FLAGELLANT_" + key.Substring(10);
            foreach (var r in IndexKeys)
            {
                var m = r.Match(key);
                if (!m.Success) continue;
                return m.Groups.Count > 2 ? m.Groups[1].Value + OwnIndex + m.Groups[2].Value : m.Groups[1].Value + OwnIndex;
            }
            return key;
        }
    }

    [HarmonyPatch(typeof(SkillsDataBase), nameof(SkillsDataBase.Load))]
    internal static class SkillsLoadPatch
    {
        static void Prefix(SkillsDataBase __instance)
        {
            if (FlagSkills.Data == null || __instance == null) return;
            FlagSkills.RememberOriginal(__instance);
            __instance.skills_berserk = Flag.Active ? FlagSkills.Data : FlagSkills.OriginalOf(__instance);
        }
    }

    [HarmonyPatch]
    internal static class SaveKeysPatch
    {
        static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            foreach (var name in new[] { "GetInt", "SetInt", "GetString", "SetString", "GetFloat", "SetFloat", "HasKey", "DeleteKey" })
                foreach (var m in AccessTools.GetDeclaredMethods(typeof(FileBasedPrefs)).Where(x => x.Name == name && x.IsStatic && x.GetParameters().Length > 0 && x.GetParameters()[0].ParameterType == typeof(string)))
                    yield return m;
        }

        static void Prefix(ref string __0)
        {
            try { __0 = Flag.Remap(__0); } catch { }
        }
    }

    /// the skill-level cap of the character screen reads the Flagellant's own building
    [HarmonyPatch(typeof(ChoseCharsManager), nameof(ChoseCharsManager.RedrawSkills))]
    internal static class RedrawScopePatch
    {
        static void Prefix() => Flag.BuildingScope++;
        static Exception Finalizer(Exception __exception) { Flag.BuildingScope = Math.Max(0, Flag.BuildingScope - 1); return __exception; }
    }
}
