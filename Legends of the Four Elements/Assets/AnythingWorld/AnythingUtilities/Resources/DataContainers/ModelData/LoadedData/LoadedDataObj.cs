using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace AnythingWorld.Utilities.Data
{
    [Serializable]
    public class LoadedDataObj
    {
        public LoadedDataObj()
        {
            partsBytes = new Dictionary<string, byte[]>();
            loadedTextures = new Dictionary<string, Texture>();
            loadedParts = new Dictionary<string, GameObject>();
        }

        public Dictionary<string, byte[]> partsBytes;
        public Dictionary<string, GameObject> loadedParts;
        public Dictionary<string, Texture> loadedTextures;
        public byte[] mtlString;
    }
}
