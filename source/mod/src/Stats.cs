using System;
using HarmonyLib;
using UnityEngine;

namespace FlagellantMod
{
    /// The Flagellant's main attribute is Endurance. The Berserker's damage adds "Strength x0.75" (other classes x0.5);
    /// for the Flagellant that term becomes "Endurance x0.75 + Strength x0.5". Plus +2 HP/s base regeneration.
    internal static class StatsScope
    {
        [ThreadStatic] public static int Damage;
    }

    [HarmonyPatch(typeof(Character), nameof(Character.Damage), MethodType.Getter)]
    internal static class DamageScopePatch
    {
        static void Prefix() => StatsScope.Damage++;
        static Exception Finalizer(Exception __exception) { StatsScope.Damage = Math.Max(0, StatsScope.Damage - 1); return __exception; }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.DamageMiddle), MethodType.Getter)]
    internal static class DamageMiddleScopePatch
    {
        static void Prefix() => StatsScope.Damage++;
        static Exception Finalizer(Exception __exception) { StatsScope.Damage = Math.Max(0, StatsScope.Damage - 1); return __exception; }
    }

    /// inside the damage formula only: the "strength" the Flagellant hits with = Endurance + Strength x (0.5 / 0.75)
    [HarmonyPatch(typeof(Character), nameof(Character.Att_Strength), MethodType.Getter)]
    internal static class MainAttributePatch
    {
        static void Postfix(Character __instance, ref int __result)
        {
            if (StatsScope.Damage <= 0 || FLogic.HeroSelf(__instance) == null) return;
            int saved = StatsScope.Damage;
            StatsScope.Damage = 0;
            try { __result = Mathf.RoundToInt(__instance.Att_Endurance + __result * (0.5f / 0.75f)); }
            catch { }
            finally { StatsScope.Damage = saved; }
        }
    }

    /// +2 HP per second base regeneration (shown in the hero's stats too)
    [HarmonyPatch(typeof(Creature), nameof(Creature.HP_reg), MethodType.Getter)]
    internal static class RegenPatch
    {
        static void Postfix(Creature __instance, ref float __result)
        {
            var ch = FLogic.HeroSelf(__instance);
            if (ch != null) __result += 2f + Perks.VesselRegenBonus(ch);   // + Sinful Vessel lvl 5
        }
    }
}
