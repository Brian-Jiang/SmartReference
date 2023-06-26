#if USE_UNITY_ADDRESSABLES

using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace SmartReference.Runtime {
    public class AddressablesLoader: ISmartReferenceLoader {
        public Object Load(string path, Type type) {
            Object result = null;
            var handle = Addressables.LoadAssetAsync<Object>(path);
            handle.Completed += operation => {
                if (operation.Status == AsyncOperationStatus.Succeeded) {
                    result = operation.Result;
                }
            };
            
#if !UNITY_WEBGL
            handle.WaitForCompletion();
#endif
            return result;
        }

        public void LoadAsync(string path, Type type, Action<Object> callback) {
            var handle = Addressables.LoadAssetAsync<Object>(path);
            handle.Completed += operation => {
                if (operation.Status == AsyncOperationStatus.Succeeded) {
                    callback?.Invoke(operation.Result);
                }
            };
        }
    }
}

#endif