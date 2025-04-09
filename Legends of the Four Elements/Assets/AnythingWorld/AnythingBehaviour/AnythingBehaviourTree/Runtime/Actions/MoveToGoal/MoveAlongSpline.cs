#if UNITY_EDITOR
using AnythingWorld.AnythingBehaviour.Tree;
#endif
using AnythingWorld.PathCreation;
using AnythingWorld.PostProcessing;
using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Moves an agent along a set spline with a certain speed. Stops at the end if the path is not closed,
    /// moves in a loop otherwise.
    /// </summary>
    [System.Serializable]
    public class MoveAlongSpline : ActionNode
    {
        public bool scaleSpeedWithModelSpeed = true;
        public NodeProperty<float> speed = new NodeProperty<float>(4);
        public NodeProperty<bool> startAtClosestPathPoint = new NodeProperty<bool>(true);
#if UNITY_EDITOR
        [ReadOnlyField]
#endif
        public float speedScalar = 1;
        
        private float _distanceTravelled;
        private PathCreator _pathCreator;
        private Vector3 _extents;
        
        /// <summary>
        /// Initializes movement parameters and rigidbody settings on startup.
        /// </summary>
        public override void OnInit()
        {
            var go = context.GameObject;

            if (go.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = true;
            }
            
            if (go.TryGetComponent<MovementDataContainer>(out var container))
            {
                speedScalar = container.speedScalar;
                _pathCreator = container.pathCreator;
                _extents = container.extents;
                if (_extents == Vector3.zero)
                {
                    ModelDimensionsUtility.TryGetDimensions(context.Transform, out _extents, out _);
                }
            }
            else
            {
                Debug.LogWarning($"MovementDataContainer holding PathCreator wasn't added to {context.GameObject.name}.");
                canRun = false;
                return;
            }

            if (_pathCreator)
            {
                _pathCreator.PathUpdated += OnPathChanged;
            }
            else
            {
                Debug.LogWarning($"PathCreator wasn't added to {context.GameObject.name}'s MovementDataContainer.");
                canRun = false;
                return;
            }
            
            SetSpeed();
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

            _distanceTravelled = startAtClosestPathPoint ? 
                _pathCreator.Path.GetClosestDistanceAlongPath(context.Transform.position) : 0;
        }

        protected override void OnStop(){}

        /// <summary>
        /// Samples the path at a certain distance and updates the agent's position and rotation.
        /// </summary>
        protected override State OnUpdate()
        {
            if (!canRun)
            {
                return State.Failure;
            }
            
            _distanceTravelled += speed * Time.deltaTime;
            if (_pathCreator.editorData.BezierPath.IsClosed)
            {
                _distanceTravelled %= _pathCreator.Path.Length;
            }
            else if (_distanceTravelled > _pathCreator.Path.Length)
            {
                speed.Value = 0;
                return State.Success;
            }
            
            var normal = _pathCreator.Path.GetNormalAtDistance(_distanceTravelled);
            
            context.Transform.position = _pathCreator.Path.GetPointAtDistance(_distanceTravelled) + normal * _extents.y;
            context.Transform.rotation = _pathCreator.Path.GetRotationAtDistance(_distanceTravelled);

            return State.Running;
        }
        
        /// <summary>
        /// Sets the scaled speed based on model data and speed scaling option.
        /// </summary>
        private void SetSpeed()
        {
            if (scaleSpeedWithModelSpeed)
            {
                speed.Value *= speedScalar;
            }
        }
        
        /// <summary>
        /// If the path changes during the game, update the distance travelled so that the follower's position on
        /// the new path is as close as possible to its position on the old path.
        /// </summary>
        private void OnPathChanged() 
        {
            _distanceTravelled = _pathCreator.Path.GetClosestDistanceAlongPath(context.Transform.position);
        }
    }
}
