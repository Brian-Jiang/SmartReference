using UnityEngine;

namespace SmartReference.Runtime
{
    public class ResourcesHandle : ISmartReferenceHandle
    {
        public ResourceRequest request;
        public Object loadedObject;
    }
}