#if SMARTREFERENCE_ADDRESSABLES_SUPPORT

using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace SmartReference.Runtime
{
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

        public ISmartReferenceHandle LoadAsync(string path, Type type, Action<Object> callback)
        {
            var handle = Addressables.LoadAssetAsync<Object>(path);
            handle.Completed += operation => {
                if (operation.Status == AsyncOperationStatus.Succeeded) {
                    callback?.Invoke(operation.Result);
                }
            };
            var h = new AddressablesHandle { Op = handle };
            return h;
        }

        public void Release(ISmartReferenceHandle handle, UnityEngine.Object asset)
        {
            if (handle is AddressablesHandle ah && ah.IsValid)
            {
                Addressables.Release(ah.Op);
                return;
            }

            // fallback (less ideal): if someone passed only asset
            if (asset != null)
            {
                Addressables.Release(asset);
            }
        }

        public void Cancel(ISmartReferenceHandle handle)
        {
            // Addressables doesn't truly "cancel" loads the same way; you can release handle
            // but behavior depends on ref counting/state. We'll best-effort release op if valid.
            if (handle is AddressablesHandle ah && ah.IsValid)
            {
                Addressables.Release(ah.Op);
            }
        }
    }
}

#endif