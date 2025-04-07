using AnythingWorld.Utilities.Data;
using System;
using System.Collections;
using Cysharp.Threading.Tasks;
#if UNITY_EDITOR
#endif
using UnityEngine;
using UnityEngine.Networking;

namespace AnythingWorld.Models
{
    /// <summary>
    /// Provides methods to request OBJ model bytes from a server.
    /// </summary>
    public static class ObjBytesRequester
    {
        /// <summary>
        /// Requests the bytes for each part of the model asynchronously.
        /// </summary>
        /// <param name="data">The model data containing the parts to request.</param>
        /// <param name="onFail">The action to invoke if the request fails.</param>
        /// <returns>An IEnumerator for coroutine handling.</returns>
        public static async UniTask RequestPartsAsync(ModelData data)
        {
            var parts = data.json.model.parts;

            foreach (var part in parts)
            {
                using (var www = UnityWebRequest.Get(part.Value))
                {
                    await www.SendWebRequest().ToUniTask();

                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        var fetchedBytes = www.downloadHandler.data;
                        data.loadedData.obj.partsBytes.Add(part.Key, fetchedBytes);
                    }
                    else
                    {
                        data.actions.onFailure?.Invoke(data, $"Failed while loading model part \"{part.Key}\" for model \"{data.json.name}\"");
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Requests the bytes for a single static model asynchronously.
        /// </summary>
        /// <param name="data">The model data containing the URI to request.</param>
        /// <param name="onFail">The action to invoke if the request fails.</param>
        /// <returns>An IEnumerator for coroutine handling.</returns>
        public static async UniTask RequestSingleStaticAsync(ModelData data)
        {
            var uri = data.json.model.other.model;

            using (var www = UnityWebRequest.Get(uri))
            {
                await www.SendWebRequest().ToUniTask();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    var fetchedBytes = www.downloadHandler.data;
                    data.loadedData.obj.partsBytes.Add("model", fetchedBytes);
                }
                else
                {
                    data.actions.onFailure?.Invoke(data, $"Failed while loading model part \"model\" for model \"{data.json.name}\"");
                }
            }
        }
    }
}