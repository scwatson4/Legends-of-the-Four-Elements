using AnythingWorld.Animation;
using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Expands on base class by setting up specific flying movement parameters
    /// and animation for flying vehicles.
    /// </summary>
    [System.Serializable]
    public class FlyingVehicleMoveToGoal : MoveToGoal3D
    {
        // Good default for angularSpeed = 60, for turnAcceleration = 15.
        
        private FlyingVehicleAnimator _animationController;
        
        /// <summary>
        /// Initializes the flying vehicle, setting up its animation controller and animation callback.
        /// </summary>
        public override void OnInit()
        {
            base.OnInit();
            
            if (!canRun)
            {
                return;
            }
            
            Rb.useGravity = false;
            HasVerticalMovement = true;
            
            _animationController = context.GameObject.GetComponentInChildren<FlyingVehicleAnimator>();
            
            if (_animationController)
            {
                UpdateAnimationAction = () => UpdateBladesRotationAnimation(Velocity.magnitude);
            }
        }

        /// <summary>
        /// Updates the flying vehicle's blade rotation animation based on current speed.
        /// </summary>
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
