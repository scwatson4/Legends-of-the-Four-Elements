using AnythingWorld.Animation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AnythingWorld.Behaviour
{
    public class AirPlaneController : AnythingVehicleController
    {
        [Header("Engine Settings")]
        public float fowardAcceleration = 0.15f;
        public float upAndDownAcceleration = 15f;
        public float sideMovementAcceleration = 1f;
        public float pitchMovementAcceleration = 1f;
        public float yawMovementAcceleration = 0.05f;
        public float rollMovementAcceleration = 1f;

        [Header("Movement Limits Settings")]
        public float pitchMovementLimitFactor = 0.06f;
        public float rollMovementLimitFactor = 0.04f;

        [Header("Physics Influences Settings")]
        public float movementFriction = 1f;
        public float angularFriction = 4f;
        

        private Rigidbody vehicleRigidBody;
        private Vector3 acceleration;
        private Vector3 angularAcceleration;
        private float engineThrust;
        private float gravityInfluence;
        private PropellorVehicleAnimator aiplanePropellerAnimator;


        protected override void Awake()
        {
            base.Awake();
            vehicleRigidBody = gameObject.AddComponent<Rigidbody>();
            vehicleRigidBody.useGravity = false;
        }

        private void Start()
        {
            //Try to get air plane propeller animator
            TryGetComponent<PropellorVehicleAnimator>(out aiplanePropellerAnimator);
        }

        protected override void Update()
        {
            base.Update();

            if (!controlThisVehicle) return;
            HandleInputs();
        }

        private void FixedUpdate()
        {
            if (!controlThisVehicle) return;

            var velocity = (vehicleRigidBody.linearVelocity) + (acceleration * Time.fixedDeltaTime);
            vehicleRigidBody.linearVelocity = (velocity * Time.fixedDeltaTime + (acceleration * 0.5f * Time.fixedDeltaTime * Time.fixedDeltaTime)) / Time.fixedDeltaTime;


            var angularVelocity = (vehicleRigidBody.angularVelocity) + (angularAcceleration * Time.fixedDeltaTime);
            vehicleRigidBody.angularVelocity = (angularVelocity * Time.fixedDeltaTime + (angularAcceleration * 0.5f * Time.fixedDeltaTime * Time.fixedDeltaTime)) / Time.fixedDeltaTime;
        }


        private void HandleInputs()
        {
            //Air plane controlls
            //Space Bar = increase engine thrust (which moves the air plane forward)
            //Left Shift = decrease engine thrust
            //W or Up Arrow = Lean the air plane towards the ground (making it loses altitude)
            //S or Down Arrow = Lean the air plane towards the sky (making it gain altitude)
            //A or Left Arrow = Lean the air plane to the left (making it move to the left direction)
            //D or Right Arrow = Lean the air plane to the right (making it move to the right direction)

            acceleration = Vector3.zero;
            angularAcceleration = Vector3.zero;

            //Calculate the foward "velocity" in local axis 
            var forwardInfluence = Vector3.Dot(vehicleRigidBody.linearVelocity, transform.forward);
            
            //If the airplane is moving fast the gravity influence goes lower (and vice versa)
            gravityInfluence = Mathf.Max(0, 20f - forwardInfluence * .2f);
            acceleration.y -= gravityInfluence;

            //Pressing Space the airplane engine increments its power
            if (Input.GetKey(KeyCode.Space))
            {
                //And it will create foward acceleration 
                engineThrust += fowardAcceleration;

                //And it will create a up acceleration if the airplane is fast enough
                var upInfluence = Mathf.Clamp(-50f + forwardInfluence, 0, 10);
                acceleration += transform.up * upInfluence;

                //And make propeller (if exists) increase its movement
                if (aiplanePropellerAnimator)
                {
                    aiplanePropellerAnimator.Accelerate();
                }
            }
            //Pressing Left Shift the airplane engine decrements its power
            else if (Input.GetKey(KeyCode.LeftShift))
            {
                //And the foward acceleration will decrease
                engineThrust -= fowardAcceleration;

                //And make propeller (if exists) decrease its movement
                if (aiplanePropellerAnimator)
                {
                    aiplanePropellerAnimator.Decelerate();
                }
            }

            //Create Limits and Apply the angine power.
            engineThrust = Mathf.Clamp(engineThrust, 0f, 100f);
            acceleration += transform.forward * engineThrust;

            Vector3 normalizedAngles = gameObject.transform.rotation.eulerAngles;
            if (normalizedAngles.x > 180) normalizedAngles.x -= 360;
            if (normalizedAngles.z > 180) normalizedAngles.z -= 360;

            //This is the movement friction. It applies for foward/backward and up/down movements
            acceleration -= vehicleRigidBody.linearVelocity * movementFriction;

            //This controls the lean movement (Pitch Movement)
            angularAcceleration += transform.right * Input.GetAxis("Vertical") * pitchMovementAcceleration;

            //This is a limitation for lean movement (as much as the airplane is leaned a contrary force will be applied)
            angularAcceleration -= transform.right * normalizedAngles.x * pitchMovementLimitFactor * pitchMovementAcceleration;

            //This is the angular friction. It applies for all angular movement (Pitch, Yaw and Roll).
            angularAcceleration -= vehicleRigidBody.angularVelocity * angularFriction;

            //As much as the helicopter is sides leaned it will move right or left and it will move up also
            acceleration += gameObject.transform.right * normalizedAngles.z * -sideMovementAcceleration;
            angularAcceleration += gameObject.transform.up * normalizedAngles.z * -yawMovementAcceleration;

            //This controls the side inclination (Roll Movement)
            angularAcceleration += transform.forward * -Input.GetAxis("Horizontal") * rollMovementAcceleration;

            //This is a limitation for roll movement (as much as the airplane is sided leaned a contrary force will be applied)
            angularAcceleration -= transform.forward * normalizedAngles.z * rollMovementLimitFactor * rollMovementAcceleration;
        }
    }
}
