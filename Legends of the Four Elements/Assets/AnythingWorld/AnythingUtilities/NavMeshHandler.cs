using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace AnythingWorld.Behaviour
{
    /// <summary>
    /// Static class responsible for managing NavMesh and NavMeshAgent properties and creation in Unity.
    /// </summary>
    public static class NavMeshHandler
    {
        
        /// <summary>
        /// Adds a NavMeshAgent to a GameObject and sets its dimensions and type based on given extents.
        /// Creates a new NavMesh of the agent's type if one does not exist.
        /// </summary>
        public static void AddNavMeshAndAgent(Vector3 extents, GameObject go)
        {
            var agent = go.AddComponent<NavMeshAgent>();
            SetAgentDimensionsAndType(agent, go, extents);
            
            if (!IsNavMeshWithIdExists(agent.agentTypeID))
            {
                CreateNavMeshOfType(agent);
            }
            
            agent.enabled = false;
            agent.enabled = true;
        }
        
        /// <summary>
        /// Creates a new NavMesh specific to the type of the given NavMeshAgent.
        /// </summary>
        private static void CreateNavMeshOfType(NavMeshAgent agent)
        {
            var name = agent.name;
            var poundIndex = name.IndexOf('#'); 
            if (name.IndexOf('#') != -1)
            {
                name = name.Substring(0, poundIndex);
            }
            
            var navMeshGo = new GameObject($"{name}NavMesh");
            var surface = navMeshGo.AddComponent<NavMeshSurface>();
            surface.agentTypeID = agent.agentTypeID;
            surface.BuildNavMesh();
        }
        
        /// <summary>
        /// Sets the dimensions and type of the NavMeshAgent based on the given extents.
        /// </summary>
        public static void SetAgentDimensionsAndType(NavMeshAgent agent, GameObject go, 
            Vector3 extents)
        {
            var transform = go.transform;
            
            var radiusMultiplier = .9f;
            agent.baseOffset = extents.y / transform.lossyScale.y;
            agent.height = agent.baseOffset * 2;

            var isLargestX = extents.x > extents.z;
            var largestExtents = isLargestX ? extents.x : extents.z;
            var radius = isLargestX ? largestExtents / transform.lossyScale.x : largestExtents / transform.lossyScale.z;
            agent.radius = radius * radiusMultiplier;
            
            var agentTypeRadius = largestExtents * radiusMultiplier;
            
            SetMostSuitableType(agentTypeRadius, agent);
        }

        /// <summary>
        /// Determines and sets the most suitable NavMeshAgent type for the given agent,
        /// based on a comparison of existing types and the agent's radius.
        /// </summary>
        private static void SetMostSuitableType(float agentTypeRadius, NavMeshAgent agent)
        {
            var bestTypeId = -1;
            var minRadiusDifference = float.MaxValue;
            
            int agentTypeCount = NavMesh.GetSettingsCount();
            for (int i = 0; i < agentTypeCount; i++)
            {
                var currentSetting = NavMesh.GetSettingsByIndex(i);
                var currentDifference = Mathf.Abs(currentSetting.agentRadius - agentTypeRadius); 
                if (currentDifference < minRadiusDifference)
                {
                    minRadiusDifference = currentDifference;
                    bestTypeId = currentSetting.agentTypeID;
                }
            }

            agent.agentTypeID = bestTypeId;
        }
        
        /// <summary>
        /// Checks if a NavMesh of the specified type id exists in the scene.
        /// </summary>
        private static bool IsNavMeshWithIdExists(int typeId) =>
            Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None).Any(x => x.agentTypeID == typeId);
    }
}
