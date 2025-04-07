using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnythingWorld.Animation
{
    public class PropellorVehicleAnimator : FlyingVehicleAnimator
    {
        public List<GameObject> propellorBlades = new List<GameObject>();
        public float engineForce = 0;
        public float speedUpperLimit = 70;
        public float speedLowerLimit = 0;
        public float accelerationSpeed = 1;
        public float decelerationSpeed = 1;
        public bool drawGizmos = false;


        public void FixedUpdate()
        {
            UpdatePropellorMovement();
        }
        public void UpdatePropellorMovement()
        {
            foreach(var prop in propellorBlades)
            { 
                prop.transform.Rotate(0, engineForce / 60 * 360 * Time.fixedDeltaTime, 0, Space.Self);
            }
        }

        public override void Accelerate()
        {
            engineForce = Mathf.MoveTowards(engineForce, speedUpperLimit, accelerationSpeed);
        }
        public override void Decelerate()
        {
            engineForce = Mathf.MoveTowards(engineForce, speedLowerLimit, decelerationSpeed);

        }
        public override void Stop()
        {
            engineForce = 0;
        }

        public void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;
            foreach (var renderer in GetComponentsInChildren<Renderer>()) DrawBounding(renderer);
        }
        
        public void DrawBounding(Renderer rend)
        {
            var r = rend;
            if (rend== null)
                return;
            var bounds = r.bounds;
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(bounds.center, bounds.extents * 2);
            Gizmos.DrawSphere(bounds.center, 0.3f);
        }
    }
}
