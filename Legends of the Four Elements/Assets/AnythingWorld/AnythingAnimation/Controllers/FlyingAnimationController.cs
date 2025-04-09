using UnityEngine;

namespace AnythingWorld.Animation
{
    public class FlyingAnimationController : LegacyAnimationController
    {
        private AnimationState currentState = AnimationState.idle;

        public void Fly()
        {
            if (currentState != AnimationState.fly)
            {
                base.CrossFadeAnimation("fly");
                currentState = AnimationState.fly;
            }
        }
        public void Idle()
        {
            if (currentState != AnimationState.idle)
            {
                base.CrossFadeAnimation("idle");
                currentState = AnimationState.idle;
            }

        }

        private enum AnimationState
        {
            fly,
            idle
        }
    }
}
