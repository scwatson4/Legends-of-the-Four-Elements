using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Represents the root node of a Behaviour Tree, from which all other nodes are executed.
    /// </summary>
    [System.Serializable]
    public class RootNode : Node
    {
        [SerializeReference]
        [HideInInspector] 
        public Node child;

        /// <summary>
        /// Defines the behavior at the start of the Node's lifecycle.
        /// </summary>
        protected override void OnStart()
        {
        }

        /// <summary>
        /// Defines the behavior at the end of the Node's lifecycle.
        /// </summary>
        protected override void OnStop()
        {
        }

        /// <summary>
        /// Updates the state of the Node, determining its success or failure based on the child node's state.
        /// </summary>
        protected override State OnUpdate()
        {
            if (child != null)
            {
                return child.Update();
            }
            else
            {
                return State.Failure;
            }
        }
    }
}
