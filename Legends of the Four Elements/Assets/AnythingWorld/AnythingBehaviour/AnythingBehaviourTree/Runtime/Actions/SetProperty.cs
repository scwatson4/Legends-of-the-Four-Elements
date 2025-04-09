namespace AnythingWorld.Behaviour.Tree 
{
    /// <summary>
    /// An action node that sets a specified property on the blackboard to a new value.
    /// Useful for modifying the state of the behavior tree's environment.
    /// </summary>
    [System.Serializable]
    public class SetProperty : ActionNode
    {
        public BlackboardKeyValuePair pair;

        /// <summary>
        /// Placeholder for initialization logic at the start of the node's execution.
        /// </summary>
        protected override void OnStart()
        {
        }

        /// <summary>
        /// Placeholder for cleanup logic at the end of the node's execution.
        /// </summary>
        protected override void OnStop()
        {
        }

        /// <summary>
        /// Sets a property on the blackboard and always returns success.
        /// </summary>
        protected override State OnUpdate()
        {
            pair.WriteValue();
            
            return State.Success;
        }
    }
}
