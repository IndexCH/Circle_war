using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CircleWar.EditorTools
{
    public enum RoadSegmentPreviewStatusKind
    {
        Disabled,
        Ready,
        MissingSelection,
        Playing,
        MissingMapView,
        InvalidMapView
    }

    [InitializeOnLoad]
    public static class RoadSegmentScenePreview
    {
        public const string PreviewRootName = "[Circle War] Road Segment Editor Preview";

        private const string PreviewEnabledSessionKey = "CircleWar.RoadSegmentPreview.Enabled";
        private const HideFlags PreviewHideFlags = HideFlags.HideAndDontSave;

        private static GameObject previewRoot;
        private static RoadSegmentDefinition currentDefinition;
        private static CircleMapSegment selectedSegment;
        private static Transform selectedSegmentTransform;
        private static Transform selectedImageTransform;
        private static SpriteRenderer selectedSpriteRenderer;
        private static bool isRefreshing;

        static RoadSegmentScenePreview()
        {
            AssemblyReloadEvents.beforeAssemblyReload += DestroyPreviewObjects;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
            Selection.selectionChanged += OnSelectionChanged;
            Undo.undoRedoPerformed += OnUndoRedo;
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.delayCall += InitializeAfterReload;
        }

        public static bool IsEnabled => SessionState.GetBool(PreviewEnabledSessionKey, false);
        public static RoadSegmentPreviewStatusKind StatusKind { get; private set; } =
            RoadSegmentPreviewStatusKind.Disabled;
        public static string StatusMessage { get; private set; } = "Scene 预览已关闭。";
        public static RoadSegmentDefinition CurrentDefinition => currentDefinition;
        public static GameObject PreviewRoot => previewRoot;
        public static Transform SelectedSegmentTransform => selectedSegmentTransform;
        public static Transform SelectedImageTransform => selectedImageTransform;

        public static void SetEnabled(bool enabled)
        {
            SessionState.SetBool(PreviewEnabledSessionKey, enabled);
            if (!enabled)
            {
                currentDefinition = null;
                DestroyPreviewObjects();
                SetStatus(RoadSegmentPreviewStatusKind.Disabled, "Scene 预览已关闭。");
                return;
            }

            RefreshFromSelection();
        }

        public static void EnsurePreview(RoadSegmentDefinition definition)
        {
            if (!IsEnabled || definition == null || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (currentDefinition != definition || previewRoot == null)
            {
                Rebuild(definition);
            }
        }

        public static bool Rebuild(RoadSegmentDefinition definition)
        {
            return Rebuild(definition, FindCircleMapViewInActiveScene());
        }

        public static bool Rebuild(RoadSegmentDefinition definition, CircleMapView mapView)
        {
            if (isRefreshing)
            {
                return previewRoot != null;
            }

            isRefreshing = true;
            try
            {
                DestroyPreviewObjects();
                currentDefinition = definition;

                if (definition == null)
                {
                    SetStatus(
                        RoadSegmentPreviewStatusKind.MissingSelection,
                        "请选择一个 RoadSegmentDefinition 资源。");
                    return false;
                }

                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SetStatus(
                        RoadSegmentPreviewStatusKind.Playing,
                        "进入 Play Mode 时编辑预览会自动暂停。");
                    return false;
                }

                if (mapView == null)
                {
                    SetStatus(
                        RoadSegmentPreviewStatusKind.MissingMapView,
                        "当前活动场景中没有 CircleMapView，无法取得圆环布局参数。");
                    return false;
                }

                MapPreviewSettings settings;
                if (!TryReadMapSettings(mapView, out settings))
                {
                    SetStatus(
                        RoadSegmentPreviewStatusKind.InvalidMapView,
                        "CircleMapView 缺少 Circle Ring、Sprite 或父节点引用，无法创建预览。");
                    return false;
                }

                BuildPreview(definition, settings);
                SetStatus(
                    RoadSegmentPreviewStatusKind.Ready,
                    "正在预览道路 " + definition.RoadIndex + "；所选节点位于 6 点钟位置。");
                SceneView.RepaintAll();
                return true;
            }
            finally
            {
                isRefreshing = false;
            }
        }

        public static void Clear()
        {
            currentDefinition = null;
            DestroyPreviewObjects();
            SetStatus(
                IsEnabled
                    ? RoadSegmentPreviewStatusKind.MissingSelection
                    : RoadSegmentPreviewStatusKind.Disabled,
                IsEnabled ? "请选择一个 RoadSegmentDefinition 资源。" : "Scene 预览已关闭。");
        }

        private static void InitializeAfterReload()
        {
            DestroyStalePreviewObjects();
            if (IsEnabled)
            {
                RefreshFromSelection();
            }
        }

        private static void RefreshFromSelection()
        {
            if (!IsEnabled)
            {
                return;
            }

            RoadSegmentDefinition definition = Selection.activeObject as RoadSegmentDefinition;
            if (definition == null)
            {
                Clear();
                return;
            }

            Rebuild(definition);
        }

        private static void OnSelectionChanged()
        {
            RefreshFromSelection();
        }

        private static void OnUndoRedo()
        {
            if (IsEnabled && currentDefinition != null)
            {
                Rebuild(currentDefinition);
            }
        }

        private static void OnActiveSceneChanged(Scene previousScene, Scene newScene)
        {
            DestroyPreviewObjects();
            if (IsEnabled)
            {
                EditorApplication.delayCall += RefreshFromSelection;
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode ||
                state == PlayModeStateChange.EnteredPlayMode)
            {
                DestroyPreviewObjects();
                SetStatus(
                    RoadSegmentPreviewStatusKind.Playing,
                    "进入 Play Mode 时编辑预览会自动暂停。");
                return;
            }

            if (state == PlayModeStateChange.EnteredEditMode && IsEnabled)
            {
                EditorApplication.delayCall += RefreshFromSelection;
            }
        }

        private static CircleMapView FindCircleMapViewInActiveScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            CircleMapView[] mapViews = Object.FindObjectsByType<CircleMapView>(
                FindObjectsInactive.Include);

            for (int index = 0; index < mapViews.Length; index++)
            {
                CircleMapView mapView = mapViews[index];
                if (mapView != null && mapView.gameObject.scene == activeScene)
                {
                    return mapView;
                }
            }

            return null;
        }

        private static bool TryReadMapSettings(
            CircleMapView mapView,
            out MapPreviewSettings settings)
        {
            settings = default;
            SerializedObject serializedMap = new SerializedObject(mapView);
            SerializedProperty ringProperty = serializedMap.FindProperty("circleRingRenderer");
            SerializedProperty totalCountProperty = serializedMap.FindProperty("totalRoadSegmentCount");
            SerializedProperty visibleCountProperty = serializedMap.FindProperty("visibleSegmentCount");
            SerializedProperty insetProperty = serializedMap.FindProperty("segmentInsetFromRing");
            SerializedProperty scaleProperty = serializedMap.FindProperty("segmentScale");

            SpriteRenderer ringRenderer = ringProperty != null
                ? ringProperty.objectReferenceValue as SpriteRenderer
                : null;
            if (ringRenderer == null ||
                ringRenderer.sprite == null ||
                ringRenderer.transform.parent == null ||
                totalCountProperty == null ||
                visibleCountProperty == null ||
                insetProperty == null ||
                scaleProperty == null ||
                visibleCountProperty.intValue <= 0)
            {
                return false;
            }

            Vector2 spriteSize = ringRenderer.sprite.bounds.size;
            Vector3 ringScale = ringRenderer.transform.localScale;
            float ringWidth = spriteSize.x * Mathf.Abs(ringScale.x);
            settings = new MapPreviewSettings(
                ringRenderer.transform.parent,
                Mathf.Max(0, totalCountProperty.intValue),
                visibleCountProperty.intValue,
                ringWidth * 0.5f - insetProperty.floatValue,
                scaleProperty.floatValue);
            return true;
        }

        private static void BuildPreview(
            RoadSegmentDefinition definition,
            MapPreviewSettings settings)
        {
            previewRoot = CreatePreviewGameObject(PreviewRootName);
            previewRoot.transform.SetParent(settings.Parent, false);
            previewRoot.transform.localPosition = Vector3.zero;
            previewRoot.transform.localRotation = Quaternion.identity;
            previewRoot.transform.localScale = Vector3.one;

            Dictionary<int, RoadSegmentDefinition> definitions = LoadSeasonDefinitions(definition);
            IReadOnlyList<RoadSegmentPreviewSlot> slots = RoadSegmentPreviewLayout.Build(
                definition.RoadIndex,
                settings.TotalRoadSegmentCount,
                settings.VisibleSegmentCount);

            for (int index = 0; index < slots.Count; index++)
            {
                RoadSegmentPreviewSlot slot = slots[index];
                RoadSegmentDefinition slotDefinition;
                if (!slot.IsInRange || !definitions.TryGetValue(slot.RoadIndex, out slotDefinition))
                {
                    continue;
                }

                float angleRadians = slot.ViewAngleDegrees * Mathf.Deg2Rad;
                GameObject segmentObject = CreatePreviewGameObject(
                    "Preview Road " + slot.RoadIndex);
                segmentObject.transform.SetParent(previewRoot.transform, false);
                segmentObject.transform.localPosition = new Vector3(
                    Mathf.Cos(angleRadians) * settings.Radius,
                    Mathf.Sin(angleRadians) * settings.Radius,
                    0f);
                segmentObject.transform.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    slot.ViewAngleDegrees - RoadSegmentPreviewLayout.CircleStartAngle);
                segmentObject.transform.localScale = new Vector3(
                    settings.SegmentScale,
                    settings.SegmentScale,
                    1f);

                GameObject imageObject = CreatePreviewGameObject("Image");
                imageObject.transform.SetParent(segmentObject.transform, false);
                SpriteRenderer spriteRenderer = imageObject.AddComponent<SpriteRenderer>();
                spriteRenderer.hideFlags = PreviewHideFlags;
                spriteRenderer.sortingOrder = 5;

                CircleMapSegment segment = segmentObject.AddComponent<CircleMapSegment>();
                segment.hideFlags = PreviewHideFlags;
                segment.Setup(spriteRenderer, null, null, null, null, 0f);
                segment.Show(new CircleRoadSegmentData(slotDefinition, null));

                if (slot.IsSelected)
                {
                    selectedSegment = segment;
                    selectedSegmentTransform = segmentObject.transform;
                    selectedImageTransform = imageObject.transform;
                    selectedSpriteRenderer = spriteRenderer;
                }
            }
        }

        private static Dictionary<int, RoadSegmentDefinition> LoadSeasonDefinitions(
            RoadSegmentDefinition selectedDefinition)
        {
            Dictionary<int, RoadSegmentDefinition> definitions =
                new Dictionary<int, RoadSegmentDefinition>();
            string[] definitionGuids = AssetDatabase.FindAssets("t:RoadSegmentDefinition");

            for (int index = 0; index < definitionGuids.Length; index++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(definitionGuids[index]);
                RoadSegmentDefinition definition =
                    AssetDatabase.LoadAssetAtPath<RoadSegmentDefinition>(assetPath);
                if (definition == null || definition.Season != selectedDefinition.Season)
                {
                    continue;
                }

                if (!definitions.ContainsKey(definition.RoadIndex))
                {
                    definitions.Add(definition.RoadIndex, definition);
                }
            }

            definitions[selectedDefinition.RoadIndex] = selectedDefinition;
            return definitions;
        }

        private static GameObject CreatePreviewGameObject(string objectName)
        {
            GameObject previewObject = new GameObject(objectName);
            previewObject.hideFlags = PreviewHideFlags;
            previewObject.transform.hideFlags = PreviewHideFlags;
            return previewObject;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!IsEnabled ||
                StatusKind != RoadSegmentPreviewStatusKind.Ready ||
                currentDefinition == null ||
                selectedSegmentTransform == null)
            {
                return;
            }

            DrawSelectedMarker();
            if (selectedSpriteRenderer == null || selectedSpriteRenderer.sprite == null)
            {
                return;
            }

            DrawYOffsetHandle();
            DrawZRotationHandle();
        }

        private static void DrawSelectedMarker()
        {
            Vector3 markerPosition = selectedSegmentTransform.position;
            float markerSize = HandleUtility.GetHandleSize(markerPosition) * 0.18f;
            using (new Handles.DrawingScope(new Color(0.15f, 0.95f, 1f, 0.95f)))
            {
                Handles.DrawWireDisc(
                    markerPosition,
                    selectedSegmentTransform.forward,
                    markerSize);
            }

            Vector3 labelPosition = selectedSpriteRenderer != null && selectedSpriteRenderer.enabled
                ? selectedSpriteRenderer.bounds.max
                : markerPosition;
            Handles.Label(
                labelPosition,
                "#" + currentDefinition.RoadIndex + " " + currentDefinition.DisplayName +
                "\nY " + currentDefinition.Y.ToString("0.###") +
                "   Z " + currentDefinition.Z.ToString("0.###") + "°");
        }

        private static void DrawYOffsetHandle()
        {
            Vector3 currentPosition = selectedImageTransform.position;
            Vector3 radialDirection = selectedSegmentTransform.up.normalized;
            float handleSize = HandleUtility.GetHandleSize(currentPosition) * 0.12f;
            float worldSnap = Mathf.Abs(
                EditorSnapSettings.move.y * selectedSegmentTransform.lossyScale.y);

            EditorGUI.BeginChangeCheck();
            Vector3 newPosition;
            using (new Handles.DrawingScope(new Color(0.2f, 1f, 0.45f, 1f)))
            {
                newPosition = Handles.Slider(
                    currentPosition,
                    radialDirection,
                    handleSize,
                    Handles.ArrowHandleCap,
                    worldSnap);
            }

            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            Vector3 newLocalPosition = selectedSegmentTransform.InverseTransformPoint(newPosition);
            float bottomAlignedY = -selectedSpriteRenderer.sprite.bounds.min.y *
                                   selectedImageTransform.localScale.y;
            ApplyFloatProperty(
                currentDefinition,
                "y",
                newLocalPosition.y - bottomAlignedY,
                "Adjust Road MapSprite Y");
        }

        private static void DrawZRotationHandle()
        {
            Vector3 pivot = selectedImageTransform.position;
            Vector3 axis = selectedSegmentTransform.forward.normalized;
            float handleSize = HandleUtility.GetHandleSize(pivot) * 0.42f;

            EditorGUI.BeginChangeCheck();
            Quaternion newWorldRotation;
            using (new Handles.DrawingScope(new Color(1f, 0.75f, 0.15f, 1f)))
            {
                newWorldRotation = Handles.Disc(
                    selectedImageTransform.rotation,
                    pivot,
                    axis,
                    handleSize,
                    false,
                    EditorSnapSettings.rotate);
            }

            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            Quaternion newLocalRotation =
                Quaternion.Inverse(selectedSegmentTransform.rotation) * newWorldRotation;
            float signedZ = Mathf.DeltaAngle(0f, newLocalRotation.eulerAngles.z);
            ApplyFloatProperty(
                currentDefinition,
                "z",
                signedZ,
                "Adjust Road MapSprite Z");
        }

        private static void ApplyFloatProperty(
            RoadSegmentDefinition definition,
            string propertyName,
            float value,
            string undoName)
        {
            Undo.RecordObject(definition, undoName);
            SerializedObject serializedDefinition = new SerializedObject(definition);
            SerializedProperty property = serializedDefinition.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            property.floatValue = value;
            serializedDefinition.ApplyModifiedProperties();
            EditorUtility.SetDirty(definition);
            RefreshSelectedVisual();
        }

        private static void RefreshSelectedVisual()
        {
            if (selectedSegment == null || currentDefinition == null)
            {
                return;
            }

            selectedSegment.Show(new CircleRoadSegmentData(currentDefinition, null));
            SceneView.RepaintAll();
        }

        private static void DestroyPreviewObjects()
        {
            selectedSegment = null;
            selectedSegmentTransform = null;
            selectedImageTransform = null;
            selectedSpriteRenderer = null;

            if (previewRoot != null)
            {
                Object.DestroyImmediate(previewRoot);
                previewRoot = null;
            }

            SceneView.RepaintAll();
        }

        private static void DestroyStalePreviewObjects()
        {
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int index = 0; index < allObjects.Length; index++)
            {
                GameObject previewObject = allObjects[index];
                if (previewObject != null &&
                    previewObject != previewRoot &&
                    previewObject.name == PreviewRootName &&
                    (previewObject.hideFlags & HideFlags.DontSaveInEditor) != 0)
                {
                    Object.DestroyImmediate(previewObject);
                }
            }
        }

        private static void SetStatus(
            RoadSegmentPreviewStatusKind statusKind,
            string statusMessage)
        {
            StatusKind = statusKind;
            StatusMessage = statusMessage;
        }

        private readonly struct MapPreviewSettings
        {
            public MapPreviewSettings(
                Transform parent,
                int totalRoadSegmentCount,
                int visibleSegmentCount,
                float radius,
                float segmentScale)
            {
                Parent = parent;
                TotalRoadSegmentCount = totalRoadSegmentCount;
                VisibleSegmentCount = visibleSegmentCount;
                Radius = radius;
                SegmentScale = segmentScale;
            }

            public Transform Parent { get; }
            public int TotalRoadSegmentCount { get; }
            public int VisibleSegmentCount { get; }
            public float Radius { get; }
            public float SegmentScale { get; }
        }
    }
}
