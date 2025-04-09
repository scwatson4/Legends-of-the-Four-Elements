using AnythingWorld.Utilities;
using AnythingWorld.Utilities.Data;
using Cysharp.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace AnythingWorld.Models
{
    /// <summary>
    /// Provides a set of static methods for loading models.
    /// </summary>
    public static class ModelLoader
    {
        /// <summary>
        /// Loads a model using the specified <paramref name="data"/> object asynchronously.
        /// </summary>
        /// <param name="data">The model data to load.</param>
        public static async UniTask LoadAsync(ModelData data)
        {
            if (data.model == null)
            {
                data.actions.onFailure?.Invoke(data, "Object parent has been destroyed, returning.");
                return;
            }

            data.Debug($"Loading model with {data.modelLoadingPipeline} pipeline.");

            var needAnimatedModel = data.modelLoadingPipeline == ModelLoadingPipeline.RiggedGLB;

            if (data.parameters.CacheModel && TrySpawnCachedModel(data, needAnimatedModel))
            {
                if (needAnimatedModel)
                {
                    data.rig = data.model.transform.GetChild(0).gameObject;
                }
                data.isCachedModelLoaded = true;
                return;
            }

            // Determine which pipeline to use based on the specified ModelLoadingPipeline value
            switch (data.modelLoadingPipeline)
            {
                case ModelLoadingPipeline.RiggedGLB:
                    await GltfLoader.LoadAsync(data);
                    break;

                case ModelLoadingPipeline.GLTF:
                    // TODO: Implement GLTF loading if necessary
                    break;

                case ModelLoadingPipeline.OBJ_Static:
                    await ObjLoader.LoadAsync(data, true);
                    break;

                case ModelLoadingPipeline.OBJ_Part_Based:
                    await ObjLoader.LoadAsync(data, false);
                    break;

                default:
                    data.actions.onFailure?.Invoke(data, "Unsupported model loading pipeline.");
                    break;
            }
        }

        /// <summary>
        /// Attempts to spawn a cached model based on the provided model data and animation requirements.
        /// </summary>
        /// <param name="data">Requested model data.</param>
        /// <param name="needAnimatedModel">Indicates whether an animated model is needed.</param>
        /// <returns>True if a cached model or prefab is successfully spawned, false otherwise.</returns>
        private static bool TrySpawnCachedModel(ModelData data, bool needAnimatedModel)
        {
            if (CachedModelsRepository.TryGetModelData(data.json._id, out var cachedModelData) &&
                cachedModelData.isAnimated == needAnimatedModel &&
                data.parameters.UseLegacyAnimatorInEditor == cachedModelData.isLegacyAnimationPipeline)
            {
                TransferCachedModelToModelGameObject(data.model, cachedModelData.modelRoot.transform, false);
                return true;
            }

#if UNITY_EDITOR
            if (AssetSaver.TryGetSerializedPrefab(data.json.name, out var prefab))
            {
                bool hasModernAnimation = prefab.GetComponentInChildren<Animator>();
                bool hasLegacyAnimation = prefab.GetComponentInChildren<Animation>();
                var isAnimated = hasModernAnimation || hasLegacyAnimation;

                if (isAnimated != needAnimatedModel ||
                    data.parameters.UseLegacyAnimatorInEditor != hasLegacyAnimation)
                {
                    return false;
                }

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                TransferCachedModelToModelGameObject(data.model, instance.transform, true);
                return true;
            }
#endif
            return false;
        }

        /// <summary>
        /// Transfers the cached model's children to the target GameObject, copying transformations.
        /// </summary>
        /// <param name="modelGameObject">The target GameObject where the model is being transferred.</param>
        /// <param name="cachedModelRoot">The transform of the cached model root.</param>
        /// <param name="isPrefab">Indicates if the model is a prefab instance.</param>
        private static void TransferCachedModelToModelGameObject(GameObject modelGameObject, Transform cachedModelRoot, bool isPrefab)
        {
            for (var i = 0; i < cachedModelRoot.childCount; i++)
            {
                var currentChild = cachedModelRoot.GetChild(i);

                var newChild = Object.Instantiate(currentChild, modelGameObject.transform);
                newChild.name = currentChild.name;
                newChild.localPosition = currentChild.localPosition;
                newChild.localRotation = currentChild.localRotation;
                newChild.localScale = currentChild.localScale;
            }

            if (isPrefab)
            {
#if UNITY_EDITOR
                Object.DestroyImmediate(cachedModelRoot.gameObject);
#endif
            }
        }
    }
}
