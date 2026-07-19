using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CircleWar.Tests
{
    public sealed class BossDroneSwarmIntegrationTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly List<GameObject> createdObjects = new List<GameObject>();
        private readonly List<GameObject> disabledHudObjects = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            GameHud[] existingHuds = UnityEngine.Object.FindObjectsByType<GameHud>(FindObjectsInactive.Exclude);
            foreach (GameHud existingHud in existingHuds)
            {
                if (existingHud != null && existingHud.gameObject.activeSelf)
                {
                    disabledHudObjects.Add(existingHud.gameObject);
                    existingHud.gameObject.SetActive(false);
                }
            }
        }

        [TearDown]
        public void TearDown()
        {
            for (int objectIndex = createdObjects.Count - 1; objectIndex >= 0; objectIndex--)
            {
                if (createdObjects[objectIndex] != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdObjects[objectIndex]);
                }
            }

            createdObjects.Clear();
            foreach (GameObject disabledHudObject in disabledHudObjects)
            {
                if (disabledHudObject != null)
                {
                    disabledHudObject.SetActive(true);
                }
            }

            disabledHudObjects.Clear();
        }

        [TestCase(0, 4)]
        [TestCase(1, 3)]
        [TestCase(20, 1)]
        public void FinalBossSpawnsResolvedDroneCount(int reduction, int expectedCount)
        {
            GameHud hud = CreateGameObject("Test HUD").AddComponent<GameHud>();
            hud.RuntimeData.State.SetCustomValue(BossDroneCountResolver.ReductionValueId, reduction);
            CircleMapView mapView = CreateGameObject("Test Circle Map").AddComponent<CircleMapView>();
            SetPrivateField(mapView, "gameHud", hud);

            RoadSegmentDefinition definition = LoadFinalBossSegment();
            CircleRoadSegmentData segment = new CircleRoadSegmentData(definition, null);
            CombatEnemyProgressBinding progressBinding = StartBossEncounter(mapView, segment);
            SpawnSegmentEnemyOnce(mapView, segment, progressBinding);

            List<ICombatEnemy> spawnedDrones = GetSpawnedEnemies(mapView, definition.RoadIndex);
            Assert.That(spawnedDrones, Has.Count.EqualTo(expectedCount));
            TrackSpawnedEnemyObjects(mapView);
        }

        [Test]
        public void FinalBossWithoutHudUsesSharedBossHealthAndTacticalDronePortraits()
        {
            Assert.That(UnityEngine.Object.FindAnyObjectByType<GameHud>(), Is.Null);
            CircleMapView mapView = CreateGameObject("Test Circle Map").AddComponent<CircleMapView>();
            RoadSegmentDefinition definition = LoadFinalBossSegment();
            CircleRoadSegmentData segment = new CircleRoadSegmentData(definition, null);

            CombatEnemyProgressBinding progressBinding = StartBossEncounter(mapView, segment);
            SpawnSegmentEnemyOnce(mapView, segment, progressBinding);
            TrackSpawnedEnemyObjects(mapView);

            Assert.That(progressBinding, Is.Not.Null);
            Assert.That(progressBinding.MaxHealth, Is.EqualTo(definition.Boss.MaxHealth));
            Assert.That(progressBinding.CurrentHealth, Is.EqualTo(definition.Boss.MaxHealth));

            List<ICombatEnemy> spawnedDrones = GetSpawnedEnemies(mapView, definition.RoadIndex);
            Assert.That(spawnedDrones, Has.Count.EqualTo(BossDroneCountResolver.DefaultDroneCount));
            List<EnemyHealthBar> healthBars = new List<EnemyHealthBar>(spawnedDrones.Count);
            foreach (FlyingRobotEnemy drone in spawnedDrones.Cast<FlyingRobotEnemy>())
            {
                Assert.That(GetPrivateField<CombatEnemyProgressBinding>(drone, "progressBinding"),
                    Is.SameAs(progressBinding));
                Assert.That(GetPrivateField<AttackPatternDefinition>(drone, "activeBossAttackPattern"),
                    Is.Not.Null);

                SpriteRenderer bodyRenderer = drone.transform.Find("Body").GetComponent<SpriteRenderer>();
                Assert.That(bodyRenderer.sprite, Is.SameAs(definition.Enemy.Portrait));
                Assert.That(bodyRenderer.sprite, Is.Not.SameAs(definition.Boss.Portrait));

                EnemyHealthBar healthBar = drone.GetComponent<EnemyHealthBar>();
                Assert.That(healthBar, Is.Not.Null);
                healthBar.RefreshVisual();
                Assert.That(healthBar.NormalizedHealth, Is.EqualTo(1f).Within(0.0001f));
                Assert.That(GetHealthBarRenderer(drone.transform, "Background").enabled, Is.True);
                healthBars.Add(healthBar);
            }

            PopulateTerminalRoad(mapView, segment);
            Assert.That(InvokePrivate<bool>(mapView, "IsTerminalCombatBlockingSeasonAdvance"), Is.True);

            Assert.That(spawnedDrones[0].TryTakeDamage(10), Is.True);
            Assert.That(progressBinding.CurrentHealth, Is.EqualTo(definition.Boss.MaxHealth - 10));
            Assert.That(spawnedDrones.All(drone => drone.IsAlive), Is.True);
            foreach (EnemyHealthBar healthBar in healthBars)
            {
                healthBar.RefreshVisual();
                Assert.That(
                    healthBar.NormalizedHealth,
                    Is.EqualTo((float)(definition.Boss.MaxHealth - 10) / definition.Boss.MaxHealth)
                        .Within(0.0001f));
            }

            progressBinding.ApplyDamage(progressBinding.CurrentHealth);
            Assert.That(spawnedDrones.All(drone => !drone.IsAlive), Is.True);
            foreach (EnemyHealthBar healthBar in healthBars)
            {
                healthBar.RefreshVisual();
                Assert.That(healthBar.NormalizedHealth, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(GetHealthBarRenderer(healthBar.transform, "Background").enabled, Is.False);
                Assert.That(GetHealthBarRenderer(healthBar.transform, "Fill").enabled, Is.False);
            }

            Assert.That(InvokePrivate<bool>(mapView, "IsTerminalCombatBlockingSeasonAdvance"), Is.False);
        }

        private GameObject CreateGameObject(string objectName)
        {
            GameObject gameObject = new GameObject(objectName);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private void TrackSpawnedEnemyObjects(CircleMapView mapView)
        {
            List<GameObject> spawnedEnemyObjects = GetPrivateField<List<GameObject>>(
                mapView,
                "spawnedEnemyObjects");
            foreach (GameObject spawnedEnemyObject in spawnedEnemyObjects)
            {
                if (spawnedEnemyObject != null && !createdObjects.Contains(spawnedEnemyObject))
                {
                    createdObjects.Add(spawnedEnemyObject);
                }
            }
        }

        private static RoadSegmentDefinition LoadFinalBossSegment()
        {
            RoadSegmentDefinition definition = Resources.LoadAll<RoadSegmentDefinition>("GameData/RoadSegments")
                .Single(segment => segment.Season != null &&
                                   string.Equals(segment.Season.DefinitionId, "winter", StringComparison.OrdinalIgnoreCase) &&
                                   segment.RoadIndex == 39);
            Assert.That(definition.ContentType, Is.EqualTo(SegmentContentType.Boss));
            Assert.That(definition.Boss, Is.Not.Null);
            Assert.That(definition.Enemy, Is.Not.Null);
            return definition;
        }

        private static CombatEnemyProgressBinding StartBossEncounter(
            CircleMapView mapView,
            CircleRoadSegmentData segment)
        {
            return InvokePrivate<CombatEnemyProgressBinding>(
                mapView,
                "StartBossEncounter",
                segment.roadIndex,
                segment);
        }

        private static void SpawnSegmentEnemyOnce(
            CircleMapView mapView,
            CircleRoadSegmentData segment,
            CombatEnemyProgressBinding progressBinding)
        {
            InvokePrivate<object>(
                mapView,
                "SpawnSegmentEnemyOnce",
                segment.roadIndex,
                segment,
                progressBinding);
        }

        private static List<ICombatEnemy> GetSpawnedEnemies(CircleMapView mapView, int roadIndex)
        {
            Dictionary<int, List<ICombatEnemy>> enemiesByRoadIndex =
                GetPrivateField<Dictionary<int, List<ICombatEnemy>>>(mapView, "spawnedEnemiesByRoadIndex");
            Assert.That(enemiesByRoadIndex.TryGetValue(roadIndex, out List<ICombatEnemy> enemies), Is.True);
            return enemies;
        }

        private static SpriteRenderer GetHealthBarRenderer(Transform enemyTransform, string rendererName)
        {
            Transform rendererTransform = enemyTransform.Find("Health Bar/" + rendererName);
            Assert.That(rendererTransform, Is.Not.Null, rendererName);
            return rendererTransform.GetComponent<SpriteRenderer>();
        }

        private static void PopulateTerminalRoad(CircleMapView mapView, CircleRoadSegmentData finalSegment)
        {
            List<CircleRoadSegmentData> roadSegments = GetPrivateField<List<CircleRoadSegmentData>>(
                mapView,
                "roadSegmentList");
            roadSegments.Clear();
            for (int roadIndex = 0; roadIndex < finalSegment.roadIndex; roadIndex++)
            {
                roadSegments.Add(new CircleRoadSegmentData("空白", null));
            }

            roadSegments.Add(finalSegment);
            SetPrivateField(mapView, "currentRoadPosition", (float)finalSegment.roadIndex);
        }

        private static T InvokePrivate<T>(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, PrivateInstance);
            Assert.That(method, Is.Not.Null, methodName);
            object result = method.Invoke(target, arguments);
            return result == null ? default : (T)result;
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, PrivateInstance);
            Assert.That(field, Is.Not.Null, fieldName);
            return (T)field.GetValue(target);
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, PrivateInstance);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}
