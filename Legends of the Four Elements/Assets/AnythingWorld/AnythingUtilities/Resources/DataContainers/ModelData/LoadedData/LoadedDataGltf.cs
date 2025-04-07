using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace AnythingWorld.Utilities.Data
{
    [Serializable]
    public class LoadedDataGltf
    {
        public LoadedDataGltf()
        {
            animationBytes = new Dictionary<string, byte[]>();
            animationClips = new Dictionary<string, AnimationClip>();
            animationClipsLegacy = new Dictionary<string, AnimationClip>();
        }
        public byte[] bytes;
        public Dictionary<string, AnimationClip> animationClips;
        public Dictionary<string, AnimationClip> animationClipsLegacy;
        public Dictionary<string, byte[]> animationBytes;
    }
}
