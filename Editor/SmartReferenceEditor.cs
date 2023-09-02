using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SmartReference.Editor {
    [CustomPropertyDrawer(typeof(Runtime.SmartReference), true)]
    internal class SmartReferenceEditor: PropertyDrawer {
        private SerializedProperty cacheProperty;
        private Object referencedObject;
        private SerializedProperty guidProp;
        private SerializedProperty fileIDProp;
        private SerializedProperty pathProp;
        private SerializedProperty typeProp;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if (!SerializedProperty.EqualContents(property, cacheProperty)) {
                cacheProperty = property;
                referencedObject = null;
                guidProp = property.FindPropertyRelative("guid");
                fileIDProp = property.FindPropertyRelative("fileID");
                pathProp = property.FindPropertyRelative("path");
                typeProp = property.FindPropertyRelative("type");
                SmartReferenceUtils.UpdateReferenceWithProperty(property);
                if (!string.IsNullOrEmpty(guidProp.stringValue)) {
                    var guid = guidProp.stringValue;
                    var fileID = fileIDProp.longValue;
                    
                    var objects = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid));
                    foreach (var obj in objects) {
                        var succeed = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out _, out long objFileID);
                        if (succeed && fileID == objFileID) {
                            referencedObject = obj;
                            break;
                        }
                    }
                }
            }
            
            var type = Type.GetType(typeProp.stringValue);
            var referenced = EditorGUI.ObjectField(position, label, referencedObject, type, false);
            if (referencedObject != referenced) {
                if (referenced == null) {
                    guidProp.stringValue = string.Empty;
                    fileIDProp.longValue = 0;
                    pathProp.stringValue = string.Empty;
                    return;
                }
                
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(referenced, out var guid, out long fileID)) {
                    Debug.LogError(
                        $"[SmartReferenceEditor] Failed to get guid and fileID, path: {AssetDatabase.GetAssetPath(referenced)}");
                    return;
                }
                
                guidProp.stringValue = guid;
                fileIDProp.longValue = fileID;
                pathProp.stringValue = AssetDatabase.GetAssetPath(referenced);
                
                referencedObject = referenced;
            }
        }
    }
}