using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if UNITY_6000_0_OR_NEWER
using UnityEditor.Build.Profile;
#endif

namespace SmartReference.Editor
{
    [CustomPropertyDrawer(typeof(Runtime.SceneReference), true)]
    internal class SceneReferenceEditor: PropertyDrawer
    {
        private SerializedProperty cacheProperty;
        private SceneAsset currentScene;
        private SerializedProperty scenePathProp;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!SerializedProperty.EqualContents(property, cacheProperty)) {
                cacheProperty = property;
                scenePathProp = property.FindPropertyRelative("scenePath");
                currentScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePathProp.stringValue);
            }

            EditorGUI.BeginChangeCheck();
            var sceneRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var newScene = EditorGUI.ObjectField(sceneRect, property.displayName, currentScene, typeof(SceneAsset), false) as SceneAsset;
            if (EditorGUI.EndChangeCheck())
            {
                currentScene = newScene;
                var newPath = AssetDatabase.GetAssetPath(newScene);
                scenePathProp.stringValue = newPath;
            }

            var buildScenes = GetBuildScenes(out var listName);
            var scenePath = scenePathProp.stringValue;
            var sceneInBuildSettings = buildScenes.Any(t => t.path == scenePath);
            var sceneEnabled = buildScenes.Any(t => t.path == scenePath && t.enabled);
            if (!sceneInBuildSettings || !sceneEnabled) {
                var warningRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width,
                    EditorGUIUtility.singleLineHeight);
                if (!sceneInBuildSettings) {
                    EditorGUI.HelpBox(warningRect, $"Scene is not in {listName}", MessageType.Warning);
                }
                else {
                    EditorGUI.HelpBox(warningRect, $"Scene is not enabled in {listName}", MessageType.Warning);
                }

                var fixRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight * 2, position.width,
                    EditorGUIUtility.singleLineHeight);
                if (GUI.Button(fixRect, "Fix")) {
                    if (!sceneInBuildSettings) {
                        var newScenes = buildScenes.ToList();
                        newScenes.Add(new EditorBuildSettingsScene(scenePath, true));
                        SetBuildScenes(newScenes.ToArray());
                    }
                    else {
                        var newScenes = buildScenes.ToList();
                        var index = newScenes.FindIndex(t => t.path == scenePath);
                        newScenes[index] = new EditorBuildSettingsScene(scenePath, true);
                        SetBuildScenes(newScenes.ToArray());
                    }
                }
            } else {
                var tipRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width,
                    EditorGUIUtility.singleLineHeight);
                // Runtime build indices only count enabled scenes
                var index = buildScenes.Where(t => t.enabled).ToList().FindIndex(t => t.path == scenePath);
                EditorGUI.HelpBox(tipRect, $"Index in {listName}: {index}", MessageType.Info);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight;
            var scenePath = property.FindPropertyRelative("scenePath").stringValue;
            var buildScenes = GetBuildScenes(out _);
            var sceneInBuildSettings = buildScenes.Any(t => t.path == scenePath && t.enabled);
            if (!sceneInBuildSettings) {
                height += EditorGUIUtility.singleLineHeight * 2;
            } else {
                height += EditorGUIUtility.singleLineHeight;
            }

            return height;
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
