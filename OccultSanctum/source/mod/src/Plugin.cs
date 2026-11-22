using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using I2.Loc;
using UnityEngine;

namespace OccultSanctum
{
    [BepInPlugin("custom.pocketrogues.occultsanctum", "Occult Sanctum - a cultist sub-location of the Void Crypt", "0.37.1")]
    public class SanctumPlugin : BaseUnityPlugin
    {
        public static ManualLogSource Log;
        public static string Dir;
        public static ConfigEntry<bool> CfgEnabled;
        public static ConfigEntry<int> CfgChance, CfgFloorFrom, CfgFloorTo, CfgSecretChance;

        private void Awake()
        {
            Log = Logger;
            Dir = Path.GetDirectoryName(Info.Location);
            CfgEnabled = Config.Bind("General", "Enabled", true, "Gates to the Occult Sanctum on Void Crypt floors (врата в Оккультное святилище на этажах Крипты Пустоты)");
            CfgChance = Config.Bind("General", "ChancePercent", 100, "Chance that a floor gets the gates, % - 100 for testing (шанс врат на этаже, % - 100 для теста)");
            CfgFloorFrom = Config.Bind("General", "FloorFrom", 1, "From this floor (с какого этажа)");
            CfgFloorTo = Config.Bind("General", "FloorTo", 5, "Up to this floor (по какой этаж)");
            CfgSecretChance = Config.Bind("General", "SecretRoomChancePercent", 100, "Chance of a secret room behind a breakable wall, % - 100 for testing (шанс секретной комнаты за ломаемой стеной, % - 100 для теста)");
            new Harmony("custom.pocketrogues.occultsanctum").PatchAll();
            var go = new GameObject("SanctumDirector");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<SanctumDirector>();
            Log.LogInfo("Occult Sanctum loaded from " + Dir);
        }
    }

