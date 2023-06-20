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
            var newScene = EditorGUI.ObjectField(position, property.displayName, currentScene, typeof(SceneAsset), false) as SceneAsset;
            if (EditorGUI.EndChangeCheck())
            {
                currentScene = newScene;
                var newPath = AssetDatabase.GetAssetPath(newScene);
                scenePathProp.stringValue = newPath;
            }
        }
    }
}