using UnityEngine.UIElements;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// A mouse manipulator for selecting a node and its hierarchy on ctrl + click.
    /// </summary>
    public class HierarchySelector : MouseManipulator
    {
        /// <summary>
        /// Registers the callback for the mouse down event to detect interactions.
        /// </summary>
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
        }

        /// <summary>
        /// Unregisters the callback for the mouse down event to prevent further interaction handling.
        /// </summary>
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
        }

        /// <summary>
        /// Handles the mouse down event, identifying ctrl + click to initiate hierarchy selection.
        /// </summary>
        private void OnMouseDown(MouseDownEvent evt)
        {
            if (!CanStopManipulation(evt))
                return; 
            
            // Cast the target to a BehaviourTreeView to access tree-specific functionalities.
            var graphView = target as BehaviourTreeView;
            if (graphView == null)
                return;

            // Determine the clicked element and cast it to NodeView, if possible.
            NodeView clickedElement = evt.target as NodeView;
            if (clickedElement == null)
            {
                // If the direct target is not a NodeView, attempt to find a NodeView ancestor.
                var ve = evt.target as VisualElement;
                clickedElement = ve.GetFirstAncestorOfType<NodeView>();
                if (clickedElement == null)
                    return;
            }

            // If the Ctrl key is held during the click, select the clicked node and its children.
            if (evt.ctrlKey)
            {
                SelectChildren(graphView, clickedElement);
            }
        }

        // Selects the clicked node and recursively selects all its child nodes.
        void SelectChildren(BehaviourTreeView graphView, NodeView clickedElement)
        {
            // Traverses the behavior tree from the clicked node, adding each node to the graphView's selection.
            BehaviourTree.Traverse(clickedElement.node, node =>
            {
                var view = graphView.FindNodeView(node);
                graphView.AddToSelection(view);
            });
        }
    }
}
