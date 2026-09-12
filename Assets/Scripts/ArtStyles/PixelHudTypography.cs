using UnityEngine;
using UnityEngine.UI;

namespace CircleWar
{
    /// <summary>Scene-local optical sizes for the existing HUD and its generated messages.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public sealed class PixelHudTypography : MonoBehaviour
    {
        [SerializeField] private Font smallFont;
        [Tooltip("Full 16-dot Chinese glyphs for readable body text.")]
        [SerializeField] private Font readableFont;
        [SerializeField] private Font mediumFont;
        [SerializeField] private Font largeFont;
        [Tooltip("Short HUD labels that must fit the existing narrow frames.")]
        [SerializeField] private Text[] compactTexts = new Text[0];

        private void OnEnable()
        {
            Font.textureRebuilt += OnFontTextureRebuilt;
            ApplyToExistingText();
        }

        private void OnDisable() => Font.textureRebuilt -= OnFontTextureRebuilt;

        public void ApplyToExistingText()
        {
            foreach (Text text in GetComponentsInChildren<Text>(true)) Apply(text);
        }

        public void Apply(Text text)
        {
            if (text == null || smallFont == null || mediumFont == null || largeFont == null) return;
            int requestedSize = text.fontSize;
            if (requestedSize <= 22)
            {
                bool compact = compactTexts != null && System.Array.IndexOf(compactTexts, text) >= 0;
                // Integer 2x scaling keeps every authored dot an even square.
                text.font = compact ? smallFont : mediumFont;
                text.fontSize = compact ? 16 : 20;
                // Fusion 10px has a 28px line box at 2x: 0.75 gives a 21px advance,
                // keeping 20px glyphs distinct without the original oversized gaps.
                text.lineSpacing = compact ? 1f : 0.75f;
            }
            else if (requestedSize <= 26) { text.font = largeFont; text.fontSize = 24; }
            else if (requestedSize <= 30) { text.font = mediumFont; text.fontSize = 30; }
            else if (requestedSize <= 34) { text.font = readableFont != null ? readableFont : smallFont; text.fontSize = 32; }
            else { text.font = largeFont; text.fontSize = Mathf.Max(36, Mathf.RoundToInt(requestedSize / 12f) * 12); }

            // Synthetic bold and automatic fitting distort the authored pixel grid.
            // Large message sizes and their existing outline retain the feedback emphasis.
            text.fontStyle = FontStyle.Normal;
            text.resizeTextForBestFit = false;
            OnFontTextureRebuilt(text.font);
        }

        public static void ApplyToGeneratedText(Text text, Text reference)
        {
            Canvas canvas = reference != null ? reference.GetComponentInParent<Canvas>()
                : text != null ? text.GetComponentInParent<Canvas>() : null;
            if (canvas != null && canvas.TryGetComponent(out PixelHudTypography style) && style.isActiveAndEnabled)
                style.Apply(text);
        }

        private void OnFontTextureRebuilt(Font font)
        {
            if (font != smallFont && font != mediumFont && font != largeFont && font != readableFont) return;
            if (font != null && font.material != null && font.material.mainTexture != null)
                font.material.mainTexture.filterMode = FilterMode.Point;
        }
    }
}
