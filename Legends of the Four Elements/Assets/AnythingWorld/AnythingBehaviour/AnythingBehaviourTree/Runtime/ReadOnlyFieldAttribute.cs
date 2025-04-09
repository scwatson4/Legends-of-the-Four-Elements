#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace AnythingWorld.AnythingBehaviour.Tree
{
    /// <summary>
    /// Attribute to mark fields as read-only in the Unity Inspector. Useful for display purposes
    /// where editing is not intended.
    /// </summary>
    public class ReadOnlyFieldAttribute : PropertyAttribute{}

    /// <summary>
    /// Custom property drawer for the ReadOnlyFieldAttribute, rendering the associated field as disabled (read-only)
    /// in the Unity Inspector.
    /// </summary>
    [CustomPropertyDrawer(typeof(ReadOnlyFieldAttribute))]
    public class ReadOnlyFieldDrawer : PropertyDrawer
    {
        /// <summary>
        /// Renders the property field in the Unity Inspector as read-only.
        /// </summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Draw the property label using the standard label field.
            EditorGUI.LabelField(new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height), label);

            // Temporarily disable GUI to make the field read-only.
            GUI.enabled = false;

            // Draw the property field without a label to make it appear as read-only.
            EditorGUI.PropertyField(
                new Rect(position.x + EditorGUIUtility.labelWidth, position.y, 
                    position.width - EditorGUIUtility.labelWidth, position.height), property, GUIContent.none, true);

            // Re-enable GUI for subsequent fields to be editable.
            GUI.enabled = true;
        }
    }
}
#endif