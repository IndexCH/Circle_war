using System.Collections.Generic;
using UnityEngine;

namespace CircleWar
{
    public sealed class GroundEnemy : MonoBehaviour, ICombatEnemy
    {
        private const string DefaultPlayerName = "Player";
        private const int SpriteTextureSize = 16;

        [Header("References")]
        [SerializeField] private Transform playerTarget;
        [SerializeField] private Transform gunPivot;
        [SerializeField] private Transform shootPoint;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer gunRenderer;
        [SerializeField] private CircleMapView circleMapView;
        [SerializeField] private GameHud gameHud;

        [Header("Ground")]
        [SerializeField] private EnemyAttackType attackType = EnemyAttackType.GroundMelee;
        [SerializeField] private float radius = 3f;
        [SerializeField] private float angleDegrees = 90f;
        [Min(0f)]
        [SerializeField] private float angularSpeedDegrees = 120f;
        [Min(0.01f)]
        [SerializeField] private float hitRadius = 0.42f;

        [Header("Ranged")]
        [Min(0.01f)]
        [SerializeField] private float fireCooldown = 1.5f;
        [Min(0.1f)]
        [SerializeField] private float bulletSpeed = 5.2f;
        [Min(0.1f)]
        [SerializeField] private float bulletLifetime = 4f;
        [Min(0.01f)]
        [SerializeField] private float bulletWorldSize = 0.11f;
        [SerializeField] private Sprite bulletSprite;
        [SerializeField] private Color bulletColor = new Color(1f, 0.35f, 0.1f, 1f);
        [SerializeField] private int sortingOrder = 29;

        [Header("Melee")]
        [Min(0.01f)]
        [SerializeField] private float meleeRange = 0.48f;
        [Min(0.01f)]
        [SerializeField] private float meleeCooldown = 0.85f;
        [SerializeField] private float meleeHoldAheadAngleDegrees = 10f;
        [Min(0.01f)]
        [SerializeField] private float meleeAttackAngleTolerance = 0.25f;
        [Min(0.01f)]
        [SerializeField] private float meleeAttackDuration = 0.26f;
        [Min(0f)]
        [SerializeField] private float meleeAttackScalePulse = 0.18f;

        private static readonly List<GroundEnemy> ActiveEnemies = new List<GroundEnemy>();
        private static Sprite bodySprite;
        private static Sprite gunSprite;
        private static Sprite runtimeBulletSprite;

        private EnemyDefinition enemyDefinition;
        private CombatEnemyProgressBinding progressBinding;
        private Vector2 worldPosition;
        private Vector2 aimWorldDirection = Vector2.down;
        private float nextFireTime;
        private float nextMeleeTime;
        private float meleeAttackAge;
        private Vector3 bodyBaseLocalScale = Vector3.one;
        private bool isMeleeAttacking;
        private bool hasAppliedMeleeDamage;
        private bool hasBodyBasePose;
        private bool isConfigured;
        private bool isDead;
        private int currentHealth;

        public Vector2 WorldPosition => worldPosition;
        public float AngleDegrees => angleDegrees;
        public EnemyAttackType AttackType => attackType;
        public bool IsAlive => isActiveAndEnabled && isConfigured && !isDead;
        public float HitRadius => hitRadius;
        public CircleMapView CircleMapView => ResolveCircleMapView();

        public static bool IsAnyMeleeBlockingForward(CircleMapView mapView)
        {
            if (mapView == null)
            {
                return false;
            }

            for (int index = ActiveEnemies.Count - 1; index >= 0; index--)
            {
                GroundEnemy enemy = ActiveEnemies[index];
                if (enemy == null)
                {
                    ActiveEnemies.RemoveAt(index);
                    continue;
                }

                if (enemy.BlocksForwardMovement(mapView))
                {
                    return true;
                }
            }

            return false;
        }

        public void Configure(
            CircleMapView newCircleMapView,
            Transform newPlayerTarget,
            GameHud newGameHud,
            EnemyDefinition newEnemyDefinition,
            EnemyAttackType newAttackType,
            float newAngleDegrees,
            float newRadius,
            float newAngularSpeedDegrees,
            CombatEnemyProgressBinding newProgressBinding = null)
        {
            circleMapView = newCircleMapView != null ? newCircleMapView : ResolveCircleMapView();
            playerTarget = newPlayerTarget;
            gameHud = newGameHud;
            enemyDefinition = newEnemyDefinition;
            progressBinding = newProgressBinding;
            attackType = newAttackType;
            angleDegrees = newAngleDegrees;
            radius = Mathf.Max(0.01f, newRadius);
            angularSpeedDegrees = Mathf.Max(0f, newAngularSpeedDegrees);
            currentHealth = progressBinding != null ? progressBinding.CurrentHealth : GetMaxHealth();
            isDead = false;
            isConfigured = true;
            UpdateWorldPosition();
            EnsureVisuals();
            ApplyViewTransform();
        }

