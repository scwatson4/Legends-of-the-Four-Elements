using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Executes a child behavior tree as a subtree of the current behavior tree.
    /// </summary>
    [System.Serializable]
    public class SubTree : ActionNode
    {
        [Tooltip("Behaviour tree asset to run as a subtree")] public BehaviourTree treeAsset;
        [HideInInspector] public BehaviourTree treeInstance;

        /// <summary>
        /// Initializes the subtree by cloning the provided behavior tree asset.
        /// </summary>
        public override void OnInit()
        {
            if (treeAsset)
            {
                treeInstance = treeAsset.Clone();
                treeInstance.Bind(context);
            }
        }

        /// <summary>
        /// Starts the subtree, marking its state as running.
        /// </summary>
        protected override void OnStart()
        {
            if (treeInstance)
            {
                treeInstance.treeState = State.Running;
            }
        }

        /// <summary>
        /// Placeholder for cleanup logic at the end of the subtree's execution.
        /// </summary>
        protected override void OnStop()
        {
        }

        /// <summary>
        /// Updates and returns the subtree's current state.
        /// </summary>
        protected override State OnUpdate()
        {
            if (treeInstance)
            {
                return treeInstance.Update();
            }
            return State.Failure;
        }
    }
}
