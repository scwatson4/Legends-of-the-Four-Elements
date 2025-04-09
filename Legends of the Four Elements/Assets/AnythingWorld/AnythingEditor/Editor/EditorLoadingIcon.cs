namespace AnythingWorld.Editor
{
    using UnityEditor;
    using UnityEngine;
    /// <summary>
    /// Editor class that draws a loading message.
    /// </summary>
    internal class EditorLoadingIcon : Editor
    {
        private static EditorLoadingIcon _instance;
        public static EditorLoadingIcon Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = ScriptableObject.CreateInstance<EditorLoadingIcon>();
                }
                return _instance;
            }
        }
        /// <summary>
        /// Draws message.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="view"></param>
        public void ShowToastyMessage(string message, EditorWindow view)
        {
            view.ShowNotification(new GUIContent(message), 1);
        }
        public void ShowToastyMessage(string message, EditorWindow view, float time)
        {
            view.ShowNotification(new GUIContent(message), time);
        }
    }
}