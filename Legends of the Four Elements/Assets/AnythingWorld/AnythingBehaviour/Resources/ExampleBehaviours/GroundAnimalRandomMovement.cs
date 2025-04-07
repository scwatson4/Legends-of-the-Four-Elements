#if UNITY_EDITOR
using AnythingWorld.AnythingBehaviour.Tree;
#endif
using UnityEngine;
using Random = UnityEngine.Random;

namespace AnythingWorld.Behaviour
{
    /// <summary>
    /// Implements random movement for ground animals with additional features like jump detection and goal generation,
    /// leveraging Rigidbody for physics-based interactions.
    /// </summary>
    public class GroundAnimalRandomMovement : GroundAnimalRandomMovementBase
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
        
        // Initializes component, sets up jump parameters and grounds animal.
        protected override void Start()
        {
            base.Start();
            
            _stepHeight = Extents.y * StepHeightMultiplier;
            
            SetJumpParameters();
            PlaceOnGround();
        }
        
        // Handles physics-based movement and jumping logic each FixedUpdate cycle.
        private void FixedUpdate()
        {
            if (IsResting)
            {
                return;
            }

            if (IsGoalReached)
            {
                if (!TryGenerateNewGoal(out var newGoalPosition))
                {
                    return;
                }
                
                SetNewGoalParameters(newGoalPosition);
            }
            
            if (IsNearGoal())
            {
                SetGoalReachedParameters();
                return;
            }
            
            if (!IsJumping)
            {
                JumpIfNecessary();
            }
            
            if (IsJumping || IsGoalReached)
            {
                ElapsedPositionCheckTime = 0;
                return;
            }
            
            MoveTowardsGoal();
            UpdateMovementAnimation(Velocity.magnitude);
        }
        
        // Adjusts jump parameters on property changes in the editor.
        protected override void OnValidate()
        {
            base.OnValidate();
            SetJumpParameters();
        }

        // Sets new goal parameters and makes Rigidbody non kinematic to enable physics based movement.
        protected override void SetNewGoalParameters(Vector3 newGoalPosition)
        {
            base.SetNewGoalParameters(newGoalPosition);
            Rb.isKinematic = false;
        }
        
        // Finalizes goal achievement by updating movement animation and making Rigidbody kinematic to handle stopping
        // on slopes.
        protected override void SetGoalReachedParameters()
        {
            base.SetGoalReachedParameters();
            UpdateMovementAnimation(0);
            Rb.isKinematic = true;
        }

        // Sets jump height and detection size based on model properties.
        private void SetJumpParameters()
        {
            jumpDetectorSizeScaled = scaleJumpWithModelHeight ? jumpDetectorSize * Extents.y : jumpDetectorSize;
            jumpHeightScaled = scaleJumpWithModelHeight ? maxJumpHeight * Extents.y : maxJumpHeight;
        }
        
        // Manages movement towards the target, adjusting for slopes.
        private void MoveTowardsGoal()
        {
            Rb.useGravity = true;
            Rb.isKinematic = false;
            bool isOnSlope = false;
            
            if (Physics.Raycast(transform.position, Vector3.down, out var hit, Extents.y + 0.1f))
            {
                // Check if the surface is a slope
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);

                isOnSlope = Mathf.Abs(slopeAngle) > 0.1f;

                if (isOnSlope)
                {
                    var forwardToGoal = GoalPosition - transform.position;
                    forwardToGoal.y = transform.position.y;
                    forwardToGoal.Normalize();
                
                    float dir = Vector3.Dot(forwardToGoal, hit.normal);

                    var isDownwardSlope = dir > 0;
                
                    if (!isDownwardSlope)
                    {
                        if (slopeAngle > 0 && slopeAngle < maxSlope)
                        {
                            Rb.useGravity = false;
                        }
                        else if (slopeAngle > maxSlope)
                        {
                            GoalPosition = transform.position;
                            return;
                        }
                    }
                }
            }
            
            var goalLeveled = GoalPosition;
            goalLeveled.y = transform.position.y;
            
