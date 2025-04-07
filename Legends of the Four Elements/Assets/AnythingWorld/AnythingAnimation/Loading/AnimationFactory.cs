using AnythingWorld.Utilities.Data;
using AnythingWorld.Utilities;
using AnythingWorld.Animation.Vehicles;

namespace AnythingWorld.Animation
{
    public static class AnimationFactory
    {
        /// <summary>
        /// Start process for loading animation components and controllers depending on the animation pipeline.
        /// </summary>
        /// <param name="data">The model data containing animation pipeline information.</param>
        public static void Load(ModelData data)
        {
            if (data.isCachedModelLoaded && data.animationPipeline != AnimationPipeline.Rigged)
            {
                return;
            }

            switch (data.animationPipeline)
            {
                case AnimationPipeline.Static:
                    break;
                case AnimationPipeline.Rigged:
                    StartRiggedAnimationPipeline(data);
                    break;
                case AnimationPipeline.WheeledVehicle:
                    StartWheeledVehicleAnimation(data);
                    break;
                case AnimationPipeline.PropellorVehicle:
                    StartPropellorVehicleAnimation(data);
                    break;
                case AnimationPipeline.Shader:
                    StartShaderAnimationPipeline(data);
                    break;
            }
        }

        /// <summary>
        /// Starts the rigged animation pipeline for the model.
        /// </summary>
        /// <param name="data">The model data containing animation pipeline information.</param>
        private static void StartRiggedAnimationPipeline(ModelData data)
        {
            data.Debug($"StartRiggedAnimationPipeline called for {data.guid}");
            AnimationClipLoader.Load(data);
        }

        /// <summary>
        /// Starts the shader animation pipeline for the model.
        /// </summary>
        /// <param name="data">The model data containing animation pipeline information.</param>
        private static void StartShaderAnimationPipeline(ModelData data)
        {
            data.Debug("StartShaderAnimationPipeline called.");
            AnimationShaderLoader.Load(data);
        }

        /// <summary>
        /// Starts the wheeled vehicle animation pipeline for the model.
        /// </summary>
        /// <param name="data">The model data containing animation pipeline information.</param>
        private static void StartWheeledVehicleAnimation(ModelData data)
        {
            data.Debug($"StartVehicleAnimationPipeline called for {data.guid}");
            WheeledVehicleAnimationLoader.Load(data);
        }

        /// <summary>
        /// Starts the propellor vehicle animation pipeline for the model.
        /// </summary>
        /// <param name="data">The model data containing animation pipeline information.</param>
        private static void StartPropellorVehicleAnimation(ModelData data)
        {
            data.Debug($"StartVehicleAnimationPipeline called for {data.guid}");
            VehiclePropellorAnimationLoader.Load(data);
        }
    }
}