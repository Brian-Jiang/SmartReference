#if SMARTREFERENCE_ADDRESSABLES_SUPPORT

using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace SmartReference.Runtime
{
    public class AddressablesLoader: ISmartReferenceLoader
    {
        public Object Load(string path, Type type)
        {
            Object result = null;
            var handle = Addressables.LoadAssetAsync<Object>(path);
            handle.Completed += operation =>
            {
                if (operation.Status == AsyncOperationStatus.Succeeded)
                {
                    result = operation.Result;
                }
            };
            
#if !UNITY_WEBGL
            handle.WaitForCompletion();
#else
            UnityEngine.Debug.LogError("AddressablesLoader.Load called in WebGL build; synchronous loading is not supported. Consider using LoadAsync instead.");
#endif
            return result;
        }

        public ISmartReferenceHandle LoadAsync(string path, Type type, Action<Object> callback)
        {
            var handle = Addressables.LoadAssetAsync<Object>(path);
            handle.Completed += operation =>
            {
                if (operation.Status == AsyncOperationStatus.Succeeded)
                {
                    callback?.Invoke(operation.Result);
                }
            };
            var h = new AddressablesHandle { op = handle };
            return h;
        }

        public void Release(ISmartReferenceHandle handle)
        {
            if (handle is AddressablesHandle { IsValid: true } addressablesHandle)
            {
                Addressables.Release(addressablesHandle.op);
            }
        }

        public void Cancel(ISmartReferenceHandle handle)
        {
            
        }
    }
}

#endif