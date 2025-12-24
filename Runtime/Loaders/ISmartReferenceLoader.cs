using System;
using Object = UnityEngine.Object;

namespace SmartReference.Runtime
{
    public interface ISmartReferenceLoader
    {
        /// <summary>
        /// Load an asset synchronously.
        /// </summary>
        /// <param name="path">The path of the asset, begin with `Assets/`.</param>
        /// <param name="type">The type of the asset.</param>
        /// <param name="loadedObject">The loaded asset.</param>
        /// <returns>An `ISmartReferenceHandle` that stores custom data about the load operation.</returns>
        public ISmartReferenceHandle Load(string path, Type type, out Object loadedObject);
        
        /// <summary>
        /// Load an asset asynchronously.
        /// </summary>
        /// <param name="path">The path of the asset, begin with `Assets/`.</param>
        /// <param name="type">the type of the asset.</param>
        /// <param name="callback">The callback to invoke once the load has finished.
        /// If your loader cancels the async load, you should call this callback with `null`.
        /// If not, you should call this with loaded `Object`, another `Release` will be called afterwards</param>
        /// <returns>An `ISmartReferenceHandle` that stores custom data about the load operation.</returns>
        public ISmartReferenceHandle LoadAsync(string path, Type type, Action<Object> callback);
        
        /// <summary>
        /// Release an asset/handle created by this loader using the provided handle.
        /// </summary>
        /// <param name="handle">The handle returned by `LoadAsync`.</param>
        void Release(ISmartReferenceHandle handle);

        /// <summary>
        /// Cancel an ongoing asynchronous load operation. This will be called when the asset is released when it's still loading asynchronously.
        /// Note that if the async callback is called later after loading is canceled, the `Release` method will be called to clean up the loaded asset.
        /// </summary>
        /// <param name="handle">The handle returned by `LoadAsync`.</param>
        void Cancel(ISmartReferenceHandle handle);
    }
}