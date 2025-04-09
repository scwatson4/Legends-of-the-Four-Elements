using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnythingWorld.Behaviour
{
    public class WalkingAnimalsPackController : AnythingPackController
    {
        WalkInPackGoal packLeaderWalkInPackGoal;


        public Transform anchor;

        private void Start()
        {
            anchor = this.transform;
        }
        public override void SetPackLeader(GameObject _packLeader)
        {
            base.SetPackLeader(_packLeader);
            packLeaderWalkInPackGoal = _packLeader.GetComponent<WalkInPackGoal>();
        }

        protected override void GoToCustomDestination()
        {

            if (packLeaderWalkInPackGoal)
            {
                packLeaderWalkInPackGoal.isCustomDestinationActive = true;
                packLeaderWalkInPackGoal.customDestination.x = packCustomDestinationX;
                packLeaderWalkInPackGoal.customDestination.z = packCustomDestinationZ;
                packLeaderWalkInPackGoal.customDestination.y = 0f;
            }
        }

        protected override void GoToRandomDestination()
        {
            if (packLeaderWalkInPackGoal)
            {
                packLeaderWalkInPackGoal.isCustomDestinationActive = false;
            }
        }
    }
}
