using AnythingWorld.Animation.Vehicles;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnythingWorld.Animation
{
    public class WheeledVehicleControllerScript : MonoBehaviour
    {        
        //Position variables
        private Vector3 goalPosition;
        private Vector3 directionToGoal;
        /// <summary>
        /// Multiplied by velocity in animation, used to scale root movement in comparison to animation.
        /// </summary>
        public float movementScalar = 1;
        public float turnSpeed = 10;
        
        public bool controlThisVehicle = true;
        public bool rootMovement = true;
        private VehicleAnimator animator;
        // Start is called before the first frame update
        void Start()
        {
            TryGetComponent<VehicleAnimator>(out animator);
        }

        // Update is called once per frame
        void Update()
        {
            if (controlThisVehicle && animator!=null)
            {
                directionToGoal = new Vector3(goalPosition.x, transform.position.y, goalPosition.z) - transform.position;


                //if(rootMovement) VehicleRootMotion.MoveForward(transform, animator.velocity, movementScalar);
                //if(rootMovement) VehicleRootMotion.Rotate(transform,animator.wheelYRotation, animator.velocity, turnSpeed);
            }
        }
    }
}
