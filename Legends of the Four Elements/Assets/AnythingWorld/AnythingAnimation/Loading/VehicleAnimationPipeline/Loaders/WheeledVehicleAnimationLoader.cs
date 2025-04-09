using AnythingWorld.Utilities.Data;

namespace AnythingWorld.Animation.Vehicles
{
    public static class WheeledVehicleAnimationLoader
    {
        public static void Load(ModelData data)
        {
            //Add vehicle animation script
            var animationScript = data.model.AddComponent<VehicleAnimator>();
            var rot = data.model.transform.rotation;
            data.model.transform.rotation = UnityEngine.Quaternion.identity;
            //find wheels
            foreach (var part in data.loadedData.obj.loadedParts)
            {
                if (part.Key.Contains("wheel"))
                {
                    //Make center pivot for wheel mesh
                    var centrePivot = CenterWheelPivot.CenterWheel(part.Value);
                    //Pass pivot to animation script for rotation
                    animationScript.wheels.Add(centrePivot);
                    if (part.Key.Contains("front"))
                    {
                        animationScript.frontWheels.Add(centrePivot);
                    }
                }
            }

            data.model.transform.rotation = rot;
        }
    }
}
