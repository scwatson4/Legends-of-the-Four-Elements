using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnythingWorld.Animation
{
    public abstract class FlyingVehicleAnimator : MonoBehaviour
    {
        public void Start()
        {
            //If rigidbody with gravity has been applied turn this off because it will break the system.
            if(TryGetComponent<Rigidbody>(out var rb))
            {
                rb.useGravity = false;

            }
        }
        public abstract void Accelerate();
        public abstract void Decelerate();
        public abstract void Stop();
    }
}
