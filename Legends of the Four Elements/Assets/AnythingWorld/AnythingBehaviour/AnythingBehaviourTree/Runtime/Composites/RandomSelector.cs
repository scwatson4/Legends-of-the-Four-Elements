using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// A composite node that selects one of its children at random to execute.
    /// The selected child node is determined at the start of this node's execution.
    /// </summary>
    [System.Serializable]
    public class RandomSelector : CompositeNode
    {
        protected int current;

        /// <summary>
        /// Randomly selects a child node to execute.
        /// </summary>
        protected override void OnStart()
        {
            current = Random.Range(0, children.Count);
        }

        /// <summary>
        /// Placeholder for cleanup logic at the end of the node's execution.
        /// </summary>
        protected override void OnStop()
        {
        }

        /// <summary>
        /// Executes the randomly selected child node and returns its state.
        /// </summary>
        protected override State OnUpdate()
        {
            var child = children[current];
            return child.Update();
        }
    }
}
