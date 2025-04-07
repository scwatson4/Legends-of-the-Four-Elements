using System.Collections.Generic;
using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Serves as the base class for nodes that manage a collection of child nodes, 
    /// facilitating the creation of composite structures within the behavior tree.
    /// Composite nodes are used to define control flow logic that dictates the order 
    /// and condition under which child nodes are executed.
    /// </summary>
    [System.Serializable]
    public abstract class CompositeNode : Node
    {
        [HideInInspector] 
        [SerializeReference]
        public List<Node> children = new List<Node>(); // A list of child nodes that this composite node will manage.
    }
}
