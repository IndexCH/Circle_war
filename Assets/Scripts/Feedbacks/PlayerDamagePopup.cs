using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace CircleWar
{
    [DisallowMultipleComponent]
    public sealed class PlayerDamagePopup : MonoBehaviour
    {
        private Canvas canvas;
        private RectTransform anchor;
        private GameObject border;
        private CanvasGroup labelGroup;
        private CanvasGroup borderGroup;
        private Text label;
        private SpriteRenderer body;
        private Transform target;
        private Camera gameplayCamera;
        private MMF_Player feedback;
        private int totalDamage;
        private float lastDamageTime = -10f;
        private float expiresAt;

        public void Show(PlayerAimShooter player, Text reference, FeelFeedbackSettings settings, int damage)
        {
            if (damage <= 0 || player == null || settings == null) return;
            if (feedback == null && !Create(reference, settings)) return;
            target = player.transform;
            body = player.GetComponent<SpriteRenderer>();
            if (gameplayCamera == null) gameplayCamera = Camera.main;
            totalDamage = Time.unscaledTime - lastDamageTime < .2f ? totalDamage + damage : damage;
            lastDamageTime = Time.unscaledTime;
            expiresAt = Time.unscaledTime + 1f;
            label.text = "−" + totalDamage + " 生命";
            UpdatePosition();
            FeelFeedbackFactory.Replay(feedback);
        }

        private bool Create(Text reference, FeelFeedbackSettings settings)
        {
            canvas = reference != null ? reference.GetComponentInParent<Canvas>() : GetComponentInParent<Canvas>();
            if (canvas == null) return false;
            anchor = (RectTransform)new GameObject("FEEL Player Damage", typeof(RectTransform)).transform;
            anchor.SetParent(canvas.transform, false);
            anchor.anchorMin = anchor.anchorMax = new Vector2(.5f, .5f);
            anchor.sizeDelta = new Vector2(240f, 52f);
            GameObject textObject = new GameObject("Damage Amount", typeof(RectTransform), typeof(CanvasGroup), typeof(Text), typeof(Outline));
            var rect = (RectTransform)textObject.transform;
            rect.SetParent(anchor, false);
            rect.sizeDelta = anchor.sizeDelta;
            label = textObject.GetComponent<Text>();
            label.font = reference != null ? reference.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontStyle = FontStyle.Bold;
            label.fontSize = settings.DamageFontSize;
            PixelHudTypography.ApplyToGeneratedText(label, reference);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(1f, .26f, .22f);
            label.raycastTarget = false;
            textObject.GetComponent<Outline>().effectDistance = new Vector2(2f, -2f);
            textObject.GetComponent<Outline>().effectColor = new Color(.12f, .015f, .015f, 1f);
            labelGroup = textObject.GetComponent<CanvasGroup>();
            labelGroup.alpha = 0f;
            labelGroup.blocksRaycasts = labelGroup.interactable = false;
            border = new GameObject("FEEL Hurt Border", typeof(RectTransform), typeof(CanvasGroup));
            var borderRect = (RectTransform)border.transform;
            borderRect.SetParent(canvas.transform, false);
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = borderRect.offsetMax = Vector2.zero;
            borderGroup = border.GetComponent<CanvasGroup>();
            borderGroup.alpha = 0f;
            borderGroup.blocksRaycasts = borderGroup.interactable = false;
            for (int i = 0; i < 4; i++)
            {
                var edge = (RectTransform)new GameObject("Edge", typeof(RectTransform), typeof(Image)).transform;
                edge.SetParent(borderRect, false);
                bool vertical = i < 2;
                edge.anchorMin = vertical ? new Vector2(i, 0f) : new Vector2(0f, i - 2);
                edge.anchorMax = vertical ? new Vector2(i, 1f) : new Vector2(1f, i - 2);
                edge.pivot = vertical ? new Vector2(i, .5f) : new Vector2(.5f, i - 2);
                edge.sizeDelta = vertical ? new Vector2(12f, 0f) : new Vector2(0f, 12f);
                Image image = edge.GetComponent<Image>();
                image.color = settings.PlayerHurtColor;
                image.raycastTarget = false;
            }
            feedback = FeelFeedbackFactory.CreatePlayer(anchor, "FEEL Damage Popup");
            feedback.ForceTimescaleMode = true;
            feedback.ForcedTimescaleMode = TimescaleModes.Unscaled;
            feedback.PlayerTimescaleMode = TimescaleModes.Unscaled;
            feedback.AddFeedback(new MMF_CanvasGroup { TargetCanvasGroup = labelGroup, Duration = 1f,
                AlphaCurve = new MMTweenType(new AnimationCurve(new Keyframe(0, 1), new Keyframe(.65f, 1), new Keyframe(1, 0))) });
            feedback.AddFeedback(new MMF_CanvasGroup { TargetCanvasGroup = borderGroup, Duration = .3f,
                AlphaCurve = new MMTweenType(new AnimationCurve(new Keyframe(0, settings.HurtBorderOpacity), new Keyframe(1, 0))) });
            FeelFeedbackFactory.AddMotion(feedback, textObject, 26f, 1f, false);
            feedback.Initialization();
            return true;
        }

        private void LateUpdate() { if (Time.unscaledTime < expiresAt) UpdatePosition(); }
        private void UpdatePosition()
        {
            if (target == null || gameplayCamera == null || anchor == null) return;
            Vector3 head = body != null ? new Vector3(body.bounds.center.x, body.bounds.max.y, body.bounds.center.z) : target.position;
            Vector3 screen = gameplayCamera.WorldToScreenPoint(head);
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, screen, uiCamera, out Vector2 position)) return;
            position += new Vector2(0f, 26f);
            Rect bounds = ((RectTransform)canvas.transform).rect;
            position.x = Mathf.Clamp(position.x, bounds.xMin + 120f, bounds.xMax - 120f);
            position.y = Mathf.Clamp(position.y, bounds.yMin + 30f, bounds.yMax - 65f);
            anchor.anchoredPosition = position;
        }

        public void Clear()
        {
            if (feedback != null) { feedback.StopFeedbacks(); feedback.RestoreInitialValues(); }
            if (labelGroup != null) labelGroup.alpha = 0f;
            if (borderGroup != null) borderGroup.alpha = 0f;
            totalDamage = 0;
            expiresAt = 0f;
            lastDamageTime = -10f;
        }
        private void OnDisable() { Clear(); }
        private void OnDestroy()
        {
            if (anchor != null) Destroy(anchor.gameObject);
            if (border != null) Destroy(border);
        }
    }
}
