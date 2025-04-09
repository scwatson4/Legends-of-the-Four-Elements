using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Defines goal-generation logic for wheeled vehicles, generating randomized goals within a specified radius
    /// from the vehicle's current position.
    /// </summary>
    [System.Serializable]
    public class WheeledVehicleRandomGoal : RandomGoalBase
    {
        /// <summary>
        /// Generates a new goal position for the vehicle to move towards, ensuring the goal is reachable
        /// by verifying there's ground beneath the target location.
        /// </summary>
        protected override bool TryGenerateNewMovementGoal()
        {
            var radius = Random.Range(minGoalSpawnRadius, maxGoalSpawnRadius);
            float angle = Random.Range(0, 2 * Mathf.PI); 
            float x = radius * Mathf.Cos(angle); 
            float z = radius * Mathf.Sin(angle); 

            newGoal.Value = new Vector3(x, 0, z) + context.Transform.position;
         
            return Physics.Raycast(newGoal.Value + Vector3.up * MoveToGoalBase.MaxRaycastDistance / 2, Vector3.down, 
                MoveToGoalBase.MaxRaycastDistance);
        }
    }
}
