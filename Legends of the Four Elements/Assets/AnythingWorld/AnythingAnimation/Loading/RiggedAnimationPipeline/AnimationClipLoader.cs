#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using AnythingWorld.Utilities.Data;
using System.Collections.Generic;
using AnythingWorld.Behaviour.Tree;
using System.IO;

namespace AnythingWorld.Animation
{
    public static class AnimationClipLoader
    {
        /// <summary>
        /// Load animations onto model.
        /// </summary>
        /// <param name="data"></param>
        public static void Load(ModelData data)
        {
            if (AnythingSettings.DebugEnabled)
            {
                Debug.Log(data.parameters.UseLegacyAnimatorInEditor ? "Legacy" : "Modern" + " animator selected");
            }
            
#if UNITY_EDITOR
            if (data.isCachedModelLoaded)
            {
                LoadAnimFromCachedModel(data);
                return;
            }
            
            if (data.parameters.UseLegacyAnimatorInEditor)
            {
                if (AnythingSettings.DebugEnabled) Debug.Log("Loading legacy clips");
                LoadAnimClipLegacy(data.loadedData.gltf.animationClipsLegacy, data.rig, data);
            }
            else
            {
                if (AnythingSettings.DebugEnabled) Debug.Log("Loading modern clips");
                LoadAnimClip(data.loadedData.gltf.animationClips, data.rig, data);
            }
#else
            if (data.isCachedModelLoaded)
            {
                return;
            }

            if (AnythingSettings.DebugEnabled)
            {
                Debug.Log("loading legacy clips");
            }

            LoadAnimClipLegacy(data.loadedData.gltf.animationClipsLegacy, data.rig, data);
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Loads animation data from the cached model.
        /// </summary>
        /// <param name="data">The model data containing the rig and animation information.</param>
        private static void LoadAnimFromCachedModel(ModelData data)
        {
            if (data.parameters.UseLegacyAnimatorInEditor)
            {
                return;
            }
            
            var animator = data.rig.GetComponent<Animator>();
                
            if (!animator.DoesJumpAnimationExist())
            {
                return;
            }

            var container = data.model.AddComponent<MovementDataContainer>();
            container.jumpAnimationDuration = animator.GetJumpAnimationDuration();
            container.jumpEndAnimationDuration = animator.GetJumpEndAnimationDuration();
        }
#endif
        
        /// <summary>
        /// Load animation clips into a legacy Animation component. 
        /// </summary>
        /// <param name="animationClips"></param>
        /// <param name="target"></param>
        private static void LoadAnimClipLegacy(Dictionary<string,AnimationClip> animationClips, GameObject target, ModelData data)
        {
            //Load legacy animations
            var anim = target.AddComponent<UnityEngine.Animation>();

            LegacyAnimationController legacyAnimationController = null;
            switch (data.defaultBehaviourType)
            {
                case Utilities.DefaultBehaviourType.Static:
                    break;
                case Utilities.DefaultBehaviourType.GroundCreature:
                    legacyAnimationController = target.AddComponent<MovementJumpLegacyController>();
                    break;
                case Utilities.DefaultBehaviourType.GroundVehicle:
                    break;
                case Utilities.DefaultBehaviourType.FlyingVehicle:
                    break;
                case Utilities.DefaultBehaviourType.FlyingCreature:
                    legacyAnimationController = target.AddComponent<FlyingAnimationController>();
                    break;
                case Utilities.DefaultBehaviourType.SwimmingCreature:
                    break;
            }
            
            if (legacyAnimationController)
            {
                foreach (var kvp in animationClips)
                {
                    var clipName = kvp.Key;
                    var clip = kvp.Value;
                    clip.legacy = true;
                    clip.wrapMode = WrapMode.Loop;
                    anim.AddClip(clip, clipName);
                    
                    legacyAnimationController.loadedAnimationNames.Add(clipName);
                    legacyAnimationController.loadedAnimationDurations.Add(clip.length);
                }
            }
        }

        #region Modern Animation System
#if UNITY_EDITOR
        /// <summary>
        /// Serialize animation clip to asset database, 
        /// </summary>
        /// <param name="animationClips"></param>
        /// <param name="target"></param>
        /// <param name="data"></param>
        private static void LoadAnimClip(Dictionary<string, AnimationClip> animationClips, GameObject target, 
            ModelData data)
        {
            foreach (var kvp in animationClips)
            {
                var clip = kvp.Value;
                if (data.parameters.SerializeAssets && 
                    !File.Exists($"{Application.dataPath}/SavedAssets/{data.json.name}/.flag"))
                {
                    Utilities.AssetSaver.SerializeAnimationClips(animationClips, data.model.name);
                }
                clip.wrapMode = WrapMode.Loop;
            }

            //Add animator to model
            if (!target.TryGetComponent<Animator>(out var animator))
            {
                animator = target.AddComponent<Animator>();
            }

            //Create override animator controller and add to animator component
            var animatorOverrideController = CreateAnimatorOverrideController(animator, data);

            SetAnimationOverrides(animatorOverrideController, animationClips);

            if (!animator.DoesJumpAnimationExist())
            {
                return;
            }

            var container = data.model.AddComponent<MovementDataContainer>();
            container.jumpAnimationDuration = animator.GetJumpAnimationDuration();
            container.jumpEndAnimationDuration = animator.GetJumpEndAnimationDuration();
        }

        private static void SetAnimationOverrides(AnimatorOverrideController animatorOverrideController, 
            Dictionary<string,AnimationClip> animationClips)
        {
            var clipOverrides = new AnimationClipOverrides(animatorOverrideController.overridesCount);

            animatorOverrideController.GetOverrides(clipOverrides);

            foreach (var clip in animationClips)
            {
                SetAnimationClipOverride(clipOverrides, clip.Value, clip.Key);
            }

            animatorOverrideController.ApplyOverrides(clipOverrides);
            EditorUtility.SetDirty(animatorOverrideController);
        }

        /// <summary>
        /// Get clips overrides from the controller and apply to the new animation clip. 
        /// </summary>
        /// <param name="animatorOverrideController"></param>
        /// <param name="clipOverrides"></param>
        /// <param name="animationClip"></param>
        /// <param name="stateName"></param>
        private static void SetAnimationClipOverride(AnimationClipOverrides clipOverrides, 
            AnimationClip animationClip, string stateName)
        {
            foreach (var clip in clipOverrides)
            {
                if (clip.Key.name == stateName)
                {
                    bool isLooped = clip.Key.isLooping;
                    var settings = AnimationUtility.GetAnimationClipSettings(animationClip);
                    settings.loopTime = isLooped;
                    AnimationUtility.SetAnimationClipSettings(animationClip, settings);
                    animationClip.wrapMode = WrapMode.Loop;
                    AnimationClip clipOverride = animationClip;
                    clipOverride.wrapMode = WrapMode.Loop;
                    clipOverride.name = stateName;
                    clipOverrides[stateName.ToLower()] = clipOverride;
                    return;
                }
            }
        }

        /// <summary>
        /// Create override animation controller from template controller.
        /// </summary>
        /// <param name="animator"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private static AnimatorOverrideController CreateAnimatorOverrideController(Animator animator, ModelData data)
        {
            var controller = data.defaultBehaviourType switch
            {
                Utilities.DefaultBehaviourType.FlyingCreature => "BaseControllers/BirdController",
                _ => "BaseControllers/BaseController"
            };

            var animatorOverrideController =
                new AnimatorOverrideController(Resources.Load(controller) as RuntimeAnimatorController)
                {
                    name = "Override Controller"
                };
            animator.runtimeAnimatorController = animatorOverrideController;
            return animatorOverrideController;
        }
#endif
#endregion
    }
}
