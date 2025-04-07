using System.Collections;
#if UNITY_EDITOR
using AnythingWorld.AnythingBehaviour.Tree;
#endif
using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Implements random movement for ground animals with additional features like jump detection
    /// and obstacle avoidance, leveraging Rigidbody for physics-based interactions.
    /// </summary>
    [System.Serializable]
    public class GroundAnimalMoveToGoal : GroundAnimalMoveToGoalBase 
    {
        public bool scaleJumpWithModelHeight = true;
        public float maxJumpHeight = 5f;
#if UNITY_EDITOR
        [ReadOnlyField]
#endif
        public float jumpHeightScaled;
        public float jumpDetectorSize = 3;
#if UNITY_EDITOR
        [ReadOnlyField]
#endif 
        public float jumpDetectorSizeScaled;
        
        public float maxSlope = 45;
        
        private const float PositionDelta = .002f;
        private const float StepHeightMultiplier = 0.7f;

        private float _stepHeight;

        /// <summary>
        /// Initializes component, sets up jump parameters and grounds animal.
        /// </summary>
        public override void OnInit()
        {
            base.OnInit();
            if (!canRun)
            {
                return;
            }

            if (!context.Rb)
            {
                Debug.LogWarning($"{context.GameObject.name} is missing RigidBody component required to move agent.");
                canRun = false;
                return;
            }
            
            _stepHeight = Extents.y * StepHeightMultiplier;
            
            PlaceOnGround();
            
            jumpDetectorSizeScaled = scaleJumpWithModelHeight ? jumpDetectorSize * Extents.y : jumpDetectorSize;
            jumpHeightScaled = scaleJumpWithModelHeight ? maxJumpHeight * Extents.y : maxJumpHeight;
        }
        
        /// <summary>
        /// Updates jump parameters, makes Rigidbody non kinematic to enable physics based movement
        /// and starts FixedUpdate loop.
        /// </summary>
        protected override void OnStart()
        {
            base.OnStart();
            
            if (!canRun)
            {
                return;
            }
            
            SetJumpParameters();
            context.Rb.isKinematic = false;
            Container.StartCoroutine(FixedUpdateCoroutine());
        }

        /// <summary>
        /// Stops FixedUpdate loop at the end of the Node's lifecycle.
        /// </summary>
        protected override void OnStop()
        {
            Container.StopCoroutine(FixedUpdateCoroutine());
        }

        /// <summary>
        /// Updates the state of the Node, determining its success or failure
        /// </summary>
        protected override State OnUpdate() 
        {
            if (!canRun)
            {
                return State.Failure;
            }
            
            if (IsGoalReached)
            {
                return State.Success;
            }
            
            StopIfStuck();

            return State.Running;
        }
        
        /// <summary>
        /// Handles physics-based movement and jumping logic each FixedUpdate cycle.
        /// </summary>
        private IEnumerator FixedUpdateCoroutine()
        {
            var waitForFixedUpdate = new WaitForFixedUpdate();
            
            while (!IsGoalReached)
            {
                if (IsNearGoal())
                {
                    SetGoalReachedParameters();
                    break;
                }
         
                if (!IsJumping)
                {
                    JumpIfNecessary();
                }
                
                if (!IsJumping)
                {
                    MoveTowardsGoal();
                    UpdateMovementAnimation(Velocity.magnitude);
                }
                else
                {
                    ElapsedPositionCheckTime = 0;
                }
                
                yield return waitForFixedUpdate;
            }
        } 
        
        /// <summary>
        /// Finalizes goal achievement by updating movement animation and making Rigidbody kinematic to handle stopping
        /// on slopes.
        /// </summary>
        protected override void SetGoalReachedParameters() 
        {
            base.SetGoalReachedParameters();
            UpdateMovementAnimation(0);
            context.Rb.isKinematic = true;
        }
        
        /// <summary>
        /// Sets jump height and detection size based on model properties.
        /// </summary>
        private void SetJumpParameters()
        {
            jumpDetectorSizeScaled = scaleJumpWithModelHeight ? jumpDetectorSize * Extents.y : jumpDetectorSize;
            jumpHeightScaled = scaleJumpWithModelHeight ? maxJumpHeight * Extents.y : maxJumpHeight;
        }
        
        /// <summary>
        /// Evaluates and executes jump actions when necessary based on obstacles and terrain.
        /// </summary>
        private void MoveTowardsGoal()
        {
            context.Rb.useGravity = true;
            context.Rb.isKinematic = false;
            bool isOnSlope = false;
            
            if (Physics.Raycast(context.Transform.position, Vector3.down, out var hit, Extents.y + 0.1f))
            {
                // Check if the surface is a slope
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                isOnSlope = Mathf.Abs(slopeAngle) > 0.1f;

                if (isOnSlope)
                {
                    var forwardToGoal = goalPosition - context.Transform.position;
                    forwardToGoal.y = context.Transform.position.y;
                    forwardToGoal.Normalize();
                
                    float dir = Vector3.Dot(forwardToGoal, hit.normal);

                    var isDownwardSlope = dir > 0;
                
                    if (!isDownwardSlope)
                    {
                        if (slopeAngle > 0 && slopeAngle < maxSlope)
                        {
                            context.Rb.useGravity = false;
                        }
                        else if (slopeAngle > maxSlope)
                        {
                            goalPosition.Value = context.Transform.position;
                            return;
                        }
                    }
                }
            }

            var goalLeveled = goalPosition.Value;
            goalLeveled.y = context.Transform.position.y;
            
            var rotationDirection = (goalLeveled - context.Transform.position).normalized;
            var movementDirection = isOnSlope ? 
                (goalPosition - context.Transform.position).normalized : rotationDirection;
            
            Vector3 targetVelocity = movementDirection * scaledSpeed;
           
            // Determine the angle between current velocity and target direction
            float turnAngle = Vector3.Angle(Velocity, targetVelocity);
            // Scale acceleration based on turn angle
            float finalAcceleration = Mathf.Lerp(scaledAcceleration, turnAcceleration, turnAngle / 180f);
            // Apply acceleration
            Velocity = Vector3.MoveTowards(Velocity, targetVelocity, finalAcceleration * Time.fixedDeltaTime);

            // Calculate rotation towards target
            if (rotationDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(rotationDirection);
                context.Transform.rotation = Quaternion.RotateTowards(context.Transform.rotation, targetRotation, 
                    angularSpeed * Time.fixedDeltaTime);
            }

            // Apply velocity
            context.Rb.MovePosition(context.Transform.position + Velocity * Time.fixedDeltaTime);
        }
        
        /// <summary>
        /// Evaluates and executes jump actions when necessary based on obstacles and terrain.
        /// </summary>
        private void JumpIfNecessary()
        {
            var goalPositionLeveled = goalPosition.Value;
            goalPositionLeveled.y = context.Transform.position.y;
            
            var toGoalVector = goalPositionLeveled - context.Transform.position;
            
            if (Vector3.Dot(toGoalVector, context.Transform.forward) < 0.2f)
            {
                return;
            }
            
            if (!TryFindObstacleAhead(out var obstacleHitPoint))
            {
                return;
            }
            
            var distToGoal = Vector3.Distance(goalPosition, context.Transform.position);
            var distToObstacle = Vector3.Distance(context.Transform.position, new Vector3(obstacleHitPoint.x,
                context.Transform.position.y, obstacleHitPoint.z));

            if (distToGoal < distToObstacle)
            {
                return;
            }

            if (!TryFindSurfacePoint(MaxRaycastDistance / 2, obstacleHitPoint, out var obstacleSurfacePoint))
            {
                goalPosition.Value = context.Transform.position;
                return;
            }
            
            var obstacleHeight = GetObstacleHeight(obstacleSurfacePoint.y);
            
            if (obstacleHeight <= _stepHeight)
            {
                return;
            }

            if (!canJump || obstacleHeight > jumpHeightScaled || IsObstructedByCeiling(obstacleHeight))
            {
                goalPosition.Value = context.Transform.position;
                return;
            }

            context.Rb.isKinematic = true;

            Velocity = Vector3.zero;

            var landingPoint = obstacleSurfacePoint + Vector3.up * Extents.y;

            var isObstacleHeightEqualGoal = Mathf.Approximately(goalPosition.Value.y, landingPoint.y);
            var distanceToObstacleSurface = Vector3.Distance(context.Transform.position, landingPoint);

            var distanceToGoal = Vector3.Distance(context.Transform.position, goalPosition); 
            if (isObstacleHeightEqualGoal && distanceToGoal < distanceToObstacleSurface)
            {
                landingPoint = goalPosition;
            }

            Container.StartCoroutine(ParabolaJump(landingPoint));
        }
        
        /// <summary>
        /// Checks if jump is obstructed by ceiling above the animal.
        /// </summary>
        private bool IsObstructedByCeiling(float obstacleHeight) =>
            Physics.Raycast(context.Transform.position, Vector3.up, obstacleHeight + Extents.y);
        
        /// <summary>
        /// Calculates the obstacle surface height relative to the animal.
        /// </summary>
        private float GetObstacleHeight(float obstacleY) => obstacleY - context.Transform.position.y + Extents.y;

        /// <summary>
        /// Attempts to detect obstacles in the path towards the goal.
        /// </summary>
        private bool TryFindObstacleAhead(out Vector3 obstacleHitPoint)
        {
            obstacleHitPoint = Vector3.zero;

            var floorPosition = context.Transform.TransformPoint(Center);
            floorPosition.y -= Extents.y - _stepHeight;

            var goal = goalPosition.Value;
            goal.y = floorPosition.y;
            
            var dirToGoal = goal - floorPosition;
            dirToGoal.Normalize();
            
            if (Physics.Raycast(floorPosition, dirToGoal, out var hit, jumpDetectorSizeScaled + Extents.z))
            {
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                if (slopeAngle < maxSlope)
                {
                    return false;
                }
    
                obstacleHitPoint = hit.point;
                return true;
            }
   
            return false;
        }
        
        /// <summary>
        /// Identifies a viable landing point on the surface of an obstacle for jumping.
        /// </summary>
        private bool TryFindSurfacePoint(float upDistance, Vector3 obstacleHitPoint, out Vector3 surfacePoint)
        {
            var trPos = context.Transform.position;
            trPos.y = obstacleHitPoint.y;
            var dirToObstacle = (obstacleHitPoint - trPos).normalized;
            
            // Move the origin inside the obstacle a little to make sure it's above obstacle surface.
            var origin = obstacleHitPoint + dirToObstacle * PositionDelta + Vector3.up * upDistance;
            
            if (Physics.Raycast(origin, Vector3.down, out var hit, MaxRaycastDistance))
            {
                surfacePoint = hit.point;
                return true;
            }

            surfacePoint = Vector3.zero;
            return false;
        }
    }
}
