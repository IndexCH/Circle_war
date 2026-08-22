using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CircleWar.Tests
{
    public sealed class CircleMapSeasonVisualTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void SeasonalRingSpritesHaveIdenticalDimensions()
        {
            SeasonDefinition[] seasons = Resources.LoadAll<SeasonDefinition>("GameData/Seasons");
            SeasonDefinition spring = Array.Find(
                seasons,
                season => season != null && string.Equals(season.DefinitionId, "spring", StringComparison.OrdinalIgnoreCase));
            Assert.That(spring, Is.Not.Null);

            Sprite reference = spring.CircleRingSprite;
            Assert.That(reference, Is.Not.Null);
            foreach (SeasonDefinition season in seasons)
            {
                Assert.That(season, Is.Not.Null);
                Assert.That(season.CircleRingSprite, Is.Not.Null, season.DefinitionId);
                Assert.That(season.CircleRingSprite.rect.size, Is.EqualTo(reference.rect.size), season.DefinitionId);
                Assert.That(season.CircleRingSprite.pixelsPerUnit, Is.EqualTo(reference.pixelsPerUnit), season.DefinitionId);
                Assert.That(season.CircleRingSprite.bounds.size, Is.EqualTo(reference.bounds.size), season.DefinitionId);
            }
        }

        [Test]
        public void VisibleSegmentsUseTheCenterOfEachAngularSection()
        {
            GameObject mapObject = new GameObject("Test Circle Map");
            try
            {
                CircleMapView mapView = mapObject.AddComponent<CircleMapView>();
                SetPrivateField(mapView, "visibleSegmentCount", 8);

                float firstAngle = InvokePrivate<float>(mapView, "GetLocalAngleOnCircle", 0);
                float secondAngle = InvokePrivate<float>(mapView, "GetLocalAngleOnCircle", 1);

                Assert.That(firstAngle, Is.EqualTo(-67.5f).Within(0.0001f));
                Assert.That(secondAngle, Is.EqualTo(-22.5f).Within(0.0001f));
                Assert.That(secondAngle - firstAngle, Is.EqualTo(45f).Within(0.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mapObject);
            }
        }

        [Test]
        public void RoadMaximumAndInteractionRangeUseSegmentCenters()
        {
            GameObject mapObject = new GameObject("Test Circle Map");
            try
            {
                CircleMapView mapView = mapObject.AddComponent<CircleMapView>();
                List<CircleRoadSegmentData> roadSegments = GetPrivateField<List<CircleRoadSegmentData>>(
                    mapView,
                    "roadSegmentList");
                for (int roadIndex = 0; roadIndex < 40; roadIndex++)
                {
                    roadSegments.Add(new CircleRoadSegmentData("Test Segment " + roadIndex, null));
                }

                SetPrivateField(mapView, "interactionPromptRangeInSegments", 0.3f);
                Assert.That(
                    InvokePrivate<float>(mapView, "GetMaximumRoadPosition"),
                    Is.EqualTo(39.5f).Within(0.0001f));

                SetPrivateField(mapView, "currentRoadPosition", 0.5f);
                Assert.That(InvokePrivate<bool>(mapView, "IsPlayerNearRoadSegmentCenter", 0), Is.True);
                SetPrivateField(mapView, "currentRoadPosition", 0.2f);
                Assert.That(InvokePrivate<bool>(mapView, "IsPlayerNearRoadSegmentCenter", 0), Is.True);
                SetPrivateField(mapView, "currentRoadPosition", 0.8f);
                Assert.That(InvokePrivate<bool>(mapView, "IsPlayerNearRoadSegmentCenter", 0), Is.True);
                SetPrivateField(mapView, "currentRoadPosition", 0.19f);
                Assert.That(InvokePrivate<bool>(mapView, "IsPlayerNearRoadSegmentCenter", 0), Is.False);
                SetPrivateField(mapView, "currentRoadPosition", 0.81f);
                Assert.That(InvokePrivate<bool>(mapView, "IsPlayerNearRoadSegmentCenter", 0), Is.False);

                SetPrivateField(mapView, "currentRoadPosition", 39.5f);
                Assert.That(InvokePrivate<bool>(mapView, "IsPlayerNearRoadSegmentCenter", 39), Is.True);
                Assert.That(InvokePrivate<bool>(mapView, "IsAtEndOfCurrentSeason"), Is.True);
                SetPrivateField(mapView, "currentRoadPosition", 39.49f);
                Assert.That(InvokePrivate<bool>(mapView, "IsAtEndOfCurrentSeason"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mapObject);
            }
        }

        [Test]
        public void InteractionAvailabilityOnlyIncludesIncompleteInteractiveContent()
        {
            GameObject mapObject = new GameObject("Test Circle Map");
            GameObject hudObject = new GameObject("Test HUD");
            try
            {
                CircleMapView mapView = mapObject.AddComponent<CircleMapView>();
                GameHud hud = hudObject.AddComponent<GameHud>();
                SetPrivateField(mapView, "gameHud", hud);

                RoadSegmentDefinition[] definitions = Resources.LoadAll<RoadSegmentDefinition>(
                    "GameData/RoadSegments");
                SegmentContentType[] interactiveTypes =
                {
                    SegmentContentType.Npc,
                    SegmentContentType.Event,
                    SegmentContentType.Resource
                };

                foreach (SegmentContentType contentType in interactiveTypes)
                {
                    RoadSegmentDefinition definition = definitions.First(
                        candidate => candidate.ContentType == contentType);
                    CircleRoadSegmentData segment = new CircleRoadSegmentData(definition, null);
                    string interactionId = GetInteractionId(segment);

                    Assert.That(
                        InvokePrivate<bool>(mapView, "IsInteractionAvailable", segment),
                        Is.True,
                        contentType.ToString());
                    hud.RuntimeData.State.MarkEventCompleted(interactionId);
                    Assert.That(
                        InvokePrivate<bool>(mapView, "IsInteractionAvailable", segment),
                        Is.False,
                        contentType.ToString());
                }

                SegmentContentType[] nonInteractiveTypes =
                {
                    SegmentContentType.None,
                    SegmentContentType.Monster,
                    SegmentContentType.Facility,
                    SegmentContentType.Boss
                };
                foreach (SegmentContentType contentType in nonInteractiveTypes)
                {
                    CircleRoadSegmentData segment = new CircleRoadSegmentData("Test", null, contentType);
                    Assert.That(
                        InvokePrivate<bool>(mapView, "IsInteractionAvailable", segment),
                        Is.False,
                        contentType.ToString());
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mapObject);
                UnityEngine.Object.DestroyImmediate(hudObject);
            }
        }

        [Test]
        public void InteractionRequiresPlayerToBeNearTheSegmentCenter()
        {
            GameObject mapObject = new GameObject("Test Circle Map");
            GameObject hudObject = new GameObject("Test HUD");
            try
            {
                CircleMapView mapView = mapObject.AddComponent<CircleMapView>();
                GameHud hud = hudObject.AddComponent<GameHud>();
                SetPrivateField(mapView, "gameHud", hud);
                SetPrivateField(mapView, "interactionPromptRangeInSegments", 0.3f);

                RoadSegmentDefinition resourceDefinition = Resources.LoadAll<RoadSegmentDefinition>(
                        "GameData/RoadSegments")
                    .First(candidate => candidate.ContentType == SegmentContentType.Resource);
                CircleRoadSegmentData resourceSegment = new CircleRoadSegmentData(resourceDefinition, null);
                GetPrivateField<List<CircleRoadSegmentData>>(mapView, "roadSegmentList").Add(resourceSegment);

                SetPrivateField(mapView, "currentRoadPosition", 0.19f);
                InvokePrivate<object>(mapView, "TryInteractWithCurrentRoadSegment");
                Assert.That(hud.RuntimeData.IsInteractionCompleted(resourceSegment.segmentId), Is.False);

                SetPrivateField(mapView, "currentRoadPosition", 0.5f);
                InvokePrivate<object>(mapView, "TryInteractWithCurrentRoadSegment");
                Assert.That(hud.RuntimeData.IsInteractionCompleted(resourceSegment.segmentId), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mapObject);
                UnityEngine.Object.DestroyImmediate(hudObject);
            }
        }

        [Test]
        public void InteractionPromptCanBeShownAndHiddenIndependentlyOfSegmentContent()
        {
            GameObject root = new GameObject("Test Segment");
            Texture2D texture = new Texture2D(1, 1);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            try
            {
                SpriteRenderer segmentRenderer = new GameObject("Image").AddComponent<SpriteRenderer>();
                segmentRenderer.transform.SetParent(root.transform, false);
                SpriteRenderer promptRenderer = new GameObject("Interaction Prompt Image").AddComponent<SpriteRenderer>();
                promptRenderer.transform.SetParent(root.transform, false);

                CircleMapSegment mapSegment = root.AddComponent<CircleMapSegment>();
                mapSegment.Setup(segmentRenderer, promptRenderer, sprite, sprite, sprite, 0f);
                CircleRoadSegmentData roadSegment = new CircleRoadSegmentData(
                    "Test Resource",
                    sprite,
                    SegmentContentType.Resource);

                mapSegment.Show(roadSegment);
                Assert.That(promptRenderer.enabled, Is.False);
                Assert.That(promptRenderer.sprite, Is.Null);

                mapSegment.SetInteractionPromptVisible(roadSegment, true);
                Assert.That(promptRenderer.enabled, Is.True);
                Assert.That(promptRenderer.sprite, Is.SameAs(sprite));

                mapSegment.SetInteractionPromptVisible(roadSegment, false);
                Assert.That(promptRenderer.enabled, Is.False);
                Assert.That(promptRenderer.sprite, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void NpcRoadSegmentUsesNativeAnimatorForIdleAnimation()
        {
            CharacterDefinition character =
                Resources.Load<CharacterDefinition>("GameData/Characters/Graff");
            Sprite fallbackSprite = CreateTestSprite();
            Texture2D fallbackTexture = fallbackSprite.texture;
            GameObject root = new GameObject("Animated NPC Segment");
            RoadSegmentDefinition definition =
                ScriptableObject.CreateInstance<RoadSegmentDefinition>();
            try
            {
                Assert.That(character, Is.Not.Null);

                SerializedObject serializedDefinition = new SerializedObject(definition);
                serializedDefinition.FindProperty("contentType").enumValueIndex =
                    (int)SegmentContentType.Npc;
                serializedDefinition.FindProperty("character").objectReferenceValue = character;
                serializedDefinition.FindProperty("mapSprite").objectReferenceValue = fallbackSprite;
                serializedDefinition.ApplyModifiedPropertiesWithoutUndo();

                SpriteRenderer renderer =
                    new GameObject("Animated NPC Image").AddComponent<SpriteRenderer>();
                renderer.transform.SetParent(root.transform, false);
                CircleMapSegment mapSegment = root.AddComponent<CircleMapSegment>();
                mapSegment.Setup(renderer, null, null, null, null, 0f);
                mapSegment.Show(new CircleRoadSegmentData(definition, fallbackSprite));

                Animator animator = renderer.GetComponent<Animator>();
                Assert.That(animator, Is.Not.Null);
                Assert.That(animator.enabled, Is.True);
                Assert.That(animator.runtimeAnimatorController, Is.Not.Null);
                Assert.That(
                    animator.runtimeAnimatorController.name,
                    Is.EqualTo("graff_idle_controller"));
                Assert.That(renderer.sprite, Is.Not.Null);
                Assert.That(renderer.sprite.name, Is.EqualTo("frame_004"));

                animator.Update(0.1f);

                Assert.That(renderer.sprite.name, Is.EqualTo("frame_005"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(definition);
                UnityEngine.Object.DestroyImmediate(fallbackSprite);
                UnityEngine.Object.DestroyImmediate(fallbackTexture);
            }
        }

        [Test]
        public void EliNpcRoadSegmentUsesStaticSpriteWithoutAnimatorPlayback()
        {
            CharacterDefinition character =
                Resources.Load<CharacterDefinition>("GameData/Characters/Eli");
            Sprite fallbackSprite = CreateTestSprite();
            Texture2D fallbackTexture = fallbackSprite.texture;
            GameObject root = new GameObject("Static Eli NPC Segment");
            RoadSegmentDefinition definition =
                ScriptableObject.CreateInstance<RoadSegmentDefinition>();
            try
            {
                Assert.That(character, Is.Not.Null);

                SerializedObject serializedDefinition = new SerializedObject(definition);
                serializedDefinition.FindProperty("contentType").enumValueIndex =
                    (int)SegmentContentType.Npc;
                serializedDefinition.FindProperty("character").objectReferenceValue = character;
                serializedDefinition.FindProperty("mapSprite").objectReferenceValue = fallbackSprite;
                serializedDefinition.ApplyModifiedPropertiesWithoutUndo();

                SpriteRenderer renderer =
                    new GameObject("Static Eli NPC Image").AddComponent<SpriteRenderer>();
                renderer.transform.SetParent(root.transform, false);
                CircleMapSegment mapSegment = root.AddComponent<CircleMapSegment>();
                mapSegment.Setup(renderer, null, null, null, null, 0f);
                mapSegment.Show(new CircleRoadSegmentData(definition, fallbackSprite));

                Animator animator = renderer.GetComponent<Animator>();
                Assert.That(animator, Is.Not.Null);
                Assert.That(animator.enabled, Is.False);
                Assert.That(animator.runtimeAnimatorController, Is.Null);
                Assert.That(renderer.sprite, Is.SameAs(fallbackSprite));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(definition);
                UnityEngine.Object.DestroyImmediate(fallbackSprite);
                UnityEngine.Object.DestroyImmediate(fallbackTexture);
            }
        }

        private static Sprite CreateTestSprite()
        {
            Texture2D texture = new Texture2D(1, 1);
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
        }

        private static T InvokePrivate<T>(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, PrivateInstance);
            Assert.That(method, Is.Not.Null, methodName);
            return (T)method.Invoke(target, arguments);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, PrivateInstance);
            Assert.That(field, Is.Not.Null, fieldName);
            return (T)field.GetValue(target);
        }

        private static string GetInteractionId(CircleRoadSegmentData segment)
        {
            switch (segment.contentType)
            {
                case SegmentContentType.Npc:
                    return segment.dialogue.DefinitionId;
                case SegmentContentType.Event:
                    return segment.gameEvent.DefinitionId;
                case SegmentContentType.Resource:
                    return segment.segmentId;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, PrivateInstance);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }

}
