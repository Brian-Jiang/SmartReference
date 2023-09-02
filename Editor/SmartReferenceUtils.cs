using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SmartReference.Editor {
    public static class SmartReferenceUtils {
        [MenuItem("SmartReference/Update All References", priority = 0)]
        public static void UpdateAllReferences() {
            EditorUtility.DisplayProgressBar("SmartReference", "Updating all references...", 0);

            try {
                var typeList = GetTypesWithSpecificField(typeof(Runtime.SmartReference));
                foreach (var type in typeList) {
                    var guids = AssetDatabase.FindAssets($"t:{type.Name}");
                    foreach (var guid in guids) {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        var asset = AssetDatabase.LoadAssetAtPath(path, type);
                        var serializedObject = new SerializedObject(asset);
                        var fields = type.GetFields(
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        foreach (var field in fields) {
                            if (!typeof(Runtime.SmartReference).IsAssignableFrom(field.FieldType)) continue;
                        
                            var property = serializedObject.FindProperty(field.Name);
                            UpdateReferenceWithProperty(property);
                        }
                    
                        serializedObject.ApplyModifiedProperties();
                    }
                }
            }
            finally {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
            }
        }

        public static void UpdateReference(this Runtime.SmartReference smartReference) {
            if (smartReference == null || string.IsNullOrEmpty(smartReference.guid)) return;

            var succeed = false;
            // Update path with guid and fileID
            var objects = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(smartReference.guid));
            foreach (var obj in objects) {
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out _, out long objFileID) && 
                    smartReference.fileID == objFileID) {
                    smartReference.path = AssetDatabase.GetAssetPath(obj);
                    succeed = true;
                    break;
                }
            }
            
            // if failed, try to update guid and fileID with path
            if (!succeed) {
                var obj = AssetDatabase.LoadAssetAtPath(smartReference.path, Type.GetType(smartReference.type));
                if (obj != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out long objFileID)) {
                    smartReference.guid = AssetDatabase.AssetPathToGUID(smartReference.path);
                    smartReference.fileID = objFileID;
                    Debug.LogWarning("[SmartReference] The object referenced is missing, try to update guid and fileID with path");
                    succeed = true;
                }
            }
            
            if (!succeed) {
                Debug.LogError($"[SmartReference] Failed to update smart reference, path: {smartReference.path}");
            }
        }

        internal static void UpdateReferenceWithProperty(SerializedProperty property) {
            if (property == null) return;

            var guidProp = property.FindPropertyRelative("guid");
            if (string.IsNullOrEmpty(guidProp.stringValue)) return;
            
            var fileIDProp = property.FindPropertyRelative("fileID");
            var pathProp = property.FindPropertyRelative("path");
            var typeProp = property.FindPropertyRelative("type");
            var succeed = false;
            // Update path with guid and fileID
            var objects = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guidProp.stringValue));
            foreach (var obj in objects) {
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out _, out long objFileID) && 
                    fileIDProp.longValue == objFileID) {
                    pathProp.stringValue = AssetDatabase.GetAssetPath(obj);
                    succeed = true;
                    break;
                }
            }
            
            // if failed, try to update guid and fileID with path
            if (!succeed) {
                var obj = AssetDatabase.LoadAssetAtPath(pathProp.stringValue, Type.GetType(typeProp.stringValue));
                if (obj != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out long objFileID)) {
                    guidProp.stringValue = AssetDatabase.AssetPathToGUID(pathProp.stringValue);
                    fileIDProp.longValue = objFileID;
                    Debug.LogWarning("[SmartReference] The object referenced is missing, try to update guid and fileID with path");
                    succeed = true;
                }
            }
            
            if (!succeed) {
                Debug.LogError($"[SmartReference] Failed to update smart reference, path: {pathProp.stringValue}");
            }
        }
        
        private static List<Type> GetTypesWithSpecificField(Type fieldType)
        {
            List<Type> result = new List<Type>();

            // Iterating over all assemblies
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Optional: Filter the assemblies by name if necessary
                var fullName = assembly.FullName;
                if (fullName.StartsWith("UnityEngine.") || fullName.StartsWith("UnityEditor.") ||
                    fullName.StartsWith("Unity.") || fullName.StartsWith("Bee.") ||
                    fullName.StartsWith("System.") || fullName.StartsWith("Mono.")) continue;

                foreach (Type type in assembly.GetTypes())
                {
                    // Safeguard against types that might throw exceptions
                    try
                    {
                        if (type.IsSubclassOf(typeof(UnityEngine.Object)))
                        {
                            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                            {
                                if (fieldType.IsAssignableFrom(field.FieldType))
                                {
                                    result.Add(type);
                                    break;
                                }
                            }
                        }
                    }
                    catch
                    {
                        // In some cases, attempting to inspect a type might throw an exception.
                        // Just catch and ignore.
                    }
                }
            }

            return result;
        }
    }
}