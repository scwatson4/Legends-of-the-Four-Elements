namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// An enhanced selector node that interrupts its currently running child if a higher priority child becomes
    /// ready to run. This allows for dynamic reevaluation of priorities among child nodes during execution.
    /// </summary>
    [System.Serializable]
    public class InterruptSelector : Selector
    {
        /// <summary>
        /// Evaluates children nodes and interrupts the currently running child if
        /// a higher priority child becomes available.
        /// </summary>
        protected override State OnUpdate()
        {
            int previous = current; 
            base.OnStart(); 
            // Evaluate children based on the base selector logic.
            var status = base.OnUpdate(); 

            // If the current child has changed due to the base update logic,
            if (previous != current) 
            {
                // and the previously running child was still running,
                if (children[previous].state == State.Running)
                {
                    // then abort the previously running child.
                    children[previous].Abort(); 
                }
            }

            return status; // Return the status determined by the base selector logic.
        }
    }
}
