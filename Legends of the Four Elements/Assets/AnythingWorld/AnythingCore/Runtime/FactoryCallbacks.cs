using AnythingWorld.Utilities;
using AnythingWorld.Utilities.Data;
using System;
using UnityEngine;

namespace AnythingWorld.Core
{
    /// <summary>
    /// Provides callback methods for handling model data loading and processing in the factory
    /// depending on request type.
    /// </summary>
    public static class FactoryCallbacks
    {
        /// <summary>
        /// Sets references to callback actions that can be called from the factory.
        /// </summary>
        /// <param name="data">The model data containing callback actions.</param>
        public static void Subscribe(ModelData data)
        {
            if (data.requestType == RequestType.Json)
            {
                data.actions.onFailure = OnJsonLoadFailure;
                data.actions.onFailureException = OnJsonLoadFailure;
            }
            else
            {
                data.actions.onFailure = OnFailure;
                data.actions.onFailureException = OnFailure;
            }

            data.actions.onSuccess.Add(OnSuccess);
            data.actions.factoryDebug = OnFactoryDebug;
        }

        /// <summary>
        /// Handles failure during model data loading.
        /// </summary>
        /// <param name="data">The model data that failed to load.</param>
        /// <param name="e">The exception that occurred.</param>
        /// <param name="message">The failure message.</param>
        private static void OnFailure(ModelData data, Exception e, string message)
        {
            Debug.LogException(e);
            OnFailure(data, message);
        }

        /// <summary>
        /// Handles failure during model data loading.
        /// </summary>
        /// <param name="data">The model data that failed to load.</param>
        /// <param name="message">The failure message.</param>
        private static void OnFailure(ModelData data, string message)
        {
            // Run the user defined actions for failure
            foreach (var action in data.actions.onFailureUserActions)
            {
                action?.Invoke();
            }

            foreach (var action in data.actions.onFailureUserParamActions)
            {
                action?.Invoke(new CallbackInfo(data, message));
            }

            // Display log for failed object creation
            Debug.LogWarning($"Failed to make {data.searchTerm}: {message}");

            data.isModelProcessingStopped = true;

            // Destroy game object
            Destroy.GameObject(data.model);
        }

        /// <summary>
        /// Handles failure to load existing JSON
        /// by restarting model processing pipeline with requesting a new JSON from server.
        /// </summary>
        /// <param name="data">The model data that failed to load.</param>
        /// <param name="e">The exception that occurred.</param>
        /// <param name="message">The failure message.</param>
        private static void OnJsonLoadFailure(ModelData data, Exception e, string message)
        {
            Debug.LogException(e);
            OnJsonLoadFailure(data, message);
        }

        /// <summary>
        /// Handles failure to load existing JSON
        /// by restarting model processing pipeline with requesting a new JSON from server.
        /// </summary>
        /// <param name="data">The model data that failed to load.</param>
        /// <param name="message">The failure message.</param>
        private static void OnJsonLoadFailure(ModelData data, string message)
        {
            Debug.LogWarning($"Failed to make {data.searchTerm} from JSON: {message}, retrying via search");
            data.requestType = RequestType.Search;
            data.loadedData = new LoadedData();
            data.json = null;
            data.actions.onFailure = OnFailure;
            data.actions.onFailureException = OnFailure;
            data.isModelProcessingStopped = true;
            data.actions.onJsonLoadFailure?.Invoke(data);
        }

        /// <summary>
        /// Handles success during model data loading.
        /// </summary>
        /// <param name="data">The model data that was successfully loaded.</param>
        /// <param name="message">The success message.</param>
        private static void OnSuccess(ModelData data, string message = null)
        {
            foreach (var action in data.actions.onSuccessUserActions)
            {
                action?.Invoke();
            }
            foreach (var action in data.actions.onSuccessUserParamActions)
            {
                action?.Invoke(new CallbackInfo(data, message));
            }
            data.Debug(message);
        }

        /// <summary>
        /// Handles debug messages for the factory.
        /// </summary>
        /// <param name="data">The model data containing debug information.</param>
        /// <param name="message">The debug message.</param>
        private static void OnFactoryDebug(ModelData data, string message = null)
        {
            if (AnythingSettings.DebugEnabled)
            {
                Debug.Log($"Debug ({data?.guid}): {message}", data?.model);
            }
        }
    }
}