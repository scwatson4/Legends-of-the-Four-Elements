using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnythingWorld.Behaviour
{
    public abstract class AnythingPackController : MonoBehaviour
    {
        [Header("Gizmos Controller")]
        public bool showPackLeader = false;
        public bool showPackFollowers = false;
        public bool showPackDestination = false;
        public bool showPackRandomMovementRange = false;

        [Header("Pack Settings")]
        public string packModelEntity;
        public GameObject packLeader;
        public bool setPackRandomMovementDistanceRange = false;
        [Tooltip("PackRandomMovementRange value must be greater than zero.")]
        public float packRandomMovementDistanceRange = 0f;
        public bool setPackCustomDestination = false;
        public float packCustomDestinationX = 0f;
        public float packCustomDestinationZ = 0f;

        protected bool goingToCustomDestination = false;
        protected bool showingPackLeader = false;
        protected bool showingPackFollowers = false;
        protected bool showingPackDestination = false;
        protected bool showingPackRandomMovementRange = false;
        protected bool packRandomMovementDistanceRangeAplied = false;
        protected float defaultPackRandomMovementDistanceRange;
        protected float packRandomMovementDistanceRangeApliedValue = 0f;
        protected float packNewCustomDestinationXValueApplied = 0f;
        protected float packNewCustomDestinationZValueApplied = 0f;


        //Static variables to control when the central pack controller should be spawned and destroyed
        public static GameObject packsCentralController;
        public static List<AnythingPackController> allPacksControllers = new List<AnythingPackController>(); 

        public virtual void SetPackLeader(GameObject _packLeader)
        {
            packLeader = _packLeader;
        }

        public static void SearchForCurrentPackLeaderOrSetNewPackLeader(WalkInPackGoal requesterWalkInPackComponent, string requesterModelEntity)
        {
            foreach (var controller in allPacksControllers)
            {
                if (controller.packModelEntity == requesterModelEntity)
                {
                    if (controller.packLeader)
                    {
                        WalkInPackGoal leadeWalkInPackComponent = controller.packLeader.gameObject.GetComponent<WalkInPackGoal>();
                        
                        //If already exists a pack leader and the pack leader is not been destroyed, the requester will follow the leader
                        if (leadeWalkInPackComponent && leadeWalkInPackComponent.isPossibleLeader)
                        {
                            requesterWalkInPackComponent.packLeader = controller.packLeader;
                            leadeWalkInPackComponent.followers.Add(requesterWalkInPackComponent.gameObject);

                            //Show Gizmos of Pack Follower
                            if (leadeWalkInPackComponent.walkingAnimalsPackController.showPackFollowers == true)
                            {
                                requesterWalkInPackComponent.showPackFollowersFromCentralController = true;
                            }
                            else
                            {
                                requesterWalkInPackComponent.showPackFollowersFromCentralController = false;
                            }

                            requesterWalkInPackComponent.goToDestinationBehaviourComponent.SetMyDestination(leadeWalkInPackComponent.leaderDestination);
                            return;
                        }
                        //If already exists a pack leader but the pack leader is been destroyed, the first requester to reach here will become the pack leader
                        else if (leadeWalkInPackComponent && !leadeWalkInPackComponent.isPossibleLeader)
                        {
                            requesterWalkInPackComponent.packLeader = requesterWalkInPackComponent.gameObject;
                            controller.SetPackLeader(requesterWalkInPackComponent.gameObject);
                        }
                    }
                }
            }

            //If there's no previous pack leader, the first requester to reach here will become the pack leader
            requesterWalkInPackComponent.packLeader = requesterWalkInPackComponent.gameObject;
        }

        
        protected virtual void Update()
        {
            if(showPackLeader)
            {
                if(!showingPackLeader)
                {
                    packLeader.GetComponent<WalkInPackGoal>().showPackLeaderFromCentralController = true;
                    showingPackLeader = true;
                }
            }
            else
            {
                if(showingPackLeader)
                {
                    packLeader.GetComponent<WalkInPackGoal>().showPackLeaderFromCentralController = false;
                    showingPackLeader = false;
                }
            }

            if (showPackFollowers)
            {
                if (!showingPackFollowers)
                {
                    packLeader.GetComponent<WalkInPackGoal>().ShowFollowersGizmos();
                    showingPackFollowers = true;
                }
            }
            else
            {
                if (showingPackFollowers)
                {
                    packLeader.GetComponent<WalkInPackGoal>().HideFollewersGizmos();
                    showingPackFollowers = false;
                }
            }

            if (showPackDestination)
            {
                if (!showingPackDestination)
                {
                    packLeader.GetComponent<WalkInPackGoal>().showPackDestinationFromCentralController = true;
                    showingPackDestination = true;
                }
            }
            else
            {
                if (showingPackDestination)
                {
                    packLeader.GetComponent<WalkInPackGoal>().showPackDestinationFromCentralController = false;
                    showingPackDestination = false;
                }
            }

            if(showPackRandomMovementRange)
            {
                if (!showingPackRandomMovementRange)
                {
                    packLeader.GetComponent<WalkInPackGoal>().showPackMovementRangeFromCentralController = true;
                    showingPackRandomMovementRange = true;
                }
            }
            else
            {
                if (showingPackRandomMovementRange)
                {
                    packLeader.GetComponent<WalkInPackGoal>().showPackMovementRangeFromCentralController = false;
                    showingPackRandomMovementRange = false;
                }
            }

            if (setPackRandomMovementDistanceRange)
            {
                if(packRandomMovementDistanceRangeApliedValue != packRandomMovementDistanceRange)
                {
                    packRandomMovementDistanceRangeAplied = false;
                }

                if (!packRandomMovementDistanceRangeAplied)
                {
                    if(packRandomMovementDistanceRange > 0f)
                    {
                        WalkInPackGoal packLeaderWalkInPackGoal = packLeader.GetComponent<WalkInPackGoal>();
                        defaultPackRandomMovementDistanceRange = packLeaderWalkInPackGoal.packMovementRange;
                        packLeaderWalkInPackGoal.packMovementRange = packRandomMovementDistanceRange;
                        packRandomMovementDistanceRangeApliedValue = packRandomMovementDistanceRange;
                        packRandomMovementDistanceRangeAplied = true;
                    }
                }
            }
            else
            {
                if (packRandomMovementDistanceRangeAplied)
                {
                    packLeader.GetComponent<WalkInPackGoal>().packMovementRange = defaultPackRandomMovementDistanceRange;
                    packRandomMovementDistanceRangeAplied = false;
                }
            }

            if (setPackCustomDestination)
            {
                if(packCustomDestinationX != packNewCustomDestinationXValueApplied ||
                    packCustomDestinationZ != packNewCustomDestinationZValueApplied)
                {
                    goingToCustomDestination = false;
                }

                if (!goingToCustomDestination)
                {
                    GoToCustomDestination();
                    goingToCustomDestination = true;
                }
            }
            else
            {
                if (goingToCustomDestination)
                {
                    GoToRandomDestination();
                    goingToCustomDestination = false;
                }
            }

        }

        public void ResetGizmos()
        {
            showingPackLeader = false;
            showingPackFollowers = false;
            showingPackDestination = false;
            showingPackRandomMovementRange = false;
        }

        protected virtual void GoToCustomDestination() { }

        protected virtual void GoToRandomDestination() { }
    }
}
