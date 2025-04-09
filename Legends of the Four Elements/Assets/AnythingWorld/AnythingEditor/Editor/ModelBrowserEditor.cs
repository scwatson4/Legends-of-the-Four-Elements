using AnythingWorld.Networking;
using AnythingWorld.Utilities;
using AnythingWorld.Utilities.Data;

using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

namespace AnythingWorld.Editor
{
    public class ModelBrowserEditor : AnythingCreatorEditor
    {
        #region Fields
        private string searchBarInput = "";
        private string searchTerm;

        #region Textures
        private Texture2D baseSearchIcon;

        static DateTime currentTime;

        protected Texture2D BaseSearchIcon
        {
            get
            {
                if (baseSearchIcon == null)
                {
                    baseSearchIcon = TintTextureToEditorTheme(Resources.Load("Editor/Shared/Icons/SettingsIcons/search") as Texture2D);
                }

                return baseSearchIcon;
            }
        }
        #endregion Textures
        #region Styles
        protected GUIStyle SearchBarStyle;
        #endregion Styles
        #endregion Fields

        #region Initialization
        /// <summary>
        /// Initializes and shows window, called from Anything World top bar menu.
        /// </summary>
        [MenuItem("Tools/Anything World/Browser", false, 21)]
        internal static void Initialize()
        {
            // set a draggable icon for the window
            AnythingCreatorEditor tabWindow;
            Vector2 windowSize = new Vector2(500, 800);
            if (AnythingSettings.HasAPIKey)
            {
                CloseWindowIfOpen<ModelBrowserEditor>();
                var browser = GetWindow(typeof(ModelBrowserEditor), false, "Model Browser");
                browser.position = new Rect(EditorGUIUtility.GetMainWindowPosition().center - windowSize / 2, windowSize);
                browser.Show();
                browser.Focus();
            }
            else
            {
                CloseWindowIfOpen<ModelBrowserEditor>();
                CloseWindowIfOpen<MyWorldEditor>();
                CloseWindowIfOpen<AICreatorEditor>();
                tabWindow = GetWindow<LogInEditor>("Log In | Sign Up", false, typeof(ModelBrowserEditor));
                tabWindow.position = new Rect(EditorGUIUtility.GetMainWindowPosition().center - windowSize / 2, windowSize);
            }
        }

        private void OnEnable()
        {
            //get the current time
            currentTime = DateTime.Now;
        }

        internal static void Initialize(Rect windowPosition)
        {
            AnythingCreatorEditor tabWindow;
            Vector2 windowSize = new Vector2(500, 800);
            Vector2 windowDiff = windowPosition.size - windowSize;

            if (AnythingSettings.HasAPIKey)
            {
                CloseWindowIfOpen<ModelBrowserEditor>();
                var browser = GetWindow(typeof(ModelBrowserEditor), false, "Model Browser");
                browser.position = new Rect(windowPosition.position + (windowDiff / 2), windowSize);

                browser.Show();
                browser.Focus();

                GetWindow<AnimateAnythingEditor>("Animate Anything", false);
                GetWindow<MyWorldEditor>("My World", false, typeof(ModelBrowserEditor));
                #if !UNITY_WEBGL
                GetWindow<AICreatorEditor>("AI Creator", false, typeof(ModelBrowserEditor));
                #endif
                browser.Focus();
            }
            else
            {
                tabWindow = GetWindow<LogInEditor>("Log In | Sign Up", false, typeof(ModelBrowserEditor));
                tabWindow.position = new Rect(windowPosition.position + (windowDiff / 2), windowSize);
            }
        }

        protected new void Awake()
        {
            base.Awake();
            windowTitle = "Browser";
            bannerTintA = HexToColour("00FDFF");
            bannerTintB = HexToColour("FF00E7");
            searchResults = new List<SearchResult>();
            filteredResults = new List<SearchResult>();

            OnFeatured();
        }
        protected override void DefineCustomStyles()
        {
            base.DefineCustomStyles();

            SearchBarStyle = new GUIStyle(InputFieldStyle)
            {
                contentOffset = new Vector2(32, 0)
            };
        }

#endregion Initialisation

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
                EditorGUILayout.BeginVertical();
                if (selectedResult == null || string.IsNullOrEmpty(selectedResult.data._id)) DrawBrowser();
                else DrawDetails();
                EditorGUILayout.EndVertical();

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

