using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Defines project-wide settings for the Behaviour Tree Editor, such as paths for new assets and script templates.
    /// </summary>
    [CreateAssetMenu(menuName = "ScriptableObjects/BehaviourTreeProjectSettings")]
    public class BehaviourTreeProjectSettings : ScriptableObject
    {
        [Tooltip("Transfer values of node fields when copying them.")]
        public bool enableNodeValuesCopying = true;
        
        [Tooltip("Folder where new tree assets will be created. (Must begin with 'Assets')")]
        public string newTreePath = "Assets/";

        [Tooltip("Folder where new node scripts will be created. (Must begin with 'Assets')")]
        public string newNodePath = "Assets/";

        [Tooltip("Script template to use when creating action nodes")]
        public TextAsset scriptTemplateActionNode;

        [Tooltip("Script template to use when creating composite nodes")]
        public TextAsset scriptTemplateCompositeNode;

        [Tooltip("Script template to use when creating decorator nodes")]
        public TextAsset scriptTemplateDecoratorNode;

        // Locates existing project settings for the Behaviour Tree Editor, warning if multiple are found.
        static BehaviourTreeProjectSettings FindSettings()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(BehaviourTreeProjectSettings)}");
            if (guids.Length > 1)
            {
                Debug.LogWarning($"Found multiple settings files, using the first.");
            }

            switch (guids.Length)
            {
                case 0:
                    return null;
                default:
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    return AssetDatabase.LoadAssetAtPath<BehaviourTreeProjectSettings>(path);
            }
        }

        /// <summary>
        /// Retrieves or creates new project settings for the Behaviour Tree Editor.
        /// </summary>
        internal static BehaviourTreeProjectSettings GetOrCreateSettings()
        {
            var settings = FindSettings();
            if (settings == null)
            {
                settings = CreateInstance<BehaviourTreeProjectSettings>();
                AssetDatabase.CreateAsset(settings, "Assets/AnythingWorld/AnythingBehaviour/AnythingBehaviourTree/" +
                                                    "BehaviourTreeProjectSettings.asset");
                AssetDatabase.SaveAssets();
            }
            return settings;
        }

        /// <summary>
        /// Provides a serialized object representation of the Behaviour Tree Editor project settings for UI binding.
        /// </summary>
        internal static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }
    }
    
    /// <summary>
    /// Registers a settings provider for the Behaviour Tree Editor, using UIElements for the settings UI.
    /// </summary>
    static class MyCustomSettingsUIElementsRegister
    {
        // Creates and returns the settings provider for the Behaviour Tree Editor settings, including UI construction logic.
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Settings window for the Project scope.
            var provider = new SettingsProvider("Project/BehaviourTreeProjectSettings", SettingsScope.Project)
            {
                label = "BehaviourTree",
                // activateHandler is called when the user clicks on the Settings item in the Settings window.
                activateHandler = (searchContext, rootElement) =>
                {
                    var settings = BehaviourTreeProjectSettings.GetSerializedSettings();

                    // rootElement is a VisualElement. If you add any children to it, the OnGUI function
                    // isn't called because the SettingsProvider uses the UIElements drawing framework.
                    var title = new Label()
                    {
                        text = "Behaviour Tree Settings"
                    };
                    title.AddToClassList("title");
                    rootElement.Add(title);

                    var properties = new VisualElement()
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Column
                        }
                    };
                    properties.AddToClassList("property-list");
                    rootElement.Add(properties);

                    properties.Add(new InspectorElement(settings));

                    rootElement.Bind(settings);
                },
            };

            return provider;
        }
    }
}
