using AnythingWorld.Utilities.Data;
using System;

namespace AnythingWorld.Core
{
    public static class UserCallbacks
    {
        /// <summary>
        /// Subscribe users Actions to the onSuccessUser and onFailureUser delegates.
        /// </summary>
        /// <param name="onFailure">Action to be invoked on model creation failure.</param>
        /// <param name="onSuccess">Action to be invoked on model creation success.</param>
        /// <param name="data">Request these actions will be linked to.</param>
        public static void Subscribe(ModelData data)
        {
            data.actions.onSuccessUserActions.Add(data.parameters?.OnSuccessAction);
            data.actions.onFailureUserActions.Add(data.parameters?.OnFailAction);
            data.actions.onSuccessUserParamActions.Add(data.parameters?.OnSuccessActionCallback);
            data.actions.onFailureUserParamActions.Add(data.parameters?.OnFailActionCallback);
        }
    }
}
