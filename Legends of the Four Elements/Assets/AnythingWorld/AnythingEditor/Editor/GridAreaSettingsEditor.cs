using AnythingWorld.Utilities;

using UnityEditor;
using UnityEngine;

namespace AnythingWorld.Editor
{
    /// <summary>
    /// Editor window for Grid Area Settings
    /// </summary>
    public class GridAreaSettingsEditor : AnythingEditor
    {
        private string tempGridOriginX = "", tempGridOriginY = "", tempGridOriginZ = "";

        private string tempGridAreaFitRow = "4";
        private string tempGridAreaClipDistanceX = "1.0";
        private string tempGridAreaClipDistanceY = "0.0";
        private string tempGridAreaClipDistanceZ = "1.0";

        protected bool transformSettingsActive = false;

        private Transform defaultGridAreaObject = null;
        private Transform oldGridAreaObject = null;
        private bool gridAreaFitBool = true;
        private bool gridAreaClipBool = false;
        private int  gridAreaFitRow;
        private Vector3 gridAreaClipDistance;
        private bool gridAreaViewBool = true;
        private bool gridAreaGrowBool = false;
        private bool gridAreaIgnoreBool = false;
        private bool gridAreaRandomBool = false;

        private Vector3 gridOrigin;
        private int gridCellCount;
        private float gridCellWidth;
        private Vector2 scrollPosition;

        /// <summary>
        /// Initialize Grid Area Settings Editor Window and load settings from disk if they exist
        /// </summary>
        public static void Initialize()
        {
            Resources.LoadAll<AnythingSettings>("Settings");
            Resources.LoadAll<TransformSettings>("Settings");

            AnythingCreatorEditor tabWindow;
            Vector2 windowSize;

            if (AnythingSettings.HasAPIKey)
            {
                windowSize = new Vector2(425, 540);

                var browser = GetWindow(typeof(GridAreaSettingsEditor), false, "Grid Area Settings") as GridAreaSettingsEditor;
                browser.position = new Rect(EditorGUIUtility.GetMainWindowPosition().center - windowSize / 2, windowSize);
                browser.minSize = windowSize;
                browser.Show();
                browser.Focus();

                EditorUtility.SetDirty(TransformSettings.GetInstance());
                EditorUtility.SetDirty(AnythingSettings.Instance);
                EditorUtility.SetDirty(browser);
            }
            else
            {
                windowSize = new Vector2(450, 800);

                tabWindow = GetWindow<LogInEditor>("Log In | Sign Up", false);
                tabWindow.position = new Rect(EditorGUIUtility.GetMainWindowPosition().center - windowSize / 2, windowSize);
            }
        }
        /// <summary>
        /// Setup variables from disk on awake
        /// </summary>
        private new void Awake()
        {
            base.Awake();
            SetupVariables();
        }
        /// <summary>
        /// Setup variables from disk
        /// </summary>
        internal void SetupVariables()
        {
            defaultGridAreaObject = null;
            GridArea.initialized = TransformSettings.GridAreaEnabled;
            GridArea.origin = TransformSettings.GridAreaOrigin;
            GridArea.areaSize = TransformSettings.GridAreaSize;
            GridArea.areaForward = TransformSettings.GridAreaForward;
            GridArea.areaRight = TransformSettings.GridAreaRight;
            gridAreaFitBool = TransformSettings.GridAreaFitMode;
            gridAreaClipBool = TransformSettings.GridAreaClipMode;
            gridAreaFitRow = TransformSettings.GridAreaObjectsPerRow;
            gridAreaClipDistance = TransformSettings.GridAreaObjectsDistance;
            gridAreaViewBool = TransformSettings.GridAreaShowPositions;
            gridAreaGrowBool = TransformSettings.GridAreaCanGrow;
            gridAreaIgnoreBool = TransformSettings.GridAreaIgnoreCollision;
            gridAreaRandomBool = TransformSettings.GridAreaRandomOffset;
            GridArea.SetPlaceOnGroundMode(TransformSettings.PlaceOnGround);

            gridOrigin = TransformSettings.GridOrigin;
            gridCellCount = TransformSettings.GridCellCount;
            gridCellWidth = TransformSettings.GridCellWidth;

            tempGridAreaFitRow = gridAreaFitRow.ToString();
            tempGridAreaClipDistanceX = gridAreaClipDistance.x.ToString();
            tempGridAreaClipDistanceY = gridAreaClipDistance.y.ToString();
            tempGridAreaClipDistanceZ = gridAreaClipDistance.z.ToString();

            tempGridOriginX = SimpleGrid.origin.x.ToString();
            tempGridOriginY = SimpleGrid.origin.y.ToString();
            tempGridOriginZ = SimpleGrid.origin.z.ToString();
        }
        /// <summary>
        /// Apply settings and save to disk
        /// </summary>
        internal void ApplySettings()
        {
            if (ApplySettingsLight())
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Undo.RecordObject(TransformSettings.GetInstance(), "Changed Transform Settings");
                EditorUtility.SetDirty(AnythingSettings.Instance);
                EditorUtility.SetDirty(TransformSettings.GetInstance());
            }
        }

