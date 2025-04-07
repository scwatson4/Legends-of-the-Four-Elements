using AnythingWorld.Networking.Editor;
using AnythingWorld.Utilities;
using AnythingWorld.Utilities.Data;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.PlayerLoop;

namespace AnythingWorld.Editor
{
    /// <summary>
    /// The Editor script for Animate Anything
    /// </summary>
    public class AnimateAnythingEditor : AnythingCreatorEditor
    {
        #region Fields
        private static Bounds relativeHeightBoundsSize;

        private static bool polling;
        private static string pollCode;
        private static int pollTime;
        static string inputFieldValue;
        private static float countUpdate;
        #region Textures
        private static Texture2D baseHelpIcon;
        protected static Texture2D BaseHelpIcon
        {
            get
            {
                if (baseHelpIcon == null)
                {
                    baseHelpIcon = Resources.Load("Editor/Shared/helpIcon") as Texture2D;
                }
                return baseHelpIcon;
            }
        }

        private static Texture2D baseExample;
        protected static Texture2D BaseExample
        {
            get
            {
                if (baseExample == null)
                {
                    baseExample = Resources.Load("Editor/Shared/tposeExample") as Texture2D;
                }
                return baseExample;
            }
        }
        private static Texture2D loadingIcon;
        protected static Texture2D LoadingIcon
        {
            get
            {
                if (loadingIcon == null)
                {
                    loadingIcon = Resources.Load("Editor/Shared/loadingIcon") as Texture2D;
                }
                return loadingIcon;
            }
        }
        private static Texture2D baseGizmo;
        protected static Texture2D BaseGizmo
        {
            get
            {
                if (baseGizmo == null)
                {
                    baseGizmo = Resources.Load("Editor/Shared/gizmo") as Texture2D;
                }
                return baseGizmo;
            }
        }

        float rotationAngle = 1f;
        float iconSize = 100f;
        #endregion Textures
        #endregion Fields

        #region Initialization
        /// <summary>
        /// Initializes and shows window, called from Anything World top bar menu.
        /// </summary>
        [MenuItem("Tools/Anything World/Animate Anything", false, 24)]
        internal static void Initialize()
        {
            AnythingCreatorEditor tabWindow;
            Vector2 windowSize = new Vector2(900, 875);
            if (AnythingSettings.HasAPIKey)
            {
                CloseWindowIfOpen<AnimateAnythingEditor>();
                var browser = GetWindow(typeof(AnimateAnythingEditor), false, "Animate Anything");
                browser.position = new Rect(EditorGUIUtility.GetMainWindowPosition().center - windowSize / 2, windowSize);

                browser.Show();
                browser.Focus();
            }
            else
            {
                CloseWindowIfOpen<ModelBrowserEditor>();
                CloseWindowIfOpen<MyWorldEditor>();
                CloseWindowIfOpen<AICreatorEditor>();
                CloseWindowIfOpen<AnimateAnythingEditor>();
                tabWindow = GetWindow<LogInEditor>("Log In | Sign Up", false);
                tabWindow.position = new Rect(EditorGUIUtility.GetMainWindowPosition().center - windowSize / 2, windowSize);
            }
        }


        protected new void Awake()
        {
            base.Awake();

            windowTitle = "Animate Anything";
            bannerTintA = HexToColour("00FDFF");
            bannerTintB = HexToColour("FF59E9");
        }
        #endregion Initialization

        #region Editor Drawing
        protected new void OnGUI()
        {

            base.OnGUI();
            if (Event.current.type == EventType.Repaint && !AnythingSettings.HasAPIKey) Close();
            #region Overwriting Editor Styles
            var backupLabelStyle = new GUIStyle(EditorStyles.label);
            var backupObjectStyle = new GUIStyle(EditorStyles.objectField);
            var backupNumberStyle = new GUIStyle(EditorStyles.numberField);

            EditorStyles.label.font = GetPoppinsFont(PoppinsStyle.Bold);
            EditorStyles.objectField.font = GetPoppinsFont(PoppinsStyle.Medium);
            EditorStyles.numberField.font = GetPoppinsFont(PoppinsStyle.Medium);
            #endregion Overwriting Editor Styles

            if (!EditorGUIUtility.wideMode)
            {
                EditorGUIUtility.wideMode = true;
                EditorGUIUtility.labelWidth = 170;
            }

            try
            {
                DrawAnimateAnything();

                #region Resetting Editor Styles
                EditorStyles.label.font = backupLabelStyle.font;
                EditorStyles.objectField.font = backupObjectStyle.font;
                EditorStyles.numberField.font = backupNumberStyle.font;
                #endregion Resetting Editor Styles
            }
            catch (Exception e)
            {
                Debug.LogException(e);

                #region Resetting Editor Styles
                EditorStyles.label.font = backupLabelStyle.font;
                EditorStyles.objectField.font = backupObjectStyle.font;
                EditorStyles.numberField.font = backupNumberStyle.font;
                #endregion Resetting Editor Styles
            }
           
        }

        public void Update()
        {
            //Repaint slowly the window if we're polling
            if (polling)
            {
                if (countUpdate > 5f)
                {
                    Repaint();
                    countUpdate = 0;
                }
                countUpdate += Time.deltaTime;
            }
        }

        protected Vector2 newScrollPosition;

        /// <summary>
        /// Draw the user interface for Animate Anything
        /// </summary>
        private void DrawAnimateAnything()
        {
            if (polling)
            {
                //We're polling, draw the polling UI
                GUIStyle MessageStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    font = GetPoppinsFont(PoppinsStyle.Bold),
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 24,
                    margin = UniformRectOffset(10),
                    wordWrap = true
                };
                Rect pos = GUILayoutUtility.GetRect(200, 50, HeaderLabelStyle);
                GUI.Label(pos, new GUIContent("Processing your model, please wait..."), MessageStyle);

                Rect posIco = new Rect(position.width / 2f - (iconSize * 0.5f), 400, iconSize, iconSize);
                Vector2 pivot = new Vector2(posIco.x + (iconSize * 0.5f), posIco.y + (iconSize * 0.5f));
                GUIUtility.RotateAroundPivot(rotationAngle, pivot);
                GUI.DrawTexture(posIco, LoadingIcon, ScaleMode.ScaleToFit);
                GUIUtility.RotateAroundPivot(-rotationAngle, pivot);
                rotationAngle += 200f * Time.deltaTime; 
                if (rotationAngle >= 360f) rotationAngle -= 360f;
                return;
            }


            var headerRect = DrawHeaderLabel();
            var animateAnythingRect = new Rect(0, headerRect.yMax, position.width, position.height - headerRect.yMax);
            var infoRect = new Rect(animateAnythingRect.x, animateAnythingRect.y, animateAnythingRect.width * 0.6f, animateAnythingRect.height);
            var confirmationRect = new Rect(animateAnythingRect.x + infoRect.width, animateAnythingRect.y, animateAnythingRect.width - infoRect.width, animateAnythingRect.height);
            // Decide how tall your download button is and any spacing
            float downloadButtonHeight = 30f;
            float spacingBelowConfirmation = 10f;
            float textFieldHeight = 20f;
            float spacingBetweenInputAndButton = 5f;
            float infoHeight = GetInfoHeight(infoRect), confirmationHeight = GetConfirmationHeight(confirmationRect);

