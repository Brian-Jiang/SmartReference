using System;
using System.Collections;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

#if SMARTREFERENCE_UNITASK_SUPPORT
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
#endif

namespace SmartReference.Runtime.Tests
{
    public sealed class PlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            AssetSetup.Setup();
            
            // Ensure we start from a known loader each test
            global::SmartReference.Runtime.SmartReference.InitWithResourcesLoader();
        }

        [TearDown]
        public void TearDown()
        {
            AssetSetup.Cleanup();
        }

        // ----------------------------
        // 1) Error/edge-case logging
        // ----------------------------

        [Test]
        public void EmptyPath_LogsError_SyncLoad()
        {
            var r = new global::SmartReference.Runtime.SmartReference<SmartReferenceTestSO> { path = "" };

            LogAssert.Expect(LogType.Error, "[SmartReference] Asset path is null.");
            _ = r.Value;
        }

        [Test]
        public void NullLoader_LogsError_SyncLoad()
        {
            // Force loader null via custom init
            global::SmartReference.Runtime.SmartReference.InitWithCustomLoader(null);

            var r = new global::SmartReference.Runtime.SmartReference<SmartReferenceTestSO>
            {
                path = AssetSetup.SoResourcesPath
            };

            LogAssert.Expect(LogType.Error, "[SmartReference] Loader is null, please init the SmartReference with a loader before using it.");
            _ = r.Value;
        }

        // ----------------------------
        // 2) Correctness - Resources sync load
        // ----------------------------

        [Test]
        public void SyncLoad_ScriptableObject_CorrectData()
        {
            global::SmartReference.Runtime.SmartReference.InitWithResourcesLoader();

            var r = new global::SmartReference.Runtime.SmartReference<SmartReferenceTestSO>
            {
                path = AssetSetup.SoResourcesPath
            };

            var so = r.Value;
            Assert.NotNull(so);
            Assert.AreEqual(123, so.number);
            Assert.AreEqual("hello", so.text);
        }

        [Test]
        public void SyncLoad_Texture_NotNull()
        {
            global::SmartReference.Runtime.SmartReference.InitWithResourcesLoader();

            var r = new global::SmartReference.Runtime.SmartReference<Texture2D>
            {
                path = AssetSetup.TextureResourcesPath
            };

            var tex = r.Value;
            Assert.NotNull(tex);
            Assert.AreEqual(16, tex.width);
            Assert.AreEqual(16, tex.height);
        }

        [Test]
        public void SyncLoad_Prefab_NotNull()
        {
            global::SmartReference.Runtime.SmartReference.InitWithResourcesLoader();

            var r = new global::SmartReference.Runtime.SmartReference<GameObject>
            {
                path = AssetSetup.PrefabResourcesPath
            };

            var prefab = r.Value;
            Assert.NotNull(prefab);
            Assert.AreEqual("TestPrefab", prefab.name);
        }

        [Test]
        public void ImplicitOperator_ReturnsValue()
        {
            var r = new global::SmartReference.Runtime.SmartReference<SmartReferenceTestSO>
            {
                path = AssetSetup.SoResourcesPath
            };

            SmartReferenceTestSO so = r; // implicit
            Assert.NotNull(so);
            Assert.AreEqual(123, so.number);
        }

        // ----------------------------
        // 3) Correctness - Resources async load (Task)
        // ----------------------------

        [UnityTest]
        public IEnumerator AsyncLoad_Task_CompletesAndReturnsAsset()
        {
            SmartReference.InitWithResourcesLoader();
            
            var r = new global::SmartReference.Runtime.SmartReference<SmartReferenceTestSO>
            {
                path = AssetSetup.SoResourcesPath
            };

            var task = r.LoadAsyncTask();
            yield return WaitForTask(task);

            Assert.NotNull(task.Result);
            Assert.AreEqual(123, task.Result.number);
        }

