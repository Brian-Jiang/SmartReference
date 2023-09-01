using System;
using Object = UnityEngine.Object;

namespace SmartReference.Runtime {
    public delegate Object SmartReferenceLoader(string path, Type type);
    public delegate void SmartReferenceLoaderAsync(string path, Type type, Action<Object> callback);
    
    public class CustomLoader: ISmartReferenceLoader {
        public SmartReferenceLoader loader;
        public SmartReferenceLoaderAsync loaderAsync;
        
        public Object Load(string path, Type type) {
            return loader(path, type);
        }

        public void LoadAsync(string path, Type type, Action<Object> callback) {
            loaderAsync(path, type, callback);
        }
    }
}