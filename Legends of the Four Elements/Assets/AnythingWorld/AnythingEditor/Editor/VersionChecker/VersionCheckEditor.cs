#if UNITY_EDITOR
using AnythingWorld.Utilities;

using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine.Networking;
using UnityEngine;

namespace AnythingWorld.Editor
{
    public class VersionCheckEditor : UnityEditor.Editor
    {
        private static string versionCheckOptOut = "anythingWorldVersionCheckOptOut";
        private static string lastVersionOptedOut = "anythingWorldVersionCheckedOptOut";

        public static void TryGetUpdateDialogue()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                GetVersionAsync($"{NetworkConfig.ApiUrlStem}/version?v=1").Forget();
            }
        }
        public static void CheckVersion()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                GetVersionWithConfirmAsync($"{NetworkConfig.ApiUrlStem}/version?v=1").Forget();
            }
        }
        
        private static async UniTask GetVersionWithConfirmAsync(string uri)
        {
            using (UnityWebRequest uwr = UnityWebRequest.Get(uri))
            {
                await uwr.SendWebRequest();
                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    VersionResponse response = VersionResponse.CreateFromJson(uwr.downloadHandler.text);

                    if (response.version != AnythingSettings.PackageVersion)
                    {
                        AnythingEditor.DisplayAWDialog("Anything World Version Upgrade", $"{response.message} \nLocal AW version: {AnythingSettings.PackageVersion} \nCurrent AW Version: {response.version}", 
                            "Get Latest Version", "Exit", "Don't Show Me Again", 
                            () => Application.OpenURL(response.downloadLink), () =>
                            {
                                EditorPrefs.SetBool(versionCheckOptOut, true);
                                EditorPrefs.SetString(lastVersionOptedOut, response.version);
                            });
                    }
                    else
                    {
                        AnythingEditor.DisplayAWDialog("Version Up To Date", "You are using the latest version of Anything World.");
                    }
                }
            }
        }

        private static async UniTask GetVersionAsync(string uri)
        {
            using (UnityWebRequest uwr = UnityWebRequest.Get(uri))
            {
                await uwr.SendWebRequest().ToUniTask();

                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    VersionResponse response = VersionResponse.CreateFromJson(uwr.downloadHandler.text);

                    if (response.version != AnythingSettings.PackageVersion && !AnythingSettings.PackageVersion.Contains("a") && AnythingSettings.PackageVersion.Contains("BETA"))
                    {
                        // If user has opted out of seeing the upgrade dialogue
                        if (CheckDialogueEnabled(response))
                        {
                            AnythingEditor.DisplayAWDialog("Anything World Version Upgrade", $"{response.message} \nLocal AW version: {AnythingSettings.PackageVersion} \nCurrent AW Version: {response.version}",
                                "Get Latest Version", "Exit", "Don't Show Me Again",
                                () => Application.OpenURL(response.downloadLink), () =>
                                {
                                    EditorPrefs.SetBool(versionCheckOptOut, true);
                                    EditorPrefs.SetString(lastVersionOptedOut, response.version);
                                });
                        }
                    }
                }
            }
        }

        private static bool CheckDialogueEnabled(VersionResponse response)
        {
            if (EditorPrefs.HasKey(versionCheckOptOut) && EditorPrefs.GetBool(versionCheckOptOut) == true)
            {
                //If version is higher than the version opted out of, show to user again
                if (EditorPrefs.HasKey(lastVersionOptedOut) && EditorPrefs.GetString(lastVersionOptedOut) != response.version)
                {
                    EditorPrefs.SetBool(versionCheckOptOut, false);
                    EditorPrefs.DeleteKey(lastVersionOptedOut);
                    return true;
                }
                else
                {
                    //If opt out matches version then do not show
                    return false;
                }
            }
            else
            {
                //If no opt out stored, show dialogue to users
                return true;
            }
        }

        [System.Serializable]
        public class VersionResponse
        {
            public string downloadLink;
            public string version;
            public string message;

            public static VersionResponse CreateFromJson(string jsonString)
            {
                return JsonUtility.FromJson<VersionResponse>(jsonString);
            }
        }

    }

}

#endif