using System;
using Object = UnityEngine.Object;

namespace SmartReference.Runtime {
    public interface ISmartReferenceLoader {
        public Object Load(string path, Type type);
        public void LoadAsync(string path, Type type, Action<Object> callback);
    }
}