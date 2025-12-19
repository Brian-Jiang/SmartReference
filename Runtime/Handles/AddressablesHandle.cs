using UnityEngine.ResourceManagement.AsyncOperations;

namespace SmartReference.Runtime
{
    public class AddressablesHandle : ISmartReferenceHandle
    {
        public AsyncOperationHandle<UnityEngine.Object> Op;
        public bool IsValid => Op.IsValid();
    }
}