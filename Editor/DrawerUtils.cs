#if UNITY_2022_2_OR_NEWER
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace SmartReference.Editor
{
    internal static class DrawerUtils
    {
        internal static void UpdatePropertyStyle(BaseField<UnityEngine.Object> field, SerializedProperty property)
        {
            field.showMixedValue = property.hasMultipleDifferentValues;
            field.EnableInClassList(BindingExtensions.prefabOverrideUssClassName, property.prefabOverride);
        }

        internal static void AddPrefabOverrideMenu(VisualElement element, SerializedProperty property)
        {
            element.AddManipulator(new ContextualMenuManipulator(evt => {
                if (property.serializedObject.isEditingMultipleObjects || !property.prefabOverride) return;

                var assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(property.serializedObject.targetObject);
                if (!string.IsNullOrEmpty(assetPath)) {
                    evt.menu.AppendAction($"Apply to Prefab '{Path.GetFileNameWithoutExtension(assetPath)}'",
                        _ => PrefabUtility.ApplyPropertyOverride(property, assetPath, InteractionMode.UserAction));
                }
                evt.menu.AppendAction("Revert", _ => PrefabUtility.RevertPropertyOverride(property, InteractionMode.UserAction));
            }));
        }
    }
}
#endif
