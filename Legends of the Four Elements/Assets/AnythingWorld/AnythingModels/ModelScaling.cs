using AnythingWorld.Utilities.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnythingWorld.Models
{
    public static class ModelScaling
    {
        private enum ScalingDimension
        {
            X,
            Y,
            Z,
            None
        }
        
        /// <summary>
        /// Scales the model data and applies transformations if the scale type is parsed successfully.
        /// </summary>
        /// <param name="data">The model data to be scaled.</param>
        /// <param name="onSuccess">The action to be performed on successful scaling.</param>
        public static void Scale(ModelData data)
        {
            //Parse dimension dictionary from db into vector 3
            LoadDimensionsVectorFromDB(data);

            if (ParseScaleType(data))
            {
                ModelPositioning.ApplyTransforms(data);
            }
        }

        /// <summary>
        /// Parses the scale type from the model data and applies the appropriate scaling.
        /// </summary>
        /// <param name="data">The model data containing scale parameters.</param>
        /// <returns>True if the scale type is parsed and applied successfully, otherwise false.</returns>
        private static bool ParseScaleType(ModelData data)
        {
            if (data.parameters.Scale.IsSet)
            {
                data.loadedData.bounds = data.parameters.Scale.value * data.parameters.ScaleMultiplier;
                switch (data.parameters.ScaleType)
                {
                    case Utilities.ScaleType.SetRealWorld:
                        return TryCenterAndSizeObjectToBounds(data, data.loadedData.bounds);

                    case Utilities.ScaleType.ScaleRealWorld:
                        return TryCenterAndSizeObjectToBounds(data, data.loadedData.bounds);

                    case Utilities.ScaleType.Absolute:
                        data.model.transform.localScale = data.loadedData.bounds;
                        return true;
                }
            }
            else
            {
                data.loadedData.bounds = data.loadedData.dbDimensionsVector * data.parameters.ScaleMultiplier;

                return TryCenterAndSizeObjectToBounds(data, data.loadedData.bounds);
            }
            
            return false;
        }

        /// <summary>
        /// Attempts to center and size the object to the target bounding dimensions.
        /// </summary>
        /// <param name="data">The model data containing the object to be scaled.</param>
        /// <param name="targetBoundingDimensions">The target bounding dimensions for the object.</param>
        /// <returns>True if the object is centered and sized successfully, otherwise false.</returns>
        private static bool TryCenterAndSizeObjectToBounds(ModelData data, Vector3 targetBoundingDimensions)
        {
            // targetBoundingDimensions = new Vector3(Minimum(targetBoundingDimensions.x),
            // Minimum(targetBoundingDimensions.y), Minimum(targetBoundingDimensions.z));
            // Get renderers and object bounds from renderers.
            var renderers = data.model.GetComponentsInChildren<Renderer>();
            var objectBounds = GetObjectBounds(renderers);
            var skinnedRenderer = data.model.GetComponentInChildren<SkinnedMeshRenderer>();
            Bounds bounds;

            if (skinnedRenderer)
            {
                var rot = skinnedRenderer.rootBone.localRotation;
                skinnedRenderer.rootBone.localRotation = Quaternion.Euler(0, 0, 0);
                skinnedRenderer.updateWhenOffscreen = true;

                bounds = new Bounds();
                Vector3 center = skinnedRenderer.localBounds.center;
                Vector3 extents = skinnedRenderer.localBounds.extents;

                bounds.center = center;
                bounds.extents = extents;
                skinnedRenderer.updateWhenOffscreen = false;
                skinnedRenderer.localBounds = bounds;
                bounds = skinnedRenderer.bounds;
                skinnedRenderer.rootBone.localRotation = rot;
            }
            else
            {
                var meshFilters = data.model.GetComponentsInChildren<MeshFilter>();
                bounds = GetObjectBounds(meshFilters);
            }

            var relativeScale = CalculateRelativeScaleForDimension(objectBounds, targetBoundingDimensions);
            if (float.IsNaN(relativeScale) || float.IsPositiveInfinity(relativeScale) || 
                float.IsNegativeInfinity(relativeScale))
            {
                // Debug.Log($"Bounds: {objectBounds}, target bounds: {targetBoundingDimensions}");
                string error = $"Error while calculating bounds scale for {data.guid}, " +
                               $"calculated bound was {relativeScale.ToString()}";
                data.actions?.onFailure?.Invoke(data, error);
                return false;
            }

            var pivotCenterDifference = (bounds.center - data.model.transform.position) * relativeScale;

            if (!data.json.preserveOriginalScale)
            {
                data.model.transform.localScale = new Vector3(relativeScale, relativeScale, relativeScale);
            }

            // Center object by moving bounds center down.
            if (!data.json.preserveOriginalPosition)
            {
                data.model.transform.position -= pivotCenterDifference;
            }
            var yOffset = bounds.size.y / 2 * relativeScale;
            data.loadedData.boundsYOffset = yOffset;
            return true;
        }

        #region Utility Operations

        /// <summary>
        /// Calculates the relative scale for the dimension based on current and desired dimensions.
        /// </summary>
        /// <param name="currentDimensions">The current dimensions of the object.</param>
        /// <param name="desiredDimensions">The desired dimensions of the object.</param>
        /// <returns>The relative scale factor.</returns>
        private static float CalculateRelativeScaleForDimension(Vector3 currentDimensions, Vector3 desiredDimensions)
        {
            var maxDesiredDimension = GetObjectScalarDimension(desiredDimensions);
            switch (maxDesiredDimension)
            {
                case ScalingDimension.X:
                    return GetRelativeScale(currentDimensions.x, desiredDimensions.x);
                case ScalingDimension.Y:
                    return GetRelativeScale(currentDimensions.y, desiredDimensions.y);
                case ScalingDimension.Z:
                    return GetRelativeScale(currentDimensions.z, desiredDimensions.z);
                case ScalingDimension.None:
                default:
                    //If scalar is none, default to 1m on the x-axis.
                    return GetRelativeScale(currentDimensions.x, 1);
            }
        }

        /// <summary>
        /// Gets the bounds of the object from the renderers.
        /// </summary>
        /// <param name="renderers">The renderers of the object.</param>
        /// <returns>The size of the bounds.</returns>
        private static Vector3 GetObjectBounds(Renderer[] renderers)
        {
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            foreach (var objRenderer in renderers)
            {
                if (bounds.size == Vector3.zero)
                    bounds = objRenderer.bounds;
                else
                    bounds.Encapsulate(objRenderer.bounds);
            }
            return bounds.size;
        }

        /// <summary>
        /// Gets the bounds of the object from the mesh filters.
        /// </summary>
        /// <param name="meshFilters">The mesh filters of the object.</param>
        /// <returns>The bounds of the object.</returns>
        private static Bounds GetObjectBounds(MeshFilter[] meshFilters)
        {
            var totalBounds = new Bounds(Vector3.zero, Vector3.zero);
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
        /// Loads the dimensions vector from the database and applies clamping if necessary.
        /// </summary>
        /// <param name="data">The model data containing the dimensions.</param>
        private static void LoadDimensionsVectorFromDB(ModelData data)
        {
            data.loadedData.dbDimensionsVector = ParseDimension(data.json.scale);
            if (data.parameters.ClampDbScale)
            {
                data.loadedData.dbDimensionsVector = ClampVector3(data.loadedData.dbDimensionsVector,
                    data.parameters.ClampDbScaleLowerBounds.value, data.parameters.ClampDbScaleUpperBounds.value);
            }
        }

        /// <summary>
        /// Clamp vector value-wise between two Vector bounds.
        /// </summary>
        /// <param name="input">Values to clamp.</param>
        /// <param name="lower">Lower bounds of values.</param>
        /// <param name="upper">Upper bounds of values.</param>
        /// <returns>The clamped vector.</returns>
        private static Vector3 ClampVector3(Vector3 input, Vector3 lower, Vector3 upper)
        {
            Vector3 output = Vector3.zero;
            output.x = Mathf.Clamp(input.x, lower.x, upper.x);
            output.y = Mathf.Clamp(input.y, lower.y, upper.y);
            output.z = Mathf.Clamp(input.z, lower.z, upper.z);
            return output;
        }

        /// <summary>
        /// Gets the relative scale factor between the current and desired dimensions.
        /// </summary>
        /// <param name="currentDimension">The current dimension of the object.</param>
        /// <param name="desiredDimension">The desired dimension of the object.</param>
        /// <returns>The relative scale factor.</returns>
        private static float GetRelativeScale(float currentDimension, float desiredDimension)
        {
            return desiredDimension / currentDimension;
        }

        /// <summary>
        /// Gets the scalar dimension of the object based on the maximum dimension.
        /// </summary>
        /// <param name="dimensions">The dimensions of the object.</param>
        /// <returns>The scalar dimension.</returns>
        private static ScalingDimension GetObjectScalarDimension(Vector3 dimensions)
        {
            var maxDimension = Math.Max(dimensions.z, Math.Max(dimensions.x, dimensions.y));

            if (maxDimension == 0)
            {
                return ScalingDimension.None;
            }

            if (Mathf.Approximately(maxDimension, dimensions.x))
            {
                return ScalingDimension.X;
            }

            if (Mathf.Approximately(maxDimension, dimensions.y))
            {
                return ScalingDimension.Y;
            }

            if (Mathf.Approximately(maxDimension, dimensions.z))
            {
                return ScalingDimension.Z;
            }

            return ScalingDimension.None;
        }

        /// <summary>
        /// Parses the dimension dictionary into a Vector3.
        /// </summary>
        /// <param name="dictionary">The dictionary containing dimension values.</param>
        /// <returns>The parsed dimensions as a Vector3.</returns>
        private static Vector3 ParseDimension(Dictionary<string, float> dictionary)
        {
            Vector3 dimensions = Vector3.one;
            dictionary.TryGetValue("width", out dimensions.x);
            dictionary.TryGetValue("height", out dimensions.y);
            dictionary.TryGetValue("length", out dimensions.z);

            dimensions = SetMinimumDimensions(dimensions);

            return dimensions;
        }

        /// <summary>
        /// Sets the minimum dimensions for the vector.
        /// </summary>
        /// <param name="vector">The vector to set minimum dimensions for.</param>
        /// <returns>The vector with minimum dimensions set.</returns>
        private static Vector3 SetMinimumDimensions(Vector3 vector)
        {
            return new Vector3(Minimum(vector.x), Minimum(vector.y), Minimum(vector.z));
        }

        /// <summary>
        /// Gets the minimum value for the dimension.
        /// </summary>
        /// <param name="value">The value to get the minimum for.</param>
        /// <returns>The minimum value.</returns>
        private static float Minimum(float value)
        {
            // Minimum dimension of 20cm
            const float minimumDimension = 0.2f;
            return Mathf.Max(minimumDimension, value);
        }

        #endregion Utility Operations
    }
}