using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace SmartReference.Editor {
    public class SmartReferencePreBuildProcessor: IPreprocessBuildWithReport {
        public int callbackOrder => 0;
        
        public void OnPreprocessBuild(BuildReport report) {
            // update all references
            SmartReferenceUtils.UpdateAllReferences();
        }
    }
}