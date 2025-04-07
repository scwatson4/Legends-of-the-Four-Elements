using AnythingWorld.Utilities;
using AnythingWorld.Utilities.Data;
using AnythingWorld.Utilities.Networking;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace AnythingWorld.Networking.Editor
{
    public static class ReportProcessor
    {
        public enum ReportReason
        {
            COPYRIGHT, EMPTY, INAPPROPRIATE, QUALITY, OTHER
        }

        public delegate void ReportSentDelegate();
        private static ReportSentDelegate reportDelegate;

        public delegate void OnErrorDelegate(NetworkErrorMessage errorMessage);
        private static OnErrorDelegate failDelegate;

        public static void SendReport(ReportSentDelegate reportSent, SearchResult searchResult, ReportReason reason, OnErrorDelegate onErrorDelegate)
        {
            SendReportAsync(reportSent, searchResult, reason, onErrorDelegate).Forget();
        }

        private static async UniTask SendReportAsync(ReportSentDelegate delegateFunc, SearchResult searchResult, ReportReason reason, OnErrorDelegate onErrorDelegate)
        {
            reportDelegate += delegateFunc;
            var reasonString = reason switch
            {
                ReportReason.COPYRIGHT => "copyright",
                ReportReason.EMPTY => "empty",
                ReportReason.INAPPROPRIATE => "inappropriate",
                ReportReason.QUALITY => "poor-quality",
                _ => "other"
            };
            var nameSplit = searchResult.data.name.Split('#');

            UnityWebRequest www;
            var apiCall = NetworkConfig.ReportUri(nameSplit[0], nameSplit[1], reasonString);
#if UNITY_2022_2_OR_NEWER
            www = UnityWebRequest.PostWwwForm(apiCall, "");
#else
            www = UnityWebRequest.Post(apiCall, "");
#endif
            www.timeout = 5;
            await www.SendWebRequest().ToUniTask();

            if (www.result == UnityWebRequest.Result.Success)
            {
                if (AnythingSettings.DebugEnabled) Debug.Log($"Report ({searchResult.data.name} | {reason}) succeeded!");
                reportDelegate?.Invoke();
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

            reportDelegate -= delegateFunc;
        }
    }
}