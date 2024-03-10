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
        // ReSharper disable once UnusedMember.Global
        public string ScenePath => scenePath;

        public override string ToString() {
            return scenePath;
        }
        
        public static implicit operator string(SceneReference sceneReference) {
            return sceneReference.scenePath;
        }
    }
}