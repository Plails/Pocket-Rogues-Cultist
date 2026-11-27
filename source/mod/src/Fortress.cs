using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FlagellantMod
{
    /// The Flagellant's own building in the fortress tree ("Обитель Флагеллянтов"), a branch of the Berserker's.
    /// Building it opens the class; its level caps the Flagellant's skill levels (like every class building).
    internal static class Fortress
    {
        static Sprite icon;

        /// the helmet the user chose: the game's own sprite "SacrificeSkeleton_03" (the Sacrifice Skeleton's helmet);
        /// our drawn icon only if the game doesn't have it
        public static Sprite Icon()
        {
            if (icon != null) return icon;
            // the user's own helmet picture (SacrificeSkeleton_03.png) is the building's icon; the drawn skull only as a spare
            var path = Path.Combine(FlagellantPlugin.Dir, "art", "helmet.png");
            if (!File.Exists(path)) path = Path.Combine(FlagellantPlugin.Dir, "art", "building.png");
            if (!File.Exists(path)) return null;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "Flg_building" };
            ImageConversion.LoadImage(tex, File.ReadAllBytes(path));
            tex.hideFlags = HideFlags.HideAndDontSave;
            icon = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 48f);
            icon.name = "Flg_building"; icon.hideFlags = HideFlags.HideAndDontSave;
            return icon;
        }

        public static void Add(BaseManager bm)
        {
            if (bm == null || bm.buildingsDB == null) return;
            if (bm.buildingsDB.Any(b => b != null && b.nameID == Flag.BuildingId)) return;
            var bers = bm.buildingsDB.FirstOrDefault(b => b != null && b.nameID == "bers");
            if (bers == null) { FlagellantPlugin.Log.LogWarning("Fortress: Berserker building not found"); return; }

            var go = UnityEngine.Object.Instantiate(bers.gameObject, bers.transform.parent);
            go.name = "Building_FLAGELLANT";
            var b = go.GetComponent<Building>();
            b.nameID = Flag.BuildingId;
            var tr = Traverse.Create(b);
            tr.Field("_localizationID").SetValue("ClassFlagellant");
            tr.Field("unlock_preview").SetValue(null);
            tr.Field("unlock_preview_RU").SetValue(null);
            tr.Field("unlock_preview_UK").SetValue(null);
            b.Parent = bers;
            var ic = Icon();
            if (ic != null) b.icon = ic;
            b.dialogue_onBuild = null;
            b.dialogue_main = null;
            b.dialoguesOnUpgrade = new DialogueOnBuild[0];
            b.event_onBuild = new UnityEvent();
            b.event_onPressDialogueButton = new UnityEvent();
            b.LoadBuildingPrefs();

            // its own icon image and level text (the Berserker's may already sit in the shared icons container)
            if (b.iconImage == null || !b.iconImage.transform.IsChildOf(go.transform))
            {
                var src = bers.iconImage;
                if (src != null)
                {
                    var img = UnityEngine.Object.Instantiate(src.gameObject, go.transform);
                    img.transform.localPosition = src.transform.parent == bers.transform ? src.transform.localPosition : Vector3.zero;
                    b.iconImage = img.GetComponent<Image>();
                }
            }
            if (b.txt != null && !b.txt.transform.IsChildOf(go.transform))
            {
                var t = UnityEngine.Object.Instantiate(b.txt.gameObject, go.transform);
                t.transform.localPosition = Vector3.zero;
                b.txt = t.GetComponent<Text>();
            }
            // where the icon and the number sit inside a class building (the clone still has them as its children)
            if (b.iconImage != null) { IconLocal = b.iconImage.transform.localPosition; IconScale = b.iconImage.transform.localScale; }
            if (b.txt != null && b.txt.transform.parent == go.transform) { TxtLocal = b.txt.transform.localPosition; TxtScale = b.txt.transform.localScale; }
            else TxtLocal = new Vector3(40f, -40f, 0f);

            // place: the free cell diagonally above-left of the Berserker
            var rt = (RectTransform)go.transform;
            var brt = (RectTransform)bers.transform;
            rt.anchoredPosition = brt.anchoredPosition + new Vector2(-192f, 171f);

            // one corridor to the Berserker, the copied ones removed
            var corridors = b.corridor ?? new GameObject[0];
            GameObject corr = null;
            foreach (var c in corridors)
            {
                if (c == null) continue;
                if (corr == null && c.transform.IsChildOf(go.transform)) corr = c;
                else if (c.transform.IsChildOf(go.transform)) UnityEngine.Object.Destroy(c);
            }
            if (corr == null && bers.corridor != null && bers.corridor.Length > 0 && bers.corridor[0] != null)
                corr = UnityEngine.Object.Instantiate(bers.corridor[0], go.transform);
            if (corr != null)
            {
                // one straight vertical strip, like every corridor of the fortress: up from the cell right below
                var parent = go.transform.parent;
                Vector3 p3 = go.transform.localPosition, p0 = new Vector3(p3.x, bers.transform.localPosition.y, p3.z);
                var crt0 = corr.transform as RectTransform;
                float thick = crt0 != null ? Mathf.Min(Mathf.Abs(crt0.sizeDelta.x), Mathf.Abs(crt0.sizeDelta.y)) : 12f;
                if (thick < 1f) thick = 12f;
                var segs = new[] { (p0, p3) };
                var made = new List<GameObject>();
                for (int i = 0; i < segs.Length; i++)
                {
                    var c = i == 0 ? corr : UnityEngine.Object.Instantiate(corr, go.transform);
                    c.name = "_corridorFlg" + i;
                    Vector3 wa = parent.TransformPoint(segs[i].Item1), wz = parent.TransformPoint(segs[i].Item2);
                    c.transform.position = (wa + wz) / 2f;
                    float scale = Mathf.Max(0.0001f, Mathf.Abs(c.transform.lossyScale.x));
                    var crt = c.transform as RectTransform;
                    if (crt != null)
                    {
                        crt.pivot = new Vector2(0.5f, 0.5f);
                        crt.sizeDelta = new Vector2(Vector3.Distance(wa, wz) / scale, thick);
                    }
                    c.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(wz.y - wa.y, wz.x - wa.x) * Mathf.Rad2Deg);
                    made.Add(c);
                }
                b.corridor = made.ToArray();
            }
            else b.corridor = new GameObject[0];

            // its own button
            int index = bm.buildingsDB.Count;
            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick = new Button.ButtonClickedEvent();
                // like the other buildings: a click selects it and the icon bounces
                // (the bounce only when the building gets selected, not on every click - as the game does)
                btn.onClick.AddListener(() =>
                {
                    bool already = false;
                    try { already = Traverse.Create(bm).Field("selectedIndex").GetValue<int>() == index; } catch { }
                    if (!already) try { bm.ButtBuildingAnimation(index); } catch { }
                    try { bm.ButtBuilding(index); } catch { }
                });
            }
            foreach (var et in go.GetComponentsInChildren<UnityEngine.EventSystems.EventTrigger>(true)) et.triggers.Clear();   // no bounce on hover
            bm.buildingsDB.Add(b);
            FlagellantPlugin.Log.LogInfo("Fortress: Flagellant building added (lvl " + b.lvl + ")");
        }

        public static Vector3 IconLocal, IconScale = Vector3.one, TxtLocal, TxtScale = Vector3.one;
        static Vector2 iconSize, txtSize;
        /// (the user, 2026-11-27: the icon twitched on entering the guild) where the parts were placed last time -
        /// used at once on the next visit; until the first placing they are invisible
        static bool savedIcon, savedTxt;
        static Vector3 savedIconPos, savedIconScale, savedTxtPos, savedTxtScale;
        static void Show(RectTransform h, bool on)
        {
            if (h == null) return;
            var g = h.GetComponent<CanvasGroup>() ?? h.gameObject.AddComponent<CanvasGroup>();
            g.alpha = on ? 1f : 0f; g.blocksRaycasts = false; g.interactable = false;
        }

        /// The game moves every building's icon and number into one shared container; for ours that put them off
        /// the building. So ours stay the building's own children (in holders), on top of its frame.
        public static Building Live;
        static RectTransform iconHolder, txtHolder;
        static float liveSince = -1f;
        static int tries;
        public static bool Measured;

        static Vector2 BuildingSize(Building b)
        {
            var r = ((RectTransform)b.transform).rect.size;
            return r.x > 1f && r.y > 1f ? r : new Vector2(128f, 128f);
        }

        /// a part with a fixed size, centred, not stretched by its parent (the game's icons stretch with the building)
        static void Center(RectTransform rt, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        /// the holder has the building's own size; it carries the place and scale, the part inside keeps scale 1,
        /// so the game's hover pulse (1 -> 1.3 -> 1) works and nothing fights it
        static RectTransform Holder(Building b, ref RectTransform h, string name)
        {
            if (h == null || h.parent != b.transform)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.layer = b.gameObject.layer;
                h = (RectTransform)go.transform;
                h.SetParent(b.transform, false);
                h.anchorMin = h.anchorMax = new Vector2(0.5f, 0.5f);
                h.pivot = new Vector2(0.5f, 0.5f);
                h.sizeDelta = BuildingSize(b);
                h.localPosition = Vector3.zero; h.localScale = Vector3.one;
            }
            return h;
        }

        /// after the fortress is on screen: size, place and scale of the Berserker building's own part at rest
        static bool Measure(Building b, RectTransform holder, RectTransform mine, Transform his, ref Vector2 size)
        {
            var bers = b.Parent;
            if (bers == null || !(his is RectTransform h) || his.parent == null) return false;
            Vector3 hs = his.parent.lossyScale, bs = b.transform.lossyScale;
            if (Mathf.Abs(bs.x) < 1e-5f || Mathf.Abs(bs.y) < 1e-5f || Mathf.Abs(hs.x) < 1e-5f) return false;
            var s = h.rect.size;
            if (s.x < 2f || s.y < 2f) return false;
            size = s;
            Center(mine, size);
            holder.localScale = new Vector3(hs.x / bs.x, hs.y / bs.y, 1f);
            Vector3 hisCenter = his.TransformPoint(h.rect.center);
            holder.localPosition = b.transform.InverseTransformPoint(b.transform.position + (hisCenter - bers.transform.position));
            return true;
        }

        /// called by the runtime every half second until measured (a few tries at most, then it leaves things alone)
        public static void MeasureLater()
        {
            var b = Live;
            if (Measured || b == null || !b.gameObject.activeInHierarchy) { if (b == null || !b.gameObject.activeInHierarchy) liveSince = -1f; return; }
            if (liveSince < 0f) { liveSince = Time.unscaledTime; return; }
            if (Time.unscaledTime - liveSince < 1f) return;
            KeepOnBuilding(b);
            bool ok = true;
            if (b.iconImage != null && iconHolder != null) ok &= Measure(b, iconHolder, (RectTransform)b.iconImage.transform, b.Parent?.iconImage?.transform, ref iconSize);
            if (b.txt != null && txtHolder != null) ok &= Measure(b, txtHolder, (RectTransform)b.txt.transform, b.Parent?.txt?.transform, ref txtSize);
            if (ok || ++tries >= 6) Measured = true;
            if (Measured)
            {
                if (iconHolder != null) { savedIcon = true; savedIconPos = iconHolder.localPosition; savedIconScale = iconHolder.localScale; Show(iconHolder, true); }
                if (txtHolder != null) { savedTxt = true; savedTxtPos = txtHolder.localPosition; savedTxtScale = txtHolder.localScale; Show(txtHolder, true); }
            }
            FlagellantPlugin.Log.LogInfo("Fortress: icon measured=" + ok + " size " + iconSize + " holder " + (iconHolder ? iconHolder.localPosition + " x" + iconHolder.localScale.x.ToString("0.00") : "-"));
        }

        public static void KeepOnBuilding(Building b)
        {
            if (b == null || b.nameID != Flag.BuildingId) return;
            if (Live != b) { Live = b; Measured = false; liveSince = -1f; tries = 0; }
            var bsz = BuildingSize(b);
            if (iconSize == Vector2.zero) iconSize = bsz * 0.62f;
            if (txtSize == Vector2.zero) txtSize = new Vector2(bsz.x * 0.6f, bsz.y * 0.35f);
            if (b.iconImage != null)
            {
                var hold = Holder(b, ref iconHolder, "FlgIconHolder");
                var t = (RectTransform)b.iconImage.transform;
                if (t.parent != hold)
                {
                    t.SetParent(hold, false);
                    Center(t, iconSize);
                    if (!Measured)
                    {
                        if (savedIcon) { hold.localPosition = savedIconPos; hold.localScale = savedIconScale; }
                        else { hold.localPosition = Vector3.zero; hold.localScale = Vector3.one; }
                    }
                }
                Show(hold, Measured || savedIcon);
                b.iconImage.preserveAspect = true;
                b.iconImage.enabled = true;
                hold.SetAsLastSibling();
            }
            if (b.txt != null)
            {
                var hold = Holder(b, ref txtHolder, "FlgTxtHolder");
                var t = (RectTransform)b.txt.transform;
                if (t.parent != hold)
                {
                    t.SetParent(hold, false);
                    Center(t, txtSize);
                    if (!Measured)
                    {
                        if (savedTxt) { hold.localPosition = savedTxtPos; hold.localScale = savedTxtScale; }
                        else { hold.localPosition = new Vector3(bsz.x * 0.2f, -bsz.y * 0.3f, 0f); hold.localScale = Vector3.one; }
                    }
                }
                Show(hold, Measured || savedTxt);
                var his = b.Parent != null ? b.Parent.txt : null;
                if (his != null) { b.txt.fontSize = his.fontSize; b.txt.alignment = his.alignment; }
                hold.SetAsLastSibling();
            }
        }

        public static void Fix(BaseManager bm)
        {
            var b = bm?.buildingsDB?.FirstOrDefault(x => x != null && x.nameID == Flag.BuildingId);
            if (b == null) return;
            b.LoadBuilding();
            KeepOnBuilding(b);
            FlagellantPlugin.Log.LogInfo("Fortress: icon " + (b.iconImage ? b.iconImage.sprite?.name + " at " + b.iconImage.transform.localPosition : "-") + ", level text " + (b.txt ? "'" + b.txt.text + "' at " + b.txt.transform.localPosition : "-"));
        }
    }

    [HarmonyPatch(typeof(BaseManager), "Awake")]
    internal static class BaseAwakeFixPatch
    {
        static void Postfix(BaseManager __instance)
        {
            try { Fortress.Fix(__instance); } catch (Exception e) { FlagellantPlugin.Log.LogError("Fortress fix: " + e); }
        }
    }

    /// after every refresh / purchase / upgrade the icon and the number go back onto our building
    [HarmonyPatch(typeof(Building), nameof(Building.LoadBuilding))]
    internal static class BuildingTextPatch
    {
        static void Postfix(Building __instance) { try { Fortress.KeepOnBuilding(__instance); } catch { } }
    }

    [HarmonyPatch(typeof(Building), nameof(Building.CreateChildImage))]
    internal static class BuildingIconPatch
    {
        static void Postfix(Building __instance) { try { Fortress.KeepOnBuilding(__instance); } catch { } }
    }

    [HarmonyPatch(typeof(BaseManager), "Awake")]
    internal static class BaseAwakePatch
    {
        static void Prefix(BaseManager __instance)
        {
            try { Fortress.Add(__instance); } catch (Exception e) { FlagellantPlugin.Log.LogError("Fortress building: " + e); }
        }
    }
}
