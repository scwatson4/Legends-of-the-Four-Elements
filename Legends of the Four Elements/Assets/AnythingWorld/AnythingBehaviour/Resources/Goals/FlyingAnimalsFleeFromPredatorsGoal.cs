using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AnythingWorld.Behaviour
{
    public class FlyingAnimalsFleeFromPredatorsGoal : FleeFromPredatorsGoal
    {
        //Flee From Predators Settings
        [Tooltip("Set the new center point Axis Y (FlyingHeight Axis) of the perimeter in where a animal can go when moving randomly.")]
        public float maxFlyingHeightFromSpawnPoint = 5f;

        protected override void Update()
        {
            detectPredatorTimer += Time.deltaTime;
            if (detectPredatorTimer >= searchForPredatorFrequencyInSeconds)
            {
                TryToDetectPredator();
                detectPredatorTimer -= searchForPredatorFrequencyInSeconds;
            }

            if (!goalActivated) return;

            if (goToDestinationBehaviourComponent && predatorDetected)
            {
                GameObject closestPredator = null;
                float closestDistanceFromPredator = 0f;

                for (int i = 0; i < detectedPredators.Count; i++)
                {
                    if (i <= 0)
                    {
                        closestPredator = detectedPredators[i];
                        closestDistanceFromPredator = Vector3.Distance(transform.position, closestPredator.transform.position);
                    }
                    else
                    {
                        if (Vector3.Distance(transform.position, detectedPredators[i].transform.position) < closestDistanceFromPredator)
                        {
                            closestPredator = detectedPredators[i];
                            closestDistanceFromPredator = Vector3.Distance(transform.position, closestPredator.transform.position);
                        }
                    }
                }

                if (closestPredator)
                {
                    Vector3 fleeLocation = transform.position + (transform.position - closestPredator.transform.position).normalized;
                    goToDestinationBehaviourComponent.speed = goToDestinationBehaviourComponent.maxSpeed;
                    goToDestinationBehaviourComponent.turnSpeed = fleeingFromPredatorTurnSpeed; //@Hardcoded
                    goToDestinationBehaviourComponent.goalRadius = fleeingFromPredatorGoalRadius; //@Hardcoded
                    float clampedFleeLocationYByMaxFlyingHeight = Mathf.Clamp(fleeLocation.y, spawnPoint.y, spawnPoint.y + maxFlyingHeightFromSpawnPoint);
                    goToDestinationBehaviourComponent.SetMyDestination(new Vector3(fleeLocation.x, clampedFleeLocationYByMaxFlyingHeight, fleeLocation.z));
                }
            }
        }
 
        public override void ExecuteGoal()
        {
            if (!goToDestinationBehaviourComponent)
            {
                goToDestinationBehaviourComponent = gameObject.AddComponent(typeof(GoToDestination)) as GoToDestination;
                goToDestinationBehaviourComponent.SetAnimalType(AnimalType.FlyingAnimal, maxFlyingHeightFromSpawnPoint);
                defaultTurnSpeed = goToDestinationBehaviourComponent.turnSpeed;
                defaultGoalRange = goToDestinationBehaviourComponent.goalRadius;
                goToDestinationBehaviourComponent.onDestinationReached += OnReachedDestination;

                if (!predatorDetected)
                {
                    WalkRandomly();
                }

                goalActivated = true;
            }
        }

        protected override void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR
            if (showDetectPredatorsRange)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(gameObject.transform.position, predatorDetectionRange);
                GUI.color = Color.white;
                UnityEditor.Handles.Label(gameObject.transform.position + (Vector3.left * predatorDetectionRange), new GUIContent("Predator Detection Range"));
            }

            if (showMovingRandomlyPerimeter)
            {
                Handles.color = Color.blue;
                Vector3 walkingRandomlyCenterPoint;
                if (setNewWalkingRandomlyCenterPoint)
                {
                    walkingRandomlyCenterPoint = new Vector3(newWalkingRandomlyCenterPointX, spawnPoint.y, newWalkingRandomlyCenterPointZ);
                }
                else
                {
                    if (spawnPointDefined)
                    {
                        walkingRandomlyCenterPoint = spawnPoint;
                    }
                    else
                    {
                        walkingRandomlyCenterPoint = gameObject.transform.position;
                    }

                }
                UnityEditor.Handles.DrawWireDisc(walkingRandomlyCenterPoint, Vector3.up, movingRandomlyMovementRange);
                UnityEditor.Handles.DrawWireDisc(walkingRandomlyCenterPoint + new Vector3(0, maxFlyingHeightFromSpawnPoint * distanceScaleFactor, 0), Vector3.up, movingRandomlyMovementRange);
                UnityEditor.Handles.DrawLine(walkingRandomlyCenterPoint + new Vector3(movingRandomlyMovementRange, 0, 0), walkingRandomlyCenterPoint + new Vector3(movingRandomlyMovementRange, maxFlyingHeightFromSpawnPoint * distanceScaleFactor, 0));
                UnityEditor.Handles.DrawLine(walkingRandomlyCenterPoint + new Vector3(movingRandomlyMovementRange * -1, 0, 0), walkingRandomlyCenterPoint + new Vector3(movingRandomlyMovementRange * -1, maxFlyingHeightFromSpawnPoint * distanceScaleFactor, 0));
                GUI.color = Color.white;
                UnityEditor.Handles.Label(walkingRandomlyCenterPoint + (Vector3.left * movingRandomlyMovementRange), new GUIContent("Walking Randomly movement range"));
            }
#endif
        }
    }
}
