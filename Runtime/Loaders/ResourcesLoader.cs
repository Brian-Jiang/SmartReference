using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SmartReference.Runtime
{
    public class ResourcesLoader: ISmartReferenceLoader
    {
        public Object Load(string path, Type type)
        {
            var resourcesPath = GetResourcesPath(path);
            return Resources.Load(resourcesPath, type);
        }

        public ISmartReferenceHandle LoadAsync(string path, Type type, Action<Object> callback)
        {
            var resourcesPath = GetResourcesPath(path);
            var request = Resources.LoadAsync(resourcesPath, type);
            request.completed += _ => callback?.Invoke(request.asset);
            return new ResourcesHandle();
        }

        public void Release(ISmartReferenceHandle handle, UnityEngine.Object asset)
        {
            // Best effort: only safe for certain asset types loaded from Resources.
            if (asset != null)
            {
                try { Resources.UnloadAsset(asset); }
                catch { /* ignore */ }
            }
        }

        public void Cancel(ISmartReferenceHandle handle)
        {
            // Resources load cannot be cancelled; no-op.
        }

        // todo fix resources path
        private string GetResourcesPath(string path)
        {
            var index = path.LastIndexOf("Resources/", StringComparison.Ordinal);
            if (index == -1) {
                Debug.LogError($"[SmartReference] ResourcesLoader: Path {path} is not in Resources folder");
                return path;
            }

            var extensionIndex = path.LastIndexOf(".", StringComparison.Ordinal);
            return path[(index + "Resources/".Length)..extensionIndex];
        }
    }
}