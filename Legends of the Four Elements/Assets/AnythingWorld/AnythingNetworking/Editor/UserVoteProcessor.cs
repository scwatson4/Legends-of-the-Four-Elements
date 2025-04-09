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
    public class VotedModels
    {
        public List<ModelJson> upvoted;
        public List<ModelJson> downvoted;
    }

    public static class UserVoteProcessor
    {
        public delegate void SearchCompleteDelegate(SearchResult[] results);
        private static SearchCompleteDelegate searchDelegate;

        public delegate void VoteChanged();
        public static VoteChanged voteChangeDelegate;

        public delegate void OnErrorDelegate(NetworkErrorMessage errorMessage);
        private static OnErrorDelegate failDelegate;

        public static void FlipUserVote(SearchResult searchResult, OnErrorDelegate onErrorDelegate, object owner)
        {
            ChangeUserVoteAsync(searchResult, onErrorDelegate).Forget();
        }

        public static void GetVoteCards(SearchCompleteDelegate searchCompleteDelegate, Action onThumbnailLoaded, OnErrorDelegate onErrorDelegate, object owner)
        {
            GetUserVotedAsync(searchCompleteDelegate, onThumbnailLoaded, onErrorDelegate, owner).Forget();
        }

        public static async UniTask ChangeUserVoteAsync(SearchResult searchResult, OnErrorDelegate onErrorDelegate)
        {
            string voteType = searchResult.data.userVote == "none" ? "upvote" : "revoke";
            var nameSplit = searchResult.data.name.Split('#');
            var apiCall = NetworkConfig.VoteUri(voteType, nameSplit[0], nameSplit[1]);

            UnityWebRequest www;
#if UNITY_2022_2_OR_NEWER
            www = UnityWebRequest.PostWwwForm(apiCall, "");
#else
            www = UnityWebRequest.Post(apiCall, "");
#endif
            www.timeout = 5;
            await www.SendWebRequest().ToUniTask();

            if (www.result == UnityWebRequest.Result.Success)
            {
                switch (searchResult.data.userVote)
                {
                    case "upvote":
                        searchResult.data.voteScore--;
                        searchResult.data.userVote = "none";
                        break;
                    case "none":
                        searchResult.data.voteScore++;
                        searchResult.data.userVote = "upvote";
                        break;
                }
                voteChangeDelegate?.Invoke();
            }
            else
            {
                try
                {
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
                    Debug.Log($"Couldn't parse error: {www.downloadHandler.text}");
                }
            }
            www.Dispose();
        }

        public static async UniTask GetUserVotedAsync(SearchCompleteDelegate delegateFunc, Action onThumbnailLoaded, OnErrorDelegate onErrorDelegate, object owner)
        {
            var searchResultArray = new SearchResult[0];
            searchDelegate += delegateFunc;
            var apiCall = NetworkConfig.MyLikesUri();
            UnityWebRequest www = UnityWebRequest.Get(apiCall);
            await www.SendWebRequest().ToUniTask();

            if (www.result != UnityWebRequest.Result.Success)
            {
                try
                {
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
                    Debug.Log($"Couldn't parse error: {www.downloadHandler.text}");
                }
            }
            else
            {
                var result = www.downloadHandler.text;
                VotedModels resultsList;
                try
                {
                    resultsList = JsonConvert.DeserializeObject<VotedModels>(result);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Could not fetch the search results: {e}");
                    resultsList = new VotedModels();
                }

                if (resultsList == null) resultsList = new VotedModels();

                searchResultArray = new SearchResult[resultsList.upvoted.Count];
                for (var i = 0; i < searchResultArray.Length; i++)
                {
                    try
                    {
                        searchResultArray[i] = new SearchResult(resultsList.upvoted[i]);
                        var madeResult = searchResultArray[i];
                        var animationPipeline = JsonProcessor.ParseAnimationPipeline(madeResult.data);
                        madeResult.isAnimated = animationPipeline != AnimationPipeline.Static;
                    }
                    catch
                    {
                        Debug.Log($"Error setting value at index {i}");
                    }
                }
                var results = searchResultArray.ToList();
                await ThumbnailRequester.LoadThumbnailsIndividuallyAsync(results, onThumbnailLoaded, owner);
            }
            www.Dispose();
            searchDelegate?.Invoke(searchResultArray);
            searchDelegate -= delegateFunc;
        }
    }
}