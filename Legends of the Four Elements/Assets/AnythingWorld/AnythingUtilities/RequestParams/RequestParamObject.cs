using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnythingWorld.Utilities.Data
{
    [Serializable]
    public class RequestParamObject
    {
        // By default we will use the standard animation system to load models in Editor, and the legacy version to load at runtime.
        // This is due to the fact that the modern animation system doesn't allow runtime loading.
        public bool useLegacyAnimatorInEditor = false;
        public bool clampDbScale = false;
        public bool addBehaviour = false;
        public bool addCollider = false;
        public bool addRigidbody = false;
        public bool serializeAsset = false;
        public bool placeOnGrid = false;
        public bool placeOnGround = false;
        public bool useGridArea = false;
        public bool animateModel = true;
        public bool useNavMesh = false;
        public bool cacheModel = true;
        public Vector3Param position = new Vector3Param();
        public Vector3Param scale = new Vector3Param();
        public Vector3Param clampDbScaleUpperBounds = new Vector3Param();
        public Vector3Param clampDbScaleLowerBounds = new Vector3Param();
        public Quaternion rotation = Quaternion.identity;
        public float scaleMultiplier = 1;
        public ScaleType scaleType;
        public TransformSpace transformSpace;
        public Transform parentTransform;
        public Action onSuccessAction;
        public Action onFailAction;
        public Action<CallbackInfo> onSuccessActionCallback;
        public Action<CallbackInfo> onFailActionCallback;
        public Type[] monoBehaviours;
        public Dictionary<DefaultBehaviourType, Type> categorizedBehaviours;
        public bool setDefaultBehaviourPreset = true;
        public DefaultBehaviourPreset defaultBehaviourPreset = null;
    }
}
