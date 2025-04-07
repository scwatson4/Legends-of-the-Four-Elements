#define UAS
using AnythingWorld.Networking.Editor;
using Cysharp.Threading.Tasks;
using System;
using UnityEditor;
using UnityEngine;

namespace AnythingWorld.Editor
{
    public class LogInEditor : AnythingCreatorEditor
    {
        #region Fields

        private enum LoginTabCategory
        {
            LOGIN,
            SIGNUP
        }

        private LoginTabCategory loginTabCategory;

        private string loginEmailField = "";
        private string loginPasswordField = "";
        private string loginAPIKeyField = "";

        private bool showPassword = false;
        private bool showAPIKey = true;
        private bool successfulLogin = false;
        private string loginErrorMessage;



        #region Textures

        #region Base Textures
        private static Texture2D baseBanner;
        protected static Texture2D BaseBanner
        {
            get
            {
                if (baseBanner == null)
                {
                    baseBanner = Resources.Load("Editor/SettingsPanel/banner") as Texture2D;
                }
                return baseBanner;
            }
        }
        private static Texture2D baseLoginTabIcon;
        protected static Texture2D BaseLoginTabIcon
        {
            get
            {
                if (baseLoginTabIcon == null)
                {
                    baseLoginTabIcon = Resources.Load("Editor/SettingsPanel/Icons/loginTab") as Texture2D;
                }
                return baseLoginTabIcon;
            }
        }
        private static Texture2D baseSignupTabIcon;
        protected static Texture2D BaseSignupTabIcon
        {
            get
            {
                if (baseSignupTabIcon == null)
                {
                    baseSignupTabIcon = Resources.Load("Editor/SettingsPanel/Icons/signupTab") as Texture2D;
                }

                return baseSignupTabIcon;
            }
        }
        private static Texture2D baseLoginLabelIcon;
        protected static Texture2D BaseLoginLabelIcon
        {
            get
            {
                if (baseLoginLabelIcon == null)
                {
                    baseLoginLabelIcon = Resources.Load("Editor/SettingsPanel/loginLabel") as Texture2D;
                }
                return baseLoginLabelIcon;
            }
        }
        private static Texture2D baseSignupLabelIcon;
        protected static Texture2D BaseSignupLabelIcon
        {
            get
            {
                if (baseSignupLabelIcon == null)
                {
                    baseSignupLabelIcon = Resources.Load("Editor/SettingsPanel/signupLabel") as Texture2D;
                }
                return baseSignupLabelIcon;
            }
        }
        private static Texture2D baseHidePasswordIcon;
        protected static Texture2D BaseHidePasswordIcon
        {
            get
            {
                if (baseHidePasswordIcon == null)
                {
                    baseHidePasswordIcon = Resources.Load("Editor/SettingsPanel/Icons/hidePassword") as Texture2D;
                }
                return baseHidePasswordIcon;
            }
        }
        private static Texture2D baseShowPasswordIcon;
        protected static Texture2D BaseShowPasswordIcon
        {
            get
            {
                if (baseShowPasswordIcon == null)
                {
                    baseShowPasswordIcon = Resources.Load("Editor/SettingsPanel/Icons/showPassword") as Texture2D;
                }

                return baseShowPasswordIcon;
            }
        }
        #endregion Base Textures

        #region Tinted Textures
        private Texture2D tintedLoginLabelIcon;
        protected Texture2D TintedLoginLabelIcon
        {
            get
            {
                if (tintedLoginLabelIcon == null)
                {
                    tintedLoginLabelIcon = TintTextureToEditorTheme(BaseLoginLabelIcon, Color.white, Color.black);
                }

                return tintedLoginLabelIcon;
            }
            set => tintedLoginLabelIcon = value;
        }
        private Texture2D tintedSignupLabelIcon;
        protected Texture2D TintedSignupLabelIcon
        {
            get
            {
                if (tintedSignupLabelIcon == null)
                {
                    tintedSignupLabelIcon = TintTextureToEditorTheme(BaseSignupLabelIcon, Color.white, Color.black);
                }

                return tintedSignupLabelIcon;
            }
            set => tintedSignupLabelIcon = value;
        }
        #endregion Tinted Textures

