using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// An action node that logs a specified message to the console.
    /// Useful for debugging or tracking behavior tree execution.
    /// </summary>
    [System.Serializable]
    public class Log : ActionNode
    {
        [Tooltip("Message to log to the console")]
        public NodeProperty<string> message = new NodeProperty<string>();

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
        /// Logs a message to the console and always returns success.
        /// </summary>
        protected override State OnUpdate()
        {
            Debug.Log($"{message.Value}");
            return State.Success;
        }
    }
}