        private void Awake()
        {
            ResolveReferences();
            EnsureVisuals();
        }

        private void OnEnable()
        {
            if (!ActiveEnemies.Contains(this))
            {
                ActiveEnemies.Add(this);
            }

            CombatEnemyRegistry.Register(this);
        }

        private void OnDisable()
        {
            ActiveEnemies.Remove(this);
            CombatEnemyRegistry.Unregister(this);
        }

        private void Update()
        {
            if (isDead)
            {
                return;
            }

            ResolveReferences();
            EnsureConfigured();
            Move();
            ApplyViewTransform();
            AimGunAtPlayer();
            TryShoot();
            TryMeleeAttack();
            UpdateMeleeAttackAnimation();
        }

        private void ResolveReferences()
        {
            if (playerTarget == null)
            {
                GameObject playerObject = GameObject.Find(DefaultPlayerName);
                if (playerObject != null)
                {
                    playerTarget = playerObject.transform;
                }
            }

            ResolveCircleMapView();
            ResolveGameHud();
        }

        private void EnsureVisuals()
        {
            if (bodyRenderer == null)
            {
                GameObject bodyObject = new GameObject("Body");
                bodyObject.layer = gameObject.layer;
                bodyObject.transform.SetParent(transform, false);

                bodyRenderer = bodyObject.AddComponent<SpriteRenderer>();
                bodyRenderer.sortingOrder = sortingOrder;
            }

            Sprite configuredBodySprite = enemyDefinition != null ? enemyDefinition.Portrait : null;
            bodyRenderer.sprite = configuredBodySprite != null ? configuredBodySprite : GetBodySprite();
            bodyRenderer.color = configuredBodySprite != null
                ? Color.white
                : attackType == EnemyAttackType.GroundRanged
                    ? new Color(1f, 0.72f, 0.26f, 1f)
                    : new Color(1f, 0.28f, 0.2f, 1f);
            float sourceBodySize = Mathf.Max(
                0.01f,
                Mathf.Max(bodyRenderer.sprite.bounds.size.x, bodyRenderer.sprite.bounds.size.y));
            float fittedBodyScale = configuredBodySprite != null ? 0.9f / sourceBodySize : 0.44f;
            bodyRenderer.transform.localScale = new Vector3(fittedBodyScale, fittedBodyScale, 1f);
            bodyBaseLocalScale = bodyRenderer.transform.localScale;
            hasBodyBasePose = true;

            if (attackType != EnemyAttackType.GroundRanged)
            {
                if (gunRenderer != null)
                {
                    gunRenderer.enabled = false;
                }

                return;
            }

            if (gunPivot == null)
            {
                GameObject pivotObject = new GameObject("Gun Pivot");
                pivotObject.layer = gameObject.layer;
                pivotObject.transform.SetParent(transform, false);
                gunPivot = pivotObject.transform;
            }

            if (gunRenderer == null)
            {
                GameObject gunObject = new GameObject("Gun");
                gunObject.layer = gameObject.layer;
                gunObject.transform.SetParent(gunPivot, false);
                gunObject.transform.localPosition = new Vector3(0.25f, 0f, 0f);
                gunObject.transform.localScale = new Vector3(0.5f, 0.09f, 1f);

                gunRenderer = gunObject.AddComponent<SpriteRenderer>();
                gunRenderer.sprite = GetGunSprite();
                gunRenderer.color = new Color(1f, 0.96f, 0.82f, 1f);
                gunRenderer.sortingOrder = sortingOrder + 1;
            }

            gunRenderer.enabled = configuredBodySprite == null;

            if (shootPoint == null)
            {
                GameObject shootPointObject = new GameObject("Shoot Point");
                shootPointObject.layer = gameObject.layer;
                shootPointObject.transform.SetParent(gunPivot, false);
                shootPointObject.transform.localPosition = new Vector3(0.55f, 0f, 0f);
                shootPoint = shootPointObject.transform;
            }
        }

