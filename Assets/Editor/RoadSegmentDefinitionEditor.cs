using UnityEditor;
using UnityEngine;

namespace CircleWar.EditorTools
{
    [CustomEditor(typeof(RoadSegmentDefinition))]
    public sealed class RoadSegmentDefinitionEditor : UnityEditor.Editor
    {
        private void OnEnable()
        {
            EditorApplication.delayCall += EnsurePreviewAfterEnable;
        }

        private void OnDisable()
        {
            EditorApplication.delayCall -= EnsurePreviewAfterEnable;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EnsureLegacyMapSpriteLayers(serializedObject);
            EditorGUI.BeginChangeCheck();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            bool definitionChanged = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();

            RoadSegmentDefinition definition = target as RoadSegmentDefinition;
            if (definitionChanged &&
                definition != null &&
                RoadSegmentScenePreview.IsEnabled)
            {
                RoadSegmentScenePreview.Rebuild(definition);
            }

            DrawPreviewControls(definition);
        }

        private static void EnsureLegacyMapSpriteLayers(SerializedObject serializedDefinition)
        {
            SerializedProperty layersProperty = serializedDefinition.FindProperty("mapSpriteLayers");
            if (layersProperty == null || layersProperty.arraySize > 0)
            {
                return;
            }

            SerializedProperty mapSpriteProperty = serializedDefinition.FindProperty("mapSprite");
            SerializedProperty additionalSpritesProperty =
                serializedDefinition.FindProperty("additionalMapSprites");
            SerializedProperty yProperty = serializedDefinition.FindProperty("y");
            SerializedProperty zProperty = serializedDefinition.FindProperty("z");

            int migratedLayerCount = 0;
            if (mapSpriteProperty != null && mapSpriteProperty.objectReferenceValue != null)
            {
                AddLegacyMapSpriteLayer(
                    layersProperty,
                    migratedLayerCount,
                    mapSpriteProperty.objectReferenceValue,
                    yProperty,
                    zProperty);
                migratedLayerCount++;
            }

            if (additionalSpritesProperty != null)
            {
                for (int index = 0; index < additionalSpritesProperty.arraySize; index++)
                {
                    Object sprite = additionalSpritesProperty
                        .GetArrayElementAtIndex(index)
                        .objectReferenceValue;
                    if (sprite == null)
                    {
                        continue;
                    }

                    AddLegacyMapSpriteLayer(
                        layersProperty,
                        migratedLayerCount,
                        sprite,
                        yProperty,
                        zProperty);
                    migratedLayerCount++;
                }
            }

            if (migratedLayerCount > 0)
            {
                serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void AddLegacyMapSpriteLayer(
            SerializedProperty layersProperty,
            int layerIndex,
            Object sprite,
            SerializedProperty yProperty,
            SerializedProperty zProperty)
        {
            layersProperty.arraySize = layerIndex + 1;
            SerializedProperty layerProperty = layersProperty.GetArrayElementAtIndex(layerIndex);
            layerProperty.FindPropertyRelative("sprite").objectReferenceValue = sprite;
            layerProperty.FindPropertyRelative("offset").vector2Value = new Vector2(
                0f,
                yProperty != null ? yProperty.floatValue : 0f);
            layerProperty.FindPropertyRelative("scale").vector2Value = Vector2.one;
            layerProperty.FindPropertyRelative("z").floatValue =
                zProperty != null ? zProperty.floatValue : 0f;
        }

        public override bool RequiresConstantRepaint()
        {
            return RoadSegmentScenePreview.IsEnabled;
        }

        private static void DrawPreviewControls(RoadSegmentDefinition definition)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("MapSprite Scene 预览", EditorStyles.boldLabel);

            bool previewEnabled = RoadSegmentScenePreview.IsEnabled;
            bool requestedEnabled = EditorGUILayout.ToggleLeft(
                "启用当前节点和周边节点预览",
                previewEnabled);
            if (requestedEnabled != previewEnabled)
            {
                RoadSegmentScenePreview.SetEnabled(requestedEnabled);
                previewEnabled = requestedEnabled;
            }

            if (!previewEnabled)
            {
                EditorGUILayout.HelpBox(
                    "开启后无需进入 Play Mode；修改 Map Sprite Layers 会立即刷新 Scene 视图。",
                    MessageType.Info);
                return;
            }

            RoadSegmentScenePreview.EnsurePreview(definition);
            EditorGUILayout.HelpBox(
                RoadSegmentScenePreview.StatusMessage,
                GetMessageType(RoadSegmentScenePreview.StatusKind));

            if (RoadSegmentScenePreview.StatusKind == RoadSegmentPreviewStatusKind.Ready)
            {
                EditorGUILayout.LabelField("绿色箭头", "调整第一层 Map Sprite 的 Offset Y");
                EditorGUILayout.LabelField("橙色圆环", "调整第一层 Map Sprite 的 Z Rotation");
                EditorGUILayout.LabelField("其它层", "在 Map Sprite Layers 列表里单独调整 Offset / Scale / Z");

                if (GUILayout.Button("重新同步游戏相机"))
                {
                    RoadSegmentScenePreview.SyncSceneViewToGameCamera();
                }

                if (GUILayout.Button("重新生成预览"))
                {
                    RoadSegmentScenePreview.Rebuild(definition);
                }
            }
        }

        private void EnsurePreviewAfterEnable()
        {
            RoadSegmentDefinition definition = target as RoadSegmentDefinition;
            if (definition != null && RoadSegmentScenePreview.IsEnabled)
            {
                RoadSegmentScenePreview.EnsurePreview(definition);
            }
        }

        private static MessageType GetMessageType(RoadSegmentPreviewStatusKind statusKind)
        {
            switch (statusKind)
            {
                case RoadSegmentPreviewStatusKind.Ready:
                    return MessageType.Info;
                case RoadSegmentPreviewStatusKind.Disabled:
                    return MessageType.None;
                default:
                    return MessageType.Warning;
            }
        }
    }
}
