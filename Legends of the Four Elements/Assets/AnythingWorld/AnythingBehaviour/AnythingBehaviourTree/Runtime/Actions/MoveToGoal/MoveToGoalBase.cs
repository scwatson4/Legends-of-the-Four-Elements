#if UNITY_EDITOR
using AnythingWorld.AnythingBehaviour.Tree;
#endif
using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Provides a framework for implementing movement to goal behavior in various agents,
    /// utilizing NavMesh or Rigidbody for physics-based movement. Supports scaling movement parameters
    /// based on model specifics and implements basic obstacle avoidance logic.
    /// </summary>
    [System.Serializable]
    public abstract class MoveToGoalBase : ActionNode
    {
        protected const float BaseTurnAcceleration = 90;
        protected const float BaseAngularSpeed = 360;
        public const int MaxRaycastDistance = 100;
        private const float BaseStuckPositionDelta = .01f;
        private const float ObstacleDetectorSize = .5f;
        private const float PositionCheckInterval = .1f;
        
        [Header("Something")]
        public NodeProperty<Vector3> goalPosition = new NodeProperty<Vector3>(Vector3.zero);
        
        public bool scaleSpeedWithModelSpeed = true;
        public NodeProperty<float> speed = new NodeProperty<float>(4);
        public float acceleration = 8;
        public float turnAcceleration = BaseTurnAcceleration;
        public float angularSpeed = BaseAngularSpeed;
#if UNITY_EDITOR
        [ReadOnlyField]
#endif
        public float speedScalar;
#if UNITY_EDITOR
        [ReadOnlyField]
#endif
        public float scaledSpeed;
#if UNITY_EDITOR
        [ReadOnlyField]
#endif
        public float scaledAcceleration;
        
        public NodeProperty<float> stoppingDistance = new NodeProperty<float>(.5f);
        
        protected Vector3 Extents;
        protected Vector3 Center;
        protected Rigidbody Rb;
        protected Vector3 Velocity;
        protected float ObstacleDetectorSizeScaled;
        protected float ElapsedPositionCheckTime;
        protected MovementDataContainer Container;
        protected bool IsGoalReached;
        
        private float _scaledStuckPositionDelta;
        
        private Vector3 _previousPosition;
        private Quaternion _previousRotation;
        
        /// <summary>
        /// Initializes movement parameters and rigidbody settings on startup.
        /// </summary>
        public override void OnInit()
        {
            var go = context.GameObject;

            if (go.TryGetComponent<Rigidbody>(out var rigidBod))
            {
                Rb = rigidBod;
                Rb.freezeRotation = true;                
            }
            
            if (go.TryGetComponent(out Container))
            {
                Extents = Container.extents;
                speedScalar = Container.speedScalar;
                Center = Container.center;

                ObstacleDetectorSizeScaled = Mathf.Min(ObstacleDetectorSize, ObstacleDetectorSize * Extents.z);
            }
            else
            {
                Debug.LogWarning($"{context.GameObject.name} doesn't have MovementDataContainer component, " +
                                 "cannot get model dimensions and start movement.");
                canRun = false;
                return;
            }
            
            SetInitialPositionAndRotation();
            SetAccelerationAndSpeed();
            SetStuckPositionDelta();
        }

        /// <summary>
        /// Updates movement and goal spawn parameters before node's execution.
        /// </summary>
        protected override void OnStart()
        {
            if (!canRun)
            {
                return;
            }
            
            IsGoalReached = false;
            ElapsedPositionCheckTime = 0;
            SetAccelerationAndSpeed();
            SetStuckPositionDelta();
        }
        
        /// <summary>
        /// Ensures the agent is positioned on the ground if it is supposed to be grounded.
        /// </summary>
        protected void PlaceOnGround()
        {
            if (Physics.Raycast(context.Transform.position, Vector3.down, out var hitInfo, MaxRaycastDistance))
            {
                context.Transform.position = hitInfo.point + Vector3.up * Extents.y;
            }
        }

        // Updates internal state once the goal is reached, preparing for potential waiting.
        protected virtual void SetGoalReachedParameters()
        {
            IsGoalReached = true;
        }

        // Determines if the agent is near its current goal.
        protected virtual bool IsNearGoal() => Vector3.Distance(goalPosition, context.Transform.position) < stoppingDistance;
        
#if UNITY_EDITOR
        /// <summary>
        /// Draws debug gizmos in the Editor to visualize goal positions and spawn areas.
        /// </summary>
        public override void OnDrawGizmosSelectedTree()
        {
            GUI.color = Color.white;
            Gizmos.color = Color.white;
            
            UnityEditor.Handles.Label(goalPosition + Vector3.left * stoppingDistance, new GUIContent("Goal"));
            UnityEditor.Handles.DrawWireDisc(goalPosition, Vector3.up, stoppingDistance);
        }
#endif
        
        /// <summary>
        /// Stops the agent if it gets stuck, based on minimal movement and rotation over time.
        /// </summary>
        protected void StopIfStuck()
        {
            ElapsedPositionCheckTime += Time.deltaTime;
            if (ElapsedPositionCheckTime > PositionCheckInterval)
            {
                ElapsedPositionCheckTime = 0;
                var rotationDelta = Quaternion.Angle(_previousRotation, context.Transform.rotation);
                var positionDelta = Vector3.Distance(_previousPosition, context.Transform.position);
                if (positionDelta <= _scaledStuckPositionDelta && rotationDelta <= 2)
                {
                    goalPosition.Value = context.Transform.position;
                }

                _previousPosition = context.Transform.position;
                _previousRotation = context.Transform.rotation;
            }
        }

        /// <summary>
        /// Sets initial position and rotation for movement tracking.
        /// </summary>
        private void SetInitialPositionAndRotation()
        {
            _previousPosition = context.Transform.position;
            _previousRotation = context.Transform.rotation;
        }

        /// <summary>
        /// Sets the scaled speed and acceleration based on model data and speed scaling option.
        /// </summary>
        private void SetAccelerationAndSpeed()
        {
            scaledSpeed = speed;
            scaledAcceleration = acceleration;
            
            if (scaleSpeedWithModelSpeed)
            {
                scaledSpeed *= speedScalar;
                scaledAcceleration *= speedScalar;
            }
        }

        /// <summary>
        /// Calculates a delta for detecting stuck positions, scaled by movement speed.
        /// </summary>
        private void SetStuckPositionDelta() => _scaledStuckPositionDelta = BaseStuckPositionDelta * scaledSpeed;
    }
}
