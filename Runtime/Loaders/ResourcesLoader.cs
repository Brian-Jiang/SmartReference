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

            // Normalize slashes so Windows paths work.
            var p = path.Replace('\\', '/');
            const string segment = "/Resources/";
            var idx = p.LastIndexOf(segment, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                const string segmentNoSlash = "/Resources";
                var idx2 = p.LastIndexOf(segmentNoSlash, StringComparison.OrdinalIgnoreCase);
                if (idx2 >= 0)
                {
                    var next = idx2 + segmentNoSlash.Length;
                    if (next == p.Length)
                    {
                        Debug.LogError($"[SmartReference] ResourcesLoader: Path points to Resources folder, not an asset: {path}");
                        return StripExtension(p);
                    }
                    
                    if (p[next] == '/')
                    {
                        idx = idx2;
                    }
                }
            }

            string relative;
            if (idx >= 0)
            {
                var start = idx + segment.Length;
                if (start >= p.Length)
                {
                    Debug.LogError($"[SmartReference] ResourcesLoader: Path points to Resources folder, not an asset: {path}");
                    return StripExtension(p);
                }

                relative = p.Substring(start);
            }
            else
            {
                Debug.LogWarning($"[SmartReference] ResourcesLoader: Path '{path}' does not contain a Resources folder segment.");
                relative = p;
            }

            return StripExtension(relative);
        }

        private static string StripExtension(string s)
        {
            var slash = s.LastIndexOf('/');
            var dot = s.LastIndexOf('.');
            if (dot > slash)
            {
                return s.Substring(0, dot);
            }
            
            return s;
        }
    }
}