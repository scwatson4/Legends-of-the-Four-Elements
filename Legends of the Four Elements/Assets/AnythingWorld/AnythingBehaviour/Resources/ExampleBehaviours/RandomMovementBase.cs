using UnityEngine;
#if UNITY_EDITOR
using AnythingWorld.AnythingBehaviour.Tree;
#endif
using AnythingWorld.Behaviour.Tree;

namespace AnythingWorld.Behaviour
{
    /// <summary>
    /// Provides a framework for implementing random movement behavior in various agents,
    /// utilizing NavMesh or Rigidbody for physics-based movement. Supports scaling movement parameters
    /// based on model specifics and implements basic goal-setting and obstacle avoidance logic.
    /// </summary>
    public abstract class RandomMovementBase : MonoBehaviour
    {
        protected const float BaseTurnAcceleration = 90;
        protected const float BaseAngularSpeed = 360;
        protected const float BaseMinGoalSpawnRadius = 5;
        protected const float BaseMaxGoalSpawnRadius = 20;
        protected const int MaxRaycastDistance = 100;
        
        private const float BaseStuckPositionDelta = .01f;
        private const float ObstacleDetectorSize = .5f;
        private const float PositionCheckInterval = .1f;
        
        [Header("Movement parameters")]
        public bool scaleSpeedWithModelSpeed = true;
        public float speed = 4;
        public float acceleration = 8;
        public float turnAcceleration = BaseTurnAcceleration;
        public float angularSpeed = BaseAngularSpeed;
#if UNITY_EDITOR
        [ReadOnlyField]
#endif
        public float scaledSpeed;
#if UNITY_EDITOR
        [ReadOnlyField]
#endif
        public float scaledAcceleration;
        public float restDuration;
        
        [Header("Goal parameters")]
        public float stoppingDistance = 0.5f;
        public float minGoalSpawnRadius = BaseMinGoalSpawnRadius;
        public float maxGoalSpawnRadius = BaseMaxGoalSpawnRadius;

        public bool showGizmos = true;
        
        protected Vector3 GoalPosition;
        protected Vector3 Extents;
        protected Vector3 Center;
        protected Rigidbody Rb;
        protected bool IsGoalReached = true;
        protected bool IsResting;
        protected Vector3 Velocity;
        protected float ObstacleDetectorSizeScaled;
        protected float ElapsedPositionCheckTime;
        protected MovementDataContainer Container;
        
        private float _scaledStuckPositionDelta;
        private float _speedScalar;
        private Vector3 _previousPosition;
        private Quaternion _previousRotation;
        private float _elapsedRestTime;
        private Vector3 _movementStartPosition;

        // Initializes movement parameters and rigidbody settings on startup.
        protected virtual void Start()
        {
            _elapsedRestTime = restDuration;
            
            if (TryGetComponent<Rigidbody>(out var rigidBod))
            {
                Rb = rigidBod;
                Rb.freezeRotation = true;              
            }
            
            if (TryGetComponent(out Container))
            {
                Extents = Container.extents;
                _speedScalar = Container.speedScalar;
                Center = Container.center;

                ObstacleDetectorSizeScaled = Mathf.Min(ObstacleDetectorSize, ObstacleDetectorSize * Extents.z);
            }

            SetInitialPositionAndRotation();
            SetAccelerationAndSpeed();
            SetStuckPositionDelta();
        }

        // Checks if the agent is resting or needs to stop due to being stuck, on every frame.
        protected virtual void Update()
        {
            CheckIfResting();
            StopIfStuck();
        }

        // Adjusts movement and goal spawn parameters when changed in the Inspector.
        protected virtual void OnValidate()
        {
            SetAccelerationAndSpeed();
            SetStuckPositionDelta();
            maxGoalSpawnRadius = Mathf.Max(minGoalSpawnRadius, maxGoalSpawnRadius);
        }

