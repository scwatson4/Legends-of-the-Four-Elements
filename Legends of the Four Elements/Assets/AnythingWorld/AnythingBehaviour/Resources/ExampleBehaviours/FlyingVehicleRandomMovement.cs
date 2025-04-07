using AnythingWorld.Animation;
using UnityEngine;

namespace AnythingWorld.Behaviour
{
    /// <summary>
    /// Controls random movement behavior for flying vehicles, extending flying/swimming movement base class.
    /// This class specifically handles setting up flying parameters and animation for flying vehicles.
    /// </summary>
    public class FlyingVehicleRandomMovement : FlyingSwimmingRandomMovementBase
    {
        private const float DefaultFlyingAngularSpeed = 60;
        private const float DefaultFlyingTurnAcceleration = 15;
        private const float DefaultFlyingMinGoalSpawnRadius = 10;
        private const float DefaultFlyingMaxGoalSpawnRadius = 40;

        public float groundYCoordinate;
        public float minFlyingHeight = 15;
        public float maxFlyingHeight = 35;

        private bool _isInitialized;
        private FlyingVehicleAnimator _animationController;

        // Initializes the flying vehicle, setting up its animation controller and animation callback.
        protected override void Start()
        {
            base.Start();
            
            _animationController = GetComponentInChildren<FlyingVehicleAnimator>();
            
            if (_animationController)
            {
                UpdateAnimationAction = () => UpdateBladesRotationAnimation(Velocity.magnitude);
            }
            
            SetDefaultFlyingMovementParameters();
        }

        // Sets the minimum and maximum height constraints for flying based on ground coordinate and flying height range.
        protected override void SetHeightLimits()
        {
            MinHeightCoordinate = groundYCoordinate + minFlyingHeight + Extents.y;
            MaxHeightCoordinate = groundYCoordinate + maxFlyingHeight - Extents.y;
        }

        // Sets default flying movement parameters if they haven't been customized in the Inspector.
        protected override void OnValidate()
        {
            base.OnValidate();
            SetDefaultFlyingMovementParameters();
        }

        // Sets default movement parameters if they haven't been customized in the Inspector.
        private void SetDefaultFlyingMovementParameters()
        {
            // Prevents re-initialization if already set.
            if (_isInitialized)
            {
                return;
            }
            
            if (Mathf.Approximately(maxGoalSpawnRadius, BaseMaxGoalSpawnRadius) &&
                Mathf.Approximately(minGoalSpawnRadius, BaseMinGoalSpawnRadius))
            {
                minGoalSpawnRadius = DefaultFlyingMinGoalSpawnRadius;
                maxGoalSpawnRadius = DefaultFlyingMaxGoalSpawnRadius;
            }
            
            if (Mathf.Approximately(angularSpeed, BaseAngularSpeed))
            {
                angularSpeed = DefaultFlyingAngularSpeed;
            }
         
            if (Mathf.Approximately(turnAcceleration, BaseTurnAcceleration))
            {
                turnAcceleration = DefaultFlyingTurnAcceleration;
            }
            
            _isInitialized = true;
        }

        // Updates the flying vehicle's blade rotation animation based on current speed.
        private void UpdateBladesRotationAnimation(float currentSpeed)
        {
            if (_animationController)
            {
                if (currentSpeed > 0.1)
                {
                    _animationController.Accelerate();
                }
                else
                {
                    _animationController.Decelerate();
                }
            }
        }
    }
}
