using System.Linq;
using UnityEngine;

namespace AnythingWorld.Animation
{
    /// <summary>
    /// Controls animation transitions for running, walking, idling, and jumping states in a character animation system.
    /// </summary>
    public class MovementJumpLegacyController : LegacyAnimationController
    {
        private const string JumpStartName = "jump_start";
        private const string JumpFallName = "jump_fall";
        private const string JumpEndName = "jump_end";
        
        private const string RunName = "run";

        public float runThreshold = 1.5f;
        public float walkThreshold = 0.01f;
        
        [HideInInspector] public float currentAnimationSpeed = 1;
        
        private AnimationState _currentState = AnimationState.idle;

        // Returns the total duration of jump animations.
        public float GetJumpAnimationDuration() => loadedAnimationDurations.Sum();

        // Returns the duration of the jump end animation.
        public float GetJumpEndAnimationDuration() => animationNamesToDurations[JumpEndName];

        // Checks if jump animation is available.
        public bool DoesJumpAnimationExist() => loadedAnimationNames.Contains(JumpStartName);
        
        // Checks if run animation is available.
        public bool DoesRunAnimationExist() => loadedAnimationNames.Contains(RunName);

        // Sets the speed for all jump-related animations.
        public void SetJumpAnimationSpeed(float speed)
        {
            currentAnimationSpeed = speed;
            animationPlayer[JumpStartName].speed = speed;
            animationPlayer[JumpFallName].speed = speed;
            animationPlayer[JumpEndName].speed = speed;
        }
        
        // Retrieves the current speed of the jump animation. Since we set all of them to the same value we can take any.
        public float GetJumpAnimationSpeed() => animationPlayer[JumpFallName].speed;
        
        // Transitions the character movement animation between walking, running, and idling based on movement speed.
        public void BlendMovementAnimationOnSpeed(float speed)
        {
            if (_currentState == AnimationState.jump_start || _currentState == AnimationState.jump_fall || 
                _currentState == AnimationState.jump_end)
            {
                return;
            }
            if (loadedAnimationNames.Contains("run") && speed > runThreshold)
            {
                Run();
            }
            else if (speed > walkThreshold)
            {
                Walk();
            }
            else
            {
                Idle();
            }
        }
        
        // Activates the walk animation and updates the current state.
        public void Walk()
        {
            if (_currentState != AnimationState.walk)
            {
                base.CrossFadeAnimation("walk");
                _currentState = AnimationState.walk;
            }

        }
        
        // Activates the run animation and updates the current state.
        public void Run()
        {
            if (_currentState != AnimationState.run)
            {
                base.CrossFadeAnimation("run");
                _currentState = AnimationState.run;
            }
        }
        
        // Activates the idle animation and updates the current state.
        public void Idle()
        {
            if (_currentState != AnimationState.idle)
            {
                base.CrossFadeAnimation("idle");
                _currentState = AnimationState.idle;
            }
        }
        
        // Initiates the jump start animation and transitions the state accordingly.
        public void JumpStart()
        {
            if (_currentState != AnimationState.jump_start)
            {
                _currentState = AnimationState.jump_start;
                var stateDuration = animationNamesToDurations[JumpStartName];
                
                base.CrossFadeAnimation(JumpStartName);
                Invoke(nameof(JumpFall), stateDuration / currentAnimationSpeed);
            }
        }

        // Activates the jump fall animation and updates the current state.
        public void JumpFall()
        {
            if (_currentState != AnimationState.jump_fall)
            {
                base.CrossFadeAnimation(JumpFallName);
                _currentState = AnimationState.jump_fall;
            }
        }
        
        // Activates the jump end animation and transitions the state accordingly.
        public void JumpEnd()
        {
            if (_currentState != AnimationState.jump_end)
            {
                _currentState = AnimationState.jump_end;
                var stateDuration = animationNamesToDurations[JumpEndName];
     
                base.CrossFadeAnimation(JumpEndName);
                Invoke(nameof(ExitLand), stateDuration / currentAnimationSpeed);
            }
        }
        
        // Transitions from the jump end state back to idle.
        private void ExitLand()
        {
            if (_currentState == AnimationState.jump_end)
            {
                Idle();
            }
        }
        
        // Defines the possible animation states for the character.
        private enum AnimationState
        {
            idle,
            walk,
            run,
            jump_start,
            jump_fall,
            jump_end
        }
    }
}
