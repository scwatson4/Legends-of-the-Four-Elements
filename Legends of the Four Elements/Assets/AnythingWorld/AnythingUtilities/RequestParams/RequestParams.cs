using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AnythingWorld.Utilities.Data
{
    [Serializable]
    public class RequestParams
    {
        // By default, we will use the standard animation system to load models in Editor,
        // and the legacy version to load at runtime. This is due to the fact that the modern animation system
        // doesn't allow runtime loading.
        
        public bool UseLegacyAnimatorInEditor { get; private set; }
        public bool ClampDbScale { get; private set; }
        public bool AddBehaviour { get; private set; } = true;
        public bool AddCollider { get; private set; }
        public bool AddRigidbody { get; private set; }
        public bool SerializeAssets { get; private set; }
        public bool PlaceOnGrid { get; private set; }
        public bool PlaceOnGround { get; private set; }
        public bool UseGridArea { get; private set; }
        public bool AnimateModel { get; private set; } = true;
        public bool UseNavMesh { get; private set; }
        public bool CacheModel { get; private set; } = true;
        public Vector3Param Position { get; private set; } = new Vector3Param();
        public Vector3Param Scale { get; private set; } = new Vector3Param();
        public Vector3Param ClampDbScaleUpperBounds { get; private set; } = new Vector3Param();
        public Vector3Param ClampDbScaleLowerBounds { get; private set; } = new Vector3Param();
        public Quaternion Rotation { get; private set; } = Quaternion.identity;
        public float ScaleMultiplier { get; private set; } = 1;
        public ScaleType ScaleType { get; private set; } = ScaleType.SetRealWorld;
        public TransformSpace TransformSpace { get; private set; } = TransformSpace.Local;
        public Transform ParentTransform { get; private set; }
        public Action OnSuccessAction { get; private set; }
        public Action OnFailAction { get; private set; }
        public Action<CallbackInfo> OnSuccessActionCallback { get; private set; }
        public Action<CallbackInfo> OnFailActionCallback { get; private set; }
        public Type[] MonoBehaviours { get; private set; }
        public Dictionary<DefaultBehaviourType, Type> CategorizedBehaviours { get; private set; }
        public bool ApplyDefaultBehaviourPreset { get; private set; } = true;
        public DefaultBehaviourPreset DefaultBehaviourPreset { get; private set; } 
        
        /// <summary>
        /// Add legacy animator to animated models if requesting them in Editor.
        /// </summary>
        /// <param name="value"></param>
        public RequestParams SetUseLegacyAnimatorInEditor(bool value)
        {
            UseLegacyAnimatorInEditor = value;
            return this;
        }
        
        /// <summary>
        /// If using scales from DB, clamp the scale between certain values. Defaults to unclamped.
        /// </summary>
        public RequestParams SetClampDatabaseScale(Vector3 lowerBound, Vector3 upperBound)
        {
            ClampDbScale = true;
            ClampDbScaleLowerBounds = new Vector3Param(lowerBound);
            ClampDbScaleUpperBounds = new Vector3Param(upperBound);
            return this;
        }
        
        /// <summary>
        /// If using scales from DB, clamp the scale between certain values. Defaults to unclamped.
        /// </summary>
        public RequestParams SetClampDatabaseScale(Vector3 upperBound)
        {
            ClampDbScale = true;
            ClampDbScaleLowerBounds = new Vector3Param(Vector3.zero);
            ClampDbScaleUpperBounds = new Vector3Param(upperBound);
            return this;
        }

        /// <summary>
        /// Add Behaviour to object.
        /// </summary>
        /// <param name="value"></param>
        public RequestParams SetAddBehaviour(bool value)
        {
            AddBehaviour = value;
            return this;
        }

        /// <summary>
        /// Add collider around object that encloses object mesh(es).
        /// </summary>
        /// <param name="value"></param>
        public RequestParams SetAddCollider(bool value)
        {
            AddCollider = value;
            return this;
        }

        /// <summary>
        /// Add Rigidbody to object.
        /// </summary>
        /// <param name="value"></param>
        public RequestParams SetAddRigidbody(bool value)
        {
            AddRigidbody = value;
            return this;
        }

        /// <summary>
        /// Serialize model assets and put them into Assets/SavedAssets folder on loading completion.
        /// </summary>
        public RequestParams SetSerializeAssets(bool value)
        {
            SerializeAssets = value;
            return this;
        }
        
        /// <summary>
        /// Specify if model should be spawned on a grid.
        /// </summary>
        /// <param name="value"></param>
        public RequestParams SetPlaceOnGrid(bool value)
        {
            PlaceOnGrid = value;
            return this;
        }

        /// <summary>
        /// Specify if model should be spawned on a grid.
        /// </summary>
        /// <param name="value"></param>
        public RequestParams SetPlaceOnGround(bool value)
        {
            PlaceOnGround = value;
            return this;
        }

        /// <summary>
        /// Specify if the grid alignement uses an area from an object.
        /// </summary>
        /// <param name="value"></param>
        public RequestParams SetUseGridArea(bool value)
        {
            UseGridArea = value;
            return this;
        }

        /// <summary>
        /// Request model with animation system if available.
        /// </summary>
        public RequestParams SetAnimateModel(bool value)
        {
            AnimateModel = value;
            return this;
        }

        /// <summary>
        /// Add NavMeshAgent to model and use NavMesh for pathfinding.
        /// </summary>
        /// <param name="value"></param>
        public RequestParams SetUseNavMesh(bool value)
        {
            UseNavMesh = value;
            return this;
        }

        /// <summary>
        /// Cache model after import and instantiate from cache on subsequent imports.
        /// </summary>
        public RequestParams SetModelCaching(bool value)
        {
            CacheModel = value;
            return this;
        }

        /// <summary>
        /// Position of created model.
        /// </summary>
        /// <param name="positionVector">Vector3 value that will be set to object transform Position.</param>
        public RequestParams SetPosition(Vector3 positionVector)
        {
            Position = new Vector3Param(positionVector);
            return this;
        }
        
        /// <summary>
        /// Position of created model.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public RequestParams SetPosition(int x, int y, int z)
        {
            Position = new Vector3Param(new Vector3(x, y, z));
            return this;
        }

        /// <summary>
        /// Set model scale with vector.
        /// </summary>
        /// <param name="value"></param>
        public RequestParams SetScale(Vector3 value)
        {
            Scale = new Vector3Param(value);
            return this;
        }
        
        /// <summary>
        /// Set model scale with separate axis values.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public RequestParams SetScale(float x, float y, float z)
        {
            Scale = new Vector3Param(new Vector3(x, y, z));
            return this;
        }

        /// <summary>
        /// Set rotation of created model with Quaternion.
        /// </summary>
        /// <param name="quaternionRotation">Quaternion rotation value to be assigned to model rotation.</param>
        public RequestParams SetRotation(Quaternion quaternionRotation)
        {
            Rotation = quaternionRotation;
            return this;
        }
        
        /// <summary>
        /// Set rotation of created model with euler angles.
        /// </summary>
        /// <param name="eulerRotation">Euler rotation to apply to model.</param>
        public RequestParams SetRotation(Vector3 eulerRotation)
        {
            Rotation = Quaternion.Euler(eulerRotation);
            return this;
        }
        
        /// <summary>
        /// Set rotation of created model with euler angles.
        /// </summary>
        /// <param name="x">Euler angle for x axis.</param>
        /// <param name="y">Euler angle for y axis.</param>
        /// <param name="z">Euler angle for z axis.</param>
        public RequestParams SetRotation(int x, int y, int z)
        {
            Rotation = Quaternion.Euler(x, y, z);
            return this;
        }

        /// <summary>
        /// Multiply the default or defined scale value by this value.
        /// </summary>
        /// <param name="value"></param>
        public RequestParams SetScaleMultiplier(float value)
        {
            ScaleMultiplier = value;
            return this;
        }

        /// <summary>
        /// Set type of scaling operation to apply to model.
        /// </summary>
        /// <param name="scaleType"></param>
        public RequestParams SetScaleType(ScaleType scaleType)
        {
            ScaleType = scaleType;
            return this;
        }

        /// <summary>
        /// Specify which transform space input rotation and position will be applied to.
        /// </summary>
        /// <param name="space"></param>
        public RequestParams SetTransformSpace(TransformSpace space)
        {
            TransformSpace = space;
            return this;
        }
        
        /// <summary>
        /// Parent the model to a parent transform.
        /// </summary>
        /// <param name="parentTransform">Transform that model will be parented to.</param>
        public RequestParams SetParent(Transform parentTransform)
        {
            ParentTransform = parentTransform;
            return this;
        }

        /// <summary>
        /// Action that will be called on successful model creation.
        /// </summary>
        /// <param name="action">Function to be invoked.</param>
        public RequestParams SetOnSuccessAction(Action action)
        {
            OnSuccessAction = action;
            return this;
        }
        
        /// <summary>
        /// Action that will be called on successful model creation that is passed CallbackInfo
        /// object as a parameter.
        /// </summary>
        /// <param name="action"></param>
        public RequestParams SetOnSuccessAction(Action<CallbackInfo> action)
        {
            OnSuccessActionCallback = action;
            return this;
        }

        /// <summary>
        /// Action called on failed model creation.
        /// </summary>
        /// <param name="action">Function to be invoked.</param>
        public RequestParams SetOnFailAction(Action action)
        {
            OnFailAction = action;
            return this;
        }

        /// <summary>
        /// Action that will be called on failed model creation that is passed CallbackInfo
        /// object as a parameter.
        /// </summary>
        /// <param name="action"></param>
        public RequestParams SetOnFailAction(Action<CallbackInfo> action)
        {
            OnFailActionCallback = action;
            return this;
        }

        /// <summary>
        /// Specify an array of behaviour scripts to be added to model on completion.
        /// </summary>
        /// <param name="types">Must be script deriving from Monobehaviour.</param>
        public RequestParams SetCustomScripts(params Type[] types)
        {
            var monoBehaviourType = typeof(MonoBehaviour);

            foreach (var type in types)
            {
                if (type.IsSubclassOf(monoBehaviourType)) continue;

                Debug.LogError($"The script of type {type.Name} provided in AnythingMaker.Make() " +
                               "doesn't inherit from MonoBehaviour. User scripts will not be added");
                return this;
            }
            
            MonoBehaviours = types;
            return this;
        }
        
        /// <summary>
        /// Specify an array of behaviour scripts to be added to model on completion.
        /// </summary>
        /// <param name="monoBehaviours"></param>
        public RequestParams SetCustomScripts(params MonoBehaviour[] monoBehaviours)
        {
            var typeArray = monoBehaviours.Select(x => x.GetType()).ToArray();
            MonoBehaviours = typeArray;
            return this;
        }
        
        /// <summary>
        /// Specify scripts that correspond to different categories of models' behaviours.
        /// </summary>
        /// <param name="behaviours"></param>
        /// <returns></returns>
        public RequestParams SetCategorizedBehaviours(Dictionary<DefaultBehaviourType, Type> behaviours)
        {
            var monoBehaviourType = typeof(MonoBehaviour);

            foreach (var behaviour in behaviours.Values)
            {
                if (behaviour.IsSubclassOf(monoBehaviourType)) continue;

                Debug.LogError($"The script of type {behaviour.Name} provided in AnythingMaker.Make() " +
                               "doesn't inherit from MonoBehaviour. User scripts will not be added");
                return this;
            }
            
            CategorizedBehaviours = behaviours;
            return this;
        }

        /// <summary>
        /// Finds first instance of RuntimeDefaultBehaviours and applies to model.
        /// </summary>
        public RequestParams SetDefaultBehaviour(bool addDefault)
        {
            if (addDefault)
            {
                ApplyDefaultBehaviourPreset = true;
                DefaultBehaviourPreset = ScriptableObject.CreateInstance<DefaultBehaviourPreset>();
            }
            else
            {
                ApplyDefaultBehaviourPreset = false;
            }
            return this;
        }

        /// <summary>
        /// Specify a RuntimeDefaultBehaviour instance to apply to this model.
        /// </summary>
        /// <param name="behaviourPreset"></param>
        public RequestParams SetDefaultBehaviourPreset(DefaultBehaviourPreset behaviourPreset)
        {
            DefaultBehaviourPreset = behaviourPreset;
            ApplyDefaultBehaviourPreset = true;
            return this;
        }
    }
}
