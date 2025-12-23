using UnityEngine;

namespace SmartReference.Runtime
{
    public class ResourcesHandle : ISmartReferenceHandle
    {
        public Object loadedObject;
        public bool IsValid => loadedObject != null;
    }
}