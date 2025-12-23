using System;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

#if SMARTREFERENCE_UNITASK_SUPPORT
using System.Threading;
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
        
        /// <summary>
        /// Event invoked when the asynchronous load is complete.
        /// </summary>
        public event Action<T> OnAsyncLoadComplete; 
        
        [NonSerialized] private bool isLoading;
        [NonSerialized] private TaskCompletionSource<T> inFlightTaskTcs;
#if SMARTREFERENCE_UNITASK_SUPPORT
        [NonSerialized] private UniTaskCompletionSource<T> inFlightUniTaskTcs;
#endif
        
        [NonSerialized] private ISmartReferenceHandle handle;
        [NonSerialized] private bool releaseRequested;
        
        /// <summary>
        /// Check if the asset is loaded.
        /// </summary>
        public bool IsLoaded => value != null;
        
        /// <summary>
        /// Check if the asset is loading asynchronously.
        /// </summary>
        public bool IsLoading => isLoading;
        
        /// <summary>
        /// Check if a release has been requested while loading.
        /// </summary>
        public bool ReleaseRequested => releaseRequested;

        /// <summary>
        /// Get the asset. If the asset is not loaded, it will be loaded automatically.
        /// </summary>
        public T Value
        {
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
        public void Load()
        {
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
        /// Call this method to load the asset asynchronously. You can subscribe to the OnAsyncLoadComplete event to get notified when the load is complete.
        /// </summary>
        public void LoadAsync()
        {
            if (value != null)
            {
                OnAsyncLoadComplete?.Invoke(value);
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

            // If already loading, do nothing
            if (isLoading) return;

            isLoading = true;
            releaseRequested = false;
            handle = Loader.LoadAsync(path, typeof(T), OnAsyncLoadCompleteCallback);
        }
        
        /// <summary>
        /// Load the asset asynchronously with C# async/await.
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

            if (inFlightTaskTcs != null)
            {
                return inFlightTaskTcs.Task;
            }

            inFlightTaskTcs = new TaskCompletionSource<T>();
            LoadAsync();
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
        /// Load the asset asynchronously with UniTask and cancellation token.
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

            if (inFlightUniTaskTcs != null)
            {
                return inFlightUniTaskTcs.Task.AttachExternalCancellation(cancellationToken);
            }

            inFlightUniTaskTcs = new UniTaskCompletionSource<T>();
            LoadAsync();
            return inFlightUniTaskTcs.Task.AttachExternalCancellation(cancellationToken);
        }
        
#endif
        
        /// <summary>
        /// Release the loaded asset.
        /// Safe to call multiple times.
        /// If called during loading, attempts cancel the loading.
        /// </summary>
        public void Release()
        {
            if (Loader == null)
            {
                LogEmptyLoaderError();
                return;
            }

            if (isLoading)
            {
                releaseRequested = true;
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
            OnAsyncLoadComplete?.Invoke(value);
            
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
                if (releaseRequested) return;
                
                LogLoadAssetNullError();
                return;
            }

            value = (T) obj;
            CompleteInFlight(value);
            
            // If Release was called during loading, release immediately after completion.
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

        public void OnBeforeSerialize()
        {
            type = typeof(T).AssemblyQualifiedName;
        }

        public void OnAfterDeserialize()
        {
            
        }
    }
}