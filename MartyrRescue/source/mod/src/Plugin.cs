using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace CultistRescue
{
    /// (the user, 2026-11-26) the Cultist class is won by rescuing him: with a 5% chance one cage of the Occult Sanctum
    /// holds the Cultist himself. Break the cage - he fights at the hero's side; clear the sanctum - he thanks the hero
    /// (a dialogue with his portrait), and the class is open (his building in the fortress stands at level 1, the
    /// fortress shows "Unlocked: Cultist!" like the game's own classes). Until then his building cannot be built.
    [BepInPlugin("custom.pocketrogues.cultistrescue", "Martyr Rescue", "1.0.2")]
    [BepInDependency("custom.pocketrogues.occultsanctum", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("custom.pocketrogues.flagellant", BepInDependency.DependencyFlags.SoftDependency)]
    public class RescuePlugin : BaseUnityPlugin
    {
        public static ManualLogSource Log;
        public static string Dir;
        public static ConfigEntry<int> CfgChance;
        public static ConfigEntry<bool> CfgEvenIfUnlocked;

        void Awake()
        {
            Log = Logger;
            Dir = Path.GetDirectoryName(Info.Location);
            CfgChance = Config.Bind("General", "ChancePercent", 5, "Chance that a sanctum holds the Martyr in a cage, % (шанс, что в святилище в клетке сидит Мученик, %)");
            CfgEvenIfUnlocked = Config.Bind("General", "EvenIfUnlocked", false, "Put him in a cage even when the class is already open - for testing (сажать его в клетку, даже если класс уже открыт - для проверки)");
            var h = new Harmony("custom.pocketrogues.cultistrescue");
            h.PatchAll();
            Rescue.PatchSanctum(h);
            var go = new GameObject("CultistRescueRunner");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<Runner>();
            Log.LogInfo("Cultist Rescue loaded from " + Dir + " (chance " + CfgChance.Value + "%, unlocked=" + Unlock.IsOpen() + ")");
        }
    }

    /// coroutines live here (it never goes away)
    public class Runner : MonoBehaviour
    {
        public static Runner I;
        void Awake() { I = this; }
    }

    public static class Pics
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Texture2D Tex(string rel)
        {
            var path = Path.Combine(RescuePlugin.Dir, rel);
            if (!File.Exists(path)) { RescuePlugin.Log.LogWarning("Missing " + path); return null; }
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            ImageConversion.LoadImage(t, File.ReadAllBytes(path));
            t.hideFlags = HideFlags.HideAndDontSave;
            return t;
        }

        public static Sprite Get(string rel, float ppu, Vector2 pivotPx)
        {
            string key = rel + "@" + ppu + pivotPx;
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            var t = Tex(rel);
            if (t == null) return null;
            s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(pivotPx.x / t.width, pivotPx.y / t.height), ppu, 0, SpriteMeshType.FullRect);
            s.name = "CR_" + Path.GetFileNameWithoutExtension(rel); s.hideFlags = HideFlags.HideAndDontSave;
            cache[key] = s;
            return s;
        }

        /// (the user, 2026-11-26) his own picture, art/cultist_portrait_user.png, if it is there - a painted (not pixel)
        /// picture, so it is scaled smoothly; else our pixel drawing
        public static Sprite Portrait()
        {
            const string user = "art/cultist_portrait_user.png";
            if (File.Exists(Path.Combine(RescuePlugin.Dir, user)))
            {
                var s = Get(user, 100f, new Vector2(157f, 294f));
                if (s != null) { s.texture.filterMode = FilterMode.Bilinear; return s; }
            }
            return Get("art/cultist_portrait.png", 32f, new Vector2(56f, 89f));
        }

        /// a .grip file next to a picture: "x,y" in pixels from the bottom left
        public static Vector2 Grip(string rel, Vector2 fallback)
        {
            try
            {
                var p = Path.Combine(RescuePlugin.Dir, rel);
                if (!File.Exists(p)) return fallback;
                var a = File.ReadAllText(p).Split(',');
                return new Vector2(float.Parse(a[0], System.Globalization.CultureInfo.InvariantCulture), float.Parse(a[1], System.Globalization.CultureInfo.InvariantCulture));
            }
            catch { return fallback; }
        }
    }

    /// the class's state in the save: open once he was rescued, or once his building stands (older saves keep him)
    public static class Unlock
    {
        public const string BuildingId = "flg", RescuedKey = "CR_rescued", ShowKey = "CR_showUnlock";

        public static bool IsOpen()
        {
            try { return FileBasedPrefs.GetInt(RescuedKey, 0) == 1 || FileBasedPrefs.GetInt("bld_" + BuildingId, 0) > 0; }
            catch { return false; }
        }

        public static bool Russian()
        {
            try { int l = FileBasedPrefs.GetInt("lang", 0); return l == 1 || l == 2; } catch { return true; }
        }

        /// he is rescued: the class opens, his building stands at level 1, the fortress shows the unlock screen next time
        public static void Open()
        {
            try
            {
                FileBasedPrefs.SetInt(RescuedKey, 1);
                if (FileBasedPrefs.GetInt("bld_" + BuildingId, 0) < 1) FileBasedPrefs.SetInt("bld_" + BuildingId, 1);
                FileBasedPrefs.SetInt(ShowKey, 1);
                FileBasedPrefs.ManualySave();
                RescuePlugin.Log.LogInfo("Cultist Rescue: the Cultist is rescued - the class is open");
            }
            catch (Exception e) { RescuePlugin.Log.LogError("Cultist Rescue: unlock: " + e); }
        }
    }
}
