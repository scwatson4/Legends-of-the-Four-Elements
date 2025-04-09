using AnythingWorld.Utilities;
using AnythingWorld.Behaviour.Tree;
using AnythingWorld.Behaviour;
using UnityEditor;
using UnityEngine;

using System.Collections.Generic;
using System;

namespace AnythingWorld.Editor
{
    /// <summary>
    /// Editor window for Transform Settings
    /// </summary>
    public class TransformSettingsEditor : AnythingEditor
    {
        private string tempObjectPositionX = "", tempObjectPositionY = "", tempObjectPositionZ = "";
        private string tempObjectRotationX = "", tempObjectRotationY = "", tempObjectRotationZ = "";
        private string tempObjectScalar = "";

        private string tempGridOriginX = "", tempGridOriginY = "", tempGridOriginZ = "";
        private string tempCellWidth = "";
        private string tempCellCount = "";

        protected bool showGridOptionsDrawer = false;
        protected bool showDefaultBehavioursDrawer = false;
        protected bool showTransformDrawer = false;
        protected bool showGeneralDrawer = false;
        protected bool showPhysicsDrawer = false;
        protected bool gridPlacementEnabled = false;
        protected bool transformSettingsActive = false;

        private bool animateModel = true;
        private bool cacheModel = true;
        private bool placeOnGround = true;
        private bool placeOnGrid = true;
        private bool clickInPlacementLocation = true;
        private bool continueUntilCancelled = false;
        private bool loadingImageActive = false;
        private bool followCam = false;
        private bool loadingMessage = true;

        private bool serializeAsset = false;
        private bool addCollider = true;
        private bool addRigidbody = true;
        private bool showGridHandles = false;
        private bool repositioning = true;
        private bool debugOn = false;

        private static Transform objectParent;
        private Vector3 objectPosition;
        private Vector3 objectRotation;
        private float objectScale;

        private bool behaviourChanged;

        private bool customGroundCreatureBehaviour;
        private int groundCreatureBehaviourIndex;
        private DropdownOption[] GroundCreatureDropdownOptions;
        private BehaviourTree groundCreatureBehaviourTree;

        private bool customGroundVehicleBehaviour;
        private int groundVehicleBehaviourIndex;
        private DropdownOption[] GroundVehicleDropdownOptions;
        private BehaviourTree groundVehicleBehaviourTree;

        private bool customFlyingCreatureBehaviour;
        private int flyingCreatureBehaviourIndex;
        private DropdownOption[] FlyingCreatureDropdownOptions;
        private BehaviourTree flyingCreatureBehaviourTree;

        private bool customFlyingVehicleBehaviour;
        private int flyingVehicleBehaviourIndex;
        private DropdownOption[] FlyingVehicleDropdownOptions;
        private BehaviourTree flyingVehicleBehaviourTree;

        private bool customSwimmingCreatureBehaviour;
        private int swimmingCreatureBehaviourIndex;
        private DropdownOption[] SwimmingCreatureDropdownOptions;
        private BehaviourTree swimmingCreatureBehaviourTree;

        private bool customStaticBehaviour;
        private int staticBehaviourIndex;
        private DropdownOption[] StaticDropdownOptions;
        private BehaviourTree staticBehaviourTree;

        private Vector3 gridOrigin;
        private int gridCellCount;
        private float gridCellWidth;
        private Vector2 scrollPosition;

