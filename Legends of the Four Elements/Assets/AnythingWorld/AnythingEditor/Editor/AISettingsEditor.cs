using AnythingWorld.Utilities;
using UnityEditor;
using UnityEngine;

namespace AnythingWorld.Editor
{
    public class AISettingsEditor : AnythingEditor
    {
        protected bool gridPlacementEnabled = false;

        private string tempGridOriginX = "", tempGridOriginY = "", tempGridOriginZ = "";
        private string tempCellWidth = "";
        private string tempCellCount = "";

        private bool showGridHandles = false;
        private Vector3 gridOrigin;
        private int gridCellCount;
        private float gridCellWidth;

        private bool autoCreate = true;

        [MenuItem("Tools/Anything World/AI Creator Settings", false, 43)]
        public static void Initialize()
        {
            //check if path exists
            if (!System.IO.Directory.Exists("Assets/Resources/Settings"))
            {
                System.IO.Directory.CreateDirectory("Assets/Resources/Settings");
            }
            Resources.LoadAll<AnythingSettings>("Settings");
            Resources.LoadAll<TransformSettings>("Settings");

            AnythingCreatorEditor tabWindow;
            Vector2 windowSize;

            if (AnythingSettings.HasAPIKey)
            {
                windowSize = new Vector2(425, 300);

                var browser = GetWindow(typeof(AISettingsEditor), false, "AI Creator Settings") as AISettingsEditor;
                browser.position = new Rect(EditorGUIUtility.GetMainWindowPosition().center - windowSize / 2, windowSize);
                browser.minSize = windowSize;
                browser.Show();
                browser.Focus();

                EditorUtility.SetDirty(TransformSettings.GetInstance());
                EditorUtility.SetDirty(AnythingSettings.Instance);
                browser.SetupVariables();
                EditorUtility.SetDirty(browser);
            }
            else
            {
                windowSize = new Vector2(450, 800);

                tabWindow = GetWindow<LogInEditor>("Log In | Sign Up", false);
                tabWindow.position = new Rect(EditorGUIUtility.GetMainWindowPosition().center - windowSize / 2, windowSize);
            }
        }

        internal void SetupVariables()
        {
            autoCreate = AnythingSettings.AutoCreateInAICreator;

            showGridHandles = TransformSettings.ShowGridHandles;

            gridOrigin = TransformSettings.GridOrigin;
            gridCellCount = TransformSettings.GridCellCount;
            gridCellWidth = TransformSettings.GridCellWidth;

            tempGridOriginX = SimpleGrid.origin.x.ToString();
            tempGridOriginY = SimpleGrid.origin.y.ToString();
            tempGridOriginZ = SimpleGrid.origin.z.ToString();
            tempCellWidth = SimpleGrid.cellWidth.ToString();
            tempCellCount = SimpleGrid.cellCount.ToString();
        }

        internal void Reset()
        {
            autoCreate = true;

            showGridHandles = false;

            gridOrigin = Vector3.zero;
            gridCellCount = 10;
            gridCellWidth = 1f;

            tempGridOriginX = SimpleGrid.origin.x.ToString();
            tempGridOriginY = SimpleGrid.origin.y.ToString();
            tempGridOriginZ = SimpleGrid.origin.z.ToString();
            tempCellWidth = SimpleGrid.cellWidth.ToString();
            tempCellCount = SimpleGrid.cellCount.ToString();
        }

        internal void ApplySettings()
        {
            if (ApplySettingsLight()) 
            { 
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Undo.RecordObject(TransformSettings.GetInstance(), "Changed AI Creator Settings");
                EditorUtility.SetDirty(AnythingSettings.Instance);
                EditorUtility.SetDirty(TransformSettings.GetInstance());
            }
        }

