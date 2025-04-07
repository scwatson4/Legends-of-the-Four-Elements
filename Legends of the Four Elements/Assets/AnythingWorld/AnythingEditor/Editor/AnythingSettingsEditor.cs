using UnityEngine;
using UnityEditor;

namespace AnythingWorld.Editor
{
    public class AnythingSettingsEditor : AnythingEditor
    {
        private string apiKey;
        private string email;
        private string appName;

        [MenuItem("Tools/Anything World/General Settings", false, 41)]
        internal static void Initialize()
        {
            Resources.LoadAll<AnythingSettings>("Settings");

            Vector2 windowSize = new Vector2(425, 250);

            var browser = GetWindow(typeof(AnythingSettingsEditor), false, "Anything Settings") as AnythingSettingsEditor;
            browser.position = new Rect(EditorGUIUtility.GetMainWindowPosition().center - windowSize / 2, windowSize);
            browser.minSize = windowSize;
            browser.Show();
            browser.Focus();

            EditorUtility.SetDirty(AnythingSettings.Instance);
            browser.SetupVariables();
            EditorUtility.SetDirty(browser);
        }

        internal void SetupVariables()
        {
            apiKey = AnythingSettings.APIKey;
            appName = AnythingSettings.AppName;
            email = AnythingSettings.Email;
        }

        protected void ApplySettings()
        {
            if (AnythingSettings.Instance == null)
            {
                Debug.LogError("No AnythingSettings instance located.");
            }
            else
            {
                var settingsSerializedObject = new SerializedObject(AnythingSettings.Instance);
                settingsSerializedObject.FindProperty("apiKey").stringValue = apiKey;
                settingsSerializedObject.FindProperty("appName").stringValue = appName;
                settingsSerializedObject.FindProperty("email").stringValue = email;
                settingsSerializedObject.ApplyModifiedProperties();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Undo.RecordObject(AnythingSettings.Instance, "Changed General Settings");
                EditorUtility.SetDirty(AnythingSettings.Instance);
            }
        }

        protected bool CheckForChanges()
        {
            bool changesDetected = false;

            if (AnythingSettings.Instance == null || TransformSettings.GetInstance() == null)
            {
                Debug.LogError("No AnythingSettings instance located.");
            }
            else
            {
                var settingsSerializedObject = new SerializedObject(AnythingSettings.Instance);
                changesDetected = settingsSerializedObject.FindProperty("apiKey").stringValue != apiKey ||
                                  settingsSerializedObject.FindProperty("appName").stringValue != appName ||
                                  settingsSerializedObject.FindProperty("email").stringValue != email;
            }

            return changesDetected;
        }

        protected new void OnGUI()
        {
            base.OnGUI();

            InitializeResources();
            DrawGeneralSettings();
        }

        private void OnDestroy()
        {
            if (CheckForChanges())
            {
                if (EditorUtility.DisplayDialog("UNSAVED CHANGES", "You have not saved your changes, would you like to save your changes?", "Yes, save my changes", "No"))
                {
                    ApplySettings();
                }
            }
        }

        protected void DrawGeneralSettings()
        {
            fieldPadding = 12f;
            fieldLabelWidthPercentage = 0.3f;
            var fieldHeight = 25f;

            GUILayout.Space(fieldPadding);

            var apiKeyStringFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            CustomStringField(ref apiKey, new GUIContent("API Key"), apiKeyStringFieldRect);

            GUILayout.Space(fieldPadding);

            var emailStringFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            CustomStringField(ref email, new GUIContent("Email"), emailStringFieldRect);

            GUILayout.Space(fieldPadding);

            var appNameStringFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            CustomStringField(ref appName, new GUIContent("Application Name"), appNameStringFieldRect);

            GUILayout.Space(fieldPadding);

            var checkForUpdateButtonRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            checkForUpdateButtonRect.x += fieldPadding;
            checkForUpdateButtonRect.width -= fieldPadding * 2;
            if (DrawRoundedButton(checkForUpdateButtonRect, new GUIContent("Check for Updates"))) VersionCheckEditor.CheckVersion();

            GUILayout.FlexibleSpace();
            var applyButtonRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            applyButtonRect.x += fieldPadding;
            applyButtonRect.width -= fieldPadding * 2;
            if (DrawRoundedButton(applyButtonRect, new GUIContent("Apply"))) ApplySettings();

            GUILayout.Space(fieldPadding);

            var resetButtonRect = GUILayoutUtility.GetRect(position.width, fieldHeight + fieldPadding);
            resetButtonRect.x += fieldPadding;
            resetButtonRect.width -= fieldPadding * 2;
            resetButtonRect.height -= fieldPadding;
            if (DrawRoundedButton(resetButtonRect, new GUIContent("Reset"))) SetupVariables();
        }
    }
}
