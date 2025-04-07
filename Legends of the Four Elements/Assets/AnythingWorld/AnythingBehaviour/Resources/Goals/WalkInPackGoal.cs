using AnythingWorld.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AnythingWorld.Behaviour
{
    public class WalkInPackGoal : AnythingCharacterGoal
    {
        [Header("Gizmos Controller")]
        [Tooltip("Show the perimeter in where a pack can move. On Runtime, the actually pack movement radius and destination is only defined by the pack leader")]
        public bool showPackMovementDistance;

        [Header("Pack Settings")]
        [Tooltip("Set the perimeter radius in where a pack can move. On Runtime, the actually pack movement radius and destination is only defined by the pack leader.")]
        public float packMovementRange = 20f;

        [HideInInspector]
        public List<GameObject> followers = new List<GameObject>();
        [HideInInspector]
        public bool isCustomDestinationActive = false;
        [HideInInspector]
        public bool showPackLeaderFromCentralController = false;
        [HideInInspector]
        public bool showPackFollowersFromCentralController = false;
        [HideInInspector]
        public bool showPackDestinationFromCentralController = false;
        [HideInInspector]
        public bool showPackMovementRangeFromCentralController = false;
        [HideInInspector]
        public Vector3 customDestination = Vector3.zero;
        [HideInInspector]
        public GameObject packLeader;
        [HideInInspector]
        public WalkingAnimalsPackController walkingAnimalsPackController;
        [HideInInspector]
        public  GoToDestination goToDestinationBehaviourComponent = null;
        [HideInInspector]
        public Vector3 leaderDestination;
        [HideInInspector]
        public bool isPossibleLeader = true;

        protected Vector3 followerDestination;
        protected Vector3 spawnPosition;
        protected bool leaderIsSettingMyFirstPosition = true;
        protected string myModelEntity;
        protected float packFollowerDestinationOffset = 10f; //@Hardcoded

        private void Awake()
        {
            //Its not expected that an animal already has GoToDestination component when it is created
            //because GoToDestination component is created when the goal is executed (ExecuteGoal)
            //but if we duplicate an animal instead of creating a new one it will have GoTo Destination component since its start
            //and we don't want because it will generates unexpected movements 
            GoToDestination goToDestinationOnSpawn = GetComponent<GoToDestination>();
            if(goToDestinationOnSpawn)
            {
                Destroy(goToDestinationOnSpawn);
            }
        }

        protected override void Start()
        {
            base.Start();
            spawnPosition = transform.position;
        }
        
        public override void ExecuteGoal()
        {
            if (!goToDestinationBehaviourComponent)
            {
                isPossibleLeader = true;
                myModelEntity = GetComponent<ModelDataInspector>()?.entity;
                goToDestinationBehaviourComponent = gameObject.AddComponent<GoToDestination>();
                goToDestinationBehaviourComponent.onDestinationReached += SetPackNewDestination;
                goToDestinationBehaviourComponent.isWalkingInPack = true;
                SearchForCurrentPackLeaderOrDetectNewLeader();

                if (packLeader == gameObject)
                {
                    StartCoroutine(SetPackDestinationFirstTime());
                }
            }
        }

        private void SearchForCurrentPackLeaderOrDetectNewLeader()
        {
            //The first animal to run this function will become the packLeader
            //If an animal is not the pack leader it will follow the leader
            AnythingPackController.SearchForCurrentPackLeaderOrSetNewPackLeader(this, myModelEntity);

            if (packLeader == gameObject)
            {
                //The pack leader creates the Pack Controller 
                SpawnPackController();
            }
        }

        IEnumerator SetPackDestinationFirstTime()
        {
            //Only Pack Leader can set the Pack Destination.
            //A Coroutine was used here because in the firt time the leader has to set the pack destination
            //we need to wait until all non leaders subscribe as followers in the leader script 
            yield return new WaitForSeconds(0.2f);
            SetPackNewDestination();
        }

        protected virtual void SetPackNewDestination()
        {
            //Only Pack Leader sets the Pack Destination.
            if (packLeader == gameObject)
            {
                if(goToDestinationBehaviourComponent)
                {
                    if(isCustomDestinationActive)
                    {
                        leaderDestination.x = customDestination.x;
                        leaderDestination.z = customDestination.z;
                        leaderDestination.y = gameObject.transform.position.y;
                    }
                    else
                    {
                        leaderDestination = goToDestinationBehaviourComponent.GetARandomDestinationInsideAPerimeter(spawnPosition, packMovementRange);
                    }
                    goToDestinationBehaviourComponent.SetMyDestination(leaderDestination);
                    foreach(var member in followers)
                    {
                        Vector3 memberDestination = new Vector3(Random.Range(0, packFollowerDestinationOffset), gameObject.transform.position.y, Random.Range(0, packFollowerDestinationOffset)) + leaderDestination;
                        WalkInPackGoal memberWalkInPackGoalComponent = member.GetComponent<WalkInPackGoal>();
                        if(memberWalkInPackGoalComponent)
                        {
                            memberWalkInPackGoalComponent.followerDestination = memberDestination;
                            if (memberWalkInPackGoalComponent.leaderIsSettingMyFirstPosition)
                            {
                                member.GetComponent<GoToDestination>().SetMyDestination(memberDestination);
                                memberWalkInPackGoalComponent.leaderIsSettingMyFirstPosition = false;
                            }
                        }
                    }
                }
                
            }
            else
            {
                if(goToDestinationBehaviourComponent.GetMyDestination() != followerDestination)
                {
                    goToDestinationBehaviourComponent.SetMyDestination(followerDestination);
                }
            }
        }

        protected virtual void SpawnPackController()
        {
            bool alreadyLeader = false;

            if(!AnythingPackController.packsCentralController)
            {
                GameObject packController = new GameObject("PacksCentralController");
                AnythingPackController.packsCentralController = packController;
            }

            foreach(var controller in AnythingPackController.allPacksControllers)
            {
                if(controller.packLeader == gameObject)
                {
                    walkingAnimalsPackController = controller as WalkingAnimalsPackController;
                    walkingAnimalsPackController.ResetGizmos();
                    alreadyLeader = true;
                }
            }
            
            if(!alreadyLeader)
            {
                walkingAnimalsPackController = AnythingPackController.packsCentralController.AddComponent<WalkingAnimalsPackController>();
                AnythingPackController.allPacksControllers.Add(walkingAnimalsPackController);
                walkingAnimalsPackController.SetPackLeader(gameObject);
                walkingAnimalsPackController.packModelEntity = this.myModelEntity;
            }
        }

        public override float UpdatePriority(float priority)
        {
            return priority;
        }

        public override void TerminateGoalExecution()
        {
            if (packLeader == gameObject)
            {
                DeactivatePackLeader();
            }
            else
            {
                DeactivatePackFollower();
            }

            if (goToDestinationBehaviourComponent)
            {
                goToDestinationBehaviourComponent.isWalkingInPack = false;
                goToDestinationBehaviourComponent.onDestinationReached -= SetPackNewDestination;
                Destroy(goToDestinationBehaviourComponent);
            }
        }

        public void ShowFollowersGizmos()
        {
            foreach (var follower in followers)
            {
                WalkInPackGoal followerWalkInPackGoal = follower.GetComponent<WalkInPackGoal>();
                if(followerWalkInPackGoal)
                {
                    followerWalkInPackGoal.showPackFollowersFromCentralController = true;
                }

            }
        }

        public void HideFollewersGizmos()
        {
            foreach (var follower in followers)
            {
                WalkInPackGoal followerWalkInPackGoal = follower.GetComponent<WalkInPackGoal>();
                if (followerWalkInPackGoal)
                {
                    followerWalkInPackGoal.showPackFollowersFromCentralController = false;
                }

            }
        }

        protected virtual void ShowPackMovementRange()
        {
#if UNITY_EDITOR
            Handles.color = Color.blue;
            UnityEditor.Handles.DrawWireDisc(spawnPosition, Vector3.up, packMovementRange);
            GUI.color = Color.white;
            UnityEditor.Handles.Label(spawnPosition + (Vector3.left * packMovementRange), new GUIContent("Pack Movement Range"));
#endif
        }

        private void OnDestroy()
        {
            //If the gameobject is been destroyed by scene destroy don't do anything
            if (!gameObject.scene.isLoaded) {return;}

            if (packLeader == gameObject)
            {
                DeactivatePackLeader();
            }
            else
            {
                DeactivatePackFollower();
            }
        }

        protected virtual void DeactivatePackLeader()
        {
            showPackLeaderFromCentralController = false;
            showPackFollowersFromCentralController = false;
            showPackDestinationFromCentralController = false;
            showPackMovementRangeFromCentralController = false;
            isPossibleLeader = false;
           
            if (followers.Count <= 0)
            {
                AnythingPackController.allPacksControllers.Remove(walkingAnimalsPackController);
                Destroy(walkingAnimalsPackController);
                if(AnythingPackController.allPacksControllers.Count <= 0)
                {
                    Destroy(AnythingPackController.packsCentralController);
                }
            }
            MakeFollowersChooseAnotherPackLeader();
            followers.Clear();
            packLeader = null;
        }

        protected virtual void DeactivatePackFollower()
        {
            isPossibleLeader = false;
            showPackFollowersFromCentralController = false;
            packLeader.GetComponent<WalkInPackGoal>().followers.Remove(gameObject);
            packLeader = null;
            leaderIsSettingMyFirstPosition = true;
        }


        protected virtual void MakeFollowersChooseAnotherPackLeader()

        {
            foreach(var follower in followers)
            {
                WalkInPackGoal followerWalkInPackGoal = follower.GetComponent<WalkInPackGoal>();
                if(followerWalkInPackGoal)
                {
                    followerWalkInPackGoal.showPackFollowersFromCentralController = false;
                    followerWalkInPackGoal.SearchForCurrentPackLeaderOrDetectNewLeader();
                }
                
            }
        }

        private void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR

            if(!Application.isPlaying)
            {
                if (showPackMovementDistance)
                {
                    ShowPackMovementDistance();
                }
            }
            else
            {
                if (showPackMovementDistance && packLeader)
                {
                    if (gameObject == packLeader)
                    {
                        ShowPackMovementRange();
                    }
                    else
                    {
                        packLeader.GetComponent<WalkInPackGoal>().ShowPackMovementRange();
                    }
                }
            }
#endif
        }

        protected virtual void ShowPackMovementDistance()
        {
#if UNITY_EDITOR
            Handles.color = Color.blue;
            UnityEditor.Handles.DrawWireDisc(gameObject.transform.position, Vector3.up, packMovementRange);
            GUI.color = Color.white;
            UnityEditor.Handles.Label(gameObject.transform.position + (Vector3.left * packMovementRange), new GUIContent("Pack Movement Distance"));
#endif
        }

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR

            if (gameObject == packLeader)
            {
                if (showPackLeaderFromCentralController)
                {
                    Handles.color = Color.red;
                    UnityEditor.Handles.DrawWireDisc(gameObject.transform.position, Vector3.up, 1f);
                    GUI.color = Color.white;
                    UnityEditor.Handles.Label(gameObject.transform.position + (Vector3.left * 2f), new GUIContent("Pack Leader"));
                }

                if (showPackDestinationFromCentralController)
                {
                    Handles.color = Color.yellow;
                    float goalRadius = goToDestinationBehaviourComponent.goalRadius;
                    UnityEditor.Handles.DrawWireDisc(leaderDestination, Vector3.up, goalRadius);
                    GUI.color = Color.white;
                    UnityEditor.Handles.Label(leaderDestination + (Vector3.left * 2f), new GUIContent("Pack Destination"));
                }

                if (showPackMovementRangeFromCentralController)
                {
                    ShowPackMovementRange();
                }
            }
            else
            {
                if (showPackFollowersFromCentralController)
                {
                    Handles.color = Color.blue;
                    UnityEditor.Handles.DrawWireDisc(gameObject.transform.position, Vector3.up, 1f);
                    GUI.color = Color.white;
                    UnityEditor.Handles.Label(gameObject.transform.position + (Vector3.left * 2f), new GUIContent("Pack Follower"));
                }
            }
#endif
        }
    }
}
