using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    [System.Serializable]
    public class Timeout : DecoratorNode
    {
        [Tooltip("Returns failure after this amount of time if the subtree is still running.")] 
        public float duration = 1.0f;
        private float _startTime;

        /// <summary>
        /// Defines the behavior at the start of the Node's lifecycle.
        /// </summary>
        protected override void OnStart()
        {
            _startTime = Time.time;
        }

        /// <summary>
        /// Defines the behavior at the end of the Node's lifecycle.
        /// </summary>
        protected override void OnStop()
        {
        }

        /// <summary>
        /// Updates the state of the Node, determining its success or failure based on the child node's state.
        /// </summary>
        protected override State OnUpdate()
        {
            if (child == null)
            {
                return State.Failure;
            }

            if (Time.time - _startTime > duration)
            {
                return State.Failure;
            }

            return child.Update();
        }
    }
}