        /// <summary>
        /// Initialize Transform Settings Editor Window and load settings from disk if they exist
        /// </summary>
        [MenuItem("Tools/Anything World/Transform Settings", false, 42)]
        public static void Initialize()
        {
            Resources.LoadAll<AnythingSettings>("Settings");
            Resources.LoadAll<TransformSettings>("Settings");

            AnythingCreatorEditor tabWindow;
            Vector2 windowSize;

            if (AnythingSettings.HasAPIKey)
            {
                windowSize = new Vector2(425, 540);

                var browser = GetWindow(typeof(TransformSettingsEditor), false, "Transform Settings") as TransformSettingsEditor;
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
            cacheModel = TransformSettings.CacheModel;
            animateModel = TransformSettings.AnimateModel;
            placeOnGround = TransformSettings.PlaceOnGround;

            clickInPlacementLocation = TransformSettings.ClickInPlacementLocation;
            placeOnGrid = !TransformSettings.ClickInPlacementLocation;
            continueUntilCancelled = TransformSettings.ContinueUntilCancelled;
            loadingImageActive = TransformSettings.LoadingImageActive;
            followCam = TransformSettings.FollowCam;
            loadingMessage = TransformSettings.LoadingMessage;

            serializeAsset = TransformSettings.SerializeAsset;
            addCollider = TransformSettings.AddCollider;
            addRigidbody = TransformSettings.AddRigidbody;
            showGridHandles = TransformSettings.ShowGridHandles;
            repositioning = TransformSettings.Repositioning;
            debugOn = AnythingSettings.DebugEnabled;

            objectParent = AnythingCreatorEditor.objectParent;
            objectPosition = TransformSettings.PositionField;
            objectRotation = TransformSettings.RotationField;
            objectScale = TransformSettings.ScaleField;

            RecompileBehaviours();

            gridOrigin = TransformSettings.GridOrigin;
            gridCellCount = TransformSettings.GridCellCount;
            gridCellWidth = TransformSettings.GridCellWidth;

            tempGridOriginX = SimpleGrid.origin.x.ToString();
            tempGridOriginY = SimpleGrid.origin.y.ToString();
            tempGridOriginZ = SimpleGrid.origin.z.ToString();
            tempCellWidth = SimpleGrid.cellWidth.ToString();
            tempCellCount = SimpleGrid.cellCount.ToString();

            tempObjectPositionX = objectPosition.x.ToString();
            tempObjectPositionY = objectPosition.y.ToString();
            tempObjectPositionZ = objectPosition.z.ToString();
            tempObjectRotationX = objectRotation.x.ToString();
            tempObjectRotationY = objectRotation.y.ToString();
            tempObjectRotationZ = objectRotation.z.ToString();
            tempObjectScalar = objectScale.ToString();
        }

        void RecompileBehaviours()
        {
            GroundCreatureDropdownOptions = CreateBehaviourDropdownOptions(DefaultBehaviourType.GroundCreature);
            GroundVehicleDropdownOptions = CreateBehaviourDropdownOptions(DefaultBehaviourType.GroundVehicle);
            FlyingCreatureDropdownOptions = CreateBehaviourDropdownOptions(DefaultBehaviourType.FlyingCreature);
            FlyingVehicleDropdownOptions = CreateBehaviourDropdownOptions(DefaultBehaviourType.FlyingVehicle);
            SwimmingCreatureDropdownOptions = CreateBehaviourDropdownOptions(DefaultBehaviourType.SwimmingCreature);
            StaticDropdownOptions = CreateBehaviourDropdownOptions(DefaultBehaviourType.Static);

            groundCreatureBehaviourTree = TransformSettings.GroundCreatureBehaviourTree;
            groundCreatureBehaviourIndex = Array.FindIndex(CuratedBehaviourPreset.Instance.groundCreatureBehaviours, a => a.behaviourTree == groundCreatureBehaviourTree);
            if (groundCreatureBehaviourIndex < 0)
            {
                if (groundCreatureBehaviourTree == null)
                {
                    groundCreatureBehaviourIndex = CuratedBehaviourPreset.Instance.groundCreatureBehaviours.Length;
                }
                else
                {
                    groundCreatureBehaviourIndex = CuratedBehaviourPreset.Instance.groundCreatureBehaviours.Length + 1;
                    customGroundCreatureBehaviour = true;
                }
            }

            groundVehicleBehaviourTree = TransformSettings.GroundVehicleBehaviourTree;
            groundVehicleBehaviourIndex = Array.FindIndex(CuratedBehaviourPreset.Instance.groundVehicleBehaviours, a => a.behaviourTree == groundVehicleBehaviourTree);
            if (groundVehicleBehaviourIndex < 0)
            {
                if (groundVehicleBehaviourTree == null)
                {
                    groundVehicleBehaviourIndex = CuratedBehaviourPreset.Instance.groundVehicleBehaviours.Length;
                }
                else
                {
                    groundVehicleBehaviourIndex = CuratedBehaviourPreset.Instance.groundVehicleBehaviours.Length + 1;
                    customGroundVehicleBehaviour = true;
                }
            }

            flyingCreatureBehaviourTree = TransformSettings.FlyingCreatureBehaviourTree;
            flyingCreatureBehaviourIndex = Array.FindIndex(CuratedBehaviourPreset.Instance.flyingCreatureBehaviours, a => a.behaviourTree == flyingCreatureBehaviourTree);
            if (flyingCreatureBehaviourIndex < 0)
            {
                if (flyingCreatureBehaviourTree == null)
                {
                    flyingCreatureBehaviourIndex = CuratedBehaviourPreset.Instance.flyingCreatureBehaviours.Length;
                }
                else
                {
                    flyingCreatureBehaviourIndex = CuratedBehaviourPreset.Instance.flyingCreatureBehaviours.Length + 1;
                    customFlyingCreatureBehaviour = true;
                }
            }

            flyingVehicleBehaviourTree = TransformSettings.FlyingVehicleBehaviourTree;
            flyingVehicleBehaviourIndex = Array.FindIndex(CuratedBehaviourPreset.Instance.flyingVehicleBehaviours, a => a.behaviourTree == flyingVehicleBehaviourTree);
            if (flyingVehicleBehaviourIndex < 0)
            {
                if (flyingVehicleBehaviourTree == null)
                {
                    flyingVehicleBehaviourIndex = CuratedBehaviourPreset.Instance.flyingVehicleBehaviours.Length;
                }
                else
                {
                    flyingVehicleBehaviourIndex = CuratedBehaviourPreset.Instance.flyingVehicleBehaviours.Length + 1;
                    customFlyingVehicleBehaviour = true;
                }
            }

            swimmingCreatureBehaviourTree = TransformSettings.SwimmingCreatureBehaviourTree;
            swimmingCreatureBehaviourIndex = Array.FindIndex(CuratedBehaviourPreset.Instance.swimmingCreatureBehaviours, a => a.behaviourTree == swimmingCreatureBehaviourTree);
            if (swimmingCreatureBehaviourIndex < 0)
            {
                if (swimmingCreatureBehaviourTree == null)
                {
                    swimmingCreatureBehaviourIndex = CuratedBehaviourPreset.Instance.swimmingCreatureBehaviours.Length;
                }
                else
                {
                    swimmingCreatureBehaviourIndex = CuratedBehaviourPreset.Instance.swimmingCreatureBehaviours.Length + 1;
                    customSwimmingCreatureBehaviour = true;
                }
            }

            staticBehaviourTree = TransformSettings.StaticBehaviourTree;
            staticBehaviourIndex = Array.FindIndex(CuratedBehaviourPreset.Instance.staticBehaviours, a => a.behaviourTree == staticBehaviourTree);
            if (staticBehaviourIndex < 0)
            {
                if (staticBehaviourTree == null)
                {
                    staticBehaviourIndex = CuratedBehaviourPreset.Instance.staticBehaviours.Length;
                }
                else
                {
                    staticBehaviourIndex = CuratedBehaviourPreset.Instance.staticBehaviours.Length + 1;
                    customStaticBehaviour = true;
                }
            }
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
            transformSettingsSerializedObject.FindProperty("animateModel").boolValue = animateModel;
            transformSettingsSerializedObject.FindProperty("cacheModel").boolValue = cacheModel;
            generalSettingsSerializedObject.FindProperty("showDebugMessages").boolValue = debugOn;
            transformSettingsSerializedObject.FindProperty("placeOnGround").boolValue = placeOnGround;
            transformSettingsSerializedObject.FindProperty("placeOnGrid").boolValue = placeOnGrid;
            transformSettingsSerializedObject.FindProperty("clickInPlacementLocation").boolValue = clickInPlacementLocation;
            transformSettingsSerializedObject.FindProperty("continueUntilCancelled").boolValue = continueUntilCancelled;
            transformSettingsSerializedObject.FindProperty("loadingImageActive").boolValue = loadingImageActive;
            transformSettingsActive = transformSettingsSerializedObject.FindProperty("followCam").boolValue = followCam;

            transformSettingsActive = transformSettingsSerializedObject.FindProperty("loadingMessage").boolValue = loadingMessage;

            transformSettingsSerializedObject.FindProperty("serializeAsset").boolValue = serializeAsset;

            transformSettingsSerializedObject.FindProperty("addCollider").boolValue = addCollider;
            transformSettingsSerializedObject.FindProperty("addRigidbody").boolValue = addRigidbody;

            AnythingCreatorEditor.objectParent = objectParent;
            transformSettingsSerializedObject.FindProperty("objectPosition").vector3Value = objectPosition;
            transformSettingsSerializedObject.FindProperty("objectRotation").vector3Value = objectRotation;
            transformSettingsSerializedObject.FindProperty("objectScaleMultiplier").floatValue = objectScale;

            transformSettingsSerializedObject.FindProperty("groundCreatureBehaviourTree").objectReferenceValue = groundCreatureBehaviourTree;
            transformSettingsSerializedObject.FindProperty("groundVehicleBehaviourTree").objectReferenceValue = groundVehicleBehaviourTree;
            transformSettingsSerializedObject.FindProperty("flyingCreatureBehaviourTree").objectReferenceValue = flyingCreatureBehaviourTree;
            transformSettingsSerializedObject.FindProperty("flyingVehicleBehaviourTree").objectReferenceValue = flyingVehicleBehaviourTree;
            transformSettingsSerializedObject.FindProperty("swimmingCreatureBehaviourTree").objectReferenceValue = swimmingCreatureBehaviourTree;
            transformSettingsSerializedObject.FindProperty("staticBehaviourTree").objectReferenceValue = staticBehaviourTree;

            transformSettingsSerializedObject.FindProperty("showGridHandles").boolValue = showGridHandles;
            transformSettingsSerializedObject.FindProperty("repositioning").boolValue = repositioning;
            SimpleGrid.origin = transformSettingsSerializedObject.FindProperty("gridOrigin").vector3Value = gridOrigin;
            SimpleGrid.cellCount = transformSettingsSerializedObject.FindProperty("gridCellCount").intValue = gridCellCount;
            SimpleGrid.cellWidth = transformSettingsSerializedObject.FindProperty("gridCellWidth").floatValue = gridCellWidth;

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
                InitializeResources();
                if(GroundCreatureDropdownOptions == null ||
                   GroundVehicleDropdownOptions == null ||
                   FlyingCreatureDropdownOptions == null ||
                   FlyingVehicleDropdownOptions == null ||
                   SwimmingCreatureDropdownOptions == null ||
                   StaticDropdownOptions == null)
                {
                    RecompileBehaviours();
                }
                DrawTransformSettings();
                #region Resetting Editor Styles
                EditorStyles.label.font = backupLabelStyle.font;
                EditorStyles.objectField.font = backupObjectStyle.font;
                EditorStyles.numberField.font = backupNumberStyle.font;
                EditorStyles.foldout.font = backupFoldoutStyle.font;
                EditorStyles.foldout.fontSize = backupFoldoutStyle.fontSize;
                #endregion Resetting Editor Styles
            }
            catch (Exception e)
            {
                Debug.LogError(e);
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
                changesDetected = transformSettingsSerializedObject.FindProperty("animateModel").boolValue != animateModel ||
                                  transformSettingsSerializedObject.FindProperty("cacheModel").boolValue != cacheModel ||
                                  generalSettingsSerializedObject.FindProperty("showDebugMessages").boolValue != debugOn ||
                                  transformSettingsSerializedObject.FindProperty("placeOnGround").boolValue != placeOnGround ||
                                  transformSettingsSerializedObject.FindProperty("placeOnGrid").boolValue != placeOnGrid ||
                                  transformSettingsSerializedObject.FindProperty("clickInPlacementLocation").boolValue != clickInPlacementLocation ||
                                  transformSettingsSerializedObject.FindProperty("continueUntilCancelled").boolValue != continueUntilCancelled ||
                                  transformSettingsSerializedObject.FindProperty("loadingImageActive").boolValue != loadingImageActive ||
                                  transformSettingsSerializedObject.FindProperty("followCam").boolValue != followCam ||
                                  transformSettingsSerializedObject.FindProperty("serializeAsset").boolValue != serializeAsset ||
                                  transformSettingsSerializedObject.FindProperty("loadingMessage").boolValue != loadingMessage ||

                                  transformSettingsSerializedObject.FindProperty("addCollider").boolValue != addCollider ||
                                  transformSettingsSerializedObject.FindProperty("addRigidbody").boolValue != addRigidbody ||

                                  AnythingCreatorEditor.objectParent != objectParent ||
                                  transformSettingsSerializedObject.FindProperty("objectPosition").vector3Value != objectPosition ||
                                  transformSettingsSerializedObject.FindProperty("objectRotation").vector3Value != objectRotation ||
                                  transformSettingsSerializedObject.FindProperty("objectScaleMultiplier").floatValue != objectScale ||

                                  transformSettingsSerializedObject.FindProperty("groundVehicleBehaviourTree").objectReferenceValue != groundVehicleBehaviourTree ||
                                  transformSettingsSerializedObject.FindProperty("groundCreatureBehaviourTree").objectReferenceValue != groundCreatureBehaviourTree ||
                                  transformSettingsSerializedObject.FindProperty("flyingCreatureBehaviourTree").objectReferenceValue != flyingCreatureBehaviourTree ||
                                  transformSettingsSerializedObject.FindProperty("flyingVehicleBehaviourTree").objectReferenceValue != flyingVehicleBehaviourTree ||
                                  transformSettingsSerializedObject.FindProperty("swimmingCreatureBehaviourTree").objectReferenceValue != swimmingCreatureBehaviourTree ||
                                  transformSettingsSerializedObject.FindProperty("staticBehaviourTree").objectReferenceValue != staticBehaviourTree ||

                                  transformSettingsSerializedObject.FindProperty("showGridHandles").boolValue != showGridHandles ||
                                  transformSettingsSerializedObject.FindProperty("repositioning").boolValue != repositioning ||
                                  transformSettingsSerializedObject.FindProperty("gridOrigin").vector3Value != gridOrigin ||
                                  transformSettingsSerializedObject.FindProperty("gridCellCount").intValue != gridCellCount ||
                                  transformSettingsSerializedObject.FindProperty("gridCellWidth").floatValue != gridCellWidth;
            }

            return changesDetected;
        }

