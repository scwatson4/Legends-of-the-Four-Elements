using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnythingWorld.Models
{
    /// <summary>
    /// Stores cached model data, that helps to determine if cached model type corresponds to a newly requested.
    /// </summary>
    [Serializable]
    public class CachedModelData
    {
        public GameObject modelRoot;
        public bool isAnimated;
        public bool isLegacyAnimationPipeline;

        public CachedModelData(GameObject modelRoot, bool isAnimated, bool isLegacyAnimationPipeline)
        {
            this.modelRoot = modelRoot;
            this.isAnimated = isAnimated;
            this.isLegacyAnimationPipeline = isLegacyAnimationPipeline;
        }
    }
    
    /// <summary>
    /// Repository to manage cached models and their data.
    /// </summary>
    public static class CachedModelsRepository
    {
        private static readonly Dictionary<string, CachedModelData> CachedModels = 
            new Dictionary<string, CachedModelData>();

        private static string _currentSceneName;
        
        /// <summary>
        /// Adds a model to the cache if it's not already present.
        /// </summary>
        /// <param name="modelId">Id of the model to cache.</param>
        /// <param name="model">The model's root GameObject.</param>
        public static void AddModel(string modelId, GameObject model)
        {
            if (CachedModels.Count == 0)
            {
                _currentSceneName = SceneManager.GetActiveScene().name;
            }
            else if (_currentSceneName != SceneManager.GetActiveScene().name)
            {
                CachedModels.Clear();
                _currentSceneName = SceneManager.GetActiveScene().name;
            }

            if (CachedModels.ContainsKey(modelId))
            {
                return;
            }
            
            bool hasModernAnimation = model.GetComponentInChildren<Animator>();
            bool hasLegacyAnimation = model.GetComponentInChildren<Animation>();
            bool isAnimated = hasModernAnimation || hasLegacyAnimation;
            CachedModels[modelId] = new CachedModelData(model, isAnimated, hasLegacyAnimation);
        }
        
        /// <summary>
        /// Tries to retrieve the cached model data for a given model name.
        /// </summary>
        /// <param name="modelId">Id the model to retrieve.</param>
        /// <param name="modelData">The output parameter for the retrieved model data.</param>
        /// <returns>True if the model is found and valid, false otherwise.</returns>
        public static bool TryGetModelData(string modelId, out CachedModelData modelData)
        {
            modelData = null;
            
            if (_currentSceneName != SceneManager.GetActiveScene().name || 
                !CachedModels.TryGetValue(modelId, out modelData))
            {
                return false;
            }
            
            if (modelData.modelRoot)
            {
                return true;
            }
            
            CachedModels.Remove(modelId);
            return false;
        }

        public static void Clear()
        {
            CachedModels.Clear();
            _currentSceneName = string.Empty;
        }
    }
}