using System;
#if UNITY_EDITOR
using AnythingWorld.AnythingBehaviour.Tree;
#endif
using AnythingWorld.PathCreation;
using AnythingWorld.Utilities;
using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// A MonoBehaviour that serves as a container for movement-related data.
    /// It holds data such as a speed scalar, which can modify movement speed, vector data for extents and center,
    /// and optionally bezier curve path used for calculations related to obstacle avoidance and goal generation.
    /// </summary>
    [Serializable]
    public class MovementDataContainer : MonoBehaviour
    {
#if UNITY_EDITOR
        [ReadOnlyField]
#endif
        public float speedScalar = 1;
        public PathCreator pathCreator;
        [HideInInspector] public DefaultBehaviourType behaviourType;
        [HideInInspector] public Vector3 extents;
        [HideInInspector] public Vector3 center;
        [HideInInspector] public float jumpAnimationDuration;
        [HideInInspector] public float jumpEndAnimationDuration;
    }
}
