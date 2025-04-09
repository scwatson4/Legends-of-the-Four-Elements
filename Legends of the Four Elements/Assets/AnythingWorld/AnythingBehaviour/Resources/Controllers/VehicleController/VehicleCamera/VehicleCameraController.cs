using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnythingWorld.Behaviour
{
    public class VehicleCameraController : MonoBehaviour
    {
        //This code controls the camera of the vehicles
        public float cameraMoveSpeed = 5f;

        public Transform target;

        public Vector3 cameraOffset = new Vector3(0f, 0f, 0f);

        public bool followFromBehind = true;

        public bool lookDireclty = false;

        private bool freeViewIsAdjusted = false;



        private void Start()
        {
            Vector3 targetSize = target.transform.GetComponentInChildren<MeshRenderer>().bounds.size;
            cameraOffset = new Vector3(0f, Vector3.Dot(targetSize, transform.up) * 0.7f, Mathf.Abs(Vector3.Dot(targetSize, transform.forward)) * -1.6f);
        }
        private void FixedUpdate()
        {
            if(!target) return;

            if (followFromBehind)
            {
                transform.position = target.transform.position + (target.transform.forward * cameraOffset.z) + (target.transform.right * cameraOffset.x);

                transform.position = new Vector3(transform.position.x, target.transform.position.y + cameraOffset.y, transform.position.z);
                
                transform.localRotation = Quaternion.Euler(0f, target.localRotation.eulerAngles.y, 0f);

                freeViewIsAdjusted= false;

            }
            else
            {
                AdjustFreeView();
                transform.position = Vector3.Lerp(transform.position, target.position + cameraOffset, cameraMoveSpeed * Time.deltaTime);
            }

            if (lookDireclty)
            {
                transform.LookAt(target.position);
            }
        }

        private void AdjustFreeView()
        {
            if(!freeViewIsAdjusted)
            {
                lookDireclty = true;
                freeViewIsAdjusted = true;
            }
        }
    }
}