        /// <summary>
        /// Draw the Transform Settings GUI
        /// </summary>
        protected void DrawTransformSettings()
        {
            int settingsCount = 33;
            int paddingCount = settingsCount + 5;

            fieldPadding = 12f;
            fieldLabelWidthPercentage = 0.4f;
            var fieldHeight = 25f;
            GUIContent content;

            var warningHeightOffset = 0f;
            string placeOnGroundString;
            string placeOnGridString;

            GUIContent modifierWarningBoxContent = new GUIContent();
            GUIStyle modifierWarningBoxStyle = new GUIStyle();

            GUILayout.Space(fieldPadding);
            float scrollBarAllowance = 6;
            if (TransformSettings.PlaceOnGround || TransformSettings.PlaceOnGrid)
            {
                placeOnGroundString = TransformSettings.PlaceOnGround ? "WARNING: Place on Top of Ground is currently active. This might impact the Transform settings and might result in unexpected behaviour!" : "";
                placeOnGridString = TransformSettings.PlaceOnGrid ? "WARNING: Grid Placement is currently active. This might impact the Transform settings and might result in unexpected behaviour!" : "";

                modifierWarningBoxContent = new GUIContent($"{placeOnGroundString}{(TransformSettings.PlaceOnGround && TransformSettings.PlaceOnGrid ? "\n" : "")}{placeOnGridString}");
                modifierWarningBoxStyle = new GUIStyle(BodyLabelStyle) { padding = new RectOffset((int)fieldPadding, (int)fieldPadding, 0, 0) };

                warningHeightOffset = modifierWarningBoxStyle.CalcHeight(modifierWarningBoxContent, position.width - scrollBarAllowance);
            }

            if (!TransformSettings.PlaceOnGrid)
            {
                settingsCount += 1;
                if (TransformSettings.PlaceOnGround)
                    paddingCount += 1;
            }

            var lastRect = GUILayoutUtility.GetLastRect();
            var settingsArea = new Rect(0, lastRect.yMax, position.width - scrollBarAllowance, (fieldHeight * settingsCount) + (fieldPadding * paddingCount) + warningHeightOffset);
            var view = new Rect(0, lastRect.yMax, position.width, (position.height  - (fieldPadding * 3)) - lastRect.yMax);
            scrollPosition = GUI.BeginScrollView(view, scrollPosition, settingsArea, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);

            #region General Settings Drawer
            content = new GUIContent("Cache Model", "Caches reference to a model in the scene after import to speed up " +
                                                    "subsequent imports. If no cached model available " +
                                                    "tries to instantiate it from serialized prefab.");
            var cacheModelBoolFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            cacheModelBoolFieldRect.x = 0;
            cacheModelBoolFieldRect.width = settingsArea.width;
            CustomBoolField(ref cacheModel, content, cacheModelBoolFieldRect);
            GUILayout.Space(fieldPadding);
            
            content = new GUIContent("Animate Model", "If animations exist for this model, disable this option to override it and make a static object.");
            var animateModelBoolFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            animateModelBoolFieldRect.x = 0;
            animateModelBoolFieldRect.width = settingsArea.width;
            CustomBoolField(ref animateModel, content, animateModelBoolFieldRect);
            GUILayout.Space(fieldPadding);

            content = new GUIContent("Enable Debug Messages", "Print debug messages from the make process into console.");
            var debugOnBoolFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            debugOnBoolFieldRect.x = 0;
            debugOnBoolFieldRect.width = settingsArea.width;
            CustomBoolField(ref debugOn, content, debugOnBoolFieldRect);
            GUILayout.Space(fieldPadding);

            content = new GUIContent("Serialize Assets", "Attempt to serialize model assets and put them into Assets/SavedAssets folder.");
            var serializeAssetBoolFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            serializeAssetBoolFieldRect.x = 0;
            serializeAssetBoolFieldRect.width = settingsArea.width;
            CustomBoolField(ref serializeAsset, content, serializeAssetBoolFieldRect);

            GUILayout.Space(fieldPadding);
            #endregion General Settings Drawer

            #region Placements Drawer
            var dividerRect = GUILayoutUtility.GetRect(position.width, 0);
            DrawUILine(Color.white, dividerRect.position, dividerRect.width);

            GUILayout.Space(fieldPadding);
            content = new GUIContent("Place on Top of Ground", "Align the object with the surface beneath it.");
            var placeOnGroundBoolFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            placeOnGroundBoolFieldRect.x = 0;
            placeOnGroundBoolFieldRect.width = settingsArea.width;
            if (CustomBoolField(ref placeOnGround, content, placeOnGroundBoolFieldRect))
            {
                if (GridArea.IsReady())
                {
                    GridArea.SetPlaceOnGroundMode(placeOnGround);
                    GridArea.RearrangeObjects();
                }
            }
            GUILayout.Space(fieldPadding);

            content = new GUIContent("Placement mode", "This button allows you to switch between 'Grid Placement' and 'Click-to-Place' modes. In 'Grid Placement' mode, objects snap to a predefined grid for precise alignment. In 'Click-to-Place' mode, you have the freedom to place objects anywhere without grid constraints.");
            var toggleGridPlace = GUILayoutUtility.GetRect(position.width, fieldHeight);
            toggleGridPlace.x = 0;
            toggleGridPlace.width = settingsArea.width;
            CustomBoolField(ref clickInPlacementLocation, ref placeOnGrid,content,toggleGridPlace, "Click to Place", "Grid Placement");
            GUILayout.Space(fieldPadding);

            var dividerPlacementRect = GUILayoutUtility.GetRect(position.width, 0);
            DrawUILine(Color.white, dividerPlacementRect.position, dividerPlacementRect.width);
            GUILayout.Space(fieldPadding);

            if (placeOnGrid)
            {
                content = new GUIContent("Repositioning if was in collision", "Reposition model if it was in collision with other models.");
                var repositioningBoolFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
                repositioningBoolFieldRect.x = 0;
                repositioningBoolFieldRect.width = settingsArea.width;
                CustomBoolField(ref repositioning, content, repositioningBoolFieldRect);
                GUILayout.Space(fieldPadding);

                content = new GUIContent("Show loading icon on screen", "Show the loading message while the model are loaded in the scene.");
                var loadingImageBoolFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
                loadingImageBoolFieldRect.x = 0;
                loadingImageBoolFieldRect.width = settingsArea.width;
                CustomBoolField(ref loadingMessage, content, loadingImageBoolFieldRect);
                GUILayout.Space(fieldPadding);
            }
            if (clickInPlacementLocation)
            {
                content = new GUIContent("Continue Until Cancelled", "Continue placing models until cancels.");
                var continueUntilCancelledBoolFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
                continueUntilCancelledBoolFieldRect.x = 0;
                continueUntilCancelledBoolFieldRect.width = settingsArea.width;
                CustomBoolField(ref continueUntilCancelled, content, continueUntilCancelledBoolFieldRect);
                GUILayout.Space(fieldPadding);

                content = new GUIContent("Show loading icon", "Show the icon of the model being loaded in the scene.");
                var loadingImageBoolFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
                loadingImageBoolFieldRect.x = 0;
                loadingImageBoolFieldRect.width = settingsArea.width;
                CustomBoolField(ref loadingImageActive, content, loadingImageBoolFieldRect);
                GUILayout.Space(fieldPadding);
            }
            content = new GUIContent("Snap camera to spawned", "Follow the camera when placing models.");
            var followCamBoolFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            followCamBoolFieldRect.x = 0;
            followCamBoolFieldRect.width = settingsArea.width;
            CustomBoolField(ref followCam, content, followCamBoolFieldRect);

            GUILayout.Space(fieldPadding);
            #endregion Placements Drawer

            #region Physics Drawer
            var physicsDividerRect = GUILayoutUtility.GetRect(position.width, 0);
            DrawUILine(Color.white, physicsDividerRect.position, physicsDividerRect.width);
            GUILayout.Space(fieldPadding);

            content = new GUIContent("Add Collider", "A collider encapsulating the mesh(es) to the top level GameObject.");
            var addColliderBoolFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            addColliderBoolFieldRect.x = 0;
            addColliderBoolFieldRect.width = settingsArea.width;
            CustomBoolField(ref addCollider, content, addColliderBoolFieldRect);
            GUILayout.Space(fieldPadding);

            content = new GUIContent("Add Rigidbody", "Add a default rigidbody to top level model GameObject.");
            var addRigidbodyBoolFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            addRigidbodyBoolFieldRect.x = 0;
            addRigidbodyBoolFieldRect.width = settingsArea.width;
            CustomBoolField(ref addRigidbody, content, addRigidbodyBoolFieldRect);
            GUILayout.Space(fieldPadding);
            #endregion Physics Drawer

            #region Transform Drawer
            var transformDividerRect = GUILayoutUtility.GetRect(position.width, 0);
            DrawUILine(Color.white, transformDividerRect.position, transformDividerRect.width);
            GUILayout.Space(fieldPadding);

            var transformHelpBoxContent = new GUIContent("Change positioning, rotation and scale of model within the scene");
            var transformHelpBoxStyle = new GUIStyle(BodyLabelStyle) { padding = new RectOffset((int)fieldPadding, (int)fieldPadding, 0, 0) };
            var transformHelpBoxRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            transformHelpBoxRect.x = 0;
            transformHelpBoxRect.width = settingsArea.width;
            GUI.Label(transformHelpBoxRect, transformHelpBoxContent, transformHelpBoxStyle);
            GUILayout.Space(fieldPadding);

            if (TransformSettings.PlaceOnGround || TransformSettings.PlaceOnGrid)
            {
                var modifierWarningBoxRect = GUILayoutUtility.GetRect(position.width, modifierWarningBoxStyle.CalcHeight(modifierWarningBoxContent, position.width - scrollBarAllowance));
                modifierWarningBoxRect.x = 0;
                modifierWarningBoxRect.width = settingsArea.width;
                GUI.Label(modifierWarningBoxRect, modifierWarningBoxContent, modifierWarningBoxStyle);
                GUILayout.Space(fieldPadding);
            }

            var objectTransformFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            objectTransformFieldRect.x = 0;
            objectTransformFieldRect.width = settingsArea.width;
            CustomTransformField(ref objectParent, new GUIContent("Parent Transform", "Use this field to establish a parent-child relationship between objects. Drag an object from the hierarchy into this field to set it as the 'parent'. The selected object ('child') will then inherit the transform properties (position, rotation, scale) of the parent."), objectTransformFieldRect);
            GUILayout.Space(fieldPadding);

            var objectPositionVectorFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            objectPositionVectorFieldRect.x = 0;
            objectPositionVectorFieldRect.width = settingsArea.width;
            CustomVectorField(ref objectPosition, ref tempObjectPositionX, ref tempObjectPositionY, ref tempObjectPositionZ, new GUIContent("Object Position", "This field defines the world position of the object in the scene. If no parent object is set, the position values here relate to the world coordinates, providing a global reference point."), objectPositionVectorFieldRect);
            GUILayout.Space(fieldPadding);

            var objectRotationVectorFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            objectRotationVectorFieldRect.x = 0;
            objectRotationVectorFieldRect.width = settingsArea.width;
            CustomVectorField(ref objectRotation, ref tempObjectRotationX, ref tempObjectRotationY, ref tempObjectRotationZ, new GUIContent("Object Rotation", "This setting controls the rotation of the object. When no parent object is assigned, the rotation values are based on the world axis, ensuring that the object's orientation is aligned with the global coordinate system."), objectRotationVectorFieldRect);
            GUILayout.Space(fieldPadding);

            var objectScalarFloatFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            objectScalarFloatFieldRect.x = 0;
            objectScalarFloatFieldRect.width = settingsArea.width;
            CustomFloatField(ref objectScale, ref tempObjectScalar, new GUIContent("Scale Multiplier"), objectScalarFloatFieldRect);
            GUILayout.Space(fieldPadding);
            #endregion Transform Drawer

            #region Behaviours Drawer
            var behavioursDividerRect = GUILayoutUtility.GetRect(position.width, 0);
            DrawUILine(Color.white, behavioursDividerRect.position, behavioursDividerRect.width);
            GUILayout.Space(fieldPadding);

            var behavioursHelpBoxContent = new GUIContent("Specify the default behaviour trees added to models for each type of model");
            var behavioursHelpBoxStyle = new GUIStyle(BodyLabelStyle) { padding = new RectOffset((int)fieldPadding, (int)fieldPadding, 0, 0) };
            var behavioursHelpBoxRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            behavioursHelpBoxRect.x = 0;
            behavioursHelpBoxRect.width = settingsArea.width;
            GUI.Label(behavioursHelpBoxRect, behavioursHelpBoxContent, behavioursHelpBoxStyle);
            GUILayout.Space(fieldPadding);

            var dropdownFieldHeight = fieldHeight * 1.5f;

            var groundCreatureBehaviourFieldRect = GUILayoutUtility.GetRect(position.width, dropdownFieldHeight);
            groundCreatureBehaviourFieldRect.x = 0;
            groundCreatureBehaviourFieldRect.width = settingsArea.width;
            DrawDropdown(groundCreatureBehaviourFieldRect, GroundCreatureDropdownOptions, groundCreatureBehaviourIndex, "Ground Creature");
            if (customGroundCreatureBehaviour)
            {
                GUILayout.Space(fieldPadding);
                var customBehaviourFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
                customBehaviourFieldRect.x = 0;
                customBehaviourFieldRect.width = settingsArea.width;
                DrawBehaviourField(ref groundCreatureBehaviourTree, new GUIContent("Custom Behaviour", ""), customBehaviourFieldRect);
            }
            GUILayout.Space(fieldPadding);

            var groundVehicleBehaviourFieldRect = GUILayoutUtility.GetRect(position.width, dropdownFieldHeight);
            groundVehicleBehaviourFieldRect.x = 0;
            groundVehicleBehaviourFieldRect.width = settingsArea.width;
            DrawDropdown(groundVehicleBehaviourFieldRect, GroundVehicleDropdownOptions, groundVehicleBehaviourIndex, "Ground Vehicle");
            if(customGroundVehicleBehaviour)
            {
                GUILayout.Space(fieldPadding);
                var customBehaviourFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
                customBehaviourFieldRect.x = 0;
                customBehaviourFieldRect.width = settingsArea.width;
                DrawBehaviourField(ref groundVehicleBehaviourTree, new GUIContent("Custom Behaviour", ""), customBehaviourFieldRect);
            }
            GUILayout.Space(fieldPadding);

            var flyingCreatureBehaviourFieldRect = GUILayoutUtility.GetRect(position.width, dropdownFieldHeight);
            flyingCreatureBehaviourFieldRect.x = 0;
            flyingCreatureBehaviourFieldRect.width = settingsArea.width;
            DrawDropdown(flyingCreatureBehaviourFieldRect, FlyingCreatureDropdownOptions, flyingCreatureBehaviourIndex, "Flying Creature");
            if (customFlyingCreatureBehaviour)
            {
                GUILayout.Space(fieldPadding);
                var customBehaviourFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
                customBehaviourFieldRect.x = 0;
                customBehaviourFieldRect.width = settingsArea.width;
                DrawBehaviourField(ref flyingCreatureBehaviourTree, new GUIContent("Custom Behaviour", ""), customBehaviourFieldRect);
            }
            GUILayout.Space(fieldPadding);

            var flyingVehicleBehaviourFieldRect = GUILayoutUtility.GetRect(position.width, dropdownFieldHeight);
            flyingVehicleBehaviourFieldRect.x = 0;
            flyingVehicleBehaviourFieldRect.width = settingsArea.width;
            DrawDropdown(flyingVehicleBehaviourFieldRect, FlyingVehicleDropdownOptions, flyingVehicleBehaviourIndex, "Flying Vehicle");
            if (customFlyingVehicleBehaviour)
            {
                GUILayout.Space(fieldPadding);
                var customBehaviourFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
                customBehaviourFieldRect.x = 0;
                customBehaviourFieldRect.width = settingsArea.width;
                DrawBehaviourField(ref flyingVehicleBehaviourTree, new GUIContent("Custom Behaviour", ""), customBehaviourFieldRect);
            }
            GUILayout.Space(fieldPadding);

            var swimmingCreatureBehaviourFieldRect = GUILayoutUtility.GetRect(position.width, dropdownFieldHeight);
            swimmingCreatureBehaviourFieldRect.x = 0;
            swimmingCreatureBehaviourFieldRect.width = settingsArea.width;
            DrawDropdown(swimmingCreatureBehaviourFieldRect, SwimmingCreatureDropdownOptions, swimmingCreatureBehaviourIndex, "Swimming Creature");
            if (customSwimmingCreatureBehaviour)
            {
                GUILayout.Space(fieldPadding);
                var customBehaviourFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
                customBehaviourFieldRect.x = 0;
                customBehaviourFieldRect.width = settingsArea.width;
                DrawBehaviourField(ref swimmingCreatureBehaviourTree, new GUIContent("Custom Behaviour", ""), customBehaviourFieldRect);
            }
            GUILayout.Space(fieldPadding);

            var staticBehaviourFieldRect = GUILayoutUtility.GetRect(position.width, dropdownFieldHeight);
            staticBehaviourFieldRect.x = 0;
            staticBehaviourFieldRect.width = settingsArea.width;
            DrawDropdown(staticBehaviourFieldRect, StaticDropdownOptions, staticBehaviourIndex, "Static");
            if (customStaticBehaviour)
            {
                GUILayout.Space(fieldPadding);
                var customBehaviourFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
                customBehaviourFieldRect.x = 0;
                customBehaviourFieldRect.width = settingsArea.width;
                DrawBehaviourField(ref staticBehaviourTree, new GUIContent("Custom Behaviour", ""), customBehaviourFieldRect);
            }
            GUILayout.Space(fieldPadding);
            #endregion Behaviours Drawer

            #region Grid Settings
            var gridDividerRect = GUILayoutUtility.GetRect(position.width, 0);
            DrawUILine(Color.white, gridDividerRect.position, gridDividerRect.width);
            GUILayout.Space(fieldPadding);

            var gridHelpBoxContent = new GUIContent("Edit the way models are placed when generated");
            var gridHelpBoxStyle = new GUIStyle(BodyLabelStyle) { padding = new RectOffset((int)fieldPadding, (int)fieldPadding, 0, 0) };
            var gridHelpBoxRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            gridHelpBoxRect.x = 0;
            gridHelpBoxRect.width = settingsArea.width;
            GUI.Label(gridHelpBoxRect, gridHelpBoxContent, gridHelpBoxStyle);
            GUILayout.Space(fieldPadding);

            var gridGizmosBoolFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            gridGizmosBoolFieldRect.x = 0;
            gridGizmosBoolFieldRect.width = settingsArea.width;
            if (CustomBoolField(ref showGridHandles, new GUIContent("Grid Gizmos", "This tool provides a visual representation of the origin point for the beginning of the grid position in your scene. It's a crucial aid for aligning and positioning objects accurately on the grid."), gridGizmosBoolFieldRect))
            {
                SceneView.RepaintAll();
            }
            GUILayout.Space(fieldPadding);

            var gridOriginVectorFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            gridOriginVectorFieldRect.x = 0;
            gridOriginVectorFieldRect.width = settingsArea.width;
            CustomVectorField(ref gridOrigin, ref tempGridOriginX, ref tempGridOriginY, ref tempGridOriginZ, new GUIContent("Grid Origin", "This setting defines the starting point for the first model placed on the grid in your scene. It essentially marks the initial position from which the grid layout begins. By adjusting the Grid Origin, you can control where your grid-aligned objects start being placed."), gridOriginVectorFieldRect);
            GUILayout.Space(fieldPadding);

            // grid settings
            var cellWidthFloatFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            cellWidthFloatFieldRect.x = 0;
            cellWidthFloatFieldRect.width = settingsArea.width;
            CustomFloatField(ref gridCellWidth, ref tempCellWidth, new GUIContent("Cell Width", "This setting allows you to manually set the distance between each model placed in the grid. By adjusting the cell width, you control the horizontal spacing of objects within the grid layout. This feature is essential for customizing the density and arrangement of models on the grid."), cellWidthFloatFieldRect);
            GUILayout.Space(fieldPadding);

            var gridWidthIntFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            gridWidthIntFieldRect.x = 0;
            gridWidthIntFieldRect.width = settingsArea.width;
            CustomIntField(ref gridCellCount, ref tempCellCount, new GUIContent("Grid Width", "This tool sets the number of models that will be placed in a single row before starting a new row in the grid layout. It essentially determines the horizontal capacity of each row in your grid."), gridWidthIntFieldRect);
            GUILayout.Space(fieldPadding);

            var gridAreaSettingsFieldRect = GUILayoutUtility.GetRect(position.width, fieldHeight);
            gridAreaSettingsFieldRect.x = fieldPadding;
            gridAreaSettingsFieldRect.width = settingsArea.width - (fieldPadding * 2);
            bool gridAreaSettingsWindowOpen = HasOpenInstances<GridAreaSettingsEditor>();
            var settingsOptionsContent = new GUIContent("Use Grid Area", "This option will show the Grid Area Settings windows");

            if (DrawRoundedButton(gridAreaSettingsFieldRect, settingsOptionsContent, 13))
            {
                if (gridAreaSettingsWindowOpen)
                {
                    CloseWindowIfOpen<GridAreaSettingsEditor>();
                }
                else
                {
                    GridAreaSettingsEditor.Initialize();
                }
            }
            GUILayout.Space(fieldPadding);
            var endDividerRect = GUILayoutUtility.GetRect(position.width, 0);
            DrawUILine(Color.white, endDividerRect.position, endDividerRect.width);
            #endregion Grid Settings

            GUILayout.Space(fieldPadding);
            GUI.EndScrollView();

            //reset button
            var resetButtonRect = new Rect(fieldPadding, position.height - fieldHeight - fieldPadding, position.width - (fieldPadding * 2), fieldHeight);
            if (DrawRoundedButton(resetButtonRect, new GUIContent("Reset")))
            {
                if (GridArea.IsReady())
                    GridArea.KeepModelsAndReset();
                AnythingCreatorEditor.objectParent = objectParent = null;
                TransformSettings.ResetSettings();
                SetupVariables();
            }

            //if any interaction was detected, apply settings lightly
            if (GUI.changed || behaviourChanged)
            {
                behaviourChanged = false;
                ApplySettingsLight();
            }
        }

