using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;

namespace CircleWar
{
    internal static class FeelFeedbackFactory
    {
        internal static MMF_Player CreatePlayer(Transform parent, string name)
        {
            GameObject owner = new GameObject(name);
            owner.transform.SetParent(parent, false);
            MMF_Player player = owner.AddComponent<MMF_Player>();
            player.InitializationMode = MMFeedbacks.InitializationModes.Script;
            player.AutoInitialization = false;
            player.AutoPlayOnStart = false;
            player.AutoPlayOnEnable = false;
            player.CanPlayWhileAlreadyPlaying = true;
            return player;
        }

        internal static void AddSound(MMF_Player player, AudioClip clip, FeelFeedbackSettings settings, float volume = 1f)
        {
            if (clip == null) return;
            AudioSource source = player.gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.clip = clip;
            source.outputAudioMixerGroup = settings.MixerGroup;
            player.AddFeedback(new MMF_AudioSource
            {
                Label = "Sound",
                TargetAudioSource = source,
                MinVolume = settings.Volume * volume,
                MaxVolume = settings.Volume * volume,
                MinPitch = 0.96f,
                MaxPitch = 1.04f
            });
        }

        internal static ParticleSystem AddBurst(MMF_Player player, FeelFeedbackSettings settings,
            Color color, int count, float speed, float lifetime, int sortingLayer, int sortingOrder)
        {
            GameObject child = new GameObject("Sparks");
            child.transform.SetParent(player.transform, false);
            ParticleSystem particles = child.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = lifetime;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.55f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.45f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.085f);
            main.startColor = color;
            main.maxParticles = 64;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.useUnscaledTime = true;
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.035f;
            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = fade;
            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = settings.ParticleMaterial;
            renderer.sortingLayerID = sortingLayer;
            renderer.sortingOrder = sortingOrder;
            player.AddFeedback(new MMF_Particles
            {
                Label = "Spark burst",
                BoundParticleSystem = particles,
                Mode = MMF_Particles.Modes.Emit,
                EmitCount = count,
                DeclaredDuration = lifetime
            });
            return particles;
        }

        internal static MMF_Position AddMotion(MMF_Player player, GameObject target, float distance,
            float duration, bool horizontal)
        {
            MMF_Position motion = new MMF_Position
            {
                Label = horizontal ? "Weapon recoil" : "Message rise",
                AnimatePositionTarget = target,
                Mode = MMF_Position.Modes.AlongCurve,
                Space = MMF_Position.Spaces.Local,
                AnimatePositionDuration = duration,
                RelativePosition = true,
                AnimateX = horizontal,
                AnimateY = !horizontal,
                AnimateZ = false,
                RemapCurveZero = 0f,
                RemapCurveOne = distance,
                AnimatePositionTweenX = new MMTweenType(new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.15f, 1), new Keyframe(1, 0))),
                AnimatePositionTweenY = new MMTweenType(AnimationCurve.Linear(0, 0, 1, 1))
            };
            player.AddFeedback(motion);
            return motion;
        }

        internal static void AddHitShake(MMF_Player player, Transform visual, float distance, float duration)
        {
            // Compensate for parent scale so all enemy sizes get a small world-space shake.
            Vector3 parentScale = visual.parent != null ? visual.parent.lossyScale : Vector3.one;
            float x = distance / Mathf.Max(0.001f, Mathf.Abs(parentScale.x));
            float y = distance * 0.35f / Mathf.Max(0.001f, Mathf.Abs(parentScale.y));
            player.AddFeedback(new MMF_Position
            {
                Label = "Small hit shake",
                AnimatePositionTarget = visual.gameObject,
                Mode = MMF_Position.Modes.AlongCurve,
                Space = MMF_Position.Spaces.Local,
                AnimatePositionDuration = duration,
                RelativePosition = true,
                AnimateX = true,
                AnimateY = true,
                AnimateZ = false,
                RemapCurveZero = 0f,
                RemapCurveOne = 1f,
                AnimatePositionTweenX = new MMTweenType(new AnimationCurve(
                    new Keyframe(0f, 0f), new Keyframe(0.12f, -x), new Keyframe(0.28f, x),
                    new Keyframe(0.46f, -x * 0.65f), new Keyframe(0.64f, x * 0.4f),
                    new Keyframe(0.82f, -x * 0.2f), new Keyframe(1f, 0f))),
                AnimatePositionTweenY = new MMTweenType(new AnimationCurve(
                    new Keyframe(0f, 0f), new Keyframe(0.16f, y), new Keyframe(0.38f, -y),
                    new Keyframe(0.62f, y * 0.4f), new Keyframe(1f, 0f)))
            });
        }

        internal static void Replay(MMF_Player player)
        {
            player.StopFeedbacks();
            player.RestoreInitialValues();
            player.PlayFeedbacks();
        }
    }
}
