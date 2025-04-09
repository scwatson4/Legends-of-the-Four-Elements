using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using AnythingWorld.Utilities;
using UnityEngine;
using UnityEngine.Networking;
using AnythingWorld.Networking;
using AnythingWorld.Utilities.Data;

namespace AnythingWorld.Core
{
    /// <summary>
    /// A test script for making JSON requests and modifying the received data.
    /// </summary>
    public class JsonRequestTest : MonoBehaviour
    {
        public string requestObject = "cat#0000";

        /// <summary>
        /// Requests JSON data and invokes the provided callback with the result.
        /// </summary>
        /// <param name="callback">The callback to invoke with the JSON data.</param>
        public void RequestJsonWithCallback(Action<ModelJson> callback)
        {
            FetchJsonAsync(requestObject, callback).Forget();
        }

        /// <summary>
        /// Modifies the animation URLs in the JSON data to incorrect values.
        /// </summary>
        /// <param name="json">The JSON data to modify.</param>
        public void IncorrectAnimationUrls(ModelJson json)
        {
            if (json?.model?.rig?.animations != null)
            {
                foreach (var kvp in json.model.rig.animations)
                {
                    kvp.Value.GLB = $"ERASED GLB RIG URL FOR {kvp.Key}";
                }
            }

            AnythingFactory.RequestModel(json, null);
        }

        /// <summary>
        /// Modifies the texture URLs in the JSON data to incorrect values.
        /// </summary>
        /// <param name="json">The JSON data to modify.</param>
        public void IncorrectObjTextureUrls(ModelJson json)
        {
            List<string> modifiedTextureList = new List<string>();
            foreach (var url in json.model.other.texture)
            {
                modifiedTextureList.Add("ERASED TEXTURE TEST URL");
            }
            json.model.other.texture = modifiedTextureList.ToArray();
            AnythingFactory.RequestModel(json, null);
        }

        /// <summary>
        /// Modifies the material URL in the JSON data to an incorrect value.
        /// </summary>
        /// <param name="json">The JSON data to modify.</param>
        public void IncorrectMtlUrl(ModelJson json)
        {
            json.model.other.material = "ERASED MTL TEST URL";
            AnythingFactory.RequestModel(json, null);
        }

        /// <summary>
        /// Modifies the part URLs in the JSON data to incorrect values.
        /// </summary>
        /// <param name="json">The JSON data to modify.</param>
        public void IncorrectPartUrl(ModelJson json)
        {
            Dictionary<string, string> modifiedDictionary = new Dictionary<string, string>();
            foreach (var url in json.model.parts)
            {
                modifiedDictionary.Add(url.Key, "ERASED OBJ PART URL");
            }
            json.model.parts = modifiedDictionary;
            AnythingFactory.RequestModel(json, null);
        }

        /// <summary>
        /// Requests JSON data matching the request term and invokes the callback on success.
        /// </summary>
        /// <param name="requestTerm">The term to search for.</param>
        /// <param name="callback">The callback to invoke on successful completion, requires ModelJson parameter.</param>
        private static async UniTask FetchJsonAsync(string requestTerm, Action<ModelJson> callback)
        {
            var uri = NetworkConfig.GetNameEndpointUri(requestTerm);

            Debug.Log(uri);

            using var www = UnityWebRequest.Get(uri);
            www.timeout = 10;
            await www.SendWebRequest().ToUniTask();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log($"Error fetching model data for {requestTerm}, returning.");
                return;
            }

            ModelJson modelJson = JsonRequester.DeserializeStringJson(www.downloadHandler.text);
            callback?.Invoke(modelJson);
        }
    }
}
