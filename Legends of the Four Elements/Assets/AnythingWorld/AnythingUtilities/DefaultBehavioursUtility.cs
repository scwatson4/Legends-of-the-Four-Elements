using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using AnythingWorld.Behaviour.Tree;

namespace AnythingWorld.Utilities
{
    public static class DefaultBehavioursUtility
    {
        /// <summary>
        /// Create an unserialized instance of behaviour preset (runtime only)
        /// </summary>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public static DefaultBehaviourPreset CreateNewTemporaryInstance(Dictionary<DefaultBehaviourType, BehaviourTree> dictionary)
        {
            var asset = ScriptableObject.CreateInstance<DefaultBehaviourPreset>();
            foreach (var tuple in dictionary)
            {
                asset.behaviourRules.Add(new BehaviourRule(tuple.Key, tuple.Value));
            }
            return asset;
        }
#if UNITY_EDITOR
        /// <summary>
        /// Creates a scriptable instance serialized in the asset database.
        /// </summary>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public static DefaultBehaviourPreset CreateSerializedInstance(Dictionary<DefaultBehaviourType, BehaviourTree> dictionary)
        {
            DefaultBehaviourPreset asset;
            var path = "Assets/AnythingWorld/Resources/Settings/DefaultBehaviours.asset";
            if (!InstanceExists())
            {
                asset = ScriptableObject.CreateInstance<DefaultBehaviourPreset>();
                //check if the directory exists
                if (!AssetDatabase.IsValidFolder("Assets/AnythingWorld"))
                {
                    AssetDatabase.CreateFolder("Assets", "AnythingWorld");
                }
                if (!AssetDatabase.IsValidFolder("Assets/AnythingWorld/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets/AnythingWorld", "Resources");
                }
                if (!AssetDatabase.IsValidFolder("Assets/AnythingWorld/Resources/Settings"))
                {
                    AssetDatabase.CreateFolder("Assets/AnythingWorld/Resources", "Settings");
                }
                AssetDatabase.CreateAsset(asset, path);
                foreach (var tuple in dictionary)
                {
                    asset.behaviourRules.Add(new BehaviourRule(tuple.Key, tuple.Value));
                }
            }
            else
            {
                asset = Resources.Load<DefaultBehaviourPreset>("Settings/DefaultBehaviours");
            }

            var behaviourSerializedObject = new SerializedObject(asset);
            var rulesProperty = behaviourSerializedObject.FindProperty("behaviourRules");

            if (rulesProperty.isArray)
            {
                rulesProperty.Next(true); //go to array
                rulesProperty.Next(true); //go to arraySize

                //Array Length
                var arrayLength = rulesProperty.intValue = asset.behaviourRules.Count;

                rulesProperty.Next(true); //go to first element

                int lastIndex = arrayLength - 1;
                for (int i = 0; i < arrayLength; i++)
                {
                    var rule = asset.behaviourRules[i];
                    rulesProperty.FindPropertyRelative("behaviourType").intValue = (int)rule.behaviourType;
                    rulesProperty.FindPropertyRelative("treeAsset").objectReferenceValue = rule.treeAsset;
                    if (i < lastIndex)
                    {
                        rulesProperty.Next(false);
                    }
                }
            }

            behaviourSerializedObject.ApplyModifiedProperties();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        public static bool InstanceExists()
        {
            var path = "Assets/AnythingWorld/Resources/Settings/DefaultBehaviours.asset";
            return AssetDatabase.GetMainAssetTypeAtPath(path) != null;
        }
#endif
    }
}