            var contentRect = new Rect(animateAnythingRect.x, animateAnythingRect.y, animateAnythingRect.width, Mathf.Max(infoHeight, confirmationHeight));
            newScrollPosition = GUI.BeginScrollView(animateAnythingRect, newScrollPosition, contentRect, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
            DrawAnimateAnythingInfo(infoRect);
            DrawAnimateAnythingConfirmation(confirmationRect);
            // Position for the text field, BELOW the confirmation
            var textFieldRect = new Rect(
                confirmationRect.x,
                confirmationRect.y + confirmationHeight + spacingBelowConfirmation,
                confirmationRect.width - margin,
                textFieldHeight
            );

            // Draw the text field
            inputFieldValue = GUI.TextField(textFieldRect, inputFieldValue);
            
            // Now position the Download button below the text field
            var downloadRect = new Rect(
                textFieldRect.x,
                textFieldRect.yMax + spacingBetweenInputAndButton,
                textFieldRect.width,
                downloadButtonHeight
            );

            if (inputFieldValue !=null && inputFieldValue.Length > 23)
            {
                if (DrawRoundedButton(downloadRect, new GUIContent("Download")))
                {
                   
                    polling = true;
                }
            }
            GUI.EndScrollView();

            if (polling) 
            {
                Rect pollRect = DrawSquareInSquare(animateAnythingRect.center, Mathf.Min(animateAnythingRect.width / 2f, 250f), 100f, 2f);
                GUI.Label(pollRect, new GUIContent($"{pollCode}{new string('.', pollTime % 4)}"), HeaderLabelStyle);
            }
        }

        /// <summary>
        /// Draw the header label of Animate Anything
        /// </summary>
        /// <returns></returns>
        private Rect DrawHeaderLabel()
        {
            var labelContent = new GUIContent("Upload your own 3D model and let AI rig and animate it for you");
            var labelWidth = position.width * (2f / 3f);
            var labelHeight = HeaderLabelStyle.CalcHeight(labelContent, labelWidth);
            var labelRect = GUILayoutUtility.GetRect(labelWidth, labelHeight, HeaderLabelStyle);
            labelRect.width = labelWidth;
            labelRect.x = (position.width - labelWidth) / 2;
            GUI.Label(labelRect, labelContent, HeaderLabelStyle);

            return labelRect;
        }
       
        string modelName = "";
        string modelClassification = "";
        GameObject model;
        string authorName = "";
        bool cc0License = true;
        bool homemade = false;
        FolderReference assetsFolder = new FolderReference();

        bool understandingConstraints = false;
        bool systemImproveConsent = false;
        static bool addBehaviour = false;
        static bool processedAddBehaviour = false;

        float margin = 50f;
        float padding = 10f;

        Rect confirmationPreviewRect;
        Rect confirmationExampleRect;
        Rect confirmationGizmoRect;
        Rect confirmationInfoRect;
        Rect confirmationToggleRect;
        Rect confirmationToggleUnderstoodRect;
        Rect confirmationToggleImproveRect;
        Rect confirmationToggleBehaviourRect;
        Rect confirmationUploadRect;

        Rect infoLabelRect;
        Rect infoNameRect;
        Rect infoTypeRect;
        Rect infoMeshRect;
        Rect infoAuthorRect;
        Rect infoLicenseRect;
        Rect infoAssetsFolderRect;

        GUIContent understandingConstraintsContent = new GUIContent("I've checked and understood the model processing constraints of the current early access version. My model belongs to a category which is already available.");
        GUIContent systemImprovementContent = new GUIContent("Allow us to use this model to improve our AI system.");
        GUIContent behaviourContent = new GUIContent("Assign a default NPC behaviour to the model.");

        /// <summary>
        /// Draw the leftmost side of Animate Anything for user data entry
        /// </summary>
        /// <param name="infoRect">The rect to draw within</param>
        private void DrawAnimateAnythingInfo(Rect infoRect)
        {
            var labelContent = new GUIContent("Model Info");
            var labelWidth = infoRect.width - (margin * 2f);
            var labelHeight = HeaderLabelStyle.CalcHeight(labelContent, labelWidth);
            infoLabelRect = new Rect(infoRect.x + margin, infoRect.y, labelWidth, labelHeight);
            GUI.Label(infoLabelRect, labelContent, new GUIStyle(HeaderLabelStyle) { alignment = TextAnchor.MiddleLeft });

            modelName = DrawInfoField_String(infoLabelRect, padding, "Model Name", "Choose a name for your model", modelName, out infoNameRect);
            modelClassification = DrawInfoField_String(infoNameRect, padding, "Model Type", "(e.g. woman, man, dog, cat, ant, tree)\nThis will help us to classify your model in the next step", "Model Type", new Vector2(700, 300), ModelTypePopup, modelClassification, out infoTypeRect);
            model = DrawInfoField_Mesh(infoTypeRect, padding, "Mesh", "This will help us to select the mesh asset from your Unity project files. If you don't have the model in Unity, import it there first. Ensure that textures are visible within Unity, if your model has them.", "Model Processing Constraints", new Vector2(700, 1000), ModelProcessingPopup, model, out infoMeshRect);
            authorName = DrawInfoField_StringToggle(infoMeshRect, padding, "Model Author", "Please credit the author below. We cannot process your model without this information.", "Did you create this model?", authorName, ref homemade, out infoAuthorRect);
            assetsFolder = DrawInfoField_Folder(infoAuthorRect, padding, "Textures, Materials, & Additional Files", "If your model has any external assets that should be sent along, you may attach a folder containing these assets here. File extensions accepted are .mtl, .bin, .jpg, .jpeg, .png, .bmp, .gif, .tiff, .tif, .targa, .tga, and .zip.", assetsFolder, out infoAssetsFolderRect);
        }

        /// <summary>
        /// Draw the rightmost side of Animate Anything for displaying sample preview, toggle boxes for the user to opt in to, and the upload button.
        /// </summary>
        /// <param name="confirmationRect"></param>
        private void DrawAnimateAnythingConfirmation(Rect confirmationRect)
        {
            var previewSize = confirmationRect.width - margin;
            var exampleRatio = (float)BaseExample.width / (float)BaseExample.height;
            var exampleWidth = previewSize * exampleRatio;
            var gizmoSize = previewSize / 6f;

            var infoContent = new GUIContent("Important:\nYour model must be correctly rotated to get proper results. In Unity, the model should be facing +Z axis, with the vertical axis being Y and side axis being X.");
            var infoStyle = new GUIStyle(BodyLabelStyle) { fontSize = 9, wordWrap = true, padding = UniformRectOffset(5) };
            var infoHeight = infoStyle.CalcHeight(infoContent, previewSize);

            confirmationPreviewRect = new Rect(confirmationRect.x, confirmationRect.y + padding, previewSize, previewSize);
            GUI.DrawTexture(confirmationPreviewRect, BaseDetailsThumbnailBackdrops[2]);
            confirmationExampleRect = new Rect(confirmationPreviewRect.x + (previewSize - exampleWidth) / 2, confirmationPreviewRect.y, exampleWidth, previewSize);
            GUI.DrawTexture(confirmationExampleRect, BaseExample);
            confirmationGizmoRect = new Rect(confirmationPreviewRect.xMax - gizmoSize - padding, confirmationPreviewRect.yMax - gizmoSize - padding, gizmoSize, gizmoSize);
            GUI.DrawTexture(confirmationGizmoRect, BaseGizmo);
            confirmationInfoRect = new Rect(confirmationPreviewRect.x, confirmationPreviewRect.yMax, previewSize, infoHeight);
            EditorGUI.DrawRect(confirmationInfoRect, HexToColour("575859"));
            GUI.Label(confirmationInfoRect, infoContent, infoStyle);

            DrawInfoField_RadioButtonToggle(confirmationInfoRect, padding, "Original License", "CC0", "CC BY", ref cc0License, out infoLicenseRect);

            var confirmationToggleStyle = new GUIStyle(BodyLabelStyle) { wordWrap = true, fontSize = 10 };

            var toggleRectHeight = (padding * 4) +
                                   confirmationToggleStyle.CalcHeight(understandingConstraintsContent, previewSize - (padding * 2) - GetToggleSize()) +
                                   confirmationToggleStyle.CalcHeight(systemImprovementContent, previewSize - (padding * 2) - GetToggleSize()) +
                                   confirmationToggleStyle.CalcHeight(behaviourContent, previewSize - (padding * 2) - GetToggleSize());

            confirmationToggleRect = new Rect(confirmationRect.x, infoLicenseRect.yMax + padding, previewSize, toggleRectHeight);
            DrawSquareInSquare(confirmationToggleRect.center, confirmationToggleRect.width, confirmationToggleRect.height, 2);
            understandingConstraints = DrawInfoField_Toggle(new Rect(confirmationToggleRect.position, new Vector2(previewSize, 0)), padding, understandingConstraintsContent.text, ref understandingConstraints, out confirmationToggleUnderstoodRect);
            systemImproveConsent = DrawInfoField_Toggle(confirmationToggleUnderstoodRect, padding, systemImprovementContent.text, ref systemImproveConsent, out confirmationToggleImproveRect);
            addBehaviour = DrawInfoField_Toggle(confirmationToggleImproveRect, padding, behaviourContent.text, ref addBehaviour, out confirmationToggleBehaviourRect);
            confirmationUploadRect = new Rect(confirmationRect.x, confirmationToggleRect.yMax + padding, previewSize, 30);
            if (DrawRoundedButton(confirmationUploadRect, new GUIContent("Upload")))
            {
                if (model != null &&
                    !string.IsNullOrEmpty(modelName) &&
                    !string.IsNullOrEmpty(modelClassification) &&
                    (!string.IsNullOrEmpty(authorName) || homemade) &&
                    understandingConstraints)
                {
                    
                    try
                    {
                        if (string.IsNullOrEmpty(assetsFolder.GUID))
                        {
                            DisplayAWDialog("Warning",
                            $"You've not added a folder containing any additional assets. Once processed, your model will not have any textures. Are you sure you want to upload your model without additional assets?",
                            "Yes, upload my model",
                            "No, I want to add a folder",
                            () => Upload());
                        }
                        else
                        {
                            Upload();
                        }

                    }
                    catch (Exception e)
                    {
                        DisplayAWDialog("Error",
                            $"Unfortunately an error occurred when trying to process the model. Please make sure that your model meets the current constraints of the system. \n\n{e.Message}",
                            "Open Constraints Guidelines",
                            "Close",
                            () => ModelProcessingPopup(this));
                        Debug.LogException(e);
                        polling= false;
                    }
                }
                else
                {
                    DisplayAWDialog("Error",
                        $"Your form is incomplete. To upload a model, make sure to supply a model, a name for the model to go by, the model's type (see the constraints guidelines for more details), the author's name, as well as confirming that you understand the processing constraints of Animate Anything.",
                        "Open Constraints Guidelines",
                        "Close",
                        () => ModelProcessingPopup(this));
                    polling = false;
                }
            }
            confirmationUploadRect = new Rect(confirmationRect.x, confirmationToggleRect.yMax + padding, previewSize, 30);
            
        }

