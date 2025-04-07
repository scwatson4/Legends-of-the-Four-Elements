using AnythingWorld.Core;
using AnythingWorld.Utilities;
using AnythingWorld.Utilities.Data;

using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace AnythingWorld
{
    public static class AnythingAnimate
    {
#if UNITY_EDITOR
        /// <summary>
        /// Processes the model and rigs it through Animate Anything. Extracts any additional files from the initial model. (Unity Editor only)
        /// </summary>
        /// <param name="model">The model to be rigged</param>
        /// <param name="modelName">The name of the model</param>
        /// <param name="modelType">The type of model (please see <a href="https://anything-world.gitbook.io/anything-world/quickstart/animate-anything-quickstart/faq">the model constraints</a> to know what types are supported)</param>
        /// <param name="authorName">The author name</param>
        /// <param name="license">What license the model should use (CC0, CC BY 4.0, and MIT are the licenses supported currently by Animate Anything)</param>
        /// <param name="symmetrical">Is the model symmetrical?</param>
        /// <param name="allowSystemImprovement">Is Anything World allowed to use this model for internal improvement?</param>
        /// <param name="onExport">A function specifying what should be done when the model has been successfully exported (variable of the function produce the ID of the model)</param>
        /// <param name="onError">A function specifying what should be done if an error occurs during the export (variables of the function produce the ID of the model if one is supplied and the UnityWebRequest details used to troubleshoot the error)</param>
        /// <param name="additionalAssetsPath">The path to any additional assets for the model to be rigged (if no additional assets are necessary, set this to null)</param>
        public static async UniTask AnimateAsync(GameObject model, string modelName, string modelType, string authorName, string license, bool symmetrical, bool allowSystemImprovement, Action<string> onExport, Action<string, string, string> onError, string additionalAssetsPath = "", Action<string, string> onProcessFail = null)
        {
            await AnimateFactory.AnimateAsync($"{Directory.GetCurrentDirectory()}/{AssetDatabase.GetAssetPath(model)}", modelName, modelType, authorName, license, onExport, onError, allowSystemImprovement, symmetrical, string.IsNullOrWhiteSpace(additionalAssetsPath) ? "" : $"{Directory.GetCurrentDirectory()}/{additionalAssetsPath}", onProcessFail);
        }
#endif
        
        /// <summary>
        /// Processes the model and rigs it through Animate Anything. The model and any additional files are accessed through a direct file path.
        /// </summary>
        /// <param name="modelPath">The path to the model to be rigged</param>
        /// <param name="additionalAssetsPath">The path to any additional assets for the model to be rigged (if no additional assets are necessary, set this to null)</param>
        /// <param name="modelName">The name of the model</param>
        /// <param name="modelType">The type of model (please see <a href="https://anything-world.gitbook.io/anything-world/quickstart/animate-anything-quickstart/faq">the model constraints</a> to know what types are supported)</param>
        /// <param name="authorName">The author name</param>
        /// <param name="license">What license the model should use (CC0, CC BY 4.0, and MIT are the licenses supported currently by Animate Anything)</param>
        /// <param name="symmetrical">Is the model symmetrical?</param>
        /// <param name="allowSystemImprovement">Is Anything World allowed to use this model for internal improvement?</param>
        /// <param name="onExport">A function specifying what should be done when the model has been successfully exported (variable of the function produce the ID of the model)</param>
        /// <param name="onError">A function specifying what should be done if an error occurs during the export (variables of the function produce the ID of the model if one is supplied and the UnityWebRequest details used to troubleshoot the error)</param>
        public static async UniTask AnimateAsync(string modelPath, string additionalAssetsPath, string modelName, string modelType, string authorName, string license, bool symmetrical, bool allowSystemImprovement, Action<string> onExport, Action<string, string, string> onError)
        {
            await AnimateFactory.AnimateAsync(modelPath, modelName, modelType, authorName, license, onExport, onError, allowSystemImprovement, symmetrical, additionalAssetsPath);
        }

        
        /// <summary>
        /// Starts polling continously until the model has finished processing.
        /// </summary>
        /// <param name="onProcessFinished">A function specifying what to do with the model once it has finished processing</param>
        /// <param name="onProcessFail">A function specifying what to do with the model if processing fails</param>
        /// <param name="onProcessPoll">A function specifying any interim function to be called each time the function pings the server for an update</param>
        /// <param name="id">The ID of the model to poll for</param>
        /// <param name="timeout">The time in seconds before the polling should timeout</param>
        public static void Poll(Action<ModelJson> onProcessFinished, Action<string, string> onProcessFail, Action<string, string, int> onProcessPoll, string id, bool polling, int timeout = 600)
        {
            AnimateFactory.PollModel(onProcessFinished, onProcessFail, onProcessPoll, id, polling, timeout);
        }
    }
}
