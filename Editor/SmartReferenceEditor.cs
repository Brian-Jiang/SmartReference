using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SmartReference.Editor {
    [CustomPropertyDrawer(typeof(SmartReference.Runtime.SmartReference), true)]
    public class SmartReferenceEditor: PropertyDrawer {
        private SerializedProperty cacheProperty;
        private Object referencedObject;
        private SerializedProperty guidProp;
        private SerializedProperty fileIDProp;
        private SerializedProperty pathProp;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if (!SerializedProperty.EqualContents(property, cacheProperty)) {
                cacheProperty = property;
                guidProp = property.FindPropertyRelative("guid");
                fileIDProp = property.FindPropertyRelative("fileID");
                pathProp = property.FindPropertyRelative("path");
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
            
            // var objectArray = property.serializedObject.targetObjects;
            // var sType = objectArray[0].GetType();
            // var oType = sType.GetField(property.propertyPath).FieldType.GenericTypeArguments[0];
            var type = Type.GetType(property.FindPropertyRelative("type").stringValue);
            // var type = GetNestedObjectType(property, property.propertyPath, property.depth);
            var referenced = EditorGUI.ObjectField(position, label, referencedObject, type, false);
            if (referencedObject != referenced) {
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(referenced, out var guid, out long fileID)) {
                    Debug.LogError($"[SmartReferenceEditor] failed to get guid and fileID, path: {AssetDatabase.GetAssetPath(referenced)}");
                    return;
                }
                
                guidProp.stringValue = guid;
                fileIDProp.longValue = fileID;
                
                // property.FindPropertyRelative("type").stringValue = oType.FullName;
                referencedObject = referenced;
            }
            
            pathProp.stringValue = AssetDatabase.GetAssetPath(referenced);

            // if (referencedObject != null) {
            //     EditorGUI.bo
            // }
        }
        
        // inner2.Array.data[0].sInner
        // private Type GetNestedObjectType(SerializedProperty property, string propertyPath, int depth, Type fieldType) {
        //     if (depth > 0) {
        //         var firstPropertyPath = propertyPath[..propertyPath.IndexOf('.')];
        //         var remainingPropertyPath = propertyPath[(propertyPath.IndexOf('.') + 1)..];
        //         
        //         if (firstPropertyPath == "Array") {
        //             Regex regex = new Regex(@"data\[(\d+)\]");
        //             Match match = regex.Match(remainingPropertyPath);
        //             if (!match.Success) return null;
        //             
        //             var elementIndex = int.Parse(match.Groups[1].Value);
        //             remainingPropertyPath = remainingPropertyPath[(remainingPropertyPath.IndexOf('.') + 1)..];
        //             var arrayElementProperty = property.GetArrayElementAtIndex(elementIndex);
        //             return GetNestedObjectType(arrayElementProperty, remainingPropertyPath, depth - 1);
        //         }
        //         
        //         
        //         var innerProperty = property.serializedObject.FindProperty(firstPropertyPath);
        //         return GetNestedObjectType(innerProperty, remainingPropertyPath, depth - 1, fieldType.GetField(firstPropertyPath).FieldType);
        //     }
        //     
        //     var objectArray = property.serializedObject.targetObjects;
        //     var sType = objectArray[0].GetType();
        //     var oType = sType.GetField(property.propertyPath).FieldType.GenericTypeArguments[0];
        //     return oType;
        // }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return base.GetPropertyHeight(property, label);
        }
        
        // public override VisualElement CreatePropertyGUI(SerializedProperty property) {
        //     return base.CreatePropertyGUI(property);
        // }
    }
}