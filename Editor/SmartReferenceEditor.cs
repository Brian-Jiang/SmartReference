using System;
using UnityEditor;
using UnityEngine;
#if UNITY_2022_2_OR_NEWER
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#endif
using Object = UnityEngine.Object;

namespace SmartReference.Editor
{
    [CustomPropertyDrawer(typeof(Runtime.SmartReference), true)]
    internal class SmartReferenceEditor: PropertyDrawer
    {
#if UNITY_2022_2_OR_NEWER
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            RepairStalePath(property);

            var root = new VisualElement();
            var field = new ObjectField(SmartReferenceUtils.GetLabel(property, preferredLabel)) {
                objectType = SmartReferenceUtils.GetReferencedType(fieldInfo, property),
                allowSceneObjects = false,
            };
            field.AddToClassList(ObjectField.alignedFieldUssClassName);
            var missingBox = new HelpBox(string.Empty, HelpBoxMessageType.Warning);
            root.Add(field);
            root.Add(missingBox);

            void Refresh()
            {
                var referenced = SmartReferenceUtils.ResolveReference(property);
                field.SetValueWithoutNotify(referenced);
                DrawerUtils.UpdatePropertyStyle(field, property);

                var hasGuid = !string.IsNullOrEmpty(property.FindPropertyRelative("guid").stringValue);
                var missing = referenced == null && hasGuid && !property.hasMultipleDifferentValues;
                missingBox.text = $"Referenced asset is missing, path: {property.FindPropertyRelative("path").stringValue}";
                missingBox.style.display = missing ? DisplayStyle.Flex : DisplayStyle.None;
            }

            Refresh();
            
            field.TrackPropertyValue(property, _ => Refresh());
            field.RegisterValueChangedCallback(evt => {
                SetReference(property, evt.newValue);
                Refresh();
            });
            DrawerUtils.AddPrefabOverrideMenu(field, property);
            DrawerUtils.AddPrefabOverrideBar(field, property);
            return root;
        }

        private static void RepairStalePath(SerializedProperty property)
        {
            var serializedObject = property.serializedObject;
            if (serializedObject.isEditingMultipleObjects) return;
            if (property.isInstantiatedPrefab && !property.prefabOverride) return;

            var guid = property.FindPropertyRelative("guid").stringValue;
            if (string.IsNullOrEmpty(guid)) return;

            var obj = SmartReferenceUtils.FindAsset(guid, property.FindPropertyRelative("fileID").longValue);
            if (obj == null) return;

            var pathProp = property.FindPropertyRelative("path");
            var path = AssetDatabase.GetAssetPath(obj);
            if (pathProp.stringValue == path) return;

            pathProp.stringValue = path;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
#endif

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);
            label.text = SmartReferenceUtils.GetLabel(property, label.text);

            var type = SmartReferenceUtils.GetReferencedType(fieldInfo, property);
            var referenced = SmartReferenceUtils.ResolveReference(property);
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            var newReferenced = EditorGUI.ObjectField(position, label, referenced, type, false);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck()) {
                SetReference(property, newReferenced);
            }

            EditorGUI.EndProperty();
        }

        private void SetReference(SerializedProperty property, Object obj)
        {
            var typeProp = property.FindPropertyRelative("type");
            if (string.IsNullOrEmpty(typeProp.stringValue)) {
                typeProp.stringValue = SmartReferenceUtils.GetReferencedType(fieldInfo, property).AssemblyQualifiedName;
            }

            try {
                SmartReferenceTool.SetReference(property, obj);
            }
            catch (ArgumentException e) {
                Debug.LogError($"[SmartReference] {e.Message}");
            }
        }
    }
}
