using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnythingWorld.Behaviour
{
    public class FishesShoalingController : WalkingAnimalsPackController
    {
        FishesShoalingGoal packLeaderFishesShoalingGoal;

        public float packCustomDestinationY = 0f;
        public bool setPackRandomMovementMaxDepthSwim = false;
        [Tooltip("PackRandomMovementMaxDepthSwim value must be greater than zero.")]
        public float packRandomMovementMaxDepthSwim = 0f;

        protected bool packRandomMovementMaxDepthSwimAplied = false;

        private float defaultRandomMovementPackMaxDepthSwim;
        private float packRandomMovementMaxDepthSwimApliedValue = 0f;
        private float packNewCustomDestinationYValueApplied = 0f;

        public void Start()
        {
            anchor = this.transform;
            anchor.position = new Vector3(0, 5, 0);
        }

        public override void SetPackLeader(GameObject _packLeader)
        {
            base.SetPackLeader(_packLeader);
            packLeaderFishesShoalingGoal = _packLeader.GetComponent<FishesShoalingGoal>();
        }

        protected override void GoToCustomDestination()
        {
            if (packLeaderFishesShoalingGoal)
            {
                packLeaderFishesShoalingGoal.isCustomDestinationActive = true;
                packLeaderFishesShoalingGoal.customDestination = new Vector3(packCustomDestinationX, packCustomDestinationY, packCustomDestinationZ);
                packNewCustomDestinationXValueApplied = packCustomDestinationX;
                packNewCustomDestinationYValueApplied = packCustomDestinationY;
                packNewCustomDestinationZValueApplied = packCustomDestinationZ;
            }
        }

        protected override void GoToRandomDestination()
        {
            if (packLeaderFishesShoalingGoal)
            {
                packLeaderFishesShoalingGoal.isCustomDestinationActive = false;
            }
        }

        protected override void Update()
        {
            if (showPackLeader)
            {
                if (!showingPackLeader)
                {
                    packLeader.GetComponent<FishesShoalingGoal>().showPackLeaderFromCentralController = true;
                    showingPackLeader = true;
                }
            }
            else
            {
                if (showingPackLeader)
                {
                    packLeader.GetComponent<FishesShoalingGoal>().showPackLeaderFromCentralController = false;
                    showingPackLeader = false;
                }
            }

            if (showPackFollowers)
            {
                if (!showingPackFollowers)
                {
                    packLeader.GetComponent<FishesShoalingGoal>().ShowFollowersGizmos();
                    showingPackFollowers = true;
                }
            }
            else
            {
                if (showingPackFollowers)
                {
                    packLeader.GetComponent<FishesShoalingGoal>().HideFollewersGizmos();
                    showingPackFollowers = false;
                }
            }

            if (showPackDestination)
            {
                if (!showingPackDestination)
                {
                    packLeader.GetComponent<FishesShoalingGoal>().showPackDestinationFromCentralController = true;
                    showingPackDestination = true;
                }
            }
            else
            {
                if (showingPackDestination)
                {
                    packLeader.GetComponent<FishesShoalingGoal>().showPackDestinationFromCentralController = false;
                    showingPackDestination = false;
                }
            }

            if (showPackRandomMovementRange)
            {
                if (!showingPackRandomMovementRange)
                {
                    packLeader.GetComponent<FishesShoalingGoal>().showPackMovementRangeFromCentralController = true;
                    showingPackRandomMovementRange = true;
                }
            }
            else
            {
                if (showingPackRandomMovementRange)
                {
                    packLeader.GetComponent<FishesShoalingGoal>().showPackMovementRangeFromCentralController = false;
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
                        FishesShoalingGoal packLeaderFishesShoalingGoal = packLeader.GetComponent<FishesShoalingGoal>();
                        defaultPackRandomMovementDistanceRange = packLeaderFishesShoalingGoal.packMovementRange;
                        packLeaderFishesShoalingGoal.packMovementRange = packRandomMovementDistanceRange;
                        packRandomMovementDistanceRangeApliedValue = packRandomMovementDistanceRange;
                        packRandomMovementDistanceRangeAplied = true;
                    }
                }
            }
            else
            {
                if (packRandomMovementDistanceRangeAplied)
                {
                    packLeader.GetComponent<FishesShoalingGoal>().packMovementRange = defaultPackRandomMovementDistanceRange;
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

            if (setPackRandomMovementMaxDepthSwim)
            {
                if (packRandomMovementMaxDepthSwimApliedValue != packRandomMovementMaxDepthSwim)
                {
                    packRandomMovementMaxDepthSwimAplied = false;
                }

                if (!packRandomMovementMaxDepthSwimAplied)
                {
                    if (packRandomMovementMaxDepthSwim > 0)
                    {
                        GoToDestination packLeaderGoToDestinationBevaviour = packLeader.GetComponent<GoToDestination>();
                        defaultRandomMovementPackMaxDepthSwim = packLeaderGoToDestinationBevaviour.maximumFlyingHeightOrDepthSwimFromSpawnPoint;
                        packLeaderGoToDestinationBevaviour.maximumFlyingHeightOrDepthSwimFromSpawnPoint = packRandomMovementMaxDepthSwim;
                        FishesShoalingGoal packLeaderFishesShoalingGoal = packLeader.GetComponent<FishesShoalingGoal>();
                        packLeaderFishesShoalingGoal.shoalingMaxDepthSwimFromSpawnPoint = packRandomMovementMaxDepthSwim;
                        packRandomMovementMaxDepthSwimApliedValue = packRandomMovementMaxDepthSwim;
                        packRandomMovementMaxDepthSwimAplied = true;
                    }
                }
            }
            else
            {
                if (packRandomMovementMaxDepthSwimAplied)
                {
                    packLeader.GetComponent<GoToDestination>().maximumFlyingHeightOrDepthSwimFromSpawnPoint = defaultRandomMovementPackMaxDepthSwim;
                    packLeader.GetComponent<FishesShoalingGoal>().shoalingMaxDepthSwimFromSpawnPoint = defaultRandomMovementPackMaxDepthSwim;
                    packRandomMovementMaxDepthSwimAplied = false;
                }
            }
        }
    }
}