        /// <summary>
        /// Calculates the total height of the information half of the Animate Anything window
        /// </summary>
        /// <param name="infoRect">The rect of the information half</param>
        /// <returns>The total height</returns>
        private float GetInfoHeight(Rect infoRect)
        {
            float totalHeight = 0f;

            var labelContent = new GUIContent("Model Info");
            var labelWidth = infoRect.width - (margin * 2f);
            var labelHeight = HeaderLabelStyle.CalcHeight(labelContent, labelWidth);

            var infoRectMargined = new Rect(infoRect.x + margin, infoRect.y, labelWidth, labelHeight);

            totalHeight += HeaderLabelStyle.CalcHeight(labelContent, labelWidth);
            totalHeight += CalcInfoFillerHeight(infoRectMargined, padding, "Model Name", "Choose a name for your model") + 40 + padding;
            totalHeight += CalcInfoFillerHeight(infoRectMargined, padding, "Model Type", "(e.g. woman, man, dog, cat, ant, tree)\nThis will help us to classify your model in the next step") + 40 + padding;
            totalHeight += CalcInfoFillerHeight(infoRectMargined, padding, "Mesh", "This will help us to select the mesh asset from your Unity project files. If you don't have the model in Unity, import it there first. Ensure that textures are visible within Unity, if your model has them.") + 40 + padding;
            totalHeight += CalcInfoFillerHeight(infoRectMargined, padding, "Model Author", "Please credit the author below. We cannot process your model without this information.") + 20 + 40 + padding;
            totalHeight += CalcInfoFillerHeight(infoRectMargined, padding, "Textures, Materials, & Additional Files", "If your model has any external assets that should be sent along, you may attach a folder containing these assets here. File extensions accepted are .mtl, .bin, .jpg, .jpeg, .png, .bmp, .gif, .tiff, .tif, .targa, .tga, and .zip.") + 40 + padding;
            totalHeight += padding;

            return totalHeight;
        }

        /// <summary>
        /// Calculates the total height of the confirmation half of the Animate Anything window
        /// </summary>
        /// <param name="confirmationRect">The rect of the confirmation half</param>
        /// <returns>The total height</returns>
        private float GetConfirmationHeight(Rect confirmationRect)
        {
            float totalHeight = 0f;

            var previewSize = confirmationRect.width - margin;
            
            var infoContent = new GUIContent("Important:\nYour model must be correctly rotated to get proper results. In Unity, the model should be facing +Z axis, with the vertical axis being Y and side axis being X.");
            var infoStyle = new GUIStyle(BodyLabelStyle) { fontSize = 9, wordWrap = true, padding = UniformRectOffset(5) };

            var confirmationToggleStyle = new GUIStyle(BodyLabelStyle) { wordWrap = true, fontSize = 10 };

            var toggleRectHeight = (padding * 4) +
                                   confirmationToggleStyle.CalcHeight(understandingConstraintsContent, previewSize - (padding * 2) - GetToggleSize()) +
                                   confirmationToggleStyle.CalcHeight(systemImprovementContent, previewSize - (padding * 2) - GetToggleSize()) +
                                   confirmationToggleStyle.CalcHeight(behaviourContent, previewSize - (padding * 2) - GetToggleSize());

            totalHeight += previewSize + infoStyle.CalcHeight(infoContent, previewSize) + padding;
            totalHeight += CalcInfoFillerHeight(confirmationRect, padding, "Original License", "") + GetToggleSize();
            totalHeight += toggleRectHeight + padding;
            totalHeight += (padding * 2) + 30;
            return totalHeight;
        }

        /// <summary>
        /// Uploads the model to be animated
        /// </summary>
        private void Upload()
        {
            polling = true;
            relativeHeightBoundsSize = CalculateHeightBounds(model.GetComponentsInChildren<Renderer>());
            AnythingAnimate.AnimateAsync(model, modelName, modelClassification, homemade ? "" : authorName, cc0License ? "cc0" : "ccby", true, systemImproveConsent, OnSuccessfulExport, OnErrorProcessing, string.IsNullOrEmpty(assetsFolder.GUID) ? "" : assetsFolder.Path, OnPollFailure).Forget();

            EditorLoadingIcon.Instance.ShowToastyMessage($"Attempting to upload \"{modelName}\" to Animate Anything...", GetWindow<AnimateAnythingEditor>(), 5f);

            model = null;
            modelName = "";
            modelClassification = "";
            authorName = "";
            cc0License = true;
            homemade = false;
            understandingConstraints = false;
            systemImproveConsent = false;
            processedAddBehaviour = addBehaviour;
            addBehaviour = false;
            assetsFolder.GUID = "";
        }

        /// <summary>
        /// Draw a text box in the correct styling, using a title and descriptor.
        /// </summary>
        private Rect DrawInfoFiller(Rect previousRect, float padding, string title, string descriptor)
        {
            var titleContent = new GUIContent(title);
            var titleStyle = new GUIStyle(HeaderLabelStyle) { fontSize = 18, alignment = TextAnchor.MiddleLeft };

            var titleWidth = previousRect.width;
            var titleHeight = titleStyle.CalcHeight(titleContent, titleWidth);
            var titleRect = new Rect(previousRect.x, previousRect.yMax + padding, titleWidth, titleHeight);
            GUI.Label(titleRect, titleContent, titleStyle);


            var descriptorContent = new GUIContent(descriptor);
            var descriptorStyle = new GUIStyle(BodyLabelStyle);

            var descriptorWidth = titleRect.width;
            var descriptorHeight = descriptorStyle.CalcHeight(descriptorContent, descriptorWidth);
            var descriptorRect = new Rect(titleRect.x, titleRect.yMax, descriptorWidth, descriptorHeight);
            GUI.Label(descriptorRect, descriptorContent, descriptorStyle);

            return new Rect(titleRect.x, titleRect.y, titleRect.width, descriptorRect.yMax - titleRect.y);
        }