            if (searchMode == SearchMode.RUNNING)
            {
                Repaint();
                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();
            }
        }
        private void DrawBrowser()
        {
            DrawSearchBar();
            DrawFilters();
            GUILayout.Space(16);
            var settingsRect = GUILayoutUtility.GetRect(position.width, 30);
            settingsRect.x += 10;
            settingsRect.width -= settingsRect.x * 2;
            DrawSettingsIcons(settingsRect);
            GUILayout.Space(16);
            DrawBrowserContent();
        }

        Rect miscRect;
        protected Vector2 newScrollPosition;
        private void DrawBrowserContent()
        {
            Rect tempRect = GUILayoutUtility.GetLastRect();
            if (tempRect != new Rect(0, 0, 1, 1))
                miscRect = tempRect;

            switch (searchMode)
            {
                case SearchMode.IDLE:
                    OnFeatured();
                    break;
                case SearchMode.RUNNING:
                    try
                    {
                        DrawLoading(miscRect);
                    }
                    catch { }
                    break;
                case SearchMode.SUCCESS:
                    if (filteredResults.Count > 0)
                    {
                        DrawGrid(filteredResults, filteredResults.Count, 100.25f, 116.5f, DrawBrowserCard, ref newScrollPosition, resultThumbnailMultiplier);
                    }
                    else
                    {
                        if(string.IsNullOrEmpty(searchTerm))
                            searchModeFailReason = "Sorry, no results found. \n Please try search first.";

                        else
                            searchModeFailReason = $"Sorry, no results found for \"{searchTerm}\" when filtered.";
                        DrawError(miscRect);
                    }

                    break;
                case SearchMode.FAILURE:
                    DrawError(miscRect);
                    break;
            }
        }

