using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SmartReference.Editor
{
    [CustomPropertyDrawer(typeof(Runtime.SmartReference), true)]
    internal class SmartReferenceEditor: PropertyDrawer
    {
        private SerializedProperty cacheProperty;
        private Object referencedObject;
        private SerializedProperty guidProp;
        private SerializedProperty fileIDProp;
        private SerializedProperty pathProp;
        private SerializedProperty typeProp;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
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
            var newReferenced = EditorGUI.ObjectField(position, label, referencedObject, type, false);
            if (referencedObject != newReferenced) {
                referencedObject = newReferenced;
                if (newReferenced == null) {
                    SmartReferenceTool.ClearReference(property);
                }
                else
                {
                    SmartReferenceTool.SetReference(property, newReferenced);
                }
            }
        }
    }
}