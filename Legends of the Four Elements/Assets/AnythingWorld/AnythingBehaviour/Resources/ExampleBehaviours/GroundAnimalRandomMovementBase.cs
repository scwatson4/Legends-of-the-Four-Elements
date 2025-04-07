using System.Collections;
using AnythingWorld.Animation;
using UnityEngine;

namespace AnythingWorld.Behaviour
{
    /// <summary>
    /// Extends RandomMovementBase to include jumping behavior for ground animals, handling both animation
    /// and movement trajectory for jumps.
    /// </summary>
    public abstract class GroundAnimalRandomMovementBase : RandomMovementBase
    {
        [Header("Jump parameters")]
        public bool canJump = true;

        protected bool IsJumping;
        
        private const float JumpCurveHeight = 2;
                
        private MovementJumpLegacyController _legacyAnimationController;
        private Animator _animationController;
        private bool _hasJumpAnimation;
        private bool _isFallingLocked;
        private float _jumpEndDuration;
        private bool _isLanding;
        private float _jumpAnimationDuration;
        private bool _isAnimated;
        
        // Initializes component, detects animation controller type, and calculates jump animations duration.
        protected override void Start()
        {
            base.Start();
            
            _legacyAnimationController = GetComponentInChildren<MovementJumpLegacyController>();

            if (_legacyAnimationController)
            {
                _hasJumpAnimation = _legacyAnimationController.DoesJumpAnimationExist();
                _isAnimated = true;
            }
            else
            {
                _animationController = GetComponentInChildren<Animator>();
                if (_animationController)
                {
                    _hasJumpAnimation = _animationController.DoesJumpAnimationExist();
                    _isAnimated = true;
                }
            }
            
            GetJumpAnimationDurations();
        }
        
        // Updates the movement animation based on the current speed.
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
        
        // Executes a parabolic jump towards the specified end position, managing both the physics and animation aspects.
        protected IEnumerator ParabolaJump(Vector3 endPosition)
        {   
            IsJumping = true;
            _isFallingLocked = _isLanding = false;
            
            var startPosition = transform.position;
            
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
                transform.position = Vector3.Lerp(startPosition, endPosition, normalizedTime) + yOffset * Vector3.up;
                normalizedTime += Time.deltaTime / jumpDuration;
                elapsedTime += Time.deltaTime;
                ProcessJumpAnimations(elapsedTime, jumpDuration);
                
                yield return null;
            }

            SetJumpAnimationSpeed(1);
            IsJumping = false;
        }
        
        // Sets the durations for jump animations based on the animation controller type.
        private void GetJumpAnimationDurations()
        {
            if (!_hasJumpAnimation)
            {
                return;
            }
            
            if (_legacyAnimationController)
            {
                _jumpAnimationDuration = _legacyAnimationController.GetJumpAnimationDuration();
                _jumpEndDuration = _legacyAnimationController.GetJumpEndAnimationDuration();
                return;
            }

            _jumpAnimationDuration = Container.jumpAnimationDuration;
            _jumpEndDuration = Container.jumpEndAnimationDuration;
        }

        // Sets the speed of the jump animations to match the duration of the jump.
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

        // Retrieves the current speed of the jump animation.
        private float GetJumpAnimationSpeed()
        {
            if (_legacyAnimationController)
            {
                return _legacyAnimationController.GetJumpAnimationSpeed();
            }
                
            return _animationController.speed;
        }
        
        // Calculates the jump duration based on the height of the jump, mapping height to a suitable duration.
        private float RemapHeightToJumpDuration(float value)
        {
            var heightLowerBound = 0.76f;
            var heightUpperBound = 2.96f;

            var durationLowerBound = 0.5f;
            var durationUpperBound = 0.8f;
            
            return (value - heightLowerBound) / (heightUpperBound - heightLowerBound) * 
                (durationUpperBound - durationLowerBound) + durationLowerBound;
        }

        // Rotates the object to face the jump target.
        private void RotateToJumpTarget(Vector3 endPos, Vector3 startPos)
        {
            endPos.y = startPos.y;
            var dirToEnd = endPos - startPos;
            transform.rotation = Quaternion.LookRotation(dirToEnd);
        }

        // Processes jump animations based on elapsed time and jump duration.
        private void ProcessJumpAnimations(float elapsedTime, float jumpDuration)
        {
            if (!_hasJumpAnimation)
            {
                return;
            }

            if (!_legacyAnimationController && !(GetJumpAnimationSpeed() <= 1))
            {
                return;
            }
            
            LockAnimationIfStartedFalling();
            PlayJumpEndAnimation(elapsedTime, jumpDuration);
        }
        
        // Initiates the jump animation.
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

        // Plays the jump end animation, adjusting for the transition into landing.
        private void PlayJumpEndAnimation(float elapsedTime, float jumpDuration)
        {
            var jumpEndDurationScaled = _jumpEndDuration;
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
        
        // Locks the falling animation state if the character has started falling, ensuring smooth animation transition.
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