        /// <summary>
        /// Loads all the curated behaviour trees into the respective dropdown menu.
        /// </summary>
        /// <param name="behaviourType">The type of behaviour to pull from the curated list.</param>
        /// <returns>The completed behaviour dropdown options.</returns>
        private DropdownOption[] CreateBehaviourDropdownOptions(DefaultBehaviourType behaviourType)
        {
            var dropdownList = new List<DropdownOption>();
            BehaviourTreeDropdownOption[] behaviours;
            switch(behaviourType)
            {
                default:
                    behaviours = CuratedBehaviourPreset.Instance.staticBehaviours;
                    break;
                case DefaultBehaviourType.GroundCreature:
                    behaviours = CuratedBehaviourPreset.Instance.groundCreatureBehaviours;
                    break;
                case DefaultBehaviourType.GroundVehicle:
                    behaviours = CuratedBehaviourPreset.Instance.groundVehicleBehaviours;
                    break;
                case DefaultBehaviourType.FlyingCreature:
                    behaviours = CuratedBehaviourPreset.Instance.flyingCreatureBehaviours;
                    break;
                case DefaultBehaviourType.FlyingVehicle:
                    behaviours = CuratedBehaviourPreset.Instance.flyingVehicleBehaviours;
                    break;
                case DefaultBehaviourType.SwimmingCreature:
                    behaviours = CuratedBehaviourPreset.Instance.swimmingCreatureBehaviours;
                    break;
            }


            foreach (var (label, index) in behaviours.WithIndex())
            {
                var option = new DropdownOption()
                {
                    dataEndpoint = index,
                    label = behaviours[index].label,
                    function = () => BehaviourDropdownFunction(behaviourType, BehaviourOption.Curated, index, behaviours)
                };

                dropdownList.Add(option);
            }
            dropdownList.Add(new DropdownOption()
            {
                dataEndpoint = behaviours.Length,
                label = "[None]",
                function = () => BehaviourDropdownFunction(behaviourType, BehaviourOption.None, behaviours.Length, behaviours)
            });
            dropdownList.Add(new DropdownOption()
            {
                dataEndpoint = behaviours.Length + 1,
                label = "[Custom Behaviour Tree]",
                function = () => BehaviourDropdownFunction(behaviourType, BehaviourOption.Custom, behaviours.Length + 1, behaviours)
            });
            return dropdownList.ToArray();
        }