        /// <summary>
        /// Apply settings without saving to disk
        /// </summary>
        internal bool ApplySettingsLight()
        {
            if (AnythingSettings.Instance == null || TransformSettings.GetInstance() == null)
            {
                Debug.LogError("No AnythingSettings instance located.");
                return false;
            }

            var generalSettingsSerializedObject = new SerializedObject(AnythingSettings.Instance);
            var transformSettingsSerializedObject = new SerializedObject(TransformSettings.GetInstance());

            transformSettingsSerializedObject.FindProperty("gridAreaEnabled").boolValue = GridArea.IsReady();
            transformSettingsSerializedObject.FindProperty("gridAreaOrigin").vector3Value = GridArea.origin;
            transformSettingsSerializedObject.FindProperty("gridAreaSize").vector2Value = GridArea.areaSize;
            transformSettingsSerializedObject.FindProperty("gridAreaForward").vector3Value = GridArea.areaForward;
            transformSettingsSerializedObject.FindProperty("gridAreaRight").vector3Value = GridArea.areaRight;
            transformSettingsSerializedObject.FindProperty("gridAreaFitMode").boolValue = gridAreaFitBool;
            transformSettingsSerializedObject.FindProperty("gridAreaClipMode").boolValue = gridAreaClipBool;
            transformSettingsSerializedObject.FindProperty("gridAreaObjectsPerRow").intValue = gridAreaFitRow ;
            transformSettingsSerializedObject.FindProperty("gridAreaObjectsDistance").vector3Value = gridAreaClipDistance;
            transformSettingsSerializedObject.FindProperty("gridAreaShowPositions").boolValue = gridAreaViewBool;
            transformSettingsSerializedObject.FindProperty("gridAreaCanGrow").boolValue = gridAreaGrowBool;
            transformSettingsSerializedObject.FindProperty("gridAreaIgnoreCollision").boolValue = gridAreaIgnoreBool;
            transformSettingsSerializedObject.FindProperty("gridAreaRandomOffset").boolValue = gridAreaRandomBool;

            transformSettingsSerializedObject.ApplyModifiedProperties();
            generalSettingsSerializedObject.ApplyModifiedProperties();

            return true;
        }
        /// <summary>
        /// Draw the window GUI
        /// </summary>
        protected new void OnGUI()
        {
            base.OnGUI();
            #region Overwriting Editor Styles
            var backupLabelStyle = new GUIStyle(EditorStyles.label);
            var backupObjectStyle = new GUIStyle(EditorStyles.objectField);
            var backupNumberStyle = new GUIStyle(EditorStyles.numberField);
            var backupFoldoutStyle = new GUIStyle(EditorStyles.foldout);

            EditorStyles.label.font = GetPoppinsFont(PoppinsStyle.Bold);
            EditorStyles.objectField.font = GetPoppinsFont(PoppinsStyle.Medium);
            EditorStyles.numberField.font = GetPoppinsFont(PoppinsStyle.Medium);
            EditorStyles.foldout.font = GetPoppinsFont(PoppinsStyle.Bold);
            EditorStyles.foldout.fontSize = 16;
            #endregion Overwriting Editor Styles

            try
            {
                _ = InitializeResources();
                DrawTransformSettings();
                #region Resetting Editor Styles
                EditorStyles.label.font = backupLabelStyle.font;
                EditorStyles.objectField.font = backupObjectStyle.font;
                EditorStyles.numberField.font = backupNumberStyle.font;
                EditorStyles.foldout.font = backupFoldoutStyle.font;
                EditorStyles.foldout.fontSize = backupFoldoutStyle.fontSize;
                #endregion Resetting Editor Styles
            }
            catch
            {
                #region Resetting Editor Styles
                EditorStyles.label.font = backupLabelStyle.font;
                EditorStyles.objectField.font = backupObjectStyle.font;
                EditorStyles.numberField.font = backupNumberStyle.font;
                EditorStyles.foldout.font = backupFoldoutStyle.font;
                EditorStyles.foldout.fontSize = backupFoldoutStyle.fontSize;
                #endregion Resetting Editor Styles
            }
        }
        /// <summary>
        /// Check for changes and  save
        /// </summary>
        private void OnDestroy()
        {
            if (CheckForChanges())
            {
                GridArea.Reset();
                ApplySettings();
            }
        }
        /// <summary>
        /// Check for changes
        /// </summary>
        protected bool CheckForChanges()
        {
            bool changesDetected = false;

            if (AnythingSettings.Instance == null || TransformSettings.GetInstance() == null)
            {
                Debug.LogError("No AnythingSettings instance located.");
            }
            else
            {
                var transformSettingsSerializedObject = new SerializedObject(TransformSettings.GetInstance());
                var generalSettingsSerializedObject = new SerializedObject(AnythingSettings.Instance);
                changesDetected = transformSettingsSerializedObject.FindProperty("gridOrigin").vector3Value != gridOrigin ||
                                  transformSettingsSerializedObject.FindProperty("gridCellCount").intValue != gridCellCount ||
                                  transformSettingsSerializedObject.FindProperty("gridCellWidth").floatValue != gridCellWidth ||
                                  transformSettingsSerializedObject.FindProperty("gridAreaEnabled").boolValue != GridArea.IsReady() ||
                                  transformSettingsSerializedObject.FindProperty("gridAreaOrigin").vector3Value != GridArea.origin ||
                                  transformSettingsSerializedObject.FindProperty("gridAreaSize").vector2Value.x != GridArea.areaSize.x ||
                                  transformSettingsSerializedObject.FindProperty("gridAreaSize").vector2Value.y != GridArea.areaSize.y ||
                                  transformSettingsSerializedObject.FindProperty("gridAreaForward").vector3Value != GridArea.areaForward ||
                                  transformSettingsSerializedObject.FindProperty("gridAreaRight").vector3Value != GridArea.areaRight ||
                                  transformSettingsSerializedObject.FindProperty("gridAreaFitMode").boolValue != gridAreaFitBool ||
                                  transformSettingsSerializedObject.FindProperty("gridAreaClipMode").boolValue != gridAreaClipBool ||
                                  transformSettingsSerializedObject.FindProperty("gridAreaObjectsPerRow").intValue != gridAreaFitRow  ||
                                  transformSettingsSerializedObject.FindProperty("gridAreaObjectsDistance").vector3Value != gridAreaClipDistance ||
                                  transformSettingsSerializedObject.FindProperty("gridAreaShowPositions").boolValue != gridAreaViewBool ||
                                  transformSettingsSerializedObject.FindProperty("gridAreaCanGrow").boolValue != gridAreaGrowBool ||
                                  transformSettingsSerializedObject.FindProperty("gridAreaIgnoreCollision").boolValue != gridAreaIgnoreBool ||
                                  transformSettingsSerializedObject.FindProperty("gridAreaRandomOffset").boolValue != gridAreaRandomBool;
            }

            return changesDetected;
        }

