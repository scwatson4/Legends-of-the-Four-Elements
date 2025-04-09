using AnythingWorld.Utilities;
using AnythingWorld.Utilities.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AnythingWorld
{
    [Obsolete("ParamBuilder has been deprecated, instantiate RequestParams and set params using Set methods.")]
    public static class ParamBuilder{}

    [Obsolete("RequestParameter has been deprecated, instantiate RequestParams and set params using Set methods.")]
    public static class RequestParameter
    {
        internal static Utilities.ScaleType _scaleType = Utilities.ScaleType.SetRealWorld;
        internal static Utilities.TransformSpace _transformSpace = Utilities.TransformSpace.Local;
        internal static DefaultBehaviourPreset _defaultBehaviourPreset = null;
        internal static bool _useLegacyAnimatorInEditor = false;
        internal static bool _addBehaviour = true;
        internal static bool _addCollider = false;
        internal static bool _addRigidbody = false;
        internal static bool _clampDbScale = false;
        internal static bool _serializeAsset = false;
        internal static bool _animateModel = true;
        internal static bool _cacheModel = true;
        internal static bool _placeOnGrid = false;
        internal static bool _placeOnGround = false;
        internal static bool _useGridArea = false;
        internal static bool _setDefaultBehaviourPreset = true;
        internal static Vector3Param _clampDbScaleUpperBounds = new Vector3Param();
        internal static Vector3Param _clampDbScaleLowerBounds = new Vector3Param();
        internal static Vector3Param _position = new Vector3Param();
        internal static Quaternion _rotation = Quaternion.identity;
        internal static Vector3Param _scale = new Vector3Param();
        internal static float _scaleMultiplier = 1;
        internal static Transform _parentTransform = null;
        internal static Action _onSuccessAction = null;
        internal static Action _onFailAction = null;
        internal static Action<CallbackInfo> _onSuccessActionCallback = null;
        internal static Action<CallbackInfo> _onFailActionCallback = null;
        internal static Type[] _behaviours = null;
        internal static Dictionary<DefaultBehaviourType, Type> _categorizedBehaviours = null;

        /// <summary>
        /// Package user inputs into object and reset Request class.
        /// </summary>
        /// <returns>RequestParamObject holding user parameter inputs.</returns>
        internal static RequestParamObject Fetch()
        {
            var rPO = new RequestParamObject
            {
                cacheModel = _cacheModel,
                scaleType = _scaleType,
                transformSpace = _transformSpace,
                defaultBehaviourPreset = _defaultBehaviourPreset,
                useLegacyAnimatorInEditor = _useLegacyAnimatorInEditor,
                addBehaviour = _addBehaviour,
                addCollider = _addCollider,
                addRigidbody = _addRigidbody,
                clampDbScale = _clampDbScale,
                serializeAsset = _serializeAsset,
                animateModel = _animateModel,
                placeOnGrid = _placeOnGrid,
                placeOnGround = _placeOnGround,
                useGridArea = _useGridArea,
                setDefaultBehaviourPreset = _setDefaultBehaviourPreset,
                clampDbScaleUpperBounds = _clampDbScaleUpperBounds,
                clampDbScaleLowerBounds = _clampDbScaleLowerBounds,
                position = _position,
                rotation = _rotation,
                scale = _scale,
                scaleMultiplier = _scaleMultiplier,
                parentTransform = _parentTransform,
                onSuccessAction = _onSuccessAction,
                onFailAction = _onFailAction,
                onSuccessActionCallback = _onSuccessActionCallback,
                onFailActionCallback = _onFailActionCallback,
                monoBehaviours = _behaviours,
                categorizedBehaviours = _categorizedBehaviours
            };
            Reset();
            return rPO;
        }

        /// <summary>
        /// Reset all fields in this class.
        /// </summary>
        internal static void Reset()
        {
            _scaleType = AnythingWorld.Utilities.ScaleType.SetRealWorld;
            _transformSpace = Utilities.TransformSpace.Local;
            _defaultBehaviourPreset = null;
            _useLegacyAnimatorInEditor = false;
            _addBehaviour = true;
            _addCollider = false;
            _addRigidbody = false;
            _clampDbScale = false;
            _serializeAsset = false;
            _animateModel = true;
            _placeOnGrid = false;
            _placeOnGround = false;
            _useGridArea = false;
            _cacheModel = true;
            _setDefaultBehaviourPreset = true;
            _clampDbScaleUpperBounds = new Vector3Param();
            _clampDbScaleLowerBounds = new Vector3Param();
            _position = new Vector3Param();
            _rotation = Quaternion.identity;
            _scale = new Vector3Param();
            _scaleMultiplier = 1;
            _parentTransform = null;
            _onSuccessAction = null;
            _onFailAction = null;
            _onSuccessActionCallback = null;
            _onFailActionCallback = null;
            _behaviours = null;
            _setDefaultBehaviourPreset = true;
            _categorizedBehaviours = null;
        }


        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static RequestParameterOption ScaleType(Utilities.ScaleType scaleType)
        {
            return new ScaleType(scaleType);
        }

        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static RequestParameterOption UseLegacyAnimatorInEditor(bool value)
        {
            return new LegacyAnimatorInEditorOption(value);
        }

        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static RequestParameterOption ClampDatabaseScale(Vector3 lowerBound, Vector3 upperBound)
        {
            return new ClampScale(lowerBound, upperBound);
        }

        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static RequestParameterOption ClampDatabaseScale(Vector3 upperBound)
        {
            return new ClampScale(Vector3.zero, upperBound);
        }

        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static RequestParameterOption SetDefaultBehaviour(bool addDefault = true)
        {
            return new DefaultBehaviour(addDefault);
        }

        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static RequestParameterOption SetDefaultBehaviourPreset(DefaultBehaviourPreset behaviourPreset)
        {
            return new DefaultBehaviour(behaviourPreset);
        }

        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static RequestParameterOption TransformSpace(Utilities.TransformSpace space)
        {
            return new TransformSpace(space);
        }

        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static RequestParameterOption Parent(Transform parentTransform)
        {
            return new ParentTransform(parentTransform);
        }

        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static RequestParameterOption IsAnimated(bool value)
        {
            return new AnimateModel(value);
        }

        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static AddBehaviour AddBehaviour()
        {
            return new AddBehaviour();
        }

        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static AddBehaviour AddBehaviour(bool value)
        {
            return new AddBehaviour(value);
        }

        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static RequestParameterOption IsModelCachingEnabled(bool value)
        {
            return new CacheModel(value);
        }

        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static AddCollider AddCollider()
        {
            return new AddCollider();
        }

        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static AddCollider AddCollider(bool value)
        {
            return new AddCollider(value);
        }

        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static AddRigidbody AddRigidbody()
        {
            return new AddRigidbody();
        }

        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static AddRigidbody AddRigidbody(bool value)
        {
            return new AddRigidbody(value);
        }

        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static RequestParameterOption AddScripts(params Type[] types)
        {
            var monoBehaviourType = typeof(MonoBehaviour);

            foreach (var type in types)
            {
                if (type.IsSubclassOf(monoBehaviourType)) continue;

                Debug.LogError($"The script of type {type.Name} provided in AnythingMaker.Make() " +
                               "doesn't inherit from MonoBehaviour. User scripts will not be added");
                return null;
            }
            return new AdditionalScripts(types);
        }

        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static RequestParameterOption AddScripts(params MonoBehaviour[] monobehaviours)
        {
            return new AdditionalScripts(monobehaviours);
        }

        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static RequestParameterOption PlaceOnGrid(bool value)
        {
            return new PlaceOnGrid(value);
        }

        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static RequestParameterOption UseGridArea(bool value)
        {
            return new UseGridArea(value);
        }

        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static RequestParameterOption PlaceOnGround(bool value)
        {
            return new PlaceOnGround(value);
        }

        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static RequestParameterOption Rotation(Quaternion quaternionRotation)
        {
            return new Rotation(quaternionRotation);
        }

        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static RequestParameterOption Rotation(Vector3 eulerRotation)
        {
            return new Rotation(eulerRotation);
        }

        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static RequestParameterOption Rotation(int x, int y, int z)
        {
            return new Rotation(x, y, z);
        }

        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static RequestParameterOption Scale(Vector3 value)
        {
            return new Scale(value);
        }

        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static RequestParameterOption Scale(int x, int y, int z)
        {
            return new Scale(x, y, z);
        }

        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static RequestParameterOption ScaleMultiplier(float value)
        {
            return new ScaleMultiplier(value);
        }

        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static RequestParameterOption Position(Vector3 positionVector)
        {
            return new Position(positionVector);
        }

        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static RequestParameterOption Position(int x, int y, int z)
        {
            return new Position(x, y, z);
        }

        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static RequestParameterOption SerializeAsset()
        {
            return new SerializeAsset();
        }
        
        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static RequestParameterOption SerializeAsset(bool value)
        {
            return new SerializeAsset(value);
        }
        
        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static RequestParameterOption OnSuccessAction(Action action)
        {
            return new OnSuccessAction(action);
        }
        
        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static RequestParameterOption OnSuccessAction(Action<CallbackInfo> action)
        {
            return new OnSuccessAction(action);
        }
        
        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static RequestParameterOption OnFailAction(Action action)
        {
            return new OnFailureAction(action);
        }
        
        [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
        public static RequestParameterOption OnFailAction(Action<CallbackInfo> action)
        {
            return new OnFailureAction(action);
        }
    }

    [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
    public class RequestParam { }

    [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
    public class RequestParameterOption { }

    [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
    public class ClampScale : RequestParameterOption
    {
        public ClampScale(Vector3 lowerBound, Vector3 upperBound)
        {
            RequestParameter._clampDbScale = true;
            RequestParameter._clampDbScaleLowerBounds = new Vector3Param(lowerBound);
            RequestParameter._clampDbScaleUpperBounds = new Vector3Param(upperBound);
        }
    }

    [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
    public class LegacyAnimatorInEditorOption : RequestParameterOption
    {
        public LegacyAnimatorInEditorOption(bool value)
        {
            RequestParameter._useLegacyAnimatorInEditor = value;
        }
    }

    [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
    public class DefaultBehaviour : RequestParameterOption
    {
        public DefaultBehaviour()
        {
            RequestParameter._defaultBehaviourPreset = ScriptableObject.CreateInstance<DefaultBehaviourPreset>();
            RequestParameter._setDefaultBehaviourPreset = true;
        }
        public DefaultBehaviour(DefaultBehaviourPreset behaviourPreset)
        {
            RequestParameter._defaultBehaviourPreset = behaviourPreset;
            RequestParameter._setDefaultBehaviourPreset = true;
        }
        public DefaultBehaviour(bool _bool)
        {
            RequestParameter._defaultBehaviourPreset = ScriptableObject.CreateInstance<DefaultBehaviourPreset>();
            RequestParameter._setDefaultBehaviourPreset = _bool;
        }
    }
    
    [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
    public class CacheModel : RequestParameterOption
    {
        public CacheModel(bool value)
        {
            RequestParameter._cacheModel = value;
        }
    }

    [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
    public class AddBehaviour : RequestParameterOption
    {
        public AddBehaviour()
        {
            RequestParameter._addBehaviour = true;
        }
        public AddBehaviour(bool value)
        {
            RequestParameter._addBehaviour = value;
        }
    }
    
    [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
    public class PlaceOnGround : RequestParameterOption
    {
        public PlaceOnGround()
        {
            RequestParameter._placeOnGround = true;
        }
        public PlaceOnGround(bool value)
        {
            RequestParameter._placeOnGround = value;
        }
    }
    
    [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
    public class AddCollider : RequestParameterOption
    {
        public AddCollider()
        {
            RequestParameter._addCollider = true;
        }
        public AddCollider(bool value)
        {
            RequestParameter._addCollider = value;
        }
    }
    
    [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
    public class AddRigidbody : RequestParameterOption
    {
        public AddRigidbody()
        {
            RequestParameter._addRigidbody = true;
        }
        public AddRigidbody(bool value)
        {
            RequestParameter._addRigidbody = value;
        }
    }
    
    [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
    public class SerializeAsset : RequestParameterOption
    {
        public SerializeAsset()
        {
            RequestParameter._serializeAsset = true;
        }
        public SerializeAsset(bool value)
        {
            RequestParameter._serializeAsset = value;
        }
    }
    
    [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
    public class PlaceOnGrid : RequestParameterOption
    {
        public PlaceOnGrid(bool value)
        {
            RequestParameter._placeOnGrid = value;
        }
    }
    
    [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
    public class UseGridArea : RequestParameterOption
    {
        public UseGridArea(bool value)
        {
            RequestParameter._useGridArea = value;
        }
    }
    
    [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
    public class TransformSpace : RequestParameterOption
    {
        public TransformSpace(Utilities.TransformSpace space)
        {
            RequestParameter._transformSpace = space;
        }
    }
    
    [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
    public class ParentTransform : RequestParameterOption
    {
        public ParentTransform(GameObject gameObject)
        {
            RequestParameter._parentTransform = gameObject.transform;
        }

        public ParentTransform(Transform transform)
        {
            RequestParameter._parentTransform = transform;
        }
    }
    
    [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
    public class AdditionalScripts : RequestParameterOption
    {
        public AdditionalScripts(Dictionary<DefaultBehaviourType, Type> behaviours)
        {
            var monoBehaviourType = typeof(MonoBehaviour);

            foreach (var behaviour in behaviours.Values)
            {
                if (behaviour.IsSubclassOf(monoBehaviourType)) continue;

                Debug.LogError($"The script of type {behaviour.Name} provided in AnythingMaker.Make() " +
                               "doesn't inherit from MonoBehaviour. User scripts will not be added");
                return;
            }
            RequestParameter._categorizedBehaviours = behaviours;
        }

        public AdditionalScripts(Type[] behaviours)
        {
            RequestParameter._behaviours = behaviours;
        }

        public AdditionalScripts(MonoBehaviour[] behaviours)
        {
            //Convert array of monobehaviours to their type values
            var typeArray = behaviours.Select(x => x.GetType()).ToArray();
            RequestParameter._behaviours = typeArray;
        }
    }

    [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
    public class AnimateModel : RequestParameterOption
    {
        public AnimateModel(bool value)
        {
            RequestParameter._animateModel = value;
        }
    }

    [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
    public class Rotation : RequestParameterOption
    {
        public Rotation(Quaternion quaternionRotation)
        {
            RequestParameter._rotation = quaternionRotation;
        }

        public Rotation(Vector3 eulerRotation)
        {
            RequestParameter._rotation = Quaternion.Euler(eulerRotation);
        }

        /// <summary>
        /// Set rotation using Euler angles.
        /// </summary>
        /// <param name="x">Euler angle for x axis.</param>
        /// <param name="y">Euler angle for y axis.</param>
        /// <param name="z">Euler angle for z axis.</param>
        public Rotation(int x, int y, int z)
        {
            RequestParameter._rotation = Quaternion.Euler(x, y, z);
        }
    }

    [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
    public class Position : RequestParameterOption
    {
        public Position(Vector3 value)
        {
            RequestParameter._position = new Vector3Param(value);
        }

        public Position(int x, int y, int z)
        {
            RequestParameter._position = new Vector3Param(new Vector3(x, y, z));
        }
    }

    [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
    public class Scale : RequestParameterOption
    {
        public Scale(Vector3 value)
        {
            RequestParameter._scale = new Vector3Param(value);
        }

        public Scale(int x, int y, int z)
        {
            RequestParameter._scale = new Vector3Param(new Vector3(x, y, z));
        }
        public Scale(float x, float y, float z)
        {
            RequestParameter._scale = new Vector3Param(new Vector3(x, y, z));
        }

    }
    
    [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
    public class ScaleMultiplier : RequestParameterOption
    {
        public ScaleMultiplier(float scalar)
        {
            RequestParameter._scaleMultiplier = scalar;
        }
    }

    [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
    public class ScaleType : RequestParameterOption
    {
        public ScaleType(Utilities.ScaleType value)
        {
            RequestParameter._scaleType = value;
        }
    }

    [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
    public class OnSuccessAction : RequestParameterOption
    {
        public OnSuccessAction(Action value)
        {
            RequestParameter._onSuccessAction = value;
        }

        /// Returns callback info from model (guid, linked object, message)
        public OnSuccessAction(Action<CallbackInfo> value)
        {
            RequestParameter._onSuccessActionCallback = value;
        }
    }

    [Obsolete("Deprecated. Instantiate RequestParams instead and use corresponding Set method.")]
    public class OnFailureAction : RequestParameterOption
    {
        public OnFailureAction(Action value)
        {
            RequestParameter._onFailAction = value;
        }

        /// <summary>
        /// Returns callback info from model (guid, linked object, message)
        /// </summary>
        /// <param name="value"></param>
        public OnFailureAction(Action<CallbackInfo> value)
        {
            RequestParameter._onFailActionCallback = value;
        }
    }
}