        #region State Textures
        private StateTexture2D stateLoginTabIcon;
        protected StateTexture2D StateLoginTabIcon
        {
            get
            {
                if (stateLoginTabIcon == null || !stateLoginTabIcon.TexturesLoadedNoHover)
                {
                    stateLoginTabIcon = new StateTexture2D(
                        TintTextureToEditorTheme(BaseLoginTabIcon, Color.black, Color.white),
                        TintTextureToEditorTheme(BaseLoginTabIcon, Color.white, Color.black));
                }
                return stateLoginTabIcon;
            }
            set => stateLoginTabIcon = value;
        }
        private StateTexture2D stateSignupTabIcon;
        protected StateTexture2D StateSignupTabIcon
        {
            get
            {
                if (stateSignupTabIcon == null || !stateSignupTabIcon.TexturesLoadedNoHover)
                {
                    stateSignupTabIcon = new StateTexture2D(
                        TintTextureToEditorTheme(BaseSignupTabIcon, Color.black, Color.white),
                        TintTextureToEditorTheme(BaseSignupTabIcon, Color.white, Color.black));
                }
                return stateSignupTabIcon;
            }
            set => stateSignupTabIcon = value;
        }
        private StateTexture2D statePasswordVisibilityIcon;
        protected StateTexture2D StatePasswordVisibilityIcon
        {
            get
            {
                if (statePasswordVisibilityIcon == null || statePasswordVisibilityIcon.TexturesLoadedNoHover)
                {
                    statePasswordVisibilityIcon = new StateTexture2D(
                        TintTextureToEditorTheme(BaseShowPasswordIcon),
                        TintTextureToEditorTheme(BaseHidePasswordIcon));
                }
                return statePasswordVisibilityIcon;
            }
            set => statePasswordVisibilityIcon = value;
        }
        #endregion State Textures
        #endregion Textures
        protected GUIStyle ConfirmButtonStyle;
        #endregion Fields

        [MenuItem("Tools/Anything World/Log In \u2044 Log Out", false, 2)]
        internal static void Initialize()
        {
            AnythingEditor tabWindow;
            Vector2 windowSize;

            if (AnythingSettings.HasAPIKey)
            {
                DisplayAWDialog("Log Out", "Are you sure you would like to log out?", "Yes, Log Me Out", "Cancel", () =>
                {
                    var settingsSerializedObject = new SerializedObject(AnythingSettings.Instance);
                    settingsSerializedObject.FindProperty("apiKey").stringValue = "";
                    settingsSerializedObject.FindProperty("email").stringValue = "";
                    settingsSerializedObject.ApplyModifiedProperties();

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    Undo.RecordObject(AnythingSettings.Instance, "Logged out");
                    EditorUtility.SetDirty(AnythingSettings.Instance);

                    while (HasOpenInstances<AnythingCreatorEditor>())
                    {
                        var window = GetWindow(typeof(AnythingCreatorEditor));
                        window.Close();
                    }
                });
            }
            else
            {
                windowSize = new Vector2(475, 700);
                tabWindow = GetWindow<LogInEditor>("Login", false);
                tabWindow.position = new Rect(EditorGUIUtility.GetMainWindowPosition().center - windowSize / 2, windowSize);
                tabWindow.Show();
                tabWindow.Focus();
            }
        }

        protected new void Awake()
        {
            base.Awake();
        }

