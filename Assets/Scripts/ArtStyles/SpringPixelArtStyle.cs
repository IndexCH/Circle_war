using System;
using UnityEngine;

namespace CircleWar
{
    [CreateAssetMenu(fileName = "SpringPixelArtStyle", menuName = "Circle War/Art/Spring Pixel Style")]
    public sealed class SpringPixelArtStyle : ScriptableObject
    {
        [Serializable]
        public sealed class SpritePair
        {
            public Sprite original;
            public Sprite pixel;
            public Texture3D originalColorLut;
            public Sprite groundContact;
        }

        [SerializeField] private bool enabledByDefault = true;
        [SerializeField] private SpritePair[] sprites = Array.Empty<SpritePair>();
        private static SpringPixelArtStyle cached;
        private static bool loaded;
        private static SpringPixelArtStyle cachedSummer;
        private static bool summerLoaded;
        private static bool? runtimeEnabled;

        private static SpringPixelArtStyle Settings
        {
            get
            {
                if (!loaded)
                {
                    cached = Resources.Load<SpringPixelArtStyle>("ArtStyles/SpringPixelArtStyle");
                    loaded = true;
                }
                return cached;
            }
        }

        // Keep the original class and resource key to preserve existing serialized assets.
        private static SpringPixelArtStyle SummerSettings
        {
            get
            {
                if (!summerLoaded)
                {
                    cachedSummer = Resources.Load<SpringPixelArtStyle>("ArtStyles/SummerPixelArtStyle");
                    summerLoaded = true;
                }
                return cachedSummer;
            }
        }

        private static SpringPixelArtStyle SettingsFor(SeasonDefinition season)
        {
            if (season == null) return null;
            switch (season.DefinitionId)
            {
                case "spring": return Settings;
                case "summer": return SummerSettings;
                default: return null;
            }
        }

        public static bool SupportsSeason(SeasonDefinition season) => SettingsFor(season) != null;

        public static bool IsEnabled => runtimeEnabled ?? (Settings != null && Settings.enabledByDefault);
        public static void SetEnabled(bool enabled) => runtimeEnabled = enabled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            cached = null;
            loaded = false;
            cachedSummer = null;
            summerLoaded = false;
            runtimeEnabled = null;
        }

        public static Texture3D OriginalColorLut(Sprite pixel)
        {
            if (pixel == null) return null;
            Texture3D table = FindOriginalColorLut(Settings, pixel);
            return table != null ? table : FindOriginalColorLut(SummerSettings, pixel);
        }

        private static Texture3D FindOriginalColorLut(SpringPixelArtStyle settings, Sprite pixel)
        {
            if (settings == null) return null;
            foreach (SpritePair pair in settings.sprites)
                if (pair != null && pair.pixel == pixel) return pair.originalColorLut;
            return null;
        }

        public static Sprite GroundContact(SeasonDefinition season, Sprite original)
        {
            if (original == null || !IsEnabled) return null;
            SpringPixelArtStyle settings = SettingsFor(season);
            if (settings == null) return null;
            foreach (SpritePair pair in settings.sprites)
                if (pair != null && pair.original == original && pair.pixel != null) return pair.groundContact;
            return null;
        }

        public static Sprite Resolve(SeasonDefinition season, Sprite original, out Vector3 scale)
        {
            scale = Vector3.one;
            if (original == null || !IsEnabled)
            {
                return original;
            }

            SpringPixelArtStyle settings = SettingsFor(season);
            if (settings == null)
            {
                return original;
            }

            foreach (SpritePair pair in settings.sprites)
            {
                if (pair == null || pair.original != original || pair.pixel == null)
                {
                    continue;
                }

                // Preserve the source's world footprint even when the generated canvas differs.
                Vector3 sourceSize = original.bounds.size;
                Vector3 pixelSize = pair.pixel.bounds.size;
                if (pixelSize.x <= Mathf.Epsilon || pixelSize.y <= Mathf.Epsilon)
                {
                    return original;
                }
                scale = new Vector3(sourceSize.x / pixelSize.x, sourceSize.y / pixelSize.y, 1f);
                return pair.pixel;
            }
            return original;
        }
    }
}
