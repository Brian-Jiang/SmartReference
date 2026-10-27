#if SMARTREFERENCE_ADDRESSABLES_SUPPORT
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
#endif

namespace SmartReference.Runtime.Tests
{
    [PrebuildSetup(typeof(AddressablesTestSetup))]
    [PostBuildCleanup(typeof(AddressablesTestSetup))]
    public sealed class AddressablesPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            global::SmartReference.Runtime.SmartReference.InitWithAddressablesLoader();
        }

        [Test]
        public void Release_Prefab_NoErrors()
        {
            var r = new SmartReference<GameObject> { path = AddressablesTestSetup.PrefabPath };
            Assert.NotNull(r.Value);

            r.Release();

            Assert.IsFalse(r.IsLoaded);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Release_DuringAsyncLoad_CallersGetNull()
        {
            var r = new SmartReference<GameObject> { path = AddressablesTestSetup.PrefabPath };
            var eventCalled = false;
            GameObject eventResult = null;
            r.OnAsyncLoadComplete += result =>
            {
                eventCalled = true;
                eventResult = result;
            };
            var task = r.LoadAsyncTask();
            Assert.IsTrue(r.IsLoading);

            r.Release();
            while (!task.IsCompleted) yield return null;

            Assert.IsNull(task.Result);
            Assert.IsTrue(eventCalled);
            Assert.IsNull(eventResult);
            Assert.IsFalse(r.IsLoaded);
            Assert.IsFalse(r.IsLoading);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Release_ThenLoadAgain_DuringAsyncLoad_DeliversAsset()
        {
            var r = new SmartReference<GameObject> { path = AddressablesTestSetup.PrefabPath };
            var task = r.LoadAsyncTask();
            Assert.IsTrue(r.IsLoading);

            r.Release();
            var reloadTask = r.LoadAsyncTask();
            while (!reloadTask.IsCompleted) yield return null;

            Assert.NotNull(task.Result);
            Assert.NotNull(reloadTask.Result);
            Assert.IsTrue(r.IsLoaded);
            Assert.IsFalse(r.IsLoading);

            r.Release();
            Assert.IsFalse(r.IsLoaded);
            LogAssert.NoUnexpectedReceived();
        }
    }

    /// <summary>
    /// Creates addressable test assets before entering play mode, and removes them afterwards.
    /// </summary>
    public sealed class AddressablesTestSetup : IPrebuildSetup, IPostBuildCleanup
    {
        public const string RootFolder = "Assets/SmartReference_AddressablesTestAssets";
        public const string PrefabPath = RootFolder + "/TestPrefab.prefab";
        private const string GroupName = "SmartReference Tests";
        private const string PlayModeIndexKey = "SmartReference.Tests.PreviousPlayModeIndex";

        public void Setup()
        {
#if UNITY_EDITOR
            if (!AssetDatabase.IsValidFolder(RootFolder))
            {
                AssetDatabase.CreateFolder("Assets", "SmartReference_AddressablesTestAssets");
            }

            var go = new GameObject("TestPrefab");
            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            var group = settings.FindGroup(GroupName) ??
                        settings.CreateGroup(GroupName, false, false, true, settings.DefaultGroup.Schemas);
            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(PrefabPath), group);
            entry.address = PrefabPath;

            // Load from the asset database, so no content build is needed
            SessionState.SetInt(PlayModeIndexKey, settings.ActivePlayModeDataBuilderIndex);
            var fastModeIndex = settings.DataBuilders.FindIndex(b => b is BuildScriptFastMode);
            Assert.GreaterOrEqual(fastModeIndex, 0, "Addressables settings have no Use Asset Database play mode script");
            settings.ActivePlayModeDataBuilderIndex = fastModeIndex;

            AssetDatabase.SaveAssets();
#endif
        }

        public void Cleanup()
        {
#if UNITY_EDITOR
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings != null)
            {
                var group = settings.FindGroup(GroupName);
                if (group != null)
                {
                    settings.RemoveGroup(group);
                }

                var previousIndex = SessionState.GetInt(PlayModeIndexKey, -1);
                if (previousIndex >= 0)
                {
                    settings.ActivePlayModeDataBuilderIndex = previousIndex;
                }
                SessionState.EraseInt(PlayModeIndexKey);
            }

            if (AssetDatabase.IsValidFolder(RootFolder))
            {
                AssetDatabase.DeleteAsset(RootFolder);
            }
            AssetDatabase.SaveAssets();
#endif
        }
    }
}
#endif
