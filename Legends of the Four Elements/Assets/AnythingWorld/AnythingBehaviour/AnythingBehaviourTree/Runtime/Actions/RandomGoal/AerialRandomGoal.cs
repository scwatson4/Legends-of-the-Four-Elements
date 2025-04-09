namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Uses flying agent properties to set goal generation height constraints.
    /// </summary>
    [System.Serializable]
    public class AerialRandomGoal : FlyingSwimmingRandomGoalBase
    {
        // Good defaults for helicopters are 15, 35 for minMaxHeight and 10, 40 for minMaxGoalRadius.
        // Good defaults for birds are 2, 10 for minMaxHeight.
        
        public float groundYCoordinate;
        public float minFlyingHeight = 2;
        public float maxFlyingHeight = 10;
        
        /// <summary>
        /// Sets the vertical movement limits for the flying agent based on ground coordinates and height preferences.
        /// </summary>
        protected override void SetHeightLimits()
        {
            MinHeightCoordinate = groundYCoordinate + minFlyingHeight + Extents.y;
            MaxHeightCoordinate = groundYCoordinate + maxFlyingHeight - Extents.y;
        }
    }
}
