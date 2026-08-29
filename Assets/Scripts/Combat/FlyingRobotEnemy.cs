using UnityEngine;

namespace CircleWar
{
    public sealed class FlyingRobotEnemy : MonoBehaviour, ICombatEnemy
    {
        private const string DefaultPlayerName = "Player";
        private const int SpriteTextureSize = 16;

        [Header("References")]
        [SerializeField] private Transform playerTarget;
        [SerializeField] private Transform gunPivot;
        [SerializeField] private Transform shootPoint;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer gunRenderer;
        [SerializeField] private EnemyHealthBar healthBar;
        [SerializeField] private CircleMapView circleMapView;
        [SerializeField] private EnemyDefinition enemyDefinition;
        [SerializeField] private BossDefinition bossDefinition;

        [Header("Entry")]
        [SerializeField] private Vector2 fallbackOrbitViewCenter = new Vector2(0f, 1.7f);
        [Min(0.01f)]
        [SerializeField] private float entryDuration = 1.2f;
        [Min(0.01f)]
        [SerializeField] private float hitRadius = 0.38f;

        [Header("Figure Eight")]
        [Min(0f)]
        [SerializeField] private float horizontalAmplitude = 1.2f;
        [Min(0f)]
        [SerializeField] private float verticalAmplitude = 0.45f;
        [Min(0.01f)]
        [SerializeField] private float orbitSpeed = 1.4f;
        [Min(0f)]
        [SerializeField] private float orbitEdgePauseDuration = 2f;

        [Header("Shooting")]
        [Min(0.01f)]
        [SerializeField] private float fireCooldown = 1.4f;
        [Min(0.1f)]
        [SerializeField] private float bulletSpeed = 5.5f;
        [Min(0.1f)]
        [SerializeField] private float bulletLifetime = 4f;
        [Min(0.01f)]
        [SerializeField] private float bulletWorldSize = 0.11f;
        [SerializeField] private Sprite bulletSprite;
        [SerializeField] private Color bulletColor = new Color(1f, 0.18f, 0.12f, 1f);
        [SerializeField] private int sortingOrder = 32;

        private static Sprite bodySprite;
        private static Sprite gunSprite;
        private static Sprite runtimeBulletSprite;

        private CombatEnemyProgressBinding progressBinding;
        private CombatEnemyProgressBinding bossProgressBinding;
        private Vector2 viewPosition;
        private Vector2 worldPosition;
        private Vector2 entryStartViewPosition;
        private Vector2 orbitViewCenter;
        private Vector2 aimWorldDirection = Vector2.down;
        private float entryAge;
        private float orbitTime;
        private float orbitPauseRemaining;
        private float nextHorizontalExtremeTime = Mathf.PI * 0.5f;
        private float nextFireTime;
        private bool isConfigured;
        private bool isDead;
        private bool useBossPortrait = true;
        private int currentHealth;
        private AttackPatternDefinition activeBossAttackPattern;
        private string activeBossPhaseId = string.Empty;

        public bool IsAlive => isActiveAndEnabled &&
                               isConfigured &&
                               !isDead &&
                               CurrentHealth > 0;
        public int CurrentHealth => progressBinding != null
            ? progressBinding.CurrentHealth
            : Mathf.Clamp(currentHealth, 0, MaxHealth);
        public int MaxHealth => progressBinding != null
            ? progressBinding.MaxHealth
            : enemyDefinition != null ? Mathf.Max(1, enemyDefinition.MaxHealth) : 1;
        public Vector2 WorldPosition => GetCurrentWorldPosition();
        public float HitRadius => hitRadius;
        public CircleMapView CircleMapView => ResolveCircleMapView();

        public void Configure(Transform newPlayerTarget, Vector2 newOrbitViewCenter)
        {
            CircleMapView resolvedMapView = ResolveCircleMapView();
            ConfigureViewAnchored(resolvedMapView, newPlayerTarget, null, transform.position, newOrbitViewCenter);
        }

        public void Configure(CircleMapView newCircleMapView, Transform newPlayerTarget, Vector2 newWorldPosition, Vector2 newOrbitWorldCenter)
        {
            Configure(newCircleMapView, newPlayerTarget, null, newWorldPosition, newOrbitWorldCenter);
        }

