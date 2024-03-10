using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SmartReference.Runtime {
    public class ResourcesLoader: ISmartReferenceLoader {
        public Object Load(string path, Type type) {
            return Resources.Load(path, type);
        }

        public void LoadAsync(string path, Type type, Action<Object> callback) {
            var request = Resources.LoadAsync(path, type);
            request.completed += _ => callback?.Invoke(request.asset);
        }
    }
}