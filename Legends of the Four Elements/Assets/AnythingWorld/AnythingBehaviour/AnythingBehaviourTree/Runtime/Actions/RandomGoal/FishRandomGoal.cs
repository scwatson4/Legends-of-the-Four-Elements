using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Extends the base class to apply to underwater navigation.
    /// Allows for setting specific depth parameters and surface height to ensure that fish is always below water surface.
    /// </summary>
    [System.Serializable]
    public class FishRandomGoal : FlyingSwimmingRandomGoalBase
    {
        [Tooltip("Use manually set Y coordinate of the surface. " +
                 "If off defaults to spawn position.")]
        public bool useCustomSurfaceHeight = true; 
        public float surfaceYCoordinate = 3; 
        public float swimmingDepth = 3;

        private bool _wasSpawnPositionUsed;
        
        /// <summary>
        /// Sets the vertical limits for fish movement based on surface height and swimming depth.
        /// </summary>
        protected override void SetHeightLimits()
        {
            if (useCustomSurfaceHeight)
            {
                MaxHeightCoordinate = surfaceYCoordinate;
            }
            else if (!_wasSpawnPositionUsed)
            {
                MaxHeightCoordinate = context.Transform.position.y;
                _wasSpawnPositionUsed = true;
            }

            MinHeightCoordinate = MaxHeightCoordinate - swimmingDepth + Extents.y;
            MaxHeightCoordinate -= Extents.y;
        }
    }
}