        public void Configure(CircleMapView newCircleMapView, Transform newPlayerTarget, EnemyDefinition newEnemyDefinition, Vector2 newWorldPosition, Vector2 newOrbitWorldCenter)
        {
            circleMapView = newCircleMapView != null ? newCircleMapView : ResolveCircleMapView();
            Vector2 newViewPosition = circleMapView != null ? circleMapView.WorldToViewPosition(newWorldPosition) : newWorldPosition;
            Vector2 newOrbitViewCenter = circleMapView != null ? circleMapView.WorldToViewPosition(newOrbitWorldCenter) : newOrbitWorldCenter;
            ConfigureViewAnchored(circleMapView, newPlayerTarget, newEnemyDefinition, newViewPosition, newOrbitViewCenter);
        }

        public void ConfigureViewAnchored(
            CircleMapView newCircleMapView,
            Transform newPlayerTarget,
            EnemyDefinition newEnemyDefinition,
            Vector2 newViewPosition,
            Vector2 newOrbitViewCenter,
            CombatEnemyProgressBinding newProgressBinding = null,
            BossDefinition newBossDefinition = null,
            bool newUseBossPortrait = true,
            CombatEnemyProgressBinding newBossProgressBinding = null)
        {
            circleMapView = newCircleMapView != null ? newCircleMapView : ResolveCircleMapView();
            playerTarget = newPlayerTarget;
            enemyDefinition = newEnemyDefinition != null ? newEnemyDefinition : enemyDefinition;
            bossDefinition = newBossDefinition != null ? newBossDefinition : bossDefinition;
            if (enemyDefinition != null && enemyDefinition.Speed > 0f)
            {
                orbitSpeed = enemyDefinition.Speed;
            }
            progressBinding = newProgressBinding;
            bossProgressBinding = newBossProgressBinding != null ? newBossProgressBinding : newProgressBinding;
            useBossPortrait = newUseBossPortrait;
            viewPosition = newViewPosition;
            entryStartViewPosition = newViewPosition;
            orbitViewCenter = newOrbitViewCenter;
            UpdateWorldPositionFromView();
            entryAge = 0f;
            orbitTime = 0f;
            orbitPauseRemaining = 0f;
            nextHorizontalExtremeTime = Mathf.PI * 0.5f;
            currentHealth = progressBinding != null ? progressBinding.CurrentHealth : MaxHealth;
            isDead = false;
            isConfigured = true;
            RefreshBossAttackPattern();
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
            CombatEnemyRegistry.Register(this);
        }

        private void OnDisable()
        {
            CombatEnemyRegistry.Unregister(this);
        }

        private void Update()
        {
            if (isDead)
            {
                return;
            }

            if (progressBinding != null && progressBinding.CurrentHealth <= 0)
            {
                Die();
                return;
            }

            ResolveReferences();
            EnsureConfigured();
            RefreshBossAttackPattern();
            Move();
            ApplyViewTransform();
            AimGunAtPlayer();
            TryShoot();
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

            Sprite configuredBodySprite = useBossPortrait && bossDefinition != null && bossDefinition.Portrait != null
                ? bossDefinition.Portrait
                : enemyDefinition != null ? enemyDefinition.Portrait : null;
            bodyRenderer.sprite = configuredBodySprite != null ? configuredBodySprite : GetBodySprite();
            bodyRenderer.color = configuredBodySprite != null
                ? Color.white
                : new Color(0.48f, 0.86f, 1f, 1f);
            float targetBodySize = useBossPortrait && bossDefinition != null ? 1.65f : 0.9f;
            float sourceBodySize = Mathf.Max(
                0.01f,
                Mathf.Max(bodyRenderer.sprite.bounds.size.x, bodyRenderer.sprite.bounds.size.y));
            float fittedBodyScale = configuredBodySprite != null ? targetBodySize / sourceBodySize : 0.38f;
            bodyRenderer.transform.localScale = new Vector3(fittedBodyScale, fittedBodyScale, 1f);
            EnsureHealthBar();

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
                gunObject.transform.localPosition = new Vector3(0.23f, 0f, 0f);
                gunObject.transform.localScale = new Vector3(0.46f, 0.08f, 1f);

                gunRenderer = gunObject.AddComponent<SpriteRenderer>();
                gunRenderer.sprite = GetGunSprite();
                gunRenderer.color = new Color(0.95f, 0.98f, 1f, 1f);
                gunRenderer.sortingOrder = sortingOrder + 1;
            }

