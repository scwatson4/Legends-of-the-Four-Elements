using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnythingWorld.Behaviour
{
    public class BoatController : AnythingVehicleController
    {
        [Header("Boat Settings")]
        public float fowardResponsiviness = 0f;
        public float turnResponsiviness = 0f;
        public bool applyWaveEffectsOnTheBoat = true;
        public float depthBeforeSubmerged = 0f;
        [Header("Water Influence Settings")]
        public float waterResistance = 0.99f;
        public float waterAngularResistance = 0.5f;
        public float waterBuoyancyAmount = 3f;
        [Header("Default Values According to Boat Size")]
        public AnimationCurve boatSizeFowardSpeedCurve;
        public AnimationCurve boatSizeTurnSpeedCurve;


        private Rigidbody vehicleRigidBody;
        private Vector3 acceleration;
        private Vector3 angularAcceleration;
        private float boatBoundsHeightOffset = 0.9f;
        private float floatersClosenessOffset = 1f;
        private bool usingFloaters = false;
        private Floater[] floaters = new Floater[floatersNumber];
        private Bounds boatBounds;
        private float boatBottom;
        private float boatAbsoluteSize;

        private const int floatersNumber = 4;
        private const float maximumAbsoluteSizeToUseFloaters = 40f;

        protected override void Awake()
        {
            base.Awake();
            boatBounds = transform.GetComponentInChildren<MeshRenderer>().bounds;
            boatAbsoluteSize = (boatBounds.size.x + boatBounds.size.y + boatBounds.size.z) / 3;
            SpawnWaveController();
            SpawnBoatFloaters(boatAbsoluteSize);
            SetBoatInitialPosition();
            SetBoatDefaultFowardAndTurnSpeed(boatAbsoluteSize);
            vehicleRigidBody = gameObject.AddComponent<Rigidbody>();
            vehicleRigidBody.useGravity = false;
            vehicleRigidBody.linearDamping = 0.01f;
        }

        private void SpawnWaveController()
        {
            //Wave Controller is the gameObject that controls the waves attributes, like wave position, size and speed.
            //Boats can get from the Wave Controller the size of the wave located on its currently position as well as the wave normal

            if(!WaveController.instance)
            {
                GameObject waveControllerGameObject = new GameObject("WaveController");
                WaveController waveControllerComponent = waveControllerGameObject.AddComponent<WaveController>();
                waveControllerComponent.waveInfluencedObjects.Add(gameObject);
            }
            else
            {
                WaveController.instance.waveInfluencedObjects.Add(gameObject);
            }
        }

        private void SpawnBoatFloaters(float boatAbsoluteSize)
        {
            //Boat floaters are gameObjects that are located on the four corners of the ships hull
            //If the boat has boat floaters the forces applied on the boat are distributed on the floaters instead of applied just in the central position
            //The boat floaters gives the sensation of more instability and higher wave influence thats why it is been created and applied just on small size boats
            //The larger boats centralize the aplied forces on its center

            if(boatAbsoluteSize < maximumAbsoluteSizeToUseFloaters)
            {
                usingFloaters = true;
            }

            if(usingFloaters)
            {
                for (int i = 0; i < floatersNumber; i++)
                {
                    GameObject floater = new GameObject("Floater_" + (i + 1));
                    Floater floaterComponent = floater.AddComponent<Floater>();
                    floaters[i] = floaterComponent;
                    floater.transform.parent = transform;
                    floater.transform.position = boatBounds.center;
                    floater.transform.localRotation = Quaternion.identity;
                }
                floaters[0].transform.position += new Vector3(boatBounds.size.x / 2f * floatersClosenessOffset, -boatBounds.size.y / 2f * boatBoundsHeightOffset, boatBounds.size.z / 2f * floatersClosenessOffset);
                floaters[1].transform.position += new Vector3(-boatBounds.size.x / 2f * floatersClosenessOffset, -boatBounds.size.y / 2f * boatBoundsHeightOffset, boatBounds.size.z / 2f * floatersClosenessOffset);
                floaters[2].transform.position += new Vector3(boatBounds.size.x / 2f * floatersClosenessOffset, -boatBounds.size.y / 2f * boatBoundsHeightOffset, -boatBounds.size.z / 2f * floatersClosenessOffset);
                floaters[3].transform.position += new Vector3(-boatBounds.size.x / 2f * floatersClosenessOffset, -boatBounds.size.y / 2f * boatBoundsHeightOffset, -boatBounds.size.z / 2f * floatersClosenessOffset);

                if (depthBeforeSubmerged <= 0)
                {
                    depthBeforeSubmerged = ((89f * boatAbsoluteSize) - 810f) / 80f;
                    depthBeforeSubmerged = Mathf.Clamp(depthBeforeSubmerged, 1, depthBeforeSubmerged);
                }
            }
            else
            {
                if (depthBeforeSubmerged <= 0)
                {
                    depthBeforeSubmerged = 1f;
                }
            }
        }

        private void SetBoatInitialPosition()
        {
            //On Y axis, the boats are been positioned on the wave higher position
            //Boats may have different geometries pivots but this is been considered
            //We are using the mesh center to calculate the pivot offset

            if (WaveController.instance)
            {
                float waveHeight = WaveController.instance.GetWaveHeight(transform.position.x);
                float centerOffset = boatBounds.center.y - transform.position.y;
                float notFixedboatBottom = (boatBounds.size.y / 2f * boatBoundsHeightOffset);
                boatBottom = notFixedboatBottom - centerOffset;
                transform.position = new Vector3(transform.position.x, waveHeight + boatBottom, transform.position.z);
            }
        }

        private void SetBoatDefaultFowardAndTurnSpeed(float boatAbsoluteSize)
        {
            //Boats default foward and turn speed are calculated according to the boat "absolute size" (meshSizeX + meshSizeY + meshSizeZ / 3).

            Keyframe[] fowardSpeedKeyFrames;
            fowardSpeedKeyFrames = new Keyframe[6];
            fowardSpeedKeyFrames[0] = new Keyframe(0.0f, 0.0f);
            fowardSpeedKeyFrames[1] = new Keyframe(8f, 6f);
            fowardSpeedKeyFrames[2] = new Keyframe(33f, 14f);
            fowardSpeedKeyFrames[3] = new Keyframe(96f, 20f);
            fowardSpeedKeyFrames[4] = new Keyframe(182f, 30f);
            fowardSpeedKeyFrames[5] = new Keyframe(300f, 50f);
            boatSizeFowardSpeedCurve.keys = fowardSpeedKeyFrames;
            for (int i = 1; i < fowardSpeedKeyFrames.Length - 1; i++)
            {
                boatSizeFowardSpeedCurve.SmoothTangents(i, 1f);
            }
            if(fowardResponsiviness <= 0f)
            {
                fowardResponsiviness = boatSizeFowardSpeedCurve.Evaluate(boatAbsoluteSize);
            }

            Keyframe[] turnSpeedKeyFrames;
            turnSpeedKeyFrames = new Keyframe[6];
            turnSpeedKeyFrames[0] = new Keyframe(0.0f, 1f);
            turnSpeedKeyFrames[1] = new Keyframe(8f, 1f);
            turnSpeedKeyFrames[2] = new Keyframe(33f, 0.6f);
            turnSpeedKeyFrames[3] = new Keyframe(96f, 0.2f);
            turnSpeedKeyFrames[4] = new Keyframe(182f, 0.05f);
            turnSpeedKeyFrames[5] = new Keyframe(300f, 0.02f);
            boatSizeTurnSpeedCurve.keys = turnSpeedKeyFrames;
            for (int i = 1; i < turnSpeedKeyFrames.Length - 1; i++)
            {
                boatSizeTurnSpeedCurve.SmoothTangents(i, 1f);
            }

            if (turnResponsiviness <= 0f)
            {
                turnResponsiviness = boatSizeTurnSpeedCurve.Evaluate(boatAbsoluteSize);
            }
        }

        protected override void Update()
        {
            base.Update();

            HandleInputs();

            FollowWavesNormalIfNotUsingFloaters();
        }

        private void HandleInputs()
        {
            if (!controlThisVehicle) return;

            //Boat Controls = W A S D or Keyboard Arrows
            //The boat only can turn if it is been moving forward/backward
            acceleration = Vector3.zero;
            angularAcceleration = Vector3.zero;
            bool canTurnBoat = false;

            acceleration += transform.forward * Input.GetAxis("Vertical") * fowardResponsiviness;

            float dotVelocity = Vector3.Dot(vehicleRigidBody.linearVelocity, transform.forward);

            if (dotVelocity > 0.2f || dotVelocity < -0.2f) {canTurnBoat = true;}

            if (canTurnBoat)
            {
                if(dotVelocity > 0.2f)
                {
                    angularAcceleration += transform.up * Input.GetAxis("Horizontal") * turnResponsiviness;
                }
                else
                {
                    angularAcceleration -= transform.up * Input.GetAxis("Horizontal") * turnResponsiviness;
                }
            }
        }

        private void FollowWavesNormalIfNotUsingFloaters()
        {
            //If the boat is not using floaters it will follow the wave it is located normal movement
            //If the boat is using floaters the floaters different position on Y axis will make the boat moves according to the waves
            if (applyWaveEffectsOnTheBoat && !usingFloaters)
            {
                Vector3 deltaDesiredNormal = (Quaternion.LookRotation(WaveController.instance.GetWaveNormal(transform.position.x)).eulerAngles - transform.rotation.eulerAngles);
                var normal = WaveController.instance.GetWaveNormal(transform.position.x);
                var target_rot = Quaternion.LookRotation(transform.forward, normal);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target_rot, Time.deltaTime);
            }
        }

        private void FixedUpdate()
        {
            float waveHeight = 0f;

            if (applyWaveEffectsOnTheBoat)
            {
                //Applying the gravity and water (e. g. wave) forces on the boat if it is using floaters 
                // Loosely inspired and adapted by the physics equations in: https://www.youtube.com/watch?v=eL_zHQEju8s
                if (usingFloaters)
                {
                    foreach (var floater in floaters)
                    {
                        vehicleRigidBody.AddForceAtPosition(Physics.gravity / floaters.Length, floater.transform.position, ForceMode.Acceleration);
                        floater.waveHeight = WaveController.instance.GetWaveHeight(floater.transform.position.x);

                        if (floater.transform.position.y < floater.waveHeight)
                        {
                            float waterBuoyancyMultiplier = Mathf.Clamp01((floater.waveHeight - floater.transform.position.y) / depthBeforeSubmerged) * waterBuoyancyAmount;
                            vehicleRigidBody.AddForceAtPosition(new Vector3(0f, Mathf.Abs(Physics.gravity.y) * waterBuoyancyMultiplier, 0f), floater.transform.position, ForceMode.Acceleration);
                            vehicleRigidBody.AddForce(waterBuoyancyMultiplier * -vehicleRigidBody.linearVelocity * waterResistance * Time.fixedDeltaTime, ForceMode.VelocityChange);
                            vehicleRigidBody.AddTorque(waterBuoyancyMultiplier * -vehicleRigidBody.angularVelocity * waterAngularResistance * Time.fixedDeltaTime, ForceMode.VelocityChange);
                        }
                    }
                }
                //Applying the gravity and water (e. g. wave) forces on the boat if it is not using floaters
                // Loosely inspired and adapted by the physics equations in: https://www.youtube.com/watch?v=eL_zHQEju8s
                else
                {
                    vehicleRigidBody.AddForce(Physics.gravity, ForceMode.Acceleration);
                    waveHeight = WaveController.instance.GetWaveHeight(transform.position.x);


                    if (transform.position.y < waveHeight + boatBottom)
                    {
                        float waterBuoyancyMultiplier = Mathf.Clamp01(((waveHeight + boatBottom) - transform.position.y) / depthBeforeSubmerged) * waterBuoyancyAmount;
                        vehicleRigidBody.AddForce(new Vector3(0f, Mathf.Abs(Physics.gravity.y) * waterBuoyancyMultiplier, 0f), ForceMode.Acceleration);
                        vehicleRigidBody.AddForce(waterBuoyancyMultiplier * -vehicleRigidBody.linearVelocity * waterResistance * Time.fixedDeltaTime, ForceMode.VelocityChange);
                        vehicleRigidBody.AddTorque(waterBuoyancyMultiplier * -vehicleRigidBody.angularVelocity * waterAngularResistance * Time.fixedDeltaTime, ForceMode.VelocityChange);
                    }
                }
            }
            else
            {
                transform.position = new Vector3(transform.position.x, boatBottom, transform.position.z);
                transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
                vehicleRigidBody.AddForce(-vehicleRigidBody.linearVelocity * waterResistance * Time.fixedDeltaTime, ForceMode.VelocityChange);
                vehicleRigidBody.AddTorque(-vehicleRigidBody.angularVelocity * waterAngularResistance * Time.fixedDeltaTime, ForceMode.VelocityChange);
            }

            if (!controlThisVehicle) return;

            //Applying the forces to control the boat
            var velocity = (vehicleRigidBody.linearVelocity) + (acceleration * Time.fixedDeltaTime);
            vehicleRigidBody.linearVelocity = (velocity * Time.fixedDeltaTime + (acceleration * 0.5f * Time.fixedDeltaTime * Time.fixedDeltaTime)) / Time.fixedDeltaTime;


            var angularVelocity = (vehicleRigidBody.angularVelocity) + (angularAcceleration * Time.fixedDeltaTime);
            vehicleRigidBody.angularVelocity = (angularVelocity * Time.fixedDeltaTime + (angularAcceleration * 0.5f * Time.fixedDeltaTime * Time.fixedDeltaTime)) / Time.fixedDeltaTime;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            //If the gameobject is been destroyed by scene destroy don't do anything
            if (!gameObject.scene.isLoaded) { return; }

            WaveController.instance.waveInfluencedObjects.Remove(gameObject);
            //Remove Wave Controller if this is the only gameObject that it is been influenced by the waves 
            if(WaveController.instance.waveInfluencedObjects.Count <= 0)
            {
                Destroy(WaveController.instance.gameObject);
                WaveController.instance = null;
            }
        }
    }
}
