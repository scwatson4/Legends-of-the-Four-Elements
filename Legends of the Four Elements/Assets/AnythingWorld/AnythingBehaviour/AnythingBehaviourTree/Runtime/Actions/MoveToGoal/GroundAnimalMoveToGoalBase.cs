using System.Collections;
using AnythingWorld.Animation;
using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Extends MoveToGoalBase to include jumping behavior for ground animals, handling both animation
    /// and movement trajectory for jumps.
    /// </summary>
    [System.Serializable]
    public abstract class GroundAnimalMoveToGoalBase : MoveToGoalBase
    {
        public bool canJump = true;

        protected bool IsJumping;
        
        private const float JumpCurveHeight = 2;
        
        private MovementJumpLegacyController _legacyAnimationController;
        private Animator _animationController;
        private bool _hasJumpAnimation;
        private bool _isFallingLocked;
        private float _jumpEndAnimationDuration;
        private bool _isLanding;
        private float _jumpAnimationDuration;
        private bool _isAnimated;

        /// <summary>
        /// Initializes component, detects animation controller type, and calculates jump animations duration.
        /// </summary>
        public override void OnInit()
        {
            base.OnInit();
            
            if (!canRun)
            {
                return;
            }
            
            _legacyAnimationController = context.GameObject.GetComponentInChildren<MovementJumpLegacyController>();

            if (_legacyAnimationController)
            {
                _hasJumpAnimation = _legacyAnimationController.DoesJumpAnimationExist();
                _isAnimated = true;
            }
            else
            {
                _animationController = context.GameObject.GetComponentInChildren<Animator>();
                if (_animationController)
                {
                    _hasJumpAnimation = _animationController.DoesJumpAnimationExist();
                    _isAnimated = true;
                }
            }
            
            GetJumpAnimationDurations();
        }

        /// <summary>
        /// Updates the movement animation based on the current speed.
        /// </summary>
        protected void UpdateMovementAnimation(float currentSpeed)
        {
            if (!_isAnimated)
            {
                return;
            }

            if (_legacyAnimationController)
            {
                _legacyAnimationController.BlendMovementAnimationOnSpeed(_legacyAnimationController.DoesRunAnimationExist() ? currentSpeed : Mathf.Clamp(speed, 0f, 0.5f));
            }
            else
            {
                _animationController.BlendMovementAnimationOnSpeed(_animationController.DoesRunAnimationExist() ? currentSpeed : Mathf.Clamp(speed, 0f, 0.5f));
            }
        }
        
        /// <summary>
        /// Executes a parabolic jump towards the specified end position, managing both the physics and animation aspects.
        /// </summary>
        protected IEnumerator ParabolaJump(Vector3 endPosition)
        {   
            IsJumping = true;
            _isFallingLocked = _isLanding = false;
            
            Vector3 startPosition = context.Transform.position;
            
            var heightDifference = endPosition.y - startPosition.y;
            var finalHeight = Mathf.Max(1, Mathf.Abs(heightDifference));
            var jumpDuration = RemapHeightToJumpDuration(finalHeight);
            if (heightDifference < 0)
            {
                finalHeight = Mathf.Abs(heightDifference) * 0.6f;
                jumpDuration *= .9f;
            }
            
            var animationToJumpDurationRatio = _jumpAnimationDuration / jumpDuration;
            var isJumpFasterThanAnimation = animationToJumpDurationRatio > 1;
            if (isJumpFasterThanAnimation)
            {
                SetJumpAnimationSpeed(animationToJumpDurationRatio);
            }
            PlayJumpAnimation();
            
            RotateToJumpTarget(endPosition, startPosition);

            float normalizedTime = 0.0f;
            float elapsedTime = 0;
            while (normalizedTime < 1.0f)
            {
                float yOffset = finalHeight * JumpCurveHeight * (normalizedTime - normalizedTime * normalizedTime);
                context.Transform.position = Vector3.Lerp(startPosition, endPosition, normalizedTime) + yOffset * Vector3.up;
                normalizedTime += Time.deltaTime / jumpDuration;
                elapsedTime += Time.deltaTime; 
                ProcessJumpAnimations(elapsedTime, jumpDuration);
                
                yield return null;
            }
            
            SetJumpAnimationSpeed(1);
            IsJumping = false;
        }
        
        /// <summary>
        /// Sets the durations for jump animations based on the animation controller type.
        /// </summary>
        private void GetJumpAnimationDurations()
        {
            if (!_hasJumpAnimation)
            {
                return;
            }
            
            if (_legacyAnimationController)
            {
                _jumpAnimationDuration = _legacyAnimationController.GetJumpAnimationDuration();
                _jumpEndAnimationDuration = _legacyAnimationController.GetJumpEndAnimationDuration();
                return;
            }

            _jumpAnimationDuration = Container.jumpAnimationDuration;
            _jumpEndAnimationDuration = Container.jumpEndAnimationDuration;
        }
        
        /// <summary>
        /// Sets the speed of the jump animations to match the duration of the jump.
        /// </summary>
        private void SetJumpAnimationSpeed(float currentSpeed)
        {
            if (!_hasJumpAnimation)
            {
                return;
            }
            
            if (_legacyAnimationController)
            {
                _legacyAnimationController.SetJumpAnimationSpeed(currentSpeed);
                return;
            }
                
            _animationController.speed = currentSpeed;
        }

        /// <summary>
        /// Retrieves the current speed of the jump animation.
        /// </summary>
        private float GetJumpAnimationSpeed()
        { 
            if (_legacyAnimationController)
            {
                return _legacyAnimationController.GetJumpAnimationSpeed();
            }
                
            return _animationController.speed;
        }
        
        /// <summary>
        /// Calculates the jump duration based on the height of the jump, mapping height to a suitable duration.
        /// </summary>
        private float RemapHeightToJumpDuration(float value)
        {
            var heightLowerBound = 0.76f;
            var heightUpperBound = 2.96f;

            var durationLowerBound = 0.5f;
            var durationUpperBound = 0.8f;
            
            return (value - heightLowerBound) / (heightUpperBound - heightLowerBound) * 
                (durationUpperBound - durationLowerBound) + durationLowerBound;
        }
        
        /// <summary>
        /// Rotates the object to face the jump target.
        /// </summary>
        private void RotateToJumpTarget(Vector3 endPos, Vector3 startPos)
        {
            endPos.y = startPos.y;
            var dirToEnd = endPos - startPos;
            context.Transform.rotation = Quaternion.LookRotation(dirToEnd);
        }

        /// <summary>
        /// Processes jump animations based on elapsed time and jump duration.
        /// </summary>
        private void ProcessJumpAnimations(float elapsedTime, float jumpDuration)
        {
            if (!_hasJumpAnimation)
            {
                return;
            }
            
            if (!_legacyAnimationController && !(GetJumpAnimationSpeed() <= 1)) return;
            
            LockAnimationIfStartedFalling();
            PlayJumpEndAnimation(elapsedTime, jumpDuration);
        }
        
        /// <summary>
        /// Initiates the jump animation.
        /// </summary>
        private void PlayJumpAnimation()
        {
            if (!_hasJumpAnimation)
            {
                return;
            }
            
            if (_legacyAnimationController)
            {
                _legacyAnimationController.JumpStart();
            }
            else
            {
                _animationController.JumpStart();
            }
        }

        /// <summary>
        /// Plays the jump end animation, adjusting for the transition into landing.
        /// </summary>
        private void PlayJumpEndAnimation(float elapsedTime, float jumpDuration)
        {
            var jumpEndDurationScaled = _jumpEndAnimationDuration;
            if (_legacyAnimationController)
            {
                jumpEndDurationScaled /= GetJumpAnimationSpeed();
            }

            if (_isLanding || jumpDuration - elapsedTime > jumpEndDurationScaled)
            {
                return;
            }
            
            _isLanding = true;
            
            if (_legacyAnimationController)
            {
                _legacyAnimationController.JumpEnd();
            }
            else
            {
                _animationController.JumpEnd();
            }
        }
        
        /// <summary>
        /// Locks the falling animation state if the character has started falling, ensuring smooth animation transition.
        /// </summary>
        private void LockAnimationIfStartedFalling()
        {
            if (!_animationController)
            {
                return;
            }
            
            if (!_isFallingLocked && _animationController.IsPlayingJumpFallAnimation())
            {
                _isFallingLocked = true;
                _animationController.JumpFall();
            }
        }
    }
}
