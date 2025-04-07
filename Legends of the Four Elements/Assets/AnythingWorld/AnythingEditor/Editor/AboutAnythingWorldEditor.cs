using UnityEditor;
using UnityEngine;

namespace AnythingWorld.Editor
{
    public class AboutAnythingWorldEditor : AnythingEditor
    {
        [MenuItem("Tools/Anything World/About", false, 1)]
        internal static void Initialize()
        {
            Vector2 windowSize;

            windowSize = new Vector2(600, 180);
            CloseWindowIfOpen<AboutAnythingWorldEditor>();
            var browser = GetWindow(typeof(AboutAnythingWorldEditor), true, "About");
            browser.position = new Rect(EditorGUIUtility.GetMainWindowPosition().center - windowSize / 2, windowSize);
            browser.minSize = browser.maxSize = windowSize;
            browser.Show();
            browser.Focus();
        }

        protected new void OnGUI()
        {
            base.OnGUI();

            var margin = 20f;
            var padding = 10f;
            var logoSize = 72f;

            var versionHeight = 24f;

            var titleStyle = new GUIStyle() { normal = SetStyleState(Color.white), hover = SetStyleState(Color.white), font = GetPoppinsFont(PoppinsStyle.Bold), fontSize = 44, alignment = TextAnchor.MiddleLeft };
            var titleContent = new GUIContent("ANYTHING WORLD");
            var titleWidth = position.width - (logoSize + (margin * 2) + padding);
            var titleHeight = logoSize - versionHeight - padding;

            var logoRect = new Rect(margin, margin, logoSize, logoSize);
            var titleContentRect = new Rect(logoRect.xMax + padding, logoRect.y, titleWidth, titleHeight);
            var versionContentRect = new Rect(logoRect.xMax + padding, logoRect.yMax - versionHeight, titleWidth, versionHeight);

            GUI.DrawTexture(logoRect, BaseAnythingGlobeLogoFilled);
            GUI.Label(titleContentRect, titleContent, titleStyle);
            DrawAutoSizeRoundedLabel(versionContentRect.position, new GUIContent(AnythingSettings.PackageVersion), versionContentRect.height, 16, PoppinsStyle.Medium);

            var copyrightStyle = new GUIStyle() { normal = SetStyleState(Color.white), hover = SetStyleState(Color.white), font = GetPoppinsFont(PoppinsStyle.Medium), fontSize = 14, alignment = TextAnchor.MiddleLeft };
            var copyrightContent = new GUIContent("Copyright \u00a9 2019 - 2023 Anything World Limited, a Techstars portfolio company.\nAll rights reserved");
            var copyrightWidth = position.width - (margin * 2);
            var copyrightHeight = copyrightStyle.CalcHeight(copyrightContent, copyrightWidth);

            var copyrightContentRect = new Rect(margin, position.height - margin - copyrightHeight, copyrightWidth, copyrightHeight);
            GUI.Label(copyrightContentRect, copyrightContent, copyrightStyle);
        }

        [MenuItem("Tools/Anything World/Discord Support", false, 61)] internal static void RibbonBarDiscordSupport() => System.Diagnostics.Process.Start("https://discord.gg/anythingworld");

        [MenuItem("Tools/Anything World/Docs", false, 62)] internal static void RibbonBarDocumentation() => System.Diagnostics.Process.Start("https://anything-world.gitbook.io/anything-world/unity-quickstart");
    }
}