        /// <summary>
        /// Draw the Transform Settings GUI
        /// </summary>
        protected void DrawTransformSettings()
        {
            int settingsCount = 9;
            int paddingCount = settingsCount;

            fieldPadding = 12f;
            fieldLabelWidthPercentage = 0.4f;
            var fieldHeight = 25f;

            GUILayout.Space(fieldPadding);
            float scrollBarAllowance = 6;

            var lastRect = GUILayoutUtility.GetLastRect();
            var settingsArea = new Rect(0, lastRect.yMax, position.width - scrollBarAllowance, (fieldHeight * settingsCount));
            var view = new Rect(0, lastRect.yMax, position.width, (position.height - (fieldPadding * 3)) - lastRect.yMax);

            // scrollPosition = GUI.BeginScrollView(view, scrollPosition, settingsArea, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);

            GUILayout.Space(fieldPadding);

            #region Grid Settings
            var gridDividerRect = GUILayoutUtility.GetRect(position.width, 0);
            DrawUILine(Color.white, gridDividerRect.position, gridDividerRect.width);
            GUILayout.Space(fieldPadding);

            var gridOriginVectorFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            gridOriginVectorFieldRect.x = 0;
            gridOriginVectorFieldRect.width = settingsArea.width;
            CustomVectorField(ref gridOrigin, ref tempGridOriginX, ref tempGridOriginY, ref tempGridOriginZ, new GUIContent("Grid Origin", "This setting defines the starting point for the first model placed on the grid in your scene. It essentially marks the initial position from which the grid layout begins. By adjusting the Grid Origin, you can control where your grid-aligned objects start being placed."), gridOriginVectorFieldRect);
            GUILayout.Space(fieldPadding);

            // choose which setting to show
            if (GridArea.IsReady())
            {
                // grid area settings
                if (gridAreaFitBool)
                {
                    var gridAreaFitRowFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
                    gridAreaFitRowFieldRect.x = 0;
                    gridAreaFitRowFieldRect.width = settingsArea.width;
                    if (CustomIntField(ref gridAreaFitRow, ref tempGridAreaFitRow, new GUIContent("Models Per Row", "This setting sets the number of models to fit in a row of the grid."), gridAreaFitRowFieldRect))
                    {
                        GridArea.SetFitMode(gridAreaFitRow);
                        GridArea.RearrangeObjects();
                        SceneView.RepaintAll();
                    }
                    GUILayout.Space(fieldPadding);
                }
                else
                {
                    var gridAreaClipDistanceFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
                    gridAreaClipDistanceFieldRect.x = 0;
                    gridAreaClipDistanceFieldRect.width = settingsArea.width;
                    if (CustomVectorField(ref gridAreaClipDistance, ref tempGridAreaClipDistanceX, ref tempGridAreaClipDistanceY, ref tempGridAreaClipDistanceZ, new GUIContent("Distance", "This setting sets the distance between models when placed on the grid."), gridAreaClipDistanceFieldRect))
                    {
                        GridArea.SetClipMode(new Vector2(gridAreaClipDistance.x, gridAreaClipDistance.z));
                        GridArea.RearrangeObjects();
                        SceneView.RepaintAll();
                    }
                    GUILayout.Space(fieldPadding);
                }
            }

            // grid area settings
            var gridAreaFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            gridAreaFieldRect.x = 0;
            gridAreaFieldRect.width = settingsArea.width;
            CustomTransformField(ref defaultGridAreaObject, new GUIContent("GameObject For Grid Area", "This is the GameObject to use to calculate the bounds of the area where the grid will be placed"), gridAreaFieldRect);
            GUILayout.Space(fieldPadding);

            // detect new dragged objects
            if (oldGridAreaObject != defaultGridAreaObject)
            {
                if (defaultGridAreaObject != null)
                {
                    // initial process for the new area object
                    GridArea.GetSizeFromObject(defaultGridAreaObject.gameObject);
                    GridArea.SetPlaceOnGroundMode(TransformSettings.PlaceOnGround);
                    if (gridAreaFitBool)
                        GridArea.SetFitMode(gridAreaFitRow);
                    else
                        GridArea.SetClipMode(new Vector2(gridAreaClipDistance.x, gridAreaClipDistance.z));
                    GridArea.SetShowPositionsMode(gridAreaViewBool);
                    GridArea.SetGrowMode(gridAreaGrowBool);
                    GridArea.EnableColliders(defaultGridAreaObject.gameObject, gridAreaIgnoreBool);
                    GridArea.SetRandomMode(gridAreaRandomBool);
                    GridArea.RearrangeObjects();
                    SceneView.RepaintAll();
                }
                else
                {
                    // keep objects and reset
                    GridArea.KeepModelsAndReset();
                }
                oldGridAreaObject = defaultGridAreaObject;
            }

            // refreshing values
            if (GridArea.IsReady())
            {
                // refresh values
                tempGridOriginX = GridArea.origin.x.ToString();
                tempGridOriginY = GridArea.origin.y.ToString();
                tempGridOriginZ = GridArea.origin.z.ToString();
                gridCellWidth = GridArea.objectsDistance.x;
                gridCellCount = GridArea.objectsPerRow;
            }

            if (GridArea.IsReady())
            {
                var gridAreaFitBoolFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
                gridAreaFitBoolFieldRect.x = 0;
                gridAreaFitBoolFieldRect.width = settingsArea.width;
                if (CustomBoolField(ref gridAreaFitBool, ref gridAreaClipBool, new GUIContent("Distribution Mode", "The FIT mode will rearrange all rows of models created inside the area, while the CLIP mode will create the rows of models at constant distance."), gridAreaFitBoolFieldRect, "Fit", "Clip"))
                {
                    if (gridAreaFitBool)
                        GridArea.SetFitMode(gridAreaFitRow);
                    else
                        GridArea.SetClipMode(new Vector2(gridAreaClipDistance.x, gridAreaClipDistance.z));
                    GridArea.RearrangeObjects();
                    SceneView.RepaintAll();
                }
                GUILayout.Space(fieldPadding);

                var gridAreaViewBoolFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
                gridAreaViewBoolFieldRect.x = 0;
                gridAreaViewBoolFieldRect.width = settingsArea.width;
                if (CustomBoolField(ref gridAreaViewBool, new GUIContent("View All Grid Positions", "This setting allows to preview the next positions where the models will be created next."), gridAreaViewBoolFieldRect))
                {
                    GridArea.SetShowPositionsMode(gridAreaViewBool);
                    GridArea.RearrangeObjects();
                    SceneView.RepaintAll();
                }
                GUILayout.Space(fieldPadding);

                var gridAreaGrowBoolFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
                gridAreaGrowBoolFieldRect.x = 0;
                gridAreaGrowBoolFieldRect.width = settingsArea.width;
                if (CustomBoolField(ref gridAreaGrowBool, new GUIContent("Grow Outside Area", "This setting allows to create rows of models outside the limits of the area when using CLIP mode."), gridAreaGrowBoolFieldRect))
                {
                    GridArea.SetGrowMode(gridAreaGrowBool);
                    GridArea.RearrangeObjects();
                    SceneView.RepaintAll();
                }
                GUILayout.Space(fieldPadding);

                var gridAreaIgnoreBoolFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
                gridAreaIgnoreBoolFieldRect.x = 0;
                gridAreaIgnoreBoolFieldRect.width = settingsArea.width;
                if (CustomBoolField(ref gridAreaIgnoreBool, new GUIContent("Ignore Area Collision", "This setting will make the area GameObject invisible to place models on the ground. Otherwise, the models could be placed over the area GameObject instead of the ground."), gridAreaIgnoreBoolFieldRect))
                {
                    GridArea.EnableColliders(defaultGridAreaObject.gameObject, gridAreaIgnoreBool);
                    GridArea.RearrangeObjects();
                    SceneView.RepaintAll();
                }
                GUILayout.Space(fieldPadding);

                var gridAreaRandomBoolFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
                gridAreaRandomBoolFieldRect.x = 0;
                gridAreaRandomBoolFieldRect.width = settingsArea.width;
                if (CustomBoolField(ref gridAreaRandomBool, new GUIContent("Random Offset", "This setting moves all models around with a random offset to break the regular alignment."), gridAreaRandomBoolFieldRect))
                {
                    GridArea.SetRandomMode(gridAreaRandomBool);
                    GridArea.RearrangeObjects();
                    SceneView.RepaintAll();
                }
                GUILayout.Space(fieldPadding);

                var gridAreaKeepBoolFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
                gridAreaKeepBoolFieldRect.x = fieldPadding;
                gridAreaKeepBoolFieldRect.width = (settingsArea.width / 2f) - fieldPadding;
                if (DrawRoundedButton(gridAreaKeepBoolFieldRect, new GUIContent("Finish", "This option will finish and keep all models created so far.")))
                {
                    GridArea.KeepModelsAndReset();
                    // deactivate object for area
                    defaultGridAreaObject = null;
                    CloseWindowIfOpen<GridAreaSettingsEditor>();
                }
                var gridAreaResetBoolFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
                gridAreaResetBoolFieldRect.x = (settingsArea.width / 2f) + fieldPadding;
                gridAreaResetBoolFieldRect.y = gridAreaKeepBoolFieldRect.y;
                gridAreaResetBoolFieldRect.width = (settingsArea.width / 2f) - fieldPadding * 2;
                if (DrawRoundedButton(gridAreaResetBoolFieldRect, new GUIContent("Cancel", "This option will finish and remove all models created so far.")))
                {
                    GridArea.Reset();
                    // deactivate object for area
                    defaultGridAreaObject = null;
                    CloseWindowIfOpen<GridAreaSettingsEditor>();
                }
                GUILayout.Space(fieldPadding);
            }
            GUILayout.Space(fieldPadding);
            #endregion Grid Settings

            // GUI.EndScrollView();

            //if any interaction was detected, apply settings lightly
            if (GUI.changed)
            {
                ApplySettingsLight();
            }
        }
    }
}
