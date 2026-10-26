using System;
using System.IO;
using NUnit.Framework;
using SmartReference.Runtime.Tests;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SmartReference.Editor.Tests
{
    public sealed class UpdateAllReferencesTests
    {
        private const string RootFolder = "Assets/SmartReference_UpdateTestAssets";
        private const string TargetPath = RootFolder + "/Target.prefab";
        private const string StalePath = RootFolder + "/OldName.prefab";
        private const string HolderPath = RootFolder + "/Holder.asset";
        private const string BehaviourPrefabPath = RootFolder + "/Behaviour.prefab";
        private const string OuterPrefabPath = RootFolder + "/Outer.prefab";
        private const string ScenePath = RootFolder + "/Scene.unity";

        private string targetGuid;
        private long targetFileID;

        [SetUp]
        public void SetUp()
        {
            TestAssetUtils.EnsureFolder(RootFolder);

            var target = new GameObject("Target");
            PrefabUtility.SaveAsPrefabAsset(target, TargetPath);
            Object.DestroyImmediate(target);
            Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                AssetDatabase.LoadAssetAtPath<GameObject>(TargetPath), out targetGuid, out targetFileID));
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(RootFolder);
        }

        [Test]
        public void UpdatesNestedListArrayAndBasePrivateFields_InScriptableObject()
        {
            var holder = ScriptableObject.CreateInstance<SmartReferenceTestHolder>();
            AssetDatabase.CreateAsset(holder, HolderPath);
            using (var so = new SerializedObject(holder)) {
                SetStale(so.FindProperty("basePrivateRef"));
                SetStale(so.FindProperty("nested.nestedRef"));
                so.FindProperty("list").arraySize = 2;
                SetStale(so.FindProperty("list").GetArrayElementAtIndex(0));
                SetStale(so.FindProperty("list").GetArrayElementAtIndex(1));
                so.FindProperty("array").arraySize = 1;
                SetStale(so.FindProperty("array").GetArrayElementAtIndex(0));
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.SaveAssets();

            SmartReferenceUtils.UpdateReferencesInAssets(new[] { HolderPath });

            using (var so = new SerializedObject(AssetDatabase.LoadAssetAtPath<SmartReferenceTestHolder>(HolderPath))) {
                AssertPath(so.FindProperty("basePrivateRef"));
                AssertPath(so.FindProperty("nested.nestedRef"));
                AssertPath(so.FindProperty("list").GetArrayElementAtIndex(0));
                AssertPath(so.FindProperty("list").GetArrayElementAtIndex(1));
                AssertPath(so.FindProperty("array").GetArrayElementAtIndex(0));
            }
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void UpdatesComponentsInPrefab_WithoutAddingOverridesToNestedInstances()
        {
            CreateBehaviourPrefab();

            // Outer prefab nests the behaviour prefab without overriding it
            var outer = new GameObject("Outer");
            var nested = (GameObject) PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BehaviourPrefabPath));
            nested.transform.SetParent(outer.transform);
            PrefabUtility.SaveAsPrefabAsset(outer, OuterPrefabPath);
            Object.DestroyImmediate(outer);

            // Outer first, so the nested instance is visited while its source is still stale
            SmartReferenceUtils.UpdateReferencesInAssets(new[] { OuterPrefabPath, BehaviourPrefabPath });

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BehaviourPrefabPath);
            using (var so = new SerializedObject(prefab.GetComponentInChildren<SmartReferenceTestBehaviour>(true))) {
                AssertPath(so.FindProperty("reference"));
                AssertPath(so.FindProperty("nested.nestedRef"));
                AssertPath(so.FindProperty("list").GetArrayElementAtIndex(0));
            }

            var outerRoot = PrefabUtility.LoadPrefabContents(OuterPrefabPath);
            try {
                using var so = new SerializedObject(outerRoot.GetComponentInChildren<SmartReferenceTestBehaviour>(true));
                var reference = so.FindProperty("reference");
                Assert.IsFalse(reference.prefabOverride);
                AssertPath(reference);
            }
            finally {
                PrefabUtility.UnloadPrefabContents(outerRoot);
            }
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void UpdatesComponentsInScene()
        {
            TestAssetUtils.CreateScene(ScenePath, scene => {
                var go = new GameObject("WithBehaviour");
                SceneManager.MoveGameObjectToScene(go, scene);
                var child = new GameObject("Child");
                child.transform.SetParent(go.transform);
                using var so = new SerializedObject(child.AddComponent<SmartReferenceTestBehaviour>());
                SetStale(so.FindProperty("reference"));
                SetStale(so.FindProperty("nested.nestedRef"));
                so.ApplyModifiedPropertiesWithoutUndo();
            });
            StringAssert.Contains(StalePath, File.ReadAllText(ScenePath));

            SmartReferenceUtils.UpdateReferencesInAssets(new[] { ScenePath });

            var text = File.ReadAllText(ScenePath);
            StringAssert.DoesNotContain(StalePath, text);
            StringAssert.Contains(TargetPath, text);
            Assert.IsFalse(SceneManager.GetSceneByPath(ScenePath).IsValid(), "Scene opened for the update should be closed");
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void CandidateScripts_IncludeTypesWithSmartReferences_Only()
        {
            var holder = ScriptableObject.CreateInstance<SmartReferenceTestHolder>();
            var plain = ScriptableObject.CreateInstance<SmartReferenceTestSO>();
            try {
                var scripts = SmartReferenceUtils.GetCandidateScriptPaths();
                Assert.IsTrue(scripts.Contains(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(holder))));
                Assert.IsFalse(scripts.Contains(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(plain))));
            }
            finally {
                Object.DestroyImmediate(holder);
                Object.DestroyImmediate(plain);
            }
        }

        private void CreateBehaviourPrefab()
        {
            var go = new GameObject("Behaviour");
            var child = new GameObject("Child");
            child.transform.SetParent(go.transform);
            using (var so = new SerializedObject(child.AddComponent<SmartReferenceTestBehaviour>())) {
                SetStale(so.FindProperty("reference"));
                SetStale(so.FindProperty("nested.nestedRef"));
                so.FindProperty("list").arraySize = 1;
                SetStale(so.FindProperty("list").GetArrayElementAtIndex(0));
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(go, BehaviourPrefabPath);
            Object.DestroyImmediate(go);
        }

        private void SetStale(SerializedProperty property)
        {
            Assert.NotNull(property);
            property.FindPropertyRelative("guid").stringValue = targetGuid;
            property.FindPropertyRelative("fileID").longValue = targetFileID;
            property.FindPropertyRelative("path").stringValue = StalePath;
            property.FindPropertyRelative("type").stringValue = typeof(GameObject).AssemblyQualifiedName;
        }

        private static void AssertPath(SerializedProperty property)
        {
            Assert.NotNull(property);
            Assert.AreEqual(TargetPath, property.FindPropertyRelative("path").stringValue, property.propertyPath);
        }
    }

    internal static class TestAssetUtils
    {
        internal static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;

            var parts = folder.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++) {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        /// <summary>
        /// Create and save a scene asset, keeping the open scenes when possible.
        /// </summary>
        internal static void CreateScene(string path, Action<Scene> populate = null)
        {
            if (!HasUntitledScene()) {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                try {
                    populate?.Invoke(scene);
                    Assert.IsTrue(EditorSceneManager.SaveScene(scene, path));
                }
                finally {
                    EditorSceneManager.CloseScene(scene, true);
                }
                return;
            }

            // Unity can't add a new scene next to an untitled one, like the scene the test runner opens.
            // Replace it instead, as long as there is nothing to lose.
            for (var i = 0; i < SceneManager.sceneCount; i++) {
                if (SceneManager.GetSceneAt(i).isDirty) {
                    Assert.Ignore("Can't create a scene while a scene has unsaved changes");
                }
            }

            var setup = EditorSceneManager.GetSceneManagerSetup();
            var single = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            populate?.Invoke(single);
            Assert.IsTrue(EditorSceneManager.SaveScene(single, path));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Reopen the saved scenes that were open before
            var saved = Array.FindAll(setup, s => !string.IsNullOrEmpty(s.path));
            if (saved.Length > 0) {
                if (!Array.Exists(saved, s => s.isActive)) {
                    saved[0].isActive = true;
                }
                EditorSceneManager.RestoreSceneManagerSetup(saved);
            }
        }

        private static bool HasUntitledScene()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++) {
                if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path)) return true;
            }

            return false;
        }
    }
}
