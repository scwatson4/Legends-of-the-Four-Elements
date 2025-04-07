using AnythingWorld.Utilities.Networking;
using UnityEngine;
using UnityEditor;

namespace AnythingWorld.Editor
{
    public class EditorNetworkErrorHandler
    {
        public static void HandleError(NetworkErrorMessage errorMessage)
        {
            

            switch (errorMessage.code)
            {
                case "Unrepeatable action":
                    break;
                case "Too many requests error":
                    AnythingEditor.DisplayAWDialog("API Key Quote Exceeded", errorMessage.message, "Go to Profile", "Close", () => Application.OpenURL("https://get.anything.world/profile"));
                    PrintNetworkLogError(errorMessage);
                    break;
                default:
                    switch (errorMessage.errorCode)
                    {
                        case "400":
                            Debug.LogError($"{errorMessage.code} ({errorMessage.errorCode}) | There seems to be something off with the request format. Could you double-check it?");
                            break;
                        case "403":
                            Debug.LogError($"{errorMessage.code} ({errorMessage.errorCode}) | Looks like there's an issue with the model type. Let's make sure everything's in order!");
                            break;
                        case "404":
                            Debug.LogError($"{errorMessage.code} ({errorMessage.errorCode}) | We couldn't find what you were looking for. Might want to check that again!");
                            break;
                        case "500":
                            Debug.LogError($"{errorMessage.code} ({errorMessage.errorCode}) | We've hit a snag on our side. Rest assured, we're looking into it!");
                            break;
                        default:
                            Debug.LogError($"{errorMessage.code} ({errorMessage.errorCode}) | Something unexpected happened. Our team has been notified, and we're on it!");
                            break;
                    }
                    break;
            }
        
        }
        private static void PrintNetworkLogError(NetworkErrorMessage errorMessage)
        {
            Debug.LogError($"{errorMessage.code} ({errorMessage.errorCode}) | {errorMessage.message}");
        }
    }
}
