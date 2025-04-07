using AnythingWorld.Utilities;
using AnythingWorld.Utilities.Data;
using AnythingWorld.Utilities.Networking;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;

using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace AnythingWorld.Networking
{
    public static class SearchResultRequester
    {
        public delegate void SearchCompleteDelegate(SearchResult[] results, string onFail);
        private static SearchCompleteDelegate searchDelegate;

        public delegate void OnErrorDelegate(NetworkErrorMessage errorMessage);
        private static OnErrorDelegate failDelegate;

        public static void RequestCategorySearchResults(string searchTerm, SearchCompleteDelegate searchCompleteDelegate, Action refreshWindow, OnErrorDelegate onErrorDelegate, object owner)
        {
            RequestCategorySearchResultsAsync(searchTerm, searchCompleteDelegate, refreshWindow, onErrorDelegate, owner).Forget();
        }

        public static void RequestFeaturedResults(SearchCompleteDelegate searchCompleteDelegate, Action refreshWindow, OnErrorDelegate onErrorDelegate, object owner)
        {
            RequestFeaturedResultsAsync(searchCompleteDelegate, refreshWindow, onErrorDelegate, owner).Forget();
        }

        public static async UniTask RequestCategorySearchResultsAsync(string searchTerm, SearchCompleteDelegate delegateFunc, Action onThumbnailLoad, OnErrorDelegate onErrorDelegate, object owner)
        {
            var searchResultArray = new SearchResult[0];
            if (string.IsNullOrEmpty(searchTerm))
            {
                searchDelegate += delegateFunc;
                searchDelegate(new SearchResult[0], "The search term was empty. Try again by searching for something else!");
                searchDelegate -= delegateFunc;
                return;
            }

            searchDelegate += delegateFunc;

            var apiKey = AnythingSettings.APIKey;
            var appName = AnythingSettings.AppName;

            if (string.IsNullOrEmpty(appName))
            {
                Debug.LogWarning("App name missing! Setting app name to \"My Anything World App\"");
                appName = AnythingSettings.AppName = "My Anything World App";
            }
            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogError("Please enter an API Key in AnythingSettings!");
                return;
            }
            string sortingSubstring = $"&sort=default&descending=true&fuzzy=true";

            var apiCall = NetworkConfig.SearchUri(searchTerm, sortingSubstring);

            var www = UnityWebRequest.Get(apiCall);
            try
            {
                await www.SendWebRequest().ToUniTask();
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not fetch the search results: {e}");
                if (searchResultArray.Length == 0)
                {
                    searchDelegate?.Invoke(searchResultArray, $"Sorry! We didn't find any results for \"{searchTerm}\".");


                }
                else
                {
                    searchDelegate?.Invoke(searchResultArray, $"Sorry! We didn't find any results for \"{searchTerm}\".");

                }
                searchDelegate -= delegateFunc;
                return;
            }
            

            if (www.result != UnityWebRequest.Result.Success)
            {
                try
                {
                    var error = new NetworkErrorMessage(www);
                    failDelegate += onErrorDelegate;
                    failDelegate(error);
                    failDelegate -= onErrorDelegate;
                }
                catch
                {
                    searchDelegate(new SearchResult[0], $"Sorry! We didn't find any results for \"{searchTerm}\".");
                    searchDelegate -= delegateFunc;
                    return;
                }
            }
            else
            {
                var result = www.downloadHandler.text;
                List<ModelJson> resultsList;
                try
                {
                    resultsList = JsonConvert.DeserializeObject<List<ModelJson>>(result);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Could not deserialize into result lists: {e}");
                    resultsList = new List<ModelJson>();
                }
                searchResultArray = new SearchResult[resultsList.Count];
                for (var i = 0; i < searchResultArray.Length; i++)
                {
                   
                    try
                    {
                        searchResultArray[i] = new SearchResult(resultsList[i]);
                        var madeResult = searchResultArray[i];

                        var animationPipeline = JsonProcessor.ParseAnimationPipeline(madeResult.data);
                        madeResult.isAnimated = animationPipeline != AnimationPipeline.Static || madeResult.data.animated;
                    }
                    catch
                    {
                        Debug.Log($"Error setting value at index {i}");
                    }
                }
                UniTask un =ThumbnailRequester.LoadThumbnailsIndividuallyAsync(searchResultArray.ToList(), onThumbnailLoad, owner);
                un.Forget();
            }
            www.Dispose();
            searchDelegate(searchResultArray, $"Sorry! We didn't find any results for \"{searchTerm}\".");
            searchDelegate -= delegateFunc;
        }

        public static async UniTask RequestFeaturedResultsAsync(SearchCompleteDelegate delegateFunc, Action onThumbnailLoad, OnErrorDelegate onErrorDelegate, object owner)
        {
            var searchResultArray = new SearchResult[0];

            searchDelegate += delegateFunc;

            var apiKey = AnythingSettings.APIKey;
            var appName = AnythingSettings.AppName;

            if (string.IsNullOrEmpty(appName))
            {
                Debug.LogWarning("App name missing! Setting app name to \"My Anything World App\"");
                appName = AnythingSettings.AppName = "My Anything World App";
            }
            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogError("Please enter an API Key in AnythingSettings!");
                return;
            }

            var apiCall = NetworkConfig.FeaturedUri();
            var www = UnityWebRequest.Get(apiCall);
            await www.SendWebRequest().ToUniTask();

            if (www.result != UnityWebRequest.Result.Success)
            {
                try
                {
                    var error = new NetworkErrorMessage(www);
                    failDelegate += onErrorDelegate;
                    failDelegate(error);
                    failDelegate -= onErrorDelegate;
                }
                catch
                {
                    searchDelegate(new SearchResult[0], $"Sorry! We didn't find anything on the featured tab.");
                    searchDelegate -= delegateFunc;
                    return;
                }
            }
            else
            {
                var result = www.downloadHandler.text;
                List<ModelJson> resultsList;
                try
                {
                    resultsList = JsonConvert.DeserializeObject<List<ModelJson>>(result);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Could not deserialize into result lists: {e}");
                    resultsList = new List<ModelJson>();
                }

                searchResultArray = new SearchResult[resultsList.Count];
                for (var i = 0; i < searchResultArray.Length; i++)
                {
                    try
                    {
                        searchResultArray[i] = new SearchResult(resultsList[i]);
                        var madeResult = searchResultArray[i];
                        madeResult.isLandingPageSearch = true;
                        var animationPipeline = JsonProcessor.ParseAnimationPipeline(madeResult.data);
                        madeResult.isAnimated = animationPipeline != AnimationPipeline.Static;
                    }
                    catch
                    {
                        Debug.Log($"Error setting value at index {i}");
                    }
                }

                await ThumbnailRequester.LoadThumbnailsIndividuallyAsync(searchResultArray.ToList(), onThumbnailLoad, owner);
            }
            www.Dispose();
            searchDelegate(searchResultArray, $"Sorry! We didn't find anything on the featured tab.");
            searchDelegate -= delegateFunc;
        }
    }
}