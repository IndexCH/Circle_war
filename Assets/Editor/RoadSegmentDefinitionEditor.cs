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
                    "开启后无需进入 Play Mode；修改 MapSprite、Y、Z 会立即刷新 Scene 视图。",
                    MessageType.Info);
                return;
            }

            RoadSegmentScenePreview.EnsurePreview(definition);
            EditorGUILayout.HelpBox(
                RoadSegmentScenePreview.StatusMessage,
                GetMessageType(RoadSegmentScenePreview.StatusKind));

            if (RoadSegmentScenePreview.StatusKind == RoadSegmentPreviewStatusKind.Ready)
            {
                EditorGUILayout.LabelField("绿色箭头", "沿圆环径向调整 Y");
                EditorGUILayout.LabelField("橙色圆环", "绕本地 Z 轴调整角度");

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
