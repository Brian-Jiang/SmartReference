#if SMARTREFERENCE_ADDRESSABLES_SUPPORT
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEngine;

namespace SmartReference.Editor
{
    /// <summary>
    /// The default Addressables packed mode build, which updates all SmartReferences first.
    /// Addressables content builds don't run player build callbacks, so add this to the Data Builders of the
    /// Addressables settings and select it as the build script.
    /// </summary>
    [CreateAssetMenu(fileName = "BuildScriptSmartReferencePacked.asset",
        menuName = "Addressables/Content Builders/SmartReference Packed Mode")]
    public class SmartReferenceBuildScriptPackedMode: BuildScriptPackedMode
    {
        public override string Name => "SmartReference Packed Mode";

        protected override TResult BuildDataImplementation<TResult>(AddressablesDataBuilderInput builderInput)
        {
            // During a player build, the pre-build processor already updated them
            if (!BuildPipeline.isBuildingPlayer) {
                Debug.Log("[SmartReference] Addressables build - Updating all references...");
                SmartReferenceUtils.UpdateAllReferences();
            }

            return base.BuildDataImplementation<TResult>(builderInput);
        }
    }
}
#endif
