using System;
using UnityEngine;
using System.Collections.Generic;
namespace AnythingWorld.Utilities.Data
{
    [Serializable]
    public class Rig
    {
        /// <summary>
        /// Dictionary of animation name/AnimationFormat pairs
        /// Eg. walk : [glb: "URL", fbx: "URL"]
        /// </summary>
        public Dictionary<string, AnimationFormats> animations;
    }
}
