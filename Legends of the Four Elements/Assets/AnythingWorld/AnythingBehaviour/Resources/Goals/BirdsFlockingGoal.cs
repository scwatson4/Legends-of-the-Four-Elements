 using AnythingWorld.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AnythingWorld.Behaviour
{
    public class BirdsFlockingGoal : WalkInPackGoal
    {
        [Tooltip("Set the maximum flying height a pack can fly from the animal spawn point. On Runtime, the actually maximum FlyingHeight is defined by pack leader.")]
        public float flockingMaxFlyingHeightFromSpawnPoint = 5f;

        private const float packFollowerDestinationFlyingHeightOffset = 3f; //@Hardcoded

        protected override void Start()
        {
            base.Start();
            if (TryGetComponent<Rigidbody>(out var rigidbody))
            {
                rigidbody.useGravity = false;
                rigidbody.freezeRotation = true;
            }

            if (GetComponentInChildren<Animator>()) {
                GetComponentInChildren<Animator>().SetBool("InFlight", true);
            }
        }

        public override void ExecuteGoal()
        {
            if (GetComponentInChildren<AnythingWorld.Animation.FlyingAnimationController>())
            {
                GetComponentInChildren<AnythingWorld.Animation.FlyingAnimationController>().Fly();
            }
            else if (GetComponentInChildren<Animator>())
            {
                GetComponentInChildren<Animator>().SetTrigger("Flap");
            }
            base.ExecuteGoal();

            goToDestinationBehaviourComponent.SetAnimalType(AnimalType.FlyingAnimal,
                                                                       flockingMaxFlyingHeightFromSpawnPoint * distanceScaleFactor);
        }


        protected override void SetPackNewDestination()
        {
            //Only Pack Leader sets the Pack Destination.
            if (packLeader == gameObject)
            {
                if (goToDestinationBehaviourComponent)
                {
                    if (isCustomDestinationActive)
                    {
                        leaderDestination = customDestination;
                    }
                    else
                    {
                        leaderDestination = goToDestinationBehaviourComponent.GetARandomDestinationInsideAPerimeter(spawnPosition, packMovementRange);
                    }
                    goToDestinationBehaviourComponent.SetMyDestination(leaderDestination);
                    foreach (var member in followers)
                    {
                        Vector3 memberDestination = new Vector3(Random.Range(0, packFollowerDestinationOffset), Random.Range(0, packFollowerDestinationFlyingHeightOffset), Random.Range(0, packFollowerDestinationOffset)) + leaderDestination;
                        BirdsFlockingGoal memberBirdsFlockingGoalComponent = member.GetComponent<BirdsFlockingGoal>();
                        if (memberBirdsFlockingGoalComponent)
                        {
                            memberBirdsFlockingGoalComponent.followerDestination = memberDestination;
                            if (memberBirdsFlockingGoalComponent.leaderIsSettingMyFirstPosition)
                            {
                                member.GetComponent<GoToDestination>().SetMyDestination(memberDestination);
                                memberBirdsFlockingGoalComponent.leaderIsSettingMyFirstPosition = false;
                            }
                        }
                    }
                }
            }
            else
            {
                if (goToDestinationBehaviourComponent.GetMyDestination() != followerDestination)
                {
                    goToDestinationBehaviourComponent.SetMyDestination(followerDestination);
                }
            }

            if (GetComponentInChildren<Animator>())
            {
                if ((leaderDestination.y > transform.position.y && packLeader == gameObject) || (followerDestination.y > transform.position.y && packLeader != gameObject))
                {
                    GetComponentInChildren<Animator>().SetBool("EffortfulFlap", true);
                }
                else
                {
                    GetComponentInChildren<Animator>().SetBool("EffortfulFlap", false);
                }
                GetComponentInChildren<Animator>().SetTrigger("Flap");
            }
        }

        protected override void SpawnPackController()
        {
            bool alreadyLeader = false;

            if (!AnythingPackController.packsCentralController)
            {
                GameObject packController = new GameObject("PacksCentralController");
                AnythingPackController.packsCentralController = packController;
            }

            foreach (var controller in AnythingPackController.allPacksControllers)
            {
                if (controller.packLeader == gameObject)
                {
                    walkingAnimalsPackController = controller as BirdsFlockingController;
                    walkingAnimalsPackController.ResetGizmos();
                    alreadyLeader = true;
                }
            }

            if (!alreadyLeader)
            {
                walkingAnimalsPackController = AnythingPackController.packsCentralController.AddComponent<BirdsFlockingController>();
                AnythingPackController.allPacksControllers.Add(walkingAnimalsPackController);
                walkingAnimalsPackController.SetPackLeader(gameObject);
                walkingAnimalsPackController.packModelEntity = this.myModelEntity;
            }
                
        }

#if UNITY_EDITOR
        protected override void ShowPackMovementRange()
        {
            Handles.color = Color.blue;
            UnityEditor.Handles.DrawWireDisc(spawnPosition, Vector3.up, packMovementRange);
            UnityEditor.Handles.DrawWireDisc(spawnPosition + new Vector3(0, flockingMaxFlyingHeightFromSpawnPoint * distanceScaleFactor, 0), Vector3.up, packMovementRange);
            UnityEditor.Handles.DrawLine(spawnPosition + new Vector3(packMovementRange, 0, 0), spawnPosition + new Vector3(packMovementRange, flockingMaxFlyingHeightFromSpawnPoint * distanceScaleFactor, 0));
            UnityEditor.Handles.DrawLine(spawnPosition + new Vector3(packMovementRange * -1, 0, 0), spawnPosition + new Vector3(packMovementRange * -1, flockingMaxFlyingHeightFromSpawnPoint * distanceScaleFactor, 0));
            GUI.color = Color.white;
            UnityEditor.Handles.Label(spawnPosition + (Vector3.left * packMovementRange), new GUIContent("Pack Movement Range"));
        }

        protected override void ShowPackMovementDistance()
        {

            Handles.color = Color.blue;
            UnityEditor.Handles.DrawWireDisc(gameObject.transform.position, Vector3.up, packMovementRange);
            UnityEditor.Handles.DrawWireDisc(gameObject.transform.position + new Vector3(0, flockingMaxFlyingHeightFromSpawnPoint * distanceScaleFactor, 0), Vector3.up, packMovementRange);
            UnityEditor.Handles.DrawLine(gameObject.transform.position + new Vector3(packMovementRange, 0, 0), gameObject.transform.position + new Vector3(packMovementRange, flockingMaxFlyingHeightFromSpawnPoint * distanceScaleFactor, 0));
            UnityEditor.Handles.DrawLine(gameObject.transform.position + new Vector3(packMovementRange * -1, 0, 0), gameObject.transform.position + new Vector3(packMovementRange * -1, flockingMaxFlyingHeightFromSpawnPoint * distanceScaleFactor, 0));
            GUI.color = Color.white;
            UnityEditor.Handles.Label(gameObject.transform.position + (Vector3.left * packMovementRange), new GUIContent("Pack Movement Distance"));
        }
#endif
    }
}
