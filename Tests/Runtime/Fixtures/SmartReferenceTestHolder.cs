using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartReference.Runtime.Tests
{
    [Serializable]
    public class SmartReferenceTestNested
    {
        public SmartReference<GameObject> nestedRef;
    }

    public abstract class SmartReferenceTestHolderBase : ScriptableObject
    {
        [SerializeField] private SmartReference<GameObject> basePrivateRef;
    }

    /// <summary>
    /// ScriptableObject with SmartReferences in nested classes, lists, arrays and a private base class field.
    /// </summary>
    public sealed class SmartReferenceTestHolder : SmartReferenceTestHolderBase
    {
        public SmartReferenceTestNested nested;
        public List<SmartReference<GameObject>> list;
        public SmartReference<GameObject>[] array;
    }
}
