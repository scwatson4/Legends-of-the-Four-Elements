using System;
using System.Diagnostics;
using AnythingWorld.Animation;
using AnythingWorld.Behaviour;
using AnythingWorld.Models;
using AnythingWorld.Networking;
using AnythingWorld.Utilities;
using AnythingWorld.Utilities.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AnythingWorld.Core
{
    /// <summary>
    /// Provides methods to request and process models based on various criteria.
    /// </summary>
    public static class AnythingFactory
    {
        /// <summary>
        /// Requests a model that matches the search term.
        /// </summary>
        /// <param name="searchTerm">Search term to find the closest match to.</param>
        /// <param name="userParams">User parameters for the request.</param>
        /// <returns>The anchor GameObject for the requested model.</returns>
        public static GameObject RequestModel(string searchTerm, RequestParams userParams)
        {
            var data = ConstructModelDataContainer(searchTerm, userParams);
            var anchorGameObject = PrepareAndBeginModelRequest(data);
            return anchorGameObject;
        }

        /// <summary>
        /// Requests a model using pre-fetched JSON data.
        /// </summary>
        /// <param name="json">The JSON data for the model.</param>
        /// <param name="userParams">User parameters for the request.</param>
        /// <returns>The anchor GameObject for the requested model.</returns>
        public static GameObject RequestModel(ModelJson json, RequestParams userParams)
        {
            var data = ConstructModelDataContainer(json, userParams);
            var anchorGameObject = PrepareAndBeginModelRequest(data);
            return anchorGameObject;
        }

        /// <summary>
        /// Requests all models that were processed by the user.
        /// </summary>
        /// <param name="dataIn">The JSON data for the processed model.</param>
        /// <param name="requestParams">User parameters for the request.</param>
        /// <returns>The anchor GameObject for the requested model.</returns>
        public static GameObject RequestProcessedModel(ModelJson dataIn, RequestParams requestParams)
        {
            var data = ConstructModelDataContainer(dataIn, requestParams);
            data.requestType = RequestType.Processed;

            var anchorGameObject = PrepareAndBeginModelRequest(data);
            return anchorGameObject;
        }

        /// <summary>
        /// Requests a model using its ID.
        /// </summary>
        /// <param name="id">The ID of the model.</param>
        /// <param name="userParams">User parameters for the request.</param>
        /// <returns>The anchor GameObject for the requested model.</returns>
        public static GameObject RequestModelById(string id, RequestParams userParams)
        {
            var data = ConstructModelDataContainerById(id, userParams);
            var anchorGameObject = PrepareAndBeginModelRequest(data);
            return anchorGameObject;
        }

        /// <summary>
        /// Prepares model loading by verifying the render pipeline, creating an anchor GameObject,
        /// and starting the loading process asynchronously.
        /// </summary>
        /// <param name="data">The model data for the request.</param>
        /// <returns>The anchor GameObject for the requested model.</returns>
        private static GameObject PrepareAndBeginModelRequest(ModelData data)
        {
            if (!UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline)
            {
                Debug.LogWarning("Warning: Standard RP detected, HDRP or URP must be installed to use Anything World.");
                return null;
            }

            var anchorGameObject = CreateModelGameObject(data);

            UserCallbacks.Subscribe(data);
            FactoryCallbacks.Subscribe(data);

            // Assign the failure callback to restart the model request
            data.actions.onJsonLoadFailure = async modelData => await RestartModelRequest(modelData);

            // Start processing the model request asynchronously
            ProcessModelRequest(data).Forget();

            return anchorGameObject;
        }

        /// <summary>
        /// Restarts the model request after a failure.
        /// </summary>
        /// <param name="data">The model data for the request.</param>
        private static async UniTask RestartModelRequest(ModelData data)
        {
            data.isModelProcessingStopped = false;
            await ProcessModelRequest(data);
        }

        /// <summary>
        /// Processes the model request based on the request type specified in the ModelData.
        /// </summary>
        /// <param name="data">The model data for the request.</param>
        private static async UniTask ProcessModelRequest(ModelData data)
        {
            try
            {
                // Fetch JSON data based on the request type
                switch (data.requestType)
                {
                    case RequestType.Json:
                        // JSON is already provided
                        break;
                    case RequestType.Search:
                        await JsonRequester.FetchJsonAsync(data);
                        break;
                    case RequestType.Processed:
                        await JsonRequester.FetchProcessedJsonAsync(data);
                        break;
                    case RequestType.Id:
                        await JsonRequester.FetchJsonByIdAsync(data);
                        break;
                }

                if (data.isModelProcessingStopped)
                {
                    return;
                }

                // Process JSON data
                JsonProcessor.ProcessData(data);

                // Load the model asynchronously
                await ModelLoader.LoadAsync(data);
                if (data.isModelProcessingStopped)
                {
                    return;
                }

                // Scale the model
                ModelScaling.Scale(data);
                if (data.isModelProcessingStopped)
                {
                    return;
                }

                // Load animations and add behaviors
                AnimationFactory.Load(data);
                BehaviourHandler.AddBehaviours(data);

                // Finalize the model processing
                ModelPostProcessing.FinishMakeProcess(data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"An error occurred during model processing: {ex.Message}");
            }
        }

        /// <summary>
        /// Constructs a model data container and sets the search term.
        /// </summary>
        /// <param name="searchTerm">The search term linked to this ModelData container.</param>
        /// <param name="userParams">User parameters for the request.</param>
        /// <returns>The constructed ModelData container.</returns>
        private static ModelData ConstructModelDataContainer(string searchTerm, RequestParams userParams)
        {
            var data = new ModelData
            {
                searchTerm = searchTerm,
                requestType = RequestType.Search,
                parameters = userParams ?? new RequestParams()
            };

            return data;
        }

        /// <summary>
        /// Constructs a model data container and sets the JSON data.
        /// </summary>
        /// <param name="json">The JSON data for the model.</param>
        /// <param name="userParams">User parameters for the request.</param>
        /// <returns>The constructed ModelData container.</returns>
        private static ModelData ConstructModelDataContainer(ModelJson json, RequestParams userParams)
        {
            var data = new ModelData
            {
                searchTerm = json.name,
                json = json,
                requestType = RequestType.Json,
                parameters = userParams ?? new RequestParams()
            };

            return data;
        }

        /// <summary>
        /// Constructs a model data container and sets the ID.
        /// </summary>
        /// <param name="id">The ID of the model.</param>
        /// <param name="userParams">User parameters for the request.</param>
        /// <returns>The constructed ModelData container.</returns>
        private static ModelData ConstructModelDataContainerById(string id, RequestParams userParams)
        {
            var data = new ModelData
            {
                id = id,
                requestType = RequestType.Id,
                parameters = userParams ?? new RequestParams()
            };

            return data;
        }

        /// <summary>
        /// Creates an "anchor" GameObject returned to the user immediately.
        /// All components and meshes will be added onto or as a child of this GameObject.
        /// </summary>
        /// <param name="data">The model data that will be linked to this GameObject.</param>
        /// <returns>The anchor GameObject.</returns>
        private static GameObject CreateModelGameObject(ModelData data)
        {
            data.model = new GameObject(data.searchTerm);
            return data.model;
        }
    }
}
