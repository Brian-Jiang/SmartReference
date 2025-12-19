using System;
using Object = UnityEngine.Object;

namespace SmartReference.Runtime {
    public interface ISmartReferenceLoader {
        public Object Load(string path, Type type);
        public ISmartReferenceHandle LoadAsync(string path, Type type, Action<Object> callback);
        
        /// <summary>
        /// Release an asset/handle created by this loader.
        /// If handle is null, loader may optionally attempt to release by asset reference.
        /// </summary>
        void Release(ISmartReferenceHandle handle, UnityEngine.Object asset);

        /// <summary>
        /// Optional: try cancel an in-flight load. If unsupported, no-op.
        /// </summary>
        void Cancel(ISmartReferenceHandle handle);
    }
}