            gunRenderer.enabled = configuredBodySprite == null;

            if (shootPoint == null)
            {
                GameObject shootPointObject = new GameObject("Shoot Point");
                shootPointObject.layer = gameObject.layer;
                shootPointObject.transform.SetParent(gunPivot, false);
                shootPointObject.transform.localPosition = new Vector3(0.5f, 0f, 0f);
                shootPoint = shootPointObject.transform;
            }
        }

        private void EnsureHealthBar()
        {
            if (healthBar == null)
            {
                healthBar = GetComponent<EnemyHealthBar>();
            }

            if (healthBar == null)
            {
                healthBar = gameObject.AddComponent<EnemyHealthBar>();
            }

            healthBar.Configure(this, bodyRenderer);
        }

        private void Move()
        {
            if (!isConfigured)
            {
                return;
            }

            if (entryAge < entryDuration)
            {
                entryAge += Time.deltaTime;
                float progress = Mathf.Clamp01(entryAge / entryDuration);
                float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
                viewPosition = Vector2.Lerp(entryStartViewPosition, orbitViewCenter, easedProgress);
                UpdateWorldPositionFromView();
                return;
            }

            AdvanceOrbit(Time.deltaTime);
            Vector2 offset = new Vector2(
                Mathf.Sin(orbitTime) * horizontalAmplitude,
                Mathf.Sin(orbitTime * 2f) * verticalAmplitude);
            viewPosition = orbitViewCenter + offset;
            UpdateWorldPositionFromView();
        }

        private void AdvanceOrbit(float deltaTime)
        {
            if (orbitPauseRemaining > 0f)
            {
                orbitPauseRemaining = Mathf.Max(0f, orbitPauseRemaining - deltaTime);
                return;
            }

            float nextOrbitTime = orbitTime + deltaTime * orbitSpeed;
            if (nextOrbitTime < nextHorizontalExtremeTime)
            {
                orbitTime = nextOrbitTime;
                return;
            }

            orbitTime = nextHorizontalExtremeTime;
            nextHorizontalExtremeTime += Mathf.PI;
            orbitPauseRemaining = orbitEdgePauseDuration;
        }

