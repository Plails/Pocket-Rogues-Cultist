using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace FlagellantMod
{
    /// A new playable class: the Flagellant (self-flagellant with flails).
    /// Runs on the Berserker's engine, but is a hero of its own: own guild building, card, name, 9 skills,
    /// 4 flails, look and save data.
    [BepInPlugin("custom.pocketrogues.flagellant", "Martyr class", "2.7.1")]
    public class FlagellantPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        internal static string Dir;
        internal static ConfigEntry<bool> CfgLook, CfgShowArmour;

        private void Awake()
        {
            Log = Logger;
            Dir = Path.GetDirectoryName(Info.Location);
            CfgLook = Config.Bind("Visual", "PenitentLook", true, "The Flagellant looks like a penitent: hood, lashed shoulders, rags (облик флагеллянта)");
            CfgShowArmour = Config.Bind("Visual", "ShowArmour", false, "Draw equipped armour and helmets over the penitent look (показывать броню и шлемы)");
            new Harmony("custom.pocketrogues.flagellant").PatchAll(typeof(FlagellantPlugin).Assembly);
            var go = new GameObject("FlagellantRuntime");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<FlagRuntime>();
            StartCoroutine(Build());
            Log.LogInfo("Flagellant class loaded.");
        }

        private IEnumerator Build()
        {
            yield return null;
            yield return new WaitForSecondsRealtime(0.5f);
            try
            {
                Log.LogInfo("Game " + Application.version + ", plugins: " + string.Join(", ", BepInEx.Bootstrap.Chainloader.PluginInfos.Values.Select(p => p.Metadata.Name + " " + p.Metadata.Version + " (" + System.IO.Path.GetFileName(p.Location) + ")")));
            }
            catch { }
            try { FlagSkills.Build(); }
            catch (Exception e) { Log.LogError("Flagellant skills build failed: " + e); }
            try { Weapons.Build(); }
            catch (Exception e) { Log.LogError("Flagellant weapons build failed: " + e); }
            try { Perks.Build(); }
            catch (Exception e) { Log.LogError("Flagellant perks build failed: " + e); }
            // let the other class mods (the Swordsman) read the Berserker's own skills first
            yield return new WaitForSecondsRealtime(1.5f);
            Flag.ApplySkills();
        }
    }

    internal static class Art
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string name)
        {
            if (cache.TryGetValue(name, out var s) && s != null) return s;
            var path = Path.Combine(FlagellantPlugin.Dir, "art", name + ".png");
            if (!File.Exists(path)) { FlagellantPlugin.Log.LogWarning("Missing art: " + path); cache[name] = null; return null; }
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            ImageConversion.LoadImage(tex, File.ReadAllBytes(path));
            tex.filterMode = FilterMode.Point; tex.wrapMode = TextureWrapMode.Clamp; tex.hideFlags = HideFlags.HideAndDontSave;
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 24f, 0, SpriteMeshType.FullRect);
            s.hideFlags = HideFlags.HideAndDontSave;
            cache[name] = s;
            return s;
        }

        /// a sprite from art\<sub>\<name>.png at a given pixel density (the heroes are drawn at 42 px per unit)
        public static Sprite Get(string sub, string name, float ppu)
        {
            string key = sub + "/" + name + "@" + ppu;
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            var path = Path.Combine(FlagellantPlugin.Dir, "art", sub, name + ".png");
            if (!File.Exists(path)) { cache[key] = null; return null; }
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            ImageConversion.LoadImage(tex, File.ReadAllBytes(path));
            tex.filterMode = FilterMode.Point; tex.wrapMode = TextureWrapMode.Clamp; tex.hideFlags = HideFlags.HideAndDontSave;
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect);
            s.hideFlags = HideFlags.HideAndDontSave;
            cache[key] = s;
            return s;
        }

        public static SpriteRenderer MakeSprite(string name, Vector2 pos, float scale, Character hero, int orderOffset)
        {
            var sp = Get(name);
            if (sp == null) return null;
            var go = new GameObject("Flagellant_" + name);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            SpriteRenderer hs = null;
            try { hs = hero != null ? hero.heroSprite : null; } catch { }
            if (hs != null) { sr.sortingLayerID = hs.sortingLayerID; sr.sortingOrder = hs.sortingOrder + orderOffset; }
            return sr;
        }
    }

    /// Religious Ecstasy, Blood Cleansing (resistances, damage bonus), regeneration and the penitent look
    internal class FlagRuntime : MonoBehaviour
    {
        public static FlagRuntime Instance;
        static float ecstasyUntil; static int ecstasyLvl;
        static float cleanseUntil, cleanseBonus;
        static Character resistHero; static float resistUntil, resistVal;
        static readonly List<(float until, float perSec)> regen = new List<(float, float)>();
        float nextTick, nextLook;

        void Awake() { Instance = this; }

        public static bool EcstasyActive => Time.time < ecstasyUntil;

        public static float DamageMult(Character ch)
        {
            float m = 1f;
            if (EcstasyActive) m *= 1f + FLogic.MissingPct(ch) * FlagSkills.At(FlagSkills.T_Ecstasy, ecstasyLvl) * 0.01f;
            if (Time.time < cleanseUntil) m *= 1f + cleanseBonus;
            return m;
        }

        public static float DamageTakenMult(Character ch)
        {
            float k = Perks.VesselTakenMult(ch);                           // Sinful Vessel (perk)
            if (!EcstasyActive || ecstasyLvl < 12) return k;
            return k * (1f - Mathf.Min(75f, FLogic.MissingPct(ch)) * 0.01f);
        }

        public static void StartEcstasy(Character ch, int lvl, float dur) { ecstasyLvl = lvl; ecstasyUntil = Time.time + dur; EcstasyIcon(ch, dur); }

        /// (2.6.7, the user) the ecstasy shows among his effects under his name with its timer, like the Berserker's rage:
        /// an effect of the game's own with the skill's icon and no work of its own (the bonus is counted here as before)
        static Buff ecstasyBuff;
        static void EcstasyIcon(Character ch, float dur)
        {
            try
            {
                if (ch == null) return;
                if (ecstasyBuff == null)
                {
                    ecstasyBuff = ScriptableObject.CreateInstance<Buff>();
                    ecstasyBuff.name = "Flagellant_Ecstasy"; ecstasyBuff.hideFlags = HideFlags.HideAndDontSave;
                    ecstasyBuff.buffType = BuffTypes.damageMod; ecstasyBuff.value = 0f;
                    ecstasyBuff.titleLocalized = new I2.Loc.LocalizedString("Effects/Flagellant/ecstasy");
                    ecstasyBuff.descriptionLocalized = new I2.Loc.LocalizedString("Effects/Flagellant/ecstasy_dsc");
                    try { HarmonyLib.Traverse.Create(ecstasyBuff).Field("_hidePopup").SetValue(true); } catch { }
                }
                ecstasyBuff.icon = FlagSkills.IconOf("suffering");
                ecstasyBuff.time = dur; ecstasyBuff.timeReal = dur;
                ch.AddEffect(ecstasyBuff, false);
            }
            catch (System.Exception e) { FlagellantPlugin.Log.LogWarning("Ecstasy icon: " + e.Message); }
        }
        public static void StartCleanseBonus(float bonus, float dur) { cleanseBonus = bonus; cleanseUntil = Time.time + dur; }
        public static void AddRegen(float perSec, float dur) => regen.Add((Time.time + dur, perSec));

        public static void StartResist(Character ch, float val, float dur)
        {
            EndResist();
            resistHero = ch; resistVal = val; resistUntil = Time.time + dur;
            try { ch.BleedMax += val; ch.PoisonMax += val; ch.PertificationMax += val; } catch { }
        }

        static void EndResist()
        {
            if (resistHero != null)
                try { resistHero.BleedMax -= resistVal; resistHero.PoisonMax -= resistVal; resistHero.PertificationMax -= resistVal; } catch { }
            resistHero = null; resistVal = 0f;
        }

        static Character Hero()
        {
            try { return FLogic.HeroSelf(GameManager.Instance != null ? GameManager.Instance.Player_Mine() : null); } catch { return null; }
        }

        void Update()
        {
            float now = Time.time;
            if (resistHero != null && (now > resistUntil || !resistHero.alive)) EndResist();
            if (now >= nextTick)
            {
                nextTick = now + 1f;
                regen.RemoveAll(r => r.until < now);
                var h = Hero();
                if (h == null) { regen.Clear(); ecstasyUntil = 0f; }
                else if (h.alive && h.HP_cur < h.HP_max)
                {
                    float heal = regen.Sum(r => r.perSec);
                    if (EcstasyActive && ecstasyLvl >= 25) { try { heal += Mathf.Max(0f, h.HP_reg) * 0.5f; } catch { } }
                    if (heal > 0f) try { h.AddHP(heal, false); } catch { }
                }
            }
            if (Time.unscaledTime >= nextLook)
            {
                nextLook = Time.unscaledTime + 0.5f;
                try { Looks(); } catch { }
                ClassIcon.ApplyHud();
                // the fortress icon: once the screen is laid out, measure the Berserker's icon again
                try { Fortress.MeasureLater(); } catch { }
                try { if (SelectScreen.Card != null && SelectScreen.Card.gameObject.activeInHierarchy) SelectScreen.Title(); } catch { }
            }
        }

        void Looks()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.playerList == null) return;
            foreach (var ch in gm.playerList)
            {
                if (ch == null) continue;
                bool want = Flag.Active && ch.charClass == CharacterClasses.BERSERKER && FlagellantPlugin.CfgLook.Value;
                var d = ch.GetComponent<FlagLook>();
                if (Flag.Active && ch.charClass == CharacterClasses.BERSERKER) ClassIcon.OnHero(ch);
                if (want && d == null) ch.gameObject.AddComponent<FlagLook>();
                else if (!want && d != null) Destroy(d);
                // (the user's list) a plain whip in his empty right hand - only to look at, never in the inventory
                bool whip = Flag.Active && ch.charClass == CharacterClasses.BERSERKER;
                var iw = ch.GetComponent<IdleWhip>();
                if (iw != null) Destroy(iw);                                 // (2.6.7) the folded whip in his hand instead
                var hl = ch.GetComponent<HandsLook>();
                if (whip && hl == null) ch.gameObject.AddComponent<HandsLook>();
                else if (!whip && hl != null) Destroy(hl);
            }
        }
    }
}
