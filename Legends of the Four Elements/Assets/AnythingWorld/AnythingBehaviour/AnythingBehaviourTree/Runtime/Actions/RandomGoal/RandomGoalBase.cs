using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Abstract base class for defining goal-oriented behaviors within a behavior tree,
    /// focusing on generating random movement goals within specified radii.
    /// </summary>
    [System.Serializable]
    public abstract class RandomGoalBase : ActionNode
    {
        protected const float BaseMinGoalSpawnRadius = 5;
        protected const float BaseMaxGoalSpawnRadius = 20;
        
        public NodeProperty<Vector3> newGoal;
        
        public float minGoalSpawnRadius = BaseMinGoalSpawnRadius;
        public float maxGoalSpawnRadius = BaseMaxGoalSpawnRadius;
        
        protected Vector3 Extents;

        /// <summary>
        /// Initializes node properties and checks for necessary components.
        /// </summary>
        public override void OnInit() 
        {
            if (context.GameObject.TryGetComponent(out MovementDataContainer movementDataContainer))
            {
                Extents = movementDataContainer.extents;
            }
            else
            {
                Debug.LogWarning($"{context.GameObject.name} doesn't have MovementDataContainer component, " +
                                 "cannot get model dimensions and generate a goal.");
                canRun = false;
                return;
            }
            
            canRun = true;
        }

        /// <summary>
        /// Ensures the maximum spawn radius is at least as large as the minimum.
        /// </summary>
        protected override void OnStart()
        {
            if (!canRun)
            {
                return;
            }
            // Ensures the maximum spawn radius is at least as large as the minimum.
            maxGoalSpawnRadius = Mathf.Max(minGoalSpawnRadius, maxGoalSpawnRadius);
        }
        
        protected override void OnStop() {}
        
        /// <summary>
        /// Main update loop for the node, attempting to generate and set a new movement goal.
        /// </summary>
        protected override State OnUpdate() 
        {
            if (!canRun)
            {
                return State.Failure;
            }
            
            if (TryGenerateNewMovementGoal())
            {
                return State.Success;
            }

            return State.Running;
        }
        
        // Abstract method to generate a new movement goal, to be implemented by derived classes.
        protected abstract bool TryGenerateNewMovementGoal();
        
#if UNITY_EDITOR
        /// <summary>
        /// Draws wire discs to represent the goal spawn area radii.
        /// </summary>
        public override void OnDrawGizmosSelectedTree()
        {
            GUI.color = Color.white;
            Gizmos.color = Color.white;
            
            var trPosition = context.Transform.position;
            
            UnityEditor.Handles.DrawWireDisc(trPosition, Vector3.up, minGoalSpawnRadius);
            UnityEditor.Handles.DrawWireDisc(trPosition, Vector3.up, maxGoalSpawnRadius);
            UnityEditor.Handles.Label(trPosition + (Vector3.left * minGoalSpawnRadius + Vector3.left), "Goal Spawn Area");
        }
#endif
    }
}
