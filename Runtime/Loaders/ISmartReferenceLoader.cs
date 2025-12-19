using System;
using Object = UnityEngine.Object;

namespace SmartReference.Runtime
{
    public interface ISmartReferenceLoader
    {
        /// <summary>
        /// Load an asset synchronously.
        /// </summary>
        /// <param name="path">The path of the asset, begin with `Assets/`</param>
        /// <param name="type">The type of the asset.</param>
        /// <returns></returns>
        public Object Load(string path, Type type);
        
        /// <summary>
        /// Load an asset asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type">he type of the asset.</param>
        /// <param name="callback"></param>
        /// <returns></returns>
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