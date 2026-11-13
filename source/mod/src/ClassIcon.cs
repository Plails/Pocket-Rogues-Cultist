using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace FlagellantMod
{
    /// The Cultist's class icon (the helmet of his building) wherever the game shows the Berserker's axe:
    /// his card in the hero list, the title of the hero screen, the hero panel in a run and every other place that
    /// reads the class icon (death screen, inventory preview...).
    internal static class ClassIcon
    {
        public static Sprite Mine => Fortress.Icon();
        static bool On => Flag.Active;
        const int Base = Flag.BaseClass;

        static PlayerInputManager origFor; static Sprite original;

        /// the game reads the Berserker's icon from PlayerInputManager.pointBgBers: ours while the Cultist is chosen
        public static void ApplyHud()
        {
            try
            {
                var pim = PlayerInputManager.Instance;
                if (pim == null) return;
                var f = Traverse.Create(pim).Field("pointBgBers");
                if (origFor != pim) { origFor = pim; original = f.GetValue<Sprite>(); }
                var want = On && Mine != null ? Mine : original;
                if (f.GetValue<Sprite>() != want) f.SetValue(want);
            }
            catch { }
        }

        /// the copy of the Berserker's card: its class picture (the one that differs between the class cards) becomes ours
        public static void OnCard(GameObject card, Image baseCard, Image otherCard)
        {
            if (Mine == null || baseCard == null || otherCard == null) return;
            foreach (var img in card.GetComponentsInChildren<Image>(true))
            {
                if (img == null || img.gameObject == card) continue;
                string path = Path(img.transform, card.transform);
                var b = baseCard.transform.Find(path)?.GetComponent<Image>();
                var o = otherCard.transform.Find(path)?.GetComponent<Image>();
                if (b == null || o == null || b.sprite == null || o.sprite == null || b.sprite == o.sprite) continue;
                img.sprite = Mine; img.color = Color.white; img.preserveAspect = true;
                FlagellantPlugin.Log.LogInfo("Card class icon: " + path + " (was " + b.sprite.name + ")");
                return;
            }
            FlagellantPlugin.Log.LogWarning("Card class icon: not found");
        }

        static string Path(Transform t, Transform root)
        {
            string p = t.name;
            for (var x = t.parent; x != null && x != root; x = x.parent) p = x.name + "/" + p;
            return p;
        }

        /// the hero in a run (his icon is read by the death screen, the inventory, co-op lists...)
        public static void OnHero(Character ch)
        {
            if (ch != null && Mine != null && ch.icon != Mine) ch.icon = Mine;
        }

        public static bool Want(int cls) => On && cls == Base && Mine != null;
    }

    [HarmonyPatch(typeof(PlayerInputManager), nameof(PlayerInputManager.GetCharacterIcon))]
    internal static class HudIconPatch
    {
        static void Postfix(CharacterClasses charClass, ref Sprite __result) { if (ClassIcon.Want((int)charClass)) __result = ClassIcon.Mine; }
    }

    [HarmonyPatch(typeof(CharacterStatsUI), nameof(CharacterStatsUI.SetCharacter))]
    internal static class HudPanelPatch
    {
        static void Prefix(Character character, ref Sprite classIcon)
        {
            ClassIcon.ApplyHud();
            if (character != null && ClassIcon.Want((int)character.charClass)) classIcon = ClassIcon.Mine;
        }
    }

    [HarmonyPatch(typeof(CharacterStatsManager), "UpdateStats")]
    internal static class StatsIconPatch { static void Prefix() => ClassIcon.ApplyHud(); }

    [HarmonyPatch(typeof(PlayerInputManager), "UpdateSkillsIcons")]
    internal static class SkillsIconPatch { static void Prefix() => ClassIcon.ApplyHud(); }

    /// the title of the hero screen ("КУЛЬТИСТ" with the class icon next to it)
    [HarmonyPatch(typeof(CharacterStats_CharsScreen), nameof(CharacterStats_CharsScreen.UpdateStats))]
    internal static class ScreenTitleIconPatch
    {
        static void Postfix(CharacterStats_CharsScreen __instance, int characterIndex)
        {
            try
            {
                if (!ClassIcon.Want(characterIndex)) return;
                var img = Traverse.Create(__instance).Field("classIcon").GetValue<Image>();
                if (img != null) img.sprite = ClassIcon.Mine;
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(CharacterStats_CharsScreen), nameof(CharacterStats_CharsScreen.GetCharacterIcon))]
    internal static class ScreenIconPatch
    {
        static void Postfix(int index, ref Sprite __result) { if (ClassIcon.Want(index)) __result = ClassIcon.Mine; }
    }
}
