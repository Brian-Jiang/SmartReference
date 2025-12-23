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

        public void Release(ISmartReferenceHandle handle)
        {
            if (handle is ResourcesHandle { IsValid: true } resourcesHandle)
            {
                Resources.UnloadAsset(resourcesHandle.loadedObject);
            }
        }

        public void Cancel(ISmartReferenceHandle handle)
        {
            
        }

        private static string GetResourcesPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            // Normalize slashes for Windows paths
            var p = path.Replace('\\', '/');

            const string segment = "/Resources/";
            var idx = p.IndexOf(segment, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                Debug.LogError($"[SmartReference] ResourcesLoader: Path does not contain '/Resources/': {path}");
                return StripExtension(p);
            }

            var start = idx + segment.Length;
            if (start >= p.Length)
            {
                Debug.LogError($"[SmartReference] ResourcesLoader: Path points to Resources folder, not an asset: {path}");
                return StripExtension(p);
            }

            var relative = p.Substring(start);
            return StripExtension(relative);
        }

        private static string StripExtension(string path)
        {
            var slashIndex = path.LastIndexOf('/');
            var dotIndex = path.LastIndexOf('.');
            return (dotIndex > slashIndex) ? path.Substring(0, dotIndex) : path;
        }
    }
}