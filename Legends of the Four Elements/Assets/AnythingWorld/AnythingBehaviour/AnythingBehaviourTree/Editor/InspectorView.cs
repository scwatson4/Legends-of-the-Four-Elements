using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Visual element representing the inspector view in the behavior tree editor,
    /// displaying properties of the selected node.
    /// </summary>
    public class InspectorView : VisualElement
    {
        /// <summary>
        /// UxmlFactory class for BehaviourTreeView, enabling UIElements UXML instantiation.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<InspectorView, VisualElement.UxmlTraits>{}

        public InspectorView(){}

        /// <summary>
        /// Updates the inspector view to display properties of the selected node.
        /// </summary>
        internal void UpdateSelection(SerializedBehaviourTree serializer, NodeView nodeView)
        {
            Clear();

            if (nodeView == null)
            {
                return;
            }

            var nodeProperty = serializer.FindNode(serializer.Nodes, nodeView.node);
            if (nodeProperty == null)
            {
                return;
            }

            // Auto-expand the property
            nodeProperty.isExpanded = true;

            // Property field
            PropertyField field = new PropertyField();
#if UNITY_2021_3_OR_NEWER
            field.label = nodeProperty.managedReferenceValue.GetType().ToString();
#else
            field.label = BehaviourTreeEditorUtility.GetTargetObjectOfProperty(nodeProperty).GetType().ToString();            
#endif
            field.BindProperty(nodeProperty);

            Add(field);
        }
    }
}
