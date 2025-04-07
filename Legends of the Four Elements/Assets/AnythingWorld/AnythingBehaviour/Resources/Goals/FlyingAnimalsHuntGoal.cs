using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnythingWorld.Behaviour
{
    public class FlyingAnimalsHuntGoal : HuntGoal
    {
        //Hunting Settings
        [Tooltip("Set the new center point Axis Y (FlyingHeight Axis) of the perimeter in where an animal will move to search for its preys.")]
        public float newSearchingPreyCenterPointY;

        protected override void Update()
        {
            if (!goalActivated) { return; }

            if (!isChasingPrey)
            {
                detectPreyTimer += Time.deltaTime;
                if (detectPreyTimer >= searchForPreyFrequencyInSeconds)
                {
                    TryToDetectPrey();
                    detectPreyTimer -= searchForPreyFrequencyInSeconds;
                }
            }
            else
            {
                if (goToDestinationBehaviourComponent && chasedPrey)
                {
                    if (Vector3.Distance(transform.position, chasedPrey.transform.position) <= preyDetectionRange)
                    {
                        goToDestinationBehaviourComponent.speed = goToDestinationBehaviourComponent.maxSpeed + predatorAditionalSpeed;
                        goToDestinationBehaviourComponent.turnSpeed = huntingTurnSpeed; 
                        goToDestinationBehaviourComponent.goalRadius = huntingGoalRadius; 
                        goToDestinationBehaviourComponent.SetMyDestination(new Vector3(chasedPrey.transform.position.x, chasedPrey.transform.position.y, chasedPrey.transform.position.z));
                    }
                    else
                    {
                        isChasingPrey = false;
                        chasedPrey = null;
                        SearchForPrey();
                    }

                }
            }
        }
        public override void ExecuteGoal()
        {
            base.ExecuteGoal();

            goToDestinationBehaviourComponent.SetAnimalType(AnimalType.FlyingAnimal);
            defaultTurnSpeed = goToDestinationBehaviourComponent.turnSpeed;
            defaultGoalRadius = goToDestinationBehaviourComponent.goalRadius;
        }
        protected override void SearchForPrey()
        {
            Vector3 searchPreyCenterPoint;
            if (setNewSearchingPreyCenterPoint)
            {
                searchPreyCenterPoint = new Vector3(newSearchingPreyCenterPointX, newSearchingPreyCenterPointY, newSearchingPreyCenterPointZ);
            }
            else
            {
                searchPreyCenterPoint = spawnPoint;
            }
            goToDestinationBehaviourComponent.turnSpeed = defaultTurnSpeed; 
            goToDestinationBehaviourComponent.goalRadius = defaultGoalRadius; 
            goToDestinationBehaviourComponent.speed = UnityEngine.Random.Range(goToDestinationBehaviourComponent.minSpeed, goToDestinationBehaviourComponent.maxSpeed);
            Vector3 randomDestination = goToDestinationBehaviourComponent.GetARandomDestinationInsideAPerimeter(searchPreyCenterPoint, searchingForPreyMovementRange);
            randomDestination.y = spawnPoint.y;
            goToDestinationBehaviourComponent.SetMyDestination(randomDestination);
        }
    }
}
