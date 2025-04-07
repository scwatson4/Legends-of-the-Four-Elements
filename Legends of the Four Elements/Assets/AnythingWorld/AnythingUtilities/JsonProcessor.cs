using System;
using AnythingWorld.Utilities;
using AnythingWorld.Utilities.Data;

namespace AnythingWorld.Networking
{
    /// <summary>
    /// Provides methods to process JSON data for model creation.
    /// </summary>
    public static class JsonProcessor
    {
        /// <summary>
        /// Calls methods to do secondary processing on JSON data before further creation steps.
        /// </summary>
        /// <param name="data">The model data to process.</param>
        public static void ProcessData(ModelData data)
        {
            data.Debug("Processing data");
            SetAnimationPipeline(data);
            SetBehaviourType(data);
            SetModelLoadingPipeline(data);
            AddModelInspector(data);
        }

        /// <summary>
        /// Decides the correct animation pipeline from user request and/or JSON data.
        /// Sets the animation pipeline enum based on the outcome.
        /// </summary>
        /// <param name="data">Model data for the request.</param>
        private static void SetAnimationPipeline(ModelData data)
        {
            // If requested to be static or type is static, use static mesh pipeline override
            if (data?.parameters?.AnimateModel == false || data.json.behaviour == "static")
            {
                data.animationPipeline = AnimationPipeline.Static;
            }
            else
            {
                data.animationPipeline = ParseAnimationPipeline(data.json);
            }
        }

        /// <summary>
        /// Sets the behaviour type based on the model data.
        /// </summary>
        /// <param name="data">Model data for the request.</param>
        private static void SetBehaviourType(ModelData data)
        {
            data.defaultBehaviourType = data.parameters.AddBehaviour ? 
                ParseBehaviourType(data.json) : DefaultBehaviourType.Static;
        }

        /// <summary>
        /// Parses the behaviour type from the JSON data.
        /// </summary>
        /// <param name="json">The JSON data to parse.</param>
        /// <returns>The parsed behaviour type.</returns>
        private static DefaultBehaviourType ParseBehaviourType(ModelJson json)
        {
            var animationDictionary = json?.model?.rig?.animations;
            var behaviourType = DefaultBehaviourType.Static;
            if (animationDictionary != null && animationDictionary.Count > 0)
            {
                behaviourType = DefaultBehaviourType.GroundCreature;
            }
            if (json.behaviour == "fly")
            {
                if (json.type.Contains("vehicle"))
                {
                    behaviourType = DefaultBehaviourType.FlyingVehicle;
                }
                else
                {
                    behaviourType = DefaultBehaviourType.FlyingCreature;
                }
            }
            else if (json.behaviour.Contains("swim"))
            {
                if (json.type.Contains("vehicle"))
                {
                    // behaviourType = DefaultBehaviourType.FlyingVehicle;
                }
                else
                {
                    behaviourType = DefaultBehaviourType.SwimmingCreature;
                }
            }
            else if (json.behaviour == "drive")
            {
                behaviourType = DefaultBehaviourType.GroundVehicle;
            }
            else if (json.type == "uniform")
            {
                if (json.behaviour == "static")
                {
                    behaviourType = DefaultBehaviourType.Static;
                }
            }
            return behaviourType;
        }
        
        /// <summary>
        /// Parses the animation pipeline from the JSON data.
        /// </summary>
        /// <param name="json">The JSON data to parse.</param>
        /// <returns>The parsed animation pipeline.</returns>
        public static AnimationPipeline ParseAnimationPipeline(ModelJson json)
        {
            var animationDictionary = json?.model?.rig?.animations;
            // If walk rig URL is present, use rigged pipeline
            if (animationDictionary != null && animationDictionary.Count > 0)
            {
                return AnimationPipeline.Rigged;
            }
            
            // If behaviour is type drive, set animation to drive
            // (lots of different car types, switch to consistent type based)
            if (json.behaviour == "drive")
            {
                return AnimationPipeline.WheeledVehicle;
            }

            return json.type switch
            {
                "vehicle_propeller" => AnimationPipeline.PropellorVehicle,
                // If type is uniform, use shader animation pipeline.
                "uniform" when json.behaviour == "static" => AnimationPipeline.Static,
                "uniform" => AnimationPipeline.Shader,
                _ => AnimationPipeline.Static
            };
        }

        /// <summary>
        /// Sets the model loading pipeline, currently hard linked to animation type but could offer different options in future.
        /// </summary>
        /// <param name="data">Model data for the request.</param>
        private static void SetModelLoadingPipeline(ModelData data)
        {
            switch (data.animationPipeline)
            {
                case AnimationPipeline.Static:
                    data.modelLoadingPipeline = ModelLoadingPipeline.OBJ_Static;
                    break;
                
                case AnimationPipeline.Rigged:
                    data.modelLoadingPipeline = ModelLoadingPipeline.RiggedGLB;
                    break;
                
                case AnimationPipeline.WheeledVehicle:
                case AnimationPipeline.PropellorVehicle:
                    data.modelLoadingPipeline = ModelLoadingPipeline.OBJ_Part_Based;
                    break;
                
                case AnimationPipeline.Shader:
                    data.modelLoadingPipeline = ModelLoadingPipeline.OBJ_Static;
                    break;
                
                case AnimationPipeline.Unset:
                    data.modelLoadingPipeline = ModelLoadingPipeline.Unset;
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(data.animationPipeline),
                        $"Not expected animation pipeline value: {data.animationPipeline}");
            }
        }
        
        /// <summary>
        /// Adds an inspector component to the model game object to allow the user to edit model parameters in the editor.
        /// </summary>
        /// <param name="data">Model data for the request.</param>
        private static void AddModelInspector(ModelData data)
        {
            if (!data.model.TryGetComponent(out ModelDataInspector inspector))
            {
                inspector = data.model.AddComponent<ModelDataInspector>();
            }
            
            inspector.Populate(data);
            bool hasSpeed = false;
            foreach (LabelledFloat lf in inspector.movement)
            {
                if (lf.label.ToLower() == "speed")
                {
                    hasSpeed = true;
                }
            }
            
            // Set a default speed if none is found in the model data (messes animations otherwise)
            if (!hasSpeed)
            {
                inspector.movement.Add(new LabelledFloat("Speed", 42));
            }
            
            data.model.name = data.json.name;
        }
    }
}
