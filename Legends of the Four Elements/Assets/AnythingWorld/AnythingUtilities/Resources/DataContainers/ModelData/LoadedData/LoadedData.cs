using UnityEngine;
using System;

namespace AnythingWorld.Utilities.Data
{
    [Serializable]
    public class LoadedData
    {
        public Vector3 bounds = Vector3.one;
        public LoadedDataObj obj = new LoadedDataObj();
        public LoadedDataGltf gltf = new LoadedDataGltf();
        public Vector3 dbDimensionsVector;
        public float boundsYOffset = 0;
    }
}
