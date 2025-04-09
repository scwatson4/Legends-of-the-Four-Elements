namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// A class designed for flying and swimming entities that disables gravity and enables vertical movement.
    /// </summary>
    [System.Serializable]
    public class MoveToGoal3D : NonJumpingMoveToGoalBase
    {
        /// <summary>
        /// Initializes the entity, disabling gravity and setting min and max movement height.
        /// </summary>
        public override void OnInit()
        {
            base.OnInit();
            
            if (!canRun)
            {
                return;
            }
            
            Rb.useGravity = false;
            HasVerticalMovement = true;
        }
    }
}
