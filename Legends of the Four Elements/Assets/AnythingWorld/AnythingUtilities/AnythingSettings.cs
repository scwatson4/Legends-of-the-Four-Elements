using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AnythingWorld
{
    /// <summary>
    /// Settings for Anything World API 
    /// </summary>
    public class AnythingSettings : ScriptableObject
    {
        /// <summary>
        /// Singleton instance of settings
        /// </summary>
        public static AnythingSettings Instance
        {
            get
            {
                var instance = Resources.Load<AnythingSettings>("Settings/AnythingSettings");
#if UNITY_EDITOR // If we are in the editor, create the settings file if it doesn't exist
                if (instance == null)
                {
                    Debug.Log("Instance is null, making new Settings file");
                    var asset = CreateInstance<AnythingSettings>();
                    if (!AssetDatabase.IsValidFolder("Assets/AnythingWorld"))
                    {
                        AssetDatabase.CreateFolder("Assets", "AnythingWorld");
                    }
                    if (!AssetDatabase.IsValidFolder("Assets/AnythingWorld/Resources"))
                    {
                        AssetDatabase.CreateFolder("Assets/AnythingWorld", "Resources");
                    }
                    if (!AssetDatabase.IsValidFolder("Assets/AnythingWorld/Resources/Settings"))
                    {
                        AssetDatabase.CreateFolder("Assets/AnythingWorld/Resources", "Settings");
                    }

                    AssetDatabase.CreateAsset(asset, "Assets/AnythingWorld/Resources/Settings/AnythingSettings.asset");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    return asset;
                }
                return instance;
#else
                return instance;
#endif
            }
        }

        [SerializeField] private string apiKey = "";
        [SerializeField] private string appName = "My Anything World App";
        [SerializeField] private string email = "";

        [SerializeField] private bool showDebugMessages = false;
        [SerializeField] private bool autoCreate = true;
        private const string versionNumber = "v1.2.0.0";
        [SerializeField] private bool UAS = false;
        [SerializeField] private bool welcomeMessage = false;
        public static bool IsUAS { get { return Instance.UAS; } set { Instance.UAS = value; } }
        public static string PackageVersion { get { return versionNumber; } }
        public static string APIKey { get { return Instance.apiKey; } set { Instance.apiKey = value; } }
        public static string AppName { get { return Instance.appName; } set { Instance.appName = value; } }
        public static string Email { get { return Instance.email; } set { Instance.email = value; } }

        public static bool HasEmail { get { return Instance.email != ""; } }
        public static bool HasAPIKey { get { return Instance.apiKey != ""; } }

        public static bool DebugEnabled { get { return Instance.showDebugMessages; } set { Instance.showDebugMessages = value; } }
        public static bool AutoCreateInAICreator { get { return Instance.autoCreate; } set { Instance.autoCreate = value; } }

        public static bool ShowWelcomeMessage { get { return Instance.welcomeMessage; } set { Instance.welcomeMessage = value; } }

        public void ClearSettings()
        {
            apiKey = "";
            appName = "My Anything World App";
            email = "";
            showDebugMessages = false;
        }
    }
}
