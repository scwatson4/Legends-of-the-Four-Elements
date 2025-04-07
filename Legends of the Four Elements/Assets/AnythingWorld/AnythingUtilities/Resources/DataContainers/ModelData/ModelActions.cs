using System;
using System.Collections.Generic;

namespace AnythingWorld.Utilities.Data
{
    /// <summary>
    /// Represents a collection of actions that can be performed on model data.
    /// </summary>
    [Serializable]
    public class ModelActions
    {
        public Action<ModelData, string> factoryDebug;
        public List<Action<ModelData, string>> onSuccess = new List<Action<ModelData, string>>();
        
        public Action<ModelData> onJsonLoadFailure;
        public Action<ModelData, string> onFailure;
        public Action<ModelData, Exception, string> onFailureException;

        public List<Action> onSuccessUserActions = new List<Action>();
        public List<Action> onFailureUserActions = new List<Action>();

        public List<Action<CallbackInfo>> onSuccessUserParamActions = new List<Action<CallbackInfo>>();
        public List<Action<CallbackInfo>> onFailureUserParamActions = new List<Action<CallbackInfo>>();
    }
}
