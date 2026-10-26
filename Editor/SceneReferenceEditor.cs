using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if UNITY_2022_2_OR_NEWER
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#endif
#if UNITY_6000_0_OR_NEWER
using UnityEditor.Build.Profile;
#endif

namespace SmartReference.Editor
{
    [CustomPropertyDrawer(typeof(Runtime.SceneReference), true)]
    internal class SceneReferenceEditor: PropertyDrawer
    {
#if UNITY_2022_2_OR_NEWER
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var scenePathProp = property.FindPropertyRelative("scenePath");

            var root = new VisualElement();
            var field = new ObjectField(SmartReferenceUtils.GetLabel(property, preferredLabel)) {
                objectType = typeof(SceneAsset),
                allowSceneObjects = false,
            };
            field.AddToClassList(ObjectField.alignedFieldUssClassName);
            var statusBox = new HelpBox();
            var fixButton = new Button { text = "Fix" };
            root.Add(field);
            root.Add(statusBox);
            root.Add(fixButton);

            void RefreshStatus()
            {
                var hasStatus = GetSceneStatus(scenePathProp.stringValue, out var message, out var fixable);
                var showStatus = hasStatus && !property.hasMultipleDifferentValues;
                statusBox.style.display = showStatus ? DisplayStyle.Flex : DisplayStyle.None;
                fixButton.style.display = showStatus && fixable ? DisplayStyle.Flex : DisplayStyle.None;
                if (!showStatus) return;

                statusBox.text = message;
                statusBox.messageType = fixable ? HelpBoxMessageType.Warning : HelpBoxMessageType.Info;
            }

            void Refresh()
            {
                field.SetValueWithoutNotify(AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePathProp.stringValue));
                DrawerUtils.UpdatePropertyStyle(field, property);
                RefreshStatus();
            }

            Refresh();
            
            field.TrackPropertyValue(property, _ => Refresh());
            field.RegisterValueChangedCallback(evt => {
                scenePathProp.stringValue = AssetDatabase.GetAssetPath(evt.newValue);
                property.serializedObject.ApplyModifiedProperties();
                Refresh();
            });
            DrawerUtils.AddPrefabOverrideMenu(field, property);
            DrawerUtils.AddPrefabOverrideBar(field, property);
            fixButton.clicked += () => {
                FixScene(scenePathProp.stringValue);
                RefreshStatus();
            };

            // Build profile scene lists have no change event, so poll as well. Scheduled items pause while detached.
            Action onSceneListChanged = RefreshStatus;
            root.RegisterCallback<AttachToPanelEvent>(_ => EditorBuildSettings.sceneListChanged += onSceneListChanged);
            root.RegisterCallback<DetachFromPanelEvent>(_ => EditorBuildSettings.sceneListChanged -= onSceneListChanged);
            root.schedule.Execute(RefreshStatus).Every(1000);
            return root;
        }
#endif

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var scenePathProp = property.FindPropertyRelative("scenePath");
            var sceneRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            label = EditorGUI.BeginProperty(sceneRect, label, property);
            label.text = SmartReferenceUtils.GetLabel(property, label.text);
            var currentScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePathProp.stringValue);
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            var newScene = EditorGUI.ObjectField(sceneRect, label, currentScene, typeof(SceneAsset), false) as SceneAsset;
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck()) {
                scenePathProp.stringValue = AssetDatabase.GetAssetPath(newScene);
            }
            EditorGUI.EndProperty();

            var hasStatus = GetSceneStatus(scenePathProp.stringValue, out var message, out var fixable);
            if (!hasStatus || property.hasMultipleDifferentValues) return;

            var statusRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width,
                EditorGUIUtility.singleLineHeight);
            EditorGUI.HelpBox(statusRect, message, fixable ? MessageType.Warning : MessageType.Info);
            if (!fixable) return;

            var fixRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight * 2, position.width,
                EditorGUIUtility.singleLineHeight);
            if (GUI.Button(fixRect, "Fix")) {
                FixScene(scenePathProp.stringValue);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight;
            var hasStatus = GetSceneStatus(property.FindPropertyRelative("scenePath").stringValue, out _, out var fixable);
            if (!hasStatus || property.hasMultipleDifferentValues) return height;

            return height + EditorGUIUtility.singleLineHeight * (fixable ? 2 : 1);
        }
        
        private static bool GetSceneStatus(string scenePath, out string message, out bool fixable)
        {
            message = null;
            fixable = false;
            if (string.IsNullOrEmpty(scenePath)) return false;

            var buildScenes = GetBuildScenes(out var listName);
            var sceneInBuildSettings = buildScenes.Any(t => t.path == scenePath);
            var sceneEnabled = buildScenes.Any(t => t.path == scenePath && t.enabled);
            if (!sceneInBuildSettings) {
                message = $"Scene is not in {listName}";
                fixable = true;
            }
            else if (!sceneEnabled) {
                message = $"Scene is not enabled in {listName}";
                fixable = true;
            }
            else {
                // Runtime build indices only count enabled scenes
                var index = buildScenes.Where(t => t.enabled).ToList().FindIndex(t => t.path == scenePath);
                message = $"Index in {listName}: {index}";
            }

            return true;
        }

        private static void FixScene(string scenePath)
        {
            var newScenes = GetBuildScenes(out _).ToList();
            var index = newScenes.FindIndex(t => t.path == scenePath);
            if (index < 0) {
                newScenes.Add(new EditorBuildSettingsScene(scenePath, true));
            }
            else {
                newScenes[index] = new EditorBuildSettingsScene(scenePath, true);
            }
            SetBuildScenes(newScenes.ToArray());
        }

        private static EditorBuildSettingsScene[] GetBuildScenes(out string listName)
        {
#if UNITY_6000_0_OR_NEWER
            var profile = BuildProfile.GetActiveBuildProfile();
            if (profile != null && profile.overrideGlobalScenes) {
                listName = $"build profile '{profile.name}'";
                return profile.scenes ?? Array.Empty<EditorBuildSettingsScene>();
            }
#endif
            listName = "build settings";
            return EditorBuildSettings.scenes;
        }

        private static void SetBuildScenes(EditorBuildSettingsScene[] scenes)
        {
#if UNITY_6000_0_OR_NEWER
            var profile = BuildProfile.GetActiveBuildProfile();
            if (profile != null && profile.overrideGlobalScenes) {
                profile.scenes = scenes;
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssetIfDirty(profile);
                return;
            }
#endif
            EditorBuildSettings.scenes = scenes;
        }
    }
}
