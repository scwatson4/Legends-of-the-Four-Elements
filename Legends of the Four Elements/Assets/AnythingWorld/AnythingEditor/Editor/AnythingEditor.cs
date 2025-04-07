using AnythingWorld.Voice;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using AnythingWorld.Behaviour.Tree;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace AnythingWorld.Editor
{
    public class AnythingEditor : EditorWindow
    {
        #region Fields
        #region Colors
        protected Color ColourInactiveInteractiveElement { get => HexToColour(EditorGUIUtility.isProSkin ? "98999A" : "979797"); }
        protected Color ColourActiveInteractiveElement { get => HexToColour(EditorGUIUtility.isProSkin ? "575859" : "E4E5E3"); }
        protected Color ColourHoverInteractiveElement { get => HexToColour(EditorGUIUtility.isProSkin ? "606162" : "EDEEEC"); }

        #endregion
        #region Fonts
        private static Font poppinsThin;
        protected static Font POPPINS_THIN
        {
            get
            {
                if (poppinsThin == null)
                {
                    poppinsThin = (Font)Resources.Load("Fonts/Poppins/Poppins-Thin", typeof(Font));
                }
                return poppinsThin;
            }
        }

        private static Font poppinsExtraLight;
        protected static Font POPPINS_EXTRA_LIGHT
        {
            get
            {
                if (poppinsExtraLight == null)
                {
                    poppinsExtraLight = (Font)Resources.Load("Fonts/Poppins/Poppins-ExtraLight", typeof(Font));
                }
                return poppinsExtraLight;
            }
        }

        private static Font poppinsLight;
        protected static Font POPPINS_LIGHT
        {
            get
            {
                if (poppinsLight == null)
                {
                    poppinsLight = (Font)Resources.Load("Fonts/Poppins/Poppins-Light", typeof(Font));
                }
                return poppinsLight;
            }
        }

        private static Font poppinsRegular;
        protected static Font POPPINS_REGULAR
        {
            get
            {
                if (poppinsRegular == null)
                {
                    poppinsRegular = (Font)Resources.Load("Fonts/Poppins/Poppins-Regular", typeof(Font));
                }
                return poppinsRegular;
            }
        }

        private static Font poppinsMedium;
        protected static Font POPPINS_MEDIUM
        {
            get
            {
                if (poppinsMedium == null)
                {
                    poppinsMedium = (Font)Resources.Load("Fonts/Poppins/Poppins-Medium", typeof(Font));
                }
                return poppinsMedium;
            }
        }

        private static Font poppinsSemiBold;
        protected static Font POPPINS_SEMI_BOLD
        {
            get
            {
                if (poppinsSemiBold == null)
                {
                    poppinsSemiBold = (Font)Resources.Load("Fonts/Poppins/Poppins-SemiBold", typeof(Font));
                }
                return poppinsSemiBold;
            }
        }

        private static Font poppinsBold;
        protected static Font POPPINS_BOLD
        {
            get
            {
                if (poppinsBold == null)
                {
                    poppinsBold = (Font)Resources.Load("Fonts/Poppins/Poppins-Bold", typeof(Font));
                }
                return poppinsBold;
            }
        }

        private static Font poppinsExtraBold;
        protected static Font POPPINS_EXTRA_BOLD
        {
            get
            {
                if (poppinsExtraBold == null)
                {
                    poppinsExtraBold = (Font)Resources.Load("Fonts/Poppins/Poppins-ExtraBold", typeof(Font));
                }
                return poppinsExtraBold;
            }
        }

        private static Font poppinsBlack;
        protected static Font POPPINS_BLACK
        {
            get
            {
                if (poppinsBlack == null)
                {
                    poppinsBlack = (Font)Resources.Load("Fonts/Poppins/Poppins-Black", typeof(Font));
                }
                return poppinsBlack;
            }
        }

        public enum PoppinsStyle
        {
            Thin,
            ExtraLight,
            Light,
            Regular,
            Medium,
            SemiBold,
            Bold,
            ExtraBold,
            Black
        }
        #endregion Fonts
        #region Textures
        private Texture2D baseTabFrame;
        protected Texture2D BaseTabFrame
        {
            get
            {
                if (baseTabFrame == null)
                {
                    baseTabFrame = Resources.Load("Editor/AnythingBrowser/buttonFrame") as Texture2D;
                }
                return baseTabFrame;
            }
        }

        private Texture2D baseGradientBanner;
        protected Texture2D BaseGradientBanner
        {
            get
            {
                if (baseGradientBanner == null)
                {
                    baseGradientBanner = Resources.Load("Editor/Shared/gradientRectangleBW") as Texture2D;
                }
                return baseGradientBanner;
            }
        }

        private Texture2D baseAnythingGlobeLogo;
        protected Texture2D BaseAnythingGlobeLogo
        {
            get
            {
                if (baseAnythingGlobeLogo == null)
                {
                    baseAnythingGlobeLogo = Resources.Load("Editor/Shared/whiteGlobeLogo") as Texture2D;
                }
                return baseAnythingGlobeLogo;
            }
        }

        private Texture2D baseAnythingGlobeErrorLogo;
        protected Texture2D BaseAnythingGlobeErrorLogo
        {
            get
            {
                if (baseAnythingGlobeErrorLogo == null)
                {
                    baseAnythingGlobeErrorLogo = Resources.Load("Editor/Shared/whiteGlobeErrorLogo") as Texture2D;
                }
                return baseAnythingGlobeErrorLogo;
            }
        }

        private Texture2D baseAnythingGlobeLogoFilled;
        protected Texture2D BaseAnythingGlobeLogoFilled
        {
            get
            {
                if (baseAnythingGlobeLogoFilled == null)
                {
                    baseAnythingGlobeLogoFilled = Resources.Load("Editor/Shared/filledGreenGlobe") as Texture2D;
                }

                return baseAnythingGlobeLogoFilled;
            }
        }

        private Texture2D baseAnythingGlobeErrorLogoFilled;
        protected Texture2D BaseAnythingGlobeErrorLogoFilled
        {
            get
            {
                if (baseAnythingGlobeErrorLogoFilled == null)
                {
                    baseAnythingGlobeErrorLogoFilled = Resources.Load("Editor/Shared/filledGreenErrorGlobe") as Texture2D;
                }

                return baseAnythingGlobeErrorLogoFilled;
            }
        }

        private Texture2D baseLoadingIconSmall;
        protected Texture2D BaseLoadingIconSmall
        {
            get
            {
                if (baseLoadingIconSmall == null)
                {
                    baseLoadingIconSmall = Resources.Load("Editor/Shared/loadingIcon") as Texture2D;
                }
                return baseLoadingIconSmall;
            }
        }
        private Texture2D baseDropdownArrow;
        protected Texture2D BaseDropdownArrow
        {
            get
            {
                if (baseDropdownArrow == null)
                {
                    baseDropdownArrow = TintTextureToEditorTheme(Resources.Load("Editor/Shared/dropdownArrow") as Texture2D);
                }
                return baseDropdownArrow;
            }
        }
        private static Texture2D baseConfirmIcon;
        protected static Texture2D BaseConfirmIcon
        {
            get
            {
                if (baseConfirmIcon == null)
                {
                    baseConfirmIcon = Resources.Load("Editor/Shared/confirmIcon") as Texture2D;
                }
                return baseConfirmIcon;
            }
        }
        private static Texture2D baseDenyIcon;
        protected static Texture2D BaseDenyIcon
        {
            get
            {
                if (baseDenyIcon == null)
                {
                    baseDenyIcon = Resources.Load("Editor/Shared/denyIcon") as Texture2D;
                }
                return baseDenyIcon;
            }
        }

        #region Toggle
        private static Texture2D baseToggleBackdrop;
        protected static Texture2D BaseToggleBackdrop
        {
            get
            {
                if (baseToggleBackdrop == null)
                {
                    baseToggleBackdrop = Resources.Load("Editor/Shared/toggleBackdrop") as Texture2D;
                }
                return baseToggleBackdrop;
            }
        }
        #endregion Toggle
        #region Button Backdrops
        #region Rounded Button Backdrop
        private Texture2D baseRoundButtonLeft;
        protected Texture2D BaseRoundButtonLeft
        {
            get
            {
                if (baseRoundButtonLeft == null)
                {
                    baseRoundButtonLeft = Resources.Load("Editor/Shared/buttonRoundLeft") as Texture2D;
                }
                return baseRoundButtonLeft;
            }
        }

        private Texture2D baseRoundButtonRight;
        protected Texture2D BaseRoundButtonRight
        {
            get
            {
                if (baseRoundButtonRight == null)
                {
                    baseRoundButtonRight = Resources.Load("Editor/Shared/buttonRoundRight") as Texture2D;
                }
                return baseRoundButtonRight;
            }
        }
        #endregion Rounded Button Backdrop
        #region Square Button Backdrop
        private Texture2D baseSquareButtonLeft;
        protected Texture2D BaseSquareButtonLeft
        {
            get
            {
                if (baseSquareButtonLeft == null)
                {
                    baseSquareButtonLeft = Resources.Load("Editor/Shared/buttonSquareLeft") as Texture2D;
                }
                return baseSquareButtonLeft;
            }
        }

        private Texture2D baseSquareButtonRight;
        protected Texture2D BaseSquareButtonRight
        {
            get
            {
                if (baseSquareButtonRight == null)
                {
                    baseSquareButtonRight = Resources.Load("Editor/Shared/buttonSquareRight") as Texture2D;
                }
                return baseSquareButtonRight;
            }
        }
        #endregion Square Button Backdrop
        private Texture2D baseButtonMain;
        protected Texture2D BaseButtonMain
        {
            get
            {
                if (baseButtonMain == null)
                {
                    baseButtonMain = Resources.Load("Editor/Shared/buttonMiddle") as Texture2D;
                }
                return baseButtonMain;
            }
        }
        #endregion Button Backdrops
        #region Label Backdrop
        private Texture2D baseLabelBackdropLeft;
        protected Texture2D BaseLabelBackdropLeft
        {
            get
            {
                if (baseLabelBackdropLeft == null)
                {
                    baseLabelBackdropLeft = Resources.Load("Editor/Shared/labelBackdropLeft") as Texture2D;
                }
                return baseLabelBackdropLeft;
            }
        }

        private Texture2D baseLabelBackdropRight;
        protected Texture2D BaseLabelBackdropRight
        {
            get
            {
                if (baseLabelBackdropRight == null)
                {
                    baseLabelBackdropRight = Resources.Load("Editor/Shared/labelBackdropRight") as Texture2D;
                }
                return baseLabelBackdropRight;
            }
        }

        private Texture2D baseLabelBackdropMiddle;
        protected Texture2D BaseLabelBackdropMiddle
        {
            get
            {
                if (baseLabelBackdropMiddle == null)
                {
                    baseLabelBackdropMiddle = Resources.Load("Editor/Shared/labelBackdropMiddle") as Texture2D;
                }
                return baseLabelBackdropMiddle;
            }
        }
        #endregion Label Backdrop
        #region Rounded Input Field Backdrop
        private Texture2D baseInputFieldRoundLeft;
        protected Texture2D BaseInputFieldRoundLeft
        {
            get
            {
                if (baseInputFieldRoundLeft == null)
                {
                    baseInputFieldRoundLeft = Resources.Load("Editor/Shared/inputFieldRoundLeft") as Texture2D;
                }
                return baseInputFieldRoundLeft;
            }
        }

        private Texture2D baseInputFieldRoundRight;
        protected Texture2D BaseInputFieldRoundRight
        {
            get
            {
                if (baseInputFieldRoundRight == null)
                {
                    baseInputFieldRoundRight = Resources.Load("Editor/Shared/inputFieldRoundRight") as Texture2D;
                }
                return baseInputFieldRoundRight;
            }
        }

        private Texture2D baseInputFieldRoundMain;
        protected Texture2D BaseInputFieldRoundMain
        {
            get
            {
                if (baseInputFieldRoundMain == null)
                {
                    baseInputFieldRoundMain = Resources.Load("Editor/Shared/inputFieldRoundMiddle") as Texture2D;
                }
                return baseInputFieldRoundMain;
            }
        }
        #endregion Rounded Input Field Backdrop
        #region Squared Input Field Backdrop
        private Texture2D baseInputFieldSquareLeft;
        protected Texture2D BaseInputFieldSquareLeft
        {
            get
            {
                if (baseInputFieldSquareLeft == null)
                {
                    baseInputFieldSquareLeft = Resources.Load("Editor/Shared/inputFieldSquareLeft") as Texture2D;
                }
                return baseInputFieldSquareLeft;
            }
        }

        private Texture2D baseInputFieldSquareRight;
        protected Texture2D BaseInputFieldSquareRight
        {
            get
            {
                if (baseInputFieldSquareRight == null)
                {
                    baseInputFieldSquareRight = Resources.Load("Editor/Shared/inputFieldSquareRight") as Texture2D;
                }
                return baseInputFieldSquareRight;
            }
        }

        private Texture2D baseInputFieldSquareMain;
        protected Texture2D BaseInputFieldSquareMain
        {
            get
            {
                if (baseInputFieldSquareMain == null)
                {
                    baseInputFieldSquareMain = Resources.Load("Editor/Shared/inputFieldSquareMiddle") as Texture2D;
                }
                return baseInputFieldSquareMain;
            }
        }
        #endregion Squared Input Field Backdrop

        #region Tinted Textures
        private Texture2D blackLoadingIconSmall;
        protected Texture2D BlackLoadingIconSmall
        {
            get
            {
                if (blackLoadingIconSmall == null)
                {
                    blackLoadingIconSmall = TintTexture(BaseLoadingIconSmall, Color.black);
                }
                return blackLoadingIconSmall;
            }
        }
        #endregion Tinted Textures

        #region State Textures
        private StateTexture2D stateTabFrame;
        protected StateTexture2D StateTabFrame
        {
            get
            {
                if (stateTabFrame == null || !stateTabFrame.TexturesLoadedHover)
                {
                    stateTabFrame = new StateTexture2D(
                        TintTexture(BaseTabFrame, ColourInactiveInteractiveElement),
                        TintTexture(BaseTabFrame, ColourActiveInteractiveElement),
                        TintTexture(BaseTabFrame, ColourHoverInteractiveElement));
                }
                return stateTabFrame;
            }
            set => stateTabFrame = value;
        }

        #region Button Backdrops
        private StateTexture2D stateButtonMain;
        protected StateTexture2D StateButtonMain
        {
            get
            {
                if (stateButtonMain == null || !stateButtonMain.TexturesLoadedHover)
                {
                    stateButtonMain = new StateTexture2D(
                        TintTexture(BaseButtonMain, ColourInactiveInteractiveElement),
                        TintTexture(BaseButtonMain, ColourActiveInteractiveElement),
                        TintTexture(BaseButtonMain, ColourHoverInteractiveElement));
                }

                return stateButtonMain;
            }
        }
        #region Rounded Button Backdrop
        private StateTexture2D stateRoundButtonLeft;
        protected StateTexture2D StateRoundButtonLeft
        {
            get
            {
                if (stateRoundButtonLeft == null || !stateRoundButtonLeft.TexturesLoadedHover)
                {
                    stateRoundButtonLeft = new StateTexture2D(
                        TintTexture(BaseRoundButtonLeft, ColourInactiveInteractiveElement),
                        TintTexture(BaseRoundButtonLeft, ColourActiveInteractiveElement),
                        TintTexture(BaseRoundButtonLeft, ColourHoverInteractiveElement));
                }

                return stateRoundButtonLeft;
            }
        }
        private StateTexture2D stateRoundButtonRight;
        protected StateTexture2D StateRoundButtonRight
        {
            get
            {
                if (stateRoundButtonRight == null || !stateRoundButtonRight.TexturesLoadedHover)
                {
                    stateRoundButtonRight = new StateTexture2D(
                        TintTexture(BaseRoundButtonRight, ColourInactiveInteractiveElement),
                        TintTexture(BaseRoundButtonRight, ColourActiveInteractiveElement),
                        TintTexture(BaseRoundButtonRight, ColourHoverInteractiveElement));
                }

                return stateRoundButtonRight;
            }
        }
        #endregion Rounded Button Backdrop

        #region Square Button Backdrop
        private StateTexture2D stateSquareButtonLeft;
        protected StateTexture2D StateSquareButtonLeft
        {
            get
            {
                if (stateSquareButtonLeft == null || !stateSquareButtonLeft.TexturesLoadedHover)
                {
                    stateSquareButtonLeft = new StateTexture2D(
                        TintTexture(BaseSquareButtonLeft, ColourInactiveInteractiveElement),
                        TintTexture(BaseSquareButtonLeft, ColourActiveInteractiveElement),
                        TintTexture(BaseSquareButtonLeft, ColourHoverInteractiveElement));
                }

                return stateSquareButtonLeft;
            }
        }
        private StateTexture2D stateSquareButtonRight;
        protected StateTexture2D StateSquareButtonRight
        {
            get
            {
                if (stateSquareButtonRight == null || !stateSquareButtonRight.TexturesLoadedHover)
                {
                    stateSquareButtonRight = new StateTexture2D(
                        TintTexture(BaseSquareButtonRight, ColourInactiveInteractiveElement),
                        TintTexture(BaseSquareButtonRight, ColourActiveInteractiveElement),
                        TintTexture(BaseSquareButtonRight, ColourHoverInteractiveElement));
                }

                return stateSquareButtonRight;
            }
        }
        #endregion Square Button Backdrop
        #endregion Button Backdrops

        #endregion State Textures
        #endregion Textures
        #region Styles

        protected static GUIStyle HeaderLabelStyle;
        protected static GUIStyle BodyLabelStyle;
        protected static GUIStyle ButtonStyle;
        protected static GUIStyle DropdownStyle;
        protected static GUIStyle InputFieldStyle;
        protected static GUIStyle ToggleStyle;
        protected static GUIStyle FieldLabelStyle;

        protected static GUIStyle ButtonActiveStyle;
        protected static GUIStyle ButtonInactiveStyle;
        protected static GUIStyle TabButtonInactiveStyle;
        protected static GUIStyle TabButtonActiveStyle;
        #endregion Styles
        protected static bool editorInitialized = false;
        protected float fieldLabelWidthPercentage = 0.4f;
        protected float fieldPadding = 8f;
        #endregion Fields

        #region Unity Messages

        protected void Awake()
        {
            editorInitialized = false;
        }

        protected void OnGUI()
        {
            InitializeResources();
            GUI.skin.settings.cursorFlashSpeed = -1;
        }
        #endregion Unity Messages

        #region Functions
        #region Initialization

        /// <summary>
        /// Calls the <see cref="InitializeCustomStyles"/> function.
        /// </summary>
        public bool InitializeResources()
        {
            editorInitialized = false;

            if (InitializeCustomStyles())
            {
                editorInitialized = true;
                return true;
            }
            return false;
        }

        public void InitializeResources(PlayModeStateChange state)
        {
            editorInitialized = InitializeCustomStyles();

            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode) Repaint();
        }

        private bool InitializeCustomStyles()
        {
            try
            {
                DefineCustomStyles();
            }
            catch (Exception e)
            {
                if (AnythingSettings.DebugEnabled) Debug.LogError($"Error initializing custom styles with error: \n{e}");
                if (AnythingSettings.DebugEnabled) Debug.LogException(e);
                return false;
            }
            return true;
        }

        protected virtual void DefineCustomStyles()
        {
            if (EditorStyles.boldLabel != null)
            {
                HeaderLabelStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    font = GetPoppinsFont(PoppinsStyle.Bold),
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 24,
                    margin = UniformRectOffset(10),
                    wordWrap = true
                };
            }

            if (BodyLabelStyle == null)
            {
                BodyLabelStyle = new GUIStyle(EditorStyles.label)
                {
                    font = GetPoppinsFont(PoppinsStyle.Regular),
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 12,
                    wordWrap = true
                };    
            }

            if (FieldLabelStyle == null)
            {
                FieldLabelStyle = new GUIStyle(EditorStyles.label)
                {
                    font = GetPoppinsFont(PoppinsStyle.Bold),
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 12,
                    wordWrap = true,
                    normal = SetStyleState(Color.white)
                };
            }

            if (ButtonStyle == null)
            {
                ButtonStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    stretchHeight = true,
                    fixedHeight = 30,
                    font = GetPoppinsFont(PoppinsStyle.Bold),
                    fontSize = 12,
                    margin = UniformRectOffset(10)
                };
            }
            
            if (DropdownStyle == null)
            {
                DropdownStyle = new GUIStyle
                {
                    stretchHeight = true,
                    font = GetPoppinsFont(PoppinsStyle.Medium),
                    fontSize = 12,
                    alignment = TextAnchor.MiddleLeft,
                    normal =
                    {
                        textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black
                    }
                };
            }
            
            if (InputFieldStyle == null)
            {
                InputFieldStyle = new GUIStyle
                {
                    font = GetPoppinsFont(PoppinsStyle.Medium),
                    fontSize = 14,
                    margin = UniformRectOffset(10),
                    padding =
                    {
                        top = 0
                    },
                    alignment = TextAnchor.MiddleLeft,
                    normal =
                    {
                        textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black
                    },
                    contentOffset = new Vector2(16, 0),
                    clipping = TextClipping.Clip
                };
            }

            if (ToggleStyle == null)
            {
                ToggleStyle = new GUIStyle(EditorStyles.toggle)
                {
                    fixedHeight = 30,
                    imagePosition = ImagePosition.ImageLeft,
                    font = GetPoppinsFont(PoppinsStyle.Regular),
                    alignment = TextAnchor.MiddleLeft,
                    margin = new RectOffset(InputFieldStyle.margin.left, 0, 0, 0),
                    padding = new RectOffset(20, 0, 0, 2)
                };
            }

            if (TabButtonInactiveStyle == null)
            {
                TabButtonInactiveStyle = new GUIStyle(ButtonStyle)
                {
                    normal = new GUIStyleState
                    {
                        textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black,
                        background = StateTabFrame.inactiveTexture
                    },
                    hover = new GUIStyleState
                    {
                        textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black,
                        background = StateTabFrame.hoverTexture
                    },
                    fixedHeight = 40
                };
            }
            
            if (TabButtonActiveStyle == null)
            {
                TabButtonActiveStyle = new GUIStyle(ButtonStyle)
                {
                    normal = new GUIStyleState
                    {
                        textColor = EditorGUIUtility.isProSkin ? Color.black : Color.white,
                        background = StateTabFrame.activeTexture
                    },
                    hover = new GUIStyleState
                    {
                        textColor = EditorGUIUtility.isProSkin ? Color.black : Color.white,
                        background = StateTabFrame.activeTexture
                    },
                    fixedHeight = 40
                };
            }

            if (ButtonActiveStyle == null)
            {
                ButtonActiveStyle = new GUIStyle(TabButtonActiveStyle)
                {
                    fixedHeight = 20,
                    fontSize = 10,
                    margin = UniformRectOffset(10),
                    padding = UniformRectOffset(2)
                };
            }

            if (ButtonInactiveStyle == null)
            {
                ButtonInactiveStyle = new GUIStyle(TabButtonInactiveStyle)
                {
                    fixedHeight = 20,
                    fontSize = 10,
                    margin = UniformRectOffset(10),
                    padding = UniformRectOffset(2)
                };
            }
        }
        #endregion Initialization

        #region Helper Functions
        protected struct DropdownOption
        {
            public string label;
            public GenericMenu.MenuFunction function;
            public GenericMenu.MenuFunction2 function2;
            public object dataEndpoint;
        }

        protected void DrawDropdown(Rect position, DropdownOption[] options, object dataMetric, string dropdownTitle = "", int dropdownPadding = 10, bool active = true)
        {
            var titleRect = new Rect();
            if (!string.IsNullOrEmpty(dropdownTitle))
            {
                var titleStyle = new GUIStyle(BodyLabelStyle) { fontSize = 10, wordWrap = false, font = GetPoppinsFont(PoppinsStyle.SemiBold), normal = SetStyleState(HexToColour("999999")), hover = SetStyleState(HexToColour("999999")) };
                var titleSize = titleStyle.CalcSize(new GUIContent(dropdownTitle));

                titleRect = new Rect(position.x + dropdownPadding, position.y, Mathf.Min(titleSize.x, position.width - (dropdownPadding * 2)), titleSize.y);
                GUI.Label(titleRect, new GUIContent(dropdownTitle), titleStyle);
            }

            var dropdownRect = new Rect(position.x + dropdownPadding, position.y + titleRect.height, position.width - (dropdownPadding * 2), position.height - titleRect.height);
            var dropdownStyle = new GUIStyle(DropdownStyle) { clipping = TextClipping.Clip, normal = SetStyleState(active ? Color.white : HexToColour("666666")), hover = SetStyleState(active ? Color.white : HexToColour("666666")) };
            var dropdownStatus = EditorGUI.DropdownButton(dropdownRect, new GUIContent(options.FirstOrDefault(x => x.dataEndpoint.Equals(dataMetric)).label), FocusType.Passive, dropdownStyle);
            DrawUILine(HexToColour("555555"), new Vector2(position.xMin + dropdownPadding, position.yMax), position.width - (dropdownPadding * 2));

            var arrowSize = dropdownRect.height / 2;
            var arrowPadding = (dropdownRect.height - arrowSize) / 2;
            var arrowRect = new Rect(dropdownRect.xMax - arrowSize - arrowPadding, dropdownRect.y + arrowPadding, arrowSize, arrowSize);
            GUI.DrawTexture(arrowRect, BaseDropdownArrow, ScaleMode.ScaleToFit);

            if (!dropdownStatus) return;

            if (active)
            {
                GenericMenu menu = new GenericMenu();

                foreach (var option in options)
                {
                    AddMenuOption(menu, option.label, option.function, option.dataEndpoint.Equals(dataMetric));
                }

                menu.DropDown(position);
            }
        }

        protected void AddMenuOption(GenericMenu menu, string label, GenericMenu.MenuFunction function, bool activeCondition)
        {
            bool itemActive = activeCondition;
            menu.AddItem(new GUIContent(label.Replace("&", "and")), itemActive, function);
        }

        protected bool DrawSquareButton(Rect buttonRect, GUIContent buttonContent, int fontSize = 12, PoppinsStyle fontOverride = PoppinsStyle.SemiBold)
        {
            Event e = Event.current;
            GUIStyle buttonStyle = new GUIStyle(BodyLabelStyle)
            {
                fontSize = fontSize,
                alignment = TextAnchor.MiddleCenter,
                normal = SetStyleState(EditorGUIUtility.isProSkin ? Color.white : Color.black),
                hover = SetStyleState(EditorGUIUtility.isProSkin ? Color.white : Color.black),
                font = GetPoppinsFont(fontOverride)
            };

            var buttonHeight = buttonRect.height;

            var textureScalar = buttonHeight / BaseButtonMain.height;
            var buffer = 2f;
            var buttonLeftEdgeRect = new Rect(buttonRect.xMin, buttonRect.yMin, BaseSquareButtonLeft.width * textureScalar, buttonHeight);
            var buttonRightEdgeRect = new Rect(buttonRect.xMax - (BaseSquareButtonRight.width * textureScalar), buttonRect.yMin, BaseSquareButtonRight.width * textureScalar, buttonHeight);
            var buttonMainRect = new Rect(buttonLeftEdgeRect.xMax - (buffer / 2), buttonRect.y, buttonRect.width - buttonLeftEdgeRect.width - buttonRightEdgeRect.width + buffer, buttonHeight);

            GUI.DrawTexture(buttonLeftEdgeRect, buttonRect.Contains(e.mousePosition) ? StateSquareButtonLeft.hoverTexture : StateSquareButtonLeft.inactiveTexture);
            GUI.DrawTexture(buttonMainRect, buttonRect.Contains(e.mousePosition) ? StateButtonMain.hoverTexture : StateButtonMain.inactiveTexture);
            GUI.DrawTexture(buttonRightEdgeRect, buttonRect.Contains(e.mousePosition) ? StateSquareButtonRight.hoverTexture : StateSquareButtonRight.inactiveTexture);

            EditorGUIUtility.SetIconSize(Vector2.one * fontSize);

            return GUI.Button(buttonRect, buttonContent, buttonStyle);
        }

        protected bool DrawSquareButton(Rect buttonRect, GUIContent buttonContent, bool active, int fontSize = 12, PoppinsStyle fontOverride = PoppinsStyle.SemiBold)
        {
            Event e = Event.current;
            GUIStyle buttonStyle = new GUIStyle(BodyLabelStyle)
            {
                fontSize = fontSize,
                alignment = TextAnchor.MiddleCenter,
                normal = SetStyleState(EditorGUIUtility.isProSkin ? active ? Color.black : Color.white : active ? Color.white : Color.black),
                hover = SetStyleState(EditorGUIUtility.isProSkin ? active ? Color.black : Color.white : active ? Color.white : Color.black),
                active = SetStyleState(EditorGUIUtility.isProSkin ? active ? Color.black : Color.white : active ? Color.white : Color.black),
                font = GetPoppinsFont(fontOverride)
            };

            var buttonHeight = buttonRect.height;

            var textureScalar = buttonHeight / BaseButtonMain.height;
            var buffer = 2f;
            var buttonLeftEdgeRect = new Rect(buttonRect.xMin, buttonRect.yMin, BaseSquareButtonLeft.width * textureScalar, buttonHeight);
            var buttonRightEdgeRect = new Rect(buttonRect.xMax - (BaseSquareButtonRight.width * textureScalar), buttonRect.yMin, BaseSquareButtonRight.width * textureScalar, buttonHeight);
            var buttonMainRect = new Rect(buttonLeftEdgeRect.xMax - (buffer / 2), buttonRect.y, buttonRect.width - buttonLeftEdgeRect.width - buttonRightEdgeRect.width + buffer, buttonHeight);

            GUI.DrawTexture(buttonMainRect, buttonRect.Contains(e.mousePosition) ? active ? StateButtonMain.activeTexture : StateButtonMain.hoverTexture : active ? StateButtonMain.activeTexture : StateButtonMain.inactiveTexture);
            GUI.DrawTexture(buttonLeftEdgeRect, buttonRect.Contains(e.mousePosition) ? active ? StateSquareButtonLeft.activeTexture : StateSquareButtonLeft.hoverTexture : active ? StateSquareButtonLeft.activeTexture : StateSquareButtonLeft.inactiveTexture);
            GUI.DrawTexture(buttonRightEdgeRect, buttonRect.Contains(e.mousePosition) ? active ? StateSquareButtonRight.activeTexture : StateSquareButtonRight.hoverTexture : active ? StateSquareButtonRight.activeTexture : StateSquareButtonRight.inactiveTexture);

            EditorGUIUtility.SetIconSize(Vector2.one * fontSize);

            return GUI.Button(buttonRect, buttonContent, buttonStyle);
        }

        protected bool DrawRoundedButton(Rect buttonRect, GUIContent buttonContent, GUIStyle buttonStyle)
        {
            Event e = Event.current;

            var buttonHeight = buttonRect.height;

            var textureScalar = buttonHeight / BaseButtonMain.height;
            var buffer = 2f;
            var buttonLeftEdgeRect = new Rect(buttonRect.xMin, buttonRect.yMin, BaseRoundButtonLeft.width * textureScalar, buttonHeight);
            var buttonRightEdgeRect = new Rect(buttonRect.xMax - (BaseRoundButtonRight.width * textureScalar), buttonRect.yMin, BaseRoundButtonRight.width * textureScalar, buttonHeight);
            var buttonMainRect = new Rect(buttonLeftEdgeRect.xMax - (buffer / 2), buttonRect.y, buttonRect.width - buttonLeftEdgeRect.width - buttonRightEdgeRect.width + buffer, buttonHeight);

            GUI.DrawTexture(buttonLeftEdgeRect, buttonRect.Contains(e.mousePosition) ? StateRoundButtonLeft.hoverTexture : StateRoundButtonLeft.inactiveTexture);
            GUI.DrawTexture(buttonMainRect, buttonRect.Contains(e.mousePosition) ? StateButtonMain.hoverTexture : StateButtonMain.inactiveTexture);
            GUI.DrawTexture(buttonRightEdgeRect, buttonRect.Contains(e.mousePosition) ? StateRoundButtonRight.hoverTexture : StateRoundButtonRight.inactiveTexture);

            EditorGUIUtility.SetIconSize(Vector2.one * buttonStyle.fontSize);

            return GUI.Button(buttonRect, buttonContent, buttonStyle);
        }

        protected bool DrawRoundedButton(Rect buttonRect, GUIContent buttonContent, int fontSize = 12, PoppinsStyle fontOverride = PoppinsStyle.SemiBold)
        {
            Event e = Event.current;
            GUIStyle buttonStyle;
            if (BodyLabelStyle != null)
            {
                buttonStyle = new GUIStyle(BodyLabelStyle) { fontSize = fontSize, alignment = TextAnchor.MiddleCenter, normal = SetStyleState(EditorGUIUtility.isProSkin ? Color.white : Color.black), hover = SetStyleState(EditorGUIUtility.isProSkin ? Color.white : Color.black), font = GetPoppinsFont(fontOverride) };
            }
            else
            {
                buttonStyle = new GUIStyle() { fontSize = fontSize, alignment = TextAnchor.MiddleCenter, normal = SetStyleState(EditorGUIUtility.isProSkin ? Color.white : Color.black), hover = SetStyleState(EditorGUIUtility.isProSkin ? Color.white : Color.black), font = GetPoppinsFont(fontOverride) };
            }
            var buttonHeight = buttonRect.height;

            var textureScalar = buttonHeight / BaseButtonMain.height;
            var buffer = 2f;
            var buttonLeftEdgeRect = new Rect(buttonRect.xMin, buttonRect.yMin, BaseRoundButtonLeft.width * textureScalar, buttonHeight);
            var buttonRightEdgeRect = new Rect(buttonRect.xMax - (BaseRoundButtonRight.width * textureScalar), buttonRect.yMin, BaseRoundButtonRight.width * textureScalar, buttonHeight);
            var buttonMainRect = new Rect(buttonLeftEdgeRect.xMax - (buffer / 2), buttonRect.y, buttonRect.width - buttonLeftEdgeRect.width - buttonRightEdgeRect.width + buffer, buttonHeight);

            GUI.DrawTexture(buttonLeftEdgeRect, buttonRect.Contains(e.mousePosition) ? StateRoundButtonLeft.hoverTexture : StateRoundButtonLeft.inactiveTexture);
            GUI.DrawTexture(buttonMainRect, buttonRect.Contains(e.mousePosition) ? StateButtonMain.hoverTexture : StateButtonMain.inactiveTexture);
            GUI.DrawTexture(buttonRightEdgeRect, buttonRect.Contains(e.mousePosition) ? StateRoundButtonRight.hoverTexture : StateRoundButtonRight.inactiveTexture);

            EditorGUIUtility.SetIconSize(Vector2.one * fontSize);

            return GUI.Button(buttonRect, buttonContent, buttonStyle);
        }

        protected bool DrawRoundedButton(Rect buttonRect, GUIContent buttonContent, bool active, int fontSize = 12, PoppinsStyle fontOverride = PoppinsStyle.SemiBold)
        {
            Event e = Event.current;
            GUIStyle buttonStyle = new GUIStyle(BodyLabelStyle)
            {
                fontSize = fontSize,
                alignment = TextAnchor.MiddleCenter,
                normal = SetStyleState(EditorGUIUtility.isProSkin ? active ? Color.black : Color.white : active ? Color.white : Color.black),
                hover = SetStyleState(EditorGUIUtility.isProSkin ? active ? Color.black : Color.white : active ? Color.white : Color.black),
                active = SetStyleState(EditorGUIUtility.isProSkin ? active ? Color.black : Color.white : active ? Color.white : Color.black),
                font = GetPoppinsFont(fontOverride)
            };

            var buttonHeight = buttonRect.height;

            var textureScalar = buttonHeight / BaseButtonMain.height;
            var buffer = 2f;
            var buttonLeftEdgeRect = new Rect(buttonRect.xMin, buttonRect.yMin, BaseRoundButtonLeft.width * textureScalar, buttonHeight);
            var buttonRightEdgeRect = new Rect(buttonRect.xMax - (BaseRoundButtonRight.width * textureScalar), buttonRect.yMin, BaseRoundButtonRight.width * textureScalar, buttonHeight);
            var buttonMainRect = new Rect(buttonLeftEdgeRect.xMax - (buffer / 2), buttonRect.y, buttonRect.width - buttonLeftEdgeRect.width - buttonRightEdgeRect.width + buffer, buttonHeight);

            GUI.DrawTexture(buttonLeftEdgeRect, buttonRect.Contains(e.mousePosition) ? active ? StateRoundButtonLeft.activeTexture : StateRoundButtonLeft.hoverTexture : active ? StateRoundButtonLeft.activeTexture : StateRoundButtonLeft.inactiveTexture);
            GUI.DrawTexture(buttonMainRect, buttonRect.Contains(e.mousePosition) ? active ? StateButtonMain.activeTexture : StateButtonMain.hoverTexture : active ? StateButtonMain.activeTexture : StateButtonMain.inactiveTexture);
            GUI.DrawTexture(buttonRightEdgeRect, buttonRect.Contains(e.mousePosition) ? active ? StateRoundButtonRight.activeTexture : StateRoundButtonRight.hoverTexture : active ? StateRoundButtonRight.activeTexture : StateRoundButtonRight.inactiveTexture);

            EditorGUIUtility.SetIconSize(Vector2.one * fontSize);

            return GUI.Button(buttonRect, buttonContent, buttonStyle);
        }

        protected bool DrawToggle(Rect toggleRect, bool active)
        {
            //TODO: Make it custom

            return GUI.Toggle(toggleRect, active, GUIContent.none);
        }

        protected string DrawSquaredInputField(Rect inputFieldRect, string writtenPrompt, ref bool changed, string regexLimitor = "")
        {
            var inputFieldStyle = new GUIStyle(InputFieldStyle)
            {
                margin = UniformRectOffset(0),
                contentOffset = Vector2.zero,
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12
            };

            var edgeWidth = inputFieldRect.height * ((float)BaseInputFieldSquareLeft.width / (float)BaseInputFieldSquareLeft.height);
            var buffer = 1f;
            var inputFieldLeftEdgeRect = new Rect(inputFieldRect.xMin, inputFieldRect.yMin, edgeWidth, inputFieldRect.height);
            var inputFieldRightEdgeRect = new Rect(inputFieldRect.xMax - edgeWidth, inputFieldRect.yMin, edgeWidth, inputFieldRect.height);
            var inputFieldMainRect = new Rect(inputFieldLeftEdgeRect.xMax - (buffer / 2), inputFieldRect.y, inputFieldRect.width - inputFieldLeftEdgeRect.width - inputFieldRightEdgeRect.width + buffer, inputFieldRect.height);

            GUI.DrawTexture(inputFieldLeftEdgeRect, BaseInputFieldSquareLeft);
            GUI.DrawTexture(inputFieldMainRect, BaseInputFieldSquareMain);
            GUI.DrawTexture(inputFieldRightEdgeRect, BaseInputFieldSquareRight);
            GUI.SetNextControlName("TextInput");

            int id = GUIUtility.GetControlID(FocusType.Passive) + 1;
            writtenPrompt = GUI.TextField(inputFieldMainRect, writtenPrompt, inputFieldStyle);
            changed = InputFocusManager.CheckOnEndChanges(id, writtenPrompt, Event.current);

            writtenPrompt = Regex.Replace(writtenPrompt, regexLimitor, "");

            return writtenPrompt;
        }

        protected string DrawRoundedInputField(Rect inputFieldRect, string writtenPrompt, ref bool changed, string regexLimitor = "")
        {
            var inputFieldStyle = new GUIStyle(InputFieldStyle)
            {
                margin = UniformRectOffset(0),
                contentOffset = Vector2.zero,
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12
            };

            var edgeWidth = inputFieldRect.height * ((float)BaseInputFieldRoundLeft.width / (float)BaseInputFieldRoundLeft.height);
            var buffer = 1f;
            var inputFieldLeftEdgeRect = new Rect(inputFieldRect.xMin, inputFieldRect.yMin, edgeWidth, inputFieldRect.height);
            var inputFieldRightEdgeRect = new Rect(inputFieldRect.xMax - edgeWidth, inputFieldRect.yMin, edgeWidth, inputFieldRect.height);
            var inputFieldMainRect = new Rect(inputFieldLeftEdgeRect.xMax - (buffer / 2), inputFieldRect.y, inputFieldRect.width - inputFieldLeftEdgeRect.width - inputFieldRightEdgeRect.width + buffer, inputFieldRect.height);

            GUI.DrawTexture(inputFieldLeftEdgeRect, BaseInputFieldRoundLeft);
            GUI.DrawTexture(inputFieldMainRect, BaseInputFieldRoundMain);
            GUI.DrawTexture(inputFieldRightEdgeRect, BaseInputFieldRoundRight);
            GUI.SetNextControlName("TextInput");

            int id = GUIUtility.GetControlID(FocusType.Passive) + 1;
            writtenPrompt = GUI.TextField(inputFieldMainRect, writtenPrompt, inputFieldStyle);
            changed = InputFocusManager.CheckOnEndChanges(id, writtenPrompt, Event.current);

            if(!string.IsNullOrEmpty(writtenPrompt)) writtenPrompt = Regex.Replace(writtenPrompt, regexLimitor, "");

            return writtenPrompt;
        }

        protected void DrawRoundedLabel(string labelContent, float fontSizeModifier = 1f, float heightPadding = 0f, float widthModifier = 1f, bool centre = false, PoppinsStyle fontOverride = PoppinsStyle.Regular)
        {
            GUIContent content = new GUIContent(labelContent);
            GUIStyle labelStyle = new GUIStyle(BodyLabelStyle) { fontSize = (int)(BodyLabelStyle.fontSize * fontSizeModifier), alignment = TextAnchor.MiddleCenter, normal = SetStyleState(EditorGUIUtility.isProSkin ? Color.white : Color.black), font = GetPoppinsFont(fontOverride) };

            var labelHeight = labelStyle.CalcSize(content).y + heightPadding;
            var labelRect = GUILayoutUtility.GetRect(labelStyle.CalcSize(content).x + 16, labelHeight);
            var initialWidth = labelRect.width;
            var initialPosition = labelRect.position;
            labelRect.width *= widthModifier;
            if (centre) { labelRect.x = initialPosition.x + (initialWidth - labelRect.width) / 2; }

            var textureScalar = labelHeight / BaseButtonMain.height;
            var labelLeftEdgeRect = new Rect(labelRect.xMin, labelRect.yMin, BaseRoundButtonLeft.width * textureScalar, labelHeight);
            var labelRightEdgeRect = new Rect(labelRect.xMax - (BaseInputFieldRoundRight.width * textureScalar), labelRect.yMin, BaseRoundButtonRight.width * textureScalar, labelHeight);
            var labelMainRect = new Rect(labelLeftEdgeRect.xMax, labelRect.y, labelRect.width - labelLeftEdgeRect.width - labelRightEdgeRect.width, labelHeight);

            GUI.DrawTexture(labelLeftEdgeRect, StateRoundButtonLeft.inactiveTexture);
            GUI.DrawTexture(labelMainRect, StateButtonMain.inactiveTexture);
            GUI.DrawTexture(labelRightEdgeRect, StateRoundButtonRight.inactiveTexture);
            GUI.Label(labelRect, labelContent, labelStyle);
        }

        protected void DrawRoundedLabel(Rect labelRect, GUIContent labelContent, int fontSize = 12, PoppinsStyle fontOverride = PoppinsStyle.SemiBold)
        {
            GUIStyle labelStyle = new GUIStyle(BodyLabelStyle) { fontSize = fontSize, alignment = TextAnchor.MiddleCenter, normal = SetStyleState(EditorGUIUtility.isProSkin ? Color.white : Color.black), hover = SetStyleState(EditorGUIUtility.isProSkin ? Color.white : Color.black), font = GetPoppinsFont(fontOverride) };

            var labelHeight = labelRect.height;

            var textureScalar = labelHeight / BaseLabelBackdropMiddle.height;
            var buffer = 2f;
            var labelLeftEdgeRect = new Rect(labelRect.xMin, labelRect.yMin, BaseLabelBackdropLeft.width * textureScalar, labelHeight);
            var labelRightEdgeRect = new Rect(labelRect.xMax - (BaseLabelBackdropRight.width * textureScalar), labelRect.yMin, BaseLabelBackdropRight.width * textureScalar, labelHeight);
            var labelMainRect = new Rect(labelLeftEdgeRect.xMax - (buffer / 2), labelRect.y, labelRect.width - labelLeftEdgeRect.width - labelRightEdgeRect.width + buffer, labelHeight);

            GUI.DrawTexture(labelLeftEdgeRect, BaseLabelBackdropLeft);
            GUI.DrawTexture(labelMainRect, BaseLabelBackdropMiddle);
            GUI.DrawTexture(labelRightEdgeRect, BaseLabelBackdropRight);

            EditorGUIUtility.SetIconSize(Vector2.one * fontSize);

            GUI.Label(labelMainRect, labelContent, labelStyle);
        }

        protected bool DrawAutoSizeRoundedButton(Vector2 buttonStartingPosition, GUIContent buttonContent, float buttonHeight, int fontSize = 12, PoppinsStyle fontOverride = PoppinsStyle.SemiBold, TextAnchor startingPosAnchor = TextAnchor.UpperLeft)
        {
            Event e = Event.current;
            GUIStyle buttonStyle = new GUIStyle(BodyLabelStyle) { fontSize = fontSize, alignment = TextAnchor.MiddleCenter, normal = SetStyleState(EditorGUIUtility.isProSkin ? Color.white : Color.black), hover = SetStyleState(EditorGUIUtility.isProSkin ? Color.white : Color.black), font = GetPoppinsFont(fontOverride) };
            var buttonWidth = buttonStyle.CalcSize(buttonContent).x;

            var textureScalar = buttonHeight / BaseButtonMain.height;

            Rect buttonLeftEdgeRect;
            Rect buttonMainRect;
            Rect buttonRightEdgeRect;

            float yPos = ((int)startingPosAnchor / 3) switch
            {
                2 => buttonStartingPosition.y - buttonHeight,
                1 => buttonStartingPosition.y - (buttonHeight / 2),
                _ => buttonStartingPosition.y
            };

            var buffer = 2f;
            switch (startingPosAnchor)
            {
                case TextAnchor.UpperLeft:
                case TextAnchor.MiddleLeft:
                case TextAnchor.LowerLeft:
                default:
                    buttonLeftEdgeRect = new Rect(buttonStartingPosition.x, yPos, BaseRoundButtonLeft.width * textureScalar, buttonHeight);
                    buttonMainRect = new Rect(buttonLeftEdgeRect.xMax - (buffer / 2), yPos, buttonWidth + buffer, buttonHeight);
                    buttonRightEdgeRect = new Rect(buttonMainRect.xMax, yPos, BaseRoundButtonRight.width * textureScalar, buttonHeight);
                    break;
                case TextAnchor.UpperCenter:
                case TextAnchor.MiddleCenter:
                case TextAnchor.LowerCenter:
                    buttonMainRect = new Rect(buttonStartingPosition.x - (buttonWidth / 2) - (buffer / 2), yPos, buttonWidth + buffer, buttonHeight);
                    buttonLeftEdgeRect = new Rect(buttonMainRect.xMin - BaseRoundButtonLeft.width * textureScalar, yPos, BaseRoundButtonLeft.width * textureScalar, buttonHeight);
                    buttonRightEdgeRect = new Rect(buttonMainRect.xMax, yPos, BaseRoundButtonRight.width * textureScalar, buttonHeight);
                    break;
                case TextAnchor.UpperRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.LowerRight:
                    buttonRightEdgeRect = new Rect(buttonStartingPosition.x, yPos, BaseRoundButtonRight.width * textureScalar, buttonHeight);
                    buttonMainRect = new Rect(buttonRightEdgeRect.xMin - buttonWidth - (buffer / 2), yPos, buttonWidth + buffer, buttonHeight);
                    buttonLeftEdgeRect = new Rect(buttonMainRect.xMin - BaseRoundButtonLeft.width * textureScalar, yPos, BaseRoundButtonLeft.width * textureScalar, buttonHeight);
                    break;
            }

            var buttonRect = new Rect(buttonLeftEdgeRect.xMin, buttonLeftEdgeRect.yMin, buttonLeftEdgeRect.width + buttonMainRect.width + buttonRightEdgeRect.width, buttonLeftEdgeRect.height);

            GUI.DrawTexture(buttonLeftEdgeRect, buttonRect.Contains(e.mousePosition) ? StateRoundButtonLeft.hoverTexture : StateRoundButtonLeft.inactiveTexture);
            GUI.DrawTexture(buttonMainRect, buttonRect.Contains(e.mousePosition) ? StateButtonMain.hoverTexture : StateButtonMain.inactiveTexture);
            GUI.DrawTexture(buttonRightEdgeRect, buttonRect.Contains(e.mousePosition) ? StateRoundButtonRight.hoverTexture : StateRoundButtonRight.inactiveTexture);

            EditorGUIUtility.SetIconSize(Vector2.one * fontSize);

            return GUI.Button(buttonRect, buttonContent, buttonStyle);
        }

        protected void DrawAutoSizeRoundedLabel(Vector2 labelStartingPosition, GUIContent labelContent, float labelHeight, int fontSize = 12, PoppinsStyle fontOverride = PoppinsStyle.SemiBold, TextAnchor startingPosAnchor = TextAnchor.UpperLeft)
        {
            GUIStyle labelStyle = new GUIStyle(BodyLabelStyle) { fontSize = fontSize, alignment = TextAnchor.MiddleCenter, normal = SetStyleState(EditorGUIUtility.isProSkin ? Color.white : Color.black), hover = SetStyleState(EditorGUIUtility.isProSkin ? Color.white : Color.black), font = GetPoppinsFont(fontOverride), wordWrap = false };
            var labelWidth = labelStyle.CalcSize(labelContent).x;

            var textureScalar = labelHeight / BaseLabelBackdropMiddle.height;

            Rect labelLeftEdgeRect;
            Rect labelMainRect;
            Rect labelRightEdgeRect;

            float yPos = ((int)startingPosAnchor / 3) switch
            {
                2 => labelStartingPosition.y - labelHeight,
                1 => labelStartingPosition.y - (labelHeight / 2),
                _ => labelStartingPosition.y
            };

            switch (startingPosAnchor) {
                case TextAnchor.UpperLeft:
                case TextAnchor.MiddleLeft:
                case TextAnchor.LowerLeft:
                default:
                    labelLeftEdgeRect = new Rect(labelStartingPosition.x, yPos, BaseLabelBackdropLeft.width * textureScalar, labelHeight);
                    labelMainRect = new Rect(labelLeftEdgeRect.xMax, yPos, labelWidth, labelHeight);
                    labelRightEdgeRect = new Rect(labelMainRect.xMax, yPos, BaseLabelBackdropRight.width * textureScalar, labelHeight);
                    break;
                case TextAnchor.UpperCenter:
                case TextAnchor.MiddleCenter:
                case TextAnchor.LowerCenter:
                    labelMainRect = new Rect(labelStartingPosition.x - (labelWidth / 2), yPos, labelWidth, labelHeight);
                    labelLeftEdgeRect = new Rect(labelMainRect.xMin - BaseLabelBackdropLeft.width * textureScalar, yPos, BaseLabelBackdropLeft.width * textureScalar, labelHeight);
                    labelRightEdgeRect = new Rect(labelMainRect.xMax, yPos, BaseLabelBackdropRight.width * textureScalar, labelHeight);
                    break;
                case TextAnchor.UpperRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.LowerRight:
                    labelRightEdgeRect = new Rect(labelStartingPosition.x, yPos, BaseLabelBackdropRight.width * textureScalar, labelHeight);
                    labelMainRect = new Rect(labelRightEdgeRect.xMin - labelWidth, yPos, labelWidth, labelHeight);
                    labelLeftEdgeRect = new Rect(labelMainRect.xMin - BaseLabelBackdropLeft.width * textureScalar, yPos, BaseLabelBackdropLeft.width * textureScalar, labelHeight);
                    break;
            }


            GUI.DrawTexture(labelLeftEdgeRect, BaseLabelBackdropLeft);
            GUI.DrawTexture(labelMainRect, BaseLabelBackdropMiddle);
            GUI.DrawTexture(labelRightEdgeRect, BaseLabelBackdropRight);

            EditorGUIUtility.SetIconSize(Vector2.one * fontSize);

            GUI.Label(labelMainRect, labelContent, labelStyle);
        }

        protected void DrawHyperlinkLabelWithPreText(Rect position, string preTextLabel, string hyperlinkLabel, string link, GUIStyle overwriteNormalStyle = null, GUIStyle overwriteHyperlinkStyle = null, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            GUIStyle hyperlinkStyle, normalStyle;

            if (overwriteNormalStyle == null) normalStyle = BodyLabelStyle;
            else normalStyle = overwriteNormalStyle;

            if (overwriteHyperlinkStyle == null)
            {
                hyperlinkStyle = new GUIStyle(normalStyle)
                {
                    normal = SetStyleState(EditorGUIUtility.isProSkin ? HexToColour("7588FF") : HexToColour("0E2EFF")),
                    focused = SetStyleState(EditorGUIUtility.isProSkin ? HexToColour("B987E8") : HexToColour("551A8B")),
                    hover = SetStyleState(EditorGUIUtility.isProSkin ? HexToColour("B987E8") : HexToColour("551A8B")),
                    active = SetStyleState(EditorGUIUtility.isProSkin ? HexToColour("B987E8") : HexToColour("551A8B"))
                };
            }
            else hyperlinkStyle = overwriteHyperlinkStyle;

            normalStyle.alignment = alignment;
            hyperlinkStyle.alignment = alignment;

            var preTextLabelWidth = normalStyle.CalcSize(new GUIContent(preTextLabel)).x;
            var hyperlinkLabelWidth = hyperlinkStyle.CalcSize(new GUIContent(hyperlinkLabel)).x;

            Rect preTextLabelRect;
            Rect hyperlinkLabelRect;

            switch (alignment)
            {
                case TextAnchor.MiddleRight:
                case TextAnchor.UpperRight:
                case TextAnchor.LowerRight:
                    hyperlinkLabelRect = new Rect(position.xMax - hyperlinkLabelWidth, position.y, hyperlinkLabelWidth, position.height);
                    preTextLabelRect = new Rect(hyperlinkLabelRect.xMax - preTextLabelWidth, position.y, preTextLabelWidth, position.height);
                    break;
                case TextAnchor.MiddleCenter:
                case TextAnchor.UpperCenter:
                case TextAnchor.LowerCenter:
                    var totalWidth = preTextLabelWidth + hyperlinkLabelWidth;

                    preTextLabelRect = new Rect(position.center.x - (totalWidth / 2), position.y, preTextLabelWidth, position.height);
                    hyperlinkLabelRect = new Rect(preTextLabelRect.xMax, position.y, hyperlinkLabelWidth, position.height);
                    break;
                case TextAnchor.MiddleLeft:
                case TextAnchor.UpperLeft:
                case TextAnchor.LowerLeft:
                default:
                    preTextLabelRect = new Rect(position.x, position.y, preTextLabelWidth, position.height);
                    hyperlinkLabelRect = new Rect(preTextLabelRect.xMax, position.y, hyperlinkLabelWidth, position.height);
                    break;
            }

            GUI.Label(preTextLabelRect, preTextLabel, normalStyle);
            if (GUI.Button(hyperlinkLabelRect, hyperlinkLabel, hyperlinkStyle))
            {
                Application.OpenURL(link);
            }
        }
        protected void DrawHyperlinkLabelWithPostText(Rect position, string hyperlinkLabel, string postTextLabel, string link, GUIStyle overwriteNormalStyle = null, GUIStyle overwriteHyperlinkStyle = null, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            GUIStyle hyperlinkStyle, normalStyle;

            if (overwriteNormalStyle == null) normalStyle = BodyLabelStyle;
            else normalStyle = overwriteNormalStyle;

            if (overwriteHyperlinkStyle == null)
            {
                hyperlinkStyle = new GUIStyle(normalStyle)
                {
                    normal = SetStyleState(EditorGUIUtility.isProSkin ? HexToColour("7588FF") : HexToColour("0E2EFF")),
                    focused = SetStyleState(EditorGUIUtility.isProSkin ? HexToColour("B987E8") : HexToColour("551A8B")),
                    hover = SetStyleState(EditorGUIUtility.isProSkin ? HexToColour("B987E8") : HexToColour("551A8B")),
                    active = SetStyleState(EditorGUIUtility.isProSkin ? HexToColour("B987E8") : HexToColour("551A8B"))
                };
            }
            else hyperlinkStyle = overwriteHyperlinkStyle;

            normalStyle.alignment = alignment;
            hyperlinkStyle.alignment = alignment;

            var postTextLabelWidth = normalStyle.CalcSize(new GUIContent(postTextLabel)).x;
            var hyperlinkLabelWidth = hyperlinkStyle.CalcSize(new GUIContent(hyperlinkLabel)).x;

            Rect postTextLabelRect;
            Rect hyperlinkLabelRect;

            switch (alignment)
            {
                case TextAnchor.MiddleRight:
                case TextAnchor.UpperRight:
                case TextAnchor.LowerRight:
                    postTextLabelRect = new Rect(position.xMax - postTextLabelWidth, position.y, postTextLabelWidth, position.height);
                    hyperlinkLabelRect = new Rect(postTextLabelRect.xMax - hyperlinkLabelWidth, position.y, hyperlinkLabelWidth, position.height);
                    break;
                case TextAnchor.MiddleCenter:
                case TextAnchor.UpperCenter:
                case TextAnchor.LowerCenter:
                    var totalWidth = postTextLabelWidth + hyperlinkLabelWidth;

                    hyperlinkLabelRect = new Rect(position.center.x - (totalWidth / 2), position.y, hyperlinkLabelWidth, position.height);
                    postTextLabelRect = new Rect(hyperlinkLabelRect.xMax, position.y, postTextLabelWidth, position.height);
                    break;
                case TextAnchor.MiddleLeft:
                case TextAnchor.UpperLeft:
                case TextAnchor.LowerLeft:
                default:
                    hyperlinkLabelRect = new Rect(position.x, position.y, hyperlinkLabelWidth, position.height);
                    postTextLabelRect = new Rect(hyperlinkLabelRect.xMax, position.y, postTextLabelWidth, position.height);
                    break;
            }

            if (GUI.Button(hyperlinkLabelRect, hyperlinkLabel, hyperlinkStyle))
            {
                Application.OpenURL(link);
            }
            GUI.Label(postTextLabelRect, postTextLabel, normalStyle);
        }
        protected void DrawHyperlinkLabelWithPreAndPostText(Rect position, string preTextLabel, string hyperlinkLabel, string postTextLabel, string link, GUIStyle overwriteNormalStyle = null, GUIStyle overwriteHyperlinkStyle = null, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            GUIStyle hyperlinkStyle, normalStyle;

            if (overwriteNormalStyle == null) normalStyle = BodyLabelStyle;
            else normalStyle = overwriteNormalStyle;

            if (overwriteHyperlinkStyle == null)
            {
                hyperlinkStyle = new GUIStyle(normalStyle)
                {
                    normal = SetStyleState(EditorGUIUtility.isProSkin ? HexToColour("7588FF") : HexToColour("0E2EFF")),
                    focused = SetStyleState(EditorGUIUtility.isProSkin ? HexToColour("B987E8") : HexToColour("551A8B")),
                    hover = SetStyleState(EditorGUIUtility.isProSkin ? HexToColour("B987E8") : HexToColour("551A8B")),
                    active = SetStyleState(EditorGUIUtility.isProSkin ? HexToColour("B987E8") : HexToColour("551A8B"))
                };
            }
            else hyperlinkStyle = overwriteHyperlinkStyle;

            normalStyle.alignment = alignment;
            hyperlinkStyle.alignment = alignment;

            var preTextLabelWidth = normalStyle.CalcSize(new GUIContent(preTextLabel)).x;
            var hyperlinkLabelWidth = hyperlinkStyle.CalcSize(new GUIContent(hyperlinkLabel)).x;
            var postTextLabelWidth = normalStyle.CalcSize(new GUIContent(postTextLabel)).x;

            Rect preTextLabelRect;
            Rect hyperlinkLabelRect;
            Rect postTextLabelRect;

            switch (alignment)
            {
                case TextAnchor.MiddleRight:
                case TextAnchor.UpperRight:
                case TextAnchor.LowerRight:
                    postTextLabelRect = new Rect(position.xMax - postTextLabelWidth, position.y, postTextLabelWidth, position.height);
                    hyperlinkLabelRect = new Rect(postTextLabelRect.xMax - hyperlinkLabelWidth, position.y, hyperlinkLabelWidth, position.height);
                    preTextLabelRect = new Rect(hyperlinkLabelRect.xMax - preTextLabelWidth, position.y, preTextLabelWidth, position.height);
                    break;
                case TextAnchor.MiddleCenter:
                case TextAnchor.UpperCenter:
                case TextAnchor.LowerCenter:
                    var totalWidth = preTextLabelWidth + hyperlinkLabelWidth + postTextLabelWidth;

                    preTextLabelRect = new Rect(position.center.x - (totalWidth / 2), position.y, preTextLabelWidth, position.height);
                    hyperlinkLabelRect = new Rect(preTextLabelRect.xMax, position.y, hyperlinkLabelWidth, position.height);
                    postTextLabelRect = new Rect(hyperlinkLabelRect.xMax, position.y, postTextLabelWidth, position.height);
                    break;
                case TextAnchor.MiddleLeft:
                case TextAnchor.UpperLeft:
                case TextAnchor.LowerLeft:
                default:
                    preTextLabelRect = new Rect(position.x, position.y, preTextLabelWidth, position.height);
                    hyperlinkLabelRect = new Rect(preTextLabelRect.xMax, position.y, hyperlinkLabelWidth, position.height);
                    postTextLabelRect = new Rect(hyperlinkLabelRect.xMax, position.y, postTextLabelWidth, position.height);
                    break;
            }

            GUI.Label(preTextLabelRect, preTextLabel, normalStyle);
            if (GUI.Button(hyperlinkLabelRect, hyperlinkLabel, hyperlinkStyle))
            {
                Application.OpenURL(link);
            }
            GUI.Label(postTextLabelRect, postTextLabel, normalStyle);
        }

        protected Rect DrawSquareInSquare(Vector2 centre, float width, float height, float marginSize)
        {
            var rect = new Rect(centre.x - (width / 2), centre.y - (height / 2), width, height);
            EditorGUI.DrawRect(new Rect(rect.xMin - marginSize, rect.yMin - marginSize, rect.width + (2 * marginSize), rect.height + (2 * marginSize)), HexToColour("575859"));
            EditorGUI.DrawRect(rect, HexToColour("373839"));

            return rect;
        }

        #region Custom Fields

        protected void DrawBehaviourField(ref BehaviourTree behaviourTree, GUIContent label, Rect position)
        {
            var labelWidth = position.width * fieldLabelWidthPercentage;
            var labelRect = new Rect(position.x + fieldPadding, position.y, labelWidth, position.height);
            GUI.Label(labelRect, label, FieldLabelStyle);

            var fieldWidth = position.xMax - labelRect.xMax - fieldPadding;
            var fieldRect = new Rect(labelRect.xMax, position.y, fieldWidth, position.height);

            //FB: Unresolved ExitGUIException that is a bug from unity gotta be caught here.
            try
            {
                behaviourTree = EditorGUI.ObjectField(fieldRect, behaviourTree, typeof(BehaviourTree), true) as BehaviourTree;
            }
            catch (UnityEngine.ExitGUIException)
            {
                //We suppress this because unity hasn't fixed this bug after 10 years.
                //throw;
                GUILayout.EndHorizontal();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }

        protected bool CustomBoolField(ref bool boolean, GUIContent label, Rect position, string activeText = "On", string inactiveText = "Off")
        {
            var labelWidth = position.width * fieldLabelWidthPercentage;
            var labelRect = new Rect(position.x + fieldPadding, position.y, labelWidth, position.height);
            GUI.Label(labelRect, label, FieldLabelStyle);

            var fieldWidth = position.xMax - labelRect.xMax - fieldPadding;
            var fieldRect = new Rect(labelRect.xMax, position.y, fieldWidth, position.height);

            var buttonWidth = (fieldWidth / 2) - (fieldPadding / 2);
            var activeButtonRect = new Rect(fieldRect.x, fieldRect.y, buttonWidth, fieldRect.height);
            var inactiveButtonRect = new Rect(activeButtonRect.xMax + fieldPadding, fieldRect.y, buttonWidth, fieldRect.height);

            bool buttonPressed = false;

            if (DrawRoundedButton(activeButtonRect, new GUIContent(activeText), boolean))
            {
                boolean = true;
                buttonPressed = true;
            }
            if (DrawRoundedButton(inactiveButtonRect, new GUIContent(inactiveText), !boolean))
            {
                boolean = false;
                buttonPressed = true;
            }

            return buttonPressed;
        }
        /// <summary>
        /// Custom bool field that is a toggle button
        /// </summary>
        protected bool CustomBoolField(ref bool boolean1, ref bool boolean2, GUIContent label, Rect position,
                               string mode1Text = "Mode1 On", string mode2Text = "Mode2 On")
        {
            var labelWidth = position.width * fieldLabelWidthPercentage;
            var labelRect = new Rect(position.x + fieldPadding, position.y, labelWidth, position.height);
            GUI.Label(labelRect, label, FieldLabelStyle);

            var fieldWidth = position.xMax - labelRect.xMax - fieldPadding;
            var fieldRect = new Rect(labelRect.xMax, position.y, fieldWidth, position.height);

            var buttonWidth = (fieldWidth / 2) - (fieldPadding / 2);
            var mode1ButtonRect = new Rect(fieldRect.x, fieldRect.y, buttonWidth, fieldRect.height);
            var mode2ButtonRect = new Rect(mode1ButtonRect.xMax + fieldPadding, fieldRect.y, buttonWidth, fieldRect.height);

            bool buttonPressed = false;

            // If mode1 button is pressed, set boolean1 to true and boolean2 to false.
            if (DrawRoundedButton(mode1ButtonRect, new GUIContent(mode1Text), boolean1 || !boolean2))
            {
                boolean1 = true;
                boolean2 = false;
                buttonPressed = true;
            }
            // If mode2 button is pressed, set boolean1 to false and boolean2 to true.
            if (DrawRoundedButton(mode2ButtonRect, new GUIContent(mode2Text), !boolean1 || boolean2))
            {
                boolean1 = false;
                boolean2 = true;
                buttonPressed = true;
            }

            return buttonPressed;
        }

        //create a custom bool field that is a toggle button
        protected bool CustomSimpleBoolField(bool boolean, string label, Rect position, int fontSize = 12)
        {
            var labelContent = new GUIContent(label);
            var labelStyle = new GUIStyle(BodyLabelStyle) { wordWrap = true, fontSize = fontSize };

            var fieldSize = GetToggleSize();
            var labelWidth = position.width - fieldSize - fieldPadding;
            var labelHeight = labelStyle.CalcHeight(labelContent, labelWidth);
            var labelRect = new Rect(position.x, position.y, labelWidth, labelHeight);
            GUI.Label(labelRect, label, labelStyle);

            var fieldRect = new Rect(labelRect.xMax + fieldPadding, position.y, fieldSize, fieldSize);
            GUI.DrawTexture(fieldRect, BaseToggleBackdrop);

            if (GUI.Button(fieldRect, new GUIContent(boolean ? BaseConfirmIcon : BaseDenyIcon), new GUIStyle() { padding = UniformRectOffset(Mathf.RoundToInt(fieldSize * 0.15f)) }))
            {
                boolean = !boolean;
            }

            return boolean;
        }

        protected float GetToggleSize() => 20f;

        protected void CustomTransformField(ref Transform transform, GUIContent label, Rect position)
        {
            var labelWidth = position.width * fieldLabelWidthPercentage;
            var labelRect = new Rect(position.x + fieldPadding, position.y, labelWidth, position.height);
            GUI.Label(labelRect, label, FieldLabelStyle);

            var fieldWidth = position.xMax - labelRect.xMax - fieldPadding;
            var fieldRect = new Rect(labelRect.xMax, position.y, fieldWidth, position.height);

            transform = EditorGUI.ObjectField(fieldRect, transform, typeof(Transform), true) as Transform;
        }

        protected void CustomTransformField(ref Transform transform, string label, ref bool fieldEnabled)
        {
            GUILayout.BeginHorizontal();
            fieldEnabled = EditorGUILayout.Toggle(fieldEnabled, GUILayout.Width(20));
            GUI.enabled = fieldEnabled;
            transform = EditorGUILayout.ObjectField(label, transform, typeof(Transform), true) as Transform;
            GUILayout.EndHorizontal();
            GUI.enabled = true;
        }

        protected bool CustomVectorField(ref Vector3 vector, ref string tempX, ref string tempY, ref string tempZ, GUIContent label, Rect position)
        {
            var labelWidth = position.width * fieldLabelWidthPercentage;
            var labelRect = new Rect(position.x + fieldPadding, position.y, labelWidth, position.height);
            GUI.Label(labelRect, label, FieldLabelStyle);

            var fieldWidth = position.xMax - labelRect.xMax - fieldPadding;
            var fieldRect = new Rect(labelRect.xMax, position.y, fieldWidth, position.height);

            var newFieldPadding = fieldPadding / 4f;
            var valueWidth = (fieldWidth / 3) - (2 * newFieldPadding / 3);
            var xValueRect = new Rect(fieldRect.x, fieldRect.y, valueWidth, fieldRect.height);
            var yValueRect = new Rect(xValueRect.xMax + newFieldPadding, fieldRect.y, valueWidth, fieldRect.height);
            var zValueRect = new Rect(yValueRect.xMax + newFieldPadding, fieldRect.y, valueWidth, fieldRect.height);

            var valuePadding = newFieldPadding / 4f;
            var valueLabelWidth = valueWidth * fieldLabelWidthPercentage / 2f;
            var valueFieldWidth = valueWidth - valueLabelWidth - valuePadding;

            var xValueLabelRect = new Rect(xValueRect.x, xValueRect.y, valueLabelWidth, xValueRect.height);
            var xValueFieldRect = new Rect(xValueLabelRect.xMax, xValueRect.y, valueFieldWidth, xValueRect.height);

            var yValueLabelRect = new Rect(yValueRect.x, yValueRect.y, valueLabelWidth, yValueRect.height);
            var yValueFieldRect = new Rect(yValueLabelRect.xMax, yValueRect.y, valueFieldWidth, yValueRect.height);

            var zValueLabelRect = new Rect(zValueRect.x, zValueRect.y, valueLabelWidth, zValueRect.height);
            var zValueFieldRect = new Rect(zValueLabelRect.xMax, zValueRect.y, valueFieldWidth, zValueRect.height);

            GUI.Label(xValueLabelRect, "X", FieldLabelStyle);
            GUI.Label(yValueLabelRect, "Y", FieldLabelStyle);
            GUI.Label(zValueLabelRect, "Z", FieldLabelStyle);

            bool changedX = false;
            bool changedY = false;
            bool changedZ = false;
            tempX = DrawSquaredInputField(xValueFieldRect, tempX, ref changedX, @"[^0-9.-]");
            tempY = DrawSquaredInputField(yValueFieldRect, tempY, ref changedY, @"[^0-9.-]");
            tempZ = DrawSquaredInputField(zValueFieldRect, tempZ, ref changedZ, @"[^0-9.-]");

            float xValue;
            float.TryParse(tempX, out xValue);
            float yValue;
            float.TryParse(tempY, out yValue);
            float zValue;
            float.TryParse(tempZ, out zValue);

            vector = new Vector3(xValue, yValue, zValue);

            return changedX || changedY || changedZ;
        }

        protected bool CustomFloatField(ref float value, ref string tempValue, GUIContent label, Rect position)
        {
            var labelWidth = position.width * fieldLabelWidthPercentage;
            var labelRect = new Rect(position.x + fieldPadding, position.y, labelWidth, position.height);
            GUI.Label(labelRect, label, FieldLabelStyle);

            var fieldWidth = position.xMax - labelRect.xMax - fieldPadding;
            var fieldRect = new Rect(labelRect.xMax, position.y, fieldWidth, position.height);

            bool changed = false;
            tempValue = DrawSquaredInputField(fieldRect, tempValue, ref changed, @"[^0-9.-]");
            float.TryParse(tempValue, out value);

            return changed;
        }

        protected bool CustomStringField(ref string value, GUIContent label, Rect position)
        {
            var labelWidth = position.width * fieldLabelWidthPercentage;
            var labelRect = new Rect(position.x + fieldPadding, position.y, labelWidth, position.height);
            GUI.Label(labelRect, label, FieldLabelStyle);

            var fieldWidth = position.xMax - labelRect.xMax - fieldPadding;
            var fieldRect = new Rect(labelRect.xMax, position.y, fieldWidth, position.height);

            bool changed = false;
            value = DrawSquaredInputField(fieldRect, value, ref changed);

            return changed;
        }

        protected bool CustomIntField(ref int value, ref string tempValue, GUIContent label, Rect position)
        {
            var labelWidth = position.width * fieldLabelWidthPercentage;
            var labelRect = new Rect(position.x + fieldPadding, position.y, labelWidth, position.height);
            GUI.Label(labelRect, label, FieldLabelStyle);

            var fieldWidth = position.xMax - labelRect.xMax - fieldPadding;
            var fieldRect = new Rect(labelRect.xMax, position.y, fieldWidth, position.height);

            bool changed = false;
            tempValue = DrawSquaredInputField(fieldRect, tempValue, ref changed, @"[^0-9.]");
            if (changed)
                int.TryParse(tempValue, out value);

            return changed;
        }
        #endregion Custom Fields
        protected Vector2 CalcLabelSize(GUIContent labelContent, float labelRectHeight, int fontSize = 12, PoppinsStyle fontOverride = PoppinsStyle.SemiBold)
        {
            GUIStyle labelStyle = new GUIStyle(BodyLabelStyle) { fontSize = fontSize, alignment = TextAnchor.MiddleCenter, normal = SetStyleState(EditorGUIUtility.isProSkin ? Color.white : Color.black), hover = SetStyleState(EditorGUIUtility.isProSkin ? Color.white : Color.black), font = GetPoppinsFont(fontOverride), wordWrap = false };

            var textureScalar = labelRectHeight / BaseLabelBackdropMiddle.height;
            var labelLeftEdgeWidth = BaseLabelBackdropLeft.width * textureScalar;
            var labelRightEdgeWidth = BaseLabelBackdropRight.width * textureScalar;
            var labelMainWidth = labelStyle.CalcSize(labelContent).x;

            return new Vector2(labelLeftEdgeWidth + labelMainWidth + labelRightEdgeWidth, labelRectHeight);
        }

        protected Vector2[] CalcAutoSizeLabelPositions(string[] labelData, out float totalHeight, float padding = 10f, float offset = 10f, float rowHeight = 20f, int fontSize = 12, PoppinsStyle fontOverride = PoppinsStyle.SemiBold)
        {
            float currentLabelRowWidth = 0f;
            int currentLabelRow = 0;
            totalHeight = rowHeight;

            Vector2[] labelStartingPositions = new Vector2[labelData.Length];
            for (int i = 0; i < labelStartingPositions.Length; i++)
            {
                labelStartingPositions[i] = new Vector2(currentLabelRowWidth, ((rowHeight + padding) * currentLabelRow));

                var labelSize = CalcLabelSize(new GUIContent(labelData[i]), rowHeight, fontSize, fontOverride);

                if (currentLabelRowWidth + labelSize.x >= position.width - (padding * 2) - offset)
                {
                    currentLabelRow++;
                    currentLabelRowWidth = 0f;
                    totalHeight += rowHeight + padding;
                    labelStartingPositions[i] = new Vector2(currentLabelRowWidth, ((rowHeight + padding) * currentLabelRow));
                }
                currentLabelRowWidth += labelSize.x + padding;
            }
            return labelStartingPositions;
        }

        protected static RectOffset UniformRectOffset(int offset)
        {
            return new RectOffset(offset, offset, offset, offset);
        }

        protected static void CloseWindowIfOpen<T>() where T : EditorWindow
        {
            if (HasOpenInstances<T>())
            {
                GetWindow<T>().Close();
            }
        }

        protected static Font GetPoppinsFont(PoppinsStyle style)
        {
            Font chosenFont;
            switch (style)
            {
                case PoppinsStyle.Thin:
                    chosenFont = POPPINS_THIN;
                    break;
                case PoppinsStyle.ExtraLight:
                    chosenFont = POPPINS_EXTRA_LIGHT;
                    break;
                case PoppinsStyle.Light:
                    chosenFont = POPPINS_LIGHT;
                    break;
                case PoppinsStyle.Regular:
                    chosenFont = POPPINS_REGULAR;
                    break;
                case PoppinsStyle.Medium:
                    chosenFont = POPPINS_MEDIUM;
                    break;
                case PoppinsStyle.SemiBold:
                    chosenFont = POPPINS_SEMI_BOLD;
                    break;
                case PoppinsStyle.Bold:
                    chosenFont = POPPINS_BOLD;
                    break;
                case PoppinsStyle.ExtraBold:
                    chosenFont = POPPINS_EXTRA_BOLD;
                    break;
                case PoppinsStyle.Black:
                    chosenFont = POPPINS_BLACK;
                    break;
                default:
                    chosenFont = POPPINS_REGULAR;
                    break;
            }
            return chosenFont;
        }

        protected GUIStyleState SetStyleState(Texture2D background)
        {
            return new GUIStyleState() { background = background };
        }

        protected GUIStyleState SetStyleState(Color textColour)
        {
            return new GUIStyleState() { textColor = textColour };
        }

        protected GUIStyleState SetStyleState(Texture2D background, Color textColour)
        {
            return new GUIStyleState() { background = background, textColor = textColour };
        }

        protected void DrawUILine(Color color, Vector2 position, float width, int thickness = 1)
        {
            var rect = new Rect(position.x, position.y, width, thickness);
            EditorGUI.DrawRect(rect, color);
        }

        protected Color HexToColour(string hexCode)
        {
            hexCode = hexCode.ToUpper();

            if (hexCode.Length != 3 && hexCode.Length != 6)
            {
                return Color.black;
            }
            else
            {
                string r;
                string g;
                string b;

                int hexCodePrecision = hexCode.Length / 3;
                r = hexCode.Substring(0 * hexCodePrecision, hexCodePrecision);
                g = hexCode.Substring(1 * hexCodePrecision, hexCodePrecision);
                b = hexCode.Substring(2 * hexCodePrecision, hexCodePrecision);


                Color rgbColor = new Color(
                    int.Parse(r, System.Globalization.NumberStyles.HexNumber) / 255f,
                    int.Parse(g, System.Globalization.NumberStyles.HexNumber) / 255f,
                    int.Parse(b, System.Globalization.NumberStyles.HexNumber) / 255f);
                return rgbColor;
            }
        }
        protected Texture2D TintTextureToEditorTheme(Texture2D texture) => TintTexture(texture, EditorGUIUtility.isProSkin ? Color.white : Color.black);
        protected Texture2D TintTextureToEditorTheme(Texture2D texture, Color darkThemeTint, Color lightThemeTint) => TintTexture(texture, EditorGUIUtility.isProSkin ? darkThemeTint : lightThemeTint);
        protected Texture2D TintTexture(Texture2D untintedTexture, Color tintColour)
        {
            
            if (untintedTexture == null)
            {
                // Create a fallback 1x1 texture with a default color (white).
                untintedTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                untintedTexture.SetPixel(0, 0, Color.white);
                untintedTexture.Apply();
            }

            Color32[] untintedPixels = untintedTexture.GetPixels32();
            Color32[] tintedPixels = new Color32[untintedPixels.Length];

            for (int i = 0; i < untintedPixels.Length; i++)
            {
                tintedPixels[i] = untintedPixels[i] * tintColour;
            }

            // Create a new texture with the same dimensions and format as the input (or fallback) texture.
            Texture2D tintedTexture = new Texture2D(untintedTexture.width, untintedTexture.height, untintedTexture.format, false);
            tintedTexture.SetPixels32(tintedPixels);
            tintedTexture.Apply();

            return tintedTexture;
        }
        protected Texture2D TintTextureWhite(Texture2D untintedTexture, Color tintColour)
        {
            Color32[] untintedPixels = untintedTexture.GetPixels32();
            Color32[] tintedPixels = new Color32[untintedPixels.Length];

            Array.Copy(untintedPixels, tintedPixels, untintedPixels.Length);

            for (int i = 0; i < tintedPixels.Length; i++)
            {
                if (untintedPixels[i] == Color.white)
                {
                    tintedPixels[i] = untintedPixels[i] * tintColour;
                }
            }

            Texture2D tintedTexture = Instantiate(untintedTexture);
            tintedTexture.SetPixels32(tintedPixels);
            tintedTexture.Apply();
            return tintedTexture;
        }
        protected Texture2D TintGradient(Texture2D untintedGradient, Color tintColourA, Color tintColourB)
        {
            Color32[] untintedPixels = untintedGradient.GetPixels32();
            Color32[] tintedPixels = new Color32[untintedPixels.Length];

            Array.Copy(untintedPixels, tintedPixels, untintedPixels.Length);

            for (int i = 0; i < tintedPixels.Length; i++)
            {
                var maxRGB = Mathf.Max(untintedPixels[i].r, untintedPixels[i].g, untintedPixels[i].b) / 255f;
                var minRGB = Mathf.Min(untintedPixels[i].r, untintedPixels[i].g, untintedPixels[i].b) / 255f;
                tintedPixels[i] = Color.Lerp(tintColourA, tintColourB, (maxRGB + minRGB) / 2);
                tintedPixels[i].a = untintedPixels[i].a;
            }

            Texture2D tintedTexture = Instantiate(untintedGradient);
            tintedTexture.SetPixels32(tintedPixels);
            tintedTexture.Apply();
            return tintedTexture;
        }

        public static void DisplayAWDialog(string title, string message)
        {
            CloseWindowIfOpen<AnythingDialog>();
            AnythingDialog.OpenWindow(title, message, EditorGUIUtility.GetMainWindowPosition());
        }
        public static void DisplayAWDialog(string title, string message, string ok, string cancel, System.Action action)
        {
            CloseWindowIfOpen<AnythingDialog>();
            AnythingDialog.OpenWindow(title, message, ok, cancel, action, EditorGUIUtility.GetMainWindowPosition());
        }
        public static void DisplayAWDialog(string title, string message, string ok, string cancel, string alt, System.Action okAction, System.Action altAction)
        {
            CloseWindowIfOpen<AnythingDialog>();
            AnythingDialog.OpenWindow(title, message, ok, cancel, alt, okAction, altAction, EditorGUIUtility.GetMainWindowPosition());
        }
        #endregion Helper Functions
        #endregion Functions
    }

    public class AnythingDialog : AnythingEditor
    {
        protected static Rect windowPosition;
        protected static Rect callingWindowScreenPosition;
        protected static bool resetWindowPosition = true;

        protected static bool windowOpen = false;
        protected static string windowTitle;
        protected static Vector2 windowSize;

        protected static GUIContent messageContent;

        protected static GUIContent okButtonContent;
        protected static GUIContent cancelButtonContent;
        protected static GUIContent altButtonContent;
        protected static System.Action okAction;
        protected static System.Action altAction;
        protected static bool hasButtons;
        protected static bool hasComplexButtons;

        private static GUIStyle messageStyle;
        private static float messageWidth;
        private static float messageHeight;

        private static GUIStyle buttonStyle;
        private static float okButtonWidth;
        private static float cancelButtonWidth;
        private static float altButtonWidth;
        private static float buttonHeight = 30f;

        private static float margin = 20f;
        private static float padding = 10f;
        private static float logoSize = 72f;
        private static float maxWindowSize = 700f;

        public static void OpenWindow(string title, string message, Rect callingWindow)
        {
            callingWindowScreenPosition = GUIUtility.GUIToScreenRect(callingWindow);
            windowTitle = title;
            messageContent = new GUIContent(message);

            okButtonContent = GUIContent.none;
            cancelButtonContent = GUIContent.none;
            altButtonContent = GUIContent.none;

            hasComplexButtons = hasButtons = false;

            ShowWindow();
        }

        public static void OpenWindow(string title, string message, string ok, string cancel, System.Action action, Rect callingWindow)
        {
            callingWindowScreenPosition = GUIUtility.GUIToScreenRect(callingWindow);
            windowTitle = title;
            messageContent = new GUIContent(message);

            okAction = action;

            okButtonContent = new GUIContent(ok);
            cancelButtonContent = new GUIContent(cancel);
            altButtonContent = GUIContent.none;
            hasButtons = true;
            hasComplexButtons = false;

            ShowWindow();
        }

        public static void OpenWindow(string title, string message, string ok, string cancel, string alt, System.Action action, System.Action alternativeAction, Rect callingWindow)
        {
            callingWindowScreenPosition = GUIUtility.GUIToScreenRect(callingWindow);
            windowTitle = title;
            messageContent = new GUIContent(message);

            okAction = action;
            altAction = alternativeAction;

            okButtonContent = new GUIContent(ok);
            cancelButtonContent = new GUIContent(cancel);
            altButtonContent = new GUIContent(alt);
            hasComplexButtons = hasButtons = true;

            ShowWindow();
        }

        protected static void ShowWindow()
        {
            var window = GetWindow<AnythingDialog>(true);
            window.titleContent = new GUIContent(windowTitle);

            messageStyle = new GUIStyle() { normal = new GUIStyleState() { textColor = Color.white }, hover = new GUIStyleState() { textColor = Color.white }, font = GetPoppinsFont(PoppinsStyle.Medium), fontSize = 14, alignment = TextAnchor.MiddleLeft, wordWrap = true };
            messageWidth = Mathf.Min(messageStyle.CalcSize(messageContent).x, maxWindowSize - (logoSize + (margin * 2) + padding));
            messageHeight = Mathf.Max(logoSize, messageStyle.CalcHeight(messageContent, messageWidth));

            buttonStyle = new GUIStyle() { normal = new GUIStyleState() { textColor = Color.white }, hover = new GUIStyleState() { textColor = Color.white }, fontSize = 16, font = GetPoppinsFont(PoppinsStyle.SemiBold), alignment = TextAnchor.MiddleCenter, wordWrap = false };
            okButtonWidth = buttonStyle.CalcSize(okButtonContent).x;
            okButtonWidth += padding * 2;
            cancelButtonWidth = buttonStyle.CalcSize(cancelButtonContent).x;
            cancelButtonWidth += padding * 2;
            altButtonWidth = buttonStyle.CalcSize(altButtonContent).x;
            altButtonWidth += padding * 2;

            var windowWidth = Mathf.Max(messageWidth, hasButtons ? (okButtonWidth + padding + cancelButtonWidth + (hasComplexButtons ? padding + altButtonWidth : 0f)) : 0f) + logoSize + (margin * 2) + padding;
            var windowHeight = (margin * 2) + Mathf.Max(logoSize, messageHeight) + (hasButtons ? (buttonHeight + padding) : 0f);
            windowSize = new Vector2(windowWidth, windowHeight);

            window.minSize = window.maxSize = windowSize;

            if (resetWindowPosition)
            {
                resetWindowPosition = false;
                windowPosition = GUIUtility.ScreenToGUIRect(new Rect(callingWindowScreenPosition.x + ((callingWindowScreenPosition.width - window.minSize.x) / 2), callingWindowScreenPosition.y + ((callingWindowScreenPosition.height - window.minSize.y) / 2), 0, 0));
            }
            else
            {
                windowPosition = window.position;
            }
            //If failed to find width give default subvalue;
            if (windowPosition.size == Vector2.zero) windowPosition.size = windowSize;
            window.position = windowPosition;
            windowOpen = true;
        }

        protected new void OnGUI()
        {
            base.OnGUI();

            if (!windowOpen) Close();

            var logoRect = new Rect(margin, margin, logoSize, logoSize);
            var messageContentRect = new Rect(logoRect.xMax + padding, logoRect.y, messageWidth, messageHeight);

            GUI.DrawTexture(logoRect, BaseAnythingGlobeLogoFilled);
            GUI.Label(messageContentRect, messageContent, messageStyle);

            if (hasButtons)
            {
                var altRect = new Rect(messageContentRect.x, messageContentRect.yMax + padding, altButtonWidth, buttonHeight);
                var cancelRect = new Rect(hasComplexButtons ? altRect.xMax + padding : messageContentRect.x, messageContentRect.yMax + padding, cancelButtonWidth, buttonHeight);
                var okRect = new Rect(cancelRect.xMax + padding, messageContentRect.yMax + padding, okButtonWidth, buttonHeight);

                if (DrawRoundedButton(cancelRect, cancelButtonContent, buttonStyle)) Close();
                if (DrawRoundedButton(okRect, okButtonContent, buttonStyle))
                {
                    okAction.Invoke();
                    Close();
                }
                if(hasComplexButtons)
                {
                    if (DrawRoundedButton(altRect, altButtonContent, buttonStyle))
                    {
                        altAction.Invoke();
                        Close();
                    }
                }
            }
        }

        protected void OnDestroy()
        {
            resetWindowPosition = true;
        }
    }
}
