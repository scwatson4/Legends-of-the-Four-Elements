namespace AnythingWorld.Utilities.Data
{
    public static class RequestParamsConverter
    {
        public static RequestParams FromRequestParamsObject(RequestParamObject oldParams)
        {
            var newParams = new RequestParams();

            newParams
                .SetUseLegacyAnimatorInEditor(oldParams.useLegacyAnimatorInEditor)
                .SetAddBehaviour(oldParams.addBehaviour)
                .SetAddCollider(oldParams.addCollider)
                .SetAddRigidbody(oldParams.addRigidbody)
                .SetSerializeAssets(oldParams.serializeAsset)
                .SetPlaceOnGrid(oldParams.placeOnGrid)
                .SetPlaceOnGround(oldParams.placeOnGround)
                .SetUseGridArea(oldParams.useGridArea)
                .SetAnimateModel(oldParams.animateModel)
                .SetUseNavMesh(oldParams.useNavMesh)
                .SetModelCaching(oldParams.cacheModel)
                .SetScaleMultiplier(oldParams.scaleMultiplier)
                .SetScaleType(oldParams.scaleType)
                .SetTransformSpace(oldParams.transformSpace)
                .SetParent(oldParams.parentTransform)
                .SetRotation(oldParams.rotation)
                .SetOnSuccessAction(oldParams.onSuccessAction)
                .SetOnSuccessAction(oldParams.onSuccessActionCallback)
                .SetOnFailAction(oldParams.onFailAction)
                .SetOnFailAction(oldParams.onFailActionCallback);

            if (oldParams.clampDbScale)
            {
                var lowerBounds = oldParams.clampDbScaleLowerBounds.value;
                var upperBounds = oldParams.clampDbScaleUpperBounds.value;
                newParams.SetClampDatabaseScale(lowerBounds, upperBounds);
            }

            if (oldParams.position.IsSet)
                newParams.SetPosition(oldParams.position.value);

            if (oldParams.scale.IsSet)
                newParams.SetScale(oldParams.scale.value);

            if (oldParams.monoBehaviours != null && oldParams.monoBehaviours.Length > 0)
                newParams.SetCustomScripts(oldParams.monoBehaviours);

            if (oldParams.categorizedBehaviours != null && oldParams.categorizedBehaviours.Count > 0)
                newParams.SetCategorizedBehaviours(oldParams.categorizedBehaviours);

            if (oldParams.setDefaultBehaviourPreset)
            {
                newParams.SetDefaultBehaviourPreset(oldParams.defaultBehaviourPreset);
            }
            else
            {
                newParams.SetDefaultBehaviour(false);
            }
            
            return newParams;
        }
    }
}
