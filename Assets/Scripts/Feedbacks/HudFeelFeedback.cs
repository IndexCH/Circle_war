using System.Collections.Generic;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace CircleWar
{
    [DisallowMultipleComponent]
    public sealed class HudFeelFeedback : MonoBehaviour
    {
        [SerializeField] private FeelFeedbackSettings settings;
        private Text hpText;
        private Image infoFrame;
        private GameObject infoOverlay;
        private MMF_Player hpPlayer;
        private MMF_Player infoPlayer;
        private MMF_Player infoSoundPlayer;
        private PlayerAimShooter player;
        private CombatFeelFeedback playerFeedback;
        private PlayerDamagePopup damagePopup;
        private readonly HashSet<Text> pendingInfoTexts = new HashSet<Text>();
        private readonly Dictionary<Text, MMF_Player> textPlayers = new Dictionary<Text, MMF_Player>();

        public void Configure(Text healthText, Image informationFrame)
        {
            hpText = healthText;
            infoFrame = informationFrame;
            if (settings == null) settings = FeelFeedbackSettings.Load();
        }

        public void PlayPlayerDamage(int amount)
        {
            if (!isActiveAndEnabled || settings == null) return;
            if (player == null) player = FindAnyObjectByType<PlayerAimShooter>();
            if (player != null)
            {
                playerFeedback = CombatFeelFeedback.GetOrAdd(player.gameObject);
                playerFeedback.PlayPlayerHit(player.GetComponent<SpriteRenderer>());
                if (damagePopup == null) damagePopup = gameObject.AddComponent<PlayerDamagePopup>();
                damagePopup.Show(player, hpText, settings, amount);
            }
            if (hpText == null) return;
            if (hpPlayer == null)
                hpPlayer = CreateTextPulse(hpText, "FEEL HP Damage", settings.PlayerHurtColor, settings.PlayerHurtDuration);
            FeelFeedbackFactory.Replay(hpPlayer);
        }

        public void QueueInfoUpdate(Text changedText)
        {
            if (isActiveAndEnabled && settings != null && changedText != null)
                pendingInfoTexts.Add(changedText);
        }

        private void LateUpdate()
        {
            if (pendingInfoTexts.Count == 0) return;
            EnsureInfoPlayer();
            FeelFeedbackFactory.Replay(infoPlayer);
            infoSoundPlayer.PlayFeedbacks();
            foreach (Text text in pendingInfoTexts)
            {
                if (text == null) continue;
                if (!textPlayers.TryGetValue(text, out MMF_Player feedback) || feedback == null)
                {
                    feedback = CreateTextPulse(text, "FEEL Feed Line", settings.InfoUpdateColor, settings.InfoUpdateDuration);
                    textPlayers[text] = feedback;
                }
                FeelFeedbackFactory.Replay(feedback);
            }
            pendingInfoTexts.Clear();
        }

        private void EnsureInfoPlayer()
        {
            if (infoPlayer != null) return;
            infoPlayer = FeelFeedbackFactory.CreatePlayer(transform, "FEEL Info Update");
            SetUnscaled(infoPlayer);
            if (infoFrame != null)
            {
                infoOverlay = new GameObject("FEEL Info Highlight", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                RectTransform rect = (RectTransform)infoOverlay.transform;
                rect.SetParent(infoFrame.transform, false);
                rect.SetAsFirstSibling();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                Image image = infoOverlay.GetComponent<Image>();
                image.sprite = infoFrame.sprite;
                image.material = infoFrame.material;
                image.type = infoFrame.type;
                image.preserveAspect = infoFrame.preserveAspect;
                image.fillCenter = infoFrame.fillCenter;
                image.color = settings.InfoUpdateColor;
                image.raycastTarget = false;
                CanvasGroup group = infoOverlay.GetComponent<CanvasGroup>();
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
                infoPlayer.AddFeedback(new MMF_CanvasGroup
                {
                    Label = "Information frame pulse",
                    TargetCanvasGroup = group,
                    Duration = settings.InfoUpdateDuration,
                    AlphaCurve = new MMTweenType(new AnimationCurve(new Keyframe(0, 0),
                        new Keyframe(0.12f, 0.9f), new Keyframe(0.4f, 0.5f), new Keyframe(1, 0)))
                });
            }
            infoPlayer.Initialization();
            infoSoundPlayer = FeelFeedbackFactory.CreatePlayer(transform, "FEEL Info Sound");
            SetUnscaled(infoSoundPlayer);
            infoSoundPlayer.CooldownDuration = settings.InfoSoundCooldown;
            infoSoundPlayer.CanPlayWhileAlreadyPlaying = false;
            FeelFeedbackFactory.AddSound(infoSoundPlayer, settings.InfoUpdateSound, settings, 0.35f);
            infoSoundPlayer.Initialization();
        }

        private static MMF_Player CreateTextPulse(Text text, string name, Color color, float duration)
        {
            MMF_Player feedback = FeelFeedbackFactory.CreatePlayer(text.transform, name);
            SetUnscaled(feedback);
            feedback.AddFeedback(new MMF_TextColor
            {
                Label = "Text highlight",
                TargetText = text,
                ColorMode = MMF_TextColor.ColorModes.Interpolate,
                DestinationColor = color,
                Duration = duration,
                ColorCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.15f, 1),
                    new Keyframe(0.45f, 0.85f), new Keyframe(1, 0))
            });
            feedback.Initialization();
            return feedback;
        }

        private static void SetUnscaled(MMF_Player feedback)
        {
            feedback.ForceTimescaleMode = true;
            feedback.ForcedTimescaleMode = TimescaleModes.Unscaled;
            feedback.PlayerTimescaleMode = TimescaleModes.Unscaled;
        }

        public void Clear()
        {
            pendingInfoTexts.Clear();
            StopAndRestore(hpPlayer);
            StopAndRestore(infoPlayer);
            if (infoSoundPlayer != null) infoSoundPlayer.StopFeedbacks();
            foreach (var entry in textPlayers)
                if (entry.Key != null) StopAndRestore(entry.Value);
            if (playerFeedback != null) playerFeedback.ClearPlayerHit();
            if (damagePopup != null) damagePopup.Clear();
        }

        private static void StopAndRestore(MMF_Player feedback)
        {
            if (feedback == null) return;
            feedback.StopFeedbacks();
            feedback.RestoreInitialValues();
        }

        private void OnDisable() { Clear(); }
        private void OnDestroy() { if (infoOverlay != null) Destroy(infoOverlay); }
    }
}
