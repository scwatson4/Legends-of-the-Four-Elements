using System;
using System.Collections;
using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// An abstract class covering movement of agents without jumping capabilities which utilize
    /// Unity's physics system. Supports both vertical and horizontal movement.
    /// </summary>
    [Serializable]
    public abstract class NonJumpingMoveToGoalBase : MoveToGoalBase
    {
        protected bool HasVerticalMovement;
        protected Action UpdateAnimationAction;

        /// <summary>
        /// Checks if agent has Rigidbody component.  
        /// </summary>
        public override void OnInit()
        {
            base.OnInit();
            
            if (!context.Rb)
            {
                Debug.LogWarning($"{context.GameObject.name} is missing RigidBody component required to move agent.");
                canRun = false;
            }
        }

        /// <summary>
        /// Starts FixedUpdate loop.
        /// </summary>
        protected override void OnStart()
        {
            base.OnStart();
            if (!canRun)
            {
                return;
            }

            Container.StartCoroutine(FixedUpdateCoroutine());
        }
        
        /// <summary>
        /// Stops FixedUpdate loop at the end of the Node's lifecycle.
        /// </summary>
        protected override void OnStop()
        {
            Container.StopCoroutine(FixedUpdateCoroutine());
        }
        
        /// <summary>
        /// Updates the state of the Node, determining its success or failure
        /// </summary>
        protected override State OnUpdate() 
        {
            if (!canRun)
            {
                return State.Failure;
            }
            
            if (IsGoalReached)
            {
                return State.Success;
            }
            
            StopIfStuck();

            return State.Running;
        }

        // Handles movement logic in the FixedUpdate cycle.
        protected virtual IEnumerator FixedUpdateCoroutine()
        {
            var waitForFixedUpdate = new WaitForFixedUpdate();

            while (!IsGoalReached)
            {
                if (IsObstacleAhead() || IsNearGoal())
                {
                    SetGoalReachedParameters();
                    break;
                }
                
                MoveTowardsTarget();
                UpdateAnimationAction?.Invoke();
                
                yield return waitForFixedUpdate;
            }
        }
        
        /// <summary>
        /// Moves the agent towards the target goal with consideration for vertical movement.
        /// </summary>
        private void MoveTowardsTarget()
        {
            Vector3 direction;
            if (HasVerticalMovement)
            {
                direction = (goalPosition - context.Transform.position).normalized;
            }
            else
            {
                var goalLeveled = goalPosition.Value;
                goalLeveled.y = context.Transform.position.y;

                direction = (goalLeveled - context.Transform.position).normalized;
            }
            
            Vector3 targetVelocity = direction * scaledSpeed;
           
            // Determine the angle between current velocity and target direction
            float turnAngle = Vector3.Angle(Velocity, targetVelocity);
            // Scale acceleration based on turn angle
            float finalAcceleration = Mathf.Lerp(scaledAcceleration, turnAcceleration, turnAngle / 180f);
            // Apply acceleration
            Velocity = Vector3.MoveTowards(Velocity, targetVelocity, finalAcceleration * Time.fixedDeltaTime);

            // Calculate rotation towards target
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                context.Transform.rotation = Quaternion.RotateTowards(context.Transform.rotation, targetRotation, angularSpeed * Time.fixedDeltaTime);
            }

            // Apply velocity
            Rb.MovePosition(context.Transform.position + Velocity * Time.fixedDeltaTime);
        }

        /// <summary>
        /// Checks for obstacles in the direct path to the goal.
        /// </summary>
        private bool IsObstacleAhead()
        {
            var floorPosition = context.Transform.TransformPoint(Center);
            floorPosition.y -= Extents.y / 2;
            var goal = goalPosition;

            var dirToGoal = goal - floorPosition;
            dirToGoal.Normalize();
            
            return Physics.Raycast(floorPosition, dirToGoal, ObstacleDetectorSizeScaled + Extents.z);
        }
    }
}
