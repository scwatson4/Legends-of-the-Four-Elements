using AnythingWorld.Utilities;
using AnythingWorld.Utilities.Data;
using AnythingWorld.Utilities.Networking;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace AnythingWorld.Networking.Editor
{
    public static class CollectionProcessor
    {
        public delegate void SearchCompleteDelegate(CollectionResult[] results);
        private static SearchCompleteDelegate searchDelegate;

        public delegate void RefreshCompleteDelegate(CollectionResult result);
        private static RefreshCompleteDelegate refreshDelegate;

        public delegate void NameFetchDelegate(string[] results);
        private static NameFetchDelegate nameDelegate;

        public delegate void OnErrorDelegate(NetworkErrorMessage errorMessage);
        private static OnErrorDelegate failDelegate;

        /// <summary>
        /// Adds a search result to a specified collection.
        /// </summary>
        public static async UniTask AddToCollectionAsync(SearchCompleteDelegate searchCompleteDelegate, SearchResult searchResult, string collectionNames, OnErrorDelegate onErrorDelegate, object parent)
        {
            try
            {
                await AddToCollectionCoroutineAsync(searchCompleteDelegate, collectionNames, searchResult, onErrorDelegate, parent);
            }
            catch (Exception e)
            {
                Debug.LogError($"An error occurred while adding to collection: {e.Message}");
            }
        }
        
        /// <summary>
        /// Removes a search result from a specified collection.
        /// </summary>
        public static async UniTask RemoveFromCollectionAsync(SearchCompleteDelegate searchCompleteDelegate, SearchResult searchResult, CollectionResult collection, OnErrorDelegate onErrorDelegate, object parent)
        {
            try
            {
                await RemoveFromCollectionCoroutineAsync(searchCompleteDelegate, collection, searchResult, onErrorDelegate, parent);
            }
            catch (Exception e)
            {
                Debug.LogError($"An error occurred while removing from collection: {e.Message}");
            }
        }
        
        /// <summary>
        /// Creates a new collection with the specified details.
        /// </summary>
        public static async UniTask CreateNewCollectionAsync(SearchCompleteDelegate searchCompleteDelegate, CollectionResult collection, OnErrorDelegate onErrorDelegate, object parent)
        {
            try
            {
                await CreateCollectionCoroutineAsync(searchCompleteDelegate, collection, onErrorDelegate, parent);
            }
            catch (Exception e)
            {
                Debug.LogError($"An error occurred while creating a new collection: {e.Message}");
            }
        }
        
        /// <summary>
        /// Deletes a specified collection.
        /// </summary>
        public static async UniTask DeleteCollectionAsync(SearchCompleteDelegate searchCompleteDelegate, CollectionResult collection, OnErrorDelegate onErrorDelegate, object parent)
        {
            try
            {
                await DeleteCollectionCoroutineAsync(searchCompleteDelegate, collection, onErrorDelegate, parent);
            }
            catch (Exception e)
            {
                Debug.LogError($"An error occurred while deleting the collection: {e.Message}");
            }
        }
        
        /// <summary>
        /// Retrieves the names of all user collections.
        /// </summary>
        public static async UniTask GetCollectionNamesAsync(NameFetchDelegate nameFetchDelegate, OnErrorDelegate onErrorDelegate, object parent)
        {
            try
            {
                await GetUserCollectionNamesCoroutineAsync(nameFetchDelegate, onErrorDelegate);
            }
            catch (Exception e)
            {
                Debug.LogError($"An error occurred while fetching collection names: {e.Message}");
            }
        }
        
        /// <summary>
        /// Retrieves all collections for the user.
        /// </summary>
        public static async UniTask GetCollectionsAsync(SearchCompleteDelegate searchCompleteDelegate, OnErrorDelegate onErrorDelegate, object parent)
        {
            try
            {
                await GetUserCollectionsCoroutineAsync(searchCompleteDelegate, onErrorDelegate);
            }
            catch (Exception e)
            {
                Debug.LogError($"An error occurred while fetching collections: {e.Message}");
            }
        }
        
        /// <summary>
        /// Retrieves details for a specific collection.
        /// </summary>
        public static async UniTask GetCollectionAsync(RefreshCompleteDelegate refreshCompleteDelegate, CollectionResult collection, OnErrorDelegate onErrorDelegate, object parent)
        {
            try
            {
                await GetUserCollectionCoroutineAsync(refreshCompleteDelegate, collection, onErrorDelegate);
            }
            catch (Exception e)
            {
                Debug.LogError($"An error occurred while fetching a specific collection: {e.Message}");
            }
        }

        private static async UniTask GetUserCollectionNamesCoroutineAsync(NameFetchDelegate delegateFunc, OnErrorDelegate onErrorDelegate)
        {
            var collectionNames = new string[0];

            nameDelegate += delegateFunc;

            var apiCall = NetworkConfig.UserCollectionsUri(true);
            using var www = UnityWebRequest.Get(apiCall);
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
                    Debug.Log($"Couldn't parse error: {www.downloadHandler.text}");
                }
            }
            else
            {
                var result = www.downloadHandler.text;
                Dictionary<string, List<string>> resultsDictionary;
                try
                {
                    resultsDictionary = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(result);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Could not fetch the search results: {e}");
                    resultsDictionary = new Dictionary<string, List<string>>();
                }

                collectionNames = resultsDictionary.Keys.ToArray();
            }
            nameDelegate(collectionNames);
            nameDelegate -= delegateFunc;
        }

        private static async UniTask GetUserCollectionsCoroutineAsync(SearchCompleteDelegate delegateFunc, OnErrorDelegate onErrorDelegate)
        {
            var collectionResults = new CollectionResult[0];

            searchDelegate += delegateFunc;

            var apiCall = NetworkConfig.UserCollectionsUri(false);
            using var www = UnityWebRequest.Get(apiCall);
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
                    Debug.Log($"Couldn't parse error: {www.downloadHandler.text}");
                }
            }
            else
            {
                var result = www.downloadHandler.text;
                Dictionary<string, List<ModelJson>> resultsDictionary;
                try
                {
                    resultsDictionary = JsonConvert.DeserializeObject<Dictionary<string, List<ModelJson>>>(result);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Could not fetch the search results: {e}");
                    resultsDictionary = new Dictionary<string, List<ModelJson>>();
                }
                collectionResults = new CollectionResult[resultsDictionary.Count];
                for (int i = 0; i < collectionResults.Length; i++)
                {
                    KeyValuePair<string, List<ModelJson>> kvp = resultsDictionary.ElementAt(i);
                    collectionResults[i] = new CollectionResult(kvp.Key, kvp.Value);
                    await ThumbnailRequester.LoadThumbnailsBatchAsync(collectionResults[i].Results.ToArray());
                }
            }
            
            searchDelegate(collectionResults);
            searchDelegate -= delegateFunc;
        }

        private static async UniTask GetUserCollectionCoroutineAsync(RefreshCompleteDelegate delegateFunc, CollectionResult collection, OnErrorDelegate onErrorDelegate)
        {
            CollectionResult collectionResult = null;

            refreshDelegate += delegateFunc;

            var apiCall = NetworkConfig.UserCollectionsUri(false);
            using var www = UnityWebRequest.Get(apiCall);
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
                    Debug.Log($"Couldn't parse error: {www.downloadHandler.text}");
                }
            }
            else
            {
                var result = www.downloadHandler.text;
                Dictionary<string, List<ModelJson>> resultsDictionary = new Dictionary<string, List<ModelJson>>();
                try
                {
                    resultsDictionary = JsonConvert.DeserializeObject<Dictionary<string, List<ModelJson>>>(result);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Could not fetch the search results: {e}");
                    resultsDictionary = new Dictionary<string, List<ModelJson>>();
                }

                KeyValuePair<string, List<ModelJson>> kvp = resultsDictionary.FirstOrDefault(x => x.Key == collection.Name);
                collectionResult = new CollectionResult(kvp.Key, kvp.Value);
                await ThumbnailRequester.LoadThumbnailsBatchAsync(collectionResult.Results.ToArray());
            }
            refreshDelegate(collectionResult);
            refreshDelegate -= delegateFunc;
        }

        private static async UniTask CreateCollectionCoroutineAsync(SearchCompleteDelegate delegateFunc, CollectionResult collection, OnErrorDelegate onErrorDelegate, object parent)
        {
            var apiCall = NetworkConfig.AddCollectionUri(collection.Name);
            using var www = UnityWebRequest.PostWwwForm(apiCall, "");
            www.timeout = 5;
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
                    Debug.Log($"Couldn't parse error: {www.downloadHandler.text}");
                }
            }
            if (www.result == UnityWebRequest.Result.Success)
            {
                await GetUserCollectionsCoroutineAsync(delegateFunc, onErrorDelegate);
            }
        }

        private static async UniTask DeleteCollectionCoroutineAsync(SearchCompleteDelegate delegateFunc, CollectionResult collection, OnErrorDelegate onErrorDelegate, object parent)
        {
            searchDelegate += delegateFunc;

            var apiCall = NetworkConfig.RemoveCollectionUri(collection.Name);
            using var www = UnityWebRequest.PostWwwForm(apiCall, "");
            www.timeout = 5;
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
                    Debug.Log($"Couldn't parse error: {www.downloadHandler.text}");
                }
            }
            if (www.result == UnityWebRequest.Result.Success)
            {
                await GetUserCollectionsCoroutineAsync(delegateFunc, onErrorDelegate);
            }
        }

        private static async UniTask AddToCollectionCoroutineAsync(SearchCompleteDelegate delegateFunc, string collectionName, SearchResult searchResult, OnErrorDelegate onErrorDelegate, object parent)
        {
            var nameSplit = searchResult.data.name.Split('#');

            var apiCall = NetworkConfig.AddToCollectionUri(collectionName, nameSplit[0], nameSplit[1]);
            using var www = UnityWebRequest.PostWwwForm(apiCall, "");
            www.timeout = 5;
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
                    Debug.Log($"Couldn't parse error: {www.downloadHandler.text}");
                }
            }
            if (www.result == UnityWebRequest.Result.Success)
            {
                await GetUserCollectionsCoroutineAsync(delegateFunc, onErrorDelegate);
            }
        }

        private static async UniTask RemoveFromCollectionCoroutineAsync(SearchCompleteDelegate delegateFunc, CollectionResult collection, SearchResult searchResult, OnErrorDelegate onErrorDelegate, object parent)
        {
            var nameSplit = searchResult.data.name.Split('#');

            var apiCall = NetworkConfig.RemoveFromCollectionUri(collection.Name, nameSplit[0], nameSplit[1]);
            using var www = UnityWebRequest.PostWwwForm(apiCall, "");
            www.timeout = 5;
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
                    Debug.Log($"Couldn't parse error: {www.downloadHandler.text}");
                }
            }
        }
    }
}