        internal bool ApplySettingsLight()
        {
            if (TransformSettings.GetInstance() == null)
            {
                Debug.LogError("No AnythingSettings instance located.");
                return false;
            }

            var generalSettingsSerializedObject = new SerializedObject(AnythingSettings.Instance);
            generalSettingsSerializedObject.FindProperty("autoCreate").boolValue = autoCreate;
            generalSettingsSerializedObject.ApplyModifiedProperties();

            var transformSettingsSerializedObject = new SerializedObject(TransformSettings.GetInstance());
            transformSettingsSerializedObject.FindProperty("showGridHandles").boolValue = showGridHandles;
            SimpleGrid.origin = transformSettingsSerializedObject.FindProperty("gridOrigin").vector3Value = gridOrigin;
            SimpleGrid.cellCount = transformSettingsSerializedObject.FindProperty("gridCellCount").intValue = gridCellCount;
            SimpleGrid.cellWidth = transformSettingsSerializedObject.FindProperty("gridCellWidth").floatValue = gridCellWidth;
            transformSettingsSerializedObject.ApplyModifiedProperties();
            return true;
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
                var generalSettingsSerializedObject = new SerializedObject(AnythingSettings.Instance);
                var transformSettingsSerializedObject = new SerializedObject(TransformSettings.GetInstance());
                changesDetected = transformSettingsSerializedObject.FindProperty("showGridHandles").boolValue != showGridHandles ||
                                  transformSettingsSerializedObject.FindProperty("gridOrigin").vector3Value != gridOrigin ||
                                  transformSettingsSerializedObject.FindProperty("gridCellCount").intValue != gridCellCount ||
                                  transformSettingsSerializedObject.FindProperty("gridCellWidth").floatValue != gridCellWidth ||
                                  generalSettingsSerializedObject.FindProperty("autoCreate").boolValue != autoCreate;
            }

            return changesDetected;
        }

        protected new void OnGUI()
        {
            base.OnGUI();

            InitializeResources();
            DrawTransformSettings();
        }

        private void OnDestroy()
        {
            if (CheckForChanges())
            {
                ApplySettings();
            }
        }

        protected void DrawTransformSettings()
        {
            fieldPadding = 12f;
            fieldLabelWidthPercentage = 0.3f;
            var fieldHeight = 25f;

            GUILayout.Space(fieldPadding);

            var autoCreateBoolFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            CustomBoolField(ref autoCreate, new GUIContent("Auto-Create"), autoCreateBoolFieldRect);

            #region Grid Settings
            GUILayout.Space(fieldPadding);

            var helpBoxContent = new GUIContent("Edit the way models are placed when generated");
            var helpBoxStyle = new GUIStyle(BodyLabelStyle) { padding = new RectOffset((int)fieldPadding, (int)fieldPadding, 0, 0) };
            var helpBoxRect = GUILayoutUtility.GetRect(position.width, helpBoxStyle.CalcHeight(helpBoxContent, position.width));
            GUI.Label(helpBoxRect, helpBoxContent, helpBoxStyle);

            GUILayout.Space(fieldPadding);

            var gridGizmosBoolFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            if (CustomBoolField(ref showGridHandles, new GUIContent("Grid Gizmos"), gridGizmosBoolFieldRect))
            {
                SceneView.RepaintAll();
            }

            GUILayout.Space(fieldPadding);

            var gridOriginVectorFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            CustomVectorField(ref gridOrigin, ref tempGridOriginX, ref tempGridOriginY, ref tempGridOriginZ, new GUIContent("Grid Origin"), gridOriginVectorFieldRect);

            GUILayout.Space(fieldPadding);

            var cellWidthFloatFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            CustomFloatField(ref gridCellWidth, ref tempCellWidth, new GUIContent("Cell Width"), cellWidthFloatFieldRect);

            GUILayout.Space(fieldPadding);

            var gridWidthIntFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            CustomIntField(ref gridCellCount, ref tempCellCount, new GUIContent("Grid Width"), gridWidthIntFieldRect);
            #endregion

            GUILayout.FlexibleSpace();

            var resetButtonRect = GUILayoutUtility.GetRect(position.width, fieldHeight + fieldPadding);
            resetButtonRect.x += fieldPadding;
            resetButtonRect.width -= fieldPadding * 2;
            resetButtonRect.height -= fieldPadding;
            if (DrawRoundedButton(resetButtonRect, new GUIContent("Reset")))
            {

                Reset();
                //delay the refresh to allow the editor to update
                System.Threading.Tasks.Task.Delay(500).ContinueWith((task) =>
                {
                    Reset();
                    Repaint();
                });
                ApplySettings();
                
            }

                if (GUI.changed)
            {
                ApplySettingsLight();
            }
        }
    }
}
