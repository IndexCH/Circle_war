using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace CircleWar
{
    [DisallowMultipleComponent]
    public sealed class ResourceFeelFeedback : MonoBehaviour
    {
        [SerializeField] private FeelFeedbackSettings settings;
        private readonly Toast[] gainToasts = new Toast[3];
        private Toast deniedToast;
        private GameObject root;
        private RectTransform gainRoot;
        private CanvasGroup gainVisibility;
        private RectTransform canvasRect;
        private Canvas targetCanvas;
        private Transform gainTarget;
        private Camera gameplayCamera;
        private float nextDeniedTime;
        private int nextGainSlot;

        private sealed class Toast
        {
            public Text Text;
            public CanvasGroup Group;
            public MMF_Player Player;
            public string ResourceId;
            public int Amount;
            public float ExpiresAt;
        }

        public void Configure(Text foodText, Text industryText)
        {
            if (root != null) return;
            if (settings == null) settings = FeelFeedbackSettings.Load();
            Text reference = industryText != null ? industryText : foodText;
            Canvas canvas = reference != null ? reference.GetComponentInParent<Canvas>() : GetComponentInParent<Canvas>();
            if (canvas == null || settings == null) return;
            targetCanvas = canvas;
            canvasRect = (RectTransform)canvas.transform;
            root = new GameObject("FEEL Resource Messages", typeof(RectTransform));
            RectTransform rect = (RectTransform)root.transform;
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -155f);
            rect.sizeDelta = new Vector2(460f, 180f);
            gainRoot = (RectTransform)new GameObject("FEEL Player Resource Gains", typeof(RectTransform)).transform;
            gainRoot.SetParent(canvas.transform, false);
            gainRoot.anchorMin = gainRoot.anchorMax = new Vector2(0.5f, 0.5f);
            gainRoot.pivot = Vector2.zero;
            gainRoot.sizeDelta = new Vector2(260f, (settings.GainFontSize + settings.GainRowGap) * gainToasts.Length);
            gainVisibility = gainRoot.gameObject.AddComponent<CanvasGroup>();
            gainVisibility.interactable = false;
            gainVisibility.blocksRaycasts = false;
            for (int i = 0; i < gainToasts.Length; i++)
                gainToasts[i] = CreateToast("Resource Gain " + i, reference,
                    0f, settings.GainColor, settings.GainSound, true);
            deniedToast = CreateToast("Resource Insufficient", reference, -116f, settings.DeniedColor, settings.DeniedSound, false);
        }

        private Toast CreateToast(string name, Text reference, float y, Color color, AudioClip clip, bool isGain)
        {
            GameObject owner = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(Text), typeof(Outline));
            RectTransform rect = (RectTransform)owner.transform;
            rect.SetParent(isGain ? gainRoot : root.transform, false);
            rect.anchorMin = rect.anchorMax = isGain ? Vector2.zero : new Vector2(0.5f, 1f);
            rect.pivot = isGain ? Vector2.zero : new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = isGain ? new Vector2(260f, settings.GainFontSize + 4f) : new Vector2(440f, 34f);
            Text text = owner.GetComponent<Text>();
            text.font = reference != null ? reference.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = isGain ? settings.GainFontSize : 22;
            text.fontStyle = isGain ? FontStyle.Bold : FontStyle.Normal;
            PixelHudTypography.ApplyToGeneratedText(text, reference);
            text.alignment = isGain ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            owner.GetComponent<Outline>().effectColor = new Color(0.03f, 0.05f, 0.08f, 0.9f);
            if (isGain) owner.GetComponent<Outline>().effectDistance = new Vector2(1.5f, -1.5f);
            CanvasGroup group = owner.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            MMF_Player player = FeelFeedbackFactory.CreatePlayer(rect, "FEEL Message");
            player.AddFeedback(new MMF_CanvasGroup
            {
                Label = "Message fade",
                TargetCanvasGroup = group,
                Duration = settings.MessageDuration,
                AlphaCurve = new MMTweenType(new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.06f, 1),
                    new Keyframe(0.68f, 1), new Keyframe(1, 0)))
            });
            if (!isGain) FeelFeedbackFactory.AddMotion(player, owner, 16f, settings.MessageDuration, false);
            FeelFeedbackFactory.AddSound(player, clip, settings, 0.6f);
            player.ForceTimescaleMode = true;
            player.ForcedTimescaleMode = TimescaleModes.Unscaled;
            player.PlayerTimescaleMode = TimescaleModes.Unscaled;
            player.Initialization();
            return new Toast { Text = text, Group = group, Player = player };
        }

        public void ShowGain(string resourceId, string displayName, int amount, bool isIndustry = false)
        {
            if (root == null || amount <= 0) return;
            ResolveGainTarget();
            UpdateGainPosition();
            Toast toast = null;
            foreach (Toast candidate in gainToasts)
            {
                if (candidate.ResourceId == resourceId && candidate.ExpiresAt > Time.unscaledTime)
                { toast = candidate; break; }
            }
            if (toast == null)
            {
                foreach (Toast candidate in gainToasts)
                    if (candidate.ExpiresAt <= Time.unscaledTime) { toast = candidate; break; }
                if (toast == null)
                {
                    toast = gainToasts[nextGainSlot];
                    nextGainSlot = (nextGainSlot + 1) % gainToasts.Length;
                }
                toast.Amount = 0;
            }
            toast.ResourceId = resourceId;
            toast.Amount += amount;
            toast.Text.text = displayName + " +" + toast.Amount;
            toast.Text.color = isIndustry ? settings.IndustryGainColor : settings.GainColor;
            toast.ExpiresAt = Time.unscaledTime + settings.MessageDuration;
            FeelFeedbackFactory.Replay(toast.Player);
            CompactGainRows();
        }

        private void ResolveGainTarget()
        {
            if (gainTarget == null)
            {
                PlayerAimShooter player = FindAnyObjectByType<PlayerAimShooter>();
                if (player != null) gainTarget = player.transform;
            }
            if (gameplayCamera == null) gameplayCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (gainRoot == null) return;
            CompactGainRows();
            foreach (Toast toast in gainToasts)
            {
                if (toast != null && toast.ExpiresAt > Time.unscaledTime)
                {
                    UpdateGainPosition();
                    return;
                }
            }
        }

        private void CompactGainRows()
        {
            int row = 0;
            float spacing = settings.GainFontSize + settings.GainRowGap;
            foreach (Toast toast in gainToasts)
            {
                if (toast.ExpiresAt <= Time.unscaledTime) continue;
                toast.Text.rectTransform.anchoredPosition = new Vector2(0f, row++ * spacing);
            }
            gainRoot.sizeDelta = new Vector2(260f, Mathf.Max(1, row) * spacing);
        }

        private void UpdateGainPosition()
        {
            if (gainRoot == null) return;
            if (gainTarget == null || gameplayCamera == null)
            {
                gainVisibility.alpha = 0f;
                return;
            }
            Vector3 screenPosition = gameplayCamera.WorldToScreenPoint(gainTarget.position);
            bool inFront = screenPosition.z > 0f;
            gainVisibility.alpha = inFront ? 1f : 0f;
            if (!inFront) return;
            Camera uiCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCanvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, uiCamera, out Vector2 local)) return;
            local += settings.GainScreenOffset;
            Rect bounds = canvasRect.rect;
            // Keep all rows inside the canvas when the player approaches an edge.
            local.x = Mathf.Clamp(local.x, bounds.xMin + 16f, Mathf.Max(bounds.xMin + 16f, bounds.xMax - gainRoot.rect.width - 16f));
            local.y = Mathf.Clamp(local.y, bounds.yMin + 16f, Mathf.Max(bounds.yMin + 16f, bounds.yMax - gainRoot.rect.height - 16f));
            gainRoot.localPosition = new Vector3(local.x, local.y, 0f);
        }

        public void ShowInsufficient(string displayName, int missingAmount)
        {
            if (deniedToast == null || Time.unscaledTime < nextDeniedTime) return;
            nextDeniedTime = Time.unscaledTime + settings.DeniedCooldown;
            deniedToast.Text.text = displayName + "不足 · 还需 " + Mathf.Max(1, missingAmount);
            FeelFeedbackFactory.Replay(deniedToast.Player);
        }

        public void Clear()
        {
            foreach (Toast toast in gainToasts) ClearToast(toast);
            ClearToast(deniedToast);
            nextDeniedTime = 0f;
            nextGainSlot = 0;
        }

        private static void ClearToast(Toast toast)
        {
            if (toast == null || toast.Player == null) return;
            toast.Player.StopFeedbacks();
            toast.Player.RestoreInitialValues();
            toast.Group.alpha = 0f;
            toast.ResourceId = null;
            toast.Amount = 0;
            toast.ExpiresAt = 0f;
        }

        private void OnDisable() { Clear(); }
        private void OnDestroy()
        {
            if (root != null) Destroy(root);
            if (gainRoot != null) Destroy(gainRoot.gameObject);
        }
    }
}
