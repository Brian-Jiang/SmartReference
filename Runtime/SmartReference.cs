using System;
using UnityEngine;
using Object = UnityEngine.Object;
// ReSharper disable NotAccessedField.Global used in serialization

namespace SmartReference.Runtime {
    [Serializable]
    public abstract class SmartReference {
        public string guid;
        public long fileID;
        public string path;

        protected static ISmartReferenceLoader loader;
        
        /// <summary>
        /// Use this method to initialize the loader if you want to use Resources for loading assets.
        /// </summary>
        public static void InitWithResourcesLoader() {
            loader = new ResourcesLoader();
        }
        
#if USE_UNITY_ADDRESSABLES
        /// <summary>
        /// Use this method to initialize the loader if you want to use Unity Addressables for loading assets.
        /// </summary>
        public static void InitWithAddressablesLoader() {
            loader = new AddressablesLoader();
        }
#endif
        
        /// <summary>
        /// Use this method to initialize the loader if you want to use a custom loader.
        /// </summary>
        /// <param name="loader">Loader that load and return asset synchronously.</param>
        /// <param name="loaderAsync">Loader that load and return asset asynchronously.</param>
        public static void InitWithCustomLoader(SmartReferenceLoader loader, SmartReferenceLoaderAsync loaderAsync) {
            SmartReference.loader = new CustomLoader {
                loader = loader,
                loaderAsync = loaderAsync,
            };
        }
        
        /// <summary>
        /// Use this method to initialize the loader if you want to use a custom loader.
        /// </summary>
        /// <param name="loader">The custom loader that you create.</param>
        public static void InitWithCustomLoader(CustomLoader loader) {
            SmartReference.loader = loader;
        }
    }
    
    [Serializable]
    public class SmartReference<T>: SmartReference, ISerializationCallbackReceiver where T: Object {
        [SerializeField]
        private string type;

        private T value;
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
            if (string.IsNullOrEmpty(path)) return;
            
            if (loader == null) {
                Debug.LogError($"[SmartReference] loader is null, path: {path}");
                return;
            }
            
            value = (T) loader.Load(path, typeof(T));
            if (value == null) {
                Debug.LogWarning($"[SmartReference] load failed, path: {path}");
            }
        }
        
        /// <summary>
        /// Call this method to load the asset asynchronously. Useful if you want to preload the asset.
        /// </summary>
        public void LoadAsync() {
            if (string.IsNullOrEmpty(path)) return;
            
            if (loader == null) {
                Debug.LogError($"[SmartReference] loaderAsync is null, path: {path}");
                return;
            }
            
            loader.LoadAsync(path, typeof(T), obj => {
                if (obj == null) {
                    Debug.LogWarning($"[SmartReference] loadAsync failed, path: {path}");
                }
                
                value = (T) obj;
            });
        }

        public void OnBeforeSerialize() {
            type = typeof(T).AssemblyQualifiedName;
        }

        public void OnAfterDeserialize() {
            
        }
    }
}