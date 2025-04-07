using AnythingWorld.Utilities;
using AnythingWorld.Utilities.Data;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace AnythingWorld.Networking
{
    public static class ThumbnailRequester
    {
        public static async UniTask LoadThumbnailBatchAsync(List<SearchResult> searchResults, int batchSize = 20)
        {
            var chunks = searchResults.ChunkBy(batchSize);
            foreach (var chunk in chunks)
            {
                await LoadThumbnailsBatchAsync(chunk.ToArray());
            }
        }

        public static async UniTask LoadThumbnailsIndividuallyAsync(List<SearchResult> searchResultArray, Action onComplete, object owner)
        {
            foreach (var result in searchResultArray)
            {
                await GetThumbnailAsync(result, onComplete);
            }
        }

        public static async UniTask GetThumbnailAsync(SearchResult result, Action onComplete)
        {
            using (var www = UnityWebRequestTexture.GetTexture(result.thumbnailUrl))
            {

                try
                {
                    await www.SendWebRequest();

                }
                catch (Exception e)
                {
                    if(AnythingSettings.DebugEnabled)
                        Debug.LogWarning("thumbnail was not generated " + e.Message);
                }
                if (www.result == UnityWebRequest.Result.Success)
                {
                    var myTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                    result.Thumbnail = myTexture;
                    result.ResultHasThumbnail = true;

                }
                else
                {
                    result.ResultHasThumbnail = false;
                }
            }
            onComplete?.Invoke();
        }

        public static async UniTask LoadThumbnailsBatchAsync(SearchResult[] searchResultArray)
        {
            var requests = new List<UnityWebRequestAsyncOperation>(searchResultArray.Length);
            foreach (var result in searchResultArray)
            {
                var www = UnityWebRequestTexture.GetTexture(result.thumbnailUrl);
                requests.Add(www.SendWebRequest());
            }

            await UniTask.WhenAll(requests.Select(r => r.ToUniTask()));

            HandleAllRequestsWhenFinished(requests, searchResultArray);

            foreach (var request in requests)
            {
                try
                {
                    request.webRequest.Dispose();
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Problem disposing of async web request");
                    Debug.LogException(e);
                }
            }
        }

        private static void HandleAllRequestsWhenFinished(IReadOnlyList<UnityWebRequestAsyncOperation> requests, SearchResult[] searchResult)
        {
            for (var i = 0; i < requests.Count; i++)
            {
                var www = requests[i].webRequest;
                if (www.result == UnityWebRequest.Result.Success)
                {
                    var myTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                    searchResult[i].Thumbnail = myTexture;
                    searchResult[i].ResultHasThumbnail = true;
                }
                else
                {
                    searchResult[i].ResultHasThumbnail = false;
                }
            }
        }
    }
}