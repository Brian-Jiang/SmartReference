using System;
using UnityEngine;

namespace SmartReference.Runtime {
    [Serializable]
    public class SceneReference {
        [SerializeField]
        private string scenePath;
        
        /// <summary>
        /// Get the scene path.
        /// </summary>
        public string ScenePath => scenePath;
    }
}