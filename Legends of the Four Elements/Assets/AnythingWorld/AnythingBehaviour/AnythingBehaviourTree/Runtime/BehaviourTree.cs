using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Represents a Behaviour Tree asset in Unity, holding the structure and logic of the tree,
    /// including nodes and blackboard.
    /// </summary>
    [CreateAssetMenu()]
    public class BehaviourTree : ScriptableObject
    {
        [SerializeReference]
        public RootNode rootNode;

        [SerializeReference]
        public List<Node> nodes = new List<Node>();

        public Node.State treeState = Node.State.Running;

        public Blackboard blackboard = new Blackboard();

        [Tooltip("By enabling this option, any object imported via Anything World with this Behaviour Tree will have a NavMeshAgent corresponding to its size added.")] public bool usesNavMesh = false;

        #region  EditorProperties 
        public Vector3 viewPosition = new Vector3(600, 300);
        public Vector3 viewScale = Vector3.one;
        #endregion

        public BehaviourTree()
        {
            rootNode = new RootNode();
            nodes.Add(rootNode);
        }

        /// <summary>
        /// Unity callback for when the editor window is enabled, setting up various event listeners.
        /// </summary>
        private void OnEnable()
        {
            // Validate the behaviour tree on load, removing all null children
            nodes.RemoveAll(node => node == null);
            Traverse(rootNode, node =>
            {
                if (node is CompositeNode composite)
                {
                    composite.children.RemoveAll(child => child == null);
                }
            });
        }

        // Updates the state of the Behaviour Tree during the game's update cycle.
        public Node.State Update()
        {
            if (treeState == Node.State.Running)
            {
                treeState = rootNode.Update();
            }
            return treeState;
        }

        // Retrieves the list of children for a given parent node in the Behaviour Tree.
        public static List<Node> GetChildren(Node parent)
        {
            List<Node> children = new List<Node>();

            if (parent is DecoratorNode decorator && decorator.child != null)
            {
                children.Add(decorator.child);
            }

            if (parent is RootNode rootNode && rootNode.child != null)
            {
                children.Add(rootNode.child);
            }

            if (parent is CompositeNode composite)
            {
                return composite.children;
            }

            return children;
        }

        /// <summary>
        /// Recursively traverses the Behaviour Tree, executing a visitor action on each node.
        /// </summary>
        public static void Traverse(Node node, System.Action<Node> visiter)
        {
            if (node != null)
            {
                visiter.Invoke(node);
                var children = GetChildren(node);
                children.ForEach(n => Traverse(n, visiter));
            }
        }

        /// <summary>
        /// Creates a clone of the Behaviour Tree, useful for creating instances at runtime.
        /// </summary>
        public BehaviourTree Clone()
        {
            BehaviourTree tree = Instantiate(this);
            return tree;
        }

        /// <summary>
        /// Binds a context to each node in the Behaviour Tree and initializes them.
        /// </summary>
        public void Bind(Context context)
        {
            Traverse(rootNode, node =>
            {
                node.context = context;
                node.blackboard = blackboard;
                node.OnInit();
            });
        }
    }
}