        /// <summary>
        /// Creates the function assigned to each dropdown option.
        /// </summary>
        /// <param name="behaviourType">The behaviour type.</param>
        /// <param name="option">The type of option chosen.</param>
        /// <param name="index">The index of the dropdown option.</param>
        /// <param name="behaviours">The list of curated behaviours.</param>
        private void BehaviourDropdownFunction(DefaultBehaviourType behaviourType, BehaviourOption option, int index, BehaviourTreeDropdownOption[] behaviours)
        {
            switch (behaviourType)
            {
                case DefaultBehaviourType.GroundCreature:
                    groundCreatureBehaviourIndex = index;
                    if (option != BehaviourOption.Custom) groundCreatureBehaviourTree = option == BehaviourOption.Curated ? behaviours[index].behaviourTree : null;
                    customGroundCreatureBehaviour = option == BehaviourOption.Custom;
                    break;
                case DefaultBehaviourType.GroundVehicle:
                    groundVehicleBehaviourIndex = index;
                    if (option != BehaviourOption.Custom) groundVehicleBehaviourTree = option == BehaviourOption.Curated ? behaviours[index].behaviourTree : null;
                    customGroundVehicleBehaviour = option == BehaviourOption.Custom;
                    break;
                case DefaultBehaviourType.FlyingCreature:
                    flyingCreatureBehaviourIndex = index;
                    if (option != BehaviourOption.Custom) flyingCreatureBehaviourTree = option == BehaviourOption.Curated ? behaviours[index].behaviourTree : null;
                    customFlyingCreatureBehaviour = option == BehaviourOption.Custom;
                    break;
                case DefaultBehaviourType.FlyingVehicle:
                    flyingVehicleBehaviourIndex = index;
                    if (option != BehaviourOption.Custom) flyingVehicleBehaviourTree = option == BehaviourOption.Curated ? behaviours[index].behaviourTree : null;
                    customFlyingVehicleBehaviour = option == BehaviourOption.Custom;
                    break;
                case DefaultBehaviourType.SwimmingCreature:
                    swimmingCreatureBehaviourIndex = index;
                    if (option != BehaviourOption.Custom) swimmingCreatureBehaviourTree = option == BehaviourOption.Curated ? behaviours[index].behaviourTree : null;
                    customSwimmingCreatureBehaviour = option == BehaviourOption.Custom;
                    break;
                case DefaultBehaviourType.Static:
                    staticBehaviourIndex = index;
                    if(option != BehaviourOption.Custom) staticBehaviourTree = option == BehaviourOption.Curated ? behaviours[index].behaviourTree : null;
                    customStaticBehaviour = option == BehaviourOption.Custom;
                    break;
            }
            behaviourChanged = true;
        }

        private enum BehaviourOption
        {
            Curated,
            Custom,
            None
        }
    }
}
