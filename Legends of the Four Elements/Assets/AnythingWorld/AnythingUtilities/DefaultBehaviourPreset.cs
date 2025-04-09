using System.Collections.Generic;
using AnythingWorld.Behaviour.Tree;
using UnityEditor;
using UnityEngine;

namespace AnythingWorld.Utilities
{
    [System.Serializable]
    [CreateAssetMenu(fileName = "DefaultBehaviours", menuName = "ScriptableObjects/DefaultBehaviour", order = 1)]
    public class DefaultBehaviourPreset : ScriptableObject
    {
        [SerializeField] public List<BehaviourRule> behaviourRules = new List<BehaviourRule>();
        
        private void OnEnable()
        {
#if UNITY_EDITOR
            AssetDatabase.importPackageCompleted += SetupDefaultBehaviourPreset;
#else
            SetupDefaultBehaviourPreset("");
#endif
        }

        private void SetupDefaultBehaviourPreset(string packageName)
        {
            TransformSettings.GetInstance();
            behaviourRules = new List<BehaviourRule>();
#if UNITY_EDITOR
            if (TransformSettings.GroundCreatureBehaviourTree != null) behaviourRules.Add(new BehaviourRule(DefaultBehaviourType.GroundCreature, TransformSettings.GroundCreatureBehaviourTree));
            if (TransformSettings.GroundVehicleBehaviourTree != null) behaviourRules.Add(new BehaviourRule(DefaultBehaviourType.GroundVehicle, TransformSettings.GroundVehicleBehaviourTree));
            if (TransformSettings.FlyingCreatureBehaviourTree != null) behaviourRules.Add(new BehaviourRule(DefaultBehaviourType.FlyingCreature, TransformSettings.FlyingCreatureBehaviourTree));
            if (TransformSettings.FlyingVehicleBehaviourTree!= null) behaviourRules.Add(new BehaviourRule(DefaultBehaviourType.FlyingVehicle, TransformSettings.FlyingVehicleBehaviourTree));
            if (TransformSettings.SwimmingCreatureBehaviourTree != null) behaviourRules.Add(new BehaviourRule(DefaultBehaviourType.SwimmingCreature, TransformSettings.SwimmingCreatureBehaviourTree));
            if (TransformSettings.StaticBehaviourTree != null) behaviourRules.Add(new BehaviourRule(DefaultBehaviourType.Static, TransformSettings.StaticBehaviourTree));
#endif
        }
    }
    [System.Serializable]
    public class BehaviourRule
    {
        [SerializeField]
        public DefaultBehaviourType behaviourType;
        [SerializeField]
        public BehaviourTree treeAsset;
        public BehaviourRule(DefaultBehaviourType _behaviourType, BehaviourTree _treeAsset)
        {
            behaviourType = _behaviourType;
            treeAsset = _treeAsset;
        }
    }
}
