using AnythingWorld.Animation;
using UnityEngine;

namespace AnythingWorld.Behaviour
{
    /// <summary>
    /// Manages random movement behaviors for birds, incorporating specific flying height limits
    /// and animations to simulate realistic bird flight patterns.
    /// </summary>
    public class BirdRandomMovement : FlyingSwimmingRandomMovementBase
    {
        public float groundYCoordinate;
        public float minFlyingHeight = 2;
        public float maxFlyingHeight = 10;
        
        private const float GlidingHeightDifference = 1f;
        
        private static readonly int EffortfulFlap = Animator.StringToHash("EffortfulFlap");
        private static readonly int Flap = Animator.StringToHash("Flap");
        private static readonly int InFlight = Animator.StringToHash("InFlight");

        private Animator _animationController;
        
        // Sets the bird to be in flight mode if an animation controller is found. Otherwise
        // uses a generic flying animation controller as a fallback.
        protected override void Start()
        {
            base.Start();
            
            _animationController = GetComponentInChildren<Animator>(); 

            if (_animationController)
            {
                _animationController.SetBool(InFlight, true);
            }
            else
            {
                var flyingAnimationController = GetComponentInChildren<FlyingAnimationController>();
                if (flyingAnimationController)
                {
                    flyingAnimationController.Fly();
                }
            }
        }

        // Sets the vertical movement limits for the bird based on ground coordinates and height preferences.
        protected override void SetHeightLimits()
        {
            MinHeightCoordinate = groundYCoordinate + minFlyingHeight + Extents.y;
            MaxHeightCoordinate = groundYCoordinate + maxFlyingHeight - Extents.y;
        }

        // Stops effortful flapping when the goal is reached.
        protected override void SetGoalReachedParameters()
        {
            base.SetGoalReachedParameters();
            if (_animationController)
            {
                _animationController.SetBool(EffortfulFlap, false);
            }
        }
        
        // Sets a new goal for the bird and determines if the bird should glide based on the height difference
        // to the new goal.
        protected override void SetNewGoalParameters(Vector3 newGoalPosition)
        {
            base.SetNewGoalParameters(newGoalPosition);
            
            if (_animationController)
            {
                var isGliding = transform.position.y - GoalPosition.y > GlidingHeightDifference;
                _animationController.SetBool(EffortfulFlap, !isGliding);
                _animationController.SetTrigger(Flap);
            }
        }
    }
}