#if SMARTREFERENCE_ADDRESSABLES_SUPPORT

using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace SmartReference.Runtime
{
    public class AddressablesLoader: ISmartReferenceLoader
    {
        public ISmartReferenceHandle Load(string path, Type type, out Object loadedObject)
        {
            var handle = Addressables.LoadAssetAsync<Object>(path);
            
#if !UNITY_WEBGL
            handle.WaitForCompletion();
#else
            UnityEngine.Debug.LogError("AddressablesLoader.Load called in WebGL build; synchronous loading is not supported. Consider using LoadAsync instead.");
#endif

            loadedObject = handle.Result;
            var h = new AddressablesHandle { op = handle };
            return h;
        }

        public ISmartReferenceHandle LoadAsync(string path, Type type, Action<Object> callback)
        {
            var handle = Addressables.LoadAssetAsync<Object>(path);
            handle.Completed += operation =>
            {
                var result = operation.Status == AsyncOperationStatus.Succeeded ? operation.Result : null;
                callback?.Invoke(result);
            };
            
            var h = new AddressablesHandle { op = handle };
            return h;
        }

        public void Release(ISmartReferenceHandle handle)
        {
            if (handle is AddressablesHandle { IsValid: true } addressablesHandle)
            {
                Addressables.Release(addressablesHandle.op);
                addressablesHandle.op = default;
            }
        }

        public void Cancel(ISmartReferenceHandle handle)
        {
            
        }
    }
}

#endif