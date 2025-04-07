using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Action Node to wait a certain time before executing next node in queue
    /// </summary>
    [System.Serializable]
    public class Wait : ActionNode
    {
        [Tooltip("Amount of time to wait before returning success")] 
        public float duration = 1;
       
        private float _elapsedRestTime;

        /// <summary>
        /// Defines the behavior at the start of the Node's lifecycle.
        /// </summary>
        protected override void OnStart()
        {
            _elapsedRestTime = 0;
        }
        
        protected override void OnStop() {}

        /// <summary>
        /// Monitors if the agent should be waiting based on elapsed time.
        /// </summary>
        protected override State OnUpdate()
        {
            if (_elapsedRestTime < duration)
            {
                _elapsedRestTime += Time.deltaTime;
                return State.Running;
            }
            
            return State.Success;   
        }
    }
}
