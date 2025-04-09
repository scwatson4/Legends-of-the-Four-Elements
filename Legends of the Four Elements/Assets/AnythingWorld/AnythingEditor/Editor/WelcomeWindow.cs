using AnythingWorld;
using AnythingWorld.Editor;
using System;
using UnityEditor;
using UnityEngine;

namespace AnythingWorld.Editor
{
    /// <summary>
    /// Welcome window for the package Anything World for Unity
    /// </summary>
    [InitializeOnLoad] // This attribute initializes the static constructor on load
    public class WelcomeWindow : AnythingCreatorEditor
    {
        private const string ShowAtStartPrefKey = "ShowWelcomeWindowAtStart";
        private const int lenght = 400;
        private const int height = 680;
        private const int bannerHeight = 280;
        private Texture2D header;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;


        protected Texture2D Header
        {
            get
            {
                if (header == null)
                {
                    header = Resources.Load("Editor/Shared/frame") as Texture2D;
                }
                return header;
            }
        }
        private Texture2D clear;
        protected Texture2D Clear
        {
            get
            {
                if (clear == null)
                {
                    clear = Resources.Load("Editor/Shared/ClearAlpha") as Texture2D;
                }
                return clear;
            }
        }
        private Texture2D globe;
        protected Texture2D Globe
        {
            get
            {
                if (globe == null)
                {
                    globe = Resources.Load("Editor/Shared/AW_LOGO") as Texture2D;
                }
                return globe;
            }
        }
        private Texture2D gtBook;
        protected Texture2D GtBook
        {
            get
            {
                if (gtBook == null)
                {
                    gtBook = Resources.Load("Editor/Shared/GTBOOK") as Texture2D;
                }
                return gtBook;
            }
        }
        /// <summary>
        /// Static constructor that subscribes to the editor's update event
        /// </summary>
        static WelcomeWindow()
        {
            // Subscribe to the delayed call
            EditorApplication.delayCall += ShowWindowAtStartup;

        }
        /// <summary>
        /// Show the window at startup
        /// </summary>
        private static void ShowWindowAtStartup()
        {
            // Only show the window if the preference key does not exist
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            // Only show once per session
            if (!AnythingSettings.ShowWelcomeMessage)
            {
                AnythingSettings.ShowWelcomeMessage = true;
                // Only show the window if the preference key does not exist
                if (!EditorPrefs.HasKey(ShowAtStartPrefKey))
                {
                    Init();
                    // Set the preference key so the window doesn't show again automatically
                    EditorPrefs.SetBool(ShowAtStartPrefKey, true);
                }
                else
                {
                    // If the preference key exists, check if it's true or false
                    if (EditorPrefs.GetBool(ShowAtStartPrefKey))
                    {
                        Init();
                    }
                }
            }
        }
        /// <summary>
        /// Initialize the window
        /// </summary>
        [MenuItem("Tools/Anything World/Welcome")]
        public static void Init()
        {
            // Get existing open window or if none, make a new one:
            var window = (WelcomeWindow)EditorWindow.GetWindow(typeof(WelcomeWindow), true, "Welcome");
            window.minSize = new Vector2(lenght, height);
            window.maxSize = new Vector2(lenght, height);
            window.Show();
        }
        /// <summary>
        /// Awake is called when the script instance is being loaded.
        /// </summary>
        protected new void Awake()
        {
            base.Awake();
            base.DefineCustomStyles();
        }
        /// <summary>
        /// OnGUI is called for rendering and handling GUI events.
        /// </summary>
        new void OnGUI()
        {
            InitializeStyles();
            
            Rect bannerRect = new Rect(Vector2.zero, new Vector2(lenght, bannerHeight));
            GUI.DrawTexture(bannerRect, Header, ScaleMode.StretchToFill);
            Rect continued = EditorGUILayout.GetControlRect(false, bannerHeight);
            EditorGUILayout.Space();
            ShowVersion(continued);
            Rect globalRect = EditorGUILayout.GetControlRect(false, 90);// 90 is the space jumped by the globe
            globalRect.size = new Vector2(80, 80);// 80 is the size of the globe
            globalRect.x = lenght / 2 - 40;
            GUI.DrawTexture(globalRect, Globe);

            EditorGUILayout.LabelField("Welcome!", _headerStyle);

            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("Welcome to Anything World package for Unity!", _labelStyle);
            EditorGUILayout.LabelField("Browse our library of animated 3D models and start creating!", _labelStyle);
            EditorGUILayout.Space(20);

            globalRect = EditorGUILayout.GetControlRect(true, 0);// 10 is the space jumped by the text
            MakeButtonAndIcon(globalRect, "<b> Get started with our </b>", GtBook, 30, 18, "https://anything-world.gitbook.io/anything-world/");

            globalRect = EditorGUILayout.GetControlRect(true, 35);// 10 is the space jumped by the text
            MakeButton(globalRect, "<b>Unity Quick Start Guide</b>", "https://anything-world.gitbook.io/anything-world/");
            globalRect = EditorGUILayout.GetControlRect(true, 35);
            MakeButtonAndIcon(globalRect, "Ask questions on <b>Discord</b>", StateDiscordIcon.activeTexture, 25, 10, "https://discordapp.com/channels/765174190197309481/928979938663628850");
            globalRect = EditorGUILayout.GetControlRect(true, 35);
            MakeButtonAndIcon(globalRect, "Bug reporting on <b>Discord</b>", StateDiscordIcon.activeTexture, 25, 10, "https://discordapp.com/channels/765174190197309481/765175839553617940");

            globalRect = EditorGUILayout.GetControlRect(true, 30);
            globalRect.x = (lenght / 2) - lenght / 3 / 2;//center the button
            globalRect.width = lenght / 3;//size of the button
            if (!AnythingSettings.HasAPIKey)
            {
                if (DrawRoundedButton(globalRect, new GUIContent("Login")))
                {
                    var window = (LogInEditor)EditorWindow.GetWindow(typeof(LogInEditor), true, "Login");
                    window.Show();
                }
            }
            EditorGUILayout.Space(20);
            // Provide option to not show the window on startup in the future
            bool showOnStart = EditorPrefs.GetBool(ShowAtStartPrefKey, true);
            showOnStart = GUILayout.Toggle(showOnStart, "Show this window at startup");
            
            if (GUI.changed)
            {
                Repaint();
                EditorPrefs.SetBool(ShowAtStartPrefKey, showOnStart);
            }
        }

