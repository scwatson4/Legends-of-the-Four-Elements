using AnythingWorld.Utilities.Data;
using System;
using System.Collections;
using Cysharp.Threading.Tasks;
#if UNITY_EDITOR
#endif
using UnityEngine.Networking;

namespace AnythingWorld.Models
{
    public static class ObjMtlRequester
    {
        /// <summary>
        /// Requests the material data for the model from the specified URL and stores it in the model data.
        /// </summary>
        /// <param name="data">The model data object containing the material URL.</param>
        /// <param name="onFail">The action to be performed if the request fails.</param>
        public static async UniTask RequestAsync(ModelData data)
        {
            var mtlUrl = data.json.model.other.material;

            data.Debug($"Fetching material from {mtlUrl}");
            using var www = UnityWebRequest.Get(mtlUrl);
            await www.SendWebRequest().ToUniTask();

            if (www.result == UnityWebRequest.Result.Success)
            {
                data.loadedData.obj.mtlString = www.downloadHandler.data;
            }
            else
            {
                data.actions.onFailure?.Invoke(data, $"Failed to load mtl data for {data.json.name}: {mtlUrl}");
            }
        }
    }
}
