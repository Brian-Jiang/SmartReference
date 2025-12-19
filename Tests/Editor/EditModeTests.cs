// SmartReferenceTool_EditModeTests.cs
// Put in an EditMode test assembly, e.g.
// Assets/Tests/EditMode/SmartReference/SmartReferenceTool_EditModeTests.cs
// or Packages/com.yourname.smartreference/Tests/EditMode/SmartReferenceTool_EditModeTests.cs

using System;
using NUnit.Framework;
using SmartReference.Runtime;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SmartReference.Editor.Tests
{
    public sealed class EditModeTests
    {
        private const string RootFolder = "Assets/SmartReferenceTool_EditModeTestAssets";
        private const string SoAssetPath = RootFolder + "/TestAsset.asset";

        private TestContainerSO container;
        private SerializedObject serializedContainer;
        private SerializedProperty smartRefProp;
        private SerializedProperty smartRefToGoProp;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            EnsureFolder(RootFolder);

            // Create a ScriptableObject asset we can reference
            var testAsset = ScriptableObject.CreateInstance<TestAssetSO>();
            testAsset.name = "TestAsset";
            CreateOrReplaceAsset(testAsset, SoAssetPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (AssetDatabase.IsValidFolder(RootFolder))
            {
                AssetDatabase.DeleteAsset(RootFolder);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        [SetUp]
        public void SetUp()
        {
            // Create a container object (not an asset) that has a SmartReference-like field
            container = ScriptableObject.CreateInstance<TestContainerSO>();
            serializedContainer = new SerializedObject(container);
            smartRefProp = serializedContainer.FindProperty("refToAsset");
            Assert.NotNull(smartRefProp, "Failed to find refToAsset SerializedProperty");
            smartRefToGoProp = serializedContainer.FindProperty("refToGameObject");
            Assert.NotNull(smartRefToGoProp, "Failed to find refToGameObject SerializedProperty");

            // Ensure type matches the asset type
            var typeProp = smartRefProp.FindPropertyRelative("type");
            typeProp.stringValue = typeof(TestAssetSO).AssemblyQualifiedName;
            serializedContainer.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            if (container != null)
            {
                Object.DestroyImmediate(container);
                container = null;
            }
        }

        // ---------------------------
        // SetReference correctness
        // ---------------------------

        [Test]
        public void SetReference_WritesGuidFileIdPath()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TestAssetSO>(SoAssetPath);
            Assert.NotNull(asset);

            SmartReferenceTool.SetReference(smartRefProp, asset);

            var guidProp = smartRefProp.FindPropertyRelative("guid");
            var fileIDProp = smartRefProp.FindPropertyRelative("fileID");
            var pathProp = smartRefProp.FindPropertyRelative("path");

            Assert.IsFalse(string.IsNullOrEmpty(guidProp.stringValue), "guid should not be empty");
            Assert.Greater(fileIDProp.longValue, 0, "fileID should be > 0");
            Assert.AreEqual(SoAssetPath, pathProp.stringValue, "path should match AssetDatabase path");

            // Verify it matches Unity's ground truth
            Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var expectedGuid, out long expectedFileID));
            Assert.AreEqual(expectedGuid, guidProp.stringValue);
            Assert.AreEqual(expectedFileID, fileIDProp.longValue);
        }

        [Test]
        public void SetReference_WhenTypeMismatch_Throws()
        {
            // type expects TestAssetSO, but we pass a material
            var gameObject = new GameObject();
            try
            {
                // Material is not an asset here; save it so it becomes an asset
                var goPath = RootFolder + "/go.prefab";
                AssetDatabase.CreateAsset(gameObject, goPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                var goAsset = AssetDatabase.LoadAssetAtPath<GameObject>(goPath);
                Assert.NotNull(goAsset);

                var ex = Assert.Throws<ArgumentException>(() => SmartReferenceTool.SetReference(smartRefProp, goAsset));
                StringAssert.Contains("Object type does not match SmartReference type", ex.Message);
            }
            finally
            {
                // cleanup
                var goPath = RootFolder + "/go.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(goPath) != null)
                    AssetDatabase.DeleteAsset(goPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void SetReference_WhenObjIsNotAsset_Throws()
        {
            var sceneGo = new GameObject("NotAnAsset");
            try
            {
                var ex = Assert.Throws<ArgumentException>(() => SmartReferenceTool.SetReference(smartRefToGoProp, sceneGo));
                StringAssert.Contains("Object is not a valid asset with GUID/fileID", ex.Message);
            }
            finally
            {
                Object.DestroyImmediate(sceneGo);
            }
        }

        [Test]
        public void SetReference_WhenPropertyIsNull_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => SmartReferenceTool.SetReference(null, AssetDatabase.LoadAssetAtPath<TestAssetSO>(SoAssetPath)));
        }

        [Test]
        public void SetReference_WhenObjIsNull_Clears()
        {
            // First set something
            var asset = AssetDatabase.LoadAssetAtPath<TestAssetSO>(SoAssetPath);
            SmartReferenceTool.SetReference(smartRefProp, asset);

            // Then set null to clear
            SmartReferenceTool.SetReference(smartRefProp, null);

            Assert.AreEqual(string.Empty, smartRefProp.FindPropertyRelative("guid").stringValue);
            Assert.AreEqual(0, smartRefProp.FindPropertyRelative("fileID").longValue);
            Assert.AreEqual(string.Empty, smartRefProp.FindPropertyRelative("path").stringValue);
        }

        // ---------------------------
        // ClearReference correctness
        // ---------------------------

        [Test]
        public void ClearReference_ClearsGuidFileIdPath()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TestAssetSO>(SoAssetPath);
            SmartReferenceTool.SetReference(smartRefProp, asset);

            SmartReferenceTool.ClearReference(smartRefProp);

            Assert.AreEqual(string.Empty, smartRefProp.FindPropertyRelative("guid").stringValue);
            Assert.AreEqual(0, smartRefProp.FindPropertyRelative("fileID").longValue);
            Assert.AreEqual(string.Empty, smartRefProp.FindPropertyRelative("path").stringValue);
        }

        [Test]
        public void ClearReference_WhenPropertyIsNull_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => SmartReferenceTool.ClearReference(null));
        }

        [Test]
        public void ClearReference_WhenNotSmartReferenceProperty_Throws()
        {
            var badProp = serializedContainer.FindProperty("notARef");
            Assert.NotNull(badProp);

            var ex = Assert.Throws<ArgumentException>(() => SmartReferenceTool.ClearReference(badProp));
            StringAssert.Contains("Property does not look like a SmartReference", ex.Message);
        }

        // ---------------------------
        // ApplyModifiedProperties behavior
        // ---------------------------

        [Test]
        public void SetReference_AppliesModifiedProperties_ToTargetObject()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TestAssetSO>(SoAssetPath);

            // Call SetReference (it calls ApplyModifiedProperties internally)
            SmartReferenceTool.SetReference(smartRefProp, asset);

            // Rebuild SerializedObject to ensure the values were truly applied
            var so2 = new SerializedObject(container);
            var prop2 = so2.FindProperty("refToAsset");

            Assert.AreEqual(smartRefProp.FindPropertyRelative("guid").stringValue, prop2.FindPropertyRelative("guid").stringValue);
            Assert.AreEqual(smartRefProp.FindPropertyRelative("fileID").longValue, prop2.FindPropertyRelative("fileID").longValue);
            Assert.AreEqual(smartRefProp.FindPropertyRelative("path").stringValue, prop2.FindPropertyRelative("path").stringValue);
        }

        // ---------------------------
        // Local test types
        // ---------------------------

        // [Serializable]
        // private struct SmartRefLike
        // {
        //     public string guid;
        //     public long fileID;
        //     public string path;
        //     public string type;
        // }

        private sealed class TestContainerSO : ScriptableObject
        {
            public SmartReference<TestAssetSO> refToAsset;
            public SmartReference<GameObject> refToGameObject;
            public Object notARef;
        }

        private sealed class TestAssetSO : ScriptableObject
        {
            public int dummy;
        }

        // ---------------------------
        // Asset helpers
        // ---------------------------

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            // Create nested folders as needed
            var parts = folder.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void CreateOrReplaceAsset(Object asset, string assetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (existing != null)
                AssetDatabase.DeleteAsset(assetPath);

            AssetDatabase.CreateAsset(asset, assetPath);
        }
    }
}
