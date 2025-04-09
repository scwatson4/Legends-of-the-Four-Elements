using UnityEngine;
using UnityEngine.AI;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Provides functionality for generating random movement goals for ground animals using Unity's NavMesh system.
    /// Ensures that generated goals are accessible within the NavMesh.
    /// </summary>
    [System.Serializable]
    public class GroundAnimalNavMeshRandomGoal : RandomGoalBase
    {
        private NavMeshAgent _agent;
        private float _navMeshSampleDistance;
        
        /// <summary>
        /// Sets up NavMesh sample distance.
        /// </summary>
        public override void OnInit()
        {
            base.OnInit();
            
            if (context.GameObject.TryGetComponent(out _agent))
            {
                // As recommended in docs https://docs.unity3d.com/ScriptReference/AI.NavMesh.SamplePosition.html
                _navMeshSampleDistance = _agent.height * 2;
            }
            else
            {
                Debug.LogWarning($"{context.GameObject.name} is missing NavMeshAgent component, " +
                                 "cannot generate a goal.");
                canRun = false;
                return;
            }
            
            canRun = true;
        }
        
        /// <summary>
        /// Attempts to find a valid new goal position on the NavMesh.
        /// </summary>
        protected override bool TryGenerateNewMovementGoal()
        {
            var randomDir = context.Transform.position + Random.insideUnitSphere * maxGoalSpawnRadius;
                
            var filter = new NavMeshQueryFilter
            {
                agentTypeID = _agent.agentTypeID,
                areaMask = NavMesh.AllAreas
            };

            if (!NavMesh.SamplePosition(randomDir, out var hit, _navMeshSampleDistance, filter))
            {
                return false;
            }

            newGoal.Value = hit.position;
            return true;
        }
    }
}
