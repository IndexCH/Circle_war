using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CircleWar.Tests
{
    public sealed class EnemyHealthBarTests
    {
        private readonly List<GameObject> createdGameObjects = new List<GameObject>();
        private readonly List<UnityEngine.Object> createdAssets = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int objectIndex = createdGameObjects.Count - 1; objectIndex >= 0; objectIndex--)
            {
                if (createdGameObjects[objectIndex] != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdGameObjects[objectIndex]);
                }
            }

            createdGameObjects.Clear();
            for (int assetIndex = createdAssets.Count - 1; assetIndex >= 0; assetIndex--)
            {
                if (createdAssets[assetIndex] != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdAssets[assetIndex]);
                }
            }

            createdAssets.Clear();
        }

        [Test]
        public void FillShrinksFromTheRightAndKeepsItsLeftEdgeFixed()
        {
            GameObject enemyObject = CreateGameObject("Test Enemy");
            SpriteRenderer bodyRenderer = CreateBodyRenderer(enemyObject.transform, 10);
            FakeCombatEnemy healthSource = new FakeCombatEnemy
            {
                IsAlive = true,
                CurrentHealth = 100,
                MaxHealth = 100
            };
            EnemyHealthBar healthBar = enemyObject.AddComponent<EnemyHealthBar>();
            healthBar.Configure(healthSource, bodyRenderer);

            SpriteRenderer fillRenderer = GetBarRenderer(enemyObject.transform, "Fill");
            float fullLeftEdge = GetLeftEdge(fillRenderer.transform);
            Assert.That(healthBar.NormalizedHealth, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(fillRenderer.enabled, Is.True);
            Assert.That(fillRenderer.transform.localScale.x, Is.EqualTo(1f).Within(0.0001f));

            healthSource.CurrentHealth = 25;
            healthBar.RefreshVisual();

            Assert.That(healthBar.NormalizedHealth, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(fillRenderer.transform.localScale.x, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(GetLeftEdge(fillRenderer.transform), Is.EqualTo(fullLeftEdge).Within(0.0001f));
        }

        [Test]
        public void VisibilityAndRatioAreClampedToValidHealthState()
        {
            GameObject enemyObject = CreateGameObject("Test Enemy");
            SpriteRenderer bodyRenderer = CreateBodyRenderer(enemyObject.transform, 10);
            FakeCombatEnemy healthSource = new FakeCombatEnemy
            {
                IsAlive = true,
                CurrentHealth = 125,
                MaxHealth = 100
            };
            EnemyHealthBar healthBar = enemyObject.AddComponent<EnemyHealthBar>();
            healthBar.Configure(healthSource, bodyRenderer);

            SpriteRenderer backgroundRenderer = GetBarRenderer(enemyObject.transform, "Background");
            SpriteRenderer fillRenderer = GetBarRenderer(enemyObject.transform, "Fill");
            Assert.That(healthBar.NormalizedHealth, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(backgroundRenderer.enabled, Is.True);

            healthSource.CurrentHealth = -5;
            healthBar.RefreshVisual();
            Assert.That(healthBar.NormalizedHealth, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(backgroundRenderer.enabled, Is.False);
            Assert.That(fillRenderer.enabled, Is.False);

            healthSource.CurrentHealth = 50;
            healthSource.IsAlive = false;
            healthBar.RefreshVisual();
            Assert.That(healthBar.NormalizedHealth, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(backgroundRenderer.enabled, Is.False);
            Assert.That(fillRenderer.enabled, Is.False);

            healthBar.Configure(null, bodyRenderer);
            Assert.That(healthBar.NormalizedHealth, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(backgroundRenderer.enabled, Is.False);
        }

        [Test]
        public void BarStaysScreenAlignedAboveRotatedEnemyAndUsesHigherSortingOrders()
        {
            GameObject enemyObject = CreateGameObject("Rotated Enemy");
            enemyObject.transform.position = new Vector3(2f, 3f, 0f);
            enemyObject.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            SpriteRenderer bodyRenderer = CreateBodyRenderer(enemyObject.transform, 20);
            bodyRenderer.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

            EnemyHealthBar healthBar = enemyObject.AddComponent<EnemyHealthBar>();
            healthBar.Configure(
                new FakeCombatEnemy { IsAlive = true, CurrentHealth = 100, MaxHealth = 100 },
                bodyRenderer);

            Transform barRoot = enemyObject.transform.Find("Health Bar");
            SpriteRenderer backgroundRenderer = GetBarRenderer(enemyObject.transform, "Background");
            SpriteRenderer fillRenderer = GetBarRenderer(enemyObject.transform, "Fill");
            Assert.That(Mathf.DeltaAngle(0f, barRoot.eulerAngles.z), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(barRoot.position.x, Is.EqualTo(bodyRenderer.bounds.center.x).Within(0.0001f));
            Assert.That(barRoot.position.y, Is.EqualTo(bodyRenderer.bounds.max.y + 0.08f).Within(0.0001f));
            Assert.That(backgroundRenderer.sortingLayerID, Is.EqualTo(bodyRenderer.sortingLayerID));
            Assert.That(backgroundRenderer.sortingOrder, Is.EqualTo(bodyRenderer.sortingOrder + 3));
            Assert.That(fillRenderer.sortingOrder, Is.EqualTo(bodyRenderer.sortingOrder + 4));
        }

        [Test]
        public void LateUpdateRefreshesHealthAndTracksTheTransformedBody()
        {
            GameObject enemyObject = CreateGameObject("Animated Enemy");
            SpriteRenderer bodyRenderer = CreateBodyRenderer(enemyObject.transform, 10);
            FakeCombatEnemy healthSource = new FakeCombatEnemy
            {
                IsAlive = true,
                CurrentHealth = 100,
                MaxHealth = 100
            };
            EnemyHealthBar healthBar = enemyObject.AddComponent<EnemyHealthBar>();
            healthBar.Configure(healthSource, bodyRenderer);

            healthSource.CurrentHealth = 40;
            enemyObject.transform.position = new Vector3(3f, -2f, 0f);
            enemyObject.transform.rotation = Quaternion.Euler(0f, 0f, 37f);
            bodyRenderer.transform.localPosition = new Vector3(0.2f, -0.1f, 0f);
            bodyRenderer.transform.localScale = new Vector3(1.2f, 0.65f, 1f);

            InvokeLateUpdate(healthBar);

            Transform barRoot = enemyObject.transform.Find("Health Bar");
            SpriteRenderer backgroundRenderer = GetBarRenderer(enemyObject.transform, "Background");
            SpriteRenderer fillRenderer = GetBarRenderer(enemyObject.transform, "Fill");
            Assert.That(healthBar.NormalizedHealth, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(backgroundRenderer.enabled, Is.True);
            Assert.That(fillRenderer.enabled, Is.True);
            Assert.That(Mathf.DeltaAngle(0f, barRoot.eulerAngles.z), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(barRoot.position.x, Is.EqualTo(bodyRenderer.bounds.center.x).Within(0.0001f));
            Assert.That(barRoot.position.y, Is.EqualTo(bodyRenderer.bounds.max.y + 0.08f).Within(0.0001f));

            healthSource.CurrentHealth = 0;
            InvokeLateUpdate(healthBar);

            Assert.That(healthBar.NormalizedHealth, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(backgroundRenderer.enabled, Is.False);
            Assert.That(fillRenderer.enabled, Is.False);
        }

        [TestCase(EnemyAttackType.GroundMelee)]
        [TestCase(EnemyAttackType.GroundRanged)]
        [TestCase(EnemyAttackType.FlyingRobotRanged)]
        public void EveryCurrentEnemyTypeCreatesOneHealthBarAndTracksLocalDamage(EnemyAttackType attackType)
        {
            EnemyDefinition definition = Resources.LoadAll<EnemyDefinition>("GameData/Enemies")
                .First(enemy => enemy.AttackType == attackType && enemy.MaxHealth > 1);
            GameObject enemyObject = CreateGameObject("Configured " + attackType);
            ICombatEnemy combatEnemy;

            if (attackType == EnemyAttackType.FlyingRobotRanged)
            {
                FlyingRobotEnemy flyingEnemy = enemyObject.AddComponent<FlyingRobotEnemy>();
                flyingEnemy.ConfigureViewAnchored(
                    null,
                    null,
                    definition,
                    Vector2.zero,
                    Vector2.zero);
                flyingEnemy.ConfigureViewAnchored(
                    null,
                    null,
                    definition,
                    Vector2.zero,
                    Vector2.zero);
                combatEnemy = flyingEnemy;
            }
            else
            {
                GroundEnemy groundEnemy = enemyObject.AddComponent<GroundEnemy>();
                groundEnemy.Configure(
                    null,
                    null,
                    null,
                    definition,
                    attackType,
                    90f,
                    3f,
                    0f);
                groundEnemy.Configure(
                    null,
                    null,
                    null,
                    definition,
                    attackType,
                    90f,
                    3f,
                    0f);
                combatEnemy = groundEnemy;
            }

            EnemyHealthBar[] healthBars = enemyObject.GetComponents<EnemyHealthBar>();
            Assert.That(healthBars, Has.Length.EqualTo(1));
            EnemyHealthBar healthBar = healthBars[0];
            healthBar.RefreshVisual();
            Assert.That(combatEnemy.CurrentHealth, Is.EqualTo(definition.MaxHealth));
            Assert.That(combatEnemy.MaxHealth, Is.EqualTo(definition.MaxHealth));
            Assert.That(healthBar.NormalizedHealth, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(GetBarRenderer(enemyObject.transform, "Background").enabled, Is.True);

            Assert.That(combatEnemy.TryTakeDamage(1), Is.True);
            healthBar.RefreshVisual();
            Assert.That(combatEnemy.CurrentHealth, Is.EqualTo(definition.MaxHealth - 1));
            Assert.That(
                healthBar.NormalizedHealth,
                Is.EqualTo((float)(definition.MaxHealth - 1) / definition.MaxHealth).Within(0.0001f));
        }

        private GameObject CreateGameObject(string objectName)
        {
            GameObject gameObject = new GameObject(objectName);
            createdGameObjects.Add(gameObject);
            return gameObject;
        }

        private SpriteRenderer CreateBodyRenderer(Transform parent, int sortingOrder)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            createdAssets.Add(texture);
            createdAssets.Add(sprite);

            GameObject bodyObject = new GameObject("Body");
            bodyObject.transform.SetParent(parent, false);
            SpriteRenderer bodyRenderer = bodyObject.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = sprite;
            bodyRenderer.sortingOrder = sortingOrder;
            return bodyRenderer;
        }

        private static SpriteRenderer GetBarRenderer(Transform enemyTransform, string rendererName)
        {
            Transform rendererTransform = enemyTransform.Find("Health Bar/" + rendererName);
            Assert.That(rendererTransform, Is.Not.Null, rendererName);
            return rendererTransform.GetComponent<SpriteRenderer>();
        }

        private static float GetLeftEdge(Transform fillTransform)
        {
            return fillTransform.localPosition.x - fillTransform.localScale.x * 0.5f;
        }

        private static void InvokeLateUpdate(EnemyHealthBar healthBar)
        {
            MethodInfo lateUpdate = typeof(EnemyHealthBar).GetMethod(
                "LateUpdate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(lateUpdate, Is.Not.Null);
            lateUpdate.Invoke(healthBar, null);
        }

        private sealed class FakeCombatEnemy : ICombatEnemy
        {
            public bool IsAlive { get; set; }
            public int CurrentHealth { get; set; }
            public int MaxHealth { get; set; }
            public Vector2 WorldPosition => Vector2.zero;
            public float HitRadius => 0f;
            public CircleMapView CircleMapView => null;

            public bool TryTakeDamage(int damage)
            {
                if (damage <= 0 || CurrentHealth <= 0)
                {
                    return false;
                }

                CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
                IsAlive = CurrentHealth > 0;
                return true;
            }
        }
    }
}