        /// <summary>
        /// Draw a text box in the correct styling, using a title, descriptor, and help button that opens a subwindow.
        /// </summary>
        private Rect DrawInfoFiller(Rect previousRect, float padding, string title, string descriptor, string subwindowTitle, Vector2 subwindowSize, Action<AnythingEditor> subwindow)
        {
            var titleContent = new GUIContent(title);
            var titleStyle = new GUIStyle(HeaderLabelStyle) { fontSize = 18, alignment = TextAnchor.MiddleLeft };

            var titleWidth = previousRect.width;
            var titleHeight = titleStyle.CalcHeight(titleContent, titleWidth);
            var titleRect = new Rect(previousRect.x, previousRect.yMax + padding, titleWidth, titleHeight);
            GUI.Label(titleRect, titleContent, titleStyle);

            if (subwindow != null)
            {
                var helpRect = new Rect(titleRect.xMax - titleHeight, titleRect.y, titleHeight, titleHeight);
                if (GUI.Button(helpRect, new GUIContent(BaseHelpIcon), new GUIStyle() { padding = UniformRectOffset(Mathf.RoundToInt(titleHeight * 0.1f)) }))
                {
                    AnythingSubwindow.OpenWindow(subwindowTitle, subwindowSize, subwindow, position);
                }
            }

            var descriptorContent = new GUIContent(descriptor);
            var descriptorStyle = new GUIStyle(BodyLabelStyle);

            var descriptorWidth = titleRect.width;
            var descriptorHeight = descriptorStyle.CalcHeight(descriptorContent, descriptorWidth);
            var descriptorRect = new Rect(titleRect.x, titleRect.yMax, descriptorWidth, descriptorHeight);
            GUI.Label(descriptorRect, descriptorContent, descriptorStyle);

            return new Rect(titleRect.x, titleRect.y, titleRect.width, descriptorRect.yMax - titleRect.y);
        }

        /// <summary>
        /// Calculates the total height the info UI element should take.
        /// </summary>
        /// <param name="coreRect">The rect the info UI element is a part of</param>
        /// <param name="padding">The padding of the UI element</param>
        /// <param name="title">The title</param>
        /// <param name="descriptor">The description</param>
        /// <returns></returns>
        private float CalcInfoFillerHeight(Rect coreRect, float padding, string title, string descriptor)
        {
            var titleContent = new GUIContent(title);
            var titleStyle = new GUIStyle(HeaderLabelStyle) { fontSize = 18, alignment = TextAnchor.MiddleLeft };

            var titleWidth = coreRect.width;
            var titleHeight = titleStyle.CalcHeight(titleContent, titleWidth);

            var descriptorContent = new GUIContent(descriptor);
            var descriptorStyle = new GUIStyle(BodyLabelStyle);

            var descriptorWidth = coreRect.width;
            var descriptorHeight = descriptorStyle.CalcHeight(descriptorContent, descriptorWidth);

            return titleHeight + descriptorHeight + padding;
        }

        /// <summary>
        /// Draw a text box in the correct styling, using a title and descriptor, with a fillable string field.
        /// </summary>
        private string DrawInfoField_String(Rect previousRect, float padding, string title, string descriptor, string data, out Rect rect)
        {
            var fillerRect = DrawInfoFiller(previousRect, padding, title, descriptor);

            bool changed = false;
            var inputRect = new Rect(fillerRect.x, fillerRect.yMax + padding, fillerRect.width, 40);
            data = DrawSquaredInputField(inputRect, data, ref changed);

            rect = new Rect(previousRect.x, previousRect.yMax, previousRect.width, inputRect.yMax - previousRect.yMax);
            return data;
        }
        /// <summary>
        /// Draw a text box in the correct styling, using a title and descriptor, and help button that opens a subwindow, with a fillable string field.
        /// </summary>
        private string DrawInfoField_String(Rect previousRect, float padding, string title, string descriptor, string subwindowTitle, Vector2 subwindowSize, Action<AnythingEditor> subwindow, string data, out Rect rect)
        {
            var fillerRect = DrawInfoFiller(previousRect, padding, title, descriptor, subwindowTitle, subwindowSize, subwindow);

            bool changed = false;
            var inputRect = new Rect(fillerRect.x, fillerRect.yMax + padding, fillerRect.width, 40);
            data = DrawSquaredInputField(inputRect, data, ref changed);

            rect = new Rect(previousRect.x, previousRect.yMax, previousRect.width, inputRect.yMax - previousRect.yMax);
            return data;
        }
        /// <summary>
        /// Draw a text box in the correct styling, using a title and descriptor, with a fillable string field and toggle.
        /// </summary>
        private string DrawInfoField_StringToggle(Rect previousRect, float padding, string title, string descriptor, string boolLabel, string stringData, ref bool boolData, out Rect rect)
        {
            var fillerRect = DrawInfoFiller(previousRect, padding, title, descriptor);

            var toggleRect = new Rect(fillerRect.x, fillerRect.yMax, fillerRect.width, 20);
            boolData = CustomSimpleBoolField(boolData, boolLabel, toggleRect);

            GUI.enabled = !boolData;
            bool changed = false;
            var inputRect = new Rect(toggleRect.x, toggleRect.yMax + padding, toggleRect.width, 40);
            stringData = DrawSquaredInputField(inputRect, stringData, ref changed);
            GUI.enabled = true;

            rect = new Rect(previousRect.x, previousRect.yMax, previousRect.width, inputRect.yMax - previousRect.yMax);
            return stringData;
        }
        /// <summary>
        /// Draw a text box in the correct styling, using a title and descriptor, with a toggle.
        /// </summary>
        private bool DrawInfoField_Toggle(Rect previousRect, float padding, string boolLabel, ref bool boolData, out Rect rect)
        {
            var labelContent = new GUIContent(boolLabel);
            var labelStyle = new GUIStyle(BodyLabelStyle) { wordWrap = true, fontSize = 10 };
            var labelHeight = labelStyle.CalcHeight(labelContent, previousRect.width - (padding * 2) - GetToggleSize());

            var toggleRect = new Rect(previousRect.x + padding, previousRect.yMax + padding, previousRect.width - (padding * 2), labelHeight);
            boolData = CustomSimpleBoolField(boolData, boolLabel, toggleRect, 10);

            rect = new Rect(previousRect.x, previousRect.yMax, previousRect.width, toggleRect.yMax - previousRect.yMax);
            return boolData;
        }
        /// <summary>
        /// Draw a text box in the correct styling, using a title and descriptor, with a radio button toggle.
        /// </summary>
        private void DrawInfoField_RadioButtonToggle(Rect previousRect, float padding, string title, string bool1Label, string bool2Label, ref bool boolData, out Rect rect)
        {
            var fillerRect = DrawInfoFiller(previousRect, padding, title, "");

            var label1Content = new GUIContent(bool1Label);
            var label2Content = new GUIContent(bool2Label);
            var labelStyle = new GUIStyle(BodyLabelStyle) { wordWrap = false };
            labelStyle.CalcMinMaxWidth(label1Content, out _, out var label1Width);
            labelStyle.CalcMinMaxWidth(label2Content, out _, out var label2Width);
            
            var bool1Rect = new Rect(fillerRect.x, fillerRect.yMax, label1Width + fieldPadding + GetToggleSize(), GetToggleSize());
            var bool2Rect = new Rect(bool1Rect.xMax + padding, fillerRect.yMax, label2Width + fieldPadding + GetToggleSize(), GetToggleSize());

            boolData = CustomSimpleBoolField(boolData, bool1Label, bool1Rect);
            boolData = !CustomSimpleBoolField(!boolData, bool2Label, bool2Rect);

            rect = new Rect(previousRect.x, previousRect.yMax, previousRect.width, bool1Rect.yMax - previousRect.yMax);
        }
        /// <summary>
        /// Draw a text box in the correct styling, using a title and descriptor, with a field for GameObjects.
        /// </summary>
        private GameObject DrawInfoField_Mesh(Rect previousRect, float padding, string title, string descriptor, string subwindowTitle, Vector2 subwindowSize, Action<AnythingEditor> subwindow, GameObject data, out Rect rect)
        {
            var fillerRect = DrawInfoFiller(previousRect, padding, title, descriptor, subwindowTitle, subwindowSize, subwindow);

            var inputRect = new Rect(fillerRect.x, fillerRect.yMax + padding, fillerRect.width, 40);
            try
            {
                data = EditorGUI.ObjectField(inputRect, data, typeof(GameObject), false) as GameObject;
            }
            catch (UnityEngine.ExitGUIException) { }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }

