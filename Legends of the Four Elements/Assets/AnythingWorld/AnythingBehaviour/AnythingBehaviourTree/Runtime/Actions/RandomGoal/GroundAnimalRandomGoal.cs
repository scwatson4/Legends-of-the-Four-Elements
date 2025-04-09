using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Generates random goals on topmost surface using a specified radius from the animal's current position
    /// and ensuring the goal is not out of bounds.
    /// </summary>
    [System.Serializable]
    public class GroundAnimalRandomGoal : RandomGoalBase
    {
        /// <summary>
        /// Generates a new navigational goal within a specified radius.
        /// </summary>
        protected override bool TryGenerateNewMovementGoal()
        {
            var radius = Random.Range(minGoalSpawnRadius, maxGoalSpawnRadius);
            float angle = Random.Range(0, 2 * Mathf.PI);
            float x = radius * Mathf.Cos(angle);
            float z = radius * Mathf.Sin(angle);

            var groundGoal = new Vector3(x, 0, z) + context.Transform.position;

            if (!Physics.Raycast(groundGoal + Vector3.up * MoveToGoalBase.MaxRaycastDistance / 2, Vector3.down, out var hit,
                    MoveToGoalBase.MaxRaycastDistance))
            {
                return false;
            }
            
            newGoal.Value = hit.point + Vector3.up * Extents.y;
            return true;
        }
    }
}
