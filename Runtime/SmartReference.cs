using System;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_2023_2_OR_NEWER
using System.Collections.Generic;
#endif

#if SMARTREFERENCE_UNITASK_SUPPORT || UNITY_2023_2_OR_NEWER
using System.Threading;
#endif

#if SMARTREFERENCE_UNITASK_SUPPORT
using Cysharp.Threading.Tasks;
#endif

// ReSharper disable NotAccessedField.Global used in serialization
// ReSharper disable ParameterHidesMember

namespace SmartReference.Runtime
{
    [Serializable]
    public abstract class SmartReference
    {
        public string guid;
        public long fileID;
        public string path;
        public string type;

        #region Statics
        
        protected static ISmartReferenceLoader Loader;

        /// <summary>
        /// Incremented at the start of every play session. Instances compare against it to detect state
        /// left over from a previous session when domain reload is disabled.
        /// </summary>
        protected static int Session;

        /// <summary>
        /// Reset static state at the start of each play session, so it works with domain reload disabled.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetStatics()
        {
            Loader = null;
            Session++;
        }

        /// <summary>
        /// Use this method to initialize the loader if you want to use Resources for loading assets.
        /// </summary>
        public static void InitWithResourcesLoader()
        {
            Loader = new ResourcesLoader();
        }
        
#if SMARTREFERENCE_ADDRESSABLES_SUPPORT
        /// <summary>
        /// Use this method to initialize the loader if you want to use Unity Addressables for loading assets.
        /// </summary>
        public static void InitWithAddressablesLoader()
        {
            Loader = new AddressablesLoader();
        }
#endif
        
        /// <summary>
        /// Use this method to initialize the loader with a custom implementation of ISmartReferenceLoader.
        /// </summary>
        /// <param name="loader">The custom loader implementation.</param>
        public static void InitWithCustomLoader(ISmartReferenceLoader loader)
        {
            Loader = loader;
        }
        
        #endregion
    }
    
