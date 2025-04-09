using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AnythingWorld.Behaviour
{
    public class FishesShoalingGoal : WalkInPackGoal
    {
        [Tooltip("Set the maximum FlyingHeight a pack can fly from the animal spawn point. On Runtime, the actually maximum FlyingHeight is defined by pack leader.")]
        public float shoalingMaxDepthSwimFromSpawnPoint = 5f;

        private const float packFollowerDestinationDepthSwinOffset = 3f;
        protected override void Start()
        {

            base.Start();
            packMovementRange = 5f;
            packFollowerDestinationOffset = 3f; 
            if(TryGetComponent<Rigidbody>(out var rigidbody))
            {
                rigidbody.useGravity = false;

            }
        }


        public override void ExecuteGoal()
        {
            base.ExecuteGoal();

            goToDestinationBehaviourComponent.SetAnimalType(AnimalType.SwimmingAnimal,
                                                                       shoalingMaxDepthSwimFromSpawnPoint * distanceScaleFactor);
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
                        leaderDestination = goToDestinationBehaviourComponent.GetARandomDestinationInsideAPerimeter(walkingAnimalsPackController.anchor.position, packMovementRange);
                    }
                    goToDestinationBehaviourComponent.SetMyDestination(leaderDestination);
                    foreach (var member in followers)
                    {
                        Vector3 memberDestination = new Vector3(Random.Range(0, packFollowerDestinationOffset), Random.Range(0, packFollowerDestinationDepthSwinOffset), Random.Range(0, packFollowerDestinationOffset)) + leaderDestination;
                       FishesShoalingGoal memberFishesShoalingGoalComponent = member.GetComponent<FishesShoalingGoal>();
                        if (memberFishesShoalingGoalComponent)
                        {
                            memberFishesShoalingGoalComponent.followerDestination = memberDestination;
                            if (memberFishesShoalingGoalComponent.leaderIsSettingMyFirstPosition)
                            {
                                member.GetComponent<GoToDestination>().SetMyDestination(memberDestination);
                                memberFishesShoalingGoalComponent.leaderIsSettingMyFirstPosition = false;
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
                    walkingAnimalsPackController = controller as FishesShoalingController;
                    walkingAnimalsPackController.ResetGizmos();
                    alreadyLeader = true;
                }
            }

            if (!alreadyLeader)
            {
                walkingAnimalsPackController = AnythingPackController.packsCentralController.AddComponent<FishesShoalingController>();
                AnythingPackController.allPacksControllers.Add(walkingAnimalsPackController);
                walkingAnimalsPackController.SetPackLeader(gameObject);
                walkingAnimalsPackController.packModelEntity = this.myModelEntity;
            }
        }

        protected override void ShowPackMovementRange()
        {
#if UNITY_EDITOR
            Handles.color = Color.blue;
            UnityEditor.Handles.DrawWireDisc(walkingAnimalsPackController.anchor.position, Vector3.down, packMovementRange);
            UnityEditor.Handles.DrawWireDisc(walkingAnimalsPackController.anchor.position + new Vector3(0, shoalingMaxDepthSwimFromSpawnPoint * distanceScaleFactor * -1, 0), Vector3.down, packMovementRange);
            UnityEditor.Handles.DrawLine(walkingAnimalsPackController.anchor.position + new Vector3(packMovementRange, 0, 0), walkingAnimalsPackController.anchor.position + new Vector3(packMovementRange, shoalingMaxDepthSwimFromSpawnPoint * distanceScaleFactor * -1, 0));
            UnityEditor.Handles.DrawLine(walkingAnimalsPackController.anchor.position + new Vector3(packMovementRange * -1, 0, 0), walkingAnimalsPackController.anchor.position + new Vector3(packMovementRange * -1, shoalingMaxDepthSwimFromSpawnPoint * distanceScaleFactor * -1, 0));
            GUI.color = Color.white;
            UnityEditor.Handles.Label(walkingAnimalsPackController.anchor.position + (Vector3.left * packMovementRange), new GUIContent("Pack Movement Range"));
#endif
        }

        protected override void ShowPackMovementDistance()
        {
#if UNITY_EDITOR
            Handles.color = Color.blue;
            UnityEditor.Handles.DrawWireDisc(gameObject.transform.position, Vector3.down, packMovementRange);
            UnityEditor.Handles.DrawWireDisc(gameObject.transform.position + new Vector3(0, shoalingMaxDepthSwimFromSpawnPoint * distanceScaleFactor * -1, 0), Vector3.down, packMovementRange);
            UnityEditor.Handles.DrawLine(gameObject.transform.position + new Vector3(packMovementRange, 0, 0), gameObject.transform.position + new Vector3(packMovementRange, shoalingMaxDepthSwimFromSpawnPoint * distanceScaleFactor * -1, 0));
            UnityEditor.Handles.DrawLine(gameObject.transform.position + new Vector3(packMovementRange * -1, 0, 0), gameObject.transform.position + new Vector3(packMovementRange * -1, shoalingMaxDepthSwimFromSpawnPoint * distanceScaleFactor * -1, 0));
            GUI.color = Color.white;
            UnityEditor.Handles.Label(gameObject.transform.position + (Vector3.left * packMovementRange), new GUIContent("Pack Movement Distance"));
#endif
        }
    }
}

