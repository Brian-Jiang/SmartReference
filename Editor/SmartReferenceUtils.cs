using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SmartReference.Editor
{
    public static class SmartReferenceUtils
    {
        [MenuItem("Tools/SmartReference/Update All References", priority = 100)]
        public static void UpdateAllReferences()
        {
            EditorUtility.DisplayProgressBar("SmartReference", "Updating all references...", 0);

            try
            {
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

        public static void UpdateReference(this Runtime.SmartReference smartReference)
        {
            if (smartReference == null || string.IsNullOrEmpty(smartReference.guid)) return;

            var succeed = false;
            
            // Update path with guid and fileID
            var found = FindAsset(smartReference.guid, smartReference.fileID);
            if (found != null) {
                smartReference.path = AssetDatabase.GetAssetPath(found);
                succeed = true;
            }

            // if failed, try to update guid and fileID with path
            if (!succeed) {
                var obj = AssetDatabase.LoadAssetAtPath(smartReference.path, GetTypeOrObject(smartReference.type));
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

        internal static void UpdateReferenceWithProperty(SerializedProperty property)
        {
            if (property == null) return;

            var guidProp = property.FindPropertyRelative("guid");
            if (string.IsNullOrEmpty(guidProp.stringValue)) return;
            
            var fileIDProp = property.FindPropertyRelative("fileID");
            var pathProp = property.FindPropertyRelative("path");
            var typeProp = property.FindPropertyRelative("type");
            var succeed = false;
            
            // Update path with guid and fileID
            var found = FindAsset(guidProp.stringValue, fileIDProp.longValue);
            if (found != null) {
                pathProp.stringValue = AssetDatabase.GetAssetPath(found);
                succeed = true;
            }

            // if failed, try to update guid and fileID with path
            if (!succeed) {
                var obj = AssetDatabase.LoadAssetAtPath(pathProp.stringValue, GetTypeOrObject(typeProp.stringValue));
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

        private static readonly Dictionary<(string guid, long fileID), Object> AssetCache = new();

        [InitializeOnLoadMethod]
        private static void RegisterAssetCacheInvalidation()
        {
            EditorApplication.projectChanged += AssetCache.Clear;
        }
        
        internal static Object FindAsset(string guid, long fileID)
        {
            if (string.IsNullOrEmpty(guid)) return null;

            var key = (guid, fileID);
            // A cached object can be destroyed without a project change, e.g. a deleted sub asset
            if (AssetCache.TryGetValue(key, out var cached) && (cached != null || ReferenceEquals(cached, null))) {
                return cached;
            }

            Object result = null;
            var objects = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid));
            foreach (var obj in objects) {
                if (obj != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out _, out long objFileID) &&
                    fileID == objFileID) {
                    result = obj;
                    break;
                }
            }

            AssetCache[key] = result;
            return result;
        }
        
        internal static Object ResolveReference(SerializedProperty property)
        {
            var guid = property.FindPropertyRelative("guid").stringValue;
            if (string.IsNullOrEmpty(guid)) return null;

            var obj = FindAsset(guid, property.FindPropertyRelative("fileID").longValue);
            if (obj != null) return obj;

            var path = property.FindPropertyRelative("path").stringValue;
            if (string.IsNullOrEmpty(path)) return null;

            return AssetDatabase.LoadAssetAtPath(path, GetTypeOrObject(property.FindPropertyRelative("type").stringValue));
        }
        
        internal static Type GetReferencedType(FieldInfo fieldInfo, SerializedProperty property)
        {
            var fieldType = fieldInfo?.FieldType;
            if (fieldType != null) {
                if (fieldType.IsArray) {
                    fieldType = fieldType.GetElementType();
                }
                else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>)) {
                    fieldType = fieldType.GetGenericArguments()[0];
                }

                for (var t = fieldType; t != null; t = t.BaseType) {
                    if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Runtime.SmartReference<>)) {
                        return t.GetGenericArguments()[0];
                    }
                }
            }

            return GetTypeOrObject(property.FindPropertyRelative("type").stringValue);
        }
        
        internal static string GetLabel(SerializedProperty property, string label)
        {
            var path = property.propertyPath;
            if (!path.EndsWith("]")) return label;

            var start = path.LastIndexOf('[') + 1;
            return $"Element {path.Substring(start, path.Length - start - 1)}";
        }

        private static Type GetTypeOrObject(string typeName)
        {
            return (string.IsNullOrEmpty(typeName) ? null : Type.GetType(typeName)) ?? typeof(Object);
        }

        private static List<Type> GetTypesWithSpecificField(Type fieldType)
        {
            List<Type> result = new List<Type>();

            // Iterating over all UnityEngine.Object subclasses known to the editor
            foreach (var type in TypeCache.GetTypesDerivedFrom<UnityEngine.Object>())
            {
                // Optional: Filter the assemblies by name if necessary
                var fullName = type.Assembly.FullName;
                if (fullName.StartsWith("UnityEngine.") || fullName.StartsWith("UnityEditor.") ||
                    fullName.StartsWith("Unity.") || fullName.StartsWith("Bee.") ||
                    fullName.StartsWith("System.") || fullName.StartsWith("Mono.")) continue;

                // Safeguard against types that might throw exceptions
                try
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
                catch
                {
                    // In some cases, attempting to inspect a type might throw an exception.
                    // Just catch and ignore.
                }
            }

            return result;
        }
    }
}