        private void Move()
        {
            if (attackType == EnemyAttackType.GroundMelee)
            {
                CircleMapView resolvedMapView = ResolveCircleMapView();
                float targetAngle = GetMeleeHoldAngle(resolvedMapView);
                float clockwiseDelta = GetClockwiseAngleDelta(angleDegrees, targetAngle);
                if (clockwiseDelta > meleeAttackAngleTolerance)
                {
                    float step = angularSpeedDegrees * Time.deltaTime;
                    angleDegrees -= Mathf.Min(clockwiseDelta, step);
                }
            }

            UpdateWorldPosition();
        }

        private void UpdateWorldPosition()
        {
            CircleMapView resolvedMapView = ResolveCircleMapView();
            Vector2 center = resolvedMapView != null ? resolvedMapView.DiskCenter : (Vector2)transform.position;
            worldPosition = center + CircleWorldSpace.DirectionFromAngleDegrees(angleDegrees) * radius;
        }

        private void ApplyViewTransform()
        {
            CircleMapView resolvedMapView = ResolveCircleMapView();
            Vector2 viewPosition = resolvedMapView != null ? resolvedMapView.WorldToViewPosition(worldPosition) : worldPosition;
            transform.position = new Vector3(viewPosition.x, viewPosition.y, transform.position.z);

            if (resolvedMapView == null)
            {
                return;
            }

            Vector2 radialWorldDirection = worldPosition - resolvedMapView.DiskCenter;
            if (radialWorldDirection.sqrMagnitude > Mathf.Epsilon)
            {
                Vector2 radialViewDirection = resolvedMapView.WorldToViewDirection(radialWorldDirection.normalized);
                if (radialViewDirection.sqrMagnitude > Mathf.Epsilon)
                {
                    transform.up = radialViewDirection.normalized;
                }
            }
        }

