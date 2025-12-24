#if SMARTREFERENCE_ADDRESSABLES_SUPPORT

using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SmartReference.Runtime
{
    public class AddressablesHandle : ISmartReferenceHandle
    {
        public AsyncOperationHandle<Object> op;
        public bool IsValid => op.IsValid();
    }
}

#endif