using UnityEngine;
using UnityEngine.UI;

namespace CircleWar
{
    /// <summary>Pixel sampling of the existing UI sprites, preserving their layout and tint.</summary>
    public static class OriginalHudPixelStyle
    {
        private static Material pixelMaterial;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() => pixelMaterial = null;

        public static void Apply(GameHud hud, Image portrait, bool enabled)
        {
            if (pixelMaterial == null)
                pixelMaterial = Resources.Load<Material>("ArtStyles/OriginalHudPixel");
            if (pixelMaterial == null || hud == null) return;

            foreach (Image image in hud.GetComponentsInChildren<Image>(true))
            {
                // Portraits belong to the character art; custom effects keep their own material.
                if (image == portrait) continue;
                if (enabled && (image.material == image.defaultMaterial || image.material == pixelMaterial))
                    image.material = pixelMaterial;
                else if (!enabled && image.material == pixelMaterial)
                    image.material = null;
            }
        }
    }
}
