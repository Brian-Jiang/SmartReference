using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SmartReference.Editor
{
    public static class SmartReferenceUtils
    {
        /// <summary>
        /// Update the path, or guid and fileID, of every SmartReference in assets, prefabs and scenes under Assets/.
        /// </summary>
        [MenuItem("Tools/SmartReference/Update All References", priority = 100)]
        public static void UpdateAllReferences()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                Debug.LogWarning("[SmartReference] Update All References is not available in play mode.");
                return;
            }

            var paths = new List<string>();
            foreach (var path in AssetDatabase.GetAllAssetPaths()) {
                if (!path.StartsWith("Assets/")) continue;
                if (!path.EndsWith(".asset") && !path.EndsWith(".prefab") && !path.EndsWith(".unity")) continue;
                paths.Add(path);
            }

            UpdateReferencesInAssets(paths);
        }
        
        internal static void UpdateReferencesInAssets(IReadOnlyList<string> paths)
        {
            // Only assets that use a script able to hold a SmartReference need to be opened
            var scriptPaths = GetCandidateScriptPaths();
            if (scriptPaths.Count == 0) return;

            try
            {
                for (var i = 0; i < paths.Count; i++) {
                    var path = paths[i];
                    EditorUtility.DisplayProgressBar("SmartReference", $"Updating references in {path}", (float) i / paths.Count);
                    if (!UsesAnyScript(path, scriptPaths)) continue;

                    if (path.EndsWith(".prefab")) {
                        UpdatePrefab(path);
                    }
                    else if (path.EndsWith(".unity")) {
                        UpdateScene(path);
                    }
                    else {
                        UpdateAsset(path);
                    }
                }
            }
            finally {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// Update every SmartReference in a serialized object, including nested fields, lists, arrays and managed references.
        /// </summary>
        /// <returns>True if any reference changed.</returns>
        internal static bool UpdateReferences(SerializedObject serializedObject, string assetPath = null)
        {
            var changed = false;
            var property = serializedObject.GetIterator();
            var enterChildren = true;
            while (property.Next(enterChildren)) {
                enterChildren = property.propertyType == SerializedPropertyType.Generic ||
                                property.propertyType == SerializedPropertyType.ManagedReference;
                if (!IsSmartReferenceProperty(property)) continue;

                enterChildren = false;
                
                if (property.isInstantiatedPrefab && !property.prefabOverride) continue;

                if (UpdateReferenceWithProperty(property, assetPath)) {
                    changed = true;
                }
            }

            if (changed) {
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

            return changed;
        }

        /// <summary>
        /// Check if a property looks like a SmartReference.
        /// </summary>
        internal static bool IsSmartReferenceProperty(SerializedProperty property)
        {
            return property.propertyType == SerializedPropertyType.Generic &&
                   property.FindPropertyRelative("guid") != null &&
                   property.FindPropertyRelative("fileID") != null &&
                   property.FindPropertyRelative("path") != null &&
                   property.FindPropertyRelative("type") != null;
        }

        private static void UpdateAsset(string path)
        {
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path)) {
                if (UpdateObject(obj, path)) {
                    EditorUtility.SetDirty(obj);
                }
            }
        }

        private static void UpdatePrefab(string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try {
                if (UpdateGameObject(root, path)) {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
            }
            finally {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void UpdateScene(string path)
        {
            var scene = SceneManager.GetSceneByPath(path);
            var wasInHierarchy = scene.IsValid();
            var wasLoaded = wasInHierarchy && scene.isLoaded;
            var wasDirty = wasLoaded && scene.isDirty;
            if (!wasLoaded) {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            }

            try {
                var changed = false;
                foreach (var root in scene.GetRootGameObjects()) {
                    if (UpdateGameObject(root, path)) {
                        changed = true;
                    }
                }

                if (!changed) return;

                EditorSceneManager.MarkSceneDirty(scene);
                if (wasDirty) {
                    // Saving would also save the user's unsaved changes
                    Debug.LogWarning($"[SmartReference] Updated references in a scene with unsaved changes, save it to keep them: {path}");
                    return;
                }

                EditorSceneManager.SaveScene(scene);
            }
            finally {
                if (!wasLoaded) {
                    EditorSceneManager.CloseScene(scene, !wasInHierarchy);
                }
            }
        }

        private static bool UpdateGameObject(GameObject gameObject, string assetPath)
        {
            var changed = false;
            foreach (var component in gameObject.GetComponentsInChildren<MonoBehaviour>(true)) {
                if (UpdateObject(component, assetPath)) {
                    changed = true;
                }
            }

            return changed;
        }

        private static bool UpdateObject(Object obj, string assetPath)
        {
            if (obj == null || !(obj is MonoBehaviour || obj is ScriptableObject)) return false;

            using var serializedObject = new SerializedObject(obj);
            return UpdateReferences(serializedObject, assetPath);
        }

        private static bool UsesAnyScript(string path, HashSet<string> scriptPaths)
        {
            foreach (var dependency in AssetDatabase.GetDependencies(path, true)) {
                if (scriptPaths.Contains(dependency)) return true;
            }

            return false;
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

        /// <returns>True if the property changed.</returns>
        internal static bool UpdateReferenceWithProperty(SerializedProperty property, string assetPath = null)
        {
            if (property == null) return false;

            var guidProp = property.FindPropertyRelative("guid");
            if (string.IsNullOrEmpty(guidProp.stringValue)) return false;

            var fileIDProp = property.FindPropertyRelative("fileID");
            var pathProp = property.FindPropertyRelative("path");
            var typeProp = property.FindPropertyRelative("type");

            // Update path with guid and fileID
            var found = FindAsset(guidProp.stringValue, fileIDProp.longValue);
            if (found != null) {
                var foundPath = AssetDatabase.GetAssetPath(found);
                if (pathProp.stringValue == foundPath) return false;

                pathProp.stringValue = foundPath;
                return true;
            }

            // if failed, try to update guid and fileID with path
            var obj = AssetDatabase.LoadAssetAtPath(pathProp.stringValue, GetTypeOrObject(typeProp.stringValue));
            if (obj != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out long objFileID)) {
                guidProp.stringValue = guid;
                fileIDProp.longValue = objFileID;
                Debug.LogWarning($"[SmartReference] The object referenced is missing, try to update guid and fileID with path: {pathProp.stringValue}");
                return true;
            }

            var location = string.IsNullOrEmpty(assetPath) ? property.propertyPath : $"{assetPath} ({property.propertyPath})";
            Debug.LogError($"[SmartReference] Failed to update smart reference, path: {pathProp.stringValue}, in {location}");
            return false;
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
        
        internal static HashSet<string> GetCandidateScriptPaths()
        {
            var result = new HashSet<string>();
            var cache = new Dictionary<Type, bool>();
            foreach (var script in MonoImporter.GetAllRuntimeMonoScripts()) {
                var type = script == null ? null : script.GetClass();
                if (type == null) continue;
                if (!typeof(MonoBehaviour).IsAssignableFrom(type) && !typeof(ScriptableObject).IsAssignableFrom(type)) continue;

                var fullName = type.Assembly.FullName;
                if (fullName.StartsWith("UnityEngine.") || fullName.StartsWith("UnityEditor.") ||
                    fullName.StartsWith("Unity.") || fullName.StartsWith("Bee.") ||
                    fullName.StartsWith("System.") || fullName.StartsWith("Mono.")) continue;

                try {
                    if (!CanContainSmartReference(type, cache)) continue;
                }
                catch {
                    continue;
                }

                var path = AssetDatabase.GetAssetPath(script);
                if (!string.IsNullOrEmpty(path)) {
                    result.Add(path);
                }
            }

            return result;
        }
        
        private static bool CanContainSmartReference(Type type, Dictionary<Type, bool> cache)
        {
            if (cache.TryGetValue(type, out var cached)) return cached;

            cache[type] = false;
            var result = false;
            for (var t = type; t != null && !IsUnityBaseType(t) && !result; t = t.BaseType) {
                foreach (var field in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)) {
                    // A managed reference can hold any type
                    if (field.IsDefined(typeof(SerializeReference), false)) {
                        result = true;
                        break;
                    }

                    if (field.IsNotSerialized || (!field.IsPublic && !field.IsDefined(typeof(SerializeField), false))) continue;

                    if (CanFieldTypeContainSmartReference(field.FieldType, cache)) {
                        result = true;
                        break;
                    }
                }
            }

            cache[type] = result;
            return result;
        }

        private static bool CanFieldTypeContainSmartReference(Type fieldType, Dictionary<Type, bool> cache)
        {
            if (fieldType.IsArray) {
                fieldType = fieldType.GetElementType();
            }
            else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>)) {
                fieldType = fieldType.GetGenericArguments()[0];
            }

            if (fieldType == null) return false;
            if (typeof(Runtime.SmartReference).IsAssignableFrom(fieldType)) return true;
            if (fieldType.IsPrimitive || fieldType.IsEnum || fieldType == typeof(string)) return false;
            // Object references are separate assets or components, scanned on their own
            if (typeof(Object).IsAssignableFrom(fieldType)) return false;
            if (!fieldType.IsSerializable) return false;

            return CanContainSmartReference(fieldType, cache);
        }

        private static bool IsUnityBaseType(Type type)
        {
            return type == typeof(object) || type == typeof(Object) || type == typeof(MonoBehaviour) ||
                   type == typeof(ScriptableObject);
        }
    }
}