        private void DrawSearchBar()
        {
            GUILayout.Space(16);

            if (Event.current.Equals(Event.KeyboardEvent("return")))
            {
                if (string.IsNullOrEmpty(searchBarInput)) OnFeatured();
                else OnSearch(searchBarInput);
            }

            GUI.SetNextControlName("SearchBar");
            Event e = Event.current;

            var searchBarRect = GUILayoutUtility.GetRect(position.width, 30, SearchBarStyle);
            var iconPadding = (searchBarRect.height - BaseSearchIcon.height) / 2f;

            var searchIconRect = new Rect(searchBarRect.xMin + iconPadding, searchBarRect.yMin + iconPadding, BaseSearchIcon.width, BaseSearchIcon.height);
            var clearIconRect = new Rect(searchBarRect.xMax - BaseClearIcon.width - iconPadding, searchBarRect.y + iconPadding, BaseClearIcon.width, BaseClearIcon.height);

            var edgeWidth = searchBarRect.height * ((float)BaseInputFieldRoundLeft.width / (float)BaseInputFieldRoundLeft.height);
            var searchBarLeftEdgeRect = new Rect(searchBarRect.xMin, searchBarRect.yMin, edgeWidth, searchBarRect.height);
            var searchBarRightEdgeRect = new Rect(searchBarRect.xMax - BaseInputFieldRoundRight.width, searchBarRect.yMin, edgeWidth, searchBarRect.height);
            var searchBarMainRect = new Rect(searchBarLeftEdgeRect.xMax, searchBarRect.y, searchBarRect.width - searchBarLeftEdgeRect.width - searchBarRightEdgeRect.width, searchBarRect.height);

            GUI.enabled = !(e.isMouse && clearIconRect.Contains(e.mousePosition));
            GUI.DrawTexture(searchBarLeftEdgeRect, BaseInputFieldRoundLeft);
            GUI.DrawTexture(searchBarMainRect, BaseInputFieldRoundMain, ScaleMode.StretchToFill);
            GUI.DrawTexture(searchBarRightEdgeRect, BaseInputFieldRoundRight);
            searchBarInput = GUI.TextField(searchBarRect, searchBarInput, SearchBarStyle);
            GUI.DrawTexture(searchIconRect, BaseSearchIcon);
            GUI.enabled = true;

            if (GUI.Button(clearIconRect, "", new GUIStyle(IconStyle) { normal = SetStyleState(StateClearIcon.activeTexture), hover = SetStyleState(StateClearIcon.hoverTexture) }))
            {
                searchBarInput = "";
            }
            if (searchMode == SearchMode.SUCCESS) DrawSearchOptionsBar(searchBarRect);
        }
        private void DrawSearchOptionsBar(Rect searchBarSize, float padding = 10f)
        {
            var optionsRect = new Rect(searchBarSize.x, searchBarSize.yMax + 8, searchBarSize.width, 20);
            float thumbnailSizingWidth = optionsRect.width;

            if (filteredResults != null)
            {
                var pluraliser = "s";
                var withTerm = " for ";
                if (filteredResults.Count == 1) pluraliser = "";
                if (string.IsNullOrEmpty(searchTerm)) withTerm = "";

                var resultsCountContent = new GUIContent($"{filteredResults.Count}");
                var resultsFillerContent = new GUIContent($" result{pluraliser}{withTerm}");
                var resultsSearchTermContent = new GUIContent($"{searchTerm}");

                var resultsStandardStyle = new GUIStyle(BodyLabelStyle) { fontSize = 12, normal = SetStyleState(HexToColour("999999")), font = GetPoppinsFont(PoppinsStyle.Regular), wordWrap = false, alignment = TextAnchor.MiddleLeft, padding = UniformRectOffset(0) };
                var resultsEmphasisStyle = new GUIStyle(resultsStandardStyle) { font = GetPoppinsFont(PoppinsStyle.SemiBold) };

                var resultsCountSize = resultsEmphasisStyle.CalcSize(resultsCountContent);
                var resultsFillerSize = resultsStandardStyle.CalcSize(resultsFillerContent);
                var resultsSearchTermSize = resultsEmphasisStyle.CalcSize(resultsSearchTermContent);

                var resultsLabelRect = new Rect(optionsRect.x, optionsRect.y, Mathf.Min(optionsRect.width / 2, resultsCountSize.x + resultsFillerSize.x + resultsSearchTermSize.x), optionsRect.height);

                var resultsCountRect = new Rect(resultsLabelRect.x, resultsLabelRect.y, resultsCountSize.x, resultsCountSize.y);
                var resultsFillerRect = new Rect(resultsCountRect.xMax, resultsLabelRect.y, resultsFillerSize.x, resultsFillerSize.y);
                var resultsSearchTermRect = new Rect(resultsFillerRect.xMax, resultsLabelRect.y, resultsSearchTermSize.x, resultsSearchTermSize.y);

                GUI.Label(resultsCountRect, resultsCountContent, resultsEmphasisStyle);
                GUI.Label(resultsFillerRect, resultsFillerContent, resultsStandardStyle);
                GUI.Label(resultsSearchTermRect, resultsSearchTermContent, resultsEmphasisStyle);
                thumbnailSizingWidth = optionsRect.width - resultsLabelRect.xMax - padding;
            }

            var thumbnailSizingRectSize = new Vector2(thumbnailSizingWidth, optionsRect.height);
            var thumbnailSizingRect = new Rect(optionsRect.xMax - thumbnailSizingRectSize.x, optionsRect.y, thumbnailSizingRectSize.x, thumbnailSizingRectSize.y);

            var roundedScale = Mathf.RoundToInt(resultThumbnailMultiplier * 10) / 10f;
            var thumbnailMultiplierLabelContent = new GUIContent($"{roundedScale}x");
            var thumbnailMultiplierLabelStyle = new GUIStyle(BodyLabelStyle) { fontSize = 10, normal = SetStyleState(HexToColour("999999")), font = GetPoppinsFont(PoppinsStyle.Medium), wordWrap = false, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(4, 0, 0, 2) };
            var thumbnailMultiplierLabelMaxWidth = 25f;
            var thumbnailMultiplierLabelRect = new Rect(thumbnailSizingRect.xMax - thumbnailMultiplierLabelMaxWidth, thumbnailSizingRect.y, thumbnailMultiplierLabelMaxWidth, thumbnailSizingRect.height);

            var thumbnailMultiplierButtonRatioScalar = BaseThumbnailIcon.width / BaseThumbnailIcon.height;
            var thumbnailMultiplierButtonHeight = Mathf.ClosestPowerOfTwo((int)(thumbnailSizingRect.height));

            var thumbnailMultiplierSliderWidth = Mathf.Min(100, thumbnailSizingRect.width - (thumbnailMultiplierButtonHeight * thumbnailMultiplierButtonRatioScalar) - thumbnailMultiplierLabelMaxWidth);
            var thumbnailMultiplierSliderRect = new Rect(thumbnailMultiplierLabelRect.x - thumbnailMultiplierSliderWidth, thumbnailSizingRect.y, thumbnailMultiplierSliderWidth, thumbnailSizingRect.height);

            var thumbnailMultiplierButtonRect = new Rect(thumbnailMultiplierSliderRect.x - (thumbnailSizingRect.height - thumbnailMultiplierButtonHeight) - thumbnailMultiplierButtonHeight, thumbnailSizingRect.center.y - (thumbnailMultiplierButtonHeight / 2), thumbnailMultiplierButtonHeight * thumbnailMultiplierButtonRatioScalar, thumbnailMultiplierButtonHeight);

            if (thumbnailMultiplierSliderWidth >= 20)
            {
                GUI.DrawTexture(thumbnailMultiplierButtonRect, TintedThumbnailIcon, ScaleMode.ScaleToFit);
                if (GUI.Button(thumbnailMultiplierButtonRect, new GUIContent((string)null, "Reset Thumbnail Sizing"), IconStyle))
                {
                    resultThumbnailMultiplier = 1f;
                }

                resultThumbnailMultiplier = GUI.HorizontalSlider(thumbnailMultiplierSliderRect, resultThumbnailMultiplier, 0.5f, 2.5f);
                GUI.Label(thumbnailMultiplierLabelRect, thumbnailMultiplierLabelContent, thumbnailMultiplierLabelStyle);
            }

            GUILayoutUtility.GetRect(position.width, optionsRect.height, GUILayout.MinWidth(500));
            GUILayout.Space(20);
        }
#endregion Editor Drawing

#region Mechanics
        private void OnSearch(string termToSearch)
        {
            newScrollPosition = Vector2.zero;
            searchTerm = termToSearch;
            searchResults = new List<SearchResult>();
            searchMode = SearchMode.RUNNING;
            SearchResultRequester.RequestCategorySearchResults(searchTerm, UpdateSearchResults, Repaint, EditorNetworkErrorHandler.HandleError, this);
        }
        
