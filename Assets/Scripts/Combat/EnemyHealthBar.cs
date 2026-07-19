using UnityEngine;

namespace CircleWar
{
    public sealed class EnemyHealthBar : MonoBehaviour
    {
        private const float MinimumWidth = 0.75f;
        private const float MaximumWidth = 1.3f;
        private const float FillHeight = 0.08f;
        private const float BorderSize = 0.02f;
        private const float VerticalGap = 0.08f;
        private const int BackgroundSortingOffset = 3;
        private const int FillSortingOffset = 4;

        private static readonly Color BackgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.92f);
        private static readonly Color FillColor = new Color(0.9f, 0.16f, 0.14f, 1f);
        private static Sprite squareSprite;

        [SerializeField] private Transform barRoot;
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private SpriteRenderer fillRenderer;

        private ICombatEnemy healthSource;
        private SpriteRenderer anchorRenderer;

        public float NormalizedHealth { get; private set; }

        public void Configure(ICombatEnemy newHealthSource, SpriteRenderer newAnchorRenderer)
        {
            healthSource = newHealthSource;
            anchorRenderer = newAnchorRenderer;
            EnsureVisuals();
            RefreshVisual();
        }

        public void RefreshVisual()
        {
            EnsureVisuals();
            if (!IsSourceAvailable() || anchorRenderer == null)
            {
                NormalizedHealth = 0f;
                SetVisible(false);
                return;
            }

            int currentHealth = Mathf.Max(0, healthSource.CurrentHealth);
            int maxHealth = Mathf.Max(1, healthSource.MaxHealth);
            NormalizedHealth = Mathf.Clamp01((float)currentHealth / maxHealth);
            bool isVisible = healthSource.IsAlive && currentHealth > 0;
            SetVisible(isVisible);
            if (!isVisible)
            {
                return;
            }

            UpdateSorting();
            UpdateLayout();
        }

        private void Awake()
        {
            EnsureVisuals();
            SetVisible(false);
        }

        private void LateUpdate()
        {
            RefreshVisual();
        }

        private bool IsSourceAvailable()
        {
            if (healthSource == null)
            {
                return false;
            }

            if (healthSource is UnityEngine.Object unityObject && unityObject == null)
            {
                return false;
            }

            return true;
        }

        private void EnsureVisuals()
        {
            if (barRoot == null)
            {
                GameObject rootObject = new GameObject("Health Bar");
                rootObject.layer = gameObject.layer;
                rootObject.transform.SetParent(transform, false);
                barRoot = rootObject.transform;
            }

            if (backgroundRenderer == null)
            {
                backgroundRenderer = CreateRenderer("Background", BackgroundColor);
            }

            if (fillRenderer == null)
            {
                fillRenderer = CreateRenderer("Fill", FillColor);
            }
        }

        private SpriteRenderer CreateRenderer(string objectName, Color color)
        {
            GameObject rendererObject = new GameObject(objectName);
            rendererObject.layer = gameObject.layer;
            rendererObject.transform.SetParent(barRoot, false);

            SpriteRenderer renderer = rendererObject.AddComponent<SpriteRenderer>();
            renderer.sprite = GetSquareSprite();
            renderer.color = color;
            return renderer;
        }

        private void SetVisible(bool isVisible)
        {
            if (backgroundRenderer != null)
            {
                backgroundRenderer.enabled = isVisible;
            }

            if (fillRenderer != null)
            {
                fillRenderer.enabled = isVisible;
            }
        }

        private void UpdateSorting()
        {
            backgroundRenderer.sortingLayerID = anchorRenderer.sortingLayerID;
            backgroundRenderer.sortingOrder = anchorRenderer.sortingOrder + BackgroundSortingOffset;
            fillRenderer.sortingLayerID = anchorRenderer.sortingLayerID;
            fillRenderer.sortingOrder = anchorRenderer.sortingOrder + FillSortingOffset;
        }

        private void UpdateLayout()
        {
            Bounds anchorBounds = anchorRenderer.bounds;
            float sourceWidth = anchorRenderer.sprite != null
                ? anchorRenderer.sprite.bounds.size.x * Mathf.Abs(anchorRenderer.transform.lossyScale.x)
                : anchorBounds.size.x;
            float barWidth = Mathf.Clamp(sourceWidth, MinimumWidth, MaximumWidth);
            float fillWidth = barWidth * NormalizedHealth;

            barRoot.SetPositionAndRotation(
                new Vector3(anchorBounds.center.x, anchorBounds.max.y + VerticalGap, transform.position.z),
                Quaternion.identity);
            barRoot.localScale = Vector3.one;

            backgroundRenderer.transform.localPosition = Vector3.zero;
            backgroundRenderer.transform.localRotation = Quaternion.identity;
            backgroundRenderer.transform.localScale = new Vector3(
                barWidth + BorderSize * 2f,
                FillHeight + BorderSize * 2f,
                1f);

            fillRenderer.transform.localPosition = new Vector3((fillWidth - barWidth) * 0.5f, 0f, 0f);
            fillRenderer.transform.localRotation = Quaternion.identity;
            fillRenderer.transform.localScale = new Vector3(fillWidth, FillHeight, 1f);
        }

        private static Sprite GetSquareSprite()
        {
            if (squareSprite != null)
            {
                return squareSprite;
            }

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.name = "Runtime Enemy Health Bar Texture";
            texture.filterMode = FilterMode.Point;
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            squareSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            squareSprite.name = "Runtime Enemy Health Bar";
            return squareSprite;
        }
    }
}
