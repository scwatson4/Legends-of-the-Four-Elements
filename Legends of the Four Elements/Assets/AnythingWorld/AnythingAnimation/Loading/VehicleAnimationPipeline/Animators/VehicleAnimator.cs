using System.Collections.Generic;
using UnityEngine;

namespace AnythingWorld.Animation.Vehicles
{
    [SelectionBase]
    public class VehicleAnimator : MonoBehaviour
    {
        [Header("Limits")]
        public float velocityUpperLimit = 60;
        public float velocityLowerLimit = -40;

        [Header("Forward Movement")]
        public float accelerationSpeed = 0.2f;
        public float brakeSpeed = 3;
        public float drag = 0.5f;
        [Header("Turning Movement")]
        public float turnSpeed = 1;
        public float steeringReturnSpeed = 1f;
        public float maxTurnAngle = 45;
        [Header("Visuals")]
        public float wheelRotationSpeed = 12;
        [HideInInspector]
        public float wheelYRotation = 0;
        [HideInInspector]
        public float velocity = 0;

        [Header("Wheel Transforms")]
        public List<GameObject> frontWheels = new List<GameObject>();
        public List<GameObject> wheels = new List<GameObject>();


        private void FixedUpdate()
        {
            UpdateWheelRotations();

        }
        private void UpdateWheelRotations()
        {
            //Vertical rotation
            foreach (var wheel in wheels)
            {
                wheel.transform.GetChild(0).Rotate(velocity * wheelRotationSpeed / 60 * 360 * Time.deltaTime, 0, 0, Space.Self);
            }
            //Lateral rotation
            foreach (var wheel in frontWheels)
            {
                wheel.transform.localRotation = Quaternion.Lerp(wheel.transform.localRotation, Quaternion.Euler(wheel.transform.localRotation.x, wheelYRotation, wheel.transform.localRotation.z), 1000);
            }
        }

        public void TurnToPercent(float amountOfTurn)
        {
            wheelYRotation = Mathf.MoveTowards(wheelYRotation, maxTurnAngle*amountOfTurn, turnSpeed);
        }
        public void ReturnSteeringToCenter()
        {
            wheelYRotation = Mathf.MoveTowards(wheelYRotation, 0, steeringReturnSpeed + (velocity / 100));
        }
        public void TurnLeft()
        {
            wheelYRotation = Mathf.MoveTowards(wheelYRotation, -maxTurnAngle, turnSpeed);
        }
        public void TurnRight()
        {
            wheelYRotation = Mathf.MoveTowards(wheelYRotation, maxTurnAngle, turnSpeed);
        }
        public void Accelerate()
        {
            velocity = Mathf.MoveTowards(velocity, velocityUpperLimit, accelerationSpeed);
        }
        public void Reverse()
        {
            velocity = Mathf.MoveTowards(velocity, velocityLowerLimit, accelerationSpeed);
        }
        public void Brake()
        {
            velocity = Mathf.MoveTowards(velocity, 0, brakeSpeed);
        }
        public void Decelerate()
        {
            velocity = Mathf.MoveTowards(velocity, 0, drag);
        }

        public void SetVelocity(float val)
        {
            velocity = val;
        }
    }
}
