using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnythingWorld.Behaviour.Tree 
{
    /// <summary>
    /// Custom property drawer for BlackboardKeyValuePair, used for rendering key-value pairs in the Unity Inspector.
    /// </summary>
    [CustomPropertyDrawer(typeof(BlackboardKeyValuePair))]
    public class BlackboardKeyValuePairPropertyDrawer : PropertyDrawer
    {
        private VisualElement _pairContainer;

        /// <summary>
        /// Creates a custom property GUI for a BlackboardKey in the editor.
        /// </summary>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var first = property.FindPropertyRelative(nameof(BlackboardKeyValuePair.key));
            var second = property.FindPropertyRelative(nameof(BlackboardKeyValuePair.value));
            PopupField<BlackboardKey> dropdown = new PopupField<BlackboardKey>();
            dropdown.label = first.displayName;
            dropdown.formatListItemCallback = FormatItem;
            dropdown.formatSelectedValueCallback = FormatItem;
            
#if UNITY_2021_3_OR_NEWER
            dropdown.value = first.managedReferenceValue as BlackboardKey;
#else
            dropdown.value = BehaviourTreeEditorUtility.GetTargetObjectOfProperty(first) as BlackboardKey;
#endif
            var tree = GetBehaviourTree(property);
            dropdown.RegisterCallback<MouseEnterEvent>(_ =>
            {
                if (tree == null)
                {
                    return;
                }
                
                var keys = tree.blackboard.keys;
                
                if (property.serializedObject.targetObject is BehaviourTreeInstanceRunner instance)
                {
                    var overriddenKeys = instance.blackboardOverrides.Select(x => x.key);
                    keys = tree.blackboard.keys.Except(overriddenKeys).ToList();
                }

#if !UNITY_2021_3_OR_NEWER && UNITY_2021
                var prop = dropdown.GetType().GetField("m_Choices", System.Reflection.BindingFlags.NonPublic
                                                                    | System.Reflection.BindingFlags.Instance);
                var choices = prop.GetValue(dropdown) as List<BlackboardKey>;
                    choices.Clear();
                foreach (var key in keys)
                {
                    choices.Add(key);
                }
                prop.SetValue(dropdown, choices);
#else
                dropdown.choices.Clear();
                foreach (var key in keys)
                {
                    dropdown.choices.Add(key);
                }
#endif
            });

            dropdown.RegisterCallback<ChangeEvent<BlackboardKey>>(evt =>
            {
                BlackboardKey newKey = evt.newValue;
                first.managedReferenceValue = newKey;
                property.serializedObject.ApplyModifiedProperties();

                if (_pairContainer.childCount > 1)
                {
                    _pairContainer.RemoveAt(1);
                }

#if UNITY_2021_3_OR_NEWER
                var secondManagedReferenceValue = second.managedReferenceValue;
#else
                var secondManagedReferenceValue = BehaviourTreeEditorUtility.GetTargetObjectOfProperty(second);
#endif
                if (secondManagedReferenceValue == null || secondManagedReferenceValue.GetType() != dropdown.value.GetType())
                {
                    second.managedReferenceValue = BlackboardKey.CreateKey(dropdown.value.GetType());
                    second.serializedObject.ApplyModifiedProperties();
                }
                PropertyField field = new PropertyField();
                field.label = second.displayName;
                field.BindProperty(second.FindPropertyRelative(nameof(BlackboardKey<object>.value)));
                _pairContainer.Add(field);
            });

            _pairContainer = new VisualElement();
            _pairContainer.Add(dropdown);

#if UNITY_2021_3_OR_NEWER
            var secondManagedReferenceValue = second.managedReferenceValue;
#else
            var secondManagedReferenceValue = BehaviourTreeEditorUtility.GetTargetObjectOfProperty(second);
#endif
            
            if (dropdown.value != null)
            {
                if (secondManagedReferenceValue == null ||
                    
#if UNITY_2021_3_OR_NEWER
                    first.managedReferenceValue.GetType()
#else
                    BehaviourTreeEditorUtility.GetTargetObjectOfProperty(first).GetType() 
#endif
                != secondManagedReferenceValue.GetType())
                {
                    second.managedReferenceValue = BlackboardKey.CreateKey(dropdown.value.GetType());
                    second.serializedObject.ApplyModifiedProperties();
                }

                PropertyField field = new PropertyField();
                field.label = second.displayName;
                field.bindingPath = nameof(BlackboardKey<object>.value);
                _pairContainer.Add(field);
            }

            return _pairContainer;
        }
        
        /// <summary>
        /// Retrieves the BehaviourTree associated with a serialized property.
        /// </summary>
        private BehaviourTree GetBehaviourTree(SerializedProperty property)
        {
            if (property.serializedObject.targetObject is BehaviourTree tree)
            {
                return tree;
            }
            
            if (property.serializedObject.targetObject is BehaviourTreeInstanceRunner instance)
            {
                return instance.behaviourTree;
            }
            
            Debug.LogError("Could not find behaviour tree this is referencing");
            return null;
        }
        
        /// <summary>
        /// Formats the display of BlackboardKey items in the dropdown.
        /// </summary>
        private string FormatItem(BlackboardKey item)
        {
            if (item == null)
            {
                return "(null)";
            }

            return item.name;
        }
    }
}
