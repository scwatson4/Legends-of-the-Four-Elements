using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Handles opening node scripts or subtrees when double-clicking on nodes within a certain interval.
    /// </summary>
    public class  DoubleClickNode : MouseManipulator
    {
        private const double DoubleClickDuration = 0.3;
        private double _time = EditorApplication.timeSinceStartup;

        /// <summary>
        /// Registers the callback for the mouse down event to detect double-clicks.
        /// </summary>
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
        }

        /// <summary>
        /// Unregisters the callback for the mouse down event.
        /// </summary>
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
        }

        /// <summary>
        /// Handles the mouse down event, checking for double-clicks to trigger node-specific actions.
        /// </summary>
        private void OnMouseDown(MouseDownEvent evt)
        {
            if (!CanStopManipulation(evt))
                return; 

            NodeView clickedElement = evt.target as NodeView;
            if (clickedElement == null)
            {
                var ve = evt.target as VisualElement;
                clickedElement = ve.GetFirstAncestorOfType<NodeView>();
                if (clickedElement == null)
                    return;
            }

            double duration = EditorApplication.timeSinceStartup - _time;
            if (duration < DoubleClickDuration)
            {
                OnDoubleClick(clickedElement);
            }

            _time = EditorApplication.timeSinceStartup;
        }

        /// <summary>
        /// Opens the associated script for a node in the editor when a node is double-clicked.
        /// </summary>
        private void OpenScriptForNode(NodeView clickedElement)
        {
            // Open script in the editor:
            var nodeName = clickedElement.node.GetType().Name;
            var assetGuids = AssetDatabase.FindAssets($"t:TextAsset {nodeName}");
            for (int i = 0; i < assetGuids.Length; ++i)
            {
                var path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                var filename = System.IO.Path.GetFileName(path);
                if (filename == $"{nodeName}.cs")
                {
                    BehaviourTreeEditorWindow.Instance.shouldOpenTree = false;
                    var script = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                    AssetDatabase.OpenAsset(script);
                    break;
                }
            }

            // Remove the node from selection to prevent dragging it around when returning to the editor.
            BehaviourTreeEditorWindow.Instance.treeView.RemoveFromSelection(clickedElement);
        }

        /// <summary>
        /// Navigates to a sub-tree when a sub-tree node is double-clicked.
        /// </summary>
        private void OpenSubtree(NodeView clickedElement)
        {
            BehaviourTreeEditorWindow.Instance.PushSubTreeView(clickedElement.node as SubTree);
        }

        /// <summary>
        /// Handles the double-click action on a node, determining whether to open a script or navigate to a sub-tree.
        /// </summary>
        private void OnDoubleClick(NodeView clickedElement)
        {
            if (clickedElement.node is SubTree)
            {
                OpenSubtree(clickedElement);
            }
            else
            {
                OpenScriptForNode(clickedElement);
            }
        }
    }
}