    public static class Art
    {
        public const float PPU = 24f;      // the game's tile: 24 px
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        /// our picture made to stand in for a part of a game monster: the same pivot (as a fraction of the picture)
        /// and the same pixels-per-unit as the part it replaces, so it sits and moves exactly like it
        public static Sprite Part(string name, Sprite like, float ppuIfNone = 38f)
        {
            string key = name + "@" + (like != null ? like.name + like.pivot + like.rect.size + like.pixelsPerUnit : "none");
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            var src = Get(name);
            if (src == null) return null;
            var tex = src.texture;
            Vector2 piv = like != null && like.rect.width > 0 ? new Vector2(like.pivot.x / like.rect.width, like.pivot.y / like.rect.height) : new Vector2(0.5f, 0.5f);
            float ppu = like != null ? like.pixelsPerUnit : ppuIfNone;
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), piv, ppu, 0, SpriteMeshType.FullRect);
            s.hideFlags = HideFlags.HideAndDontSave;
            s.name = "OS_" + name;
            cache[key] = s;
            return s;
        }

        /// our picture drawn at twice the size of a part of the game's rig (with an even margin round it, if wider):
        /// it stands just where the rig's part stood, with twice its pixels
        public static Sprite Part2x(string name, Sprite like) => PartK(name, like, 2f);

        /// our picture drawn at k times the size of a part of the game's rig (an even margin round it, if wider)
        public static Sprite PartK(string name, Sprite like, float k)
        {
            if (like == null || like.rect.width <= 0) return null;
            string key = name + "#" + k + "x" + like.pivot + like.rect.size + like.pixelsPerUnit;
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            var src = Get(name);
            if (src == null) return null;
            var tex = src.texture;
            float mx = (tex.width - k * like.rect.width) / 2f, my = (tex.height - k * like.rect.height) / 2f;
            var piv = new Vector2((like.pivot.x * k + mx) / tex.width, (like.pivot.y * k + my) / tex.height);
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), piv, like.pixelsPerUnit * k, 0, SpriteMeshType.FullRect);
            s.hideFlags = HideFlags.HideAndDontSave;
            s.name = "OS_" + name;
            cache[key] = s;
            return s;
        }

        /// our picture with a given pivot (a fraction of the picture) and pixels-per-unit
        public static Sprite Pivoted(string name, Vector2 pivot, float ppu)
        {
            string key = name + "%" + pivot + ppu;
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            var src = Get(name);
            if (src == null) return null;
            var tex = src.texture;
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, ppu, 0, SpriteMeshType.FullRect);
            s.hideFlags = HideFlags.HideAndDontSave;
            s.name = "OS_" + name;
            cache[key] = s;
            return s;
        }

        /// one 24 x 24 tile cut out of a big picture (the floor stones), so any shape of room gets a seamless floor
        public static Sprite Tile(string name, int tx, int ty)
        {
            string key = name + "#" + tx + "," + ty;
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            var big = Get(name);
            if (big == null) return null;
            var tex = big.texture;
            int n = (int)PPU;
            tx = ((tx % (tex.width / n)) + tex.width / n) % (tex.width / n); ty = ((ty % (tex.height / n)) + tex.height / n) % (tex.height / n);
            s = Sprite.Create(tex, new Rect(tx * n, ty * n, n, n), new Vector2(0.5f, 0.5f), PPU, 0, SpriteMeshType.FullRect);
            s.hideFlags = HideFlags.HideAndDontSave;
            cache[key] = s;
            return s;
        }

        public static Sprite Get(string name)
        {
            if (cache.TryGetValue(name, out var s) && s != null) return s;
            string path = Path.Combine(SanctumPlugin.Dir, "SanctumArt", name + ".png");
            if (!File.Exists(path)) { SanctumPlugin.Log.LogWarning("Missing art: " + path); return null; }
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            ImageConversion.LoadImage(tex, File.ReadAllBytes(path));
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.hideFlags = HideFlags.HideAndDontSave;
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), PPU, 0, SpriteMeshType.FullRect);
            s.hideFlags = HideFlags.HideAndDontSave;
            s.name = "OS_" + name;
            cache[name] = s;
            return s;
        }
    }

    public static class Texts
    {
        public static bool Ru
        {
            get
            {
                try
                {
                    var code = LocalizationManager.CurrentLanguageCode ?? "";
                    var lang = LocalizationManager.CurrentLanguage ?? "";
                    return code.StartsWith("ru", StringComparison.OrdinalIgnoreCase) || lang.IndexOf("Russian", StringComparison.OrdinalIgnoreCase) >= 0 || lang.IndexOf("Рус", StringComparison.OrdinalIgnoreCase) >= 0;
                }
                catch { return true; }
            }
        }

        public static string Get(string term)
        {
            bool ru = Ru;
            switch (term)
            {
                case "Sanctum/Enter": return ru ? "Войти в святилище" : "Enter the sanctum";
                case "Sanctum/Leave": return ru ? "Вернуться" : "Go back";
                case "Sanctum/Title": return ru ? "Оккультное святилище" : "Occult Sanctum";
                case "Sanctum/NotCleared": return ru ? "Сначала очистите святилище" : "Clear the sanctum first";
                case "Sanctum/Cultist": return ru ? "Культист" : "Cultist";
                case "Sanctum/Devil": return ru ? "Крылатый чёрт" : "Winged devil";
                case "Sanctum/Spearman": return ru ? "Копейщик культа" : "Cult spearman";
                case "Bestiary/MobsDescriptions/dscr_OccultSpearman": return ru ? "Воин культа в таком же сером капюшоне и с красным кушаком, но с длинным копьём, перевязанным красной тканью. Колет издалека, не подпуская к себе." : "A cult warrior in the same grey hood and red sash, but with a long spear tied with red cloth under its point. He thrusts from afar and keeps you at the spear's length.";
                case "Sanctum/Champion": return ru ? "Генерал культа" : "Cult general";
                case "Bestiary/MobsDescriptions/dscr_OccultChampion": return ru ? "Генерал культа: огромный воин в чёрной рясе с серой нашивкой, в шипастых наплечниках, с огромной двуручной шипастой булавой. Обходит всё святилище в сопровождении двух солдат. В бою вскидывает булаву и боевым кличем приводит союзников в ярость на 10 секунд. Очень живуч." : "The cult's general: a huge warrior in a black robe with a grey tabard and spiked pauldrons, wielding a huge two-handed spiked mace. He goes the rounds of the whole sanctum with two soldiers behind him. In a fight he raises his mace and his war cry sends his friends into a rage for 10 seconds. Very hard to kill.";
                case "Sanctum/Magus": return ru ? "Архимаг культа" : "Cult archmage";
                case "Bestiary/MobsDescriptions/dscr_OccultMagus": return ru ? "Второй чемпион культа, элитный маг: огромный, в чёрной рясе с красной каймой, в наплечниках из черепов, с золотой цепью и кровавым камнем на груди, в капюшоне с золотым венцом и костяными рогами, из-под которого горят красные глаза. Мечет такие же проклятые сферы, как культисты, только огромные. Обходит святилище с двумя культистами." : "The cult's second champion, an elite magus: huge, in a black robe hemmed with red, skull pauldrons, a gold chain with a blood gem on his chest, a hood crowned with gold and bone horns, red eyes burning under it. Hurls the cultists' cursed spheres, only huge. Goes the rounds of the sanctum with two cultists.";
                case "Sanctum/Elder": return ru ? "Верховный культист" : "Cult elder";
                case "Bestiary/MobsDescriptions/dscr_OccultElder": return ru ? "Глава культа: рослый, в чёрном капюшоне и рясе с золотой каймой, с огромным посохом, увенчанным рогатым черепом. Мечет проклятые сферы по три разом. Ходит по святилищу со свитой и смотрит, чем заняты остальные." : "The cult's head: tall, in a black hood and robe edged with gold, with a great staff crowned by a horned skull. Hurls cursed spheres three at a time. He goes about the sanctum with his retinue, seeing what the others are doing.";
                case "Sanctum/Crossbowman": return ru ? "Арбалетчик культа" : "Cult crossbowman";
                case "Bestiary/MobsDescriptions/dscr_OccultCrossbowman": return ru ? "Стрелок культа в глубоком сером капюшоне, с красным кушаком и большим арбалетом. Держится поодаль и бьёт болтами." : "The cult's marksman in a deep grey hood, with a red sash and a big crossbow. He keeps his distance and shoots bolts.";
                case "Locations/Titles/731": return ru ? "Оккультное святилище" : "Occult Sanctum";
                case "Bestiary/MobsDescriptions/dscr_OccultCultist": return ru ? "Служитель культа в чёрном плаще с красной каймой. Бросает проклятые сферы голыми руками. Когда их остаётся мало, они собираются у пентаграммы и вызывают крылатых чертей." : "A cult's servant in a black cloak hemmed with red. Throws cursed spheres with his bare hands. When few are left, they gather at the great circle and call up winged devils.";
                case "Bestiary/MobsDescriptions/dscr_OccultDevil": return ru ? "Мелкий крылатый чёрт, вызванный ритуалом культистов из портала в пентаграмме. Быстр и бьёт когтями." : "A small winged devil called up through the portal of the cultists' ritual. Quick, it fights with its claws.";
            }
            return term;
        }
    }

    [HarmonyPatch(typeof(LocalizationManager), nameof(LocalizationManager.GetTranslation))]
    internal static class TranslationPatch
    {
        static void Postfix(string Term, ref string __result)
        {
            if (Term == null) return;
            if (Term == "Locations/Titles/731" || (Term.StartsWith("Bestiary/MobsDescriptions/dscr_Occult", StringComparison.Ordinal)))
            { __result = Texts.Get(Term); return; }
            if (Term.Length > 8 && Term[0] == 'S' && Term.StartsWith("Sanctum/", StringComparison.Ordinal))
                __result = Texts.Get(Term);
        }
    }
}
