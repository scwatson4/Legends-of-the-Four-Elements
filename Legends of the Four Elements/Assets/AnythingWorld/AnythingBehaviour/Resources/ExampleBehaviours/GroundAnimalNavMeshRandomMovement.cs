using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace AnythingWorld.Behaviour
{
    /// <summary>
    /// Enhances GroundAnimalRandomMovementBase with navigation and movement capabilities using Unity's NavMesh system,
    /// supporting dynamic obstacle avoidance, pathfinding, and specialized jumping behavior.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class GroundAnimalNavMeshRandomMovement : GroundAnimalRandomMovementBase
    {
        private const float NavMeshAgentDefaultAngularSpeed = 720;
        private const float RotationMultiplier = 200;

        private NavMeshAgent _agent;
        private float _navMeshSampleDistance;
        private bool _isInitialized;
        
        // Overrides the Start method to set up NavMeshAgent specific properties.
        protected override void Start()
        {
            base.Start();

            if (Rb)
            {
                Rb.isKinematic = true; 
            }

            _agent = GetComponent<NavMeshAgent>();
            
            // If the agent's dimensions are not set, set them and reset the NavMeshAgent.
            if (!IsNavMeshAgentDimensionsSet())
            {
                NavMeshHandler.SetAgentDimensionsAndType(_agent, gameObject, Extents);
                _agent.enabled = false;
            }
            
            // If the agent is not on the NavMesh, place it on the ground and reset state by disabling and enabling.
            if (!_agent.isOnNavMesh)
            {
                PlaceOnGround();
                _agent.enabled = false;
            }
            
            _agent.enabled = true;
            
            SetNavMeshAgentMovementParameters();
            
            // As recommended in docs https://docs.unity3d.com/ScriptReference/AI.NavMesh.SamplePosition.html
            _navMeshSampleDistance = _agent.height * 2;
        }

        // Regularly updates movement towards the goal and handles jumping using OffMeshLinks.
        protected override void Update()
        {
            base.Update();
            
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
            
            if (canJump && !IsJumping && _agent.isOnOffMeshLink)
            {
                StartCoroutine(NavMeshParabolaJump());
            }

            if (IsGoalReached || IsJumping)
            {
                ElapsedPositionCheckTime = 0;
                return;
            }
            
            RotateToGoal();
            UpdateMovementAnimation(_agent.velocity.magnitude);
        }

        // Adjusts NavMeshAgent movement parameters when they are changed in the Inspector.
        protected override void OnValidate()
        {
            base.OnValidate();
            SetNavMeshAgentMovementParameters();
        }

        // Sets the new goal for the NavMeshAgent to navigate towards.
        protected override void SetNewGoalParameters(Vector3 newGoalPosition)
        {
            base.SetNewGoalParameters(newGoalPosition);
            _agent.destination = GoalPosition;
        }
        
        // Marks the goal as reached and stops the agent's moving animation.
        protected override void SetGoalReachedParameters()
        {
            base.SetGoalReachedParameters();
            UpdateMovementAnimation(0);
        }

        // Determines if the NavMeshAgent is close enough to its destination to consider the goal reached.
        protected override bool IsNearGoal()
        {
            return !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance &&
                   (!_agent.hasPath || _agent.velocity.sqrMagnitude == 0f);
        }
        
#if UNITY_EDITOR
        // Displays the NavMeshAgent's current path to goal in addition to common visualization.
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            if (!_agent || !showGizmos)
            {
                return;
            }
            
            Gizmos.color = Color.black;
            var agentPath = _agent.path;
            Vector3 prevCorner = transform.position;
            foreach (var corner in agentPath.corners)
            {
                Gizmos.DrawLine(prevCorner, corner);
                Gizmos.DrawSphere(corner, 0.1f);
                prevCorner = corner;
            }
        }
#endif
        // Checks if the NavMeshAgent's dimensions have been properly set up by comparing them to default values.
        private bool IsNavMeshAgentDimensionsSet()
        {
            return !(Mathf.Approximately(_agent.stoppingDistance, 0) && 
                   Mathf.Approximately(_agent.baseOffset, 0) && 
                   Mathf.Approximately(_agent.height, 2) && 
                   Mathf.Approximately(_agent.radius, 0.5f));
        }

        // Configures the NavMeshAgent's movement settings based on the class's properties.
        private void SetNavMeshAgentMovementParameters()
        {
            if (!_isInitialized && Mathf.Approximately(angularSpeed, BaseAngularSpeed))
            {
                angularSpeed = NavMeshAgentDefaultAngularSpeed;
                _isInitialized = true;
            }
            
            if (!_agent)
            {
                return;
            }
            
            _agent.speed = scaledSpeed;
            _agent.acceleration = scaledAcceleration;
            _agent.angularSpeed = angularSpeed;
            _agent.stoppingDistance = stoppingDistance;
            _agent.autoTraverseOffMeshLink = !canJump;
        }
        
        // Handles the rotation of the agent based on its velocity.
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

        // Attempts to find a valid new goal position on the NavMesh.
        private bool TryGenerateNewGoal(out Vector3 newGoal)
        {
            newGoal = Vector3.zero;
            var randomDir = transform.position + Random.insideUnitSphere * maxGoalSpawnRadius;
                
            var filter = new NavMeshQueryFilter
            {
                agentTypeID = _agent.agentTypeID,
                areaMask = NavMesh.AllAreas
            };

            if (!NavMesh.SamplePosition(randomDir, out var hit, _navMeshSampleDistance, filter))
            {
                return false;
            }

            newGoal = hit.position;
            return true;
        }
        
        // Manages the jump action when the NavMeshAgent encounters an OffMeshLink.
        private IEnumerator NavMeshParabolaJump()
        {
            _agent.velocity = Vector3.zero;
            var endPos = _agent.currentOffMeshLinkData.endPos;
            yield return StartCoroutine(ParabolaJump(endPos));
            _agent.CompleteOffMeshLink();
        }
    }
}
