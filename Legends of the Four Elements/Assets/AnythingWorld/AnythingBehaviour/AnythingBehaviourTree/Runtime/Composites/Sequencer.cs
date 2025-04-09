namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// A composite node that executes its child nodes in order, succeeding if all children succeed.
    /// If any child fails, the sequencer fails. It proceeds to the next child only if the current child succeeds.
    /// </summary>
    [System.Serializable]
    public class Sequencer : CompositeNode
    {
        protected int current;

        /// <summary>
        /// Resets the current index to 0 at the start of execution.
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
        /// Sequentially updates child nodes until one fails or all succeed.
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
                    case State.Failure:
                        return State.Failure;
                    case State.Success:
                        continue;
                }
            }

            return State.Success;
        }
    }
}