        private void AimGunAtPlayer()
        {
            if (attackType != EnemyAttackType.GroundRanged || gunPivot == null)
            {
                return;
            }

            CircleMapView resolvedMapView = ResolveCircleMapView();
            Vector2 playerWorldPosition = resolvedMapView != null
                ? resolvedMapView.PlayerWorldPosition
                : playerTarget != null ? (Vector2)playerTarget.position : worldPosition;
            Vector2 aimDirection = playerWorldPosition - worldPosition;
            if (aimDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            aimWorldDirection = aimDirection.normalized;
            Vector2 viewDirection = resolvedMapView != null
                ? resolvedMapView.WorldToViewDirection(aimWorldDirection)
                : aimWorldDirection;
            gunPivot.right = viewDirection.normalized;
        }

        private void TryShoot()
        {
            if (attackType != EnemyAttackType.GroundRanged || shootPoint == null || Time.time < nextFireTime)
            {
                return;
            }

            CircleMapView resolvedMapView = ResolveCircleMapView();
            Vector2 shootWorldPosition = GetShootPointWorldPosition(aimWorldDirection);
            Vector2 playerWorldPosition = resolvedMapView != null
                ? resolvedMapView.PlayerWorldPosition
                : playerTarget != null ? (Vector2)playerTarget.position : worldPosition;
            Vector2 fireDirection = playerWorldPosition - shootWorldPosition;
            if (fireDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            SpawnBullet(shootWorldPosition, fireDirection.normalized);
            nextFireTime = Time.time + fireCooldown;
        }

        private void TryMeleeAttack()
        {
            if (attackType != EnemyAttackType.GroundMelee ||
                isMeleeAttacking ||
                Time.time < nextMeleeTime ||
                !IsMeleeInAttackPosition(ResolveCircleMapView()))
            {
                return;
            }

            isMeleeAttacking = true;
            hasAppliedMeleeDamage = false;
            meleeAttackAge = 0f;
        }

        private void UpdateMeleeAttackAnimation()
        {
            if (!isMeleeAttacking)
            {
                ResetBodyPose();
                return;
            }

            meleeAttackAge += Time.deltaTime;
            float progress = Mathf.Clamp01(meleeAttackAge / meleeAttackDuration);
            float pulse = Mathf.Sin(progress * Mathf.PI) * meleeAttackScalePulse;
            if (bodyRenderer != null && hasBodyBasePose)
            {
                bodyRenderer.transform.localScale = bodyBaseLocalScale * (1f + pulse);
            }

            if (!hasAppliedMeleeDamage && progress >= 0.5f)
            {
                if (IsMeleeInAttackPosition(ResolveCircleMapView()))
                {
                    ApplyMeleeDamage();
                }

                hasAppliedMeleeDamage = true;
            }

            if (progress >= 1f)
            {
                isMeleeAttacking = false;
                nextMeleeTime = Time.time + meleeCooldown;
                ResetBodyPose();
            }
        }

        private void ResetBodyPose()
        {
            if (bodyRenderer != null && hasBodyBasePose)
            {
                bodyRenderer.transform.localScale = bodyBaseLocalScale;
            }
        }

        private void ApplyMeleeDamage()
        {
            CombatDamage.TryApplyPlayerDamage(ResolveGameHud(), GetAttackDamage());
        }

        private bool BlocksForwardMovement(CircleMapView mapView)
        {
            return isActiveAndEnabled &&
                IsAlive &&
                isConfigured &&
                attackType == EnemyAttackType.GroundMelee &&
                ReferenceEquals(circleMapView, mapView) &&
                IsMeleeInAttackPosition(mapView);
        }

        private bool IsMeleeInAttackPosition(CircleMapView resolvedMapView)
        {
            if (resolvedMapView == null)
            {
                return false;
            }

            float angleGap = Mathf.Abs(Mathf.DeltaAngle(angleDegrees, GetMeleeHoldAngle(resolvedMapView)));
            if (angleGap > meleeAttackAngleTolerance)
            {
                return false;
            }

            // The melee enemy intentionally holds an angular offset in front of the player,
            // so center-to-center distance includes arc length and grows with hold angle.
            float radialGap = Mathf.Abs(radius - resolvedMapView.PlayerRadius);
            return radialGap <= meleeRange;
        }

        private float GetMeleeHoldAngle(CircleMapView resolvedMapView)
        {
            float playerAngle = resolvedMapView != null ? resolvedMapView.PlayerAngleDegrees : angleDegrees;
            return playerAngle + meleeHoldAheadAngleDegrees;
        }

        private void SpawnBullet(Vector2 shootWorldPosition, Vector2 direction)
        {
            CircleMapView resolvedMapView = ResolveCircleMapView();
            Vector2 shootViewPosition = resolvedMapView != null
                ? resolvedMapView.WorldToViewPosition(shootWorldPosition)
                : shootWorldPosition;
            Vector2 shootViewDirection = resolvedMapView != null
                ? resolvedMapView.WorldToViewDirection(direction)
                : direction;

            GameObject bulletObject = new GameObject("Enemy Bullet");
            bulletObject.layer = gameObject.layer;
            bulletObject.transform.SetParent(null, true);
            bulletObject.transform.position = new Vector3(shootViewPosition.x, shootViewPosition.y, transform.position.z);
            bulletObject.transform.right = shootViewDirection.normalized;
            bulletObject.transform.localScale = new Vector3(bulletWorldSize, bulletWorldSize, 1f);

            SpriteRenderer renderer = bulletObject.AddComponent<SpriteRenderer>();
            renderer.sprite = GetBulletSprite();
            renderer.color = bulletColor;
            renderer.sortingOrder = sortingOrder + 2;

            EnemyBullet bullet = bulletObject.AddComponent<EnemyBullet>();
            bullet.Launch(resolvedMapView, shootWorldPosition, direction, bulletSpeed, bulletLifetime, GetAttackDamage());
        }

        private Vector2 GetShootPointWorldPosition(Vector2 direction)
        {
            Vector2 normalizedDirection = direction.sqrMagnitude <= Mathf.Epsilon ? Vector2.down : direction.normalized;
            float muzzleDistance = shootPoint != null ? shootPoint.localPosition.x : 0.55f;
            return worldPosition + normalizedDirection * muzzleDistance;
        }

        private void EnsureConfigured()
        {
            if (isConfigured)
            {
                return;
            }

            CircleMapView resolvedMapView = ResolveCircleMapView();
            Vector2 startWorldPosition = resolvedMapView != null
                ? resolvedMapView.ViewToWorldPosition(transform.position)
                : (Vector2)transform.position;
            Vector2 center = resolvedMapView != null ? resolvedMapView.DiskCenter : Vector2.zero;
            Vector2 offset = startWorldPosition - center;
            float resolvedRadius = offset.magnitude > Mathf.Epsilon ? offset.magnitude : radius;
            float resolvedAngle = offset.sqrMagnitude > Mathf.Epsilon ? GetAngleDegrees(offset) : angleDegrees;
            Configure(
                resolvedMapView,
                playerTarget,
                ResolveGameHud(),
                enemyDefinition,
                attackType,
                resolvedAngle,
                resolvedRadius,
                angularSpeedDegrees,
                progressBinding);
        }

        public bool TryTakeDamage(int damage)
        {
            if (isDead)
            {
                return false;
            }

            int safeDamage = Mathf.Max(0, damage);
            if (safeDamage <= 0)
            {
                return false;
            }

            if (currentHealth <= 0 && progressBinding == null)
            {
                currentHealth = GetMaxHealth();
            }

            currentHealth = progressBinding != null
                ? progressBinding.ApplyDamage(safeDamage)
                : Mathf.Max(0, currentHealth - safeDamage);
            if (currentHealth <= 0)
            {
                Die();
            }

            return true;
        }

        private void Die()
        {
            if (isDead)
            {
                return;
            }

            isDead = true;
            progressBinding?.ReportDefeated();
            ActiveEnemies.Remove(this);
            CombatEnemyRegistry.Unregister(this);
            Destroy(gameObject);
        }

        private CircleMapView ResolveCircleMapView()
        {
            if (circleMapView == null)
            {
                circleMapView = CircleMapView.Active != null ? CircleMapView.Active : FindAnyObjectByType<CircleMapView>();
            }

            return circleMapView;
        }

        private GameHud ResolveGameHud()
        {
            if (gameHud == null)
            {
                gameHud = FindAnyObjectByType<GameHud>();
            }

            return gameHud;
        }

        private int GetMaxHealth()
        {
            return progressBinding != null
                ? progressBinding.MaxHealth
                : enemyDefinition != null ? Mathf.Max(1, enemyDefinition.MaxHealth) : 1;
        }

        private int GetAttackDamage()
        {
            return enemyDefinition != null ? Mathf.Max(0, enemyDefinition.AttackPower) : 1;
        }

        private static float GetClockwiseAngleDelta(float fromAngleDegrees, float toAngleDegrees)
        {
            return Mathf.Repeat(fromAngleDegrees - toAngleDegrees, 360f);
        }

        private static float GetAngleDegrees(Vector2 direction)
        {
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }

        private static Sprite GetBodySprite()
        {
            if (bodySprite == null)
            {
                bodySprite = CreateDiscSprite("Runtime Ground Enemy Body", 0.45f);
            }

            return bodySprite;
        }

        private static Sprite GetGunSprite()
        {
            if (gunSprite == null)
            {
                gunSprite = CreateSquareSprite("Runtime Ground Enemy Gun");
            }

            return gunSprite;
        }

        private Sprite GetBulletSprite()
        {
            if (bulletSprite != null)
            {
                return bulletSprite;
            }

            if (runtimeBulletSprite == null)
            {
                runtimeBulletSprite = CreateDiscSprite("Runtime Ground Enemy Bullet", 0.42f);
            }

            return runtimeBulletSprite;
        }

        private static Sprite CreateDiscSprite(string spriteName, float radiusScale)
        {
            Texture2D texture = new Texture2D(SpriteTextureSize, SpriteTextureSize, TextureFormat.RGBA32, false);
            texture.name = spriteName + " Texture";
            texture.filterMode = FilterMode.Point;

            Vector2 center = new Vector2((SpriteTextureSize - 1) * 0.5f, (SpriteTextureSize - 1) * 0.5f);
            float radiusPixels = SpriteTextureSize * radiusScale;
            for (int y = 0; y < SpriteTextureSize; y++)
            {
                for (int x = 0; x < SpriteTextureSize; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, distance <= radiusPixels ? 1f : 0f));
                }
            }

            texture.Apply();
            return CreateSprite(texture, spriteName);
        }

        private static Sprite CreateSquareSprite(string spriteName)
        {
            Texture2D texture = new Texture2D(SpriteTextureSize, SpriteTextureSize, TextureFormat.RGBA32, false);
            texture.name = spriteName + " Texture";
            texture.filterMode = FilterMode.Point;

            for (int y = 0; y < SpriteTextureSize; y++)
            {
                for (int x = 0; x < SpriteTextureSize; x++)
                {
                    texture.SetPixel(x, y, Color.white);
                }
            }

            texture.Apply();
            return CreateSprite(texture, spriteName);
        }

        private static Sprite CreateSprite(Texture2D texture, string spriteName)
        {
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, SpriteTextureSize, SpriteTextureSize),
                new Vector2(0.5f, 0.5f),
                SpriteTextureSize);
            sprite.name = spriteName;
            return sprite;
        }
    }
}
