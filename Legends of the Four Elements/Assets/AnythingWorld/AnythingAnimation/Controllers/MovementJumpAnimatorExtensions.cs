using System.Linq;
#if UNITY_EDITOR
using UnityEditor.Animations;
#endif
using UnityEngine;

namespace AnythingWorld.Animation
{
    /// <summary>
    /// Provides extension methods for the Animator component to control jump animations
    /// and speed-based movement animations.
    /// </summary>
    public static class MovementJumpAnimatorExtensions
    {
        private const string BlendTreeStateName = "Blend Tree";
        private const string JumpFallName = "jump_fall";
        private const string JumpEndName = "jump_end";
        private static readonly int JumpId = Animator.StringToHash("Jump");
        private static readonly int FallingId = Animator.StringToHash("Falling");
        private static readonly int AnimationSpeedIdx = Animator.StringToHash("Speed");

#if UNITY_EDITOR
        // Calculates the total duration of jump animations, excluding any blend tree states.
        public static float GetJumpAnimationDuration(this Animator animator)
        {
            float animationDuration = 0;
            var animatorStates = GetAnimatorStates(animator);

            foreach (var childState in animatorStates)
            {
                if (childState.state.name == BlendTreeStateName)
                {
                    continue;
                }

                animationDuration += childState.state.transitions[0].duration;
            }

            return animationDuration;
        }

        // Retrieves the duration of the jump end animation.
        public static float GetJumpEndAnimationDuration(this Animator animator)
        {
            var animatorStates = GetAnimatorStates(animator);

            foreach (var childState in animatorStates)
            {
                if (childState.state.name == JumpEndName)
                {
                    return childState.state.transitions[0].duration;
                }
            }

            return -1;
        }
#endif
        // Checks if the jump fall animation is currently playing.
        public static bool IsPlayingJumpFallAnimation(this Animator animator) => 
            animator.GetCurrentAnimatorStateInfo(0).IsName(JumpFallName);

        // Verifies the existence of any jump animation within the animator's animations.
        public static bool DoesJumpAnimationExist(this Animator animator) =>
            animator.runtimeAnimatorController.animationClips.Any(x => x.name.StartsWith("jump") && !x.empty);

        // Verifies the existence of any run animation within the animator's animations.
        public static bool DoesRunAnimationExist(this Animator animator) =>
            animator.runtimeAnimatorController.animationClips.Any(x => x.name.StartsWith("run") && !x.empty);

        // Initiates the jump animation.
        public static void JumpStart(this Animator animator) => animator.SetTrigger(JumpId);
        
        // Stopping the falling animation transitioning to jump end animation.
        public static void JumpEnd(this Animator animator) => animator.SetBool(FallingId, false);
        
        // Triggers the jump fall animation.
        public static void JumpFall(this Animator animator) => animator.SetBool(FallingId, true);

        // Adjusts the movement animation based on the character's movement speed.
        public static void BlendMovementAnimationOnSpeed(this Animator animator, float speed) =>
            animator.SetFloat(AnimationSpeedIdx, speed);
        
#if UNITY_EDITOR
        // Retrieves all states within the animator's first layer.
        private static ChildAnimatorState[] GetAnimatorStates(Animator animator)
        {
            var overrideController = animator.runtimeAnimatorController as AnimatorOverrideController;
            var controller = overrideController?.runtimeAnimatorController as AnimatorController;
            return controller?.layers[0].stateMachine.states ?? new ChildAnimatorState[0];
        }
#endif
    }
}