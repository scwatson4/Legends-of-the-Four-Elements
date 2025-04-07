using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace AnythingWorld.Behaviour
{
    public abstract class AnythingVehicleController : MonoBehaviour
    {
        [Header("Control Settings")]
        public bool controlThisVehicle = false;

        protected VehicleCameraController vehicleCameraController;

        private Camera vehicleCamera;

        public static AnythingVehicleController controlledVehicle;

        protected virtual void Awake()
        {
            //Spawns the Vehicle Camera
            GameObject cameraGameObject = new GameObject(gameObject.name + "Camera", typeof(Camera));
            cameraGameObject.transform.position = transform.position;
            cameraGameObject.transform.rotation = transform.rotation;
            vehicleCamera = cameraGameObject.GetComponent<Camera>();
            vehicleCameraController = cameraGameObject.AddComponent<VehicleCameraController>();
            vehicleCameraController.target = gameObject.transform;
        }

        protected virtual void Update()
        {
            if (controlThisVehicle)
            {
                vehicleCamera.enabled = true;
            }
            else
            {
                vehicleCamera.enabled = false;
            }

            ChangeControlledVehicle();
        }

        private void ChangeControlledVehicle()
        {
            //If one vehicle is selected to be controlled it will disable the currently controlled one.
            if (controlThisVehicle && controlledVehicle != this)
            {
                if (controlledVehicle)
                {
                    controlledVehicle.controlThisVehicle = false;
                }

                controlledVehicle = this;
            }
        }

        protected virtual void OnDestroy()
        {
            //If the gameobject is been destroyed by scene destroy don't do anything
            if (!gameObject.scene.isLoaded) { return; }

            //If the vehicle is destroyed its camera is also destroyed
            Destroy(vehicleCameraController.gameObject);
        }
    }
}
