using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelShooting.View
{
    /// <summary>
    /// Shared code styling for HUD widgets (popup, hint button), so they match without hand-tuning
    /// every object in the scene. Sizes are Canvas Scaler reference pixels (1125 x 2436).
    /// </summary>
    public static class UiStyle
    {
        /// <summary>Rounded, tinted, 9-sliced panel. Lower cornerScale = bigger corners.</summary>
        public static void Rounded(Image image, Color color, float cornerScale)
        {
            image.sprite = RoundedSprite.Get();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = cornerScale;
            image.color = color;
        }

        public static void Text(TMP_Text text, float size, Color color, bool bold)
        {
            text.enableAutoSizing = false;
            text.fontSize = size;
            text.color = color;
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
        }

        public static void Shadow(GameObject go, Vector2 offset, float alpha)
        {
            if (!go.TryGetComponent(out Shadow shadow)) shadow = go.AddComponent<Shadow>();
            shadow.effectDistance = offset;
            shadow.effectColor = new Color(0f, 0f, 0f, alpha);
        }

        /// <summary>Pressed = slightly darker; the Image color stays the accent color.</summary>
        public static void ButtonColors(Button button)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        /// <summary>Makes a label fill its parent (e.g. a button's text).</summary>
        public static void Fill(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
