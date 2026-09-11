using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CircleWar
{
    [DisallowMultipleComponent]
    public sealed class CombatFeelFeedback : MonoBehaviour
    {
        [SerializeField] private FeelFeedbackSettings settings;
        private MMF_Player shotPlayer;
        private MMF_Player hitPlayer;
        private MMF_Player playerHitPlayer;
        private SpriteRenderer playerHitBody;
        private SpriteRenderer playerFlash;
        private bool deathPlayed;

        public static CombatFeelFeedback GetOrAdd(GameObject owner)
        {
            CombatFeelFeedback feedback = owner.GetComponent<CombatFeelFeedback>();
            return feedback != null ? feedback : owner.AddComponent<CombatFeelFeedback>();
        }

        private bool ResolveSettings()
        {
            if (settings == null) settings = FeelFeedbackSettings.Load();
            return settings != null;
        }

        public void PlayShot(Transform muzzle, Transform hand)
        {
            if (!isActiveAndEnabled || muzzle == null || !ResolveSettings()) return;
            if (shotPlayer == null)
            {
                shotPlayer = FeelFeedbackFactory.CreatePlayer(muzzle, "FEEL Shot");
                SpriteRenderer visual = hand != null ? hand.GetComponentInChildren<SpriteRenderer>() : null;
                ParticleSystem sparks = FeelFeedbackFactory.AddBurst(shotPlayer, settings,
                    Color.Lerp(settings.ShotColor, Color.white, 0.45f), 10, 2.4f, 0.15f,
                    visual != null ? visual.sortingLayerID : 0, visual != null ? visual.sortingOrder + 5 : 40);
                var main = sparks.main;
                main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.13f);
                var renderer = sparks.GetComponent<ParticleSystemRenderer>();
                if (settings.ShotParticleMaterial != null) renderer.sharedMaterial = settings.ShotParticleMaterial;
                var properties = new MaterialPropertyBlock();
                float brightness = Mathf.Max(1f, settings.ShotBrightness);
                properties.SetColor("_BaseColor", new Color(brightness, brightness, brightness, 1f));
                renderer.SetPropertyBlock(properties);
                // Only recoil a child visual: Hand itself owns aiming and facing.
                if (visual != null && visual.transform != hand && !muzzle.IsChildOf(visual.transform))
                {
                    FeelFeedbackFactory.AddMotion(shotPlayer, visual.gameObject, -settings.RecoilDistance,
                        settings.RecoilDuration, true);
                }
                FeelFeedbackFactory.AddSound(shotPlayer, settings.ShotSound, settings, 0.65f);
                shotPlayer.Initialization();
            }
            FeelFeedbackFactory.Replay(shotPlayer);
        }

        public void PlayHit(SpriteRenderer body)
        {
            if (!isActiveAndEnabled || body == null || !ResolveSettings()) return;
            if (hitPlayer == null)
            {
                hitPlayer = FeelFeedbackFactory.CreatePlayer(body.transform, "FEEL Hit");
                // Root movement and the melee body's scale remain owned by combat code.
                hitPlayer.AddFeedback(new MMF_SpriteRenderer
                {
                    Label = "Damage color",
                    BoundSpriteRenderer = body,
                    Mode = MMF_SpriteRenderer.Modes.ToDestinationColorAndBack,
                    InitialColorMode = MMF_SpriteRenderer.InitialColorModes.InitialColorOnInit,
                    ToDestinationColor = settings.HitColor,
                    Duration = settings.HitDuration
                });
                if (body.transform != transform && settings.HitShakeDistance > 0f)
                {
                    FeelFeedbackFactory.AddHitShake(hitPlayer, body.transform,
                        settings.HitShakeDistance, settings.HitShakeDuration);
                }
                FeelFeedbackFactory.AddBurst(hitPlayer, settings, settings.HitColor, 9, 1.8f, 0.22f,
                    body.sortingLayerID, body.sortingOrder + 5);
                FeelFeedbackFactory.AddSound(hitPlayer, settings.HitSound, settings, 0.55f);
                hitPlayer.Initialization();
            }
            FeelFeedbackFactory.Replay(hitPlayer);
        }

        public void PlayPlayerHit(SpriteRenderer body)
        {
            if (!isActiveAndEnabled || body == null || !ResolveSettings()) return;
            if (playerHitPlayer == null)
            {
                playerHitBody = body;
                playerHitPlayer = FeelFeedbackFactory.CreatePlayer(body.transform, "FEEL Player Hurt");
                playerHitPlayer.ForceTimescaleMode = true;
                playerHitPlayer.ForcedTimescaleMode = TimescaleModes.Unscaled;
                playerHitPlayer.PlayerTimescaleMode = TimescaleModes.Unscaled;
                playerHitPlayer.AddFeedback(new MMF_SpriteRenderer
                {
                    Label = "Player damage flash",
                    BoundSpriteRenderer = body,
                    Mode = MMF_SpriteRenderer.Modes.ToDestinationColorAndBack,
                    InitialColorMode = MMF_SpriteRenderer.InitialColorModes.InitialColorOnInit,
                    ToDestinationColor = settings.PlayerHurtColor,
                    Duration = settings.PlayerHurtDuration
                });
                if (settings.PlayerFlashMaterial != null)
                {
                    var glow = new GameObject("FEEL Hurt Silhouette");
                    glow.transform.SetParent(body.transform, false);
                    playerFlash = glow.AddComponent<SpriteRenderer>();
                    playerFlash.sharedMaterial = settings.PlayerFlashMaterial;
                    playerFlash.color = Color.clear;
                    SyncPlayerFlash();
                    var gradient = new Gradient();
                    gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f),
                            new GradientColorKey(new Color(1f, .25f, .2f), .25f), new GradientColorKey(Color.red, 1f) },
                        new[] { new GradientAlphaKey(.95f, 0f), new GradientAlphaKey(.7f, .16f),
                            new GradientAlphaKey(.1f, .4f), new GradientAlphaKey(.55f, .53f), new GradientAlphaKey(0f, 1f) });
                    playerHitPlayer.AddFeedback(new MMF_SpriteRenderer { Label = "Bright damage silhouette",
                        BoundSpriteRenderer = playerFlash, Mode = MMF_SpriteRenderer.Modes.OverTime,
                        InitialColorMode = MMF_SpriteRenderer.InitialColorModes.InitialColorOnInit,
                        ColorOverTime = gradient, Duration = settings.PlayerHurtDuration });
                }
                FeelFeedbackFactory.AddBurst(playerHitPlayer, settings, settings.PlayerHurtColor,
                    16, 2f, 0.3f, body.sortingLayerID, body.sortingOrder + 5);
                FeelFeedbackFactory.AddSound(playerHitPlayer, settings.PlayerHurtSound, settings, 0.9f);
                playerHitPlayer.Initialization();
            }
            // FEEL's SpriteRenderer restore also restores flips; facing belongs to aiming.
            bool flipX = playerHitBody.flipX;
            bool flipY = playerHitBody.flipY;
            FeelFeedbackFactory.Replay(playerHitPlayer);
            playerHitBody.flipX = flipX;
            playerHitBody.flipY = flipY;
            SyncPlayerFlash();
        }

        private void LateUpdate() { SyncPlayerFlash(); }
        private void SyncPlayerFlash()
        {
            if (playerFlash == null || playerHitBody == null) return;
            playerFlash.sprite = playerHitBody.sprite;
            playerFlash.flipX = playerHitBody.flipX;
            playerFlash.flipY = playerHitBody.flipY;
            playerFlash.sortingLayerID = playerHitBody.sortingLayerID;
            playerFlash.sortingOrder = playerHitBody.sortingOrder + 1;
        }

        public void ClearPlayerHit()
        {
            if (playerHitPlayer == null || playerHitBody == null) return;
            bool flipX = playerHitBody.flipX;
            bool flipY = playerHitBody.flipY;
            playerHitPlayer.StopFeedbacks();
            playerHitPlayer.RestoreInitialValues();
            playerHitBody.flipX = flipX;
            playerHitBody.flipY = flipY;
        }

        public void PlayDeath(SpriteRenderer body)
        {
            if (deathPlayed || !ResolveSettings()) return;
            deathPlayed = true;
            MMF_Player death = FeelFeedbackFactory.CreatePlayer(null, "FEEL Death");
            SceneManager.MoveGameObjectToScene(death.gameObject, gameObject.scene);
            death.transform.position = body != null ? body.bounds.center : transform.position;
            int layer = body != null ? body.sortingLayerID : 0;
            int order = body != null ? body.sortingOrder + 6 : 40;
            ParticleSystem fragments = FeelFeedbackFactory.AddBurst(death, settings, settings.DeathColor, 44, 3.5f, 0.75f, layer, order);
            var fragmentMain = fragments.main;
            fragmentMain.startSize = new ParticleSystem.MinMaxCurve(.08f, .18f);
            if (settings.ShotParticleMaterial != null)
                fragments.GetComponent<ParticleSystemRenderer>().sharedMaterial = settings.ShotParticleMaterial;
            ParticleSystem core = FeelFeedbackFactory.AddBurst(death, settings, new Color(1f, .9f, .65f), 5, .5f, .22f, layer, order + 1);
            var coreMain = core.main;
            coreMain.startSize = new ParticleSystem.MinMaxCurve(.35f, .55f);
            if (settings.ShotParticleMaterial != null)
                core.GetComponent<ParticleSystemRenderer>().sharedMaterial = settings.ShotParticleMaterial;
            death.gameObject.AddComponent<FeelDeathRing>().Configure(settings.ShotParticleMaterial != null
                ? settings.ShotParticleMaterial : settings.ParticleMaterial, layer, order, settings.DeathColor);
            FeelFeedbackFactory.AddSound(death, settings.DeathSound, settings, 0.8f);
            death.Initialization();
            death.PlayFeedbacks();
            float audioDuration = settings.DeathSound != null ? settings.DeathSound.length / 0.96f : 0f;
            death.gameObject.AddComponent<FeelEffectLifetime>().Begin(Mathf.Max(1f, audioDuration + 0.1f));
        }

        private void OnDisable()
        {
            ClearPlayerHit();
            if (shotPlayer != null) { shotPlayer.StopFeedbacks(); shotPlayer.RestoreInitialValues(); }
            if (hitPlayer != null) { hitPlayer.StopFeedbacks(); hitPlayer.RestoreInitialValues(); }
        }
    }
}
