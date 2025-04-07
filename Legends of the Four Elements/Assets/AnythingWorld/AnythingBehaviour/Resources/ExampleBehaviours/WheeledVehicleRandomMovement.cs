using AnythingWorld.Animation.Vehicles;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AnythingWorld.Behaviour
{
    /// <summary>
    /// Manages random movement for wheeled vehicles, leveraging the base class
    /// and integrating vehicle-specific animation controls and goal generation.
    /// </summary>
    public class WheeledVehicleRandomMovement : NonJumpingRandomMovementBase
    {
        private VehicleAnimator _animationController;

        // Finds VehicleAnimator component, and sets animation update callback.
        protected override void Start()
        {
            base.Start();
            PlaceOnGround();
            
            _animationController = GetComponentInChildren<VehicleAnimator>();
            if (_animationController)
            {
                UpdateAnimationAction = () => _animationController.SetVelocity(Velocity.magnitude);
            }
        }
        
        // Stops the vehicle's animation when a goal is reached.
        protected override void SetGoalReachedParameters()
        {
            base.SetGoalReachedParameters();
            if (_animationController)
            {
                _animationController.SetVelocity(0);
            }
        }
        
        // Generates a new goal position for the vehicle to move towards.
        protected override bool TryGenerateNewGoal(out Vector3 newGoal)
        {
            var radius = Random.Range(minGoalSpawnRadius, maxGoalSpawnRadius);
            float angle = Random.Range(0, 2 * Mathf.PI);
            float x = radius * Mathf.Cos(angle); 
            float z = radius * Mathf.Sin(angle);

            newGoal = new Vector3(x, 0, z) + transform.position;
            
            return Physics.Raycast(newGoal + Vector3.up * MaxRaycastDistance / 2, Vector3.down, 
                MaxRaycastDistance);
        }
    }
}