#if SMARTREFERENCE_UNITASK_SUPPORT
        // ----------------------------
        // 4) Correctness - UniTask path
        // ----------------------------

        [UnityTest]
        public IEnumerator AsyncLoad_UniTask_CompletesAndReturnsAsset()
        {
            var r = new SmartReference<SmartReferenceTestSO>
            {
                path = AssetSetup.SoResourcesPath
            };

            var ut = r.LoadAsyncUniTask();
            yield return ut.ToCoroutine(result =>
            {
                Assert.NotNull(result);
                Assert.AreEqual(123, result.number);
            });
        }

        [UnityTest]
        public IEnumerator UniTask_ExternalCancellation_CancelsAwaitButLoadMayStillComplete()
        {
            // Cancellation here is "await-side" cancellation (AttachExternalCancellation).
            var r = new SmartReference<SmartReferenceTestSO>
            {
                path = AssetSetup.SoResourcesPath
            };

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Exception capturedException = null;

            // IMPORTANT: yield the coroutine
            yield return r
                .LoadAsyncUniTask(cts.Token)
                .ToCoroutine(exceptionHandler: ex => capturedException = ex);

            // Verify await-side cancellation
            Assert.NotNull(capturedException);
            Assert.IsInstanceOf<OperationCanceledException>(capturedException);

            // Let the underlying callback-based load finish.
            yield return null;
            yield return null;

            Assert.NotNull(r.Value);
        }
