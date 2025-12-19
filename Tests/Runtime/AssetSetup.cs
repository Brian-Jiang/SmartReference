using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace SmartReference.Runtime.Tests
{
    /// <summary>
    /// Creates test assets BEFORE entering Play Mode (but still part of the test pipeline),
    /// and cleans them up afterward.
    /// </summary>
    public static class AssetSetup
    {
        public const string RootFolder = "Assets/SmartReference_PlayModeTestAssets";
        public const string ResourcesFolder = RootFolder + "/Resources";
        public const string ResourcesSubFolder = ResourcesFolder + "/SmartReferenceTest";

        public const string SoAssetPath = ResourcesSubFolder + "/TestSO.asset";
        public const string TextureAssetPath = ResourcesSubFolder + "/TestTex.asset";
        public const string MaterialAssetPath = ResourcesSubFolder + "/TestMat.mat";
        public const string PrefabAssetPath = ResourcesSubFolder + "/TestPrefab.prefab";

        // Resources.Load path (relative to any Resources folder)
        public const string SoResourcesPath = "SmartReferenceTest/TestSO";
        public const string TextureResourcesPath = "SmartReferenceTest/TestTex";
        public const string MaterialResourcesPath = "SmartReferenceTest/TestMat";
        public const string PrefabResourcesPath = "SmartReferenceTest/TestPrefab";

        public static void Setup()
        {
#if UNITY_EDITOR
            EnsureFolders();

            // ScriptableObject asset
            var so = ScriptableObject.CreateInstance<SmartReferenceTestSO>();
            so.number = 123;
            so.text = "hello";
            CreateOrReplaceAsset(so, SoAssetPath);

            // Texture asset
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            tex.name = "TestTex";
            // Fill a single pixel to avoid any "empty" edge cases
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            CreateOrReplaceAsset(tex, TextureAssetPath);

            // Material asset
            var mat = new Material(Shader.Find("Unlit/Color"));
            mat.name = "TestMat";
            mat.color = Color.magenta;
            CreateOrReplaceAsset(mat, MaterialAssetPath);

            // Prefab asset
            var go = new GameObject("TestPrefab");
            go.AddComponent<BoxCollider>();
            CreateOrReplacePrefab(go, PrefabAssetPath);
            Object.DestroyImmediate(go);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }

        public static void Cleanup()
        {
#if UNITY_EDITOR
            if (AssetDatabase.IsValidFolder(RootFolder))
            {
                AssetDatabase.DeleteAsset(RootFolder);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
#endif
        }

#if UNITY_EDITOR
        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/SmartReference_PlayModeTestAssets"))
                AssetDatabase.CreateFolder("Assets", "SmartReference_PlayModeTestAssets");

            if (!AssetDatabase.IsValidFolder(ResourcesFolder))
                AssetDatabase.CreateFolder(RootFolder, "Resources");

            if (!AssetDatabase.IsValidFolder(ResourcesSubFolder))
                AssetDatabase.CreateFolder(ResourcesFolder, "SmartReferenceTest");
        }

        private static void CreateOrReplaceAsset(Object asset, string assetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            AssetDatabase.CreateAsset(asset, assetPath);
        }

        private static void CreateOrReplacePrefab(GameObject go, string prefabPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(prefabPath);
            }

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        }
#endif
    }

    /// <summary>
    /// Test ScriptableObject type used in PlayMode.
    /// Keeping it in test assembly is OK for editor PlayMode tests.
    /// </summary>
    public sealed class SmartReferenceTestSO : ScriptableObject
    {
        public int number;
        public string text;
    }
}
