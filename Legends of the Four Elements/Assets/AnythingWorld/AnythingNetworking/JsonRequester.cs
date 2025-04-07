using AnythingWorld.Utilities;
using AnythingWorld.Utilities.Data;
using AnythingWorld.Utilities.Networking;
using Cysharp.Threading.Tasks;

using UnityEngine.Networking;

namespace AnythingWorld.Networking
{
    public static class JsonRequester
    {
        /// <summary>
        /// Requests JSON data for a model based on the search term and populates the data container.
        /// </summary>
        /// <param name="data">Model data object</param>
        public static async UniTask FetchJsonAsync(ModelData data)
        {
            if (string.IsNullOrEmpty(AnythingSettings.APIKey))
            {
                data.actions.onFailure?.Invoke(data, $"No API key when attempting to fetch model data for {data.searchTerm}, returning.");
                return;
            }

            if (data.model == null)
            {
                data.actions.onFailure?.Invoke(data, "Object parent has been destroyed, returning.");
                return;
            }

            var uri = NetworkConfig.GetNameEndpointUri(data.searchTerm);
            await RequestAndDeserializeJsonAsync(data, uri);
        }

        /// <summary>
        /// Requests processed JSON data for a model and populates the data container.
        /// </summary>
        /// <param name="data">Model data object</param>
        public static async UniTask FetchProcessedJsonAsync(ModelData data)
        {
            if (string.IsNullOrEmpty(AnythingSettings.APIKey))
            {
                data.actions.onFailure?.Invoke(data, $"No API key when attempting to fetch model data for {data.searchTerm}, returning.");
                return;
            }

            var uri = NetworkConfig.GetUserProcessed(data.json._id);
            await RequestAndDeserializeJsonAsync(data, uri, 10);
        }

        /// <summary>
        /// Requests JSON data for a model using its ID and populates the data container.
        /// </summary>
        /// <param name="data">Model data object</param>
        public static async UniTask FetchJsonByIdAsync(ModelData data)
        {
            if (string.IsNullOrEmpty(AnythingSettings.APIKey))
            {
                data.actions.onFailure?.Invoke(data, $"No API key when attempting to fetch model data for {data.id}, returning.");
                return;
            }

            if (data.model == null)
            {
                data.actions.onFailure?.Invoke(data, "Object parent has been destroyed, returning.");
                return;
            }

            var uri = NetworkConfig.GetIdEndpointUri(data.id);
            data.Debug("Requesting json from " + uri);

            using (var www = UnityWebRequest.Get(uri))
            {
                www.timeout = 30;

                // Send the web request asynchronously and await its completion
                await www.SendWebRequest().ToUniTask();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    var error = new NetworkErrorMessage(www);
                    NetworkErrorHandler.HandleError(error);
                    data.actions.onFailure?.Invoke(data, $"Error fetching model data for {data.id}, returning.");
                    return;
                }

                data.json = DeserializeStringJson(www.downloadHandler.text);
            }
        }

        /// <summary>
        /// Deserializes a JSON string into a ModelJson data container.
        /// </summary>
        /// <param name="stringJson">The JSON string to deserialize.</param>
        /// <returns>The deserialized ModelJson object.</returns>
        public static ModelJson DeserializeStringJson(string stringJson)
        {
            string objectJsonString = TrimJson(stringJson);
            var modelJson = JsonDeserializer.DeserializeModelJson(objectJsonString);
            return modelJson;
        }

        /// <summary>
        /// Requests JSON data from the specified URI and deserializes it into the model data.
        /// </summary>
        /// <param name="data">The model data object to populate with the JSON data.</param>
        /// <param name="uri">The URI to request the JSON data from.</param>
        /// <param name="timeout">The timeout duration for the request in seconds. Default is 30 seconds.</param>
        private static async UniTask RequestAndDeserializeJsonAsync(ModelData data, string uri, int timeout = 30)
        {
            data.Debug("Requesting json from " + uri);

            using (var www = UnityWebRequest.Get(uri))
            {
                www.timeout = timeout;

                // Send the web request asynchronously and await its completion
                await www.SendWebRequest().ToUniTask();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    var error = new NetworkErrorMessage(www);
                    NetworkErrorHandler.HandleError(error);
                    data.actions.onFailure?.Invoke(data, $"Error fetching model data for {data.searchTerm}, returning.");
                    return;
                }

                data.json = DeserializeStringJson(www.downloadHandler.text);
            }
        }

        /// <summary>
        /// Trims JSON string of array brackets if present.
        /// </summary>
        /// <param name="result">The JSON string to trim.</param>
        /// <returns>The trimmed JSON string.</returns>
        private static string TrimJson(string result)
        {
            result = result.TrimStart('[');
            result = result.TrimEnd(']');
            return result;
        }
    }
}
