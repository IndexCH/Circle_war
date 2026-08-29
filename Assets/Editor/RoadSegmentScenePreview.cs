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
        private const string PreviewBackgroundName =
            "[Circle War] Road Segment Editor Preview Background";

        private const string PreviewEnabledSessionKey = "CircleWar.RoadSegmentPreview.Enabled";
        private const string DialogueInteractionPromptResourcePath =
            "Scence/UI/InteractionPrompts/press_e_dialogue";
        private const string EventInteractionPromptResourcePath =
            "Scence/UI/InteractionPrompts/press_e_investigate";
        private const string ResourceInteractionPromptResourcePath =
            "Scence/UI/InteractionPrompts/press_e_collect";
        private const HideFlags PreviewHideFlags = HideFlags.HideAndDontSave;

        private static GameObject previewRoot;
        private static GameObject previewBackgroundObject;
        private static RoadSegmentDefinition currentDefinition;
        private static CircleMapSegment selectedSegment;
        private static Transform selectedSegmentTransform;
        private static Transform selectedImageTransform;
        private static SpriteRenderer selectedSpriteRenderer;
        private static Camera previewCamera;
        private static Vector3 previewCameraTarget;
        private static readonly List<RendererVisibilityState> hiddenSourceRenderers =
            new List<RendererVisibilityState>();
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

        public static void SyncSceneViewToGameCamera()
        {
            SyncSceneViewToGameCamera(previewCamera, previewCameraTarget);
        }

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
                    "正在预览道路 " + definition.RoadIndex +
                    "；圆环层级、节点、交互提示和游戏相机已按运行时状态同步。");
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
            SerializedProperty backgroundProperty = serializedMap.FindProperty("backgroundRenderer");
            SerializedProperty ringProperty = serializedMap.FindProperty("circleRingRenderer");
            SerializedProperty totalCountProperty = serializedMap.FindProperty("totalRoadSegmentCount");
            SerializedProperty visibleCountProperty = serializedMap.FindProperty("visibleSegmentCount");
            SerializedProperty insetProperty = serializedMap.FindProperty("segmentInsetFromRing");
            SerializedProperty scaleProperty = serializedMap.FindProperty("segmentScale");
            SerializedProperty promptOffsetProperty = serializedMap.FindProperty(
                "interactionPromptHorizontalOffset");
            SerializedProperty promptScaleProperty = serializedMap.FindProperty(
                "interactionPromptScale");
            SerializedProperty npcPromptProperty = serializedMap.FindProperty(
                "npcInteractionPromptSprite");
            SerializedProperty eventPromptProperty = serializedMap.FindProperty(
                "eventInteractionPromptSprite");
            SerializedProperty resourcePromptProperty = serializedMap.FindProperty(
                "resourceInteractionPromptSprite");

            SpriteRenderer backgroundRenderer = backgroundProperty != null
                ? backgroundProperty.objectReferenceValue as SpriteRenderer
                : null;
            SpriteRenderer ringRenderer = ringProperty != null
                ? ringProperty.objectReferenceValue as SpriteRenderer
                : null;
            if (ringRenderer == null ||
                ringRenderer.sprite == null ||
                ringRenderer.transform.parent == null ||
                ringRenderer.transform.parent.childCount == 0 ||
                totalCountProperty == null ||
                visibleCountProperty == null ||
                insetProperty == null ||
                scaleProperty == null ||
                promptOffsetProperty == null ||
                promptScaleProperty == null ||
                visibleCountProperty.intValue <= 0)
            {
                return false;
            }

            Transform rotatingRoot = ringRenderer.transform.parent;
            Transform segmentParent = rotatingRoot.GetChild(0);
            Vector2 spriteSize = ringRenderer.sprite.bounds.size;
            Vector3 ringScale = ringRenderer.transform.localScale;
            float ringWidth = spriteSize.x * Mathf.Abs(ringScale.x);
            settings = new MapPreviewSettings(
                rotatingRoot,
                segmentParent.GetSiblingIndex(),
                ringRenderer.transform.GetSiblingIndex(),
                Mathf.Max(0, totalCountProperty.intValue),
                visibleCountProperty.intValue,
                ringWidth * 0.5f - insetProperty.floatValue,
                scaleProperty.floatValue,
                promptOffsetProperty.floatValue,
                promptScaleProperty.floatValue > 0f ? promptScaleProperty.floatValue : 1f,
                ResolvePromptSprite(npcPromptProperty, DialogueInteractionPromptResourcePath),
                ResolvePromptSprite(eventPromptProperty, EventInteractionPromptResourcePath),
                ResolvePromptSprite(resourcePromptProperty, ResourceInteractionPromptResourcePath),
                FindGameCamera(mapView),
                backgroundRenderer);
            return true;
        }

        private static Sprite ResolvePromptSprite(
            SerializedProperty spriteProperty,
            string resourcePath)
        {
            Sprite sprite = spriteProperty != null
                ? spriteProperty.objectReferenceValue as Sprite
                : null;
            return sprite != null ? sprite : Resources.Load<Sprite>(resourcePath);
        }

        private static Camera FindGameCamera(CircleMapView mapView)
        {
            if (mapView == null)
            {
                return null;
            }

            Camera fallbackCamera = null;
            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
            for (int index = 0; index < cameras.Length; index++)
            {
                Camera camera = cameras[index];
                if (camera == null || camera.gameObject.scene != mapView.gameObject.scene)
                {
                    continue;
                }

                if (camera.CompareTag("MainCamera"))
                {
                    return camera;
                }

                if (fallbackCamera == null)
                {
                    fallbackCamera = camera;
                }
            }

            return fallbackCamera;
        }

        private static void BuildPreview(
            RoadSegmentDefinition definition,
            MapPreviewSettings settings)
        {
            previewRoot = Object.Instantiate(
                settings.RotatingRoot.gameObject,
                settings.RotatingRoot.parent);
            previewRoot.name = PreviewRootName;
            SetPreviewHideFlagsRecursively(previewRoot);
            previewRoot.transform.localPosition = settings.RotatingRoot.localPosition;
            previewRoot.transform.localRotation = Quaternion.Euler(
                0f,
                0f,
                RoadSegmentPreviewLayout.GetPreviewRootRotationDegrees(
                    definition.RoadIndex,
                    settings.VisibleSegmentCount));
            previewRoot.transform.localScale = settings.RotatingRoot.localScale;

            BuildPreviewBackground(definition, settings.BackgroundRenderer);

            Transform previewSegmentParent = previewRoot.transform.GetChild(
                settings.SegmentParentSiblingIndex);
            DestroyChildren(previewSegmentParent);

            Transform previewRingTransform = previewRoot.transform.GetChild(
                settings.RingSiblingIndex);
            SpriteRenderer previewRingRenderer = previewRingTransform.GetComponent<SpriteRenderer>();
            if (previewRingRenderer != null &&
                definition.Season != null &&
                definition.Season.CircleRingSprite != null)
            {
                previewRingRenderer.sprite = definition.Season.CircleRingSprite;
            }

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

                float angleRadians = slot.LocalAngleDegrees * Mathf.Deg2Rad;
                GameObject segmentObject = CreatePreviewGameObject(
                    "Preview Road " + slot.RoadIndex);
                segmentObject.transform.SetParent(previewSegmentParent, false);
                segmentObject.transform.localPosition = new Vector3(
                    Mathf.Cos(angleRadians) * settings.Radius,
                    Mathf.Sin(angleRadians) * settings.Radius,
                    0f);
                segmentObject.transform.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    slot.LocalAngleDegrees - RoadSegmentPreviewLayout.CircleStartAngle);
                segmentObject.transform.localScale = new Vector3(
                    settings.SegmentScale,
                    settings.SegmentScale,
                    1f);

                GameObject imageObject = CreatePreviewGameObject("Image");
                imageObject.transform.SetParent(segmentObject.transform, false);
                SpriteRenderer spriteRenderer = imageObject.AddComponent<SpriteRenderer>();
                spriteRenderer.hideFlags = PreviewHideFlags;
                spriteRenderer.sortingOrder = 5;

                GameObject npcImageObject = CreatePreviewGameObject("NPC Image");
                npcImageObject.transform.SetParent(segmentObject.transform, false);
                SpriteRenderer npcRenderer = npcImageObject.AddComponent<SpriteRenderer>();
                npcRenderer.hideFlags = PreviewHideFlags;
                npcRenderer.sortingOrder = 6;
                npcRenderer.enabled = false;

                GameObject promptObject = CreatePreviewGameObject("Interaction Prompt Image");
                promptObject.transform.SetParent(segmentObject.transform, false);
                promptObject.transform.localScale = new Vector3(
                    settings.InteractionPromptScale,
                    settings.InteractionPromptScale,
                    1f);
                SpriteRenderer promptRenderer = promptObject.AddComponent<SpriteRenderer>();
                promptRenderer.hideFlags = PreviewHideFlags;
                promptRenderer.sortingOrder = 8;
                promptRenderer.enabled = false;

                CircleMapSegment segment = segmentObject.AddComponent<CircleMapSegment>();
                segment.hideFlags = PreviewHideFlags;
                segment.Setup(
                    spriteRenderer,
                    npcRenderer,
                    promptRenderer,
                    settings.NpcInteractionPromptSprite,
                    settings.EventInteractionPromptSprite,
                    settings.ResourceInteractionPromptSprite,
                    settings.InteractionPromptHorizontalOffset);
                CircleRoadSegmentData segmentData = new CircleRoadSegmentData(slotDefinition, null);
                segment.Show(segmentData);
                segment.SetInteractionPromptVisible(
                    segmentData,
                    slot.IsSelected && HasInteractionPrompt(segmentData));

                if (slot.IsSelected)
                {
                    selectedSegment = segment;
                    selectedSegmentTransform = segmentObject.transform;
                    selectedImageTransform = imageObject.transform;
                    selectedSpriteRenderer = spriteRenderer;
                }
            }

            HideSourceRenderers(settings.RotatingRoot);
            HideSourceRenderer(settings.BackgroundRenderer);
            previewCamera = settings.GameCamera;
            previewCameraTarget = settings.RotatingRoot.position;
            SyncSceneViewToGameCamera();
        }

        private static bool HasInteractionPrompt(CircleRoadSegmentData segment)
        {
            if (segment == null)
            {
                return false;
            }

            switch (segment.contentType)
            {
                case SegmentContentType.Npc:
                    return segment.dialogue != null;
                case SegmentContentType.Event:
                    return segment.gameEvent != null;
                case SegmentContentType.Resource:
                    return !string.IsNullOrWhiteSpace(segment.segmentId);
                default:
                    return false;
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

        private static void BuildPreviewBackground(
            RoadSegmentDefinition definition,
            SpriteRenderer sourceBackgroundRenderer)
        {
            if (sourceBackgroundRenderer == null)
            {
                return;
            }

            previewBackgroundObject = Object.Instantiate(
                sourceBackgroundRenderer.gameObject,
                sourceBackgroundRenderer.transform.parent);
            previewBackgroundObject.name = PreviewBackgroundName;
            SetPreviewHideFlagsRecursively(previewBackgroundObject);
            previewBackgroundObject.transform.localPosition =
                sourceBackgroundRenderer.transform.localPosition;
            previewBackgroundObject.transform.localRotation =
                sourceBackgroundRenderer.transform.localRotation;
            previewBackgroundObject.transform.localScale =
                definition.Season != null
                    ? Vector3.Scale(
                        sourceBackgroundRenderer.transform.localScale,
                        definition.Season.BackgroundScaleMultiplier)
                    : sourceBackgroundRenderer.transform.localScale;

            SpriteRenderer previewBackgroundRenderer =
                previewBackgroundObject.GetComponent<SpriteRenderer>();
            if (previewBackgroundRenderer != null &&
                definition.Season != null &&
                definition.Season.BackgroundSprite != null)
            {
                previewBackgroundRenderer.sprite = definition.Season.BackgroundSprite;
            }
        }

        private static GameObject CreatePreviewGameObject(string objectName)
        {
            GameObject previewObject = new GameObject(objectName);
            previewObject.hideFlags = PreviewHideFlags;
            previewObject.transform.hideFlags = PreviewHideFlags;
            return previewObject;
        }

        private static void SetPreviewHideFlagsRecursively(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                Transform transform = transforms[index];
                transform.gameObject.hideFlags = PreviewHideFlags;
                Component[] components = transform.GetComponents<Component>();
                for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
                {
                    if (components[componentIndex] != null)
                    {
                        components[componentIndex].hideFlags = PreviewHideFlags;
                    }
                }

                Renderer renderer = transform.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.forceRenderingOff = false;
                }
            }
        }

        private static void DestroyChildren(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            for (int childIndex = parent.childCount - 1; childIndex >= 0; childIndex--)
            {
                Object.DestroyImmediate(parent.GetChild(childIndex).gameObject);
            }
        }

        private static void HideSourceRenderers(Transform sourceRoot)
        {
            RestoreSourceRenderers();
            if (sourceRoot == null)
            {
                return;
            }

            Renderer[] renderers = sourceRoot.GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                if (renderer == null)
                {
                    continue;
                }

                hiddenSourceRenderers.Add(new RendererVisibilityState(
                    renderer,
                    renderer.forceRenderingOff));
                renderer.forceRenderingOff = true;
            }
        }

        private static void HideSourceRenderer(Renderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            hiddenSourceRenderers.Add(new RendererVisibilityState(
                renderer,
                renderer.forceRenderingOff));
            renderer.forceRenderingOff = true;
        }

        private static void RestoreSourceRenderers()
        {
            for (int index = 0; index < hiddenSourceRenderers.Count; index++)
            {
                RendererVisibilityState state = hiddenSourceRenderers[index];
                if (state.Renderer != null)
                {
                    state.Renderer.forceRenderingOff = state.WasForcedOff;
                }
            }

            hiddenSourceRenderers.Clear();
        }

        private static void SyncSceneViewToGameCamera(
            Camera gameCamera,
            Vector3 mapPlanePoint)
        {
            if (gameCamera == null || SceneView.lastActiveSceneView == null)
            {
                return;
            }

            SceneView sceneView = SceneView.lastActiveSceneView;
            Transform cameraTransform = gameCamera.transform;
            float distanceToMapPlane = Vector3.Dot(
                mapPlanePoint - cameraTransform.position,
                cameraTransform.forward);
            Vector3 pivot = cameraTransform.position +
                            cameraTransform.forward * distanceToMapPlane;
            float viewSize = gameCamera.orthographic
                ? gameCamera.orthographicSize
                : Mathf.Max(0.01f, Mathf.Abs(distanceToMapPlane));

            sceneView.orthographic = gameCamera.orthographic;
            sceneView.LookAtDirect(pivot, cameraTransform.rotation, viewSize);
            sceneView.Repaint();
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
            RestoreSourceRenderers();
            selectedSegment = null;
            selectedSegmentTransform = null;
            selectedImageTransform = null;
            selectedSpriteRenderer = null;
            previewCamera = null;
            previewCameraTarget = Vector3.zero;

            if (previewRoot != null)
            {
                Object.DestroyImmediate(previewRoot);
                previewRoot = null;
            }

            if (previewBackgroundObject != null)
            {
                Object.DestroyImmediate(previewBackgroundObject);
                previewBackgroundObject = null;
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
                    (previewObject.name == PreviewRootName ||
                     previewObject.name == PreviewBackgroundName) &&
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
                Transform rotatingRoot,
                int segmentParentSiblingIndex,
                int ringSiblingIndex,
                int totalRoadSegmentCount,
                int visibleSegmentCount,
                float radius,
                float segmentScale,
                float interactionPromptHorizontalOffset,
                float interactionPromptScale,
                Sprite npcInteractionPromptSprite,
                Sprite eventInteractionPromptSprite,
                Sprite resourceInteractionPromptSprite,
                Camera gameCamera,
                SpriteRenderer backgroundRenderer)
            {
                RotatingRoot = rotatingRoot;
                SegmentParentSiblingIndex = segmentParentSiblingIndex;
                RingSiblingIndex = ringSiblingIndex;
                TotalRoadSegmentCount = totalRoadSegmentCount;
                VisibleSegmentCount = visibleSegmentCount;
                Radius = radius;
                SegmentScale = segmentScale;
                InteractionPromptHorizontalOffset = interactionPromptHorizontalOffset;
                InteractionPromptScale = interactionPromptScale;
                NpcInteractionPromptSprite = npcInteractionPromptSprite;
                EventInteractionPromptSprite = eventInteractionPromptSprite;
                ResourceInteractionPromptSprite = resourceInteractionPromptSprite;
                GameCamera = gameCamera;
                BackgroundRenderer = backgroundRenderer;
            }

            public Transform RotatingRoot { get; }
            public int SegmentParentSiblingIndex { get; }
            public int RingSiblingIndex { get; }
            public int TotalRoadSegmentCount { get; }
            public int VisibleSegmentCount { get; }
            public float Radius { get; }
            public float SegmentScale { get; }
            public float InteractionPromptHorizontalOffset { get; }
            public float InteractionPromptScale { get; }
            public Sprite NpcInteractionPromptSprite { get; }
            public Sprite EventInteractionPromptSprite { get; }
            public Sprite ResourceInteractionPromptSprite { get; }
            public Camera GameCamera { get; }
            public SpriteRenderer BackgroundRenderer { get; }
        }

        private readonly struct RendererVisibilityState
        {
            public RendererVisibilityState(Renderer renderer, bool wasForcedOff)
            {
                Renderer = renderer;
                WasForcedOff = wasForcedOff;
            }

            public Renderer Renderer { get; }
            public bool WasForcedOff { get; }
        }
    }
}
