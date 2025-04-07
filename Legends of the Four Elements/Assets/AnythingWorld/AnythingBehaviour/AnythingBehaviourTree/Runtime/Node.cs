using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Abstract base class for nodes in a Behaviour Tree, defining the common functionality and lifecycle methods.
    /// </summary>
    [System.Serializable]
    public abstract class Node
    {
        public enum State
        {
            Running,
            Failure,
            Success
        }

        [HideInInspector] public State state = State.Running;
        [HideInInspector] public bool started = false;
        [HideInInspector] public string guid = System.Guid.NewGuid().ToString();
        [HideInInspector] public Vector2 position;
        [HideInInspector] public Context context;
        [HideInInspector] public Blackboard blackboard;
        [TextArea] public string description;
        [Tooltip("When enabled, the nodes OnDrawGizmos will be invoked")] public bool drawGizmos = false;
        protected bool canRun = true;
        
        public virtual void OnInit() {}

        /// <summary>
        /// Updates the node's state and triggers lifecycle methods based on the state.
        /// </summary>
        public State Update()
        {
            if (!started)
            {
                OnStart();
                started = true;
            }

            state = OnUpdate();

            if (state != State.Running)
            {
                OnStop();
                started = false;
            }

            return state;
        }

        /// <summary>
        /// Aborts the execution of the node and resets its state.
        /// </summary>
        public void Abort()
        {
            BehaviourTree.Traverse(this, (node) =>
            {
                node.started = false;
                node.state = State.Running;
                node.OnStop();
            });
        }

        public virtual void OnDrawGizmosSelectedTree() { }

        protected abstract void OnStart();
        protected abstract void OnStop();
        protected abstract State OnUpdate();

        // Utility method for logging messages with the node's type.
        protected virtual void Log(string message)
        {
            Debug.Log($"[{GetType()}]{message}");
        }
    }
}
