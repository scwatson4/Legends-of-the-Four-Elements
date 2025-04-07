namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// A composite node that executes a specific child node based on a provided index.
    /// The node supports dynamic switching between children during execution if `interruptable` is true.
    /// </summary>
    [System.Serializable]
    public class Switch : CompositeNode
    {
        public NodeProperty<int> index;
        public bool interruptable = true; 
        int currentIndex; 

        /// <summary>
        /// Sets the initial child node to execute based on the provided index.
        /// </summary>
        protected override void OnStart()
        {
            currentIndex = index.Value;
        }

        /// <summary>
        /// Placeholder for cleanup logic at the end of the node's execution.
        /// </summary>
        protected override void OnStop(){}

        /// <summary>
        /// Updates the current child node based on the index. Supports interrupting the current child to switch to another.
        /// </summary>
        protected override State OnUpdate()
        {
            if (interruptable)
            {
                int nextIndex = index.Value;
                if (nextIndex != currentIndex)
                {
                    children[currentIndex].Abort();
                }
                currentIndex = nextIndex;
            }

            if (currentIndex < children.Count)
            {
                return children[currentIndex].Update();
            }
            return State.Failure;
        }
    }
}
