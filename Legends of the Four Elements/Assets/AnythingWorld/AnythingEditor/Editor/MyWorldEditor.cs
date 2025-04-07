using AnythingWorld.Networking.Editor;
using AnythingWorld.Utilities.Data;

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace AnythingWorld.Editor
{
    public class MyWorldEditor : AnythingCreatorEditor
    {
        #region Fields
        public enum MyWorldTabCategory
        {
            LIKES, LISTS, PROCESS
        }
        public static MyWorldEditor instance;
        public static MyWorldEditor Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = GetWindow<MyWorldEditor>();
                }
                return instance;
            }
            set
            {
                instance = value;
            }
        }

        private MyWorldTabCategory myWorldTabCategory;
        private bool modelLevel = true;
        private CollectionResult currentCollection;
        private List<CollectionResult> collections;
        private List<SearchResult> likedObjects;
        #endregion Fields

        #region Textures
        private static Texture2D baseLikesIcon;
        protected static Texture2D BaseLikesIcon
        {
            get
            {
                if (baseLikesIcon == null)
                {
                    baseLikesIcon = Resources.Load("Editor/Shared/Icons/ObjectIcons/emptyHeart") as Texture2D;
                }
                return baseLikesIcon;
            }
        }
        private static Texture2D baseProcessIcon;
        protected static Texture2D BaseProcessIcon
        {
            get
            {
                if (baseProcessIcon == null)
                {
                    baseProcessIcon = Resources.Load("Editor/Shared/Icons/ObjectIcons/objectType") as Texture2D;
                }
                return baseProcessIcon;
            }
        }
        private static Texture2D baseListsIcon;
        protected static Texture2D BaseListsIcon
        {
            get
            {
                if (baseListsIcon == null)
                {
                    baseListsIcon = Resources.Load("Editor/Shared/Icons/ObjectIcons/collections") as Texture2D;
                }
                return baseListsIcon;
            }
        }
        private static Texture2D baseDeleteIcon;
        protected static Texture2D BaseDeleteIcon
        {
            get
            {
                if (baseDeleteIcon == null)
                {
                    baseDeleteIcon = Resources.Load("Editor/Shared/Icons/SettingsIcons/delete") as Texture2D;
                }
                return baseDeleteIcon;
            }
        }
        private StateTexture2D stateProcessIcon;
        protected StateTexture2D StateProcessIcon
        {
            get
            {
                if (stateProcessIcon == null || !stateProcessIcon.TexturesLoadedNoHover)
                {
                    stateProcessIcon = new StateTexture2D(
                        TintTextureToEditorTheme(BaseProcessIcon, Color.black, Color.white),
                        TintTextureToEditorTheme(BaseProcessIcon, Color.white, Color.black));
                }
                return stateProcessIcon;
            }
            set => stateProcessIcon = value;
        }

        private StateTexture2D stateListsIcon;
        protected StateTexture2D StateListsIcon
        {
            get
            {
                if (stateListsIcon == null || !stateListsIcon.TexturesLoadedNoHover)
                {
                    stateListsIcon = new StateTexture2D(
                        TintTextureToEditorTheme(BaseListsIcon, Color.black, Color.white),
                        TintTextureToEditorTheme(BaseListsIcon, Color.white, Color.black));
                }
                return stateListsIcon;
            }
            set => stateListsIcon = value;
        }
        private StateTexture2D stateLikesIcon;
        protected StateTexture2D StateLikesIcon
        {
            get
            {
                if (stateLikesIcon == null || !stateLikesIcon.TexturesLoadedNoHover)
                {
                    stateLikesIcon = new StateTexture2D(
                        TintTextureToEditorTheme(BaseLikesIcon, Color.black, Color.white),
                        TintTextureToEditorTheme(BaseLikesIcon, Color.white, Color.black));
                }
                return stateLikesIcon;
            }
            set => stateLikesIcon = value;
        }
        private StateTexture2D stateDeleteIcon;
        protected StateTexture2D StateDeleteIcon
        {
            get
            {
                if (stateDeleteIcon == null || !stateDeleteIcon.TexturesLoadedHover)
                {
                    stateDeleteIcon = new StateTexture2D(
                        TintTextureToEditorTheme(BaseDeleteIcon, Color.white, Color.black),
                        TintTextureToEditorTheme(BaseDeleteIcon, Color.white, Color.black),
                        TintTextureToEditorTheme(BaseDeleteIcon, HexToColour("606162"), HexToColour("EDEEEC")));
                }
                return stateDeleteIcon;
            }
            set => stateDeleteIcon = value;
        }
        #endregion Textures

        #region Initialization
        [MenuItem("Tools/Anything World/My World", false, 22)]
        internal static void Initialize()
        {
            AnythingCreatorEditor tabWindow;
            Vector2 windowSize = new Vector2(500, 800);

            if (AnythingSettings.HasAPIKey)
            {
                CloseWindowIfOpen<MyWorldEditor>();
                var browser = GetWindow(typeof(MyWorldEditor), false, "My World");
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

        protected new void Awake()
        {
            base.Awake();
            Instance = this;
            windowTitle = "My World";
            bannerTintA = HexToColour("3CF2F3");
            bannerTintB = HexToColour("4BF841");
            if(AnythingSettings.HasAPIKey) InitializeCollectionsAndLikes();
            UserVoteProcessor.voteChangeDelegate -= RefreshCollectionsAndLikes;
            UserVoteProcessor.voteChangeDelegate += RefreshCollectionsAndLikes;
        }

        private void InitializeCollectionsAndLikes()
        {
            if (searchMode == SearchMode.RUNNING) return;
            if (searchMode == SearchMode.RUNNING_SILENTLY) return;
            if (collections == null || likedObjects == null)
            {
                searchMode = SearchMode.RUNNING;
                if (myWorldTabCategory == MyWorldTabCategory.LIKES) UserVoteProcessor.GetVoteCards(RefreshLikedObjects, Repaint, EditorNetworkErrorHandler.HandleError, this);
                if (myWorldTabCategory == MyWorldTabCategory.LISTS && !modelLevel) CollectionProcessor.GetCollectionsAsync(RefreshCollectionResults, EditorNetworkErrorHandler.HandleError, this);
            }
        }
        public void RefreshCollectionsAndLikes()
        {
            if (searchMode == SearchMode.RUNNING) return;
            if (searchMode == SearchMode.RUNNING_SILENTLY) return;
            if (collections == null || likedObjects == null)
            {
                searchMode = SearchMode.RUNNING;
                if (myWorldTabCategory == MyWorldTabCategory.LIKES) UserVoteProcessor.GetVoteCards(RefreshLikedObjects, Repaint, EditorNetworkErrorHandler.HandleError, this);
                if (myWorldTabCategory == MyWorldTabCategory.LISTS && !modelLevel) CollectionProcessor.GetCollectionsAsync(RefreshCollectionResults, EditorNetworkErrorHandler.HandleError, this);
            }
            else
            {
                searchMode = SearchMode.RUNNING_SILENTLY;
                if (myWorldTabCategory == MyWorldTabCategory.LIKES) UserVoteProcessor.GetVoteCards(RefreshLikedObjects, Repaint, EditorNetworkErrorHandler.HandleError, this);
                if (myWorldTabCategory == MyWorldTabCategory.LISTS && !modelLevel) CollectionProcessor.GetCollectionsAsync(RefreshCollectionResults, EditorNetworkErrorHandler.HandleError, this);
            }
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
                EditorGUILayout.BeginVertical();
                if (selectedResult == null || string.IsNullOrEmpty(selectedResult.data._id)) DrawMyWorld();
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

            //if click inside window cancel drag
            if (Event.current.type == EventType.MouseDown)
            {
                if (isDragging)
                {
                    isDragging = false;
                    SceneTextureDrawer.Instance.CancelDrag();
                }
            }

            //if drag was dropped inside window cancel drag
            if (Event.current.type == EventType.DragExited)
            {
                // Check if the drag was dropped inside the editor tab
                Rect editorTabRect = new Rect(0, 0, position.width, position.height);
                if (editorTabRect.Contains(Event.current.mousePosition))
                {
                    if (isDragging)
                    {
                        isDragging = false;
                        SceneTextureDrawer.Instance.CancelDrag();
                    }
                }
            }
        }
        private void DrawMyWorld()
        {
            var padding = 8f;
            GUILayout.Space(padding);

            var tabRect = GUILayoutUtility.GetRect(position.width, 40);
            var tabWidth = (tabRect.width - (padding * 3)) / 3;

            var likesTabRect = new Rect(tabRect.center.x - (tabWidth + padding) - tabWidth / 2, tabRect.y, tabWidth, tabRect.height);
            var listsTabRect = new Rect(tabRect.center.x - tabWidth / 2, tabRect.y, tabWidth , tabRect.height);
            var processTabRect = new Rect(tabRect.center.x + (tabWidth + padding) - tabWidth / 2, tabRect.y, tabWidth, tabRect.height);

            if (DrawSquareButton(likesTabRect, new GUIContent("My Likes", myWorldTabCategory == MyWorldTabCategory.LIKES ? StateLikesIcon.activeTexture : StateLikesIcon.inactiveTexture), myWorldTabCategory == MyWorldTabCategory.LIKES, 14))
            {
                modelLevel = true;
                currentCollection = null;
                myWorldTabCategory = MyWorldTabCategory.LIKES;
                searchMode = SearchMode.RUNNING;
                UserVoteProcessor.GetVoteCards(RefreshLikedObjects, Repaint, EditorNetworkErrorHandler.HandleError, this);
            }

            if (DrawSquareButton(processTabRect, new GUIContent("Processed Models", myWorldTabCategory == MyWorldTabCategory.PROCESS ? StateProcessIcon.activeTexture : StateProcessIcon.inactiveTexture), myWorldTabCategory == MyWorldTabCategory.PROCESS, 14))
            {
                modelLevel = true;
                currentCollection = null;
                myWorldTabCategory = MyWorldTabCategory.PROCESS;
                searchMode = SearchMode.RUNNING;
                UserProcessedModels.GetProcessedModels(RefreshProcessObjects, Repaint, EditorNetworkErrorHandler.HandleError, this);
            }

            if (DrawSquareButton(listsTabRect, new GUIContent("My Lists", myWorldTabCategory == MyWorldTabCategory.LISTS ? StateListsIcon.activeTexture : StateListsIcon.inactiveTexture), myWorldTabCategory == MyWorldTabCategory.LISTS, 14))
            {
                modelLevel = false;
                currentCollection = null;
                myWorldTabCategory = MyWorldTabCategory.LISTS;
                searchMode = SearchMode.RUNNING;
                CollectionProcessor.GetCollectionsAsync(RefreshCollectionResults, EditorNetworkErrorHandler.HandleError, this).Forget();
            }

            switch (myWorldTabCategory)
            {
                case MyWorldTabCategory.LIKES:
                    DrawToolbar(likedObjects, padding);
                    break;
                case MyWorldTabCategory.LISTS when modelLevel:
                    DrawToolbar(currentCollection.Results, padding, currentCollection.DisplayName);
                    break;
                case MyWorldTabCategory.LISTS:
                    DrawToolbar(collections, padding);
                    break;
                case MyWorldTabCategory.PROCESS:
                    DrawToolbar(likedObjects, padding);
                    break;
            }

            if (modelLevel)
            {
                DrawFilters(false);
                GUILayout.Space(16);
                var settingsRect = GUILayoutUtility.GetRect(position.width, 30);
                settingsRect.x += 10;
                settingsRect.width -= settingsRect.x * 2;
                DrawSettingsIcons(settingsRect);
            }

            GUILayout.Space(16);
            DrawMyContent();
        }

        
        private Rect miscRect;
        protected Vector2 newScrollPosition;

        private void DrawMyContent()
        {
            var tempRect = GUILayoutUtility.GetLastRect();
            if (tempRect != new Rect(0, 0, 1, 1)) miscRect = tempRect;

            switch (searchMode)
            {
                case SearchMode.IDLE:
                    DrawError(miscRect);
                    break;
                case SearchMode.RUNNING:
                    DrawLoading(miscRect);
                    break;
                case SearchMode.RUNNING_SILENTLY:
                case SearchMode.SUCCESS:
                    switch (myWorldTabCategory)
                    {
                        case MyWorldTabCategory.LIKES:
                            DrawGrid(filteredResults, filteredResults.Count, 100, 120, DrawBrowserCard, ref newScrollPosition, resultThumbnailMultiplier);
                            break;
                        case MyWorldTabCategory.LISTS when modelLevel:
                            DrawGrid(filteredResults, filteredResults.Count, 100, 120, DrawListItemCard, ref newScrollPosition, resultThumbnailMultiplier);
                            break;
                        case MyWorldTabCategory.LISTS:
                            DrawGrid(collections, collections.Count, 100, 120, DrawListCard, ref newScrollPosition, resultThumbnailMultiplier);

                            break;
                        case MyWorldTabCategory.PROCESS:
                            DrawGrid(filteredResults, filteredResults.Count, 100, 120, DrawBrowserCard, ref newScrollPosition, resultThumbnailMultiplier);
                            break;
                    }
                    break;
                case SearchMode.FAILURE:
                    DrawError(miscRect);
                    break;
            }
        }

        private void DrawToolbar<T>(List<T> results, float padding = 10f, string title = "")
        {
            Event e = Event.current;

            if (modelLevel && currentCollection != null && myWorldTabCategory == MyWorldTabCategory.LISTS)
            {
                var backButtonStyle = new GUIStyle(IconStyle)
                {
                    font = GetPoppinsFont(PoppinsStyle.SemiBold),
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 12,
                    wordWrap = true,
                    normal = new GUIStyleState
                    {
                        textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black
                    }
                };

                var backText = new GUIContent("Back", StateBackIcon.activeTexture);
                var backTextSize = backButtonStyle.CalcSize(backText);

                var backRect = GUILayoutUtility.GetRect(position.width, backTextSize.y * 1.5f);

                var margin = (backRect.height - backTextSize.y) / 2;

                var backIconRect = new Rect(backRect.x + margin, backRect.y + margin, backTextSize.x, backTextSize.y);
                if (backIconRect.Contains(e.mousePosition))
                {
                    backText.image = StateBackIcon.hoverTexture;
                    backButtonStyle.normal = SetStyleState(EditorGUIUtility.isProSkin ? HexToColour("606162") : HexToColour("EDEEEC"));
                }

                if (GUI.Button(backIconRect, backText, backButtonStyle))
                {
                    modelLevel = false;
                    currentCollection = null;
                    if (searchMode != SearchMode.RUNNING)
                    {
                        searchMode = SearchMode.RUNNING;
                        CollectionProcessor.GetCollectionsAsync(RefreshCollectionResults, EditorNetworkErrorHandler.HandleError, this).Forget();
                    }
                }
            }
            else
            {
                GUILayout.Space(8f);
            }

            var optionsRect = GUILayoutUtility.GetRect(position.width, 20f);

            var headerLabelContent = new GUIContent(myWorldTabCategory switch
            {
                MyWorldTabCategory.LIKES => "My Likes",
                MyWorldTabCategory.LISTS when !modelLevel => "My Lists",
                MyWorldTabCategory.LISTS when modelLevel => title,
                _ => ""
            });

            var headerLabelStyle = new GUIStyle(HeaderLabelStyle) { font = GetPoppinsFont(PoppinsStyle.SemiBold), fontSize = 20, normal = SetStyleState(EditorGUIUtility.isProSkin ? Color.white : Color.black) };
            var headerLabelSize = headerLabelStyle.CalcSize(headerLabelContent);
            var headerLabelRect = new Rect(optionsRect.x + padding, optionsRect.y, headerLabelSize.x, optionsRect.height);
            GUI.Label(headerLabelRect, headerLabelContent, headerLabelStyle);

            if (results != null)
            {
                if (searchMode == SearchMode.SUCCESS || searchMode == SearchMode.RUNNING_SILENTLY)
                {
                    var pluraliser = "s";
                    if (results.Count == 1) pluraliser = "";
                    var resultsFillerContent = new GUIContent(myWorldTabCategory switch
                    {
                        MyWorldTabCategory.LIKES => $" liked model{pluraliser}",
                        MyWorldTabCategory.LISTS when !modelLevel => $" list{pluraliser}",
                        MyWorldTabCategory.LISTS when modelLevel => $" object{pluraliser} in ",
                        _ => ""
                    });

                    var resultsCountContent = new GUIContent($"{results.Count}");
                    var resultsSearchTermContent = new GUIContent($"{(myWorldTabCategory == MyWorldTabCategory.LISTS && modelLevel ? title : "")}");

                    var resultsStandardStyle = new GUIStyle(BodyLabelStyle) { fontSize = 12, normal = SetStyleState(HexToColour("999999")), font = GetPoppinsFont(PoppinsStyle.Regular), wordWrap = false, alignment = TextAnchor.MiddleLeft, padding = UniformRectOffset(0) };
                    var resultsEmphasisStyle = new GUIStyle(resultsStandardStyle) { font = GetPoppinsFont(PoppinsStyle.SemiBold) };

                    var resultsCountSize = resultsEmphasisStyle.CalcSize(resultsCountContent);
                    var resultsFillerSize = resultsStandardStyle.CalcSize(resultsFillerContent);
                    var resultsSearchTermSize = resultsEmphasisStyle.CalcSize(resultsSearchTermContent);
                    var resultsLabelHeight = Mathf.Max(resultsCountSize.y, resultsFillerSize.y, resultsSearchTermSize.y);

                    var resultsLabelRect = new Rect(headerLabelRect.xMax + padding, headerLabelRect.yMax - resultsLabelHeight, Mathf.Min(optionsRect.width / 2, resultsCountSize.x + resultsFillerSize.x + resultsSearchTermSize.x), resultsLabelHeight);

                    var resultsCountRect = new Rect(resultsLabelRect.x, resultsLabelRect.y, resultsCountSize.x, resultsCountSize.y);
                    var resultsFillerRect = new Rect(resultsCountRect.xMax, resultsLabelRect.y, resultsFillerSize.x, resultsFillerSize.y);
                    var resultsSearchTermRect = new Rect(resultsFillerRect.xMax, resultsLabelRect.y, resultsSearchTermSize.x, resultsSearchTermSize.y);

                    GUI.Label(resultsCountRect, resultsCountContent, resultsEmphasisStyle);
                    GUI.Label(resultsFillerRect, resultsFillerContent, resultsStandardStyle);
                    GUI.Label(resultsSearchTermRect, resultsSearchTermContent, resultsEmphasisStyle);


                    var thumbnailSizingRectSize = new Vector2(optionsRect.width - resultsLabelRect.xMax - padding, optionsRect.height);
                    var thumbnailSizingRect = new Rect(resultsLabelRect.xMax + padding, optionsRect.yMax - resultsLabelHeight, thumbnailSizingRectSize.x, thumbnailSizingRectSize.y);

                    var roundedScale = Mathf.RoundToInt(resultThumbnailMultiplier * 10) / 10f;
                    var thumbnailMultiplierLabelContent = new GUIContent($"{roundedScale}x");
                    var thumbnailMultiplierLabelStyle = new GUIStyle(BodyLabelStyle) { fontSize = 10, normal = SetStyleState(HexToColour("999999")), font = GetPoppinsFont(PoppinsStyle.Medium), wordWrap = false, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(4, 0, 0, 2) };
                    var thumbnailMultiplierLabelMaxWidth = 25f;
                    var thumbnailMultiplierLabelRect = new Rect(thumbnailSizingRect.xMax - thumbnailMultiplierLabelMaxWidth, thumbnailSizingRect.y, thumbnailMultiplierLabelMaxWidth, thumbnailSizingRect.height);

                    var thumbnailMultiplierButtonRatioScalar = BaseThumbnailIcon.width / BaseThumbnailIcon.height;
                    var thumbnailMultiplierButtonHeight = Mathf.ClosestPowerOfTwo((int)(thumbnailSizingRect.height));

                    var thumbnailMultiplierSliderWidth = Mathf.Min(100, thumbnailSizingRect.width - (thumbnailMultiplierButtonHeight * thumbnailMultiplierButtonRatioScalar) - thumbnailMultiplierLabelMaxWidth);
                    var thumbnailMultiplierSliderRect = new Rect(thumbnailMultiplierLabelRect.x - thumbnailMultiplierSliderWidth, thumbnailSizingRect.y, thumbnailMultiplierSliderWidth, thumbnailSizingRect.height);

                    var thumbnailMultiplierButtonXPos = Mathf.Max(thumbnailMultiplierSliderRect.x - (thumbnailSizingRect.height - thumbnailMultiplierButtonHeight) - thumbnailMultiplierButtonHeight, thumbnailSizingRect.x);
                    var thumbnailMultiplierButtonRect = new Rect(thumbnailMultiplierButtonXPos, thumbnailSizingRect.center.y - (thumbnailMultiplierButtonHeight / 2), thumbnailMultiplierButtonHeight * thumbnailMultiplierButtonRatioScalar, thumbnailMultiplierButtonHeight);

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
                }
            }
            GUILayout.Space(8f);
        }

        private void DrawListCard(List<CollectionResult> resultArray, float columnCoord, float rowCoord, float buttonWidth, float buttonHeight, int searchIndex, float resultScaleMultiplier)
        {
            try
            {
                Event e = Event.current;
                // Set result data
                var result = collections[searchIndex];
                Texture2D[] displayThumbnails;
                if (result.Results != null && result.Results.Any())
                {
                    displayThumbnails = result.SearchResultThumbnails;
                }
                else
                {
                    displayThumbnails = null;
                }

                var collectionName = new GUIContent(result.DisplayName);
                var cardRect = new Rect(columnCoord, rowCoord, buttonWidth, buttonHeight);

                // Initialize padding and sizing 
                var iconSizeY = buttonHeight / 12;
                var iconSizeX = iconSizeY;

                var infoPaddingX = iconSizeX / 3f;
                var infoPaddingY = iconSizeY / 3f;

                //Draw elements
                GUI.DrawTexture(cardRect, TintedCardFrame, ScaleMode.ScaleToFit);

                var thumbnailRatio = (float)BaseCardThumbnailBackdrops[0].height / (float)BaseCardThumbnailBackdrops[0].width;
                var thumbnailBackdropRect = new Rect(cardRect.x, cardRect.y, buttonWidth, buttonWidth * thumbnailRatio);

                if (GUI.Button(thumbnailBackdropRect, new GUIContent(), GUIStyle.none))
                {
                    UpdateSearchResults(result.Results.ToArray(), "Sorry, that collection is empty!");
                    currentCollection = result;
                    modelLevel = true;
                }

                GUI.DrawTexture(thumbnailBackdropRect, BaseCardThumbnailBackdrops[searchIndex % BaseCardThumbnailBackdrops.Length]);
                if (displayThumbnails.Length >= 4)
                {
                    var topLeftThumbnailRect = new Rect(thumbnailBackdropRect.x, thumbnailBackdropRect.y, thumbnailBackdropRect.width / 2, thumbnailBackdropRect.height / 2);
                    GUI.DrawTexture(topLeftThumbnailRect, displayThumbnails[0], ScaleMode.ScaleAndCrop);

                    var topRightThumbnailRect = new Rect(thumbnailBackdropRect.x + thumbnailBackdropRect.width / 2, thumbnailBackdropRect.y, thumbnailBackdropRect.width / 2, thumbnailBackdropRect.height / 2);
                    GUI.DrawTexture(topRightThumbnailRect, displayThumbnails[1], ScaleMode.ScaleAndCrop);

                    var bottomLeftThumbnailRect = new Rect(thumbnailBackdropRect.x, thumbnailBackdropRect.y + thumbnailBackdropRect.height / 2, thumbnailBackdropRect.width / 2, thumbnailBackdropRect.height / 2);
                    GUI.DrawTexture(bottomLeftThumbnailRect, displayThumbnails[2], ScaleMode.ScaleAndCrop);

                    var bottomRightThumbnailRect = new Rect(thumbnailBackdropRect.x + thumbnailBackdropRect.width / 2, thumbnailBackdropRect.y + thumbnailBackdropRect.height / 2, thumbnailBackdropRect.width / 2, thumbnailBackdropRect.height / 2);
                    GUI.DrawTexture(bottomRightThumbnailRect, displayThumbnails[3], ScaleMode.ScaleAndCrop);
                }
                else if(displayThumbnails.Length >= 1)
                {
                    GUI.DrawTexture(thumbnailBackdropRect, displayThumbnails[0], ScaleMode.ScaleAndCrop);
                }
                else
                {
                    DrawLoadingSmall(thumbnailBackdropRect, 0.25f, true);
                }


                var infoRect = new Rect(thumbnailBackdropRect.x, thumbnailBackdropRect.yMax, buttonWidth, cardRect.height - thumbnailBackdropRect.height);

                GUI.Label(infoRect, collectionName, new GUIStyle(ModelNameStyle) { alignment = TextAnchor.MiddleCenter, fontSize = (int)(12 * resultThumbnailMultiplier), font = GetPoppinsFont(PoppinsStyle.SemiBold) });

                if (cardRect.Contains(e.mousePosition))
                {
                    GUI.DrawTexture(cardRect, BaseObjectSelectionTint, ScaleMode.ScaleToFit);
                    if (e.button == 0 && e.isMouse) GUI.DrawTexture(cardRect, BaseObjectSelectionTint, ScaleMode.ScaleToFit);
                    Repaint();
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }            
        }
        
        public void ResetDrag()
        {
            isDragging = false;
        }
        protected void DrawListItemCard(List<SearchResult> resultArray, float columnCoord, float rowCoord, float buttonWidth, float buttonHeight, int searchIndex, float resultScaleMultiplier)
        {
            try
            {
                Event e = Event.current;
                // Set result data
                var result = resultArray[searchIndex];
                var displayThumbnail = result.Thumbnail;

                var modelName = new GUIContent(result.DisplayName, result.DisplayName);
                var authorName = new GUIContent(result.data.author, result.data.author);
                var cardRect = new Rect(columnCoord, rowCoord, buttonWidth, buttonHeight);

                // Initialize padding and sizing 
                var iconSizeY = buttonHeight / 12;
                var iconSizeX = iconSizeY;

                var infoPaddingX = iconSizeX / 3f;
                var infoPaddingY = iconSizeY / 3f;

                //Draw elements
                GUI.DrawTexture(cardRect, TintedCardFrame, ScaleMode.ScaleToFit);

                var thumbnailRatio = (float)BaseCardThumbnailBackdrops[0].height / (float)BaseCardThumbnailBackdrops[0].width;
                var thumbnailBackdropRect = new Rect(cardRect.x, cardRect.y, buttonWidth, buttonWidth * thumbnailRatio);

                GUI.DrawTexture(thumbnailBackdropRect, BaseCardThumbnailBackdrops[searchIndex % BaseCardThumbnailBackdrops.Length]);
                if (cardRect.Contains(e.mousePosition))
                {
                    GUI.DrawTexture(cardRect, BaseObjectSelectionTint, ScaleMode.ScaleToFit);
                    if (e.button == 0 && e.isMouse)
                    {
                        GUI.DrawTexture(cardRect, BaseObjectSelectionTint, ScaleMode.ScaleToFit);
                        if (Event.current.type == EventType.MouseDown)
                        {
                            buttonPressed = true;
                            if (!SceneTextureDrawer.Instance.IsEnabled())
                            {
                                SceneTextureDrawer.Instance.Enable();
                            }
                        }
                        if (Event.current.type == EventType.MouseUp)
                        {
                            buttonPressed = false;
                            if (TransformSettings.ClickInPlacementLocation)
                            {
                                textureThumb = result.Thumbnail;
                                //send the result to the scene texture drawer
                                SceneTextureDrawer.Instance.SetCallBack(result.Thumbnail, MakeResult, result, ref isDragging);
                            }
                            else
                            {
                                MakeResult(result);
                                isDragging = false;
                            }
                        }
                        if (buttonPressed && Event.current.type == EventType.MouseDrag)
                        {
                            textureThumb = result.Thumbnail;
                            SceneTextureDrawer.Instance.SetCallBack(result.Thumbnail, MakeResult, result, ref isDragging);
                            isDragging = true;
                            DragAndDrop.PrepareStartDrag();
                            DragAndDrop.SetGenericData("Object", this);
                            DragAndDrop.StartDrag("Dragging an Object");
                        }
                        Repaint();
                    }
                    else if (e.button == 1 && e.isMouse && isDragging)
                    {
                        SceneTextureDrawer.Instance.CancelDrag();
                        isDragging = false;
                    }
                }

                if (isDragging && textureThumb)
                {
                    Vector2 texsize = new Vector2(textureThumb.width, textureThumb.height) * 0.8f;
                    GUI.DrawTexture(new Rect(e.mousePosition.x - texsize.x / 2, e.mousePosition.y - texsize.y / 2, texsize.x, texsize.y), textureThumb);
                    Repaint();
                }

                GUI.DrawTexture(thumbnailBackdropRect, BaseCardThumbnailBackdrops[searchIndex % BaseCardThumbnailBackdrops.Length], ScaleMode.ScaleToFit);

                if (displayThumbnail != null)
                {
                    GUI.DrawTexture(thumbnailBackdropRect, displayThumbnail, ScaleMode.ScaleAndCrop);
                }
                else
                {
                    DrawLoadingSmall(thumbnailBackdropRect, 0.25f, true);
                }

                var infoRect = new Rect(thumbnailBackdropRect.x, thumbnailBackdropRect.yMax, buttonWidth, cardRect.height - thumbnailBackdropRect.height);

                DrawCardVoteButton(result, ref infoRect, iconSizeX, iconSizeY, infoPaddingX, infoPaddingY, out var voteRect);
                DrawCardVoteCountLabel(infoPaddingX, voteRect, result.data.voteScore, resultScaleMultiplier);

                DrawCardInfoIcon(result, ref infoRect, iconSizeX, iconSizeY, infoPaddingX, infoPaddingY, out var detailRect);
                DrawDeleteListIcon(result, detailRect, iconSizeX, iconSizeY, infoPaddingX);

                DrawCardModelNameLabel(modelName, ref infoRect, infoPaddingX, infoPaddingY * 0.5f, out var modelNameLabelRect, resultScaleMultiplier);
                DrawCardAuthorLabel(authorName, ref infoRect, infoPaddingX, infoPaddingY * 0.75f, modelNameLabelRect, resultScaleMultiplier);

                if (result.isAnimated) DrawCardAnimationStatusIcon(thumbnailBackdropRect, iconSizeX, iconSizeY, infoPaddingX, infoPaddingY);

                if (cardRect.Contains(e.mousePosition))
                {
                    GUI.DrawTexture(cardRect, BaseObjectSelectionTint, ScaleMode.ScaleToFit);
                    if (e.button == 0 && e.isMouse) GUI.DrawTexture(cardRect, BaseObjectSelectionTint, ScaleMode.ScaleToFit);
                    Repaint();
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        protected void DrawDeleteListIcon(SearchResult result, Rect infoRect, float iconSizeX, float iconSizeY, float infoPaddingX)
        {
            var listRect = new Rect(infoRect.x - infoPaddingX - iconSizeX, infoRect.y, iconSizeX, iconSizeY);
            if (GUI.Button(listRect, new GUIContent((string)null, "Delete Object from List"), new GUIStyle(IconStyle) { normal = SetStyleState(StateDeleteIcon.activeTexture), hover = SetStyleState(StateDeleteIcon.hoverTexture) }))
            {
                AnythingSubwindow.OpenWindow($"Delete {result.DisplayName} from {currentCollection.DisplayName}?", new Vector2(300, 150), DrawRemoveFromCollectionWindow, position, result);
            }
        }

        protected void DrawDeleteCollectionWindow(AnythingEditor window)
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Are you sure you want to delete \"{currentCollection.DisplayName}\"?", new GUIStyle(HeaderLabelStyle) { fontSize = 16 });

            if (GUILayout.Button("Delete Collection", ButtonInactiveStyle))
            {
                CollectionProcessor.DeleteCollectionAsync(RefreshCollectionResults, currentCollection, EditorNetworkErrorHandler.HandleError, this).Forget();
                modelLevel = false;
                currentCollection = null;
                searchMode = SearchMode.RUNNING;
                CollectionProcessor.GetCollectionsAsync(RefreshCollectionResults, EditorNetworkErrorHandler.HandleError, this).Forget();
                window.Close();
            }
            GUILayout.FlexibleSpace();
        }

        protected void DrawRemoveFromCollectionWindow(AnythingEditor window, SearchResult result)
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Are you sure you want to remove \"{result.DisplayName}\" from \"{currentCollection.DisplayName}\"?", new GUIStyle(HeaderLabelStyle) { fontSize = 16 });

            if (GUILayout.Button($"Delete {result.DisplayName} from \"{currentCollection.DisplayName}\"", ButtonInactiveStyle))
            {
                if (currentCollection.Results.Any(x => x != result))
                {
                    CollectionProcessor.RemoveFromCollectionAsync(RefreshCollectionResults, result, currentCollection, EditorNetworkErrorHandler.HandleError, this).Forget();
                    modelLevel = true;
                    searchMode = SearchMode.RUNNING;
                    CollectionProcessor.GetCollectionAsync(RefreshCollectionInternalResults, currentCollection, EditorNetworkErrorHandler.HandleError, this).Forget();
                }
                else
                {
                    CollectionProcessor.DeleteCollectionAsync(RefreshCollectionResults, currentCollection, EditorNetworkErrorHandler.HandleError, this).Forget();
                    modelLevel = false;
                }
                window.Close();
            }
            GUILayout.FlexibleSpace();
        }
        #endregion Editor Drawing

        #region Helper Functions
        public void RefreshCollectionResults(CollectionResult[] collections)
        {
            if (this.collections == null && collections == null)
            {
                searchMode = SearchMode.FAILURE;
                return;
            }
            this.collections = collections.ToList();
            searchMode = SearchMode.SUCCESS;

            Instance.Repaint();
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
        public void RefreshCollectionInternalResults(CollectionResult result)
        {
            if (currentCollection == null && result == null)
            {
                searchMode = SearchMode.FAILURE;
                return;
            }

            if (result != null)
            {
                currentCollection = result;
                searchMode = SearchMode.SUCCESS;
                FilterSearchResult(currentCollection.Results);
            }
        }
        public void RefreshLikedObjects(SearchResult[] results)
        {
            if (likedObjects == null && results == null)
            {
                searchMode = SearchMode.FAILURE;
                return;
            }
            likedObjects = results.ToList();
            searchMode = SearchMode.SUCCESS;
            UpdateSearchResults(results, "You don't have any liked models!");
        }
        //refreshes the processed models list reusing a search result array
        public void RefreshProcessObjects(SearchResult[] results)
        {
            if (likedObjects == null && results == null)
            {
                searchMode = SearchMode.FAILURE;
                return;
            }
            likedObjects = results.ToList();
            searchMode = SearchMode.SUCCESS;
            UpdateSearchResults(results, "You don't have any processed models!");
        }

        #endregion Helper Functions
    }
}
