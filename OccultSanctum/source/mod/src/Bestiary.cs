using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace OccultSanctum
{
    /// The sanctum in the game's bestiary, like the crypt or the rat hole: a location of its own (shown once you have
    /// been there), and the monsters met in it - the cultist and the winged devil (entries of their own: their kills
    /// count under their own ids), and the skeleton, the rotfiend and the zombie prisoner from its cages.
    public static class Bestiary
    {
        public const int LocId = 731;                               // our own Locations value (the game's go up to 153)
        public static Locations Loc => (Locations)LocId;
        public const string CultistId = "OccultCultist", DevilId = "OccultDevil";
        static GameObject holder;
        static Mob cultistEntry, devilEntry, xbowEntry, spearEntry, elderEntry, champEntry, magusEntry;

        public static LocationData Data => new LocationData(Loc, SanctumPlugin.CfgFloorFrom.Value, SanctumPlugin.CfgFloorTo.Value);

        /// the sanctum counts as visited (the bestiary shows a location only after that)
        public static void Visited() { try { FileBasedPrefs.SetInt(Loc.ToString(), 1); } catch { } }

        static bool Has(Mob m) => m != null && m.locations != null && m.locations.Any(l => l.location == Loc);

        static void AddLoc(Mob m) { if (m != null && !Has(m)) { if (m.locations == null) m.locations = new List<LocationData>(); m.locations.Add(Data); } }

        /// a bestiary-only copy of the dressed monster: made under a switched-off holder, so it never wakes up
        static Mob Entry(GameObject prefab, string id, Action<GameObject, Mob> dress)
        {
            if (prefab == null) return null;
            if (holder == null) { holder = new GameObject("OccultSanctum_BestiaryEntries"); holder.SetActive(false); UnityEngine.Object.DontDestroyOnLoad(holder); }
            var go = UnityEngine.Object.Instantiate(prefab, holder.transform);
            foreach (var rs in go.GetComponentsInChildren<RandomSprite>(true)) UnityEngine.Object.DestroyImmediate(rs);
            var mob = go.GetComponent<Mob>();
            if (mob == null) return null;
            dress(go, mob);
            mob.creatureID = id;
            mob.locations = new List<LocationData> { Data };
            return mob;
        }

        public static void Prepare(List<Mob> mobs, List<Mob> bosses = null)
        {
            if (cultistEntry == null) cultistEntry = Entry(Cultist.PrefabForBestiary(), CultistId, Cultist.DressForBestiary);
            if (devilEntry == null) devilEntry = Entry(Devil.PrefabForBestiary(), DevilId, Devil.DressForBestiary);
            if (xbowEntry == null) xbowEntry = Entry(Crossbowman.PrefabForBestiary(), Crossbowman.Id, Crossbowman.DressForBestiary);
            if (spearEntry == null) spearEntry = Entry(Spearman.PrefabForBestiary(), Spearman.Id, Spearman.DressForBestiary);
            if (elderEntry == null) elderEntry = Entry(Elder.PrefabForBestiary(), Elder.Id, Elder.DressForBestiary);
            if (champEntry == null && Champion.Enabled) champEntry = Entry(Champion.PrefabForBestiary(), Champion.Id, Champion.DressForBestiary);
            foreach (var e in new[] { cultistEntry, devilEntry, xbowEntry, spearEntry, elderEntry }) if (e != null && !mobs.Contains(e)) mobs.Add(e);
            // the general is a champion: on the bosses' page with the game's own champions (the user)
            if (magusEntry == null) magusEntry = Entry(Magus.PrefabForBestiary(), Magus.Id, Magus.DressForBestiary);
            if (magusEntry != null)
            {
                if (bosses != null) { mobs.Remove(magusEntry); if (!bosses.Contains(magusEntry)) bosses.Add(magusEntry); }   // a champion: with the bosses
                else if (!mobs.Contains(magusEntry)) mobs.Add(magusEntry);
            }
            if (champEntry != null)
            {
                if (bosses != null) { mobs.Remove(champEntry); if (!bosses.Contains(champEntry)) bosses.Add(champEntry); }
                else if (!mobs.Contains(champEntry)) mobs.Add(champEntry);
            }
            // the cages' prisoners live here too
            foreach (var kind in new[] { "skel", "rot", "zomb" })
            {
                var p = Prisoner.PrefabOf(kind);
                var pm = p != null ? p.GetComponent<Mob>() : null;
                if (pm == null) continue;
                AddLoc(pm);
                foreach (var m in mobs) if (m != null && m.creatureID == pm.creatureID) AddLoc(m);
            }
        }
    }

    /// the bestiary's monster list gets our entries before it is first sorted
    [HarmonyPatch(typeof(Your.Namespace.Here.UniqueStringHereToAvoidNamespaceConflicts.Lists.BasicListAdapterBestiary), "Start")]
    internal static class BestiaryMobsPatch
    {
        static void Prefix(Your.Namespace.Here.UniqueStringHereToAvoidNamespaceConflicts.Lists.BasicListAdapterBestiary __instance)
        {
            try { Bestiary.Prepare(__instance.Mobs, __instance.Bosses); }
            catch (Exception e) { SanctumPlugin.Log.LogWarning("Sanctum bestiary: " + e.Message); }
        }
    }

    /// the location list gets the sanctum, with its icon
    [HarmonyPatch(typeof(BestiaryLocationsSelector), nameof(BestiaryLocationsSelector.Initialize))]
    internal static class BestiaryLocationPatch
    {
        static void Prefix(BestiaryLocationsSelector __instance)
        {
            try
            {
                if (!__instance.locations.Any(l => l.location == Bestiary.Loc)) __instance.locations.Add(Bestiary.Data);
                var det = BestiaryDetailsManager.Instance;
                if (det != null && det.allLocationIcons != null) det.allLocationIcons[Bestiary.Loc] = Art.Get("occ_icon");
            }
            catch (Exception e) { SanctumPlugin.Log.LogWarning("Sanctum bestiary location: " + e.Message); }
        }
    }

    /// goblin assassins throw knives; our devils do not - they only claw
    [HarmonyPatch(typeof(Controller), nameof(Controller.ThrowKnives))]
    internal static class DevilNoKnivesPatch
    {
        static bool Prefix(Controller __instance)
        {
            try { return __instance.GetComponent<DevilTag>() == null; } catch { return true; }
        }
    }
}
