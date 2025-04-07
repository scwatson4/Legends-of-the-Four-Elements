using System.Collections.Generic;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// A parallel node that executes all of its children simultaneously and succeeds if all children succeed.
    /// </summary>
    [System.Serializable]
    public class Parallel : CompositeNode
    {
        List<State> childrenLeftToExecute = new List<State>();

        /// <summary>
        /// Initializes the list of children's execution states.
        /// </summary>
        protected override void OnStart()
        {
            childrenLeftToExecute.Clear();
            children.ForEach(_ =>
            {
                childrenLeftToExecute.Add(State.Running);
            });
        }

        /// <summary>
        /// Placeholder for cleanup logic at the end of the parallel node's execution.
        /// </summary>
        protected override void OnStop()
        {
        }

        /// <summary>
        /// Updates all children and returns success only if all children succeed.
        /// </summary>
        protected override State OnUpdate()
        {
            bool stillRunning = false;
            for (int i = 0; i < childrenLeftToExecute.Count; ++i)
            {
                if (childrenLeftToExecute[i] == State.Running)
                {
                    var status = children[i].Update();
                    if (status == State.Failure)
                    {
                        AbortRunningChildren();
                        return State.Failure;
                    }

                    if (status == State.Running)
                    {
                        stillRunning = true;
                    }

                    childrenLeftToExecute[i] = status;
                }
            }

            return stillRunning ? State.Running : State.Success;
        }

        // Aborts all currently running children.
        void AbortRunningChildren()
        {
            for (int i = 0; i < childrenLeftToExecute.Count; ++i)
            {
                if (childrenLeftToExecute[i] == State.Running)
                {
                    children[i].Abort();
                }
            }
        }
    }
}