            rect = new Rect(previousRect.x, previousRect.yMax, previousRect.width, inputRect.yMax - previousRect.yMax);
            return data;
        }

        /// <summary>
        /// Draw a text box in the correct styling, using a title and descriptor, with a field for GameObjects.
        /// </summary>
        private FolderReference DrawInfoField_Folder(Rect previousRect, float padding, string title, string descriptor, FolderReference data, out Rect rect)
        {
            Event e = Event.current;
            var fillerRect = DrawInfoFiller(previousRect, padding, title, descriptor);

            var inputRect = new Rect(fillerRect.x, fillerRect.yMax + padding, fillerRect.width, 40);
            try
            {
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(data.GUID));
                GUIContent guiContent = EditorGUIUtility.ObjectContent(obj, typeof(DefaultAsset));

                Rect textFieldRect = inputRect;
                Rect objectFieldRect = inputRect;
                objectFieldRect.width = 18f;
                objectFieldRect.x = textFieldRect.xMax - objectFieldRect.width - 1f;
                objectFieldRect.height -= 2f;
                objectFieldRect.y += 1f;

                GUIStyle textFieldStyle = new GUIStyle("ObjectField")
                {
                    imagePosition = obj ? ImagePosition.ImageLeft : ImagePosition.TextOnly
                };


                GUI.enabled = !(e.isMouse && objectFieldRect.Contains(e.mousePosition));
                if (GUI.Button(textFieldRect, guiContent, textFieldStyle) && obj)
                    EditorGUIUtility.PingObject(obj);

                if (textFieldRect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.type == EventType.DragUpdated)
                    {
                        UnityEngine.Object reference = DragAndDrop.objectReferences[0];
                        string path = AssetDatabase.GetAssetPath(reference);
                        DragAndDrop.visualMode = Directory.Exists(path) ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                        Event.current.Use();
                    }
                    else if (Event.current.type == EventType.DragPerform)
                    {
                        UnityEngine.Object reference = DragAndDrop.objectReferences[0];
                        string path = AssetDatabase.GetAssetPath(reference);
                        if (Directory.Exists(path))
                        {
                            obj = reference;
                            data.GUID = AssetDatabase.AssetPathToGUID(path);
                        }
                        Event.current.Use();
                    }
                }
                GUI.enabled = true;