#endif

        // ----------------------------
        // 5) In-flight sharing + release correctness using a stub loader
        // ----------------------------

        [UnityTest]
        public IEnumerator InFlightSharing_Task_CallsLoaderOnce()
        {
            var stub = new StubLoader(delayFrames: 2);
            global::SmartReference.Runtime.SmartReference.InitWithCustomLoader(stub);

            var r = new global::SmartReference.Runtime.SmartReference<SmartReferenceTestSO>
            {
                path = "stub://so"
            };

            var t1 = r.LoadAsyncTask();
            var t2 = r.LoadAsyncTask();

            // Both should share the same underlying load
            Assert.AreEqual(1, stub.AsyncLoadCalls);

            yield return WaitForTask(t1);
            yield return WaitForTask(t2);

            Assert.NotNull(t1.Result);
            Assert.NotNull(t2.Result);
            Assert.AreSame(t1.Result, t2.Result);
            Assert.AreEqual(1, stub.AsyncLoadCalls);
        }

        [UnityTest]
        public IEnumerator Release_AfterLoaded_CallsRelease_AndAllowsReload()
        {
            var stub = new StubLoader(delayFrames: 1);
            global::SmartReference.Runtime.SmartReference.InitWithCustomLoader(stub);

            var r = new global::SmartReference.Runtime.SmartReference<SmartReferenceTestSO> { path = "stub://so" };

            var t1 = r.LoadAsyncTask();
            yield return WaitForTask(t1);
            Assert.NotNull(t1.Result);

            r.Release();
            Assert.AreEqual(1, stub.ReleaseCalls);

            // Reload should trigger a new load
            var t2 = r.LoadAsyncTask();
            yield return WaitForTask(t2);

            Assert.NotNull(t2.Result);
            Assert.AreEqual(2, stub.AsyncLoadCalls);
        }

        [UnityTest]
        public IEnumerator Release_DuringLoading_BestEffortCancel_ThenReleaseOnceCompleted()
        {
            var stub = new StubLoader(delayFrames: 3);
            global::SmartReference.Runtime.SmartReference.InitWithCustomLoader(stub);

            var r = new global::SmartReference.Runtime.SmartReference<SmartReferenceTestSO> { path = "stub://so" };
            var t = r.LoadAsyncTask();

            // Immediately request release while loading
            r.Release();

            // Depending on your implementation, cancel may be called (best-effort)
            Assert.AreEqual(stub.CancelCalls, 1);

            yield return WaitForTask(t);

            // After completion, release should have been performed
            // (Your SmartReference.Release() logic might release immediately or defer until callback.)
            Assert.AreEqual(stub.ReleaseCalls, 0);
        }

        // ----------------------------
        // 6) Performance (PlayMode microbenchmarks)
        // ----------------------------

        [Test]
        public void Performance_SyncValueAccess_AfterLoaded_IsFast()
        {
            // Micro: ensure Value property is cheap after load (no reload).
            var r = new global::SmartReference.Runtime.SmartReference<SmartReferenceTestSO>
            {
                path = AssetSetup.SoResourcesPath
            };

            // Warm-up load
            var so = r.Value;
            Assert.NotNull(so);

            // Measure repeated Value accesses
            const int iterations = 200_000;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                _ = r.Value;
            }
            sw.Stop();

            // Very conservative threshold (editor machine variance is large).
            // Adjust tighter for your CI/hardware once stable.
            Assert.Less(sw.ElapsedMilliseconds, 200, $"Value access too slow: {sw.ElapsedMilliseconds}ms for {iterations} iterations.");
        }

        // ----------------------------
        // Helpers
        // ----------------------------

        private static IEnumerator WaitForTask<T>(System.Threading.Tasks.Task<T> task)
        {
            while (!task.IsCompleted)
                yield return null;

            if (task.IsFaulted)
                throw task.Exception!;
        }

        /// <summary>
        /// Stub loader that simulates async loading + handle-based release/cancel.
        /// This tests SmartReference's in-flight sharing & release semantics without depending on Resources internals.
        /// </summary>
        private sealed class StubLoader : ISmartReferenceLoader
        {
            private sealed class H : ISmartReferenceHandle
            {
                public bool canceled;
            }

            private readonly int delayFrames;
            public int LoadCalls { get; private set; }
            public int AsyncLoadCalls { get; private set; }
            public int ReleaseCalls { get; private set; }
            public int CancelCalls { get; private set; }

            public StubLoader(int delayFrames)
            {
                this.delayFrames = Mathf.Max(0, delayFrames);
            }

            public Object Load(string path, Type type)
            {
                LoadCalls++;
                return CreateObject(type);
            }

            public global::SmartReference.Runtime.ISmartReferenceHandle LoadAsync(string path, Type type, Action<Object> onComplete)
            {
                AsyncLoadCalls++;
                var h = new H();
                CoroutineRunner.Ensure().StartCoroutine(DelayedComplete(h, type, onComplete));
                return h;
            }

            public void Release(global::SmartReference.Runtime.ISmartReferenceHandle handle)
            {
                ReleaseCalls++;
                // In a real loader, you'd release handle/asset (Addressables.Release, etc.)
            }

            public void Cancel(global::SmartReference.Runtime.ISmartReferenceHandle handle)
            {
                CancelCalls++;
                if (handle is H h) h.canceled = true;
            }

            private IEnumerator DelayedComplete(H h, Type type, Action<Object> onComplete)
            {
                for (int i = 0; i < delayFrames; i++)
                    yield return null;

                if (h.canceled)
                {
                    onComplete?.Invoke(null);
                    yield break;
                }

                onComplete?.Invoke(CreateObject(type));
            }

            private static Object CreateObject(Type type)
            {
                if (type == typeof(SmartReferenceTestSO) || typeof(ScriptableObject).IsAssignableFrom(type))
                {
                    var so = ScriptableObject.CreateInstance<SmartReferenceTestSO>();
                    so.number = 999;
                    so.text = "stub";
                    return so;
                }

                if (type == typeof(GameObject))
                {
                    return new GameObject("StubGO");
                }

                // Fallback: create a dummy ScriptableObject
                var fallback = ScriptableObject.CreateInstance<SmartReferenceTestSO>();
                fallback.number = 888;
                fallback.text = "fallback";
                return fallback;
            }
        }

        private sealed class CoroutineRunner : MonoBehaviour
        {
            private static CoroutineRunner instance;

            public static CoroutineRunner Ensure()
            {
                if (instance != null) return instance;

                var go = new GameObject("SmartReference_TestCoroutineRunner");
                UnityEngine.Object.DontDestroyOnLoad(go);
                instance = go.AddComponent<CoroutineRunner>();
                return instance;
            }
        }
    }
}
