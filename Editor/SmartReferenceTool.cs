using System;
using UnityEditor;
using Object = UnityEngine.Object;

namespace SmartReference.Editor
{
    public static class SmartReferenceTool
    {
        public static void SetReference(SerializedProperty smartReferenceProperty, Object obj)
        {
            if (smartReferenceProperty == null)
            {
                throw new ArgumentNullException(nameof(smartReferenceProperty));
            }
            
            if (obj == null)
            {
                ClearReference(smartReferenceProperty);
            }

            EnsureIsSmartReferenceProperty(smartReferenceProperty);
            
            var typeProp = smartReferenceProperty.FindPropertyRelative("type");
            var type = Type.GetType(typeProp.stringValue);
            if (obj.GetType() != type)
            {
                throw new ArgumentException($"Object type does not match SmartReference type. Expected: {type}, Actual: {obj.GetType()}");
            }

            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out long fileID))
            {
                throw new ArgumentException($"Object is not a valid asset with GUID/fileID: {obj}");
            }

            var path = AssetDatabase.GetAssetPath(obj);
            smartReferenceProperty.FindPropertyRelative("guid").stringValue = guid;
            smartReferenceProperty.FindPropertyRelative("fileID").longValue = fileID;
            smartReferenceProperty.FindPropertyRelative("path").stringValue = path;

            smartReferenceProperty.serializedObject.ApplyModifiedProperties();
        }
        
        /// <summary>
        /// Clears SmartReference fields (guid/fileID/path/type).
        /// </summary>
        public static void ClearReference(SerializedProperty smartReferenceProperty)
        {
            if (smartReferenceProperty == null)
                throw new ArgumentNullException(nameof(smartReferenceProperty));

            EnsureIsSmartReferenceProperty(smartReferenceProperty);

            smartReferenceProperty.FindPropertyRelative("guid").stringValue = string.Empty;
            smartReferenceProperty.FindPropertyRelative("fileID").longValue = 0;
            smartReferenceProperty.FindPropertyRelative("path").stringValue = string.Empty;
            smartReferenceProperty.FindPropertyRelative("type").stringValue = string.Empty;

            smartReferenceProperty.serializedObject.ApplyModifiedProperties();
        }
        
        private static void EnsureIsSmartReferenceProperty(SerializedProperty p)
        {
            // We check presence of the expected fields rather than type name, so it works with SmartReference<T>
            // stored in any container.
            if (p.propertyType != SerializedPropertyType.Generic)
                throw new ArgumentException($"Property is not a serialized object/struct: {p.propertyPath}");

            if (p.FindPropertyRelative("guid") == null ||
                p.FindPropertyRelative("fileID") == null ||
                p.FindPropertyRelative("path") == null ||
                p.FindPropertyRelative("type") == null)
            {
                throw new ArgumentException($"Property does not look like a SmartReference (missing guid/fileID/path/type): {p.propertyPath}");
            }
        }
    }
}