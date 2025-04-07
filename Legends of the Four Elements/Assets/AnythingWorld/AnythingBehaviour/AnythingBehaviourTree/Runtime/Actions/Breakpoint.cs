using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// A Behavior Tree action node that serves as a breakpoint, pausing the editor during execution
    /// for debugging purposes.
    /// </summary>
    [System.Serializable]
    public class Breakpoint : ActionNode
    {
        /// <summary>
        /// Triggers a breakpoint in the Unity Editor, pausing execution when this node starts.
        /// </summary>
        protected override void OnStart()
        {
            Debug.Log("Trigging Breakpoint");
            Debug.Break();
        }

        /// <summary>
        /// Placeholder for required override, no action taken on stop.
        /// </summary>
        protected override void OnStop()
        {
        }

        /// <summary>
        /// Always returns success, indicating the breakpoint action completed.
        /// </summary>
        protected override State OnUpdate()
        {
            return State.Success;
        }
    }
}
