using AnythingWorld.Utilities.Data;
using System.Collections.Generic;
using UnityEngine;

namespace AnythingWorld.Animation.Vehicles
{
    /// <summary>
    /// Provides methods to load and set up propellor animations for vehicle models.
    /// </summary>
    public static class VehiclePropellorAnimationLoader
    {
        /// <summary>
        /// Loads the propellor animation for the specified model data.
        /// </summary>
        /// <param name="data">The model data containing the vehicle model and its parts.</param>
        public static void Load(ModelData data)
        {
            var animationScript = data.model.AddComponent<PropellorVehicleAnimator>();
            List<Transform> rawBlades = new List<Transform>();
            foreach (var part in data.loadedData.obj.loadedParts)
            {
                if (part.Key.Contains("wing"))
                {
                    rawBlades.Add(part.Value.transform);
                }
            }
            var center = GetCenterOfPropellor(rawBlades);
            foreach(var blade in rawBlades)
            {
                var go = CenterWheelPivot.CenterMeshCustomPivot(blade.gameObject, center);
                animationScript.propellorBlades.Add(go.transform.GetChild(0).gameObject);
            }
        }

        /// <summary>
        /// Calculates the center point of the propellor based on the provided blades.
        /// </summary>
        /// <param name="propellors">The list of propellor blade transforms.</param>
        /// <returns>The center point of the propellor.</returns>
        private static Vector3 GetCenterOfPropellor(List<Transform> propellors)
        {
            // Get primitive centroid
            Vector3 centerAggregate = Vector3.zero;
            foreach (var blade in propellors)
            {
                var center = blade.GetComponentInChildren<Renderer>().bounds.center;
                centerAggregate += center;
            }

            return centerAggregate / propellors.Count;
        }
    }
}