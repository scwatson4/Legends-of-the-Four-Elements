using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnythingWorld.Utilities.Networking;
namespace AnythingWorld.Networking
{
    public class NetworkErrorHandler
    {
        public static void HandleError(NetworkErrorMessage errorMessage)
        {
            switch (errorMessage.code)
            {
                case "Unrepeatable action":
                    break;
                case "Too many requests error": // API key quota exceeded
                    PrintNetworkLogWarning(errorMessage);
                    break;
                default:
                    PrintNetworkLogWarning(errorMessage);
                    break;
            }
        
        }
        private static void PrintNetworkLogWarning(NetworkErrorMessage errorMessage)
        {
            Debug.LogWarning($"Network Error: {errorMessage.code}({errorMessage.errorCode}): {errorMessage.message}");
        }
    }
}
