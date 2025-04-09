namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// A selector node that iterates through its children and selects the first one that succeeds or is running.
    /// Returns failure if all children fail.
    /// </summary>
    [System.Serializable]
    public class Selector : CompositeNode
    {
        protected int current; 

        /// <summary>
        /// Resets the current child index to 0 at the start of the node's execution.
        /// </summary>
        protected override void OnStart()
        {
            current = 0;
        }

        /// <summary>
        /// Placeholder for cleanup logic at the end of the node's execution.
        /// </summary>
        protected override void OnStop()
        {
        }

        /// <summary>
        /// Iterates through its children and selects the first one that succeeds or is running.
        /// Returns failure if all children fail.
        /// </summary>
        protected override State OnUpdate()
        {
            for (int i = current; i < children.Count; ++i)
            {
                current = i;
                var child = children[current];

                switch (child.Update())
                {
                    case State.Running:
                        return State.Running;
                    case State.Success:
                        return State.Success;
                    case State.Failure:
                        continue;
                }
            }

            return State.Failure;
        }
    }
}