        private void InitializeStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    normal = new GUIStyleState { textColor = Color.white }
                };
            }

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    normal = new GUIStyleState { textColor = Color.white }
                };
            }
        }
        
        /// <summary>
        /// Show the version of the package
        /// </summary>
        private void ShowVersion(Rect bannerRect)
        {
            var versionContent = new GUIContent(AnythingSettings.PackageVersion);
            var versionStyle = new GUIStyle(EditorStyles.label) { font = GetPoppinsFont(PoppinsStyle.Medium), fontSize = 9, alignment = TextAnchor.MiddleRight, normal = SetStyleState(UnityEngine.Color.white), hover = SetStyleState(UnityEngine.Color.white) };
            var versionRectSize = versionStyle.CalcSize(versionContent);
            var versionRect = new Rect(bannerRect.xMax - versionRectSize.x * 2, versionRectSize.y, versionRectSize.x + versionRectSize.y, versionRectSize.y);
            var versionLeftEdgeRect = new Rect(versionRect.xMin, versionRect.yMin, versionRectSize.y / 2, versionRectSize.y);
            var versionRightEdgeRect = new Rect(versionRect.xMax - (versionRectSize.y / 2), versionRect.yMin, versionRectSize.y / 2, versionRectSize.y);
            var versionMainRect = new Rect(versionLeftEdgeRect.xMax, versionRect.y, versionRect.width - versionLeftEdgeRect.width - versionRightEdgeRect.width, versionRectSize.y);
            GUI.DrawTexture(versionLeftEdgeRect, BaseLabelBackdropLeft);
            GUI.DrawTexture(versionMainRect, BaseLabelBackdropMiddle);
            GUI.DrawTexture(versionRightEdgeRect, BaseLabelBackdropRight);
            GUI.Label(versionMainRect, versionContent, versionStyle);
        }

        /// <summary>
        /// Make a button with an icon
        /// </summary>
        void MakeButtonAndIcon(Rect rect, String richText, Texture2D texture, int iconSize, int distance, string url)
        {
            GUIContent gUI = new GUIContent()
            {
                image = null,
                text = richText,
                tooltip = null,
            };
            if (GUI.Button(rect, gUI, new GUIStyle()
            {
                name = "Button",
                richText = true,
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                //we need to set the background to a clear image to make the button transparent and the button state still work
                normal = new GUIStyleState { textColor = UnityEngine.Color.white, background = Clear },
                hover = new GUIStyleState { textColor = UnityEngine.Color.gray, background = Clear }
            })) System.Diagnostics.Process.Start(url);
            rect.x = (lenght / 2) - (richText.Length * 2.8f) - distance;//2.8f is the size of a character
            rect.size = new Vector2(iconSize, iconSize);
            GUI.DrawTexture(rect, texture);
        }
        /// <summary>
        /// Make a sigle button
        /// </summary>
        void MakeButton(Rect rect, String richText, string url)
        {
            if (GUI.Button(rect, richText, new GUIStyle()
            {
                name = "Button",
                richText = true,
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                normal = new GUIStyleState { textColor = UnityEngine.Color.white, background = Clear },
                hover = new GUIStyleState { textColor = UnityEngine.Color.gray, background = Clear }
            })) System.Diagnostics.Process.Start(url);
        }
    }
}