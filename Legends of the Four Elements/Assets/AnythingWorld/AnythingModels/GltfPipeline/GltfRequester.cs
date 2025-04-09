using AnythingWorld.Utilities.Data;
#if UNITY_EDITOR
#endif
using UnityEngine.Networking;
using System;
using Cysharp.Threading.Tasks;

namespace AnythingWorld.Models
{
    public static class GltfRequester
    {
        /// <summary>
        /// Fetch and load rigged animated model.
        /// </summary>
        /// <param name="data">Model data</param>
        /// <param name="onSuccess"></param>
        public static async UniTask RequestRiggedAnimationBytesAsync(ModelData data, Action<ModelData> onSuccess)
        {
            if (data.model == null)
            {
                data.actions.onFailure?.Invoke(data, "Object parent has been destroyed, returning.");
                return;
            }

            foreach (var kvp in data.json.model.rig.animations)
            {
                using var www = UnityWebRequest.Get(kvp.Value.GLB);
                await www.SendWebRequest().ToUniTask();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    var fetchedBytes = www.downloadHandler.data;
                    data.loadedData.gltf.animationBytes.Add(kvp.Key, fetchedBytes);
                    data.Debug($"Successfully fetched rig bytes from {data.guid} Animation Clip:{kvp.Key} @ {kvp.Value}");
                }
                else
                {
                    data.actions.onFailure?.Invoke(data, $"Failed while loading model animation clip {kvp.Key} for model {data.guid}");
                    return;
                }
            }

            onSuccess?.Invoke(data);
        }
    }
}