using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CultistRescue
{
    /// the Cultist's thanks, shown like the game's own class dialogues: his big portrait on the left, a dark box below
    /// with his name and his words typed out; a click (or any key) finishes the line, the next click goes on.
    /// The game stands still meanwhile.
    public static class Dialogue
    {
        static readonly string[] Ru =
        {
            "Ты... ты сломал клетку. Я думал, что сгнию там под их проклятые молитвы.",
            "Спасибо, что спас меня. Они звали меня еретиком - за то, что я каялся своей кровью, а не чужой.",
            "Мой цеп и моя вера теперь служат тебе. Найди меня в крепости - я буду ждать.",
        };
        static readonly string[] En =
        {
            "You... you broke the cage. I thought I would rot in there to their cursed prayers.",
            "Thank you for saving me. They called me a heretic - for atoning with my own blood, not someone else's.",
            "My flail and my faith serve you now. Find me in the fortress - I will be waiting.",
        };

        public static bool Open { get; private set; }

        public static void Show(Action onEnd)
        {
            if (Open) return;
            if (Runner.I == null) { onEnd?.Invoke(); return; }
            Runner.I.StartCoroutine(Run(onEnd));
        }

        static IEnumerator Run(Action onEnd)
        {
            Open = true;
            float scaleWas = Time.timeScale;
            GameObject root = null;
            try
            {
                root = Build(out var portrait, out var name, out var text, out var hint);
                Time.timeScale = 0f;
                var lines = Unlock.Russian() ? Ru : En;
                name.text = Unlock.Russian() ? "МУЧЕНИК" : "MARTYR";
                hint.text = Unlock.Russian() ? "нажмите, чтобы продолжить" : "click to continue";
                root.SetActive(true);
                Runner.I.StartCoroutine(FadeIn(root.GetComponent<CanvasGroup>()));
                Runner.I.StartCoroutine(Slide(portrait.rectTransform));
            }
            catch (Exception e) { RescuePlugin.Log.LogError("Cultist Rescue dialogue: " + e); }
            if (root != null)
            {
                var text = root.transform.Find("Box/Text").GetComponent<TextMeshProUGUI>();
                var hint = root.transform.Find("Box/Hint").GetComponent<TextMeshProUGUI>();
                var lines = Unlock.Russian() ? Ru : En;
                yield return WaitRealtime(0.35f);
                foreach (var line in lines)
                {
                    text.text = line; text.maxVisibleCharacters = 0; hint.enabled = false;
                    float shown = 0f; bool skip = false;
                    while (text.maxVisibleCharacters < line.Length)
                    {
                        if (Pressed()) skip = true;
                        shown += Time.unscaledDeltaTime * 45f;
                        text.maxVisibleCharacters = skip ? line.Length : Mathf.Min(line.Length, (int)shown);
                        yield return null;
                    }
                    hint.enabled = true;
                    yield return null;
                    while (!Pressed()) yield return null;
                    yield return null;
                }
            }
            Time.timeScale = scaleWas <= 0f ? 1f : scaleWas;
            if (root != null) UnityEngine.Object.Destroy(root);
            Open = false;
            try { onEnd?.Invoke(); } catch (Exception e) { RescuePlugin.Log.LogError("Cultist Rescue: after the dialogue: " + e); }
        }

        static bool Pressed() => Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)
                                 || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.JoystickButton0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);

        static IEnumerator WaitRealtime(float t) { float end = Time.unscaledTime + t; while (Time.unscaledTime < end) yield return null; }

        static IEnumerator FadeIn(CanvasGroup g)
        {
            if (g == null) yield break;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.35f) { if (g == null) yield break; g.alpha = t; yield return null; }
            if (g != null) g.alpha = 1f;
        }

        static IEnumerator Slide(RectTransform r)
        {
            if (r == null) yield break;
            Vector2 to = r.anchoredPosition, from = to + new Vector2(-120f, 0f);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.45f)
            {
                if (r == null) yield break;
                float e = 1f - (1f - t) * (1f - t);
                r.anchoredPosition = Vector2.Lerp(from, to, e); yield return null;
            }
            if (r != null) r.anchoredPosition = to;
        }

        static TMP_FontAsset Font()
        {
            try { return UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>(true).Select(t => t.font).FirstOrDefault(f => f != null); } catch { return null; }
        }

        static GameObject Build(out Image portrait, out TextMeshProUGUI name, out TextMeshProUGUI text, out TextMeshProUGUI hint)
        {
            var root = new GameObject("CultistRescue_Dialogue", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            root.SetActive(false);
            var cv = root.GetComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 32000;
            var sc = root.GetComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1280f, 720f); sc.matchWidthOrHeight = 1f;
            root.GetComponent<CanvasGroup>().alpha = 0f;
            var font = Font();

            // a dark veil over the game
            var veil = Child(root.transform, "Veil", typeof(Image)).GetComponent<Image>();
            Stretch(veil.rectTransform, Vector2.zero, Vector2.one); veil.color = new Color(0f, 0f, 0f, 0.62f);

            // his portrait, big, on the left, standing on the box
            portrait = Child(root.transform, "Portrait", typeof(Image)).GetComponent<Image>();
            portrait.sprite = Pics.Portrait(); portrait.preserveAspect = true; portrait.raycastTarget = false;
            var pr = portrait.rectTransform; pr.anchorMin = pr.anchorMax = new Vector2(0f, 0f); pr.pivot = new Vector2(0f, 0f);
            pr.sizeDelta = new Vector2(112f * 3f, 178f * 3f); pr.anchoredPosition = new Vector2(60f, 120f);

            // the box with his name and words
            var box = Child(root.transform, "Box", typeof(Image), typeof(Outline)).GetComponent<Image>();
            box.color = new Color(0.07f, 0.08f, 0.11f, 0.96f);
            var ol = box.GetComponent<Outline>(); ol.effectColor = new Color(0.55f, 0.42f, 0.22f, 1f); ol.effectDistance = new Vector2(3f, -3f);
            var br = box.rectTransform; br.anchorMin = new Vector2(0.04f, 0f); br.anchorMax = new Vector2(0.96f, 0f); br.pivot = new Vector2(0.5f, 0f);
            br.sizeDelta = new Vector2(0f, 190f); br.anchoredPosition = new Vector2(0f, 24f);

            name = Text(box.transform, "Name", font, 34f, new Color(1f, 0.65f, 0f, 1f), FontStyles.Bold);
            var nr = name.rectTransform; nr.anchorMin = new Vector2(0f, 1f); nr.anchorMax = new Vector2(1f, 1f); nr.pivot = new Vector2(0f, 1f);
            nr.sizeDelta = new Vector2(-60f, 46f); nr.anchoredPosition = new Vector2(30f, -14f);

            text = Text(box.transform, "Text", font, 30f, Color.white, FontStyles.Normal);
            var tr = text.rectTransform; tr.anchorMin = new Vector2(0f, 0f); tr.anchorMax = new Vector2(1f, 1f); tr.pivot = new Vector2(0f, 1f);
            tr.offsetMin = new Vector2(30f, 34f); tr.offsetMax = new Vector2(-30f, -62f);
            text.enableWordWrapping = true;

            hint = Text(box.transform, "Hint", font, 20f, new Color(0.7f, 0.7f, 0.75f, 0.9f), FontStyles.Italic);
            var hr = hint.rectTransform; hr.anchorMin = new Vector2(1f, 0f); hr.anchorMax = new Vector2(1f, 0f); hr.pivot = new Vector2(1f, 0f);
            hr.sizeDelta = new Vector2(420f, 30f); hr.anchoredPosition = new Vector2(-24f, 8f); hint.alignment = TextAlignmentOptions.BottomRight;

            portrait.transform.SetSiblingIndex(1);                                 // (the user) behind the box: the words stay readable, he stands above it
            return root;
        }

        static GameObject Child(Transform parent, string n, params Type[] comps)
        {
            var g = new GameObject(n, comps.Prepend(typeof(RectTransform)).Distinct().ToArray());
            g.transform.SetParent(parent, false);
            return g;
        }

        static void Stretch(RectTransform r, Vector2 min, Vector2 max) { r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero; }

        static TextMeshProUGUI Text(Transform parent, string n, TMP_FontAsset font, float size, Color col, FontStyles style)
        {
            var t = Child(parent, n, typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.fontSize = size; t.color = col; t.fontStyle = style; t.raycastTarget = false;
            t.alignment = TextAlignmentOptions.TopLeft;
            return t;
        }
    }
}
