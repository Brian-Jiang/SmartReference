using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SmartReference.Runtime {
    public class ResourcesLoader: ISmartReferenceLoader {
        public Object Load(string path, Type type) {
            var resourcesPath = GetResourcesPath(path);
            return Resources.Load(resourcesPath, type);
        }

        public void LoadAsync(string path, Type type, Action<Object> callback) {
            var resourcesPath = GetResourcesPath(path);
            var request = Resources.LoadAsync(resourcesPath, type);
            request.completed += _ => callback?.Invoke(request.asset);
        }
        
        private string GetResourcesPath(string path) {
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