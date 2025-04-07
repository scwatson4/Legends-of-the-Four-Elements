namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Forces child node to succeed in case of failure.
    /// </summary>
    [System.Serializable]
    public class Succeed : DecoratorNode
    {
        /// <summary>
        /// Defines the behavior at the start of the Node's lifecycle.
        /// </summary>
        protected override void OnStart(){}

        /// <summary>
        /// Defines the behavior at the end of the Node's lifecycle.
        /// </summary>
        protected override void OnStop(){}

        /// <summary>
        /// Updates the state of the Node, determining its success or failure based on the child node's state.
        /// </summary>
        protected override State OnUpdate()
        {
            if (child == null)
            {
                return State.Failure;
            }

            var state = child.Update();
            if (state == State.Failure)
            {
                return State.Success;
            }
            return state;
        }
    }
}
