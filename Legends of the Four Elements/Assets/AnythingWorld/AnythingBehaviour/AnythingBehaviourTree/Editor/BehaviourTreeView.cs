using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Represents the visual graph view for behavior trees in the editor, handling node creation,
    /// selection, and connectivity.
    /// </summary>
    public class BehaviourTreeView : GraphView
    {
        /// <summary>
        /// UxmlFactory class for BehaviourTreeView, enabling UIElements UXML instantiation.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<BehaviourTreeView, GraphView.UxmlTraits>{}

        // Node positions snap to 15 pixels
        public static int GridSnapSize = 15;

        public Action<NodeView> OnNodeSelected;

        private SerializedBehaviourTree _serializer;
        private bool _dontUpdateModel;

        // Stores and expands the list of node guids with the graph elements to copy. 
        [Serializable]
        class CopyPasteData
        {
            public List<string> nodeGuids = new List<string>();

            public void AddGraphElements(IEnumerable<GraphElement> elementsToCopy)
            {
                foreach (var element in elementsToCopy)
                {
                    NodeView nodeView = element as NodeView;
                    if (nodeView != null && !(nodeView.node is RootNode))
                    {
                        nodeGuids.Add(nodeView.node.guid);
                    }
                }
            }
        }

        // Class for storing the parent and child of an edge to create.
        class EdgeToCreate
        {
            public NodeView Parent;
            public NodeView Child;
        };

        public BehaviourTreeView()
        {
            Insert(0, new GridBackground());
            
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new HierarchySelector());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // Perform Copy
            serializeGraphElements = items =>
            {
                CopyPasteData copyPasteData = new CopyPasteData();
                copyPasteData.AddGraphElements(items);
                string data = JsonUtility.ToJson(copyPasteData);
                return data;
            };

            // Perform Paste
            unserializeAndPaste = (_, data) =>
            {
#if UNITY_2021_3_OR_NEWER
                _serializer.BeginBatch();
#endif
                
                ClearSelection();

                CopyPasteData copyPasteData = JsonUtility.FromJson<CopyPasteData>(data);
                Dictionary<string, string> oldToNewMapping = new Dictionary<string, string>();

                // Gather all nodes to copy
                List<NodeView> nodesToCopy = new List<NodeView>();
                foreach (var nodeGuid in copyPasteData.nodeGuids)
                {
                    NodeView nodeView = FindNodeView(nodeGuid);
                    nodesToCopy.Add(nodeView);
                }

                // Gather all edges to create
                List<EdgeToCreate> edgesToCreate = new List<EdgeToCreate>();
                foreach (var nodeGuid in copyPasteData.nodeGuids)
                {
                    NodeView nodeView = FindNodeView(nodeGuid);
                    var nodesParent = nodeView.NodeParent;
                    if (nodesToCopy.Contains(nodesParent))
                    {
                        EdgeToCreate newEdge = new EdgeToCreate();
                        newEdge.Parent = nodesParent;
                        newEdge.Child = nodeView;
                        edgesToCreate.Add(newEdge);
                    }
                }

                // Copy all nodes by creating a new nodes with fields reset or deep copied is it's enabled in settings.
                foreach (var nodeView in nodesToCopy)
                {
                    Node newNode;
                    
                    if (BehaviourTreeEditorWindow.Instance.settings.enableNodeValuesCopying)
                    {
                        newNode = _serializer.CreateNodeCopy(nodeView.node, nodeView.node.position + Vector2.one * 50);
                    }
                    else
                    {
                        newNode = _serializer.CreateNode(nodeView.node.GetType(), nodeView.node.position + 
                            Vector2.one * 50);
                    }
                    
                    NodeView newNodeView = CreateNodeView(newNode);
                    AddToSelection(newNodeView);

                    // Map old to new guids so edges can be cloned.
                    oldToNewMapping[nodeView.node.guid] = newNode.guid;
                }

                // Copy all edges
                foreach(var edge in edgesToCreate)
                {
                    NodeView oldParent = edge.Parent;
                    NodeView oldChild = edge.Child;

                    // These should already have been created.
                    NodeView newParent = FindNodeView(oldToNewMapping[oldParent.node.guid]);
                    NodeView newChild = FindNodeView(oldToNewMapping[oldChild.node.guid]);

                    _serializer.AddChild(newParent.node, newChild.node);
                    AddChild(newParent, newChild);
                }

#if UNITY_2021_3_OR_NEWER
                // Save changes
                _serializer.EndBatch();
#endif               
            };

            // Enable copy paste always?
            canPasteSerializedData = (_) => true;

            viewTransformChanged += OnViewTransformChanged;
        }

        // Callback for when the view transform changes, updating the serialized view transform.
        void OnViewTransformChanged(GraphView graphView)
        {
            Vector3 position = contentViewContainer.transform.position;
            Vector3 scale = contentViewContainer.transform.scale;
            _serializer.SetViewTransform(position, scale);
        }

        /// <summary>
        /// Finds and returns the NodeView corresponding to the given Node.
        /// </summary>
        public NodeView FindNodeView(Node node)
        {
            return GetNodeByGuid(node.guid) as NodeView;
        }

        /// <summary>
        /// Finds and returns the NodeView corresponding to the given GUID.
        /// </summary>
        public NodeView FindNodeView(string guid)
        {
            return GetNodeByGuid(guid) as NodeView;
        }

        /// <summary>
        /// Clears all nodes and edges from the view.
        /// </summary>
        public void ClearView()
        {
            graphViewChanged -= OnGraphViewChanged;
            DeleteElements(graphElements.ToList());
            graphViewChanged += OnGraphViewChanged; 
        }

        /// <summary>
        /// Populates the view with nodes and edges based on the given SerializedBehaviourTree.
        /// </summary>
        public void PopulateView(SerializedBehaviourTree tree)
        {
            _serializer = tree;
            
            ClearView();

            Debug.Assert(_serializer.tree.rootNode != null);

            // Creates node view
            _serializer.tree.nodes.ForEach(n => CreateNodeView(n));

            // Create edges
            _serializer.tree.nodes.ForEach(n =>
            {
                var children = BehaviourTree.GetChildren(n);
                children.ForEach(c =>
                {
                    NodeView parentView = FindNodeView(n);
                    NodeView childView = FindNodeView(c);
                    Debug.Assert(parentView != null, "Invalid parent after deserialising");
                    Debug.Assert(childView != null, $"Null child view after deserialising parent{parentView.node.GetType().Name}");
                    CreateEdgeView(parentView, childView);
                    
                });
            });

            // Set view
            contentViewContainer.transform.position = _serializer.tree.viewPosition;
            contentViewContainer.transform.scale = _serializer.tree.viewScale;
        }

        // Returns a list of ports compatible with the given port for edge creation.
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(endPort =>
            endPort.direction != startPort.direction &&
            endPort.node != startPort.node).ToList();
        }

        /// <summary>
        /// Handles changes to the graph view, such as node and edge additions or removals.
        /// </summary>
        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            if (_dontUpdateModel)
            {
                return graphViewChange;
            }

            List<GraphElement> blockedDeletes = new List<GraphElement>();

            if (graphViewChange.elementsToRemove != null)
            {
                graphViewChange.elementsToRemove.ForEach(elem =>
                {
                    NodeView nodeView = elem as NodeView;
                    if (nodeView != null)
                    {
                        // The root node is not deletable
                        if (!(nodeView.node is RootNode))
                        {
                            OnNodeSelected(null);
                            _serializer.DeleteNode(nodeView.node);
                        }
                        else
                        {
                            blockedDeletes.Add(elem);
                        }
                    }

                    Edge edge = elem as Edge;
                    if (edge != null)
                    {
                        NodeView parentView = edge.output.node as NodeView;
                        NodeView childView = edge.input.node as NodeView;
                        _serializer.RemoveChild(parentView.node, childView.node);
                    }
                });
            }

            if (graphViewChange.edgesToCreate != null)
            {
                graphViewChange.edgesToCreate.ForEach(edge =>
                {
                    NodeView parentView = edge.output.node as NodeView;
                    NodeView childView = edge.input.node as NodeView;
                    _serializer.AddChild(parentView.node, childView.node);
                });
            }

            nodes.ForEach((n) =>
            {
                NodeView view = n as NodeView;
                // Need to rebind description labels as the serialized properties will be invalidated after removing from array
                view.SetupDataBinding();
                view.SortChildren();
            });

            foreach(var elem in blockedDeletes)
            {
                graphViewChange.elementsToRemove.Remove(elem);  
            }

            return graphViewChange;
        }

        /// <summary>
        /// Builds the contextual menu for the Behaviour Tree View, allowing node creation.
        /// </summary>
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {            
            CreateNodeWindow.Show(evt.mousePosition, null);
        }

        /// <summary>
        /// Creates a new node of the specified type at the given position, optionally setting it as a child of an existing node.
        /// </summary>
        public NodeView CreateNode(System.Type type, Vector2 position, NodeView parentView)
        {
            _serializer.BeginBatch();

            // Update model
            Node node = _serializer.CreateNode(type, position);
            if (parentView != null)
            {
                _serializer.AddChild(parentView.node, node);
            }

            // Update View
            NodeView nodeView = CreateNodeView(node);
            if (parentView != null)
            {
                AddChild(parentView, nodeView);
            }

            _serializer.EndBatch();

            return nodeView;
        }

        /// <summary>
        /// Creates a new node of the specified type at the given position and sets an existing node as its child.
        /// </summary>
        public NodeView CreateNodeWithChild(System.Type type, Vector2 position, NodeView childView)
        {
            _serializer.BeginBatch();

            // Update Model
            Node node = _serializer.CreateNode(type, position);

            // Delete the childs previous parent
            foreach(var connection in childView.input.connections)
            {
                var childParent = connection.output.node as NodeView;
                _serializer.RemoveChild(childParent.node, childView.node);
            }
            // Add as child of new node.
            _serializer.AddChild(node, childView.node);

            // Update View
            NodeView nodeView = CreateNodeView(node);
            if (nodeView != null)
            {
                AddChild(nodeView, childView);
            }

            _serializer.EndBatch();
            return nodeView;
        }

        // Instantiates a NodeView for a given Node and adds it to the graph.
        NodeView CreateNodeView(Node node)
        {
            NodeView nodeView = new NodeView(node, BehaviourTreeEditorWindow.Instance.uiAssets.nodeViewXml);
            AddElement(nodeView);
            nodeView.OnNodeSelected = OnNodeSelected;
            return nodeView;
        }

        /// <summary>
        /// Adds a child node to a parent node in the view, creating an edge between them.
        /// </summary>
        public void AddChild(NodeView parentView, NodeView childView)
        {
            // Delete Previous output connections
            if (parentView.output.capacity == Port.Capacity.Single)
            {
                RemoveElements(parentView.output.connections);
            }

            // Delete previous child's parent
            RemoveElements(childView.input.connections);

            CreateEdgeView(parentView, childView);
        }

        // Creates and adds an edge between two NodeViews.
        void CreateEdgeView(NodeView parentView, NodeView childView)
        {
            Edge edge = parentView.output.ConnectTo(childView.input);
            AddElement(edge);
        }

        /// <summary>
        /// Removes specified elements from the view without triggering a graph change event.
        /// </summary>
        public void RemoveElements(IEnumerable<GraphElement> elementsToRemove)
        {
            _dontUpdateModel = true;
            // Just need to delete the ui elements without causing a graphChangedEvent here.
            DeleteElements(elementsToRemove); 
            _dontUpdateModel = false;
        }

        /// <summary>
        /// Updates the visual state of all nodes in the view, typically used in response to runtime changes.
        /// </summary>
        public void UpdateNodeStates()
        {
            nodes.ForEach(n =>
            {
                NodeView view = n as NodeView;
                view.UpdateState();
            });
        }

        /// <summary>
        /// Selects a given node in the view, clearing previous selections.
        /// </summary>
        public void SelectNode(NodeView nodeView)
        {
            ClearSelection();
            if (nodeView != null)
            {
                AddToSelection(nodeView);
            }
        }
    }
}
