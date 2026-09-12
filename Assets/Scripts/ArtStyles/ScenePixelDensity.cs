using UnityEngine;

namespace CircleWar
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ScenePixelDensity : MonoBehaviour
    {
        private static Material sharedPixelMaterial;
        private static Material groundPixelMaterial;
        private static Material enemyPixelMaterial;
        private SpriteRenderer target;
        private Material originalMaterial;
        private Material appliedMaterial;
        private bool enemyVisual;
        private MaterialPropertyBlock colorProperties;
        private static readonly int OriginalColorEnabled = Shader.PropertyToID("_OriginalColorEnabled");
        private static readonly int OriginalColorLut = Shader.PropertyToID("_OriginalColorLut");
        private static readonly int MainTexture = Shader.PropertyToID("_MainTex");
        private static readonly int SpringOutlineEnabled = Shader.PropertyToID("_SpringOutlineEnabled");
        private static readonly int SpringOutlineColor = Shader.PropertyToID("_SpringOutlineColor");
        private static readonly Color OutlineColor = new Color(0.16f, 0.14f, 0.11f, 1f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            sharedPixelMaterial = groundPixelMaterial = enemyPixelMaterial = null;
        }

        private void Awake() => Initialize(GetComponent<SpriteRenderer>());

        private void OnEnable()
        {
            if (enemyVisual) ApplyEnemy(GetComponent<SpriteRenderer>());
        }

        private void Initialize(SpriteRenderer renderer)
        {
            if (target != null) return;
            target = renderer;
            originalMaterial = renderer.sharedMaterial;
        }

        public static void Apply(SpriteRenderer renderer, bool usePixelArt, bool useSpringOutline = false)
        {
            if (usePixelArt && sharedPixelMaterial == null)
                sharedPixelMaterial = Resources.Load<Material>("ArtStyles/ScenePixelDensity");
            ApplyMaterial(renderer, usePixelArt, sharedPixelMaterial, false, useSpringOutline);
        }

        public static void ApplyGround(SpriteRenderer renderer, bool usePixelArt)
        {
            if (usePixelArt && groundPixelMaterial == null)
                groundPixelMaterial = Resources.Load<Material>("ArtStyles/CircleGroundPixelDensity");
            ApplyMaterial(renderer, usePixelArt, groundPixelMaterial, false);
        }

        public static void ApplyEnemy(SpriteRenderer renderer)
        {
            if (enemyPixelMaterial == null)
                enemyPixelMaterial = Resources.Load<Material>("ArtStyles/EnemyPixelDensity");
            ApplyMaterial(renderer, SpringPixelArtStyle.IsEnabled, enemyPixelMaterial, true);
        }

        // Style changes are infrequent. Enemies need no per-frame scans or material instances.
        public static void RefreshEnemies()
        {
            foreach (var style in FindObjectsByType<ScenePixelDensity>())
                if (style.enemyVisual) ApplyEnemy(style.target);
        }

        private static void ApplyMaterial(SpriteRenderer renderer, bool usePixelArt, Material material, bool enemy, bool outline = false)
        {
            if (renderer == null) return;
            var style = renderer.GetComponent<ScenePixelDensity>();
            // Keep a registration even when an enemy spawns with pixel mode switched off.
            if (style == null && (usePixelArt || enemy)) style = renderer.gameObject.AddComponent<ScenePixelDensity>();
            if (style == null) return;
            style.Initialize(renderer);
            style.enemyVisual = enemy;
            if (!usePixelArt || material == null) { style.Restore(); return; }
            renderer.sharedMaterial = material;
            style.appliedMaterial = material;
            style.ApplyOriginalColors(enemy ? null : SpringPixelArtStyle.OriginalColorLut(renderer.sprite), !enemy, outline);
        }

        private void ApplyOriginalColors(Texture3D table, bool bindSpriteTexture, bool outline)
        {
            if (target == null) return;
            if (colorProperties == null) colorProperties = new MaterialPropertyBlock();
            // GetPropertyBlock also reads Unity's per-sprite texture binding. Writing that
            // snapshot back can pin the previous sprite after a season/style or frame change.
            // Explicitly bind the current static environment sprite when applying its style.
            // Animated NPCs never take this path, and enemy frame binding remains with Unity.
            colorProperties.Clear();
            if (bindSpriteTexture && target.sprite != null)
                colorProperties.SetTexture(MainTexture, target.sprite.texture);
            colorProperties.SetFloat(OriginalColorEnabled, table != null ? 1f : 0f);
            if (table != null) colorProperties.SetTexture(OriginalColorLut, table);
            // Only map props opt in; every application resets the pooled renderer's flag.
            colorProperties.SetFloat(SpringOutlineEnabled, outline ? 1f : 0f);
            colorProperties.SetVector(SpringOutlineColor,
                QualitySettings.activeColorSpace == ColorSpace.Linear ? OutlineColor.linear : OutlineColor);
            target.SetPropertyBlock(colorProperties);
        }

        private void Restore()
        {
            if (target != null && appliedMaterial != null && target.sharedMaterial == appliedMaterial)
            {
                target.sharedMaterial = originalMaterial;
                colorProperties?.Clear();
                // Release our overrides so a pooled prop can become an animated NPC again.
                target.SetPropertyBlock(null);
            }
        }

        private void OnDestroy() => Restore();
    }
}
