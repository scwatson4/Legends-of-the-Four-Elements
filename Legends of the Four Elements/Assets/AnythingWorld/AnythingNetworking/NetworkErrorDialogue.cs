using AnythingWorld.Utilities.Networking;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AnythingWorld.Networking
{
    /// <summary>
    /// Handle creation and display of dialogue boxes for network errors.
    /// </summary>
    public class NetworkErrorDialogue
    {
        /// <summary>
        /// Displays a dialogue box that displays the code and message from an error,
        /// and then gives the user an option to open their profile in browse to resolve it.
        /// </summary>
        /// <param name="errorMessage"></param>
        public static void DisplayApiErrorDialogue(NetworkErrorMessage errorMessage)
        {
#if UNITY_EDITOR
            if (EditorUtility.DisplayDialog(errorMessage.code, errorMessage.message, "Go to profile", "Close"))
            {
                Application.OpenURL("https://get.anything.world/profile");
            }
#endif
        }
    }
}