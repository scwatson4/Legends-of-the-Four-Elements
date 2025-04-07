using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Visual representation of a node in the behavior tree editor, encapsulating node data
    /// and UI logic for inputs/outputs.
    /// </summary>
    public class NodeView : UnityEditor.Experimental.GraphView.Node
    {
        public Action<NodeView> OnNodeSelected;
        public Node node;
        public Port input;
        public Port output;

        public NodeView NodeParent
        {
            get
            {
                using (IEnumerator<Edge> iter = input.connections.GetEnumerator())
                {
                    iter.MoveNext();
                    return iter.Current?.output.node as NodeView;
                }
            }
        }

        public List<NodeView> NodeChildren
        {
            get
            {
                List<NodeView> children = new List<NodeView>();
                foreach(var edge in output.connections)
                {
                    NodeView child = edge.output.node as NodeView;
                    if (child != null)
                    {
                        children.Add(child);
                    }
                }
                return children;
            }
        }

        private const int PortWidthDivider = 3;

        public NodeView(Node node, VisualTreeAsset nodeXml) : base(AssetDatabase.GetAssetPath(nodeXml))
        {
            // Disable node snapping
            this.capabilities &= ~(Capabilities.Snappable); 
            this.node = node;
            this.title = node.GetType().Name;
            this.viewDataKey = node.guid;

            style.left = node.position.x;
            style.top = node.position.y;

            CreateInputPorts();
            CreateOutputPorts();
            SetupClasses();
            SetupDataBinding();

            this.AddManipulator(new DoubleClickNode());
        }
        
        /// <summary>
        /// Sets up data binding for the node, associating serialized properties with UI elements.
        /// </summary>
        public void SetupDataBinding()
        {
            SerializedBehaviourTree serializer = BehaviourTreeEditorWindow.Instance.serializer;
            var nodeProp = serializer.FindNode(serializer.Nodes, node);
            if (nodeProp != null)
            {
                var descriptionProp = nodeProp.FindPropertyRelative("description");
                if (descriptionProp != null)
                {
                    Label descriptionLabel = this.Q<Label>("description");
                    descriptionLabel.BindProperty(descriptionProp);
                }
            }
        }

        /// <summary>
        /// Sets up CSS classes for the node based on its type (e.g., action, composite, decorator, root).
        /// </summary>
        private void SetupClasses()
        {
            if (node is ActionNode)
            {
                AddToClassList("action");
            }
            else if (node is CompositeNode)
            {
                AddToClassList("composite");
            }
            else if (node is DecoratorNode)
            {
                AddToClassList("decorator");
            }
            else if (node is RootNode)
            {
                AddToClassList("root");
            }
        }

        /// <summary>
        /// Creates and configures the input port(s) for the node based on its type.
        /// </summary>
        private void CreateInputPorts()
        {
            if (node is ActionNode)
            {
                input = new NodePort(Direction.Input, Port.Capacity.Single);
            }
            else if (node is CompositeNode)
            {
                input = new NodePort(Direction.Input, Port.Capacity.Single);
            }
            else if (node is DecoratorNode)
            {
                input = new NodePort(Direction.Input, Port.Capacity.Single);
            }
            else if (node is RootNode)
            {

            }

            if (input != null)
            {
                input.portName = "";
                input.style.width = input.style.width.value.value / PortWidthDivider; 
                input.style.flexDirection = FlexDirection.Column;
                inputContainer.Add(input);
            }
        }

        /// <summary>
        /// Creates and configures the output port(s) for the node based on its type.
        /// </summary>
        private void CreateOutputPorts()
        {
            if (node is ActionNode)
            {
                // Actions have no outputs
            }
            else if (node is CompositeNode)
            {
                output = new NodePort(Direction.Output, Port.Capacity.Multi);
            }
            else if (node is DecoratorNode)
            {
                output = new NodePort(Direction.Output, Port.Capacity.Single);
            }
            else if (node is RootNode)
            {
                output = new NodePort(Direction.Output, Port.Capacity.Single);
            }

            if (output != null)
            {
                output.portName = "";
                output.style.width = output.style.width.value.value / PortWidthDivider; 
                output.style.flexDirection = FlexDirection.ColumnReverse;
                outputContainer.Add(output);
            }
        }

        /// <summary>
        /// Overrides the SetPosition method to snap the node to a grid and update the node's serialized position.
        /// </summary>
        public override void SetPosition(Rect newPos)
        {
            newPos.x = BehaviourTreeEditorUtility.RoundTo(newPos.x, BehaviourTreeView.GridSnapSize);
            newPos.y = BehaviourTreeEditorUtility.RoundTo(newPos.y, BehaviourTreeView.GridSnapSize);

            base.SetPosition(newPos);

            SerializedBehaviourTree serializer = BehaviourTreeEditorWindow.Instance.serializer;
            Vector2 position = new Vector2(newPos.xMin, newPos.yMin);
            serializer.SetNodePosition(node, position);
        }

        /// <summary>
        /// Overrides the OnSelected method to invoke the OnNodeSelected event when the node is selected in the editor.
        /// </summary>
        public override void OnSelected()
        {
            base.OnSelected();
            if (OnNodeSelected != null)
            {
                OnNodeSelected.Invoke(this);
            }
        }

        /// <summary>
        /// Sorts the children of a composite node based on their horizontal positions in the editor.
        /// </summary>
        public void SortChildren()
        {
            if (node is CompositeNode composite)
            {
                composite.children.Sort(SortByHorizontalPosition);
            }
        }

        /// <summary>
        /// Compares two nodes based on their horizontal positions, for sorting purposes.
        /// </summary>
        private int SortByHorizontalPosition(Node left, Node right)
        {
            return left.position.x < right.position.x ? -1 : 1;
        }

        /// <summary>
        /// Updates the visual state of the node (e.g., running, success, failure) based on its runtime state in play mode.
        /// </summary>
        public void UpdateState()
        {
            RemoveFromClassList("running");
            RemoveFromClassList("failure");
            RemoveFromClassList("success");

            if (Application.isPlaying)
            {
                switch (node.state)
                {
                    case Node.State.Running:
                        if (node.started)
                        {
                            AddToClassList("running");
                        }
                        break;
                    case Node.State.Failure:
                        AddToClassList("failure");
                        break;
                    case Node.State.Success:
                        AddToClassList("success");
                        break;
                }
            }
        }
    }
}
