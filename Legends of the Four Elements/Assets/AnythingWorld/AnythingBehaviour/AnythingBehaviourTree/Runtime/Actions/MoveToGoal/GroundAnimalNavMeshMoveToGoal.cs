using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Enhances GroundAnimalMoveToGoalBase with navigation capabilities using Unity's NavMesh system,
    /// supporting dynamic obstacle avoidance, pathfinding, and specialized jumping behavior.
    /// </summary>
    [System.Serializable]
    public class GroundAnimalNavMeshMoveToGoal : GroundAnimalMoveToGoalBase
    {
        private const float RotationMultiplier = 200;
        
        private bool _isDestinationSet;
        private NavMeshAgent _agent;
        private bool _isInitialized;

        /// <summary>
        /// Overrides OnInit method to set up NavMeshAgent specific properties.
        /// </summary>
        public override void OnInit()
        {
            base.OnInit();
            if (!canRun)
            {
                return;
            }

            canRun = false;
            
            _agent = context.GameObject.GetComponent<NavMeshAgent>();
            
            if (!_agent)
            {
                Debug.LogWarning($"{context.GameObject.name} is missing NavMeshAgent component. " +
                                 "Cannot generate NavMesh path.");
                return;
            }
                        
            if (!_agent.enabled)
            {
                Debug.LogWarning($"{context.GameObject.name} NavMeshAgent is not enabled. Cannot generate NavMesh path");
                return;
            }
            
            canRun = true;
            
            if (context.Rb)
            {
                context.Rb.isKinematic = true;
            }
            
            // If the agent is not on the NavMesh, place it on the ground and reset state by disabling and enabling.
            if (!_agent.isOnNavMesh)
            {
                _agent.enabled = false;
                _agent.enabled = true;
                
                if (!_agent.isOnNavMesh)
                {
                    PlaceOnGround();
                    _agent.enabled = false;
                    _agent.enabled = true;
                }
               
                Debug.LogWarning($"If you are importing model at runtime {context.GameObject.name}'s " +
                                 $"NavMesh agent creation error can be ignored.");
            }
        }

        /// <summary>
        /// Defines the behavior at the start of the Node's lifecycle.
        /// </summary>
        protected override void OnStart()
        {
            base.OnStart();

            if (!canRun)
            {
                return;
            }
            
            _isDestinationSet = false;
            SetNavMeshAgentMovementParameters();
        }

        protected override void OnStop(){}

        /// <summary>
        /// Regularly updates movement towards the goal and handles jumping using OffMeshLinks.
        /// </summary>
        protected override State OnUpdate() 
        {
            if (!canRun)
            {
                return State.Failure;
            }
            
            if (!_isDestinationSet)
            {
                // Wait till agent is registered as being on NavMesh.
                if (!_agent.isOnNavMesh)
                {
                    return State.Running;
                }
                _agent.destination = goalPosition;
                _isDestinationSet = true;
            }
            
            StopIfStuck();
            
            if (IsNearGoal())
            {
                SetGoalReachedParameters();
                return State.Success;
            }
            
            if (canJump && !IsJumping && _agent.isOnOffMeshLink)
            {
                Container.StartCoroutine(NavMeshParabolaJump());
            }

            if (IsJumping)
            {
                ElapsedPositionCheckTime = 0;
                return State.Running;
            }
  
            RotateToGoal();
            UpdateMovementAnimation(_agent.velocity.magnitude);
            return State.Running;
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// Displays the NavMeshAgent's current path to goal in addition to common visualization.
        /// </summary>
        public override void OnDrawGizmosSelectedTree() 
        {
            base.OnDrawGizmosSelectedTree();

            if (!_agent)
            {
                return;
            }
            
            Gizmos.color = Color.black;
            var agentPath = _agent.path;
            Vector3 prevCorner = context.Transform.position;
            foreach (var corner in agentPath.corners)
            {
                Gizmos.DrawLine(prevCorner, corner);
                Gizmos.DrawSphere(corner, 0.1f);
                prevCorner = corner;
            }
        }
#endif   
        /// <summary>
        /// Marks the goal as reached and stops the agent's moving animation.
        /// </summary>
        protected override void SetGoalReachedParameters()
        {
            base.SetGoalReachedParameters();
            UpdateMovementAnimation(0);
        }
        
        /// <summary>
        /// Determines if the NavMeshAgent is close enough to its destination to consider the goal reached.
        /// </summary>
        protected override bool IsNearGoal()
        {
            return !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance &&
                   (!_agent.hasPath || _agent.velocity.sqrMagnitude == 0f);
        }
        
        /// <summary>
        /// Configures the NavMeshAgent's movement settings based on the class's properties.
        /// </summary>
        private void SetNavMeshAgentMovementParameters()
        {
            _agent.speed = scaledSpeed;
            _agent.acceleration = scaledAcceleration;
            _agent.angularSpeed = angularSpeed;
            _agent.stoppingDistance = stoppingDistance;
            _agent.autoTraverseOffMeshLink = !canJump;
        }
        
        /// <summary>
        /// Handles the rotation of the agent based on its velocity.
        /// </summary>
        private void RotateToGoal()
        {
            if (_agent.velocity == Vector3.zero)
            {
                return;
            }
            
            Quaternion targetRotation = Quaternion.LookRotation(_agent.velocity);
            targetRotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
                    
            // Smoothly rotate towards the target rotation
            _agent.transform.rotation = Quaternion.RotateTowards(_agent.transform.rotation, targetRotation, 
                turnAcceleration * RotationMultiplier * Time.deltaTime);
        }
        
        /// <summary>
        /// Manages the jump action when the NavMeshAgent encounters an OffMeshLink.
        /// </summary>
        private IEnumerator NavMeshParabolaJump()
        {
            _agent.velocity = Vector3.zero;
            var endPos = _agent.currentOffMeshLinkData.endPos;
            yield return Container.StartCoroutine(ParabolaJump(endPos));
            _agent.CompleteOffMeshLink();
        }
    }
}
