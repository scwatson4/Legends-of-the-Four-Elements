using UnityEngine;

namespace AnythingWorld.Behaviour
{
    /// <summary>
    /// An abstract base class designed for flying and swimming entities, providing mechanisms to ensure
    /// movement within specified vertical constraints alongside random goal generation.
    /// </summary>
    public abstract class FlyingSwimmingRandomMovementBase : NonJumpingRandomMovementBase
    {
        protected float MinHeightCoordinate;
        protected float MaxHeightCoordinate;
        
        // Initializes the entity, disabling gravity and setting min and max movement height.
        protected override void Start()
        {
            base.Start();
            
            if (Rb)
            {
                Rb.useGravity = false;
            }
            
            HasVerticalMovement = true;
            SetHeightLimits();
        }
        
        // Updates height limits when properties change in the Inspector.
        protected override void OnValidate()
        {
            base.OnValidate();
            SetHeightLimits();
        }

        // Abstract method to be implemented by subclasses for setting min/max height constraints.
        protected abstract void SetHeightLimits();

        // Generates a new goal position within vertical and radial constraints, ensuring it's reachable.
        protected override bool TryGenerateNewGoal(out Vector3 newGoal)
        {
            var trPosition = transform.position;
            var transformY = trPosition.y;
        
            // Adjust position if below minimum height.
            if (transformY < MinHeightCoordinate)
            {
                var dif = MinHeightCoordinate - transformY + Extents.y;
                newGoal = trPosition + Vector3.up * (dif + stoppingDistance);
                return true;
            }
            
            // Adjust position if above maximum height.
            if (transformY > MaxHeightCoordinate)
            {
                var dif = transformY - MaxHeightCoordinate + Extents.y;
                newGoal = trPosition - Vector3.up * (dif + stoppingDistance);
                return true;
            }
            
            // Generate a goal position within a spherical radius.
            var radius = Random.Range(minGoalSpawnRadius, maxGoalSpawnRadius);
            newGoal = Random.onUnitSphere * radius + trPosition;
            
            // Clamp Y position within height constraints.
            var clampedY = Mathf.Clamp(newGoal.y, MinHeightCoordinate, MaxHeightCoordinate);
            
            // Recalculate position if Y is clamped.
            if (!Mathf.Approximately(newGoal.y, clampedY))
            {
                var heightDif = clampedY - transformY;
                var xzRadius = Mathf.Sqrt(radius * radius - heightDif * heightDif);
                var directionXZ = new Vector3(newGoal.x - trPosition.x, 0, newGoal.z - trPosition.z).normalized;
                newGoal = trPosition + directionXZ * xzRadius;
                newGoal.y = clampedY;
            }
            
            // Validate the new position with a raycast to ensure it's not out of bounds.
            return Physics.Raycast(newGoal + Vector3.up * MaxRaycastDistance / 2, Vector3.down, 
                MaxRaycastDistance);
        }
#if UNITY_EDITOR
        // Draw vertical wire discs to represent 3d goal generation radius.
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            if (!showGizmos || Application.isPlaying)
            {
                return;
            }

            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.right, minGoalSpawnRadius);
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.right, maxGoalSpawnRadius);
        }
#endif
    }
}