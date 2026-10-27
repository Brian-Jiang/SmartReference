using System.Collections.Generic;
using UnityEngine;

namespace SmartReference.Runtime.Tests
{
    /// <summary>
    /// MonoBehaviour with SmartReferences, used in prefabs and scenes.
    /// </summary>
    public sealed class SmartReferenceTestBehaviour : MonoBehaviour
    {
        public SmartReference<GameObject> reference;
        public SmartReferenceTestNested nested;
        public List<SmartReference<GameObject>> list;
    }
}
