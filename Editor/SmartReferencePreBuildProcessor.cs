using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SmartReference.Editor {
    public class SmartReferencePreBuildProcessor: IPreprocessBuildWithReport {
        public int callbackOrder => 0;
        
        public void OnPreprocessBuild(BuildReport report) {
            Debug.Log("[SmartReference] Prebuild - Updating all references...");
            SmartReferenceUtils.UpdateAllReferences();
        }
    }
}