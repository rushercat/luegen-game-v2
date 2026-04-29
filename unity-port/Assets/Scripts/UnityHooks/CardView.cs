// Lügen — CardView.cs
// Single-card UI widget. Wraps an Image (the card body) plus children:
//   - rank text (the big "A" / "K" / "Q" / "10" / "J" in the center)
//   - affix bar (a colored strip at the top showing the affix)
//   - selection halo (the yellow ring when selected)
//
// CardView can render a card "face-up" (showing rank + affix) or
// "face-down" (just the back). It's a Button so it can be clicked.

#if UNITY_5_3_OR_NEWER
using System;
using Lugen.Affixes;
using Lugen.Cards;
using UnityEngine;
using UnityEngine.UI;

namespace Lugen.UnityHooks
{
    public class CardView : MonoBehaviour
    {
        public Card cardData;
        public Action<CardView> onClick;

        private Image bodyImage;
        private Image affixBar;
        private Image selectionHalo;
        private Text rankText;
        private Text affixText;
        private Button button;
        private bool isSelected;
        private bool isFaceDown;

        // Dimensions
        public const float CARD_WIDTH = 70f;
        public const float CARD_HEIGHT = 100f;

        // Colors
        public static readonly Color FACE_BG = new Color(0.97f, 0.97f, 0.97f);
        public static readonly Color FACE_FG = Color.black;
        public static readonly Color BACK_BG = new Color(0.10f, 0.18f, 0.45f);   // dark blue
        public static readonly Color BACK_BG2 = new Color(0.25f, 0.20f, 0.55f); // indigo accent
        public static readonly Color SELECTED_HALO = new Color(0.93f, 0.72f, 0.05f);

        public static Color AffixColor(Affix a)
        {
            switch (a)
            {
                case Affix.Gilded: return new Color(0.98f, 0.80f, 0.18f);
                case Affix.Glass:  return new Color(0.13f, 0.83f, 0.93f);
                case Affix.Spiked: return new Color(0.95f, 0.45f, 0.45f);
                case Affix.Cursed: return new Color(0.66f, 0.33f, 0.97f);
                case Affix.Steel:  return new Color(0.80f, 0.80f, 0.85f);
                case Affix.Mirage: return new Color(0.95f, 0.45f, 0.71f);
                case Affix.Hollow: return new Color(0.50f, 0.55f, 0.97f);
                case Affix.Echo:   return new Color(0.91f, 0.47f, 0.98f);
                default:           return new Color(0, 0, 0, 0);
            }
        }

        // Build the visual hierarchy programmatically. parent is the layout
        // group / holder; this card creates itself underneath.
        public static CardView Create(Transform parent, Card data, bool faceDown, Action<CardView> onClick = null)
        {
            var go = new GameObject("Card_" + (data?.id ?? "?"));
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(CARD_WIDTH, CARD_HEIGHT);

            var view = go.AddComponent<CardView>();
            view.cardData = data;
            view.onClick = onClick;

            // Selection halo (slightly larger than card, behind everything).
            var halo = new GameObject("Halo");
            halo.transform.SetParent(go.transform, false);
            var haloRT = halo.AddComponent<RectTransform>();
            haloRT.anchorMin = Vector2.zero; haloRT.anchorMax = Vector2.one;
            haloRT.offsetMin = new Vector2(-4, -4); haloRT.offsetMax = new Vector2(4, 4);
            view.selectionHalo = halo.AddComponent<Image>();
            view.selectionHalo.color = SELECTED_HALO;
            view.selectionHalo.enabled = false;

            // Body
            view.bodyImage = go.AddComponent<Image>();
            view.bodyImage.color = FACE_BG;

            // Affix bar (top strip)
            var ab = new GameObject("AffixBar");
            ab.transform.SetParent(go.transform, false);
            var abRT = ab.AddComponent<RectTransform>();
            abRT.anchorMin = new Vector2(0, 1); abRT.anchorMax = new Vector2(1, 1);
            abRT.pivot = new Vector2(0.5f, 1f);
            abRT.sizeDelta = new Vector2(0, 14);
            view.affixBar = ab.AddComponent<Image>();
            view.affixBar.color = new Color(0, 0, 0, 0);

            // Affix text (top-right small)
            var at = new GameObject("AffixText");
            at.transform.SetParent(go.transform, false);
            var atRT = at.AddComponent<RectTransform>();
            atRT.anchorMin = new Vector2(0, 1); atRT.anchorMax = new Vector2(1, 1);
            atRT.pivot = new Vector2(0.5f, 1f);
            atRT.sizeDelta = new Vector2(0, 14);
            view.affixText = at.AddComponent<Text>();
            view.affixText.font = LegacyFont();
            view.affixText.fontSize = 10;
            view.affixText.alignment = TextAnchor.MiddleCenter;
            view.affixText.color = Color.black;

            // Rank text (big center)
            var rt2 = new GameObject("Rank");
            rt2.transform.SetParent(go.transform, false);
            var rt2RT = rt2.AddComponent<RectTransform>();
            rt2RT.anchorMin = Vector2.zero; rt2RT.anchorMax = Vector2.one;
            rt2RT.offsetMin = new Vector2(0, 0); rt2RT.offsetMax = Vector2.zero;
            view.rankText = rt2.AddComponent<Text>();
            view.rankText.font = LegacyFont();
            view.rankText.fontSize = 36;
            view.rankText.fontStyle = FontStyle.Bold;
            view.rankText.alignment = TextAnchor.MiddleCenter;
            view.rankText.color = FACE_FG;

            // Click handler.
            view.button = go.AddComponent<Button>();
            view.button.transition = Selectable.Transition.None;
            view.button.onClick.AddListener(() => view.onClick?.Invoke(view));

            view.SetFaceDown(faceDown);
            return view;
        }

        public void SetSelected(bool sel)
        {
            isSelected = sel;
            if (selectionHalo) selectionHalo.enabled = sel;
            // Lift selected cards visually with a small Y offset.
            var rt = (RectTransform)transform;
            var pos = rt.anchoredPosition;
            pos.y = sel ? 14f : 0f;
            rt.anchoredPosition = pos;
        }

        public void SetFaceDown(bool faceDown)
        {
            isFaceDown = faceDown;
            if (faceDown)
            {
                bodyImage.color = BACK_BG;
                rankText.text = "";
                affixBar.color = new Color(0, 0, 0, 0);
                affixText.text = "";
            }
            else
            {
                bodyImage.color = FACE_BG;
                if (cardData != null)
                {
                    rankText.text = cardData.rank.ToShort();
                    rankText.color = (cardData.rank == Rank.Jack) ? new Color(0.85f, 0.10f, 0.10f) : Color.black;
                    if (cardData.affix != Affix.None)
                    {
                        var c = AffixColor(cardData.affix);
                        affixBar.color = c;
                        affixText.text = cardData.affix.ToShort().ToUpperInvariant();
                        affixText.color = (cardData.affix == Affix.Steel || cardData.affix == Affix.Gilded)
                            ? Color.black : Color.white;
                    }
                    else
                    {
                        affixBar.color = new Color(0, 0, 0, 0);
                        affixText.text = "";
                    }
                }
            }
        }

        // Try to use the built-in legacy "LegacyRuntime" font (Unity ships it
        // with Editor). Falls back to "Arial" or builtin default.
        private static Font cachedFont;
        public static Font LegacyFont()
        {
            if (cachedFont != null) return cachedFont;
            cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (cachedFont == null) cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return cachedFont;
        }
    }
}
#endif
