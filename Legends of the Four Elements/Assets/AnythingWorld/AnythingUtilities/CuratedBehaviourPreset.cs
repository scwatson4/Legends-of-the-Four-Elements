using AnythingWorld.Behaviour.Tree;

using UnityEngine;

namespace AnythingWorld.Behaviour
{
    /// <summary>
    /// Scriptable Object tracking all our curated behaviour trees and their associated object type.
    /// </summary>
    public class CuratedBehaviourPreset : ScriptableObject
    {
        private static CuratedBehaviourPreset instance;
        public static CuratedBehaviourPreset Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load(typeof(CuratedBehaviourPreset).Name) as CuratedBehaviourPreset;
                }
                return instance;
            }
        }

        public int defaultGroundCreatureIndex;
        public BehaviourTreeDropdownOption[] groundCreatureBehaviours;
        public int defaultGroundVehicleIndex;
        public BehaviourTreeDropdownOption[] groundVehicleBehaviours;
        public int defaultFlyingCreatureIndex;
        public BehaviourTreeDropdownOption[] flyingCreatureBehaviours;
        public int defaultFlyingVehicleIndex;
        public BehaviourTreeDropdownOption[] flyingVehicleBehaviours;
        public int defaultSwimmingCreatureIndex;
        public BehaviourTreeDropdownOption[] swimmingCreatureBehaviours;
        public int defaultStaticIndex;
        public BehaviourTreeDropdownOption[] staticBehaviours;
    }

    /// <summary>
    /// Struct holding the details used to display the behaviour trees in the Make Options.
    /// </summary>
    [System.Serializable]
    public struct BehaviourTreeDropdownOption
    {
        public string label;
        public BehaviourTree behaviourTree;
    }
}