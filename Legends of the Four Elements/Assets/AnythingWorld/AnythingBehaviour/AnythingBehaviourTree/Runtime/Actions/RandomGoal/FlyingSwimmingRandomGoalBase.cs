using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// An abstract base class designed for flying and swimming entities, providing mechanisms
    /// to generate a randomized goal within specified vertical constraints.
    /// </summary>
    [System.Serializable]
    public abstract class FlyingSwimmingRandomGoalBase : RandomGoalBase
    {
        public NodeProperty<float> stoppingDistance = new NodeProperty<float>(.5f);
        
        protected float MinHeightCoordinate;
        protected float MaxHeightCoordinate;
        
        protected override void OnStart()
        {
            base.OnStart();
            if (!canRun)
            {
                return;
            }

            SetHeightLimits();
        }
        
        // Abstract method to be implemented by subclasses for setting min/max height constraints.
        protected abstract void SetHeightLimits();
        
        /// <summary>
        /// Generates a new goal position within vertical and radial constraints, ensuring it's reachable.
        /// </summary>
        protected override bool TryGenerateNewMovementGoal()
        {
            var trPosition = context.Transform.position;
            var transformY = trPosition.y;
        
            // Adjust position if below minimum height.
            if (transformY < MinHeightCoordinate)
            {
                var dif = MinHeightCoordinate - transformY + Extents.y;
                newGoal.Value = trPosition + Vector3.up * (dif + stoppingDistance);
                return true;
            }
            
            // Adjust position if above maximum height.
            if (transformY > MaxHeightCoordinate)
            {
                var dif = transformY - MaxHeightCoordinate + Extents.y;
                newGoal.Value = trPosition - Vector3.up * (dif + stoppingDistance);
                return true;
            }
            
            // Generate a goal position within a spherical radius.
            var radius = Random.Range(minGoalSpawnRadius, maxGoalSpawnRadius);
            var tentativeGoal = Random.onUnitSphere * radius + trPosition;
            
            // Clamp Y position within height constraints.
            var clampedY = Mathf.Clamp(tentativeGoal.y, MinHeightCoordinate, MaxHeightCoordinate);
            
            // Recalculate position if Y is clamped.
            if (!Mathf.Approximately(tentativeGoal.y, clampedY))
            {
                var heightDif = clampedY - transformY;
                var xzRadius = Mathf.Sqrt(radius * radius - heightDif * heightDif);
                var directionXZ = new Vector3(tentativeGoal.x - trPosition.x, 0, 
                    tentativeGoal.z - trPosition.z).normalized;
                tentativeGoal = trPosition + directionXZ * xzRadius;
                tentativeGoal.y = clampedY;
            }
            
            newGoal.Value = tentativeGoal;
            
            // Validate the new position with a raycast to ensure it's not out of bounds.
            return Physics.Raycast(newGoal + Vector3.up * MoveToGoalBase.MaxRaycastDistance / 2, Vector3.down, 
                MoveToGoalBase.MaxRaycastDistance);
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// Draw vertical wire discs to represent 3d goal generation radius.
        /// </summary>
        public override void OnDrawGizmosSelectedTree()
        {
            base.OnDrawGizmosSelectedTree();

            UnityEditor.Handles.DrawWireDisc(context.Transform.position, Vector3.right, minGoalSpawnRadius);
            UnityEditor.Handles.DrawWireDisc(context.Transform.position, Vector3.right, maxGoalSpawnRadius);
        }
#endif
    }
}