                if (GUI.Button(objectFieldRect, "", GUI.skin.GetStyle("ObjectFieldButton")))
                {
                    string path = EditorUtility.OpenFolderPanel("Select a folder", "Assets", "");
                    if (path.Contains(Application.dataPath))
                    {
                        path = "Assets" + path.Substring(Application.dataPath.Length);
                        obj = AssetDatabase.LoadAssetAtPath(path, typeof(DefaultAsset));
                        data.GUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
                    }
                    else
                    {
                        if(!string.IsNullOrEmpty(path))
                        {
                            Debug.LogWarning("Only folders available within the project can be loaded!");
                        }
                        path = "";
                        obj = null;
                        data.GUID = "";
                    }
                    GUIUtility.ExitGUI();
                }
            }
            catch (UnityEngine.ExitGUIException) { }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }

            rect = new Rect(previousRect.x, previousRect.yMax, previousRect.width, inputRect.yMax - previousRect.yMax);
            return data;
        }

        /// <summary>
        /// Subwindow - Describes how to define a model type
        /// </summary>
        private void ModelTypePopup(AnythingEditor window)
        {
            Rect windowBanner = MaskWindowBanner();
            DrawWindowBanner(windowBanner);
            var bannerRectWithMargin = new Rect(windowBanner.x + padding, windowBanner.y, windowBanner.width - (padding * 2), windowBanner.height);
            var detailsRect = DrawInfoFiller(bannerRectWithMargin, padding, "Model Processing Constraints",
                "Please help us to help you by being clear and tidy with your model types. We\'re looking for really common names for your models, for example:\r\n\r\n" +
                "- Person: \"man\", \"woman\", \"child\" or \"human\"\r\n" +
                "- Animal: \"cat\" instead of \"bobcat\", or \"ant\" instead of \"fire ant\", or \"dog\" instead of \"golden retriever\"\r\n" +
                "- Fictional two-legged character: \"character\" if it has humanoid looks, \"cartoon character\" if cartoony\r\n" +
                "- Fictional four-legged character: the species of a similar animal, such as \"horse\" instead of \"unicorn\". If you can't find a similar animal, use \"quadruped\"\r\n" + 
                "- Object or plant: \"box\" instead of \"storage box\", or simply \"tree\" instead of \"palm tree\"");
        }

        private Dictionary<string, RiggingMainCategoryDetails> availableCategoryDetails;
        private string selectedKey = "Biped";
        protected Vector2 categoriesScrollPosition;
        protected Vector2 subcategoriesScrollPosition;

        /// <summary>
        /// Subwindow - Describes model processing constraints
        /// </summary>
        private void ModelProcessingPopup(AnythingEditor window)
        {
            if (availableCategoryDetails == null)
            {
                availableCategoryDetails = new Dictionary<string, RiggingMainCategoryDetails>() {
                    {
                        "Biped",
                        new RiggingMainCategoryDetails() { available = true, thumbnailURL = "https://app.anything.world/static/media/biped.8ba212fa.png", subcategoryDetails = new Dictionary<string, RiggingCategoryDetails>()
                        {
                            { "Human & Humanoid",               new RiggingCategoryDetails() { available = true,    thumbnailURL = "https://app.anything.world/static/media/biped_human__walk.c40c0b7c.png" } },
                            { "Mini Humanoid",                  new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/biped_small__walk.584ae1eb.png" } },
                            { "Blob Humanoid",                  new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/biped_headbody__walk.6f25b803.png" } },
                            { "Biped Dinosaur",                 new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/biped_t-rex__walk.311f89ea.png" } },
                            { "Large Hopper",                   new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/biped_macropus__hop.bc098b5f.png" } }
                        } }
                    },
                    {
                        "Quadruped",
                        new RiggingMainCategoryDetails() { available = true, thumbnailURL = "https://app.anything.world/static/media/quadruped.15fa83ce.png", subcategoryDetails = new Dictionary<string, RiggingCategoryDetails>()
                        {
                            { "Canine & Feline",                new RiggingCategoryDetails() { available = true,    thumbnailURL = "https://app.anything.world/static/media/quadruped__walk.35b39ef9.png" } },
                            { "Lean Hooved Mammal",             new RiggingCategoryDetails() { available = true,    thumbnailURL = "https://app.anything.world/static/media/quadruped_ungulate__walk.ed3ebf52.png" } },
                            { "Long-Necked, Hooved Mammal",     new RiggingCategoryDetails() { available = true,    thumbnailURL = "https://app.anything.world/static/media/quadruped_fat_longneck__walk.d99f2ac3.png" } },
                            { "Bear & Co.",                     new RiggingCategoryDetails() { available = true,    thumbnailURL = "https://app.anything.world/static/media/quadruped_fat__walk.48d5d094.png" } },
                            { "Tortoise",                       new RiggingCategoryDetails() { available = true,    thumbnailURL = "https://app.anything.world/static/media/quadruped_fat_shortleg_generic__walk.1534612b.png" } },
                            { "Turtle",                         new RiggingCategoryDetails() { available = true,    thumbnailURL = "https://app.anything.world/static/media/quadruped_fat_shortleg_generic__swim.b1d6e1c1.png" } },
                            { "Ferret & Co.",                   new RiggingCategoryDetails() { available = true,    thumbnailURL = "https://app.anything.world/static/media/quadruped_mustelidae__walk.51a2c7da.png" } },
                            { "Hefty Hooved Mammal",            new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/quadruped_fat_ungulate__walk.87bd1e50.png" } },
                            { "Elephant",                       new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/quadruped_elephantidae__walk.b572b8d6.png" } },
                            { "Lizard, Salamander & Co.",       new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/quadruped_small__crawl.9120401b.png" } },
                            { "Tailed Rodent & Co.",            new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/quadruped_rodent__walk.3337ab6a.png" } },
                            { "Skunk & Armadillo",              new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/quadruped_small__walk.53a78ad4.png" } },
                            { "Crocodile",                      new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/quadruped_fat__crawl.3800d546.png" } },
                            { "Primate",                        new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/quadruped_primate__walk.5b73dbd5.png" } },
                            { "Tailless Rodent & Co.",          new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/quadruped_rodent_no_tail__walk.2efc19fb.png" } },
                            { "Small Hopper",                   new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/quadruped_squat__hop.197f2669.png" } },
                            { "Long-Necked Dinosaur",           new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/quadruped_sauropod__walk.3d8fe8bf.png" } }
                        } }
                    },
                    {
                        "Insect & Arachnid",
                        new RiggingMainCategoryDetails() { available = false, thumbnailURL = "https://app.anything.world/static/media/arthropod.24965e7e.png", subcategoryDetails = new Dictionary<string, RiggingCategoryDetails>()
                        {
                            { "Ant & Co.",                      new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/multileg_crawler__walk.ccb4e9b8.png" } },
                            { "Hopper Insect",                  new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/multileg_hop__walk.ff181b6b.png" } },
                            { "Beetle & Co.",                   new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/multileg_ovalshape__walk.4f987a6a.png" } },
                            { "Ladybug & Co.",                  new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/multileg_tinyleg__walk.3c4f1265.png" } },
                            { "Small-Winged Insect",            new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/multileg_flyer__fly.7b6e0034.png" } },
                            { "Big-Winged Insect",              new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/multileg_big_wings__fly.96f7fc86.png" } },
                            { "Spider & Co.",                   new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/multileg_crawler_eight__walk.dff74c0c.png" } },
                            { "Scorpion & Co.",                 new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/multileg_scorpion__walk.d2827969.png" } },
                            { "Crustacean",                     new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/multileg_crab__walk.ff712fba.png" } },
                        } }
                    },
                    {
                        "Bird & Flyer",
                        new RiggingMainCategoryDetails() { available = true, thumbnailURL = "https://app.anything.world/static/media/winged.097d482f.png", subcategoryDetails = new Dictionary<string, RiggingCategoryDetails>()
                        {
                            { "Flying Bird",                    new RiggingCategoryDetails() { available = true,    thumbnailURL = "https://app.anything.world/static/media/winged_flyer__fly.19029d8c.png" } },
                            { "Walking Bird",                   new RiggingCategoryDetails() { available = true,    thumbnailURL = "https://app.anything.world/static/media/winged_standing__walk.011325a5.png" } },
                            { "Hopping Bird",                   new RiggingCategoryDetails() { available = true,    thumbnailURL = "https://app.anything.world/static/media/winged_standing__hop.3ec9982a.png" } },
                            { "Winged Dragon",                  new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/winged_dragon__fly.ce5f6b57.png" } },
                            { "Waddling Bird",                  new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/winged_flyer__waddle.f812c557.png" } }
                        } }
                    },
                    {
                        "Vehicle",
                        new RiggingMainCategoryDetails() { available = true, thumbnailURL = "https://app.anything.world/static/media/vehicle.e8cce173.png", subcategoryDetails = new Dictionary<string, RiggingCategoryDetails>()
                        {
                            { "4-Wheel Vehicle",                new RiggingCategoryDetails() { available = true,    thumbnailURL = "https://app.anything.world/static/media/vehicle_four_wheel__drive.e8cce173.png" } },
                            { "3-Wheel Vehicle",                new RiggingCategoryDetails() { available = true,    thumbnailURL = "https://app.anything.world/static/media/vehicle_three_wheel__drive.0359c4b3.png" } },
                            { "2-Wheel Vehicle",                new RiggingCategoryDetails() { available = true,    thumbnailURL = "https://app.anything.world/static/media/vehicle_two_wheel__drive.fb12b280.png" } },
                            { "1-Wheel Vehicle",                new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/vehicle_one_wheel__drive.74331430.png" } },
                            { "Helicopter",                     new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/vehicle_propeller__fly.1c111bbc.png" } },
                            { "Airplane",                       new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/vehicle_flyer__fly.8d87a3c8.png" } },
                            { "Biplane",                        new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/vehicle_biplaneflyer__fly.f9a59002.png" } },
                            { "Spaceship",                      new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/vehicle_rocket__fly.6315cff2.png" } },
                            { "Ship",                           new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/vehicle_uniform__bob.6a2b5310.png" } },
                            { "Other Vehicle",                  new RiggingCategoryDetails() { available = false,   thumbnailURL = "https://app.anything.world/static/media/vehicle_uniform__drive.eacaf928.png" } },
                        } }
                    },
                    {
                        "Water Creature",
                        new RiggingMainCategoryDetails() { available = false, thumbnailURL = "https://app.anything.world/static/media/water_animal.4901bb37.png", subcategoryDetails = new Dictionary<string, RiggingCategoryDetails>()
                        {
                            { "Fish & Cetacean",                new RiggingCategoryDetails() { available = false,    thumbnailURL = "https://app.anything.world/static/media/uniform__swim.4901bb37.png" } }
                        } }
                    },
                    {
                        "Snake & Worm",
                        new RiggingMainCategoryDetails() { available = false, thumbnailURL = "https://app.anything.world/static/media/snake_worm.7cbfc5ac.png", subcategoryDetails = new Dictionary<string, RiggingCategoryDetails>()
                        {

                            { "Worm & Co.",                     new RiggingCategoryDetails() { available = false,    thumbnailURL = "https://app.anything.world/static/media/uniform__wriggle.7cbfc5ac.png" } },
                            { "Snake & Co.",                    new RiggingCategoryDetails() { available = false,    thumbnailURL = "https://app.anything.world/static/media/uniform__slither.e2a81200.png" } },
                            { "Cobra & Co.",                    new RiggingCategoryDetails() { available = false,    thumbnailURL = "https://app.anything.world/static/media/uniform__slithervertical.d72cf24a.png" } }
                        } }
                    },
                    {
                        "Static",
                        new RiggingMainCategoryDetails() { available = false, thumbnailURL = "https://app.anything.world/static/media/static.3122f9cb.png", subcategoryDetails = new Dictionary<string, RiggingCategoryDetails>()
                        {
                            { "Object, Flora & Place",          new RiggingCategoryDetails() { available = true,     thumbnailURL = "https://app.anything.world/static/media/scenery__static.3122f9cb.png" } }
                        } }
                    }
                };

                foreach(var mainCategory in availableCategoryDetails)
                {
                    AnimateAnythingEditorProcessor.GetThumbnailFromWeb(mainCategory.Value, Repaint);
                    foreach(var subCategory in mainCategory.Value.subcategoryDetails)
                    {
                        AnimateAnythingEditorProcessor.GetThumbnailFromWeb(subCategory.Value, Repaint);
                    }
                }
                selectedKey = availableCategoryDetails.First().Key;
            }
            Rect windowBanner = MaskWindowBanner();
            DrawWindowBanner(windowBanner);
            var bannerRectWithMargin = new Rect(windowBanner.x + padding, windowBanner.y, windowBanner.width - (padding * 2), windowBanner.height);
            var detailsRect = DrawInfoFiller(bannerRectWithMargin, padding, "Model Processing Constraints",
                "The current system is at an “early access“ stage. As with all innovations we've honed the spec for our MVP, choosing to offer a wide variety of categories for launch with a basic support function, so whilst Animate Anything is in its “experimental phase“ bear with us. Please make sure your model works within the following constraints to get the best results:\r\n\r\n" +
                "- Know the type of model that you submit, i.e. its type or creature species e.g. is it a dog, a sheep, a humanoid. You'll be asked to assign a category so that it can be animated accordingly. Our system will give you a suggestion, but if you think there's a better category fit please correct it.\r\n" +
                "- The model should conform with standard rotation - facing -Y axis in Blender; facing +Z axis in Maya. There's a rotation adjusting step with reference images to guide you.\r\n" +
                "- The model should assume a neutral pose e.g. biped standing in T-pose or A-pose, quadruped with four legs pointing straight to the floor. Your model should not be running, jumping or engaging in any other kind of energetic activity. For models with feet, no other body part should be below feet (such as a tail).\r\n" +
                "- The model should be mostly symmetrical - small asymmetries are ok in some cases.\r\n" +
                "- The model should have distinguishable body parts e.g. head, torso, arms, legs or wings, beaks, tails, etc. Please add in the end: No limb should overlap with another limb, e.g. a leg should not be touching another leg.\r\n" +
                "- The model should not have complex accessories such as swords, shields, big hats or long dresses. Its not a fashion show. Nor can we support historical fetes just yet.\r\n" +
                "- The model should not have multiple mesh layers such as different clothing submeshes. It is recommended to weld these parts together at a geometry level for better results.\r\n" +
                "- The model should not have holes, inner faces, faces that are outside in both sides (such as flat wings), or degenerate geometry. Nothing too weird please.\r\n" +
                "- Fine-grained details such as separate bones for fingers, ears, clothes and face morphs are not supported yet. We'll come to those.\r\n" +
                "- Currently generated rigs are made up of a skeleton only, without rig curves or floor-placed root. We'll get to those too.\r\n" +
                "- Please note that your model's geometry may be modified to make it more suitable for processing and animation. This can affect the polygon and vertex count as well as the topology of the model. We are working on ways to make this impact minimal the future.\r\n");

            var labelContent = new GUIContent("Supported Model Categories");
            var labelWidth = detailsRect.width;
            var labelStyle = new GUIStyle(HeaderLabelStyle) { fontSize = 18, alignment = TextAnchor.MiddleLeft };
            var labelHeight = labelStyle.CalcHeight(labelContent, labelWidth);
            infoLabelRect = new Rect(detailsRect.x, detailsRect.yMax, labelWidth, labelHeight);
            GUI.Label(infoLabelRect, labelContent, labelStyle);

            var categorylabelWidth = detailsRect.width / 2;
            var categorylabelContent = new GUIContent("Main Categories");
            var categorylabelHeight = labelStyle.CalcHeight(categorylabelContent, categorylabelWidth);
            var categoryLabelRect = new Rect(infoLabelRect.x, infoLabelRect.yMax, categorylabelWidth, categorylabelHeight);
            GUI.Label(categoryLabelRect, categorylabelContent, labelStyle);

            var categoriesHeight = window.position.height - categoryLabelRect.yMax;
            var categoriesRect = new Rect(0, categoryLabelRect.yMax, window.position.width / 2, categoriesHeight);
            DrawGrid(availableCategoryDetails, categoriesRect, availableCategoryDetails.Count, 100f, 110f, DrawAvailableCategoryCard, ref categoriesScrollPosition);

            var subcategorylabelContent = new GUIContent($"Sub Categories - {selectedKey}");
            var subcategoryLabelRect = new Rect(categoryLabelRect.xMax, categoryLabelRect.y, categorylabelWidth, categorylabelHeight);
            GUI.Label(subcategoryLabelRect, subcategorylabelContent, labelStyle);

            var subcategoriesRect = new Rect(categoriesRect.xMax, subcategoryLabelRect.yMax, window.position.width / 2, categoriesHeight);
            DrawGrid(availableCategoryDetails[selectedKey].subcategoryDetails, subcategoriesRect, availableCategoryDetails[selectedKey].subcategoryDetails.Count, 100, 110, DrawAvailableSubcategoryCard, ref subcategoriesScrollPosition);
        }

        /// <summary>
        /// Draws the card of a category constraint.
        /// </summary>
        /// <param name="resultDictionary">Dictionary of all category constraints</param>
        /// <param name="columnCoord">Coordinate of column in grid</param>
        /// <param name="rowCoord">Coordinate of row in grid</param>
        /// <param name="buttonWidth">Width of card</param>
        /// <param name="buttonHeight">Height of card</param>
        /// <param name="searchIndex">Index of the card being drawn</param>
        /// <param name="resultScaleMultiplier">Scaling multiplier for variance in card size</param>
        private void DrawAvailableCategoryCard(Dictionary<string, RiggingMainCategoryDetails> resultDictionary, float columnCoord, float rowCoord, float buttonWidth, float buttonHeight, int searchIndex, float resultScaleMultiplier)
        {
            try
            {
                Event e = Event.current;
                // Set result data
                var result = resultDictionary.ToList()[searchIndex];
                var displayThumbnail = result.Value.thumbnail;

                var categoryName = new GUIContent(result.Key);
                var cardRect = new Rect(columnCoord, rowCoord, buttonWidth, buttonHeight);

                //Draw elements
                GUI.DrawTexture(cardRect, TintedCardFrame, ScaleMode.StretchToFill);

                var thumbnailRatio = (float)BaseCardThumbnailBackdrops[0].height / (float)BaseCardThumbnailBackdrops[0].width;
                var thumbnailBackdropRect = new Rect(cardRect.x, cardRect.y, buttonWidth, buttonWidth * thumbnailRatio);

                GUI.DrawTexture(thumbnailBackdropRect, BaseCardThumbnailBackdrops[searchIndex % BaseCardThumbnailBackdrops.Length], ScaleMode.ScaleToFit);

                if (displayThumbnail != null)
                {
                    GUI.DrawTexture(thumbnailBackdropRect, displayThumbnail, ScaleMode.ScaleToFit);
                }

                if (!result.Value.available)
                {
                    DrawAutoSizeRoundedLabel(thumbnailBackdropRect.center, new GUIContent("Coming Soon"), 20, 12, PoppinsStyle.SemiBold, TextAnchor.MiddleCenter);
                }

                var infoRect = new Rect(thumbnailBackdropRect.x, thumbnailBackdropRect.yMax, buttonWidth, cardRect.height - thumbnailBackdropRect.height);

                GUI.Label(infoRect, categoryName, new GUIStyle(ModelNameStyle) { alignment = TextAnchor.MiddleCenter, fontSize = (int)(12 * resultThumbnailMultiplier), font = GetPoppinsFont(PoppinsStyle.SemiBold), wordWrap = true });

                if (cardRect.Contains(e.mousePosition))
                {
                    GUI.DrawTexture(cardRect, BaseObjectSelectionTint, ScaleMode.StretchToFill);
                    if (e.button == 0 && e.isMouse)
                    {
                        GUI.DrawTexture(cardRect, BaseObjectSelectionTint, ScaleMode.StretchToFill);
                        selectedKey = result.Key;
                        Repaint();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// Draws the card of a subcategory constraint.
        /// </summary>
        /// <param name="resultDictionary">Dictionary of all subcategory constraints</param>
        /// <param name="columnCoord">Coordinate of column in grid</param>
        /// <param name="rowCoord">Coordinate of row in grid</param>
        /// <param name="buttonWidth">Width of card</param>
        /// <param name="buttonHeight">Height of card</param>
        /// <param name="searchIndex">Index of the card being drawn</param>
        /// <param name="resultScaleMultiplier">Scaling multiplier for variance in card size</param>
        private void DrawAvailableSubcategoryCard(Dictionary<string, RiggingCategoryDetails> resultDictionary, float columnCoord, float rowCoord, float buttonWidth, float buttonHeight, int searchIndex, float resultScaleMultiplier)
        {
            try
            {
                Event e = Event.current;
                // Set result data
                var result = resultDictionary.ToList()[searchIndex];
                var displayThumbnail = result.Value.thumbnail;

                var categoryName = new GUIContent(result.Key);
                var cardRect = new Rect(columnCoord, rowCoord, buttonWidth, buttonHeight);

                //Draw elements
                GUI.DrawTexture(cardRect, TintedCardFrame, ScaleMode.StretchToFill);

                var thumbnailRatio = (float)BaseCardThumbnailBackdrops[0].height / (float)BaseCardThumbnailBackdrops[0].width;
                var thumbnailBackdropRect = new Rect(cardRect.x, cardRect.y, buttonWidth, buttonWidth * thumbnailRatio);

                GUI.DrawTexture(thumbnailBackdropRect, BaseCardThumbnailBackdrops[searchIndex % BaseCardThumbnailBackdrops.Length], ScaleMode.ScaleToFit);

                if (displayThumbnail != null)
                {
                    GUI.DrawTexture(thumbnailBackdropRect, displayThumbnail, ScaleMode.ScaleToFit);
                }

                if (!result.Value.available)
                {
                    DrawAutoSizeRoundedLabel(thumbnailBackdropRect.center, new GUIContent("Coming Soon"), 20, 12, PoppinsStyle.SemiBold, TextAnchor.MiddleCenter);
                }

                var infoRect = new Rect(thumbnailBackdropRect.x, thumbnailBackdropRect.yMax, buttonWidth, cardRect.height - thumbnailBackdropRect.height);

                GUI.Label(infoRect, categoryName, new GUIStyle(ModelNameStyle) { alignment = TextAnchor.MiddleCenter, fontSize = (int)(12 * resultThumbnailMultiplier), font = GetPoppinsFont(PoppinsStyle.SemiBold), wordWrap = true });

                if (cardRect.Contains(e.mousePosition))
                {
                    GUI.DrawTexture(cardRect, BaseObjectSelectionTint, ScaleMode.StretchToFill);
                    if (e.button == 0 && e.isMouse)
                    {
                        GUI.DrawTexture(cardRect, BaseObjectSelectionTint, ScaleMode.StretchToFill);
                        Repaint();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        #endregion Editor Drawing

        #region Helper Functions
        /// <summary>
        /// Produces a pop-up error for any issues occuring during the upload process.
        /// </summary>
        /// <param name="id">The ID generated for the model</param>
        /// <param name="code">The error code</param>
        /// <param name="message">The error message</param>
        public void OnErrorProcessing(string id, string code, string message)
        {
            DisplayAWDialog("Error",
                    $"Unfortunately an error occurred when trying to process the model. Please make sure that your model meets the current constraints of the system. {(string.IsNullOrEmpty(id) ? "If this error persists, please contact support at support@anything.world." : $"If you have doubts, please contact support at support@anything.world and be sure to mention the following Model ID: {id}.")}\n\n{code}: {message}",
                    "Open Constraints Guidelines",
                    "Close",
                    () => ModelProcessingPopup(this));
            Debug.LogError($"{code}: {message} {(string.IsNullOrEmpty(id) ? "" : $"[{id}]")}");
        }

        /// <summary>
        /// Sets the relative size of a model to ensure the sizing is similar once Animate Anything has finished processing.
        /// </summary>
        /// <param name="renderer">The renderer of the model</param>
        public static Bounds CalculateHeightBounds(Renderer[] renderers)
        {
            Vector3 maxCoord = Vector3.zero, minCoord = Vector3.zero;
            foreach (Renderer renderer in renderers)
            {
                if (renderer.bounds.center.y + renderer.bounds.extents.y > maxCoord.y) maxCoord.y = renderer.bounds.center.y + renderer.bounds.extents.y;
                if (renderer.bounds.center.y - renderer.bounds.extents.y < minCoord.y) minCoord.y = renderer.bounds.center.y - renderer.bounds.extents.y;
            }
            return new Bounds((maxCoord + minCoord) / 2, maxCoord - minCoord);
        }

        /// <summary>
        /// Inform the user that their model has been exported to Animate Anything, and start polling for a response.
        /// </summary>
        /// <param name="modelID">The ID of the model sent to Animate Anything</param>
        public static void OnSuccessfulExport(string modelID)
        {
            inputFieldValue = modelID;
            AssetDatabase.Refresh();
            Debug.Log($"Success! It might take a while before the model will appear on your 'Processed Models' page, but please stay put!");
            Debug.Log("We will attempt to add the model into your project files / scene once it has finished processing, however this process will be cancelled if you recompile and refresh the engine. Please refrain from switching scenes or updating any scripts until the model has finished processing. If a recompile happens, don't worry - your model is still being processed in the background.");
            CompilationPipeline.compilationFinished += RecompileStatement;
            AnythingAnimate.Poll(OnValidResponse, OnPollFailure, OnPollFeedback, modelID, polling, 300);
        }

        public static void RecompileStatement(object obj)
        {
            Debug.LogWarning("Editor Recompiled. Your model won't automatically be added to the scene after processing.");
        }

        /// <summary>
        /// Informing the user of the polling state of Animate Anything
        /// </summary>
        /// <param name="code">The code received from the polling endpoint</param>
        /// <param name="message">The message received from the polling endpoint</param>
        /// <param name="secondsOfPolling">The amount of time the model has been polling</param>
        public static void OnPollFeedback(string code, string message, int secondsOfPolling)
        {
            polling = true;
            pollCode = code;
            pollTime = secondsOfPolling;
        }

        /// <summary>
        /// Informing the user if Animate Anything fails in processing a model
        /// </summary>
        /// <param name="code">The code received from the polling endpoint</param>
        /// <param name="message">The message received from the polling endpoint</param>
        public static void OnPollFailure(string code, string message)
        {
            CompilationPipeline.compilationFinished -= RecompileStatement;
            polling = false;
            pollCode = null;
            pollTime = 0;
            EditorLoadingIcon.Instance.ShowToastyMessage($"Animate Anything Error - {code}: {message}", GetWindow<AnimateAnythingEditor>(), 5f);
        }

        /// <summary>
        /// Creates the object once the model has finished processing
        /// </summary>
        /// <param name="json">The JSON file of the processed model</param>
        public static void OnValidResponse(ModelJson json)
        {
            CompilationPipeline.compilationFinished -= RecompileStatement;
            polling = false;
            pollCode = null;
            pollTime = 0;
            var requestParams = new RequestParams();
            requestParams
                .SetPosition(TransformSettings.PositionField)
                .SetRotation(TransformSettings.RotationField)
                .SetScale(relativeHeightBoundsSize.size)
                .SetScaleType(Utilities.ScaleType.ScaleRealWorld)
                .SetParent(objectParent)
                .SetPlaceOnGrid(TransformSettings.PlaceOnGrid)
                .SetPlaceOnGround(TransformSettings.PlaceOnGround)
                .SetUseGridArea(TransformSettings.GridAreaEnabled)
                .SetAddBehaviour(processedAddBehaviour)
                .SetAddRigidbody(TransformSettings.AddRigidbody)
                .SetAddCollider(TransformSettings.AddCollider)
                .SetDefaultBehaviourPreset(DefaultBehavioursUtility.CreateNewTemporaryInstance(DefaultBehaviourDictionary))
                .SetAnimateModel(TransformSettings.AnimateModel)
                .SetModelCaching(TransformSettings.CacheModel)
                .SetSerializeAssets(true);
            
            AnythingMaker.Make(json, requestParams);
            processedAddBehaviour = addBehaviour;
        }
        #endregion Helper Functions
    }
}
