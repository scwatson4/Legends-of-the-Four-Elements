namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// An action node that compares two properties (keys) in the behavior tree's blackboard for equality.
    /// If the properties are equal, the node returns success; otherwise, it returns failure.
    /// </summary>
    [System.Serializable]
    public class CompareProperty : ActionNode
    {
        public BlackboardKeyValuePair pair;

        /// <summary>
        /// Initializes any required state before the node starts executing.
        /// </summary>
        protected override void OnStart()
        {
        }

        /// <summary>
        /// Cleans up the node's state after it finishes executing.
        /// </summary>
        protected override void OnStop()
        {
        }

        /// <summary>
        /// Compares two properties from the blackboard. Returns success if they are equal, otherwise returns failure.
        /// </summary>
        protected override State OnUpdate()
        {
            BlackboardKey source = pair.value;
            BlackboardKey destination = pair.key;

            if (source != null && destination != null)
            {
                if (destination.Equals(source))
                {
                    return State.Success;
                }
            }

            return State.Failure;
        }
    }
}
