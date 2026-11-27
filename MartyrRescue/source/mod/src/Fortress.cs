using System;
using System.Collections;
using HarmonyLib;
using UnityEngine;

namespace CultistRescue
{
    /// the fortress: the Cultist's building cannot be built until he is rescued (its description says where he is);
    /// after the rescue the game's own unlock screen shows his portrait - "Unlocked: Cultist!"
    [HarmonyPatch(typeof(BaseManager), nameof(BaseManager.ButBuy))]
    internal static class BuyLockPatch
    {
        static bool Prefix(BaseManager __instance)
        {
            try
            {
                var db = __instance.buildingsDB;
                int i = Traverse.Create(__instance).Field("selectedIndex").GetValue<int>();
                if (db == null || i < 0 || i >= db.Count) return true;
                var b = db[i];
                if (b == null || b.nameID != Unlock.BuildingId || b.lvl > 0 || Unlock.IsOpen()) return true;
                RescuePlugin.Log.LogInfo("Cultist Rescue: the Cultist's building is locked until he is rescued");
                return false;
            }
            catch (Exception e) { RescuePlugin.Log.LogWarning("Cultist Rescue buy lock: " + e.Message); return true; }
        }
    }

    /// (the user, 2026-11-27) until he is rescued his building is not in the guild at all - no icon, no corridor
    [HarmonyPatch(typeof(Building), nameof(Building.LoadBuilding))]
    [HarmonyPriority(Priority.Last)]
    internal static class HideUntilRescuedPatch
    {
        static void Postfix(Building __instance)
        {
            try
            {
                if (__instance == null || __instance.nameID != Unlock.BuildingId) return;
                bool open = __instance.lvl > 0 || Unlock.IsOpen();
                if (__instance.gameObject.activeSelf != open) __instance.gameObject.SetActive(open);
                if (!open && __instance.corridor != null)
                    foreach (var c in __instance.corridor) if (c != null) c.SetActive(false);
            }
            catch (Exception e) { RescuePlugin.Log.LogWarning("Cultist Rescue hide building: " + e.Message); }
        }
    }

    [HarmonyPatch(typeof(Building), nameof(Building.GetLocalizedDescription))]
    internal static class LockedDescriptionPatch
    {
        static void Postfix(Building __instance, ref string __result)
        {
            try
            {
                if (__instance == null || __instance.nameID != Unlock.BuildingId || __instance.lvl > 0 || Unlock.IsOpen()) return;
                __result += Unlock.Russian()
                    ? "\n<color=orange>Мученик томится в клетке в Оккультном святилище. Спаси его, чтобы открыть класс.</color>"
                    : "\n<color=orange>The Martyr languishes in a cage in the Occult Sanctum. Rescue him to unlock the class.</color>";
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(BaseManager), "Start")]
    internal static class UnlockScreenPatch
    {
        static void Postfix(BaseManager __instance)
        {
            try
            {
                if (FileBasedPrefs.GetInt(Unlock.ShowKey, 0) != 1) return;
                Runner.I?.StartCoroutine(Show(__instance));
            }
            catch (Exception e) { RescuePlugin.Log.LogWarning("Cultist Rescue unlock screen: " + e.Message); }
        }

        static IEnumerator Show(BaseManager bm)
        {
            yield return new WaitForSeconds(1.5f);
            if (bm == null || bm.UnlocksScreen == null) yield break;
            try
            {
                string word;
                try { word = I2.Loc.LocalizationManager.GetTranslation("UI/Other/MessageUnlocked"); } catch { word = null; }
                if (string.IsNullOrEmpty(word)) word = Unlock.Russian() ? "Разблокировано" : "Unlocked";
                string who = Unlock.Russian() ? "МУЧЕНИК" : "MARTYR";
                string desc = Unlock.Russian()
                    ? "Спасённый из клетки Оккультного святилища, он искупает грехи своей кровью. Его обитель уже стоит в крепости - выберите его в таверне героев и прокачивайте."
                    : "Rescued from a cage of the Occult Sanctum, he atones for sins with his own blood. His abode already stands in the fortress - choose him among the heroes and level him up.";
                bm.UnlocksScreen.Show(Pics.Portrait(), "<color=orange>" + word.ToUpper() + ": </color>" + who + "<color=orange>!</color>", desc);
                FileBasedPrefs.SetInt(Unlock.ShowKey, 0);
                FileBasedPrefs.ManualySave();
                RescuePlugin.Log.LogInfo("Cultist Rescue: the unlock screen shown in the fortress");
            }
            catch (Exception e) { RescuePlugin.Log.LogWarning("Cultist Rescue unlock screen: " + e.Message); }
        }
    }
}
