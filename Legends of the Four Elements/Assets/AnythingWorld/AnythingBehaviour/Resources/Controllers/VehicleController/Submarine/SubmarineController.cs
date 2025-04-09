using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnythingWorld.Behaviour
{
    public class SubmarineController : AnythingVehicleController
    {
        [Header("Water Surface Location in Y axis")]
        [Tooltip("This variable is responsible to prevent that submarine move above the water surface.")]
        public float waterSurfaceLocatedInYAxis = 0f;

        [Header("Engine Settings")]
        public float fowardAcceleration = 30f;
        public float upAndDownAcceleration = 40f;
        public float sideMovementAcceleration = 2f;
        public float pitchMovementAcceleration = 2f;
        public float yawMovementAcceleration = 1f;

        [Header("Movement Limits Settings")]
        public float pitchMovementLimitFactor = 0.06f;
        public float waterSurfaceMaxEmergeAmount = 5f;

        [Header("Physics Influences Settings")]
        public float movementFriction = 2f;
        public float angularFriction = 4f;

        private Rigidbody vehicleRigidBody;
        private Vector3 acceleration;
        private Vector3 angularAcceleration;
        private Bounds submarineBounds;
        private float submarineTransformYAndMeshCenterDifference;
        private float waterSurfaceRepelAmount = 40f;
        private float waterSurfaceRepelMultiplier;
        private float submarineMaximumElevation;


        protected override void Awake()
        {
            base.Awake();
            vehicleRigidBody = gameObject.AddComponent<Rigidbody>();
            vehicleRigidBody.useGravity = false;
            SetSubmarineInitialPosition();
        }
       
        private void SetSubmarineInitialPosition()
        {
            submarineBounds = transform.GetComponentInChildren<MeshRenderer>().bounds;
            submarineTransformYAndMeshCenterDifference = transform.position.y - submarineBounds.center.y;
            submarineMaximumElevation = waterSurfaceLocatedInYAxis + submarineTransformYAndMeshCenterDifference + waterSurfaceMaxEmergeAmount;
            transform.position = new Vector3(transform.position.x, submarineMaximumElevation, transform.position.z);
        }

    protected override void Update()
        {
            base.Update();
            if (!controlThisVehicle) return;
            HandleInputs();
        }

        private void FixedUpdate()
        {
            //This controls the water surface repel force that prevents the submarine to go above water
            waterSurfaceRepelMultiplier = Mathf.Clamp01((transform.position.y - submarineMaximumElevation) / waterSurfaceMaxEmergeAmount) * waterSurfaceRepelAmount;
            acceleration.y -= waterSurfaceRepelMultiplier;

            if (!controlThisVehicle) return;

            //This controls the forces aplyed on the submarine by the player imputs 

            var velocity = (vehicleRigidBody.linearVelocity) + (acceleration * Time.fixedDeltaTime);
            vehicleRigidBody.linearVelocity = (velocity * Time.fixedDeltaTime + (acceleration * 0.5f * Time.fixedDeltaTime * Time.fixedDeltaTime)) / Time.fixedDeltaTime;


            var angularVelocity = (vehicleRigidBody.angularVelocity) + (angularAcceleration * Time.fixedDeltaTime);
            vehicleRigidBody.angularVelocity = (angularVelocity * Time.fixedDeltaTime + (angularAcceleration * 0.5f * Time.fixedDeltaTime * Time.fixedDeltaTime)) / Time.fixedDeltaTime;
        }

        private void HandleInputs()
        {
            //Submarine Controls 
            // W = Emerge
            // S = Submerge
            // Up Arrow = Forward Movement
            // Down Arrow = Backward Movement
            // A or Left Arrow = Left Movement
            // D or Right Arror
            //The submarine only can turn if it is been moving forward/backward

            acceleration = Vector3.zero;
            angularAcceleration = Vector3.zero;

            if (Input.GetKey(KeyCode.W))
            {
                if(transform.position.y <= submarineMaximumElevation)
                {
                    acceleration.y += upAndDownAcceleration;
                    angularAcceleration -= transform.right * pitchMovementAcceleration;
                }
            }
            else if (Input.GetKey(KeyCode.S))
            {
                acceleration.y -= upAndDownAcceleration;
                angularAcceleration += transform.right * pitchMovementAcceleration;
            }

            if (Input.GetKey(KeyCode.UpArrow))
            {
                acceleration += gameObject.transform.forward * fowardAcceleration;
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                acceleration -= gameObject.transform.forward * fowardAcceleration;
            }

            Vector3 normalizedAngles = gameObject.transform.rotation.eulerAngles;
            if (normalizedAngles.x > 180) normalizedAngles.x -= 360;

            //This is the movement friction. It applies for foward/backward and up/down movements
            acceleration -= vehicleRigidBody.linearVelocity * movementFriction;

            //This is a limitation for lean movement (as much as the boat is leaned a contrary force will be applied)
            angularAcceleration -= transform.right * normalizedAngles.x * pitchMovementLimitFactor * pitchMovementAcceleration;

            //This is the angular friction. It applies for all angular movement (Pitch, Yaw and Roll).
            angularAcceleration -= vehicleRigidBody.angularVelocity * angularFriction;

            //This controls the side movement (Yam Movement)
            if (Vector3.Dot(vehicleRigidBody.linearVelocity, transform.forward) >= 0.2f)
            {
                angularAcceleration += Vector3.up * Input.GetAxis("Horizontal") * yawMovementAcceleration;
            }
            else if(Vector3.Dot(vehicleRigidBody.linearVelocity, transform.forward) <= -0.2f)
            {
                angularAcceleration -= Vector3.up * Input.GetAxis("Horizontal") * yawMovementAcceleration;
            }
        }
    }
}
