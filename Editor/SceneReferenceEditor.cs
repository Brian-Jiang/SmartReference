using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SmartReference.Editor {
    [CustomPropertyDrawer(typeof(Runtime.SceneReference), true)]
    internal class SceneReferenceEditor: PropertyDrawer {
        private SerializedProperty cacheProperty;
        private SceneAsset currentScene;
        private SerializedProperty scenePathProp;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
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

            var buildSettingsScenes = EditorBuildSettings.scenes;
            var scenePath = scenePathProp.stringValue;
            var sceneInBuildSettings = buildSettingsScenes.Any(t => t.path == scenePath);
            var sceneEnabled = buildSettingsScenes.Any(t => t.path == scenePath && t.enabled);
            if (!sceneInBuildSettings || !sceneEnabled) {
                var warningRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, 
                    EditorGUIUtility.singleLineHeight);
                if (!sceneInBuildSettings) {
                    EditorGUI.HelpBox(warningRect, "Scene is not in build settings", MessageType.Warning);
                }
                else {
                    EditorGUI.HelpBox(warningRect, "Scene is not enabled in build settings", MessageType.Warning);
                }
                
                var fixRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight * 2, position.width, 
                    EditorGUIUtility.singleLineHeight);
                if (GUI.Button(fixRect, "Fix")) {
                    if (!sceneInBuildSettings) {
                        var newScenes = buildSettingsScenes.ToList();
                        newScenes.Add(new EditorBuildSettingsScene(scenePath, true));
                        EditorBuildSettings.scenes = newScenes.ToArray();
                    }
                    else {
                        var newScenes = buildSettingsScenes.ToList();
                        var index = newScenes.FindIndex(t => t.path == scenePath);
                        newScenes[index] = new EditorBuildSettingsScene(scenePath, true);
                        EditorBuildSettings.scenes = newScenes.ToArray();
                    }
                }
            } else {
                var tipRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, 
                    EditorGUIUtility.singleLineHeight);
                var newScenes = buildSettingsScenes.ToList();
                var index = newScenes.FindIndex(t => t.path == scenePath);
                EditorGUI.HelpBox(tipRect, $"Index in build setting: {index}", MessageType.Info);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            var height = EditorGUIUtility.singleLineHeight;
            var scenePath = property.FindPropertyRelative("scenePath").stringValue;
            var buildSettingsScenes = EditorBuildSettings.scenes;
            var sceneInBuildSettings = buildSettingsScenes.Any(t => t.path == scenePath && t.enabled);
            if (!sceneInBuildSettings) {
                height += EditorGUIUtility.singleLineHeight * 2;
            } else {
                height += EditorGUIUtility.singleLineHeight;
            }
            
            return height;
        }
    }
}