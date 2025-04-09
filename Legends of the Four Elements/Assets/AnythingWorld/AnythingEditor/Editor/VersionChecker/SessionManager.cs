using AnythingWorld.Utilities;
using System;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace AnythingWorld.Editor
{
    [InitializeOnLoad]
    public class SessionManager
    {
        static SessionManager()
        {
#if UNITY_EDITOR
            EditorApplication.wantsToQuit -= LogSessionEnd;
            EditorApplication.wantsToQuit += LogSessionEnd;
#endif
        }
        public static bool LogSessionEnd()
        {
            //must not block as this will block 
            try
            {
                if (!string.IsNullOrEmpty(AnythingSettings.APIKey))
                {
                    var seconds = EditorApplication.timeSinceStartup;
                    var hours = Math.Floor(seconds / 60 / 60);
                    var minutes = Math.Floor((seconds / 60) - (hours * 60));
                    return UploadSessionLogData(hours.ToString(), minutes.ToString(), AnythingSettings.APIKey, AnythingSettings.AppName);
                }
                else
                {
                    //not logged in so may not have have accepted our terms and conditions
                    return true;
                }
            }
            catch
            {
                return true;
            }
        }
        private static bool UploadSessionLogData(string hours, string minutes, string apiKey, string appName)
        {
            string encodedAppName = System.Uri.EscapeUriString(appName);
            string url = $"{NetworkConfig.ApiUrlStem}/session-length";
#if UNITY_EDITOR_WIN
            string system = "windows";
#elif UNITY_EDITOR_OSX
            string system = "mac";
#else
            string system = "other";
#endif

            string data = $"?key={apiKey}&platform=unity&version={AnythingSettings.PackageVersion}&app={encodedAppName}&hours={hours}&minutes={minutes}&operatingSystem={system}";
            string request = url + data;
#if UNITY_2022_2_OR_NEWER
            using var www = UnityWebRequest.PostWwwForm(request, "");
#else
            using var www = UnityWebRequest.Post(request, "");
#endif
            www.SendWebRequest();
            while (www.result == UnityWebRequest.Result.InProgress) { }
            if(www.result == UnityWebRequest.Result.ProtocolError || www.result == UnityWebRequest.Result.ConnectionError || !string.IsNullOrWhiteSpace(www.error))
            {
                Debug.LogError("Couldn't upload session data!");
                return false;
            }

            return string.IsNullOrEmpty(www.error);
        }
    }
}
#endif