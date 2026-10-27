using System;
using System.Collections;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

#if SMARTREFERENCE_UNITASK_SUPPORT || UNITY_2023_2_OR_NEWER
using System.Threading;
using System.Threading.Tasks;
#endif

#if SMARTREFERENCE_UNITASK_SUPPORT
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

#if UNITY_2023_2_OR_NEWER
        // ----------------------------
        // 4b) Correctness - Awaitable path
        // ----------------------------

        [UnityTest]
        public IEnumerator AsyncLoad_Awaitable_CompletesAndReturnsAsset()
        {
            var r = new SmartReference<SmartReferenceTestSO>
            {
                path = AssetSetup.SoResourcesPath
            };

            var task = AsTask(r.LoadAsyncAwaitable());
            yield return WaitForTask(task);

            Assert.NotNull(task.Result);
            Assert.AreEqual(123, task.Result.number);
        }

        [UnityTest]
        public IEnumerator InFlightSharing_Awaitable_CallsLoaderOnce_EachCallerGetsOwnAwaitable()
        {
            var stub = new StubLoader(delayFrames: 2);
            global::SmartReference.Runtime.SmartReference.InitWithCustomLoader(stub);

            var r = new SmartReference<SmartReferenceTestSO> { path = "stub://so" };

            var a1 = r.LoadAsyncAwaitable();
            var a2 = r.LoadAsyncAwaitable();

            // An Awaitable can only be awaited once, so callers must not share one
            Assert.AreNotSame(a1, a2);
            Assert.AreEqual(1, stub.AsyncLoadCalls);

            var t1 = AsTask(a1);
            var t2 = AsTask(a2);
            yield return WaitForTask(t1);
            yield return WaitForTask(t2);

            Assert.NotNull(t1.Result);
            Assert.AreSame(t1.Result, t2.Result);
            Assert.AreEqual(1, stub.AsyncLoadCalls);
        }

        [UnityTest]
        public IEnumerator Awaitable_CancelOneCaller_OtherStillCompletes()
        {
            var stub = new StubLoader(delayFrames: 3);
            global::SmartReference.Runtime.SmartReference.InitWithCustomLoader(stub);

            var r = new SmartReference<SmartReferenceTestSO> { path = "stub://so" };

            using var cts = new CancellationTokenSource();
            var cancelled = AsTask(r.LoadAsyncAwaitable(cts.Token));
            var other = AsTask(r.LoadAsyncAwaitable());

            yield return null;
            cts.Cancel();

            yield return WaitForTask(cancelled);
            Assert.IsTrue(cancelled.IsCanceled);

            yield return WaitForTask(other);
            Assert.NotNull(other.Result);
            Assert.IsTrue(r.IsLoaded);
            Assert.AreEqual(1, stub.AsyncLoadCalls);
        }

        [UnityTest]
        public IEnumerator Awaitable_PreCancelledToken_CancelsAwaitButLoadCompletes()
        {
            var stub = new StubLoader(delayFrames: 2);
            global::SmartReference.Runtime.SmartReference.InitWithCustomLoader(stub);

            var r = new SmartReference<SmartReferenceTestSO> { path = "stub://so" };

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var task = AsTask(r.LoadAsyncAwaitable(cts.Token));
            yield return WaitForTask(task);
            Assert.IsTrue(task.IsCanceled);

            // Let the underlying callback-based load finish.
            for (int i = 0; i < 4; i++)
                yield return null;

            Assert.IsTrue(r.IsLoaded);
            Assert.AreEqual(1, stub.AsyncLoadCalls);
        }

        [UnityTest]
        public IEnumerator Awaitable_AlreadyLoaded_ReturnsCompletedWithoutLoaderCall()
        {
            var stub = new StubLoader(delayFrames: 0);
            global::SmartReference.Runtime.SmartReference.InitWithCustomLoader(stub);

            var r = new SmartReference<SmartReferenceTestSO> { path = "stub://so" };
            var loaded = r.Value;

            var task = AsTask(r.LoadAsyncAwaitable());
            yield return WaitForTask(task);

            Assert.AreSame(loaded, task.Result);
            Assert.AreEqual(0, stub.AsyncLoadCalls);
        }

        [UnityTest]
        public IEnumerator Awaitable_ReleaseDuringLoading_CompletesWithNull()
        {
            var stub = new StubLoader(delayFrames: 3);
            global::SmartReference.Runtime.SmartReference.InitWithCustomLoader(stub);

            var r = new SmartReference<SmartReferenceTestSO> { path = "stub://so" };
            var task = AsTask(r.LoadAsyncAwaitable());

            r.Release();
            Assert.AreEqual(1, stub.CancelCalls);

            yield return WaitForTask(task);

            Assert.IsNull(task.Result);
            Assert.AreEqual(0, stub.ReleaseCalls);
        }

        [UnityTest]
        public IEnumerator Awaitable_SessionReset_DropsWaiters()
        {
            var stub = new StubLoader(delayFrames: 3);
            global::SmartReference.Runtime.SmartReference.InitWithCustomLoader(stub);

            var r = new SmartReference<SmartReferenceTestSO> { path = "stub://so" };
            var oldTask = AsTask(r.LoadAsyncAwaitable());

            // Simulate exiting and re-entering play mode mid-load
            global::SmartReference.Runtime.SmartReference.ResetStatics();
            global::SmartReference.Runtime.SmartReference.InitWithCustomLoader(stub);

            var newTask = AsTask(r.LoadAsyncAwaitable());
            yield return WaitForTask(newTask);

            // Let the old session's load complete
            for (int i = 0; i < 4; i++)
                yield return null;

            Assert.NotNull(newTask.Result);
            Assert.AreEqual(2, stub.AsyncLoadCalls);
            Assert.IsFalse(oldTask.IsCompleted);
        }

        [UnityTest]
        public IEnumerator Awaitable_ReentrantLoadFromContinuation_Works()
        {
            var stub = new StubLoader(delayFrames: 1);
            global::SmartReference.Runtime.SmartReference.InitWithCustomLoader(stub);

            var r = new SmartReference<SmartReferenceTestSO> { path = "stub://so" };

            // The first waiter's continuation releases and reloads while the other waiter is still being completed
            var reloaded = LoadReleaseAndReload(r);
            var other = AsTask(r.LoadAsyncAwaitable());

            yield return WaitForTask(other);
            yield return WaitForTask(reloaded);

            Assert.NotNull(other.Result);
            Assert.NotNull(reloaded.Result);
            Assert.AreEqual(2, stub.AsyncLoadCalls);
            Assert.AreEqual(1, stub.ReleaseCalls);
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
            
            Assert.AreEqual(stub.CancelCalls, 1);

            yield return WaitForTask(t);
            
            Assert.AreEqual(stub.ReleaseCalls, 0);
        }

        [UnityTest]
        public IEnumerator Release_DuringLoading_LoaderIgnoresCancel_CallersGetNull_AndAssetReleased()
        {
            var stub = new StubLoader(delayFrames: 2, honorCancel: false);
            global::SmartReference.Runtime.SmartReference.InitWithCustomLoader(stub);

            var r = new global::SmartReference.Runtime.SmartReference<SmartReferenceTestSO> { path = "stub://so" };
            var completeEvents = 0;
            SmartReferenceTestSO eventResult = null;
            r.OnAsyncLoadComplete += result =>
            {
                completeEvents++;
                eventResult = result;
            };
            var t = r.LoadAsyncTask();

            r.Release();
            Assert.IsTrue(r.ReleaseRequested);

            yield return WaitForTask(t);

            Assert.IsNull(t.Result);
            Assert.AreEqual(1, completeEvents);
            Assert.IsNull(eventResult);
            Assert.AreEqual(1, stub.ReleaseCalls);
            Assert.IsFalse(r.IsLoaded);
            Assert.IsFalse(r.IsLoading);
            Assert.IsFalse(r.ReleaseRequested);
            LogAssert.NoUnexpectedReceived();

            // The reference can be loaded again
            var t2 = r.LoadAsyncTask();
            yield return WaitForTask(t2);
            Assert.NotNull(t2.Result);
        }

        [UnityTest]
        public IEnumerator Release_ThenLoadAgain_DuringLoading_LoaderHonorsCancel_StartsNewLoad()
        {
            var stub = new StubLoader(delayFrames: 2);
            global::SmartReference.Runtime.SmartReference.InitWithCustomLoader(stub);

            var r = new global::SmartReference.Runtime.SmartReference<SmartReferenceTestSO> { path = "stub://so" };
            var completeEvents = 0;
            r.OnAsyncLoadComplete += _ => completeEvents++;

            // e.g. a UI opens, closes and reopens within one load
            var t1 = r.LoadAsyncTask();
            r.Release();
            var t2 = r.LoadAsyncTask();
            Assert.IsFalse(r.ReleaseRequested);
            Assert.AreSame(t1, t2);

            yield return WaitForTask(t2);

            Assert.NotNull(t2.Result);
            Assert.IsTrue(r.IsLoaded);
            Assert.IsFalse(r.IsLoading);
            Assert.AreEqual(1, completeEvents);
            Assert.AreEqual(2, stub.AsyncLoadCalls);
            Assert.AreEqual(0, stub.ReleaseCalls);
            LogAssert.NoUnexpectedReceived();

            r.Release();
            Assert.AreSame(stub.LastAsyncHandle, stub.LastReleasedHandle);
        }

        [UnityTest]
        public IEnumerator Release_ThenLoadAgain_DuringLoading_LoaderIgnoresCancel_KeepsAsset()
        {
            var stub = new StubLoader(delayFrames: 2, honorCancel: false);
            global::SmartReference.Runtime.SmartReference.InitWithCustomLoader(stub);

            var r = new global::SmartReference.Runtime.SmartReference<SmartReferenceTestSO> { path = "stub://so" };
            r.LoadAsync();
            r.Release();
            var t = r.LoadAsyncTask();

            yield return WaitForTask(t);

            Assert.NotNull(t.Result);
            Assert.IsTrue(r.IsLoaded);
            Assert.AreEqual(1, stub.AsyncLoadCalls);
            Assert.AreEqual(0, stub.ReleaseCalls);
        }

        [UnityTest]
        public IEnumerator Release_ThenLoadThenRelease_DuringLoading_CallersGetNull()
        {
            var stub = new StubLoader(delayFrames: 2);
            global::SmartReference.Runtime.SmartReference.InitWithCustomLoader(stub);

            var r = new global::SmartReference.Runtime.SmartReference<SmartReferenceTestSO> { path = "stub://so" };
            var t = r.LoadAsyncTask();
            r.Release();
            r.LoadAsync();
            r.Release();

            yield return WaitForTask(t);

            Assert.IsNull(t.Result);
            Assert.IsFalse(r.IsLoaded);
            Assert.IsFalse(r.IsLoading);
            Assert.AreEqual(1, stub.AsyncLoadCalls);
        }

        [UnityTest]
        public IEnumerator AsyncLoad_HandlerAddedBeforeFirstLoad_IsInvoked()
        {
            var stub = new StubLoader(delayFrames: 1);
            global::SmartReference.Runtime.SmartReference.InitWithCustomLoader(stub);

            // A fresh reference hasn't seen this play session yet, the handler must survive the session sync
            var r = new global::SmartReference.Runtime.SmartReference<SmartReferenceTestSO> { path = "stub://so" };
            SmartReferenceTestSO eventResult = null;
            r.OnAsyncLoadComplete += result => eventResult = result;
            r.LoadAsync();

            for (int i = 0; i < 3 && eventResult == null; i++)
                yield return null;

            Assert.NotNull(eventResult);
        }

        [Test]
        public void SyncLoad_LoaderReturnsWrongType_LogsErrorAndReleases()
        {
            var stub = new StubLoader(delayFrames: 0);
            global::SmartReference.Runtime.SmartReference.InitWithCustomLoader(stub);

            // The stub returns a ScriptableObject for types it doesn't know
            var r = new global::SmartReference.Runtime.SmartReference<Texture2D> { path = "stub://tex" };

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"\[SmartReference\] Loaded asset at path: stub://tex is .*SmartReferenceTestSO, expected UnityEngine\.Texture2D"));
            Assert.IsNull(r.Value);
            Assert.AreEqual(1, stub.ReleaseCalls);
        }

        // ----------------------------
        // 5a) Loader completes synchronously (StartCoroutine runs to its first yield immediately)
        // ----------------------------

        [Test]
        public void AsyncLoad_Task_LoaderCompletesSynchronously_ReturnsAsset()
        {
            var stub = new StubLoader(delayFrames: 0);
            global::SmartReference.Runtime.SmartReference.InitWithCustomLoader(stub);

            var r = new global::SmartReference.Runtime.SmartReference<SmartReferenceTestSO> { path = "stub://so" };
            var t = r.LoadAsyncTask();

            Assert.IsTrue(t.IsCompleted);
            Assert.NotNull(t.Result);
            Assert.IsTrue(r.IsLoaded);
            Assert.IsFalse(r.IsLoading);

            r.Release();
            Assert.AreSame(stub.LastAsyncHandle, stub.LastReleasedHandle);
        }

        [Test]
        public void AsyncLoad_LoaderCompletesSynchronouslyWithWrongType_ReleasesRealHandle()
        {
            var stub = new StubLoader(delayFrames: 0);
            global::SmartReference.Runtime.SmartReference.InitWithCustomLoader(stub);

            var r = new global::SmartReference.Runtime.SmartReference<Texture2D> { path = "stub://tex" };

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"\[SmartReference\] Loaded asset at path: stub://tex is .*SmartReferenceTestSO, expected UnityEngine\.Texture2D"));
            var t = r.LoadAsyncTask();

            Assert.IsTrue(t.IsCompleted);
            Assert.IsNull(t.Result);
            Assert.AreEqual(1, stub.ReleaseCalls);
            Assert.NotNull(stub.LastReleasedHandle);
            Assert.AreSame(stub.LastAsyncHandle, stub.LastReleasedHandle);

            // Nothing is left to release
            r.Release();
            Assert.AreEqual(1, stub.ReleaseCalls);
        }

