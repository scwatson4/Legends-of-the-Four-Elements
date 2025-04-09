using AnythingWorld.Utilities;
using AnythingWorld.Utilities.Data;
using System;
using AnythingWorld.Behaviour;
using AnythingWorld.Behaviour.Tree;
using AnythingWorld.Models;
using AnythingWorld.PostProcessing;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnythingWorld.Core
{
    /// <summary>
    /// Provides methods for post-processing models after they are created.
    /// </summary>
    public static class ModelPostProcessing
    {
        /// <summary>
        /// Completes the model creation process by invoking success actions and applying additional components.
        /// </summary>
        /// <param name="data">The model data containing parameters and actions.</param>
        public static void FinishMakeProcess(ModelData data)
        {
            // Invoke factory actions stored for successful creation of model.
            foreach (var action in data.actions.onSuccess)
            {
                action?.Invoke(data, "Successfully made");
            }

            MovementDataContainer movementDataContainer = null;

            if (data.defaultBehaviourType != DefaultBehaviourType.Static)
            {
                if (!data.model.TryGetComponent(out movementDataContainer))
                {
                    movementDataContainer = data.model.AddComponent<MovementDataContainer>();
                }
            }

            // If collider specified add collider
            if (data.parameters.AddCollider)
            {
                AddCollider(data);
            }

            // If rigidbody specified, add rigibody
            if (data.parameters.AddRigidbody)
            {
                AddRigidbody(data);
            }

            if (data.defaultBehaviourType != DefaultBehaviourType.Static)
            {
                SetMovementDataProperties(movementDataContainer, data);

                if (data.parameters.UseNavMesh)
                {
                    NavMeshHandler.AddNavMeshAndAgent(movementDataContainer.extents, data.model);
                }

                if (data.model.TryGetComponent(out BehaviourTreeInstanceRunner instanceRunner))
                {
                    instanceRunner.InitializeTree();
                }
            }

            if (data.parameters.CacheModel)
            {
                CachedModelsRepository.AddModel(data.json._id, data.model);
            }

#if UNITY_EDITOR
            // If serializing parameter passed, attempt to serialize.
            if (data.parameters.SerializeAssets)
            {
                AssetSaver.CreateAssetFromData(new CallbackInfo(data));
            }

            //dirty scene on successful completion
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
#endif
        }

        /// <summary>
        /// Adds a collider to the model based on the specified parameters.
        /// </summary>
        /// <param name="data">The model data containing collider parameters.</param>
        private static void AddCollider(ModelData data)
        {
            var skinnedRenderer = data.model.GetComponentInChildren<SkinnedMeshRenderer>();
            Bounds bounds;

            if (skinnedRenderer)
            {
                bounds = GetBoundsSkinnedMeshRenderer(data);
            }
            else
            {
                bounds = data.animationPipeline == AnimationPipeline.PropellorVehicle ?
                    GetBoundsMeshFilterFromMainBodyOnly(data) : GetBoundsMeshFilter(data);
            }

            switch (data.animationPipeline)
            {
                case AnimationPipeline.Static: //TODO: Add an option to allow users for a simplified collider instead.
                    foreach (var mf in data.model.GetComponentsInChildren<MeshFilter>())
                    {
                        var mfRb = mf.gameObject.AddComponent<MeshCollider>();
                        mfRb.convex = true;
                    }
                    break;

                case AnimationPipeline.WheeledVehicle:
                    var carCollider = data.model.AddComponent<BoxCollider>();
                    carCollider.size = bounds.size;
                    carCollider.center = bounds.center;
                    break;

                default:
                    var modelCollider = data.model.AddComponent<CapsuleCollider>();
                    bounds = DetermineAxis(bounds, modelCollider);
                    modelCollider.center = bounds.center;

                    switch (modelCollider.direction)
                    {
                        case 0:
                            modelCollider.height = bounds.extents.x * 2;
                            modelCollider.radius = Math.Max(bounds.extents.y, bounds.extents.z);
                            break;
                        case 1:
                            modelCollider.height = bounds.extents.y * 2;
                            modelCollider.radius = Math.Max(bounds.extents.x, bounds.extents.z);
                            break;
                        case 2:
                            modelCollider.height = bounds.extents.z * 2;
                            modelCollider.radius = Math.Max(bounds.extents.x, bounds.extents.y);
                            break;
                    }
                    break;
            }
        }

        /// <summary>
        /// Determines the axis for the capsule collider based on the bounds.
        /// </summary>
        /// <param name="bounds">The bounds of the model.</param>
        /// <param name="modelCollider">The capsule collider to set the axis for.</param>
        /// <returns>The adjusted bounds.</returns>
        private static Bounds DetermineAxis(Bounds bounds, CapsuleCollider modelCollider)
        {
            if (bounds.extents.y > bounds.extents.x)
            {
                modelCollider.direction = bounds.extents.y > bounds.extents.z ? 1 : 2;
            }
            else if (bounds.extents.x > bounds.extents.z)
            {
                modelCollider.direction = 0;
            }
            else modelCollider.direction = 2;
            return bounds;
        }

        /// <summary>
        /// Gets the bounds of the skinned mesh renderer.
        /// </summary>
        /// <param name="data">The model data containing the skinned mesh renderer.</param>
        /// <returns>The bounds of the skinned mesh renderer.</returns>
        private static Bounds GetBoundsSkinnedMeshRenderer(ModelData data)
        {
            // Have to do a complex process because the root bone is rotated
            // which means the automatically generated bounds for the SMR
            // are also rotated, causing them to be inaccurate.
            var skinnedRenderer = data.model.GetComponentInChildren<SkinnedMeshRenderer>();
            const float waistShoulderRatio = 1.5f;
            var rot = skinnedRenderer.rootBone.localRotation;
            skinnedRenderer.rootBone.localRotation = Quaternion.Euler(0, 0, 0);
            skinnedRenderer.updateWhenOffscreen = true;
            skinnedRenderer.sharedMesh.RecalculateBounds();

            Vector3 center = skinnedRenderer.sharedMesh.bounds.center;
            Vector3 extents = new Vector3(skinnedRenderer.sharedMesh.bounds.extents.x,
                skinnedRenderer.sharedMesh.bounds.extents.y, skinnedRenderer.sharedMesh.bounds.extents.z);

            // for bipeds, we need to adjust the bounds because they use the mesh neutral pose
            if (data.json.type.ToLower() == "biped_human")
            {
                extents.x = extents.z;
                extents.z *= waistShoulderRatio;
            }

            var bounds = skinnedRenderer.bounds;
            bounds.center = center;
            bounds.extents = extents;

            skinnedRenderer.updateWhenOffscreen = false;
            skinnedRenderer.rootBone.localRotation = rot;
            return bounds;
        }

        /// <summary>
        /// Gets the bounds of the mesh filters.
        /// </summary>
        /// <param name="data">The model data containing the mesh filters.</param>
        /// <returns>The bounds of the mesh filters.</returns>
        private static Bounds GetBoundsMeshFilter(ModelData data)
        {
            var meshFilters = data.model.GetComponentsInChildren<MeshFilter>();
            var bounds = GetObjectBounds(meshFilters);
            return bounds;
        }

        /// <summary>
        /// Gets the bounds of the main body of the model.
        /// </summary>
        /// <param name="data">The model data containing the main body mesh filter.</param>
        /// <returns>The bounds of the main body mesh filter.</returns>
        private static Bounds GetBoundsMeshFilterFromMainBodyOnly(ModelData data)
        {
            var meshFilters = new[] { data.model.GetComponentInChildren<MeshFilter>() };
            var bounds = GetObjectBounds(meshFilters);
            return bounds;
        }

        /// <summary>
        /// Adds a rigidbody to the model based on the specified parameters.
        /// </summary>
        /// <param name="data">The model data containing rigidbody parameters.</param>
        private static void AddRigidbody(ModelData data)
        {
            var rb = data.model.AddComponent<Rigidbody>();
            rb.mass = data.json.mass;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        /// <summary>
        /// Gets the combined bounds of the specified mesh filters.
        /// </summary>
        /// <param name="meshFilters">The mesh filters to calculate the bounds for.</param>
        /// <returns>The combined bounds of the mesh filters.</returns>
        private static Bounds GetObjectBounds(MeshFilter[] meshFilters)
        {
            var totalBounds = new Bounds(Vector3.zero, Vector3.zero);
            var meshCenter = Vector3.zero;

            foreach (var mFilter in meshFilters)
            {
                var mMesh = mFilter.sharedMesh;
                meshCenter += mMesh.bounds.center;
            }

            meshCenter /= meshFilters.Length;
            totalBounds.center = meshCenter;

            foreach (var mFilter in meshFilters)
            {
                var mMesh = mFilter.sharedMesh;
                if (totalBounds.size == Vector3.zero)
                    totalBounds = mMesh.bounds;
                else
                    totalBounds.Encapsulate(mMesh.bounds);
            }

            return totalBounds;
        }

        /// <summary>
        /// Sets the movement data properties for the model.
        /// </summary>
        /// <param name="movementData">The movement data container to set the properties for.</param>
        /// <param name="data">The model data containing movement parameters.</param>
        private static void SetMovementDataProperties(MovementDataContainer movementData, ModelData data)
        {
            movementData.speedScalar = GetSpeedScalar(data.model);
            movementData.behaviourType = data.defaultBehaviourType;

            if (!ModelDimensionsUtility.TryGetDimensions(data.model.transform, out var extents, out var center))
            {
                return;
            }

            movementData.extents = extents;
            movementData.center = center;
        }

        /// <summary>
        /// Gets the speed scalar for the model based on its movement data.
        /// </summary>
        /// <param name="modelGo">The model GameObject.</param>
        /// <returns>The speed scalar for the model.</returns>
        private static float GetSpeedScalar(GameObject modelGo)
        {
            if (modelGo.TryGetComponent<ModelDataInspector>(out var inspector))
            {
                if (inspector.movement != null && inspector.movement.Count > 0)
                {
                    var averageScale = 0f;
                    foreach (var measurement in inspector.movement)
                    {
                        averageScale += measurement.value;
                    }

                    averageScale /= inspector.movement.Count;
                    return averageScale / 50;
                }
            }

            return 1;
        }
    }
}