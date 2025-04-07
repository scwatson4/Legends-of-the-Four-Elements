using UnityEngine;

namespace AnythingWorld.Animation
{
    public static class VehicleRootMotion
    {
        public static void MoveForward(Transform transform, float velocity, float movementSpeed = 1)
        {
            transform.position = transform.position + (transform.forward * velocity * movementSpeed * Time.deltaTime);
        }
        public static void Rotate(Transform transform, float wheelAngle, float velocity, float rotationSpeed = 1)
        {
            transform.Rotate(0, wheelAngle * velocity * rotationSpeed * 0.01f * Time.deltaTime, 0);
        }
    }
}