        private void OnFeatured()
        {
            newScrollPosition = Vector2.zero;
            searchResults = new List<SearchResult>();
            searchMode = SearchMode.RUNNING;
            SearchResultRequester.RequestFeaturedResults(UpdateSearchResults, Repaint, EditorNetworkErrorHandler.HandleError, this);
        }
        
        protected override void ResetAnythingWorld(ResetMode resetMode)
        {
            base.ResetAnythingWorld(resetMode);
            if (resetMode != ResetMode.Scene)
            {
                searchBarInput = "";
                searchTerm = "";
                searchResults = new List<SearchResult>();
                filteredResults = new List<SearchResult>();
            }
        }
        
#endregion Mechanics
#region Draw Scene GUI
        void OnFocus()
        {
            SceneView.duringSceneGui -= this.OnSceneGUI;
            // Add (or re-add) the delegate.
            SceneView.duringSceneGui += this.OnSceneGUI;

            //check the time was passed 5 minutes

            //if the current time is too update
            if(currentTime.Year < 2000)
            {
                currentTime = DateTime.Now;
            }
            var minutes = DateTime.Now.Subtract(currentTime).TotalMinutes;
            if (minutes > 5)
            {
                //refresh the window
                if (searchBarInput == "")
                    OnFeatured();
                else
                    OnSearch(searchBarInput);
                currentTime = DateTime.Now;
            }
        }

        protected void OnSceneGUI(SceneView sceneView)
        {
            if (TransformSettings.ShowGridHandles)
            {
                Handles.Label(SimpleGrid.origin, "Grid Origin");
            }
            if (TransformSettings.GridAreaEnabled)
            {
                GridArea.DrawGizmos();
            }
        }
        private void OnDestroy()
        {
            SceneView.duringSceneGui -= this.OnSceneGUI;
        }
#endregion
    }
}
