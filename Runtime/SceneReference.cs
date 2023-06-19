using System;
using UnityEngine;

namespace SmartReference.Runtime {
    [Serializable]
    public class SceneReference {
        [SerializeField]
        private string scenePath;
        public string ScenePath => scenePath;
    }
}