    [Serializable]
    public class SmartReference<T>: SmartReference, ISerializationCallbackReceiver
        where T: Object
    {
        [NonSerialized] private T value;
        
        [NonSerialized] private Action<T> onAsyncLoadComplete;

        /// <summary>
        /// Event invoked when the asynchronous load is complete.
        /// </summary>
        public event Action<T> OnAsyncLoadComplete
        {
            add {
                EnsureCurrentSession();
                onAsyncLoadComplete += value;
            }
            remove {
                EnsureCurrentSession();
                onAsyncLoadComplete -= value;
            }
        }
        
        [NonSerialized] private bool isLoading;
        [NonSerialized] private TaskCompletionSource<T> inFlightTaskTcs;
#if SMARTREFERENCE_UNITASK_SUPPORT
        [NonSerialized] private UniTaskCompletionSource<T> inFlightUniTaskTcs;
#endif
#if UNITY_2023_2_OR_NEWER
        // An Awaitable can only be awaited once, so each caller gets its own waiter
        [NonSerialized] private List<AwaitableWaiter> awaitableWaiters;
        [NonSerialized] private List<AwaitableWaiter> spareAwaitableWaiters;
#endif

        [NonSerialized] private ISmartReferenceHandle handle;
        [NonSerialized] private bool releaseRequested;
        // Loader.Cancel was called on the in-flight load. It stays set when a new load request clears releaseRequested.
        [NonSerialized] private bool cancelIssued;
#if UNITY_EDITOR
        [NonSerialized] private int session;
#endif

        /// <summary>
        /// Check if the asset is loaded.
        /// </summary>
        public bool IsLoaded
        {
            get {
                EnsureCurrentSession();
                return value != null;
            }
        }

        /// <summary>
        /// Check if the asset is loading asynchronously.
        /// </summary>
        public bool IsLoading
        {
            get {
                EnsureCurrentSession();
                return isLoading;
            }
        }

        /// <summary>
        /// Check if a release has been requested while loading.
        /// </summary>
        public bool ReleaseRequested
        {
            get {
                EnsureCurrentSession();
                return releaseRequested;
            }
        }

        /// <summary>
        /// Get the asset. If the asset is not loaded, it will be loaded automatically.
        /// </summary>
        public T Value
        {
            get {
                EnsureCurrentSession();
                if (value == null) {
                    Load();
                }

                return value;
            }
        }

        public static implicit operator T(SmartReference<T> reference)
        {
            return reference.Value;
        }

        /// <summary>
        /// Call this method to load the asset. This would be called automatically when you access the Value property.
        /// </summary>
        public void Load()
        {
            EnsureCurrentSession();

            if (string.IsNullOrEmpty(path))
            {
                LogEmptyAssetError();
                return;
            }
            
            if (Loader == null)
            {
                LogEmptyLoaderError();
                return;
            }

            if (isLoading)
            {
                Debug.LogWarning($"[SmartReference] Asset at path: {path} is currently loading asynchronously. Please wait for the OnAsyncLoadComplete event.");
                return;
            }
            
            handle = Loader.Load(path, typeof(T), out var result);
            value = CastOrRelease(result);
            if (value == null && result == null)
            {
                LogLoadAssetNullError();
            }
        }
        
        /// <summary>
        /// Call this method to load the asset asynchronously. You can subscribe to the OnAsyncLoadComplete event to get notified when the load is complete.
        /// </summary>
        public void LoadAsync()
        {
            EnsureCurrentSession();

            if (value != null)
            {
                return;
            }

            if (string.IsNullOrEmpty(path))
            {
                LogEmptyAssetError();
                return;
            }

            if (Loader == null)
            {
                LogEmptyLoaderError();
                return;
            }

            // If already loading, keep the in-flight load.
            if (isLoading)
            {
                releaseRequested = false;
                return;
            }

            isLoading = true;
            releaseRequested = false;
            cancelIssued = false;
            var loadSession = Session;

            // A loader may complete synchronously, before its handle is returned. Defer the callback until
            // handle is assigned, so a release on that path releases the real handle.
            var returned = false;
            var completedSynchronously = false;
            Object syncResult = null;
            handle = Loader.LoadAsync(path, typeof(T), obj =>
            {
                if (!returned)
                {
                    completedSynchronously = true;
                    syncResult = obj;
                    return;
                }

                OnAsyncLoadCompleteCallback(obj, loadSession);
            });
            returned = true;

            if (completedSynchronously)
            {
                OnAsyncLoadCompleteCallback(syncResult, loadSession);
            }
        }
        
        /// <summary>
        /// Load the asset asynchronously with C# async/await.
        /// </summary>
        public Task<T> LoadAsyncTask()
        {
            EnsureCurrentSession();

            if (value != null)
            {
                return Task.FromResult(value);
            }

            if (string.IsNullOrEmpty(path))
            {
                LogEmptyAssetError();
                return Task.FromResult<T>(null);
            }

            if (Loader == null)
            {
                LogEmptyLoaderError();
                return Task.FromResult<T>(null);
            }

            if (inFlightTaskTcs != null)
            {
                // Still call LoadAsync, so a release requested during the load is overridden
                var inFlightTask = inFlightTaskTcs.Task;
                LoadAsync();
                return inFlightTask;
            }

            var tcs = new TaskCompletionSource<T>();
            inFlightTaskTcs = tcs;
            LoadAsync();
            return tcs.Task;
        }
        
#if SMARTREFERENCE_UNITASK_SUPPORT
        
        /// <summary>
        /// Load the asset asynchronously with UniTask.
        /// </summary>
        public UniTask<T> LoadAsyncUniTask()
        {
            return LoadAsyncUniTask(CancellationToken.None);
        }

        /// <summary>
        /// Load the asset asynchronously with UniTask and cancellation token.
        /// </summary>
        public UniTask<T> LoadAsyncUniTask(CancellationToken cancellationToken)
        {
            EnsureCurrentSession();

            if (value != null)
            {
                return UniTask.FromResult(value);
            }

            if (string.IsNullOrEmpty(path))
            {
                LogEmptyAssetError();
                return UniTask.FromResult<T>(null);
            }

            if (Loader == null)
            {
                LogEmptyLoaderError();
                return UniTask.FromResult<T>(null);
            }

            if (inFlightUniTaskTcs != null)
            {
                // Still call LoadAsync, so a release requested during the load is overridden
                var inFlightTask = inFlightUniTaskTcs.Task;
                LoadAsync();
                return inFlightTask.AttachExternalCancellation(cancellationToken);
            }

            var utcs = new UniTaskCompletionSource<T>();
            inFlightUniTaskTcs = utcs;
            LoadAsync();
            return utcs.Task.AttachExternalCancellation(cancellationToken);
        }

#endif

#if UNITY_2023_2_OR_NEWER

        /// <summary>
        /// Load the asset asynchronously with Unity's Awaitable.
        /// Every call returns a new Awaitable, which must only be awaited once.
        /// </summary>
        public Awaitable<T> LoadAsyncAwaitable()
        {
            return LoadAsyncAwaitable(CancellationToken.None);
        }

        /// <summary>
        /// Load the asset asynchronously with Unity's Awaitable and cancellation token.
        /// Every call returns a new Awaitable, which must only be awaited once.
        /// Cancelling the token only cancels this caller's await, the load itself and other callers continue.
        /// Cancel the token from the main thread.
        /// </summary>
        public Awaitable<T> LoadAsyncAwaitable(CancellationToken cancellationToken)
        {
            EnsureCurrentSession();

            if (value != null)
            {
                return AwaitableWaiter.FromResult(value);
            }

            if (string.IsNullOrEmpty(path))
            {
                LogEmptyAssetError();
                return AwaitableWaiter.FromResult(null);
            }

            if (Loader == null)
            {
                LogEmptyLoaderError();
                return AwaitableWaiter.FromResult(null);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                LoadAsync();
                return AwaitableWaiter.FromCanceled();
            }

            var waiter = AwaitableWaiter.Rent(this);
            // Capture before LoadAsync, a loader may complete synchronously and recycle the waiter
            var awaitable = waiter.Source.Awaitable;
            if (awaitableWaiters == null)
            {
                awaitableWaiters = spareAwaitableWaiters ?? new List<AwaitableWaiter>();
                spareAwaitableWaiters = null;
            }

            awaitableWaiters.Add(waiter);
            waiter.Register(cancellationToken);
            LoadAsync();
            return awaitable;
        }

        /// <summary>
        /// Pooled per-caller completion source.
        /// </summary>
        private sealed class AwaitableWaiter
        {
            private const int MaxPoolSize = 32;
            private static readonly Stack<AwaitableWaiter> Pool = new Stack<AwaitableWaiter>();

            public readonly AwaitableCompletionSource<T> Source = new AwaitableCompletionSource<T>();
            private SmartReference<T> owner;
            private CancellationTokenRegistration registration;

            public static AwaitableWaiter Rent(SmartReference<T> owner)
            {
                var waiter = Pool.Count > 0 ? Pool.Pop() : new AwaitableWaiter();
                waiter.owner = owner;
                return waiter;
            }

            public static Awaitable<T> FromResult(T result)
            {
                var waiter = Rent(null);
                waiter.Source.SetResult(result);
                var awaitable = waiter.Source.Awaitable;
                waiter.Return();
                return awaitable;
            }

            public static Awaitable<T> FromCanceled()
            {
                var waiter = Rent(null);
                waiter.Source.SetCanceled();
                var awaitable = waiter.Source.Awaitable;
                waiter.Return();
                return awaitable;
            }

            public void Register(CancellationToken cancellationToken)
            {
                if (!cancellationToken.CanBeCanceled) return;

                registration = cancellationToken.Register(static state => ((AwaitableWaiter) state).Cancel(), this);
            }

            public void Complete(T result)
            {
                Source.TrySetResult(result);
                Return();
            }

            private void Cancel()
            {
                Source.TrySetCanceled();
                
                if (owner?.awaitableWaiters != null && owner.awaitableWaiters.Remove(this))
                {
                    Return();
                }
            }

            private void Return()
            {
                registration.Dispose();
                registration = default;
                owner = null;
                Source.Reset();
                if (Pool.Count < MaxPoolSize)
                {
                    Pool.Push(this);
                }
            }
        }

#endif

        /// <summary>
        /// Release the loaded asset.
        /// Safe to call multiple times.
        /// If called during loading, attempts to cancel the loading.
        /// </summary>
        public void Release()
        {
            EnsureCurrentSession();

            if (Loader == null)
            {
                LogEmptyLoaderError();
                return;
            }

            if (isLoading)
            {
                releaseRequested = true;
                cancelIssued = true;
                try
                {
                    Loader.Cancel(handle);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SmartReference] Cancel loading failed for asset at path: {path}. Exception: {e}");
                }
                
                return;
            }

            if (value == null && handle == null)
            {
                return;
            }

            try
            {
                Loader.Release(handle);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SmartReference] Release failed for asset at path: {path}. Exception: {e}");
            }
            finally
            {
                value = null;
                handle = null;
                releaseRequested = false;
            }
        }
        
        private void CompleteInFlight(T result)
        {
            onAsyncLoadComplete?.Invoke(result);
            
            // Task
            if (inFlightTaskTcs != null)
            {
                var tcs = inFlightTaskTcs;
                inFlightTaskTcs = null;
                tcs.TrySetResult(result);
            }

#if SMARTREFERENCE_UNITASK_SUPPORT
            // UniTask
            if (inFlightUniTaskTcs != null)
            {
                var utcs = inFlightUniTaskTcs;
                inFlightUniTaskTcs = null;
                utcs.TrySetResult(result);
            }
#endif

#if UNITY_2023_2_OR_NEWER
            // Awaitable
            if (awaitableWaiters != null)
            {
                var waiters = awaitableWaiters;
                awaitableWaiters = null;
                for (var i = 0; i < waiters.Count; i++)
                {
                    waiters[i].Complete(result);
                }

                waiters.Clear();
                spareAwaitableWaiters = waiters;
            }
#endif
        }

        /// <summary>
        /// Drop state cached in a previous play session. With domain reload disabled, non-serialized fields
        /// survive between sessions on assets that stay in memory.
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void EnsureCurrentSession()
        {
#if UNITY_EDITOR
            if (session == Session) return;

            session = Session;
            value = null;
            handle = null;
            isLoading = false;
            releaseRequested = false;
            cancelIssued = false;
            inFlightTaskTcs = null;
#if SMARTREFERENCE_UNITASK_SUPPORT
            inFlightUniTaskTcs = null;
#endif
#if UNITY_2023_2_OR_NEWER
            awaitableWaiters = null;
#endif
            onAsyncLoadComplete = null;
#endif
        }

        private void OnAsyncLoadCompleteCallback(Object obj, int loadSession)
        {
            // Completion of a load started in a previous play session, ignore it.
            if (loadSession != Session) return;

            isLoading = false;

            // Release was called during loading. Callers get null rather than an asset that is released right away.
            if (releaseRequested)
            {
                releaseRequested = false;
                cancelIssued = false;
                if (obj != null)
                {
                    ReleaseHandle();
                }

                handle = null;
                CompleteInFlight(null);
                return;
            }

            // Released and then requested again during loading.
            if (cancelIssued)
            {
                cancelIssued = false;
                if (obj == null)
                {
                    handle = null;
                    LoadAsync();
                    return;
                }
            }

            if (obj == null)
            {
                CompleteInFlight(null);
                LogLoadAssetNullError();
                return;
            }

            value = CastOrRelease(obj);
            CompleteInFlight(value);
        }

        /// <summary>
        /// Cast a loaded object to T. If the loader returned another type, log an error and release it.
        /// </summary>
        private T CastOrRelease(Object obj)
        {
            if (obj == null || obj is T) return (T) obj;

            Debug.LogError($"[SmartReference] Loaded asset at path: {path} is {obj.GetType()}, expected {typeof(T)}");
            ReleaseHandle();
            handle = null;
            return null;
        }

        private void ReleaseHandle()
        {
            try
            {
                Loader.Release(handle);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SmartReference] Release failed for asset at path: {path}. Exception: {e}");
            }
        }
        
        private void LogEmptyAssetError()
        {
            Debug.LogError($"[SmartReference] Asset path is null.");
        }
        
        private void LogEmptyLoaderError()
        {
            Debug.LogError($"[SmartReference] Loader is null, please init the SmartReference with a loader before using it.");
        }
        
        private void LogLoadAssetNullError()
        {
            Debug.LogError($"[SmartReference] Loaded asset is null, path: {path}");
        }

        public void OnBeforeSerialize()
        {
            type = typeof(T).AssemblyQualifiedName;
        }

        public void OnAfterDeserialize()
        {
            
        }
    }
}