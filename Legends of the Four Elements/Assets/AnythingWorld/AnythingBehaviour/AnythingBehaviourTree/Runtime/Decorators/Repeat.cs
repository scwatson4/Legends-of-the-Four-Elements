using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    [System.Serializable]
    public class Repeat : DecoratorNode
    {
        [Tooltip("Restarts the subtree on success")] public bool restartOnSuccess = true;
        [Tooltip("Restarts the subtree on failure")] public bool restartOnFailure = false;
        [Tooltip("Maximum number of times the subtree will be repeated. Set to 0 to loop forever")] public int maxRepeats = 0;

        int iterationCount = 0;

        /// <summary>
        /// Defines the behavior at the start of the Node's lifecycle.
        /// </summary>
        protected override void OnStart()
        {
            iterationCount = 0;
        }

        /// <summary>
        /// Defines the behavior at the end of the Node's lifecycle.
        /// </summary>
        protected override void OnStop()
        {
        }

        /// <summary>
        /// Updates the state of the Node, determining its success or failure based on the child node's state.
        /// </summary>
        protected override State OnUpdate()
        {
            if (child == null)
            {
                return State.Failure;
            }

            switch (child.Update())
            {
                case State.Running:
                    break;
                case State.Failure:
                    if (restartOnFailure)
                    {
                        iterationCount++;
                        if (iterationCount >= maxRepeats && maxRepeats > 0)
                        {
                            return State.Failure;
                        }
                        else
                        {
                            return State.Running;
                        }
                    }
                    else
                    {
                        return State.Failure;
                    }
                case State.Success:
                    if (restartOnSuccess)
                    {
                        iterationCount++;
                        if (iterationCount >= maxRepeats && maxRepeats > 0)
                        {
                            return State.Success;
                        }
                        else
                        {
                            return State.Running;
                        }
                    }
                    else
                    {
                        return State.Success;
                    }
            }
            return State.Running;
        }
    }


}