            var rotationDirection = (goalLeveled - transform.position).normalized;
            var movementDirection = isOnSlope ? (GoalPosition - transform.position).normalized : rotationDirection;
            
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
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 
                    angularSpeed * Time.fixedDeltaTime);
            }

            // Apply velocity
            Rb.MovePosition(transform.position + Velocity * Time.fixedDeltaTime);
        }
        
        // Evaluates and executes jump actions when necessary based on obstacles and terrain.
        private void JumpIfNecessary()
        {
            var goalPositionLeveled = GoalPosition;
            goalPositionLeveled.y = transform.position.y;
            
            var toGoalVector = goalPositionLeveled - transform.position;
            if (Vector3.Dot(toGoalVector, transform.forward) < 0.2f)
            {
                return;
            }
            
            if (!TryFindObstacleAhead(out var obstacleHitPoint))
            {
                return;
            }
            
            var distToGoal = Vector3.Distance(GoalPosition, transform.position);
            var distToObstacle = Vector3.Distance(transform.position, new Vector3(obstacleHitPoint.x,
                transform.position.y, obstacleHitPoint.z));

            if (distToGoal < distToObstacle)
            {
                return;
            }

            if (!TryFindSurfacePoint(MaxRaycastDistance / 2, obstacleHitPoint, out var obstacleSurfacePoint))
            {
                GoalPosition = transform.position;
                return;
            }
            
            var obstacleHeight = GetObstacleHeight(obstacleSurfacePoint.y);
            
            if (obstacleHeight <= _stepHeight)
            {
                return;
            }

            if (!canJump || obstacleHeight > jumpHeightScaled || IsObstructedByCeiling(obstacleHeight))
            {
                GoalPosition = transform.position;
                return;
            }

            Rb.isKinematic = true;

            Velocity = Vector3.zero;

            var landingPoint = obstacleSurfacePoint + Vector3.up * Extents.y;

            var isObstacleHeightEqualGoal = Mathf.Approximately(GoalPosition.y, landingPoint.y);
            var distanceToObstacleSurface = Vector3.Distance(transform.position, landingPoint);

            var distanceToGoal = Vector3.Distance(transform.position, GoalPosition); 
            if (isObstacleHeightEqualGoal && distanceToGoal < distanceToObstacleSurface)
            {
                landingPoint = GoalPosition;
            }

            StartCoroutine(ParabolaJump(landingPoint));
        }

        // Checks if jump is obstructed by ceiling above the animal.
        private bool IsObstructedByCeiling(float obstacleHeight) =>
            Physics.Raycast(transform.position, Vector3.up, obstacleHeight + Extents.y);

        // Calculates the obstacle surface height relative to the animal.
        private float GetObstacleHeight(float obstacleY) => obstacleY - transform.position.y + Extents.y;
        
        // Attempts to detect obstacles in the path towards the goal.
        private bool TryFindObstacleAhead(out Vector3 obstacleHitPoint)
        {
            obstacleHitPoint = Vector3.zero;
           
            var floorPosition = transform.TransformPoint(Center);
            floorPosition.y -= Extents.y - _stepHeight;
            
            var goal = GoalPosition;
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
        
        // Identifies a viable landing point on the surface of an obstacle for jumping.
        private bool TryFindSurfacePoint(float upDistance, Vector3 obstacleHitPoint, out Vector3 surfacePoint)
        {
            var trPos = transform.position;
            trPos.y = obstacleHitPoint.y;
            var dirToObstacle = (obstacleHitPoint - trPos).normalized;
            
            // move the origin inside the obstacle a little to make sure it's above obstacle surface.
            var origin = obstacleHitPoint + dirToObstacle * PositionDelta + Vector3.up * upDistance;
            
            if (Physics.Raycast(origin, Vector3.down, out var hit, MaxRaycastDistance))
            {
                surfacePoint = hit.point;
                return true;
            }

            surfacePoint = Vector3.zero;
            return false;
        }

        // Generates a new navigational goal within a specified radius. 
        private bool TryGenerateNewGoal(out Vector3 newGoal)
        {
            var radius = Random.Range(minGoalSpawnRadius, maxGoalSpawnRadius);
            float angle = Random.Range(0, 2 * Mathf.PI);
            float x = radius * Mathf.Cos(angle);
            float z = radius * Mathf.Sin(angle);

            newGoal = new Vector3(x, 0, z) + transform.position;

            if (!Physics.Raycast(newGoal + Vector3.up * MaxRaycastDistance / 2, Vector3.down, out var hit,
                    MaxRaycastDistance))
            {
                return false;
            }
            
            newGoal = hit.point + Vector3.up * Extents.y;
            return true;
        }
    }
}
