using UnityEngine;

namespace AnythingWorld.PathCreation 
{
    /// <summary>
    /// Stores the minimum and maximum values of a 3D vector and updates them based on the input.
    /// </summary>
    public class MinMax3D 
    {
        public Vector3 Min { get; private set; } = Vector3.one * float.MaxValue;
        public Vector3 Max { get; private set; } = Vector3.one * float.MinValue;

        /// <summary>
        /// Updates current min and max values if supplied vector components differ from them.
        /// </summary>
        public void UpdateValues(Vector3 v)
        {
            Min = new Vector3(Mathf.Min(Min.x, v.x), Mathf.Min(Min.y, v.y), Mathf.Min(Min.z, v.z));
            Max = new Vector3(Mathf.Max(Max.x, v.x), Mathf.Max(Max.y, v.y), Mathf.Max(Max.z, v.z));
        }
    }
}