        // Ensures the agent is positioned on the ground if it is supposed to be grounded.
        protected void PlaceOnGround()
        {
            if (Physics.Raycast(transform.position, Vector3.down, out var hitInfo, MaxRaycastDistance))
            {
                transform.position = hitInfo.point + Vector3.up * Extents.y;
            }
        }
        
        // Updates internal state to pursue a new goal position.
        protected virtual void SetNewGoalParameters(Vector3 newGoalPosition)
        {
            ElapsedPositionCheckTime = 0;
            IsGoalReached = false;
            GoalPosition = newGoalPosition;
            _movementStartPosition = transform.position;
        }

        // Updates internal state once the goal is reached, preparing for potential rest.
        protected virtual void SetGoalReachedParameters()
        {
            if (HasMovedToGoal())
            {
                _elapsedRestTime = 0;
            }
            IsGoalReached = true;
        }

        // Determines if the agent is near its current goal.
        protected virtual bool IsNearGoal() => Vector3.Distance(GoalPosition, transform.position) < stoppingDistance;

#if UNITY_EDITOR
        // Draws debug gizmos in the Editor to visualize goal positions and spawn areas.
        protected virtual void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;
            
            GUI.color = Color.white;
            Gizmos.color = Color.white;
            
            UnityEditor.Handles.Label(GoalPosition + Vector3.left * stoppingDistance, new GUIContent("Goal"));
            UnityEditor.Handles.DrawWireDisc(GoalPosition, Vector3.up, stoppingDistance);

            if (Application.isPlaying)
            {
                return;
            }

            var trPosition = transform.position;
            UnityEditor.Handles.DrawWireDisc(trPosition, Vector3.up, minGoalSpawnRadius);
            UnityEditor.Handles.DrawWireDisc(trPosition, Vector3.up, maxGoalSpawnRadius);
            UnityEditor.Handles.Label(trPosition + (Vector3.left * minGoalSpawnRadius + Vector3.left), "Goal Spawn Area");
        }
#endif
        
        // Monitors if the agent should be resting based on goal achievement and elapsed time.
        private void CheckIfResting()
        {
            if (!IsGoalReached)
            {
                return;
            }
            
            IsResting = _elapsedRestTime < restDuration;
            if (IsResting)
            {
                _elapsedRestTime += Time.deltaTime;
            }
        }

        // Stops the agent if it gets stuck, based on minimal movement and rotation over time.
        private void StopIfStuck()
        {
            if (IsGoalReached)
            {
                return;
            }

            ElapsedPositionCheckTime += Time.deltaTime;
            if (ElapsedPositionCheckTime > PositionCheckInterval)
            {
                ElapsedPositionCheckTime = 0;
                var rotationDelta = Quaternion.Angle(_previousRotation, transform.rotation);
                var positionDelta = Vector3.Distance(_previousPosition, transform.position);
                if (positionDelta <= _scaledStuckPositionDelta && rotationDelta <= 2)
                {
                    GoalPosition = transform.position;
                }

                _previousPosition = transform.position;
                _previousRotation = transform.rotation;
            }
        }

        // Sets initial position and rotation for movement tracking.
        private void SetInitialPositionAndRotation()
        {
            _movementStartPosition = _previousPosition = transform.position;
            _previousRotation = transform.rotation;
        }

        // Determines if movement was aborted by obstacle before the agent has moved significantly towards the goal and
        // needs to regenerate it without resting.
        private bool HasMovedToGoal() => Vector3.Distance(_movementStartPosition, transform.position) > minGoalSpawnRadius / 2;

        // Sets the scaled speed and acceleration based on model data and speed scaling option.
        private void SetAccelerationAndSpeed()
        {
            scaledSpeed = speed;
            scaledAcceleration = acceleration;
            
            if (scaleSpeedWithModelSpeed)
            {
                scaledSpeed *= _speedScalar;
                scaledAcceleration *= _speedScalar;
            }
        }

        // Calculates a delta for detecting stuck positions, scaled by movement speed.
        private void SetStuckPositionDelta() => _scaledStuckPositionDelta = BaseStuckPositionDelta * scaledSpeed;
    }
}
