using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CircleWar
{
    [DisallowMultipleComponent]
    public sealed class FacilitySegmentedProgressBar : MonoBehaviour
    {
        private const string GeneratedRootName = "Generated Segments";

        [Header("Sprites")]
        [SerializeField] private Sprite hollowSprite;
        [SerializeField] private Sprite solidSprite;

        [Header("Layout")]
        [Min(1)]
        [SerializeField] private int segmentCount = 10;
        [SerializeField] private Vector2 segmentSize = new Vector2(18f, 20f);
        [Min(0f)]
        [SerializeField] private float spacing = 6f;

        [Header("Style")]
        [SerializeField] private Color hollowColor = new Color(0.1f, 0.95f, 1f, 0.38f);
        [SerializeField] private Color solidColor = new Color(0.1f, 0.95f, 1f, 1f);
        [Min(0f)]
        [SerializeField] private float animationDuration = 0.18f;

        private readonly List<Image> segmentFills = new List<Image>();
        private Coroutine animationRoutine;
        private float displayedProgressPercent;
        private int progressPercent;
        private bool hasInitializedProgress;

        public int ProgressPercent => progressPercent;
        public float DisplayedProgressPercent => displayedProgressPercent;
        public int SegmentCount => segmentFills.Count;

        private void Awake()
        {
            RebuildVisuals();
            ApplyDisplayedProgress(progressPercent);
        }

        private void OnDisable()
        {
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
                animationRoutine = null;
            }

            displayedProgressPercent = progressPercent;
            ApplyDisplayedProgress(displayedProgressPercent);
        }

        public void SetProgressPercent(int percent, bool animate = true)
        {
            EnsureVisuals();
            int safePercent = Mathf.Clamp(percent, 0, 100);
            progressPercent = safePercent;

            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
                animationRoutine = null;
            }

            bool shouldAnimate = animate &&
                                 Application.isPlaying &&
                                 isActiveAndEnabled &&
                                 hasInitializedProgress &&
                                 animationDuration > 0f &&
                                 !Mathf.Approximately(displayedProgressPercent, safePercent);

            hasInitializedProgress = true;
            if (!shouldAnimate)
            {
                displayedProgressPercent = safePercent;
                ApplyDisplayedProgress(displayedProgressPercent);
                return;
            }

            animationRoutine = StartCoroutine(AnimateProgress(safePercent));
        }

        public float GetSegmentFillAmount(int index)
        {
            if (index < 0 || index >= segmentFills.Count)
            {
                return 0f;
            }

            return segmentFills[index].fillAmount;
        }

        [ContextMenu("Rebuild Visuals")]
        public void RebuildVisuals()
        {
            ClearGeneratedVisuals();
            segmentFills.Clear();

            int safeSegmentCount = Mathf.Max(1, segmentCount);
            float safeWidth = Mathf.Max(0f, segmentSize.x);
            float safeHeight = Mathf.Max(0f, segmentSize.y);
            float totalWidth = safeSegmentCount * safeWidth + (safeSegmentCount - 1) * Mathf.Max(0f, spacing);

            RectTransform rootRect = transform as RectTransform;
            if (rootRect != null)
            {
                rootRect.sizeDelta = new Vector2(totalWidth, safeHeight);
            }

            GameObject generatedRoot = new GameObject(GeneratedRootName, typeof(RectTransform));
            RectTransform generatedRect = generatedRoot.GetComponent<RectTransform>();
            generatedRect.SetParent(transform, false);
            generatedRect.anchorMin = new Vector2(0.5f, 0.5f);
            generatedRect.anchorMax = new Vector2(0.5f, 0.5f);
            generatedRect.pivot = new Vector2(0.5f, 0.5f);
            generatedRect.anchoredPosition = Vector2.zero;
            generatedRect.sizeDelta = new Vector2(totalWidth, safeHeight);

            float leftEdge = -totalWidth * 0.5f;
            for (int index = 0; index < safeSegmentCount; index++)
            {
                GameObject segmentObject = new GameObject(
                    string.Format("Block_{0:00}", index),
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                RectTransform segmentRect = segmentObject.GetComponent<RectTransform>();
                segmentRect.SetParent(generatedRect, false);
                segmentRect.anchorMin = new Vector2(0.5f, 0.5f);
                segmentRect.anchorMax = new Vector2(0.5f, 0.5f);
                segmentRect.pivot = new Vector2(0.5f, 0.5f);
                segmentRect.sizeDelta = new Vector2(safeWidth, safeHeight);
                segmentRect.anchoredPosition = new Vector2(
                    leftEdge + safeWidth * 0.5f + index * (safeWidth + spacing),
                    0f);

                Image hollowImage = segmentObject.GetComponent<Image>();
                ConfigureImage(hollowImage, hollowSprite, hollowColor, Image.Type.Simple);

                GameObject fillObject = new GameObject(
                    "Fill",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                RectTransform fillRect = fillObject.GetComponent<RectTransform>();
                fillRect.SetParent(segmentRect, false);
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;

                Image fillImage = fillObject.GetComponent<Image>();
                ConfigureImage(fillImage, solidSprite, solidColor, Image.Type.Filled);
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                fillImage.fillClockwise = true;
                fillImage.fillAmount = 0f;
                segmentFills.Add(fillImage);
            }

            ApplyDisplayedProgress(displayedProgressPercent);
        }

        private IEnumerator AnimateProgress(float targetPercent)
        {
            float startPercent = displayedProgressPercent;
            float elapsed = 0f;

            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / animationDuration);
                float easedTime = 1f - Mathf.Pow(1f - normalizedTime, 3f);
                displayedProgressPercent = Mathf.LerpUnclamped(startPercent, targetPercent, easedTime);
                ApplyDisplayedProgress(displayedProgressPercent);
                yield return null;
            }

            displayedProgressPercent = targetPercent;
            ApplyDisplayedProgress(displayedProgressPercent);
            animationRoutine = null;
        }

        private void EnsureVisuals()
        {
            if (segmentFills.Count != Mathf.Max(1, segmentCount) || segmentFills.Exists(image => image == null))
            {
                RebuildVisuals();
            }
        }

        private void ApplyDisplayedProgress(float percent)
        {
            if (segmentFills.Count == 0)
            {
                return;
            }

            float scaledProgress = Mathf.Clamp(percent, 0f, 100f) / 100f * segmentFills.Count;
            for (int index = 0; index < segmentFills.Count; index++)
            {
                Image fillImage = segmentFills[index];
                if (fillImage != null)
                {
                    fillImage.fillAmount = Mathf.Clamp01(scaledProgress - index);
                }
            }
        }

        private void ClearGeneratedVisuals()
        {
            Transform existingRoot = transform.Find(GeneratedRootName);
            if (existingRoot == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(existingRoot.gameObject);
            }
            else
            {
                DestroyImmediate(existingRoot.gameObject);
            }
        }

        private static void ConfigureImage(Image image, Sprite sprite, Color color, Image.Type imageType)
        {
            image.sprite = sprite;
            image.color = color;
            image.type = imageType;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.maskable = true;
        }
    }
}
