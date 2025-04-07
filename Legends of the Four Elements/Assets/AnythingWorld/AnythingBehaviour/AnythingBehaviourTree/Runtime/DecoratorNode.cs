using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// An abstract base class for decorator nodes in a behavior tree, which modify the behavior or outcome of their
    /// single child node. Decorators can alter the execution flow by controlling when or if a child node
    /// should be executed, potentially changing the result based on specific conditions.
    /// </summary>
    public abstract class DecoratorNode : Node
    {
        [SerializeReference]
        [HideInInspector] 
        public Node child;
    }
}
