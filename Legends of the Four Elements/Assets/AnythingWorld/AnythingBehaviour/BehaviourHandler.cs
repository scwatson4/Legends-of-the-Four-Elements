using AnythingWorld.Utilities;
using AnythingWorld.Utilities.Data;
using AnythingWorld.Behaviour.Tree;

using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace AnythingWorld.Behaviour
{
    /// <summary>
    /// Handles the addition of behaviors to models based on specified parameters.
    /// </summary>
    public static class BehaviourHandler
    {
        /// <summary>
        /// Adds behaviors to the model based on the provided model data.
        /// </summary>
        /// <param name="data">The model data containing behavior parameters.</param>
        public static void AddBehaviours(ModelData data)
        {
            if (data.parameters.AddBehaviour)
            {
                var wasDefaultBehaviourSet = false;

                if (data.parameters.CategorizedBehaviours != null)
                {
                    wasDefaultBehaviourSet = TrySetBehaviour(data, data.parameters.CategorizedBehaviours);
                }

                if (data.parameters.ApplyDefaultBehaviourPreset && !wasDefaultBehaviourSet)
                {
                    if (data.parameters.DefaultBehaviourPreset != null)
                    {
                        wasDefaultBehaviourSet = TrySetBehaviourTree(data, data.parameters.DefaultBehaviourPreset);
                    }

                    if (!wasDefaultBehaviourSet && data.animationPipeline != AnimationPipeline.Static)
                    {
                        var firstInstance = Resources.LoadAll<DefaultBehaviourPreset>("").FirstOrDefault();

                        if (firstInstance != null)
                        {
                            wasDefaultBehaviourSet = TrySetBehaviourTree(data, firstInstance);
                        }
                        else
                        {
                            Debug.LogWarning("Couldn't find DefaultBehaviourPreset in Resources to apply to model " +
                                      "(Do you need to create a preset in resources?)");
                        }

                        if (!wasDefaultBehaviourSet)
                        {
                            Debug.LogWarning("Couldn't find a behaviour matching model's DefaultBehaviourType in " +
                                             "DefaultBehaviourPreset to apply to model. " +
                                             "Check if scripts for all behaviour types were set.");
                        }
                    }
                }

                if (data.parameters.MonoBehaviours != null)
                {
                    foreach (var behaviour in data.parameters.MonoBehaviours)
                    {
                        data.model.AddComponent(behaviour);
                    }
                }
            }
            else
            {
                Debug.Log("Skipping Applying Behaviours");
            }
        }

        /// <summary>
        /// Tries to set a behavior on the model based on the provided dictionary.
        /// </summary>
        /// <param name="data">The model data containing the default behavior type.</param>
        /// <param name="dict">A dictionary mapping behavior types to script types.</param>
        /// <returns>True if the behavior was set, otherwise false.</returns>
        private static bool TrySetBehaviour(ModelData data, Dictionary<DefaultBehaviourType, System.Type> dict)
        {
            if (!dict.TryGetValue(data.defaultBehaviourType, out var scriptType))
            {
                return false;
            }

            data.model.AddComponent(scriptType);
            return true;
        }

        /// <summary>
        /// Tries to set a behavior tree on the model based on the provided preset.
        /// </summary>
        /// <param name="data">The model data containing the default behavior type.</param>
        /// <param name="preset">The preset containing behavior rules and tree assets.</param>
        /// <returns>True if the behavior tree was set, otherwise false.</returns>
        private static bool TrySetBehaviourTree(ModelData data, DefaultBehaviourPreset preset)
        {
            foreach (var rule in preset.behaviourRules)
            {
                if (rule.behaviourType != data.defaultBehaviourType)
                {
                    continue;
                }

                data.parameters.SetUseNavMesh(rule.treeAsset.usesNavMesh);
                var behaviourTreeRunner = data.model.AddComponent<BehaviourTreeInstanceRunner>();
                behaviourTreeRunner.behaviourTree = rule.treeAsset;
                return true;
            }
            return false;
        }
    }
}