        protected new void OnGUI()
        {
            InitializeResources();

            try
            {
                DrawLogin();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void DrawLogin()
        {
            if (!successfulLogin && Event.current.Equals(Event.KeyboardEvent("return")))
            {
                SubmitDetails();
            }

            var bannerHeight = BaseBanner.height * (position.width / BaseBanner.width);
            var bannerRect = GUILayoutUtility.GetRect(position.width, bannerHeight);
            var marginSize = 0.1f;

            GUI.DrawTexture(bannerRect, BaseBanner);

            DrawAutoSizeRoundedLabel(new Vector2(bannerRect.xMax - 20f, bannerRect.yMin + 16f), new GUIContent(AnythingSettings.PackageVersion), 20f, 14, PoppinsStyle.Medium, TextAnchor.UpperRight);

            if (successfulLogin)
            {
                GUILayout.FlexibleSpace();
                var signedInText = new GUIContent($"You've successfully {(loginTabCategory == LoginTabCategory.LOGIN ? "logged in" : "signed up")}!");
                var textHeight = HeaderLabelStyle.CalcHeight(signedInText, position.width);

                var labelRect = GUILayoutUtility.GetRect(position.width, textHeight);
                GUI.Label(labelRect, signedInText, new GUIStyle(HeaderLabelStyle) { wordWrap = true });
                GUILayout.FlexibleSpace();
            }
            else
            {
                GUILayout.FlexibleSpace();
                DrawLoginDetails(marginSize);
                GUILayout.Space(20f);
                var logInButtonRect = GUILayoutUtility.GetRect(position.width, 30f);
                logInButtonRect.width = position.width - (position.width * (marginSize * 2));
                logInButtonRect.x = position.width * marginSize;

                if (DrawRoundedButton(logInButtonRect, new GUIContent("Login")))
                {
                    SubmitDetails();
                }
                GUILayout.Space(20f);

                var hyperlinkRect = GUILayoutUtility.GetRect(position.width, 30f);
                var normalStyle = new GUIStyle(BodyLabelStyle)
                {
                    fontSize = 12,
                    font = GetPoppinsFont(PoppinsStyle.Medium),
                    normal = SetStyleState(HexToColour("FFFFFF")),
                    hover = SetStyleState(HexToColour("FFFFFF"))
                };
                var hyperlinkStyle = new GUIStyle(normalStyle)
                {
                    font = GetPoppinsFont(PoppinsStyle.Bold),
                    focused = SetStyleState(HexToColour("B987E8")),
                    hover = SetStyleState(HexToColour("B987E8")),
                    active = SetStyleState(HexToColour("B987E8"))
                };

                DrawHyperlinkLabelWithPreText(hyperlinkRect, "Don't have an account? ", "Sign up here.", "https://app.anything.world/register", normalStyle, hyperlinkStyle, TextAnchor.MiddleCenter);

                GUILayout.FlexibleSpace();
                if (!string.IsNullOrEmpty(loginErrorMessage))
                {
                    var errorRect = GUILayoutUtility.GetRect(new GUIContent(loginErrorMessage), BodyLabelStyle);
                    GUI.Label(errorRect, loginErrorMessage, new GUIStyle(BodyLabelStyle) { alignment = TextAnchor.MiddleCenter, normal = SetStyleState(Color.red) });
                }
            }
        }

        private void SubmitDetails()
        {
            if (string.IsNullOrEmpty(loginAPIKeyField)) SignupLoginProcessor.LogIn(loginEmailField, loginPasswordField, SignupLoginErrorHandler, LoginSuccessHandler, this);
            else SignupLoginProcessor.CheckAPIKeyValidity(loginAPIKeyField, SignupLoginErrorHandler, LoginSuccessHandler, this);
        }

        private void DrawLoginDetails(float marginSize)
        {
            Event e = Event.current;

            DrawHeaderLabel("Login with Email");
            GUILayout.Space(5f);
            DrawInputField("E-MAIL", e, ref loginEmailField, marginSize);
            GUILayout.Space(10f);
            DrawPasswordField("PASSWORD", e, ref loginPasswordField, ref showPassword, marginSize);

            GUILayout.Space(20f);

            DrawHeaderLabel("or API Key");
            GUILayout.Space(5f);
            DrawPasswordField("API KEY", e, ref loginAPIKeyField, ref showAPIKey, marginSize);
        }

        private void DrawHeaderLabel(string label)
        {
            var labelContent = new GUIContent(label);

            var labelStyle = new GUIStyle(HeaderLabelStyle)
            {
                normal = SetStyleState(EditorGUIUtility.isProSkin ? Color.white : Color.black),
                hover = SetStyleState(EditorGUIUtility.isProSkin ? Color.white : Color.black),
                fontSize = 20,
                font = GetPoppinsFont(PoppinsStyle.SemiBold),
                wordWrap = true,                   // Enable word wrapping
                clipping = TextClipping.Overflow   // Prevent clipping if text overflows
            };

            // Calculate the height needed for the text given the available width
            float labelHeight = labelStyle.CalcHeight(labelContent, position.width);

            // Get a rect that spans the entire available width and the calculated height
            var headerRect = GUILayoutUtility.GetRect(position.width, labelHeight);

            // Draw the label within the rect
            GUI.Label(headerRect, labelContent, labelStyle);
        }

        private bool DrawInputField(string title, Event e, ref string fieldToSet, float marginSize = 0.1f)
        {
            GUI.SetNextControlName(title);

            var titleStyle = new GUIStyle() { normal = SetStyleState(HexToColour("979797")), hover = SetStyleState(HexToColour("979797")), fontSize = 10, font = GetPoppinsFont(PoppinsStyle.Regular) };
            var titleHeight = titleStyle.CalcSize(new GUIContent(title)).y;

            var titleRect = GUILayoutUtility.GetRect(position.width, titleHeight, titleStyle, GUILayout.Height(titleHeight));
            titleRect.width = position.width - (position.width * (marginSize * 2));
            titleRect.x = position.width * marginSize;
            GUI.Label(titleRect, new GUIContent(title), titleStyle);

            GUILayout.Space(5f);

            var fieldRect = GUILayoutUtility.GetRect(position.width, 30);
            fieldRect.width = position.width - (position.width * (marginSize * 2));
            fieldRect.x = position.width * marginSize;

            bool changed = false;
            fieldToSet = DrawRoundedInputField(fieldRect, fieldToSet, ref changed);

            return changed;
        }

        private void DrawPasswordField(string title, Event e, ref string fieldToSet, ref bool showPassword, float marginSize = 0.1f)
        {
            GUI.SetNextControlName(title);

            var titleStyle = new GUIStyle() { normal = SetStyleState(HexToColour("979797")), hover = SetStyleState(HexToColour("979797")), fontSize = 10, font = GetPoppinsFont(PoppinsStyle.Regular) };
            var titleHeight = titleStyle.CalcSize(new GUIContent(title)).y;

            var titleRect = GUILayoutUtility.GetRect(position.width, titleHeight, titleStyle, GUILayout.Height(titleHeight));
            titleRect.width = position.width - (position.width * (marginSize * 2));
            titleRect.x = position.width * marginSize;
            GUI.Label(titleRect, new GUIContent(title), titleStyle);

            GUILayout.Space(5f);

            var fieldRect = GUILayoutUtility.GetRect(position.width, 30);
            fieldRect.width = position.width - (position.width * (marginSize * 2));
            fieldRect.x = position.width * marginSize;

            var edgeWidth = fieldRect.height * ((float)BaseInputFieldRoundLeft.width / (float)BaseInputFieldRoundLeft.height);
            var buffer = 1f;
            var inputFieldLeftEdgeRect = new Rect(fieldRect.xMin, fieldRect.yMin, edgeWidth, fieldRect.height);
            var inputFieldRightEdgeRect = new Rect(fieldRect.xMax - edgeWidth, fieldRect.yMin, edgeWidth, fieldRect.height);
            var inputFieldMainRect = new Rect(inputFieldLeftEdgeRect.xMax - (buffer / 2), fieldRect.y, fieldRect.width - inputFieldLeftEdgeRect.width - inputFieldRightEdgeRect.width + buffer, fieldRect.height);

            var passwordVisibilityMaxWidth = Mathf.Max(BaseShowPasswordIcon.width, BaseHidePasswordIcon.width);
            var passwordVisibilityWidth = showPassword ? BaseShowPasswordIcon.width : BaseHidePasswordIcon.width;
            var passwordVisibilityHeight = showPassword ? BaseShowPasswordIcon.height : BaseHidePasswordIcon.height;

            var iconPadding = (fieldRect.height - passwordVisibilityHeight) / 2f;
            var passwordVisibilityRect = new Rect(fieldRect.xMax - (passwordVisibilityMaxWidth / 2) - (passwordVisibilityWidth / 2) - iconPadding, fieldRect.y + iconPadding, passwordVisibilityWidth, passwordVisibilityHeight);

            GUI.enabled = !(e.isMouse && passwordVisibilityRect.Contains(e.mousePosition));
            GUI.DrawTexture(inputFieldLeftEdgeRect, BaseInputFieldRoundLeft);
            GUI.DrawTexture(inputFieldMainRect, BaseInputFieldRoundMain);
            GUI.DrawTexture(inputFieldRightEdgeRect, BaseInputFieldRoundRight);
            if (showPassword) fieldToSet = GUI.TextField(fieldRect, fieldToSet, InputFieldStyle);
            else fieldToSet = GUI.PasswordField(fieldRect, fieldToSet, '*', InputFieldStyle);
            GUI.enabled = true;

            if (GUI.Button(passwordVisibilityRect, "", new GUIStyle(IconStyle) { normal = SetStyleState(showPassword ? StatePasswordVisibilityIcon.activeTexture : StatePasswordVisibilityIcon.inactiveTexture) }))
            {
                showPassword = !showPassword;
            }
        }

        #region Helper Functions
        private void ResetDetails()
        {
            loginEmailField = "";
            loginPasswordField = "";
            loginAPIKeyField = "";

            loginErrorMessage = "";
            showPassword = false;
            showAPIKey = true;
        }

        private void SignupLoginErrorHandler(SignupLoginProcessor.SignupLoginError signupError)
        {
            loginErrorMessage = $"{signupError.message}";
            Repaint();
        }

        private void LoginSuccessHandler()
        {
            loginErrorMessage = "";
            if (AnythingSettings.IsUAS)
            {
#if UAS
                //UAS
                Utilities.Editor.VSAttribution.SendAttributionEvent("Login", "AnythingWorld", AnythingSettings.APIKey);
#endif
            }

            UniTask.Void(async () =>
            {
                PresentSuccess();
                await UniTask.Delay(TimeSpan.FromSeconds(1));
                RestartAnythingWorld();
            });
        }

        private void PresentSuccess()
        {
            successfulLogin = true;
            Repaint();
        }

        private void RestartAnythingWorld()
        {
            ModelBrowserEditor.Initialize(position);
            Close();
        }
        #endregion Helper Functions
    }
}
