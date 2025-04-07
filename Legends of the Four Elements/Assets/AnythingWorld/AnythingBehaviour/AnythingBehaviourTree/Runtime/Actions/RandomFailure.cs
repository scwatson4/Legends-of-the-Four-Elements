using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// An action node that randomly succeeds or fails based on a specified chance of failure.
    /// </summary>
    [System.Serializable]
    public class RandomFailure : ActionNode
    {
        [Range(0,1)]
        [Tooltip("Percentage chance of failure")] public float chanceOfFailure = 0.5f;

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
        /// Determines the node's success or failure randomly based on the chance of failure.
        /// </summary>
        protected override State OnUpdate()
        {
            float value = Random.value;
            if (value < chanceOfFailure)
            {
                return State.Failure;
            }
            return State.Success;
        }
    }
}
