using AnythingWorld.GLTFUtility;
using AnythingWorld.Utilities;
using AnythingWorld.Utilities.Data;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AnythingWorld.Models
{
    public static class GltfLoader
    {
        /// <summary>
        /// Loads the GLTF model data asynchronously.
        /// </summary>
        /// <param name="data">The model data to load.</param>
        /// <returns>An IEnumerator for coroutine handling.</returns>
        public static async UniTask LoadAsync(ModelData data)
        {
            await GltfRequester.RequestRiggedAnimationBytesAsync(data, LoadAnimatedGlb);
        }

        /// <summary>
        /// Request bytes and animation clips and assign to variables in LModelData.LoadedData
        /// </summary>
        /// <param name="data">Request model data</param>
        private static void LoadAnimatedGlb(ModelData data)
        {
            // Load default animation state
            var animationBytesDict = data?.loadedData?.gltf?.animationBytes;
            if (animationBytesDict == null || animationBytesDict.Count == 0)
            {
                data.actions.onFailure?.Invoke(data, "Failed to load animation dictionary.");
                return;
            }

            if (!data.model)
            {
                data.actions.onFailure?.Invoke(data, $"Parent game object {data.json?.name} has been destroyed, " +
                                                     "returning.");
                return;
            }
            
            // If default state not found, use first state
            string defaultState = "idle";
            if (!data.loadedData.gltf.animationBytes.ContainsKey(defaultState))
            {
                defaultState = data.loadedData.gltf.animationBytes.ToArray()[0].Key;
            }

            // load legacy
            foreach (var kvp in data.loadedData.gltf.animationBytes)
            {
                // If default state don't load (loaded above), else load animation into model data.
                if (kvp.Key == defaultState)
                {
                    var createdObject = LoadGlbAndAnimationLegacy(data, defaultState);
                    createdObject.transform.parent = data.model.transform;
                    data.rig = createdObject;
                }
                else
                {
                    LoadGlbAnimationOnlyLegacy(data, kvp.Key);
                }
            }
            
            // load normal
            foreach(var kvp in data.loadedData.gltf.animationBytes)
            {
                LoadGlbAnimationOnly(data, kvp.Key);
            }
        }
        
        /// <summary>
        /// Loads GLB animation only and adds the animation clips to the model data.
        /// </summary>
        /// <param name="data">The model data containing the GLB animation bytes.</param>
        /// <param name="key">The key identifying the specific animation to load.</param>
        private static void LoadGlbAnimationOnly(ModelData data, string key)
        {
            ImportSettings setting = new ImportSettings();
            setting.animationSettings.interpolationMode = InterpolationMode.LINEAR;
            setting.animationSettings.useLegacyClips = false;
            setting.animationSettings.looping = true;
            var loadedGlb = Importer.LoadFromBytes(data.loadedData.gltf.animationBytes[key], out var clips, setting);

            foreach (var (clip, index) in clips.WithIndex())
            {
                clip.legacy = false;
                clip.wrapMode = WrapMode.Loop;
                var clipName = key;
                clip.EnsureQuaternionContinuity();
                if (index != 0) clipName += index.ToString();
                data.loadedData.gltf.animationClips.Add(clipName, clip);
            }
            Utilities.Destroy.GameObject(loadedGlb);
        }
        
        /// <summary>
        /// Load model into game object and animations into data container, return game object.
        /// </summary>
        /// <param name="data">Model request data.</param>
        /// <param name="key">Animation name</param>
        /// <returns></returns>
        private static GameObject LoadGlbAndAnimationLegacy(ModelData data, string key)
        {
            ImportSettings setting = new ImportSettings();
            setting.animationSettings.interpolationMode = InterpolationMode.LINEAR;
            setting.animationSettings.useLegacyClips = true;
            setting.animationSettings.looping = true;
            var loadedGlb = Importer.LoadFromBytes(data.loadedData.gltf.animationBytes[key], out var clips, setting);
            foreach (var (clip, index) in clips.WithIndex())
            {
                clip.wrapMode = WrapMode.Loop;
                var clipName = key;
                clip.EnsureQuaternionContinuity();
                if (index != 0) clipName += index.ToString();
                data.loadedData.gltf.animationClipsLegacy.Add(clipName, clip);
            }
            return loadedGlb;
        }

        /// <summary>
        /// Load animation clips into model data and delete glb game object after.
        /// </summary>
        /// <param name="data">Model request data.</param>
        /// <param name="key">Animation name.</param>
        private static void LoadGlbAnimationOnlyLegacy(ModelData data, string key)
        {
            ImportSettings setting = new ImportSettings();
            setting.animationSettings.interpolationMode = InterpolationMode.LINEAR;
            setting.animationSettings.useLegacyClips = true;
            setting.animationSettings.looping = true;
            var loadedGlb = Importer.LoadFromBytes(data.loadedData.gltf.animationBytes[key], out var clips, setting);
            foreach (var (clip, index) in clips.WithIndex())
            {
                clip.wrapMode = WrapMode.Loop;
                var clipName = key;
                clip.EnsureQuaternionContinuity();
                if (index != 0) clipName += index.ToString();
                data.loadedData.gltf.animationClipsLegacy.Add(clipName, clip);
            }
            Utilities.Destroy.GameObject(loadedGlb);
        }
    }
}
