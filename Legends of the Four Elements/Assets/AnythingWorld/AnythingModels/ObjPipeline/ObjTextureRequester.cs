using System;
using UnityEngine;
using UnityEngine.Networking;
using AnythingWorld.Utilities.Data;
using Cysharp.Threading.Tasks;

namespace AnythingWorld.Models
{
    public static class ObjTextureRequester
    {
        /// <summary>
        /// Iterates through the Json.Model.Other.Textures array and loads them to a list.
        /// </summary>
        /// <param name="data">Model request data.</param>
        /// <param name="onFail"></param>
        public static async UniTask RequestAsync(ModelData data)
        {
            if (data.json.model.other.texture != null)
            {
                foreach (var textureUrl in data.json.model.other.texture)
                {
                    data.Debug($"Fetching texture from {textureUrl}");
                    using var www = UnityWebRequestTexture.GetTexture(textureUrl);
                    www.timeout = 40;
                    await www.SendWebRequest().ToUniTask();

                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        Texture requestedTexture = DownloadHandlerTexture.GetContent(www);
                        var textureName = Uri.UnescapeDataString(ParseTextureNameFromUrl(textureUrl));
                        data.loadedData.obj.loadedTextures.Add(textureName, requestedTexture);
                    }
                    else
                    {
                        data.actions.onFailure?.Invoke(data, $"Failed to load texture for {data.json.name}: {textureUrl}");
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Parses the name of a texture from the textures URL.
        /// </summary>
        /// <param name="textureAddress">URL of the texture to parse.</param>
        /// <returns>Returns parsed texture name (including file format) eg. basecolor.png</returns>
        private static string ParseTextureNameFromUrl(string textureAddress)
        {
            try
            {
                if (textureAddress.Contains("?"))
                {
                    var splitAddress = textureAddress.Split('?');
                    var addressWithTexName = splitAddress[0];
                    var splitAddressWithTexName = addressWithTexName.Split('/');
                    var textureName = splitAddressWithTexName[splitAddressWithTexName.Length - 1];
                    return textureName;
                }

                Debug.LogError($"No name parameter detected in texture URL: {textureAddress}");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error: trying to load texture name from texture address:{textureAddress}");
                Debug.LogException(e);
                return null;
            }
        }
    }
}
