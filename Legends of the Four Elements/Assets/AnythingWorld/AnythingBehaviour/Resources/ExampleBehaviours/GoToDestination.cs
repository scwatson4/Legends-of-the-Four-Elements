using AnythingWorld.Animation;
using UnityEngine;
using System;
using UnityEngine.Serialization;

namespace AnythingWorld.Behaviour
{
    public class GoToDestination : MonoBehaviour
    {
        [Header("Gizmos Controller")]
        public bool showAnimalDestination = true;

        [Header("Animation State Settings")]
        public MovementJumpLegacyController animationLegacyController;
        public bool walk = true;
        public bool run = true;
        public bool stopMovement = false;

        [Header("Thresholds")]
        [Tooltip("Speed above which walk animation is called.")]
        public float walkThreshold = 0.1f;
        [Tooltip("Speed above which run animation is called.")]
        public float runThreshold = 1.5f;
        [Tooltip("Limit movement speed to the maximum threshold active.")]
        public bool clampToActivatedThresholds = false;
        public float stopThreshold = 0.1f;

        //input
        [Header("Speed")]


        [HideInInspector]
        public float maxSpeed = 3f; //@Hardcoded
        [HideInInspector]
        public float minSpeed = 2.5f; //@Hardcoded
        [HideInInspector]
        public float turnSpeed = 0.7f; //@Hardcoded
        [HideInInspector]
        public float speed;

        [Header("FlyngAnimals FlyingHeight")]
        [HideInInspector]
        public float maximumFlyingHeightOrDepthSwimFromSpawnPoint;

        [Header("Goal Settings")]
        [HideInInspector]
        public float goalRadius = 4f; //@Hardcoded
        [HideInInspector]
        public bool isWalkingInPack = false;
        
        //Reached Destination Callback
        public Action onDestinationReached;

        //Position variables
        private Vector3 goalPosition;
        private Vector3 directionToGoal;
        private bool IsDestinationSet = false;
        

        //Debug
        private float variableSpeed;
        private float distanceToGoal;
        private float currentBrakeForce = 1f;

        //Animal Type
        private AnimalType animalType = AnimalType.WalkingAnimal;


        private void Start()
        {
            if (GetComponentInChildren<MovementJumpLegacyController>())
            {
                animationLegacyController = GetComponentInChildren<MovementJumpLegacyController>();
                animationLegacyController.crossfadeTime = 0.5f;
            }

            speed = UnityEngine.Random.Range(minSpeed, maxSpeed);
        }

        public Vector3 GetMyDestination()
        {
            return goalPosition;
        }

        public void SetMyDestination(Vector3 destination)
        {
            goalPosition = destination;
            IsDestinationSet = true;
        }

        public void Update()
        {
            if (IsDestinationSet)
            {
                variableSpeed = Mathf.Lerp(speed * currentBrakeForce, variableSpeed, Time.deltaTime);
                if (variableSpeed < stopThreshold) variableSpeed = 0;
                distanceToGoal = Vector3.Distance(goalPosition, transform.position);
                bool runningOrWalkingNotSelected = false;

                if (animalType == AnimalType.WalkingAnimal && isWalkingInPack)
                {
                    currentBrakeForce = Mathf.Clamp(distanceToGoal - (goalRadius / 2) - currentBrakeForce, 0, 1);
                }
                
                if (distanceToGoal <= goalRadius)
                {
                    if (onDestinationReached != null)
                    {
                        onDestinationReached();
                    }
                }

                //Calculate vector to goal
                if(animalType == AnimalType.FlyingAnimal || animalType == AnimalType.SwimmingAnimal)
                {
                    //On Y axis, Flying or Swimming animals can go higher or lower.
                    directionToGoal = new Vector3(goalPosition.x, goalPosition.y, goalPosition.z) - transform.position;
                }
                else
                {
                    //On Y axis, Walking animals aways moves in the same position.
                    //TODO: This sytem needs improvement in order that the animal can go to higher and lower (for example, to climb a montain)
                    //We could improve this system for example with a navimesh system.
                    directionToGoal = new Vector3(goalPosition.x, transform.position.y, goalPosition.z) - transform.position;

                    if (walk && run)
                    {
                        //Blend animation
                        animationLegacyController?.BlendMovementAnimationOnSpeed(variableSpeed/maxSpeed);
                    }
                    else if (walk && !run)
                    {
                        variableSpeed = Mathf.Min(variableSpeed, runThreshold);
                        animationLegacyController?.PlayAnimation("walk");
                    }
                    else if (!walk && run)
                    {
                        variableSpeed = maxSpeed;
                        animationLegacyController?.PlayAnimation("run");
                    }
                    else
                    {
                        runningOrWalkingNotSelected = true;
                        animationLegacyController?.PlayAnimation("idle");
                    }
                }

                if (runningOrWalkingNotSelected) return;
                if (stopMovement) return;
                TurnTowardsTarget(directionToGoal);
                MoveTowardsTarget();
            }
        }
        public void MoveTowardsTarget()
        {
            transform.position = Vector3.Lerp(transform.position, transform.position + (transform.forward), variableSpeed * Time.deltaTime);
        }

