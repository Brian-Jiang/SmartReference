using NUnit.Framework;
using UnityEditor;

namespace SmartReference.Editor.Tests
{
    public sealed class SceneReferenceTests
    {
        private const string RootFolder = "Assets/SmartReference_SceneReferenceTestAssets";
        private const string ScenePath = RootFolder + "/Scene.unity";
        private const string MissingScenePath = RootFolder + "/Missing.unity";

        private EditorBuildSettingsScene[] savedScenes;

        [SetUp]
        public void SetUp()
        {
            savedScenes = SceneReferenceEditor.GetBuildScenes(out _);
        }

        [TearDown]
        public void TearDown()
        {
            SceneReferenceEditor.SetBuildScenes(savedScenes);
            AssetDatabase.DeleteAsset(RootFolder);
        }

        [Test]
        public void FixScene_EmptyPath_DoesNotChangeBuildScenes()
        {
            SceneReferenceEditor.FixScene(string.Empty);

            CollectionAssert.AreEqual(Paths(savedScenes), Paths(SceneReferenceEditor.GetBuildScenes(out _)));
        }

        [Test]
        public void FixScene_MissingScene_DoesNotChangeBuildScenes()
        {
            SceneReferenceEditor.FixScene(MissingScenePath);

            CollectionAssert.AreEqual(Paths(savedScenes), Paths(SceneReferenceEditor.GetBuildScenes(out _)));
        }

        [Test]
        public void GetSceneStatus_MissingScene_IsNotFixable()
        {
            Assert.IsTrue(SceneReferenceEditor.GetSceneStatus(MissingScenePath, out var message, out var fixable));
            Assert.IsFalse(fixable);
            StringAssert.Contains("missing", message);
        }

        [Test]
        public void GetSceneStatus_EmptyPath_HasNoStatus()
        {
            Assert.IsFalse(SceneReferenceEditor.GetSceneStatus(string.Empty, out _, out var fixable));
            Assert.IsFalse(fixable);
        }

        [Test]
        public void FixScene_SceneNotInBuild_AddsItEnabled()
        {
            TestAssetUtils.EnsureFolder(RootFolder);
            TestAssetUtils.CreateScene(ScenePath);
            Assert.IsTrue(SceneReferenceEditor.GetSceneStatus(ScenePath, out _, out var fixable));
            Assert.IsTrue(fixable);

            SceneReferenceEditor.FixScene(ScenePath);

            var scenes = SceneReferenceEditor.GetBuildScenes(out _);
            Assert.AreEqual(savedScenes.Length + 1, scenes.Length);
            Assert.AreEqual(ScenePath, scenes[scenes.Length - 1].path);
            Assert.IsTrue(scenes[scenes.Length - 1].enabled);
            Assert.IsTrue(SceneReferenceEditor.GetSceneStatus(ScenePath, out _, out fixable));
            Assert.IsFalse(fixable);
        }

        private static string[] Paths(EditorBuildSettingsScene[] scenes)
        {
            var paths = new string[scenes.Length];
            for (var i = 0; i < scenes.Length; i++) {
                paths[i] = scenes[i].path;
            }

            return paths;
        }
    }
}