        private void AimGunAtPlayer()
        {
            if (gunPivot == null)
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
            if (entryAge < entryDuration || shootPoint == null || Time.time < nextFireTime)
            {
                return;
            }

            Vector2 fireDirection = aimWorldDirection;
            if (fireDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            int volleyCount = GetBossVolleyCount();
            float centerOffset = (volleyCount - 1) * 0.5f;
            for (int volleyIndex = 0; volleyIndex < volleyCount; volleyIndex++)
            {
                float angleOffset = (volleyIndex - centerOffset) * 7f;
                Vector2 volleyDirection = Quaternion.Euler(0f, 0f, angleOffset) * fireDirection.normalized;
                SpawnBullet(volleyDirection.normalized);
            }
            nextFireTime = Time.time + fireCooldown;
        }

        private void RefreshBossAttackPattern()
        {
            CombatEnemyProgressBinding resolvedBossProgressBinding = bossProgressBinding != null
                ? bossProgressBinding
                : progressBinding;
            if (bossDefinition == null || resolvedBossProgressBinding == null)
            {
                activeBossAttackPattern = null;
                activeBossPhaseId = string.Empty;
                return;
            }

            float healthRatio = resolvedBossProgressBinding.MaxHealth <= 0
                ? 0f
                : (float)resolvedBossProgressBinding.CurrentHealth / resolvedBossProgressBinding.MaxHealth;
            BossPhaseDefinition selectedPhase = null;
            float selectedThreshold = float.MaxValue;
            foreach (BossPhaseDefinition phase in bossDefinition.Phases)
            {
                if (phase == null || healthRatio > phase.HealthPercentThreshold)
                {
                    continue;
                }

                if (phase.HealthPercentThreshold < selectedThreshold)
                {
                    selectedPhase = phase;
                    selectedThreshold = phase.HealthPercentThreshold;
                }
            }

            AttackPatternDefinition selectedPattern = null;
            if (selectedPhase != null && selectedPhase.AttackPatterns.Count > 0)
            {
                selectedPattern = selectedPhase.AttackPatterns[0];
            }
            else if (bossDefinition.DefaultAttackPatterns.Count > 0)
            {
                selectedPattern = bossDefinition.DefaultAttackPatterns[0];
            }

            activeBossAttackPattern = selectedPattern;
            if (selectedPattern != null && selectedPattern.CooldownSeconds > 0f)
            {
                fireCooldown = selectedPattern.CooldownSeconds;
            }

            string selectedPhaseId = selectedPhase != null ? selectedPhase.PhaseId : string.Empty;
            if (selectedPhaseId == activeBossPhaseId)
            {
                return;
            }

            activeBossPhaseId = selectedPhaseId;
            GameHud hud = FindAnyObjectByType<GameHud>();
            if (hud != null && !string.IsNullOrWhiteSpace(activeBossPhaseId))
            {
                hud.RuntimeData.State.SetBossPhase(bossDefinition.DefinitionId, activeBossPhaseId);
            }
        }

        private int GetBossVolleyCount()
        {
            if (activeBossAttackPattern == null)
            {
                return 1;
            }

            switch (activeBossAttackPattern.PatternType)
            {
                case AttackPatternType.Summon:
                    return 3;
                case AttackPatternType.Area:
                    return 5;
                default:
                    return 1;
            }
        }

        private void SpawnBullet(Vector2 direction)
        {
            CircleMapView resolvedMapView = ResolveCircleMapView();
            Vector2 shootWorldPosition = GetShootPointWorldPosition(direction);
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

        private void ApplyViewTransform()
        {
            transform.position = new Vector3(viewPosition.x, viewPosition.y, transform.position.z);
        }

        private Vector2 GetShootPointWorldPosition(Vector2 direction)
        {
            Vector2 normalizedDirection = direction.sqrMagnitude <= Mathf.Epsilon ? Vector2.down : direction.normalized;
            float muzzleDistance = shootPoint != null ? shootPoint.localPosition.x : 0.5f;
            return worldPosition + normalizedDirection * muzzleDistance;
        }

        private void EnsureConfigured()
        {
            if (isConfigured)
            {
                return;
            }

            CircleMapView resolvedMapView = ResolveCircleMapView();
            ConfigureViewAnchored(
                resolvedMapView,
                playerTarget,
                enemyDefinition,
                transform.position,
                fallbackOrbitViewCenter,
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
                currentHealth = MaxHealth;
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

        private void UpdateWorldPositionFromView()
        {
            worldPosition = GetCurrentWorldPosition();
        }

        private Vector2 GetCurrentWorldPosition()
        {
            CircleMapView resolvedMapView = ResolveCircleMapView();
            return resolvedMapView != null ? resolvedMapView.ViewToWorldPosition(viewPosition) : viewPosition;
        }

        private int GetAttackDamage()
        {
            if (activeBossAttackPattern != null)
            {
                return Mathf.Max(0, activeBossAttackPattern.Power);
            }

            return enemyDefinition != null ? Mathf.Max(0, enemyDefinition.AttackPower) : 1;
        }

        private static Sprite GetBodySprite()
        {
            if (bodySprite == null)
            {
                bodySprite = CreateDiscSprite("Runtime Flying Robot Body", 0.46f);
            }

            return bodySprite;
        }

        private static Sprite GetGunSprite()
        {
            if (gunSprite == null)
            {
                gunSprite = CreateSquareSprite("Runtime Flying Robot Gun");
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
                runtimeBulletSprite = CreateDiscSprite("Runtime Enemy Bullet", 0.42f);
            }

            return runtimeBulletSprite;
        }

        private static Sprite CreateDiscSprite(string spriteName, float radiusScale)
        {
            Texture2D texture = new Texture2D(SpriteTextureSize, SpriteTextureSize, TextureFormat.RGBA32, false);
            texture.name = spriteName + " Texture";
            texture.filterMode = FilterMode.Point;

            Vector2 center = new Vector2((SpriteTextureSize - 1) * 0.5f, (SpriteTextureSize - 1) * 0.5f);
            float radius = SpriteTextureSize * radiusScale;
            for (int y = 0; y < SpriteTextureSize; y++)
            {
                for (int x = 0; x < SpriteTextureSize; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, distance <= radius ? 1f : 0f));
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
