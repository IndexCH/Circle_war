using UnityEngine;
using UnityEngine.Audio;

namespace CircleWar
{
    [CreateAssetMenu(menuName = "Circle War/Feel Feedback Settings")]
    public sealed class FeelFeedbackSettings : ScriptableObject
    {
        public const string ResourcePath = "Feedbacks/FeelFeedbackSettings";

        [Header("Audio (FEEL sample clips)")]
        public AudioClip ShotSound;
        public AudioClip HitSound;
        public AudioClip DeathSound;
        public AudioClip DeniedSound;
        public AudioClip GainSound;
        public AudioClip PlayerHurtSound;
        public AudioClip InfoUpdateSound;
        public AudioMixerGroup MixerGroup;
        [Range(0f, 1f)] public float Volume = 0.35f;

        [Header("Combat")]
        public Material ParticleMaterial;
        public Material ShotParticleMaterial;
        public Material PlayerFlashMaterial;
        [Min(1f)] public float ShotBrightness = 2.5f;
        public Color ShotColor = new Color(1f, 0.77f, 0.25f);
        public Color HitColor = new Color(1f, 0.42f, 0.2f);
        public Color DeathColor = new Color(1f, 0.64f, 0.16f);
        [Min(0.01f)] public float HitDuration = 0.14f;
        [Min(0f)] public float HitShakeDistance = 0.045f;
        [Min(0.01f)] public float HitShakeDuration = 0.14f;
        [Min(0f)] public float RecoilDistance = 0.07f;
        [Min(0.01f)] public float RecoilDuration = 0.11f;

        [Header("Player damage and information screen")]
        public Color PlayerHurtColor = new Color(1f, 0.2f, 0.18f);
        [Min(0.01f)] public float PlayerHurtDuration = 0.3f;
        [Min(18)] public int DamageFontSize = 36;
        [Range(0f, 1f)] public float HurtBorderOpacity = 0.65f;
        public Color InfoUpdateColor = new Color(0.55f, 1f, 0.95f);
        [Min(0.01f)] public float InfoUpdateDuration = 0.4f;
        [Min(0f)] public float InfoSoundCooldown = 0.25f;

        [Header("Resource messages")]
        public Color GainColor = new Color(0.55f, 1f, 0.72f);
        public Color IndustryGainColor = new Color(1f, 0.72f, 0.24f);
        [Min(0f)] public float GainRowGap = 2f;
        public Color DeniedColor = new Color(1f, 0.48f, 0.36f);
        public Vector2 GainScreenOffset = new Vector2(130f, 95f);
        [Min(18)] public int GainFontSize = 30;
        [Min(0.1f)] public float MessageDuration = 1.5f;
        [Min(0.1f)] public float DeniedCooldown = 0.65f;

        public static FeelFeedbackSettings Load()
        {
            return Resources.Load<FeelFeedbackSettings>(ResourcePath);
        }
    }
}
