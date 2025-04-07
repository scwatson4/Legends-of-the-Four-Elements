using System;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace AnythingWorld.Behaviour
{
    /// <summary>
    /// An abstract base class covering movement of agents without jumping capabilities which utilize
    /// Unity's physics system. Supports both vertical and horizontal movement.
    /// </summary>
    public abstract class NonJumpingRandomMovementBase : RandomMovementBase
    {
        protected bool HasVerticalMovement;
        protected Action UpdateAnimationAction;

        // Clean references held by the Action are released when the object is no longer active.
        public void OnDisable()
        {
            UpdateAnimationAction = null;
        }

        // Handles movement logic in the FixedUpdate cycle.
        protected virtual void FixedUpdate()
        {
            if (IsResting)
            {
                return;
            }

            if (IsGoalReached)
            {
                if (TryGenerateNewGoal(out var newGoalPosition))
                {
                    SetNewGoalParameters(newGoalPosition);
                }
                return;
            }
            
            if (IsObstacleAhead() || IsNearGoal())
            {
                SetGoalReachedParameters();
            }
            
            if (IsGoalReached)
            {
                return;
            }
            
            MoveTowardsGoal();
            UpdateAnimationAction?.Invoke();
        }
        
        // Moves the agent towards the target goal with consideration for vertical movement.
        private void MoveTowardsGoal()
        {
            Vector3 direction;
            if (HasVerticalMovement)
            {
                direction = (GoalPosition - transform.position).normalized;
            }
            else
            {
                var goalLeveled = GoalPosition;
                goalLeveled.y = transform.position.y;

                direction = (goalLeveled - transform.position).normalized;
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
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, angularSpeed * Time.fixedDeltaTime);
            }

            // Apply velocity
            Rb.MovePosition(transform.position + Velocity * Time.fixedDeltaTime);
        }

        // Checks for obstacles in the direct path to the goal.
        private bool IsObstacleAhead()
        {
            var floorPosition = transform.TransformPoint(Center);
            floorPosition.y -= Extents.y / 2;
            var goal = GoalPosition;

            var dirToGoal = goal - floorPosition;
            dirToGoal.Normalize();
            
            return Physics.Raycast(floorPosition, dirToGoal, ObstacleDetectorSizeScaled + Extents.z);
        }

        // Abstract method to be implemented by derived classes for generating new goals.
        protected abstract bool TryGenerateNewGoal(out Vector3 newGoal);
    }
}