#if SMARTREFERENCE_UNITASK_SUPPORT
        [UnityTest]
        public IEnumerator AsyncLoad_UniTask_LoaderCompletesSynchronously_ReturnsAsset()
        {
            var stub = new StubLoader(delayFrames: 0);
            global::SmartReference.Runtime.SmartReference.InitWithCustomLoader(stub);

            var r = new SmartReference<SmartReferenceTestSO> { path = "stub://so" };

            yield return r.LoadAsyncUniTask().ToCoroutine(result => Assert.NotNull(result));
            Assert.IsTrue(r.IsLoaded);
        }
#endif

#if UNITY_2023_2_OR_NEWER
        [UnityTest]
        public IEnumerator AsyncLoad_Awaitable_LoaderCompletesSynchronously_ReturnsAsset()
        {
            var stub = new StubLoader(delayFrames: 0);
            global::SmartReference.Runtime.SmartReference.InitWithCustomLoader(stub);

            var r = new SmartReference<SmartReferenceTestSO> { path = "stub://so" };

            var task = AsTask(r.LoadAsyncAwaitable());
            yield return WaitForTask(task);

            Assert.NotNull(task.Result);
            Assert.IsTrue(r.IsLoaded);
        }
#endif

        // ----------------------------
        // 5b) Resources release
        // ----------------------------

        [Test]
        public void Release_Prefab_Resources_NoErrors()
        {
            var r = new global::SmartReference.Runtime.SmartReference<GameObject> { path = AssetSetup.PrefabResourcesPath };
            Assert.NotNull(r.Value);

            r.Release();

            Assert.IsFalse(r.IsLoaded);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Release_Texture_Resources_Unloads()
        {
            var r = new global::SmartReference.Runtime.SmartReference<Texture2D> { path = AssetSetup.TextureResourcesPath };
            Assert.NotNull(r.Value);

            r.Release();

            Assert.IsFalse(r.IsLoaded);
            Assert.NotNull(r.Value);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Release_DuringLoading_Resources_TaskGetsNull()
        {
            var r = new global::SmartReference.Runtime.SmartReference<GameObject> { path = AssetSetup.PrefabResourcesPath };
            var t = r.LoadAsyncTask();

            r.Release();
            yield return WaitForTask(t);

            Assert.IsNull(t.Result);
            Assert.IsFalse(r.IsLoaded);
            LogAssert.NoUnexpectedReceived();
        }

        // ----------------------------
        // 6) Play session reset (Enter Play Mode with domain reload disabled)
        // ----------------------------

        [UnityTest]
        public IEnumerator SessionReset_DuringAsyncLoad_ClearsLoadingState()
        {
            var stub = new StubLoader(delayFrames: 3);
            global::SmartReference.Runtime.SmartReference.InitWithCustomLoader(stub);

            var r = new global::SmartReference.Runtime.SmartReference<SmartReferenceTestSO> { path = "stub://so" };
            var completeEvents = 0;
            r.OnAsyncLoadComplete += _ => completeEvents++;

            var oldTask = r.LoadAsyncTask();
            Assert.IsTrue(r.IsLoading);

            // Simulate exiting and re-entering play mode mid-load
            global::SmartReference.Runtime.SmartReference.ResetStatics();
            global::SmartReference.Runtime.SmartReference.InitWithCustomLoader(stub);

            Assert.IsFalse(r.IsLoading);

            var newTask = r.LoadAsyncTask();
            Assert.AreNotSame(oldTask, newTask);
            yield return WaitForTask(newTask);

            Assert.NotNull(newTask.Result);
            Assert.AreEqual(2, stub.AsyncLoadCalls);
            Assert.IsFalse(oldTask.IsCompleted);
            Assert.AreEqual(0, completeEvents);
        }

        [UnityTest]
        public IEnumerator SessionReset_LateCallbackFromOldSession_IsIgnored()
        {
            var stub = new StubLoader(delayFrames: 2);
            global::SmartReference.Runtime.SmartReference.InitWithCustomLoader(stub);

            var r = new global::SmartReference.Runtime.SmartReference<SmartReferenceTestSO> { path = "stub://so" };
            r.LoadAsync();

            global::SmartReference.Runtime.SmartReference.ResetStatics();
            global::SmartReference.Runtime.SmartReference.InitWithCustomLoader(stub);

            // Let the old session's load complete
            for (int i = 0; i < 4; i++)
                yield return null;

            Assert.IsFalse(r.IsLoaded);
            Assert.IsFalse(r.IsLoading);
        }

        [Test]
        public void SessionReset_ClearsCachedValue()
        {
            var oldStub = new StubLoader(delayFrames: 0);
            global::SmartReference.Runtime.SmartReference.InitWithCustomLoader(oldStub);

            var r = new global::SmartReference.Runtime.SmartReference<SmartReferenceTestSO> { path = "stub://so" };
            Assert.NotNull(r.Value);

            var newStub = new StubLoader(delayFrames: 0);
            global::SmartReference.Runtime.SmartReference.ResetStatics();
            global::SmartReference.Runtime.SmartReference.InitWithCustomLoader(newStub);

            Assert.IsFalse(r.IsLoaded);
            Assert.NotNull(r.Value);
            Assert.AreEqual(1, oldStub.LoadCalls);
            Assert.AreEqual(1, newStub.LoadCalls);
            // Stale handles are dropped, not released through a loader that doesn't own them
            Assert.AreEqual(0, oldStub.ReleaseCalls);
            Assert.AreEqual(0, newStub.ReleaseCalls);
        }

        [Test]
        public void SessionReset_ClearsLoader()
        {
            global::SmartReference.Runtime.SmartReference.ResetStatics();

            var r = new global::SmartReference.Runtime.SmartReference<SmartReferenceTestSO>
            {
                path = AssetSetup.SoResourcesPath
            };

            LogAssert.Expect(LogType.Error, "[SmartReference] Loader is null, please init the SmartReference with a loader before using it.");
            _ = r.Value;
        }

        // ----------------------------
        // 7) Performance (PlayMode microbenchmarks)
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

#if UNITY_2023_2_OR_NEWER
        // Each Awaitable is awaited exactly once, here
        private static async Task<T> AsTask<T>(Awaitable<T> awaitable)
        {
            return await awaitable;
        }

        private static async Task<T> LoadReleaseAndReload<T>(SmartReference<T> r) where T : Object
        {
            var first = await r.LoadAsyncAwaitable();
            Assert.NotNull(first);
            r.Release();
            return await r.LoadAsyncAwaitable();
        }
#endif

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
            private readonly bool honorCancel;
            public int LoadCalls { get; private set; }
            public int AsyncLoadCalls { get; private set; }
            public int ReleaseCalls { get; private set; }
            public int CancelCalls { get; private set; }
            public ISmartReferenceHandle LastAsyncHandle { get; private set; }
            public ISmartReferenceHandle LastReleasedHandle { get; private set; }

            public StubLoader(int delayFrames, bool honorCancel = true)
            {
                this.delayFrames = Mathf.Max(0, delayFrames);
                this.honorCancel = honorCancel;
            }

            public ISmartReferenceHandle Load(string path, Type type, out Object loadedObject)
            {
                LoadCalls++;
                loadedObject = CreateObject(type);
                return new H();
            }

            public global::SmartReference.Runtime.ISmartReferenceHandle LoadAsync(string path, Type type, Action<Object> onComplete)
            {
                AsyncLoadCalls++;
                var h = new H();
                LastAsyncHandle = h;
                CoroutineRunner.Ensure().StartCoroutine(DelayedComplete(h, type, onComplete));
                return h;
            }

            public void Release(global::SmartReference.Runtime.ISmartReferenceHandle handle)
            {
                ReleaseCalls++;
                LastReleasedHandle = handle;
                // In a real loader, you'd release handle/asset (Addressables.Release, etc.)
            }

            public void Cancel(global::SmartReference.Runtime.ISmartReferenceHandle handle)
            {
                CancelCalls++;
                if (honorCancel && handle is H h) h.canceled = true;
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
