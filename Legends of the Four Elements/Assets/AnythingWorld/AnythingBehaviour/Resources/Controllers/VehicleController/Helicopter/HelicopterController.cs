using AnythingWorld.Animation;
using UnityEngine;

namespace AnythingWorld.Behaviour
{
    public class HelicopterController : AnythingVehicleController
    {
        [Header("Engine Settings")]
        public float fowardAcceleration = 12f;
        public float upAndDownAcceleration = 10f;
        public float sideMovementAcceleration = 2f;
        public float pitchMovementAcceleration = 1f;
        public float yawMovementAcceleration = 1f;
        public float rollMovementAcceleration = 1f;

        [Header("Movement Limits Settings")]
        public float pitchMovementLimitFactor = 0.06f;
        public float rollMovementLimitFactor = 0.04f;

        [Header("Physics Influences Settings")]
        public float movementFriction = 2f;
        public float angularFriction = 4f;
        public float gravityInfluence = 1.5f;

        private Rigidbody vehicleRigidBody;
        private Vector3 acceleration;
        private Vector3 angularAcceleration;
        private PropellorVehicleAnimator helicopterAnimator;


        protected override void Awake()
        {
            base.Awake();
            vehicleRigidBody = gameObject.AddComponent<Rigidbody>();
            vehicleRigidBody.useGravity = false;
        }

        private void Start()
        {
            TryGetComponent<PropellorVehicleAnimator>(out helicopterAnimator);
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
            //Helicopter Controls:
            //W = Up (Gain altitude)
            //S = Down (Lose altitude)
            //Up Arrow = Lean and move foward (pitch movement)
            //Down Arrow = Lean and move backward (pitch movement)
            //A = Lean and move to the right (roll movement)
            //D - Lean and move to the left (roll movement)
            //Left Arrow = Rotate left (yaw movement)
            //Right Arrow = Rotate right (yaw movement)

            acceleration = Vector3.zero;
            angularAcceleration = Vector3.zero;

            acceleration.y -= gravityInfluence;

            //This controls Up and Down Movements
            if (Input.GetKey(KeyCode.W))
            {
                acceleration.y += upAndDownAcceleration;
                
                if(helicopterAnimator)
                {
                    helicopterAnimator.Accelerate();
                }
            }
            else if (Input.GetKey(KeyCode.S))
            {
                acceleration.y -= upAndDownAcceleration;
                
                if (helicopterAnimator)
                {
                    helicopterAnimator.Decelerate();
                }
            }

            //This controls the lean and forward/backward movement (Pitch Movement)
            if (Input.GetKey(KeyCode.UpArrow))
            {
                angularAcceleration += transform.right * pitchMovementAcceleration;
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                angularAcceleration -= transform.right * pitchMovementAcceleration;
            }

            //This controls the rotation to the right/left movement (Yaw Movement) 
            if (Input.GetKey(KeyCode.RightArrow))
            {
                angularAcceleration += transform.up * yawMovementAcceleration;
            }
            else if (Input.GetKey(KeyCode.LeftArrow))
            {
                angularAcceleration -= transform.up * yawMovementAcceleration;
            }

            //This controls the lean and right/left movement (Roll Movement)
            if (Input.GetKey(KeyCode.D))
            {
                angularAcceleration -= transform.forward * rollMovementAcceleration;
            }
            else if (Input.GetKey(KeyCode.A))
            {
                angularAcceleration += transform.forward * rollMovementAcceleration;
            }

            Vector3 normalizedAngles = gameObject.transform.rotation.eulerAngles;
            if (normalizedAngles.x > 180) normalizedAngles.x -= 360;
            if (normalizedAngles.z > 180) normalizedAngles.z -= 360;

            //As much as the Helicopter is leaned foward it will gain speed
            acceleration += gameObject.transform.forward * normalizedAngles.x * fowardAcceleration;

            //This is the movement friction. It applies for foward/backward and up/down movements
            acceleration -= vehicleRigidBody.linearVelocity * movementFriction;
            
            //This is a limitation for lean movement (as much as the helicopter is leaned a contrary force will be applied)
            angularAcceleration -= transform.right * normalizedAngles.x * pitchMovementLimitFactor * pitchMovementAcceleration;
            
            //This is the angular friction. It applies for all angular movement (Pitch, Yaw and Roll).
            angularAcceleration -= vehicleRigidBody.angularVelocity * angularFriction;

            //As much as the helicopter is sides leaned it will move right or left 
            acceleration += gameObject.transform.right * normalizedAngles.z * - sideMovementAcceleration;

            //This is a limitation for roll movement (as much as the helicopter is sided leaned a contrary force will be applied)
            angularAcceleration -= transform.forward * normalizedAngles.z * rollMovementLimitFactor * rollMovementAcceleration;
        }
    }
}
