using AnythingWorld.Utilities;
using AnythingWorld.Utilities.Data;
using UnityEngine;

namespace AnythingWorld.Models
{
    /// <summary>
    /// Provides methods to apply transformations to models.
    /// </summary>
    public static class ModelPositioning
    {
        /// <summary>
        /// Applies various transformations to the model based on the provided model data.
        /// </summary>
        /// <param name="data">The model data containing transformation parameters.</param>
        public static void ApplyTransforms(ModelData data)
        {
            ApplyParentTransform(data.model, data.parameters.ParentTransform);
            ApplyRotationAccordingToSpace(data);

            if (data.parameters.Position.value != Vector3.zero || !data.parameters.PlaceOnGrid)
            {
#if UNITY_EDITOR
                // this is a drag and drop model getting the position
                data.model.transform.position = TransformSettings.DragPosition;
#endif
                ApplyPositionAccordingToSpace(data);
                AdjustGroundModelPositioning(data);
            }
            else
            {
                AdjustIntraModelPositioning(data);
                ApplyGridPositionAccordingToSpace(data);
                if (!data.parameters.UseGridArea)
                    AdjustGroundModelPositioning(data);
            }
        }

        /// <summary>
        /// Adjusts the model's position relative to the ground.
        /// </summary>
        /// <param name="data">The model data containing positioning parameters.</param>
        private static void AdjustIntraModelPositioning(ModelData data)
        {
            if (data.parameters.PlaceOnGround)
            {
                data.model.transform.localPosition += new Vector3(0, data.loadedData.boundsYOffset, 0);
            }
        }

        /// <summary>
        /// Adjusts the model's position to ensure it is placed on the ground.
        /// </summary>
        /// <param name="data">The model data containing positioning parameters.</param>
        private static void AdjustGroundModelPositioning(ModelData data)
        {
            if (data.parameters.PlaceOnGround)
            {
                RaycastHit hit;

                // SphereCast to get the ground is more accurate than RayCast
                if (Physics.SphereCast(data.model.transform.position + Vector3.up * 5, 0.2f, Vector3.down, out hit, 10f))
                {
                    float lowestY = data.model.GetComponentInChildren<Renderer>().bounds.min.y;
                    foreach (Renderer child in data.model.GetComponentsInChildren<Renderer>())
                    {
                        if (child != null)
                        {
                            float childLowestY = child.bounds.min.y;
                            if (childLowestY < lowestY)
                            {
                                lowestY = childLowestY;
                            }
                        }
                    }

                    var distance = hit.point.y - lowestY;

                    data.model.transform.localPosition = new Vector3(data.model.transform.localPosition.x,
                        hit.point.y + distance, data.model.transform.localPosition.z);
                }
                else
                {
                    data.model.transform.localPosition += new Vector3(0, data.loadedData.boundsYOffset, 0);
                }
            }
        }

        /// <summary>
        /// Applies grid positioning to the model based on the provided model data.
        /// </summary>
        /// <param name="data">The model data containing grid positioning parameters.</param>
        private static void ApplyGridPositionAccordingToSpace(ModelData data)
        {
            Vector3 gridPosition;

            if (!data.parameters.UseGridArea)
                gridPosition = SimpleGrid.AddCell();
            else
                gridPosition = GridArea.AddModel(data.model);

            switch (data.parameters.TransformSpace)
            {
                case Utilities.TransformSpace.World:
                    ApplyWorldSpacePosition(data.model, gridPosition);
                    break;

                case Utilities.TransformSpace.Local:
                    ApplyLocalSpacePosition(data.model, gridPosition);
                    break;
            }
        }

        /// <summary>
        /// Applies position to the model based on the provided model data.
        /// </summary>
        /// <param name="data">The model data containing position parameters.</param>
        private static void ApplyPositionAccordingToSpace(ModelData data)
        {
            switch (data.parameters.TransformSpace)
            {
                case Utilities.TransformSpace.World:
                    ApplyWorldSpacePosition(data.model, data.parameters.Position.value);
                    break;

                case Utilities.TransformSpace.Local:
                    ApplyLocalSpacePosition(data.model, data.parameters.Position.value);
                    break;
            }
        }

        /// <summary>
        /// Applies rotation to the model based on the provided model data.
        /// </summary>
        /// <param name="data">The model data containing rotation parameters.</param>
        private static void ApplyRotationAccordingToSpace(ModelData data)
        {
            switch (data.parameters.TransformSpace)
            {
                case Utilities.TransformSpace.World:
                    ApplyWorldSpaceRotation(data.model, data.parameters.Rotation);
                    break;

                case Utilities.TransformSpace.Local:
                    ApplyLocalSpaceRotation(data.model, data.parameters.Rotation);
                    break;
            }
        }

        /// <summary>
        /// Applies local space position to the model.
        /// </summary>
        /// <param name="model">The model GameObject.</param>
        /// <param name="position">The position to apply.</param>
        private static void ApplyLocalSpacePosition(GameObject model, Vector3 position)
        {
            model.transform.localPosition = position;
        }

        /// <summary>
        /// Applies local space rotation to the model.
        /// </summary>
        /// <param name="model">The model GameObject.</param>
        /// <param name="rotation">The rotation to apply.</param>
        private static void ApplyLocalSpaceRotation(GameObject model, Quaternion rotation)
        {
            model.transform.localRotation = rotation;
        }

        /// <summary>
        /// Applies world space position to the model.
        /// </summary>
        /// <param name="model">The model GameObject.</param>
        /// <param name="position">The position to apply.</param>
        private static void ApplyWorldSpacePosition(GameObject model, Vector3 position)
        {
            model.transform.position = position;
        }

        /// <summary>
        /// Applies world space rotation to the model.
        /// </summary>
        /// <param name="model">The model GameObject.</param>
        /// <param name="rotation">The rotation to apply.</param>
        private static void ApplyWorldSpaceRotation(GameObject model, Quaternion rotation)
        {
            model.transform.rotation = rotation;
        }

        /// <summary>
        /// Applies parent transform to the model.
        /// </summary>
        /// <param name="model">The model GameObject.</param>
        /// <param name="parentTransform">The parent transform to apply.</param>
        private static void ApplyParentTransform(GameObject model, Transform parentTransform)
        {
            model.transform.parent = parentTransform;
        }
    }
}