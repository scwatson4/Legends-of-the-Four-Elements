using AnythingWorld.Animation.Vehicles;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Expands on base class by adding vehicle-specific animations.
    /// </summary>
    [System.Serializable]
    public class WheeledVehicleMoveToGoal : NonJumpingMoveToGoalBase
    {
        private VehicleAnimator _animationController;

        /// <summary>
        /// Finds VehicleAnimator component, and sets animation update callback.
        /// </summary>
        public override void OnInit()
        {
            base.OnInit();

            if (!canRun)
            {
                return;
            }

            PlaceOnGround();

            _animationController = context.GameObject.GetComponentInChildren<VehicleAnimator>();
            if (_animationController)
            {
                UpdateAnimationAction = () => _animationController.SetVelocity(Velocity.magnitude);
            }
        }
        
        /// <summary>
        /// Stops the vehicle's animation when a goal is reached.
        /// </summary>
        protected override void SetGoalReachedParameters()
        {
            base.SetGoalReachedParameters();
            if (_animationController)
            {
                _animationController.SetVelocity(0);
            }
        }
    }
}
