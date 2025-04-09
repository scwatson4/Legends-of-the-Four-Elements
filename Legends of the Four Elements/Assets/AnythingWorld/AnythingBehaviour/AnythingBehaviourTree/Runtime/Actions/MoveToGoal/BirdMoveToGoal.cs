using AnythingWorld.Animation;
using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Expands on base class by adding animations of bird movement varying according to relative goal height.
    /// </summary>
    [System.Serializable]
    public class BirdMoveToGoal : MoveToGoal3D
    {
        private const float GlidingHeightDifference = 1f;
        
        private static readonly int EffortfulFlap = Animator.StringToHash("EffortfulFlap");
        private static readonly int Flap = Animator.StringToHash("Flap");
        private static readonly int InFlight = Animator.StringToHash("InFlight");

        private Animator _animationController;

        /// <summary>
        /// Sets the bird to be in flight mode if an animation controller is found. Otherwise
        /// uses a generic flying animation controller as a fallback.
        /// </summary>
        public override void OnInit()
        {
            base.OnInit();
            if (!canRun)
            {
                return;
            }
            
            _animationController = context.GameObject.GetComponentInChildren<Animator>(); 

            if (_animationController)
            {
                _animationController.SetBool(InFlight, true);
            }
            else
            {
                var flyingAnimationController = context.GameObject.GetComponentInChildren<FlyingAnimationController>();
                if (flyingAnimationController)
                {
                    flyingAnimationController.Fly();
                }
            }
        }
        
        /// <summary>
        /// Determines if the bird should glide based on the height difference to the new goal.
        /// </summary>
        protected override void OnStart()
        {
            base.OnStart();
            
            if (!canRun)
            {
                return;
            }

            if (_animationController)
            {
                var isGliding = context.Transform.position.y - goalPosition.Value.y > GlidingHeightDifference;
                _animationController.SetBool(EffortfulFlap, !isGliding);
                _animationController.SetTrigger(Flap);
            }
        }
        
        /// <summary>
        /// Stops effortful flapping when the goal is reached.
        /// </summary>
        protected override void SetGoalReachedParameters()
        {
            base.SetGoalReachedParameters();
            if (_animationController)
            {
                _animationController.SetBool(EffortfulFlap, false);
            }
        }
    }
}
