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

        private static string GetResourcesPath(string path)
{
    if (string.IsNullOrEmpty(path))
        return path;

    // Normalize slashes so Windows paths work.
    var p = path.Replace('\\', '/');

    // If caller already passed a Resources-relative path (common), we can accept it.
    // (We still strip extension if present.)
    // Example: "UI/Icons/MyIcon.png" => "UI/Icons/MyIcon"
    // But we should prefer detecting an actual "/Resources/" segment first.

    // Find the LAST "/Resources/" segment, so nested Resources works:
    // "Assets/A/Resources/X.prefab" and "Assets/A/Resources/Sub/Resources/Y.prefab"
    // should both load correctly (use the deepest Resources root).
    const string segment = "/Resources/";
    int idx = p.LastIndexOf(segment, StringComparison.OrdinalIgnoreCase);

    // Also handle when path contains ".../Resources" with no trailing slash (rare but possible).
    if (idx < 0)
    {
        const string segmentNoSlash = "/Resources";
        int idx2 = p.LastIndexOf(segmentNoSlash, StringComparison.OrdinalIgnoreCase);
        if (idx2 >= 0)
        {
            // Ensure it's a folder boundary: next char is '/' or end-of-string.
            int next = idx2 + segmentNoSlash.Length;
            if (next == p.Length)
            {
                // Path points to the Resources folder itself (not loadable).
                Debug.LogError($"[SmartReference] ResourcesLoader: Path points to Resources folder, not an asset: {path}");
                return StripExtension(p);
            }
            if (p[next] == '/')
                idx = idx2; // treat as "/Resources/"
        }
    }

    string relative;
    if (idx >= 0)
    {
        int start = idx + segment.Length; // after "/Resources/"
        if (start >= p.Length)
        {
            Debug.LogError($"[SmartReference] ResourcesLoader: Path points to Resources folder, not an asset: {path}");
            return StripExtension(p);
        }

        relative = p.Substring(start);
    }
    else
    {
        // No Resources segment found. Best effort: assume it's already relative.
        Debug.LogWarning($"[SmartReference] ResourcesLoader: Path '{path}' does not contain a Resources folder segment. Assuming it's already Resources-relative.");
        relative = p;
    }

    return StripExtension(relative);

    static string StripExtension(string s)
    {
        // Remove extension only if it's after the last slash.
        int slash = s.LastIndexOf('/');
        int dot = s.LastIndexOf('.');
        if (dot > slash) return s.Substring(0, dot);
        return s;
    }
}

    }
}