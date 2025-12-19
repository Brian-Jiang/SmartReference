using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

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
        
        public static void InitWithCustomLoader(ISmartReferenceLoader loader)
        {
            Loader = loader;
        }
        
        #endregion
    }
    
    [Serializable]
    public class SmartReference<T>: SmartReference, ISerializationCallbackReceiver where T: Object
    {
        [NonSerialized] private T value;
        
        // public event Action<T> OnAsyncLoadComplete; 
        
        [NonSerialized] private bool isLoading;
        [NonSerialized] private TaskCompletionSource<T> inFlightTaskTcs;
#if SMARTREFERENCE_UNITASK_SUPPORT
        [NonSerialized] private UniTaskCompletionSource<T> inFlightUniTaskTcs;
#endif
        
        // NEW: track loader handle for release/cancel.
        [NonSerialized] private ISmartReferenceHandle handle;

        // NEW: if disposed while loading, we’ll release after completion.
        [NonSerialized] private bool releaseRequested;
        
        public bool IsLoaded => value != null;
        public bool IsLoading => isLoading;

        
        /// <summary>
        /// Get the asset. If the asset is not loaded, it will be loaded automatically.
        /// </summary>
        public T Value {
            get {
                if (value == null) {
                    Load();
                }

                return value;
            }
        }

        public static implicit operator T(SmartReference<T> reference) {
            return reference.Value;
        }

        /// <summary>
        /// Call this method to load the asset. This would be called automatically when you access the Value property.
        /// </summary>
        public void Load() {
            if (string.IsNullOrEmpty(path))
            {
                LogEmptyAssetError();
                return;
            }
            
            if (Loader == null) {
                LogEmptyLoaderError();
                return;
            }
            
            value = (T) Loader.Load(path, typeof(T));
            if (value == null) {
                LogLoadAssetNullError();
            }
        }
        
        /// <summary>
        /// Call this method to load the asset asynchronously. Useful if you want to preload the asset.
        /// </summary>
        public void LoadAsync()
        {
            if (value != null)
            {
                // OnAsyncLoadComplete?.Invoke(value);
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

            // If already loading, do nothing (caller can subscribe to event or await task/unitask).
            if (isLoading) return;

            isLoading = true;
            releaseRequested = false;
            handle = Loader.LoadAsync(path, typeof(T), OnAsyncLoadCompleteCallback);
        }
        
        /// <summary>
        /// Load the asset asynchronously with async/await.
        /// </summary>
        public Task<T> LoadAsyncTask()
        {
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

            // Share the same in-flight request.
            if (inFlightTaskTcs != null)
            {
                return inFlightTaskTcs.Task;
            }

            inFlightTaskTcs = new TaskCompletionSource<T>();
            LoadAsync(); // will complete TCS via CompleteInFlight(...)
            return inFlightTaskTcs.Task;
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
        /// Load the asset asynchronously with UniTask + cancellation token.
        /// Note: since your loader is callback-based, cancellation here only cancels the awaiting side.
        /// If you want true cancel (e.g., Addressables handle release), extend ISmartReferenceLoader to support it.
        /// </summary>
        public UniTask<T> LoadAsyncUniTask(CancellationToken cancellationToken)
        {
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

            // Share in-flight request.
            if (inFlightUniTaskTcs != null)
            {
                return inFlightUniTaskTcs.Task.AttachExternalCancellation(cancellationToken);
            }

            inFlightUniTaskTcs = new UniTaskCompletionSource<T>();
            LoadAsync(); // will complete UniTask TCS via CompleteInFlight(...)
            return inFlightUniTaskTcs.Task.AttachExternalCancellation(cancellationToken);
        }
        
#endif
        
        /// <summary>
        /// NEW: Release/unload the loaded asset (and any loader handle).
        /// Safe to call multiple times.
        /// If called during loading, attempts cancel + releases upon completion (best-effort).
        /// </summary>
        public void Release()
        {
            // if no loader, just clear references
            if (Loader == null)
            {
                LogEmptyLoaderError();
                return;
            }

            // If loading, request release; try cancel if supported.
            if (isLoading)
            {
                releaseRequested = true;
                try
                {
                    Loader.Cancel(handle);
                }
                catch
                {
                    // ignore - cancel may not be supported
                }
                return;
            }

            if (value == null && handle == null)
            {
                return;
            }

            try
            {
                Loader.Release(handle, value);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SmartReference] Release failed for path: {path}. Exception: {e}");
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
        }

        private void OnAsyncLoadCompleteCallback(Object obj)
        {
            isLoading = false;
            if (obj == null)
            {
                CompleteInFlight(null);
                if (releaseRequested)
                {
                    Release();
                    return;
                }
                
                LogLoadAssetNullError();
                
                return;
            }

            value = (T) obj;
            // OnAsyncLoadComplete?.Invoke(value);
            CompleteInFlight(value);
            
            // If Dispose/Release was called during loading, release immediately after completion.
            if (releaseRequested)
            {
                Release();
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

        public void OnBeforeSerialize() {
            type = typeof(T).AssemblyQualifiedName;
        }

        public void OnAfterDeserialize() {
            
        }
    }
}