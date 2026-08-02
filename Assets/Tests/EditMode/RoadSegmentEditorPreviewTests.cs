using System.Collections.Generic;
using System.Linq;
using CircleWar.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CircleWar.Tests
{
    public sealed class RoadSegmentEditorPreviewTests
    {
        private readonly List<Object> createdAssets = new List<Object>();
        private Scene previewScene;

        [TearDown]
        public void TearDown()
        {
            RoadSegmentScenePreview.Clear();

            if (previewScene.IsValid())
            {
                EditorSceneManager.ClosePreviewScene(previewScene);
            }

            for (int index = createdAssets.Count - 1; index >= 0; index--)
            {
                if (createdAssets[index] != null)
                {
                    Object.DestroyImmediate(createdAssets[index]);
                }
            }

            createdAssets.Clear();
        }

        [TestCase(0)]
        [TestCase(2)]
        [TestCase(20)]
        [TestCase(39)]
        public void SelectedRoadIsCenteredAtSixClock(int selectedRoadIndex)
        {
            IReadOnlyList<RoadSegmentPreviewSlot> slots = RoadSegmentPreviewLayout.Build(
                selectedRoadIndex,
                40,
                8);
            RoadSegmentPreviewSlot selectedSlot = slots.Single(slot => slot.IsSelected);

            Assert.That(selectedSlot.RoadIndex, Is.EqualTo(selectedRoadIndex));
            Assert.That(selectedSlot.IsInRange, Is.True);
            Assert.That(
                Mathf.DeltaAngle(-90f, selectedSlot.ViewAngleDegrees),
                Is.Zero.Within(0.0001f));
        }

        [TestCase(0, new int[] { 0, 1, 2, 3, 4 })]
        [TestCase(2, new int[] { 0, 1, 2, 3, 4, 5, 6 })]
        [TestCase(20, new int[] { 24, 17, 18, 19, 20, 21, 22, 23 })]
        [TestCase(39, new int[] { 36, 37, 38, 39 })]
        public void VisibleRoadMappingMatchesRuntimeBoundaries(
            int selectedRoadIndex,
            int[] expectedRoadIndices)
        {
            IReadOnlyList<RoadSegmentPreviewSlot> slots = RoadSegmentPreviewLayout.Build(
                selectedRoadIndex,
                40,
                8);

            Assert.That(
                slots.Where(slot => slot.IsInRange).Select(slot => slot.RoadIndex),
                Is.EqualTo(expectedRoadIndices));
        }

        [Test]
        public void PreviewUsesDefinitionYAndZAndCleansTransientObjects()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            bool activeSceneWasDirty = activeScene.isDirty;
            Sprite sprite = CreateSprite();
            RoadSegmentDefinition definition = CreateDefinition(sprite, 2, -1.4f, 30f);
            CircleMapView mapView = CreatePreviewMap(sprite);

            Assert.That(RoadSegmentScenePreview.Rebuild(definition, mapView), Is.True);

            GameObject previewRoot = RoadSegmentScenePreview.PreviewRoot;
            Transform selectedSegment = RoadSegmentScenePreview.SelectedSegmentTransform;
            Transform selectedImage = RoadSegmentScenePreview.SelectedImageTransform;
            Assert.That(previewRoot, Is.Not.Null);
            Assert.That(
                (previewRoot.hideFlags & HideFlags.DontSaveInEditor) != HideFlags.None,
                Is.True);
            Assert.That(previewRoot.scene, Is.EqualTo(previewScene));
            Assert.That(selectedSegment, Is.Not.Null);
            Assert.That(selectedImage, Is.Not.Null);
            Assert.That(
                Mathf.DeltaAngle(-90f, GetWorldAngle(selectedSegment.localPosition)),
                Is.Zero.Within(0.0001f));
            Assert.That(
                selectedImage.localPosition.y,
                Is.EqualTo(-sprite.bounds.min.y - 1.4f).Within(0.0001f));
            Assert.That(
                Mathf.DeltaAngle(30f, selectedImage.localEulerAngles.z),
                Is.Zero.Within(0.0001f));
            Assert.That(activeScene.isDirty, Is.EqualTo(activeSceneWasDirty));

            RoadSegmentScenePreview.Clear();

            Assert.That(previewRoot == null, Is.True);
            Assert.That(RoadSegmentScenePreview.PreviewRoot, Is.Null);
        }

        [Test]
        public void MissingSpriteAndMissingMapFailSafely()
        {
            RoadSegmentDefinition definition = CreateDefinition(null, 2, -1.4f, 30f);
            CircleMapView mapView = CreatePreviewMap(CreateSprite());

            Assert.DoesNotThrow(() => RoadSegmentScenePreview.Rebuild(definition, mapView));
            Assert.That(
                RoadSegmentScenePreview.SelectedImageTransform
                    .GetComponent<SpriteRenderer>().enabled,
                Is.False);

            Assert.That(RoadSegmentScenePreview.Rebuild(definition, null), Is.False);
            Assert.That(
                RoadSegmentScenePreview.StatusKind,
                Is.EqualTo(RoadSegmentPreviewStatusKind.MissingMapView));
        }

        private CircleMapView CreatePreviewMap(Sprite ringSprite)
        {
            previewScene = EditorSceneManager.NewPreviewScene();

            GameObject mapObject = new GameObject("Test CircleMapView");
            SceneManager.MoveGameObjectToScene(mapObject, previewScene);
            CircleMapView mapView = mapObject.AddComponent<CircleMapView>();

            GameObject rotatingRoot = new GameObject("Circle Rotating Root");
            SceneManager.MoveGameObjectToScene(rotatingRoot, previewScene);
            GameObject ringObject = new GameObject("Circle Ring");
            ringObject.transform.SetParent(rotatingRoot.transform, false);
            SpriteRenderer ringRenderer = ringObject.AddComponent<SpriteRenderer>();
            ringRenderer.sprite = ringSprite;

            SerializedObject serializedMap = new SerializedObject(mapView);
            serializedMap.FindProperty("circleRingRenderer").objectReferenceValue = ringRenderer;
            serializedMap.FindProperty("totalRoadSegmentCount").intValue = 40;
            serializedMap.FindProperty("visibleSegmentCount").intValue = 8;
            serializedMap.FindProperty("segmentInsetFromRing").floatValue = 0.22f;
            serializedMap.FindProperty("segmentScale").floatValue = 0.4f;
            serializedMap.ApplyModifiedPropertiesWithoutUndo();
            return mapView;
        }

        private RoadSegmentDefinition CreateDefinition(
            Sprite sprite,
            int roadIndex,
            float y,
            float z)
        {
            RoadSegmentDefinition definition =
                ScriptableObject.CreateInstance<RoadSegmentDefinition>();
            createdAssets.Add(definition);

            SerializedObject serializedDefinition = new SerializedObject(definition);
            serializedDefinition.FindProperty("roadIndex").intValue = roadIndex;
            serializedDefinition.FindProperty("mapSprite").objectReferenceValue = sprite;
            serializedDefinition.FindProperty("y").floatValue = y;
            serializedDefinition.FindProperty("z").floatValue = z;
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private Sprite CreateSprite()
        {
            Texture2D texture = new Texture2D(4, 8);
            createdAssets.Add(texture);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                1f);
            createdAssets.Add(sprite);
            return sprite;
        }

        private static float GetWorldAngle(Vector3 position)
        {
            return Mathf.Atan2(position.y, position.x) * Mathf.Rad2Deg;
        }
    }
}