        public void TurnTowardsTarget(Vector3 directionToTarget)
        {
            // Turn towards the target
            var normalizedLookDirection = directionToTarget.normalized;
            var m_LookRotation = Quaternion.LookRotation(normalizedLookDirection);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, m_LookRotation, Time.deltaTime * turnSpeed);
        }

        public Vector3 GetARandomDestinationInsideAPerimeter(Vector3 perimeterCenter, float perimeterRadius)
        {
            var randomPosition = UnityEngine.Random.insideUnitSphere * perimeterRadius;
            if(animalType == AnimalType.FlyingAnimal)
            {
                //On Y axis, Flying or Swimming animals can go higher or lower.
                //On Y axis, Flying animals moves randomly from its spawn point to higher positions (spawn point is the lower limit).
                float randomFlyingHeight = UnityEngine.Random.Range(0, maximumFlyingHeightOrDepthSwimFromSpawnPoint);
                randomPosition = new Vector3(randomPosition.x, randomFlyingHeight, randomPosition.z);
            }
            else if(animalType == AnimalType.SwimmingAnimal)
            {
                //On Y axis, Flying or Swimming animals can go higher or lower. 
                //On Y axis, Swimming animals moves randomly from its spawn points to lower positions (spawn point is the higher limit).
                float randomDepthSwim = UnityEngine.Random.Range(0, maximumFlyingHeightOrDepthSwimFromSpawnPoint);
                randomPosition = new Vector3(randomPosition.x, randomDepthSwim * -1, randomPosition.z);
            }
            else
            {
                //On Y axis, Walking animals aways moves in the same position.
                //TODO: This sytem needs improvement in order that the animal can go to higher and lower (for example, to climb a mountain)
                //We could improve this system for example with a navimesh system.
                randomPosition = new Vector3(randomPosition.x, transform.position.y, randomPosition.z);
            }

            randomPosition = perimeterCenter + randomPosition;

            return randomPosition;
        }

        public void SetAnimalType(AnimalType _animalType, float _flockingMaxFlyingHeightOrShoalingMaxSwimmingDeep = 5f) 
        {
            animalType = _animalType;
            maximumFlyingHeightOrDepthSwimFromSpawnPoint = _flockingMaxFlyingHeightOrShoalingMaxSwimmingDeep;

            if(_animalType == AnimalType.FlyingAnimal || _animalType == AnimalType.SwimmingAnimal)
            {
                maxSpeed = 5f; //@Hardcoded
                minSpeed = 2f; //@Hardcoded
                turnSpeed = 2f; //@Hardcoded
            }
        }

#if UNITY_EDITOR
        // Draws gizmos in the Unity editor when the object is selected.
        private void OnDrawGizmosSelected()
        {

            if (!showAnimalDestination) return;
            try
            {
                GUI.color = Color.yellow;
                UnityEditor.Handles.Label(goalPosition + (Vector3.left * goalRadius), new GUIContent("Goal"));
                Gizmos.color = Color.green;
                Gizmos.DrawRay(transform.position, directionToGoal);
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position, transform.forward * distanceToGoal / 2);

                UnityEditor.Handles.DrawWireDisc(goalPosition, Vector3.up, goalRadius);
               
                var angle = Vector3.SignedAngle(transform.forward, directionToGoal, Vector3.up);
                var re = Vector3.Cross(transform.forward, directionToGoal);
                UnityEditor.Handles.Label(Vector3.Lerp(transform.position, goalPosition, 0.5f), angle.ToString("F2") + "\u0176");
                UnityEditor.Handles.DrawWireArc(transform.position, transform.up, transform.forward, angle, distanceToGoal * 0.5f);
                GUI.color = Color.white;
            }
            catch
            {

            }
        }
#endif
    }

    public enum AnimalType
    {
        WalkingAnimal,
        FlyingAnimal,
        SwimmingAnimal
    }
}

