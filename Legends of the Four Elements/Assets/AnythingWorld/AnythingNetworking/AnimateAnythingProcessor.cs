using AnythingWorld.Utilities;
using AnythingWorld.Utilities.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace AnythingWorld.Networking
{
    [Serializable]
    public class ErrorResponse
    {
        public string code;
        public string message;
    }

    /// <summary>
    /// The processor class for Animate Anything web requests.
    /// </summary>
    public static class AnimateAnythingProcessor
    {
        private const string ConstraintsInfoLink =
            "https://anything-world.gitbook.io/anything-world/api/preparing-your-3d-model";

        // Polling delays (in seconds).
        private const int NormalPollingDelaySeconds = 5;
        private const int StillProcessingDelaySeconds = 10;

        /// <summary>
        /// Sends the files to be processed by Animate Anything.
        /// </summary>
        /// <param name="successDelegate">Called when the files have successfully been sent.</param>
        /// <param name="errorDelegate">Called if an error occurs.</param>
        /// <param name="modelName">The name of the model.</param>
        /// <param name="modelClassification">The type of the model.</param>
        /// <param name="files">The files being sent for processing.</param>
        /// <param name="symmetry">Flag indicating if the model is symmetric.</param>
        /// <param name="systemImproveConsent">Flag indicating if the user consents to internal improvements.</param>
        /// <param name="authorName">The author of the model.</param>
        /// <param name="license">The license used for the model.</param>
        public static void CreateRigAsync(
            Action<string> successDelegate,
            Action<string, string, string> errorDelegate,
            string modelName,
            string modelClassification,
            List<(string fileName, byte[] fileContent, string contentType)> files,
            bool symmetry,
            bool systemImproveConsent,
            string authorName,
            string license)
        {
            AnimateAnythingAsync(successDelegate, errorDelegate, modelName, modelClassification, files, symmetry, systemImproveConsent, authorName, license)
                .Forget();
        }

        /// <summary>
        /// Asynchronously sends the files to be processed by Animate Anything.
        /// </summary>
        /// <param name="onExport">Delegate called when the files have been successfully sent (with the returned model id).</param>
        /// <param name="onError">Delegate called if an error occurs (with an id, error code, and message).</param>
        /// <param name="modelName">The model name.</param>
        /// <param name="modelClassification">The model type/classification.</param>
        /// <param name="files">The list of files to send.</param>
        /// <param name="symmetry">True if the model is symmetric.</param>
        /// <param name="systemImproveConsent">True if the user consents to internal improvements.</param>
        /// <param name="authorName">The author name.</param>
        /// <param name="license">The model license.</param>
        /// <returns>A UniTask representing the asynchronous operation.</returns>
        private static async UniTask AnimateAnythingAsync(
            Action<string> onExport,
            Action<string, string, string> onError,
            string modelName,
            string modelClassification,
            List<(string fileName, byte[] fileContent, string contentType)> files,
            bool symmetry,
            bool systemImproveConsent,
            string authorName,
            string license)
        {
            WWWForm form = new WWWForm();
            form.AddField("platform", "unity");
            form.AddField("key", AnythingSettings.APIKey);
            form.AddField("model_name", modelName);
            form.AddField("model_type", modelClassification);
            form.AddField("can_use_for_internal_improvements", systemImproveConsent.ToString().ToLower());
            form.AddField("author", authorName);
            form.AddField("license", license);
            form.AddField("symmetry", symmetry.ToString().ToLower());

            foreach ((string fileName, byte[] fileContent, string contentType) file in files)
            {
                form.AddBinaryData("files", file.fileContent, file.fileName, file.contentType);
            }

            using (UnityWebRequest www = UnityWebRequest.Post(NetworkConfig.ProcessUserModel(), form))
            {
                try
                {
                    await www.SendWebRequest().ToUniTask();
                }
                catch (Exception e)
                {
                    // Provide extra context in the exception message.
                    HandleErrorResponse($"Exception during AnimateAnythingAsync: {e}");
                    return;
                }

                if (www.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var modelIdWrapper = JsonConvert.DeserializeObject<ModelIDWrapper>(www.downloadHandler.text);
                        onExport?.Invoke(modelIdWrapper.model_id);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to deserialize model id: {ex}");
                        onError?.Invoke("", "DeserializationError", ex.Message);
                    }
                }
                else
                {
                    try
                    {
                        AACode code = JsonConvert.DeserializeObject<AACode>(www.downloadHandler.text);
                        onError?.Invoke("", code.code, code.message);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Couldn't parse error: {www.downloadHandler.text}. Exception: {ex}");
                    }
                }
            }
        }

        /// <summary>
        /// Handles error responses by parsing the HTTP header and JSON body.
        /// </summary>
        /// <param name="errorResponseText">The error response text.</param>
        public static void HandleErrorResponse(string errorResponseText)
        {
            try
            {
                // Split the response by newlines.
                string[] lines = errorResponseText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                if (lines.Length < 2)
                {
                    Debug.LogError(errorResponseText);
                    return;
                }

                // Extract HTTP header (e.g., "HTTP/1.1 429 Too Many Requests").
                string headerLine = lines[0];
                string[] headerParts = headerLine.Split(' ');
                int httpStatusCode = 0;
                if (headerParts.Length >= 2 && int.TryParse(headerParts[1], out httpStatusCode))
                {
                    // Parsed status code.
                }
                else
                {
                    Debug.LogError("Failed to parse HTTP status code from header: " + headerLine);
                    return;
                }

                // Combine remaining lines into JSON body.
                string jsonBody = string.Join("\n", lines.Skip(1).ToArray());
                // Using Newtonsoft.Json for consistency.
                ErrorResponse error = JsonConvert.DeserializeObject<ErrorResponse>(jsonBody);
                string friendlyMessage = "";

                if (httpStatusCode == 429)
                {
                    friendlyMessage = "You're sending too many requests at once. Please slow down a bit.";
                }
                else if (error != null && !string.IsNullOrEmpty(error.code))
                {
                    if (error.code == "Credits exhausted")
                    {
                        friendlyMessage = "It looks like you've used all your free credits for this month. " +
                                          "Please purchase additional credits in your profile to continue.";
                    }
                    else if (string.IsNullOrEmpty(friendlyMessage))
                    {
                        friendlyMessage = $"Error ({error.code}): {error.message}";
                    }
                }
                else if (string.IsNullOrEmpty(friendlyMessage))
                {
                    friendlyMessage = "An unexpected error occurred. Please try again later.";
                }

                Debug.LogError(friendlyMessage);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception while handling error response: {ex.Message}");
                Debug.LogError("Raw error response: " + errorResponseText);
            }
        }

        /// <summary>
        /// Continuously polls for an update on the processed model.
        /// Special case: if a 403 response or exception is received with a "Still processing" message, waits for an extended period before retrying.
        /// </summary>
        /// <param name="onSuccess">Called when the model is processed successfully.</param>
        /// <param name="onFailure">Called when a fatal error occurs (with error code and message).</param>
        /// <param name="onPoll">Called on each poll iteration to report progress (code, message, poll count).</param>
        /// <param name="id">The model's ID.</param>
        /// <param name="timeout">The maximum number of polling attempts (default is 600).</param>
        /// <param name="cancellationToken">Optional cancellation token to stop polling externally.</param>
        /// <returns>A UniTask representing the asynchronous operation.</returns>
        public static async UniTask PollAnimateAnythingAsync(
            Action<ModelJson> onSuccess,
            Action<string, string> onFailure,
            Action<string, string, int> onPoll,
            string id,
            int timeout = 600,
            CancellationToken cancellationToken = default)
        {
            string uri = NetworkConfig.GetUserProcessed(id);
            int pollCount = 0;

            while (!cancellationToken.IsCancellationRequested && pollCount < timeout)
            {
                pollCount++;
                string result = string.Empty;

                try
                {
                    using (UnityWebRequest www = UnityWebRequest.Get(uri))
                    {
                        await www.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);

                        if (www.result != UnityWebRequest.Result.Success)
                        {
                            // Handle HTTP 403 errors specially.
                            if (www.responseCode == 403)
                            {
                                result = www.downloadHandler.text;
                                AACode pollData = null;
                                try
                                {
                                    pollData = JsonConvert.DeserializeObject<AACode>(result);
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogError($"Error parsing error response for model {id}: {ex}");
                                    onFailure?.Invoke("DeserializationError", ex.Message);
                                    break;
                                }

                                if (pollData != null &&
                                    pollData.code.Equals("Still processing", StringComparison.OrdinalIgnoreCase))
                                {
                                    Debug.Log("Still processing. Waiting 10 seconds before retrying.");
                                    onPoll?.Invoke(pollData.code, pollData.message, pollCount);
                                    await UniTask.Delay(TimeSpan.FromSeconds(StillProcessingDelaySeconds), cancellationToken: cancellationToken);
                                    continue;
                                }
                            }

                            Debug.LogError($"Error polling for model {id}: {www.error}");
                            onFailure?.Invoke(www.responseCode.ToString(), www.error);
                            break;
                        }
                        else
                        {
                            result = www.downloadHandler.text;
                            onFailure?.Invoke(result, string.Empty);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Check if the exception indicates a 403 "Still processing" error.
                    if (ex.Message.Contains("403") && ex.Message.Contains("Still processing"))
                    {
                        Debug.Log("Still processing. Waiting 10 seconds before retrying.");
                        await UniTask.Delay(TimeSpan.FromSeconds(StillProcessingDelaySeconds), cancellationToken: cancellationToken);
                        continue;
                    }
                    else
                    {
                        Debug.LogError($"Exception polling for model {id}: {ex}");
                        onFailure?.Invoke("RequestException", ex.Message);
                        break;
                    }
                }

                // If the result is wrapped in an array, trim the brackets.
                if (!string.IsNullOrEmpty(result) && result.StartsWith("["))
                {
                    result = result.TrimStart('[').TrimEnd(']');
                }

                // Attempt to deserialize as ModelJson.
                ModelJson model = null;
                try
                {
                    model = JsonConvert.DeserializeObject<ModelJson>(result);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Could not deserialize result as ModelJson for model {id}: {ex}");
                }

                // If the model is ready, report success and exit.
                if (model != null && model.model != null)
                {
                    Debug.Log($"Model {id} processed successfully.");
                    onSuccess?.Invoke(model);
                    return;
                }

                // Otherwise, deserialize as AACode to report polling status.
                AACode status = null;
                try
                {
                    status = JsonConvert.DeserializeObject<AACode>(result);
                }
                catch (Exception ex)
                {
                    //friendly message
                    Debug.LogError("An error occurred while processing your model. Please try again later.");
                    onFailure?.Invoke("DeserializationError", ex.Message);
                    break;
                }

                onPoll?.Invoke(status.code, status.message, pollCount);
                Debug.Log($"Polling attempt {pollCount} for model {id}: {status.code} - {status.message}");
                await UniTask.Delay(TimeSpan.FromSeconds(NormalPollingDelaySeconds), cancellationToken: cancellationToken);
            }

            if (pollCount >= timeout)
            {
                Debug.LogError("Polling timed out.");
                onFailure?.Invoke("408", "Request Timeout");
                PrintErrorMessage(id);
            }

            Debug.Log("Polling has stopped.");
            if (AnythingSettings.DebugEnabled)
            {
                Debug.Log("Animate Anything has finished processing your model! You may now add it to your scene!");
                onFailure?.Invoke("", "");
            }
        }

        /// <summary>
        /// Logs a detailed error message when model processing times out.
        /// </summary>
        /// <param name="id">The model's ID.</param>
        private static void PrintErrorMessage(string id)
        {
            Debug.LogError("Model processing timeout. Please ensure that your model fits our constraints. " +
                           $"Read about them here: {ConstraintsInfoLink}. " +
                           "If that doesn't help, contact support@anything.world and provide " +
                           $"the following model id: {id}. Your patience is appreciated!");
        }
    }

    /// <summary>
    /// Represents the status code and message returned by Animate Anything.
    /// </summary>
    public class AACode
    {
        public string code;
        public string message;
    }

    /// <summary>
    /// Wrapper for the model ID returned from Animate Anything.
    /// </summary>
    class ModelIDWrapper
    {
        public string model_id;
    }
}
