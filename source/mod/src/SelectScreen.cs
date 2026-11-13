using System;
using System.IO;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlagellantMod
{
    /// A new card in the hero selection: the Flagellant. It appears once his guild building is built.
    internal static class SelectScreen
    {
        public static Image Card;
        public static Text Points;
        private static ChoseCharsManager owner;
        private static readonly Color Tint = new Color(1f, 0.78f, 0.78f, 1f);
        private static readonly Color DiamondOn = new Color(0.95f, 0.04f, 0.02f, 1f), DiamondOff = new Color(0.29f, 0.04f, 0.01f, 1f);

        static bool Built() => Flag.Raw(() => FileBasedPrefs.GetInt(Flag.BuildingKey, 0)) > 0;

        public static void UpdatePoints(ChoseCharsManager m)
        {
            if (m == null) return;
            int mine = Flag.Raw(() => FileBasedPrefs.GetInt("curPoints" + Flag.OwnIndex, 0));
            if (Points != null)
            {
                Points.text = mine.ToString();
                Points.alignment = mine < 1000 ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            }
            if (m.bersPoints != null)
            {
                int b = Flag.Raw(() => FileBasedPrefs.GetInt("curPoints" + Flag.BaseClass, 0));
                m.bersPoints.text = b.ToString();
                m.bersPoints.alignment = b < 1000 ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            }
        }

        public static void ColorDiamonds(ChoseCharsManager m)
        {
            if (m == null || Points == null) return;
            bool sel = Flag.Active && m.classIndex == Flag.BaseClass;
            var mine = Points.transform.parent.GetComponent<Image>();
            if (mine != null) mine.color = sel ? DiamondOn : DiamondOff;
        }

        public static void AddCard(ChoseCharsManager m)
        {
            if (m == null || m.characterButtons == null || m.characterButtons.Length <= Flag.BaseClass) return;
            owner = m;
            if (Card != null) return;
            var bers = m.characterButtons[Flag.BaseClass];
            var last = m.characterButtons[m.characterButtons.Length - 1];
            var prev = m.characterButtons[m.characterButtons.Length - 2];

            var go = UnityEngine.Object.Instantiate(bers.gameObject, bers.transform.parent);
            go.name = "FlagellantButton";
            Card = go.GetComponent<Image>();
            foreach (var b in go.GetComponentsInChildren<Button>(true))
            {
                b.onClick = new Button.ButtonClickedEvent();
                b.onClick.AddListener(OnClick);
            }
            foreach (var et in go.GetComponentsInChildren<UnityEngine.EventSystems.EventTrigger>(true)) UnityEngine.Object.Destroy(et);
            foreach (var img in go.GetComponentsInChildren<Image>(true))
                if (img != Card) img.color = new Color(img.color.r * Tint.r, img.color.g * Tint.g, img.color.b * Tint.b, img.color.a);
            Points = go.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t.name == "Points");
            // the copy keeps the Berserker's translation components: they would write "Берсерк" back on the next
            // language refresh / panel enable. All of them go, and the name is kept by Title().
            foreach (var loc in go.GetComponentsInChildren<I2.Loc.Localize>(true)) UnityEngine.Object.DestroyImmediate(loc);
            Title();
            // the real Berserker card keeps saying BERSERKER (its own translation would turn into ours while he is chosen)
            BersCard = bers;
            foreach (var loc in bers.GetComponentsInChildren<I2.Loc.Localize>(true)) UnityEngine.Object.DestroyImmediate(loc);
            AddEmblem(go);
            if (bers.transform.parent.GetComponent<LayoutGroup>() == null)
            {
                var rt = (RectTransform)go.transform;
                var step = ((RectTransform)last.transform).anchoredPosition - ((RectTransform)prev.transform).anchoredPosition;
                rt.anchoredPosition = ((RectTransform)last.transform).anchoredPosition + step;
                // another class card (the Swordsman) may already stand there
                foreach (Transform sib in bers.transform.parent)
                    if (sib != go.transform && sib.name.EndsWith("Button") && sib.name != bers.name && Vector2.Distance(((RectTransform)sib).anchoredPosition, rt.anchoredPosition) < 5f)
                        rt.anchoredPosition += step;
            }
            go.transform.SetAsLastSibling();
            // the Berserker card is not touched: its own click selects the class once, and SelectClassPatch sees
            // (through ButtonPressPatch) that it was the Berserker card and switches the Cultist off before it
            FlagellantPlugin.Log.LogInfo("Flagellant card added.");
            Highlight();
            UpdatePoints(m);
            ColorDiamonds(m);
        }

        static void AddEmblem(GameObject card)
        {
            try
            {
                var sp = Fortress.Icon();
                if (sp == null) return;
                var e = new GameObject("FlagellantEmblem", typeof(RectTransform));
                e.transform.SetParent(card.transform, false);
                var rt = (RectTransform)e.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                float side = Mathf.Max(18f, ((RectTransform)card.transform).rect.height * 0.26f);
                rt.sizeDelta = new Vector2(side, side);
                rt.anchoredPosition = new Vector2(-3f, -3f);
                var img = e.AddComponent<Image>();
                img.sprite = sp; img.raycastTarget = false; img.preserveAspect = true;
            }
            catch (Exception ex) { FlagellantPlugin.Log.LogWarning("Emblem: " + ex.Message); }
        }

        private static string Name() => Texts.TryGet(Texts.Name, null, out var n) ? n : "Cultist";

        /// the card always says FLAGELLANT (every text on it except the skill points)
        public static Image BersCard;

        public static void Title()
        {
            if (Card == null) return;
            Set(Card, Name().ToUpper());
            if (BersCard != null)
            {
                string b;
                Texts.NoSwap++;
                try { b = I2.Loc.LocalizationManager.GetTranslation("Characters/Classes/Berserker"); }
                finally { Texts.NoSwap--; }
                if (!string.IsNullOrEmpty(b)) Set(BersCard, b.ToUpper());
            }
        }

        static void Set(Image card, string name)
        {
            foreach (var t in card.GetComponentsInChildren<Text>(true))
                if (t != null && t.name != "Points" && t.text != name) t.text = name;
            foreach (var t in card.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (t != null && t.text != name) t.text = name;
        }

        /// which card started the SelectClass that is running now (a class is selected exactly once per click)
        public static bool MyClick, BaseClick;

        private static void OnClick()
        {
            FlagellantPlugin.Log.LogInfo("Flagellant card clicked: built=" + Built() + " owner=" + (owner != null) + " skillsReady=" + (FlagSkills.Data != null));
            if (owner == null || !Built()) return;
            MyClick = true;
            try { owner.SelectClass(Flag.BaseClass); }
            finally { MyClick = false; }
        }

        public static bool IsBaseCard(Component c) => c != null && BersCard != null && c.transform.IsChildOf(BersCard.transform);

        public static void Highlight()
        {
            if (owner == null || Card == null) return;
            Title();
            bool built = Built() && owner.characterButtons[Flag.BaseClass].gameObject.activeSelf;
            if (Card.gameObject.activeSelf != built) Card.gameObject.SetActive(built);
            if (!built && Flag.Active) Flag.Set(false);
            var myBtn = Card.GetComponent<Button>();
            if (myBtn != null) myBtn.interactable = built;
            bool hasChar = !string.IsNullOrEmpty(Flag.Raw(() => FileBasedPrefs.GetString("NeedLoading_ReturnToTheFortress_" + Flag.OwnIndex, "")));
            bool sel = Flag.Active && owner.classIndex == Flag.BaseClass;
            Card.sprite = hasChar ? (sel ? owner.ui_classBg_Sel_hasChar : owner.ui_classBg_def_hasChar) : (sel ? owner.ui_classBg_Sel : owner.ui_classBg_def);
            if (sel)
            {
                var b = owner.characterButtons[Flag.BaseClass];
                b.sprite = b.sprite == owner.ui_classBg_Sel_hasChar ? owner.ui_classBg_def_hasChar : owner.ui_classBg_def;
            }
        }
    }

    [HarmonyPatch(typeof(ChoseCharsManager), "Awake")]
    internal static class CharsAwakePatch
    {
        static void Postfix(ChoseCharsManager __instance)
        {
            try { SelectScreen.AddCard(__instance); } catch (Exception e) { FlagellantPlugin.Log.LogError("Flagellant card: " + e); }
        }
    }

    /// a press on the Berserker's card (mouse, touch or gamepad submit) - known before its own click runs
    [HarmonyPatch(typeof(Button), "Press")]
    internal static class ButtonPressPatch
    {
        static void Prefix(Button __instance, out bool __state)
        {
            __state = false;
            try { if (SelectScreen.IsBaseCard(__instance)) { SelectScreen.BaseClick = true; __state = true; } } catch { }
        }
        static void Finalizer(bool __state) { if (__state) SelectScreen.BaseClick = false; }
    }

    [HarmonyPatch(typeof(ChoseCharsManager), nameof(ChoseCharsManager.UpdateSkillPoints))]
    internal static class PointsPatch
    {
        static void Postfix(ChoseCharsManager __instance) { try { SelectScreen.UpdatePoints(__instance); } catch { } }
    }

    [HarmonyPatch(typeof(ChoseCharsManager), nameof(ChoseCharsManager.RedrawSkills))]
    internal static class DiamondsPatch
    {
        static void Postfix(ChoseCharsManager __instance) { try { SelectScreen.ColorDiamonds(__instance); SelectScreen.UpdatePoints(__instance); } catch { } }
    }

    [HarmonyPatch(typeof(ChoseCharsManager), nameof(ChoseCharsManager.SelectClass))]
    internal static class SelectClassPatch
    {
        static void Prefix(ChoseCharsManager __instance, int index)
        {
            try
            {
                if (index != Flag.BaseClass) { if (Flag.Active) Flag.Set(false); }
                else
                {
                    // our card -> the Cultist; the Berserker's own card -> the Berserker; anything else (the game
                    // restoring the last hero) keeps what was chosen
                    if (SelectScreen.MyClick) Flag.Set(true);
                    else if (SelectScreen.BaseClick) Flag.Set(false);
                    Flag.ApplySkills();
                }
                SelfHeal(__instance, index, false);
            }
            catch (Exception e) { FlagellantPlugin.Log.LogError("SelectClass prefix: " + e); }
        }

        static void Postfix(ChoseCharsManager __instance, int index)
        {
            try { SelectScreen.Highlight(); } catch (Exception e) { FlagellantPlugin.Log.LogError("Highlight: " + e); }
            try { SelfHeal(__instance, index, true); } catch (Exception e) { FlagellantPlugin.Log.LogError("SelfHeal: " + e); }
            string shown = "";
            try { shown = string.Join(", ", __instance.skillsUI.Where(s => s != null).Take(3).Select(s => s.Name)); } catch { }
            FlagellantPlugin.Log.LogInfo("Class selected: " + index + (index == Flag.BaseClass ? (Flag.Active ? " = FLAGELLANT" : " = Berserker") : "") + " | screen shows: " + shown);
        }

        /// the hero screen reads its own SkillsDataBase: make sure it holds the right tree, and redraw if it did not
        static void SelfHeal(ChoseCharsManager m, int index, bool redraw)
        {
            if (m == null || index != Flag.BaseClass) return;
            if (Flag.Active && FlagSkills.Data == null)
            {
                FlagellantPlugin.Log.LogWarning("Flagellant skills were not built - building now");
                try { FlagSkills.Build(); } catch (Exception e) { FlagellantPlugin.Log.LogError("Flagellant skills build failed: " + e); }
            }
            var db = m.SkillsDB;
            if (db == null || FlagSkills.Data == null) return;
            FlagSkills.RememberOriginal(db);
            var want = Flag.Active ? FlagSkills.Data : FlagSkills.OriginalOf(db);
            if (db.skills_berserk == want) return;
            FlagellantPlugin.Log.LogWarning("Hero screen had the wrong skill tree - fixing (" + (Flag.Active ? "Flagellant" : "Berserker") + ")");
            db.skills_berserk = want;
            try { db.skills_berserk.Load(); } catch { }
            if (redraw) m.RedrawSkills();
        }
    }
}
