using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnythingWorld.Behaviour
{
    public class BirdsFlockingController : WalkingAnimalsPackController
    {
        BirdsFlockingGoal packLeaderBirdsFlockingGoal;

        public float packCustomDestinationY = 0f;
        public bool setPackRandomMovementMaxFlyingHeight = false;
        [Tooltip("PackRandomMovementMaxFlyingHeight value must be greater than zero.")]
        public float packRandomMovementMaxFlyingHeight = 0f;

        protected bool packRandomMovementMaxFlyingHeightAplied = false;

        private float defaultPackRandomMovementMaxFlyingHeight;
        private float packRandomMovementMaxFlyingHeightApliedValue = 0f;
        private float packNewCustomDestinationYValueApplied = 0f;

        public override void SetPackLeader(GameObject _packLeader)
        {
            base.SetPackLeader(_packLeader);
            packLeaderBirdsFlockingGoal = _packLeader.GetComponent<BirdsFlockingGoal>();
        }

        protected override void GoToCustomDestination()
        {
            if (packLeaderBirdsFlockingGoal)
            {
                packLeaderBirdsFlockingGoal.isCustomDestinationActive = true;
                packLeaderBirdsFlockingGoal.customDestination = new Vector3(packCustomDestinationX, packCustomDestinationY, packCustomDestinationZ);
                packNewCustomDestinationXValueApplied = packCustomDestinationX;
                packNewCustomDestinationYValueApplied = packCustomDestinationY;
                packNewCustomDestinationZValueApplied = packCustomDestinationZ;
            }
        }

        protected override void GoToRandomDestination()
        {
            if (packLeaderBirdsFlockingGoal)
            {
                packLeaderBirdsFlockingGoal.isCustomDestinationActive = false;
            }
        }

        protected override void Update()
        {
            if (showPackLeader)
            {
                if (!showingPackLeader)
                {
                    packLeader.GetComponent<BirdsFlockingGoal>().showPackLeaderFromCentralController = true;
                    showingPackLeader = true;
                }
            }
            else
            {
                if (showingPackLeader)
                {
                    packLeader.GetComponent<BirdsFlockingGoal>().showPackLeaderFromCentralController = false;
                    showingPackLeader = false;
                }
            }

            if (showPackFollowers)
            {
                if (!showingPackFollowers)
                {
                    packLeader.GetComponent<BirdsFlockingGoal>().ShowFollowersGizmos();
                    showingPackFollowers = true;
                }
            }
            else
            {
                if (showingPackFollowers)
                {
                    packLeader.GetComponent<BirdsFlockingGoal>().HideFollewersGizmos();
                    showingPackFollowers = false;
                }
            }

            if (showPackDestination)
            {
                if (!showingPackDestination)
                {
                    packLeader.GetComponent<BirdsFlockingGoal>().showPackDestinationFromCentralController = true;
                    showingPackDestination = true;
                }
            }
            else
            {
                if (showingPackDestination)
                {
                    packLeader.GetComponent<BirdsFlockingGoal>().showPackDestinationFromCentralController = false;
                    showingPackDestination = false;
                }
            }

            if (showPackRandomMovementRange)
            {
                if (!showingPackRandomMovementRange)
                {
                    packLeader.GetComponent<BirdsFlockingGoal>().showPackMovementRangeFromCentralController = true;
                    showingPackRandomMovementRange = true;
                }
            }
            else
            {
                if (showingPackRandomMovementRange)
                {
                    packLeader.GetComponent<BirdsFlockingGoal>().showPackMovementRangeFromCentralController = false;
                    showingPackRandomMovementRange = false;
                }
            }

            if (setPackRandomMovementDistanceRange)
            {
                if (packRandomMovementDistanceRangeApliedValue != packRandomMovementDistanceRange)
                {
                    packRandomMovementDistanceRangeAplied = false;
                }

                if (!packRandomMovementDistanceRangeAplied)
                {
                    if (packRandomMovementDistanceRange > 0f)
                    {
                        BirdsFlockingGoal packLeaderBirdsFlockingGoal = packLeader.GetComponent<BirdsFlockingGoal>();
                        defaultPackRandomMovementDistanceRange = packLeaderBirdsFlockingGoal.packMovementRange;
                        packLeaderBirdsFlockingGoal.packMovementRange = packRandomMovementDistanceRange;
                        packRandomMovementDistanceRangeApliedValue = packRandomMovementDistanceRange;
                        packRandomMovementDistanceRangeAplied = true;
                    }
                }
            }
            else
            {
                if (packRandomMovementDistanceRangeAplied)
                {
                    packLeader.GetComponent<BirdsFlockingGoal>().packMovementRange = defaultPackRandomMovementDistanceRange;
                    packRandomMovementDistanceRangeAplied = false;
                }
            }

            if (setPackCustomDestination)
            {
                if (packCustomDestinationX != packNewCustomDestinationXValueApplied ||
                    packCustomDestinationZ != packNewCustomDestinationZValueApplied ||
                    packCustomDestinationY != packNewCustomDestinationYValueApplied)
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

            if (setPackRandomMovementMaxFlyingHeight)
            {
                if (packRandomMovementMaxFlyingHeightApliedValue != packRandomMovementMaxFlyingHeight)
                {
                    packRandomMovementMaxFlyingHeightAplied = false;
                }

                if (!packRandomMovementMaxFlyingHeightAplied)
                {
                    if (packRandomMovementMaxFlyingHeight > 0)
                    {
                        GoToDestination packLeaderGoToDestinationBevaviour = packLeader.GetComponent<GoToDestination>();
                        defaultPackRandomMovementMaxFlyingHeight = packLeaderGoToDestinationBevaviour.maximumFlyingHeightOrDepthSwimFromSpawnPoint;
                        packLeaderGoToDestinationBevaviour.maximumFlyingHeightOrDepthSwimFromSpawnPoint = packRandomMovementMaxFlyingHeight;
                        BirdsFlockingGoal packLeaderBirdsFlockingGoal = packLeader.GetComponent<BirdsFlockingGoal>();
                        packLeaderBirdsFlockingGoal.flockingMaxFlyingHeightFromSpawnPoint = packRandomMovementMaxFlyingHeight;
                        packRandomMovementMaxFlyingHeightApliedValue = packRandomMovementMaxFlyingHeight;
                        packRandomMovementMaxFlyingHeightAplied = true;
                    }
                }
            }
            else
            {
                if (packRandomMovementMaxFlyingHeightAplied)
                {
                    packLeader.GetComponent<GoToDestination>().maximumFlyingHeightOrDepthSwimFromSpawnPoint = defaultPackRandomMovementMaxFlyingHeight;
                    packLeader.GetComponent<BirdsFlockingGoal>().flockingMaxFlyingHeightFromSpawnPoint = defaultPackRandomMovementMaxFlyingHeight;
                    packRandomMovementMaxFlyingHeightAplied = false;
                }
            }
        }
    }
}
