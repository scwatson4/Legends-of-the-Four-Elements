using AnythingWorld.Utilities;
using AnythingWorld.Utilities.Data;
using AnythingWorld.Utilities.Networking;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace AnythingWorld.Networking.Editor
{
    /// <summary>
    /// Class for fetching user processed models from the database and converting them into AWThing data format.
    /// </summary>
    public static class UserProcessedModels
    {
        //reuse the search result class for the user processed models
        public delegate void SearchCompleteDelegate(SearchResult[] results);
        private static SearchCompleteDelegate searchDelegate;

        public delegate void RefreshCompleteDelegate(CollectionResult result);
        private static RefreshCompleteDelegate refreshDelegate;

        public delegate void NameFetchDelegate(string[] results);
        private static NameFetchDelegate nameDelegate;

        public delegate void OnErrorDelegate(NetworkErrorMessage errorMessage);
        private static OnErrorDelegate failDelegate;

        /// <summary>
        /// Get the names of all the models processed in the database using a delegate of type searchCompleteDelegate.
        /// </summary>
        public static void GetProcessedModels(SearchCompleteDelegate searchCompleteDelegate, Action onThumbnailLoaded, OnErrorDelegate onErrorDelegate, object parent)
        {
            GetProcessedModelsAsync(searchCompleteDelegate, onThumbnailLoaded, onErrorDelegate, parent).Forget();
        }

        /// <summary>
        /// Get the names of all the models processed in the database.
        /// </summary>
        private static async UniTask GetProcessedModelsAsync(SearchCompleteDelegate delegateFunc, Action onThumbnailLoaded, OnErrorDelegate onErrorDelegate, object owner)
        {
            var searchResultArray = new SearchResult[0];
            searchDelegate += delegateFunc;
            UnityWebRequest www;
            try
            {
                var apiCall = NetworkConfig.GetUserProcessedUri(false);
                www = UnityWebRequest.Get(apiCall);
                await www.SendWebRequest().ToUniTask();
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not fetch the search results: {e}");
                searchDelegate?.Invoke(searchResultArray);
                searchDelegate -= delegateFunc;
                return;
            }
            if (www.result != UnityWebRequest.Result.Success)
            {
                try
                {
                    //If supported error format process
                    var error = new NetworkErrorMessage(www);
#if UNITY_EDITOR
                    failDelegate += onErrorDelegate;
                    failDelegate(error);
                    failDelegate -= onErrorDelegate;
#else
                    NetworkErrorHandler.HandleError(error);
#endif
                }
                catch
                {
                    //Else just debug as not handled by server properly
                    Debug.Log($"Couldn't parse error: {www.downloadHandler.text}");
                }
            }
            else
            {
                var result = www.downloadHandler.text;
                List<ModelJson> resultsList;
                try
                {
                    resultsList = JsonDeserializer.DeserializeModelJsonList(result);

                }
                catch (Exception e)
                {
                    Debug.LogError($"Could not fetch the search results: {e}");
                    resultsList = new List<ModelJson>();
                }

                if (resultsList == null) resultsList = new List<ModelJson>();


                searchResultArray = new SearchResult[resultsList.Count];

                for (var i = 0; i < searchResultArray.Length; i++)
                {
                    try
                    {
                        searchResultArray[i] = new SearchResult(resultsList[i]);
                        var madeResult = searchResultArray[i];
                        madeResult.IsProcessedResult = true;
                        //Set JSON and MongoID
                        madeResult.json = resultsList[i];
                        madeResult.mongoId = resultsList[i]._id;
                        var animationPipeline = JsonProcessor.ParseAnimationPipeline(madeResult.data);
                        //Set if model is animated through our standards, used for filtering.
                        if (!(animationPipeline == AnimationPipeline.Static)) madeResult.isAnimated = true;
                        else
                        {
                            madeResult.isAnimated = false;
                        }
                    }
                    catch
                    {
                        Debug.Log($"Error setting value at index {i}");
                    }
                }
                var results = searchResultArray.ToList();
                UniTask trequester = ThumbnailRequester.LoadThumbnailsIndividuallyAsync(results, onThumbnailLoaded, owner);
                trequester.Forget();
            }
            www.Dispose();
            //Turn JSON into AWThing data format.
            searchDelegate?.Invoke(searchResultArray);
            //Unsubscribe search delegate
            searchDelegate -= delegateFunc;
        }
    }
}