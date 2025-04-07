using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace AnythingWorld.Networking.Editor
{
    public static class AnimateAnythingEditorProcessor
    {
        public static void GetThumbnailFromWeb(RiggingCategoryDetails details, Action action)
        {
            GetThumbnailAsync(details, action).Forget();
        }

        /// <summary>
        /// The coroutine responsible for getting the thumbnails for the rigging constraint cards.
        /// </summary>
        /// <param name="details">The category to get the thumbnail for</param>
        /// <param name="onComplete">The action to call when the thumbnail has been received</param>
        /// <returns></returns>
        private static async UniTask GetThumbnailAsync(RiggingCategoryDetails details, Action onComplete)
        {
            try
            {
                using (var www = UnityWebRequestTexture.GetTexture(details.thumbnailURL))
                {
                    await www.SendWebRequest().ToUniTask();

                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        var texture = DownloadHandlerTexture.GetContent(www);
                        details.thumbnail = texture;
                    }
                    else
                    {
                        Debug.LogError($"Failed to download thumbnail from {details.thumbnailURL}: {www.error}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"An exception occurred while getting the thumbnail with URL: {details.thumbnailURL}, " +
                               $"error: {ex.Message}");
            }
            finally
            {
                onComplete?.Invoke();
            }
        }
    }

    public class RiggingCategoryDetails
    {
        public string thumbnailURL;
        public Texture2D thumbnail;
        public bool available;
    }

    public class RiggingMainCategoryDetails : RiggingCategoryDetails
    {
        public Dictionary<string, RiggingCategoryDetails> subcategoryDetails;
    }
}