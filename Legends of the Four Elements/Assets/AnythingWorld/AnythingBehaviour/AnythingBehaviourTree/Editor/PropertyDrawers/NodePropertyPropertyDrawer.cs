using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// A custom property drawer that provides an interface for managing typed node properties,
    /// including binding to typed blackboard keys and editing default values.
    /// </summary>
    [CustomPropertyDrawer(typeof(NodeProperty<>))]
    public class GenericNodePropertyPropertyDrawer : PropertyDrawer
    {
        /// <summary>
        /// Creates a custom property GUI for a BlackboardKey in the editor.
        /// </summary>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            BehaviourTree tree = property.serializedObject.targetObject as BehaviourTree;

            var genericTypes = fieldInfo.FieldType.GenericTypeArguments;
            var propertyType = genericTypes[0];
            
            SerializedProperty reference = property.FindPropertyRelative("reference");

            Label label = new Label();
            label.AddToClassList("unity-base-field__label");
            label.AddToClassList("unity-property-field__label");
            label.AddToClassList("unity-property-field");
            label.text = property.displayName;

            var defaultValueField = PropertyDrawerUtils.GetFieldByType(propertyType, nameof(NodeProperty<int>.defaultValue));
            defaultValueField.style.flexGrow = 1.0f;
            defaultValueField.AddToClassList("hide-label");
            
            PopupField<BlackboardKey> dropdown = new PopupField<BlackboardKey>();
            dropdown.label = "";
            dropdown.formatListItemCallback = FormatItem;
            dropdown.formatSelectedValueCallback = FormatSelectedItem;

#if UNITY_2021_3_OR_NEWER
            var referenceValue = reference.managedReferenceValue as BlackboardKey;
#else
            var referenceValue = BehaviourTreeEditorUtility.GetTargetObjectOfProperty(reference) as BlackboardKey;
#endif
            if (referenceValue != null)
            {
#if !UNITY_2021_3_OR_NEWER && UNITY_2021
                var prop = dropdown.GetType().GetField("m_Choices", System.Reflection.BindingFlags.NonPublic
                                                                    | System.Reflection.BindingFlags.Instance);
                var choices = prop.GetValue(dropdown) as List<BlackboardKey>;
            
                choices.Add(referenceValue);
                prop.SetValue(dropdown, choices);
#endif
                dropdown.value = referenceValue;
            }
            
            dropdown.tooltip = "Bind value to a BlackboardKey";
            dropdown.style.flexGrow = 1.0f;
            dropdown.RegisterCallback<MouseEnterEvent>((evt) =>
            {
#if !UNITY_2021_3_OR_NEWER && UNITY_2021
                var prop = dropdown.GetType().GetField("m_Choices", System.Reflection.BindingFlags.NonPublic
                                                                    | System.Reflection.BindingFlags.Instance);
                var choices = prop.GetValue(dropdown) as List<BlackboardKey>;
                choices.Clear();

                foreach (var key in tree.blackboard.keys)
                {
                    if (propertyType.IsAssignableFrom(key.underlyingType))
                    {
                        choices.Add(key);
                    }
                }
                choices.Add(null);

                choices.Sort((left, right) =>
                {
                    if (left == null)
                    {
                        return -1;
                    }

                    if (right == null)
                    {
                        return 1;
                    }
                    return left.name.CompareTo(right.name);
                });
                prop.SetValue(dropdown, choices);
#else
                dropdown.choices.Clear();
                foreach (var key in tree.blackboard.keys)
                {
                    if (propertyType.IsAssignableFrom(key.underlyingType))
                    {
                        dropdown.choices.Add(key);
                    }
                }
                dropdown.choices.Add(null);

                dropdown.choices.Sort((left, right) =>
                {
                    if (left == null)
                    {
                        return -1;
                    }

                    if (right == null)
                    {
                        return 1;
                    }
                    return left.name.CompareTo(right.name);
                });
#endif 
            });

            dropdown.RegisterCallback<ChangeEvent<BlackboardKey>>((evt) =>
            {
                BlackboardKey newKey = evt.newValue;
                reference.managedReferenceValue = newKey;
                BehaviourTreeEditorWindow.Instance.serializer.ApplyChanges();

                if (evt.newValue == null)
                {
                    defaultValueField.style.display = DisplayStyle.Flex;
                    dropdown.style.flexGrow = 0.0f;
                }
                else
                {
                    defaultValueField.style.display = DisplayStyle.None;
                    dropdown.style.flexGrow = 1.0f;
                }
            });

            defaultValueField.style.display = dropdown.value == null ? DisplayStyle.Flex : DisplayStyle.None;
            dropdown.style.flexGrow = dropdown.value == null ? 0.0f : 1.0f;

            VisualElement container = new VisualElement();
            container.AddToClassList("unity-base-field");
            container.AddToClassList("node-property-field");
            container.style.flexDirection = FlexDirection.Row;
            container.Add(label);
            container.Add(defaultValueField);
            container.Add(dropdown);

            return container;
        }

        /// <summary>
        /// Formats the display of BlackboardKey items in the dropdown.
        /// </summary>
        private string FormatItem(BlackboardKey item) => item == null ? "[Inline]" : item.name;

        /// <summary>
        /// Formats the selected BlackboardKey item in the dropdown.
        /// </summary>
        private string FormatSelectedItem(BlackboardKey item) => item == null ? "" : item.name;
    }

    /// <summary>
    /// Custom property drawer designed to enhance the Unity Inspector interface for NodeProperty instances.
    /// Simplifies the process of binding node properties to blackboard keys, providing a user-friendly dropdown
    /// interface for selecting a blackboard key to bind.
    /// </summary>
    [CustomPropertyDrawer(typeof(NodeProperty))]
    public class NodePropertyPropertyDrawer : PropertyDrawer
    {
        /// <summary>
        /// Creates a custom property GUI for a BlackboardKey in the editor.
        /// </summary>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            BehaviourTree tree = property.serializedObject.targetObject as BehaviourTree;

            SerializedProperty reference = property.FindPropertyRelative("reference");

            PopupField<BlackboardKey> dropdown = new PopupField<BlackboardKey>();
            dropdown.label = property.displayName;
            dropdown.formatListItemCallback = FormatItem;
            dropdown.formatSelectedValueCallback = FormatItem;
#if UNITY_2021_3_OR_NEWER
            dropdown.value = reference.managedReferenceValue as BlackboardKey;
#else
            dropdown.value = BehaviourTreeEditorUtility.GetTargetObjectOfProperty(reference) as BlackboardKey;
#endif
            
            dropdown.RegisterCallback<MouseEnterEvent>((evt) =>
            {
#if !UNITY_2021_3_OR_NEWER && UNITY_2021
                var prop = dropdown.GetType().GetField("m_Choices", System.Reflection.BindingFlags.NonPublic
                                                                    | System.Reflection.BindingFlags.Instance);
                var choices = prop.GetValue(dropdown) as List<BlackboardKey>;
                choices.Clear();
                foreach (var key in tree.blackboard.keys)
                {
                    choices.Add(key);
                }
                choices.Sort((left, right) =>
                {
                    return left.name.CompareTo(right.name);
                });
                prop.SetValue(dropdown, choices);
#else
                dropdown.choices.Clear();
                foreach (var key in tree.blackboard.keys)
                {
                    dropdown.choices.Add(key);
                }
                dropdown.choices.Sort((left, right) =>
                {
                    return left.name.CompareTo(right.name);
                });
#endif
            });

            dropdown.RegisterCallback<ChangeEvent<BlackboardKey>>((evt) =>
            {
                BlackboardKey newKey = evt.newValue;
                reference.managedReferenceValue = newKey;
                BehaviourTreeEditorWindow.Instance.serializer.ApplyChanges();
            });
            return dropdown;
        }

        /// <summary>
        /// Formats the display of BlackboardKey items in the dropdown.
        /// </summary>
        private string FormatItem(BlackboardKey item) => item == null ? "(null)" : item.name;
    }
}
