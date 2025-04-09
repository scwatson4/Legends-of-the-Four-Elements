using AnythingWorld.Networking;
using AnythingWorld.Utilities.Data;
using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Diagnostics.Tracing;

namespace AnythingWorld.Core
{
    public static class AnimateFactory
    {
        public static Dictionary<string, string> validModelFileTypes = new Dictionary<string, string>()
        {
            { ".obj", "text/plain" },
            { ".fbx", "application/octet-stream" },
            { ".dae", "application/xml" },
            { ".gltf", "model/gltf+json" },
            { ".glb", "model/gltf-binary" }
        };

        public static Dictionary<string, string> validAssetFileTypes = new Dictionary<string, string>()
        {
            { ".mtl", "text/plain" },
            { ".bin", "application/octet-stream" },
            { ".jpg", "image/jpg" },
            { ".jpeg", "image/jpg" },
            { ".png", "image/png" },
            { ".bmp", "image/bmp" },
            { ".gif", "image/gif" },
            { ".tiff", "image/tiff" },
            { ".tif", "image/tiff" },
            { ".targa", "image/x-tga" },
            { ".tga", "image/x-tga" },
            { ".zip", "application/zip" },
        };

        /// <summary>
        /// Breaks down the files into byte arrays to be sent to Animate Anything
        /// </summary>
        /// <param name="modelPath">The path of the model</param>
        /// <param name="modelName">The name of the model</param>
        /// <param name="modelType">The type of model (please see <a href="https://anything-world.gitbook.io/anything-world/quikcstart/animate-anything-quickstart/faq">the model constraints</a> to know what types are supported)</param>
        /// <param name="authorName">The name of the author</param>
        /// <param name="license">What license the model should use (CC0, CC BY 4.0, and MIT are the licenses supported currently by Animate Anything)</param>
        /// <param name="onSuccessfulExport">A function specifying what should be done when the model has been successfully exported (variable of the function produce the ID of the model)</param>
        /// <param name="onErrorProcessing">A function specifying what should be done if an error occurs during the export (variables of the function produce the ID of the model if one is supplied and the UnityWebRequest details used to troubleshoot the error)</param>
        /// <param name="allowSystemImprovement">Is Anything World allowed to use this model for internal improvement? (Defaulted to false)</param>
        /// <param name="symmetrical">Is the model symmetrical? (Defaulted to true)</param>
        /// <param name="additionalAssetsPath">The path of the model's additional assets such as textures and materials (Defaulted to null)</param>
        /// <returns></returns>
        public static async UniTask AnimateAsync(string modelPath, string modelName, string modelType,
            string authorName, string license,
            Action<string> onSuccessfulExport, Action<string, string, string> onErrorProcessing,
            bool allowSystemImprovement = false, bool symmetrical = true, string additionalAssetsPath = null, Action<string, string> onProcessFail = null)
        {
            if (!validModelFileTypes.ContainsKey(Path.GetExtension(modelPath)))
            {
                Debug.LogError(
                    "The chosen model isn't of a valid file type. Please ensure that the model you are uploading is of type .fbx, .obj, .glb, or .gltf.");
                     //call the onProcessFail function if it is not null
                     onProcessFail?.Invoke(modelName, "The chosen model isn't of a valid file type. Please ensure that the model you are uploading is of type .fbx, .obj, .glb, or .gltf.");
                return;
            }

            List<(string fileName, byte[] fileContent, string contentType)> filesTuple =
                new List<(string, byte[], string)>();

            byte[] modelData = await File.ReadAllBytesAsync(modelPath);
            filesTuple.Add(($"{modelName}{Path.GetExtension(modelPath)}", modelData,
                validModelFileTypes[Path.GetExtension(modelPath)]));

            if (!string.IsNullOrWhiteSpace(additionalAssetsPath))
            {
                string[] customTexturePaths = Directory.GetFiles(additionalAssetsPath);
                for (int i = 0; i < customTexturePaths.Length; i++)
                {
                    if (!validAssetFileTypes.ContainsKey(Path.GetExtension(customTexturePaths[i]))) continue;
                    filesTuple.Add((Path.GetFileName(customTexturePaths[i]),
                        await File.ReadAllBytesAsync(customTexturePaths[i]),
                        validAssetFileTypes[Path.GetExtension(customTexturePaths[i])]));
                }
            }

            AnimateAnythingProcessor.CreateRigAsync(onSuccessfulExport, onErrorProcessing, modelName, modelType,
                filesTuple, symmetrical, allowSystemImprovement, authorName, license);
        }

        /// <summary>
        /// Starts polling continously until the model has finished processing.
        /// </summary>
        /// <param name="onProcessFinished">A function specifying what to do with the model once it has finished processing</param>
        /// <param name="onProcessFail">A function specifying what to do with the model if processing fails</param>
        /// <param name="onProcessPoll">A function specifying any interim function to be called each time the function pings the server for an update</param>
        /// <param name="id">The ID of the model to poll for</param>
        /// <param name="timeout">The time in seconds before the polling should timeout</param>
        public static void PollModel(Action<ModelJson> onProcessFinished, Action<string, string> onProcessFail,
            Action<string, string, int> onProcessPoll, string id, bool polling, int timeout = 600)
        {
            AnimateAnythingProcessor
                .PollAnimateAnythingAsync(onProcessFinished, onProcessFail, onProcessPoll, id,timeout).Forget();
        }
    }
}
