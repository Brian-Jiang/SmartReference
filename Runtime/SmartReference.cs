using System;
using UnityEngine;
using Object = UnityEngine.Object;
// ReSharper disable NotAccessedField.Global used in serialization

namespace SmartReference.Runtime {
    public delegate Object SmartReferenceLoader(string path, Type type);
    public delegate void SmartReferenceLoaderAsync(string path, Type type, Action<Object> callback);
    
    [Serializable]
    public abstract class SmartReference {
        public string guid;
        public long fileID;
        public string path;

        protected static SmartReferenceLoader loader;
        protected static SmartReferenceLoaderAsync loaderAsync;
        public static void Init(SmartReferenceLoader mLoader, SmartReferenceLoaderAsync mLoaderAsync) {
            loader = mLoader;
            loaderAsync = mLoaderAsync;
        }
    }
    
    [Serializable]
    public class SmartReference<T>: SmartReference, ISerializationCallbackReceiver where T: Object {
        public string type;

        private T value;
        public T Value {
            get {
                if (value == null) {
                    Load();
                }

                return value;
            }
        }

// #if UNITY_EDITOR
//         public SmartReference() {
//             type = typeof(T).AssemblyQualifiedName;
//         }
// #endif
        
        public static implicit operator T(SmartReference<T> reference) {
            return reference.Value;
        }

        public void Load() {
            if (string.IsNullOrEmpty(path)) return;
            
            if (loader == null) {
                Debug.LogError($"[SmartReference] loader is null, path: {path}");
                return;
            }
            
            value = (T) loader(path, typeof(T));
            if (value == null) {
                Debug.LogWarning($"[SmartReference] load failed, path: {path}");
            }
        }
        
        public void LoadAsync() {
            if (string.IsNullOrEmpty(path)) return;
            
            if (loaderAsync == null) {
                Debug.LogError($"[SmartReference] loaderAsync is null, path: {path}");
                return;
            }
            
            loaderAsync(path, typeof(T), obj => {
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