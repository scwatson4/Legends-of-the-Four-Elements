using System;
using AnythingWorld.Utilities.Networking;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

namespace AnythingWorld.Networking
{
    /// <summary>
    /// Handles sending audio data to API to be parsed into text and/or commands.
    /// </summary>
    public class AudioProcessor
    {
        /// <summary>
        /// Upload speech wav file to be parsed into text.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="callbackSuccess"></param>
        /// <param name="callbackFailure"></param>
        /// <param name="callbackLoading"></param>
        /// <param name="additionalCallbackSuccess"></param>
        /// <returns></returns>
        public static async UniTask RequestTranscriptionFromSpeechFileAsync(byte[] stream, Action<string> callbackSuccess = null, Action<NetworkErrorMessage> callbackFailure = null, Action<float> callbackLoading = null, params Action<string>[] additionalCallbackSuccess)
        {
            if (!AnythingSettings.HasAPIKey) return;
            WWWForm form = new WWWForm();
            form.AddBinaryData("speech", stream);
            using (UnityWebRequest www = UnityWebRequest.Post(Utilities.NetworkConfig.SpeechToTextUri(), form))
            {
                var operation = www.SendWebRequest();
                while (!operation.isDone)
                {
                    callbackLoading?.Invoke(www.uploadProgress);
                    await UniTask.Yield();
                }
                if (www.result != UnityWebRequest.Result.Success)
                {
                    var error = new NetworkErrorMessage(www);
                    NetworkErrorHandler.HandleError(error);
                    callbackFailure?.Invoke(error);
                }
                else
                {
                    callbackSuccess?.Invoke(www.downloadHandler.text);
                    foreach (var action in additionalCallbackSuccess) action?.Invoke(www.downloadHandler.text);
                }
            }
        }

        public static async UniTask RequestTranscriptionFromSpeechFileAsync(byte[] stream, Action<string> callbackSuccess = null, Action<NetworkErrorMessage> callbackFailure = null, Action<float> callbackUploading = null, Action<float> callbackWaiting = null, params Action<string>[] additionalSuccessCallback)
        {
            if (!AnythingSettings.HasAPIKey) return;
            WWWForm form = new WWWForm();
            form.AddBinaryData("speech", stream);
            using (UnityWebRequest www = UnityWebRequest.Post(Utilities.NetworkConfig.SpeechToTextUri(), form))
            {
                var operation = www.SendWebRequest();
                while (!operation.isDone)
                {
                    if (www.uploadProgress < 1)
                    {
                        callbackUploading?.Invoke(www.uploadProgress);
                    }
                    else
                    {
                        callbackWaiting?.Invoke(www.uploadProgress);
                    }
                    await UniTask.Yield();
                }
                if (www.result != UnityWebRequest.Result.Success)
                {
                    var error = new NetworkErrorMessage(www);
                    NetworkErrorHandler.HandleError(error);
                    callbackFailure?.Invoke(error);
                }
                else
                {
                    callbackSuccess?.Invoke(www.downloadHandler.text);
                    foreach (var action in additionalSuccessCallback) action?.Invoke(www.downloadHandler.text);
                }
            }
        }

        /// <summary>
        /// Upload speech file to be parsed into command.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="callbackActionFailure"></param>
        /// <param name="callbackActionLoading"></param>
        /// <param name="additionalSuccessCallbackActions"></param>
        /// <returns></returns>
        public static async UniTask RequestCommandFromSpeechFileAsync(byte[] stream, Action<NetworkErrorMessage> callbackActionFailure = null, Action<float> callbackActionLoading = null, params Action<string>[] additionalSuccessCallbackActions)
        {
            if (!AnythingSettings.HasAPIKey) return;
            WWWForm form = new WWWForm();
            form.AddBinaryData("speech", stream);
            using (UnityWebRequest www = UnityWebRequest.Post(Utilities.NetworkConfig.SpeechToCommandUri(), form))
            {
                var operation = www.SendWebRequest();
                while (!operation.isDone)
                {
                    callbackActionLoading?.Invoke(www.uploadProgress);
                    await UniTask.Yield();
                }
                if (www.result != UnityWebRequest.Result.Success)
                {
                    var error = new NetworkErrorMessage(www);
                    callbackActionFailure?.Invoke(error);
                }
                else
                {
                    foreach (var action in additionalSuccessCallbackActions) action?.Invoke(www.downloadHandler.text);
                }
            }
        }

        public static async UniTask RequestCommandFromSpeechFileAsync(byte[] stream, Action<NetworkErrorMessage> callbackActionFailure = null, Action<float> callbackActionUploading = null, Action<float> callbackActionWaiting = null, params Action<string>[] additionalSuccessCallbackActions)
        {
            if (!AnythingSettings.HasAPIKey) return;
            WWWForm form = new WWWForm();
            form.AddBinaryData("speech", stream);
            using (UnityWebRequest www = UnityWebRequest.Post(Utilities.NetworkConfig.SpeechToCommandUri(), form))
            {
                var operation = www.SendWebRequest();
                while (!operation.isDone)
                {
                    if (www.uploadProgress < 1)
                    {
                        callbackActionUploading?.Invoke(www.uploadProgress);
                    }
                    else
                    {
                        callbackActionWaiting?.Invoke(www.uploadProgress);
                    }
                    await UniTask.Yield();
                }
                if (www.result != UnityWebRequest.Result.Success)
                {
                    var error = new NetworkErrorMessage(www);
                    callbackActionFailure?.Invoke(error);
                }
                else
                {
                    foreach (var action in additionalSuccessCallbackActions) action?.Invoke(www.downloadHandler.text);
                }
            }
        }

        /// <summary>
        /// Request input string parsed into command.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="callbackActionFailure"></param>
        /// <param name="callbackActionLoading"></param>
        /// <param name="additionalSuccessCallbackActions"></param>
        /// <returns></returns>
        public static async UniTask RequestCommandFromStringInputAsync(string input, Action<NetworkErrorMessage> callbackActionFailure = null, Action<float> callbackActionLoading = null, params Action<string>[] additionalSuccessCallbackActions)
        {
            if (!AnythingSettings.HasAPIKey) return;
            WWWForm form = new WWWForm();
            form.AddField("text", input);
            using (UnityWebRequest www = UnityWebRequest.Post(Utilities.NetworkConfig.TextToCommandUri(), form))
            {
                var operation = www.SendWebRequest();
                while (!operation.isDone)
                {
                    callbackActionLoading?.Invoke(www.uploadProgress);
                    await UniTask.Yield();
                }
                if (www.result != UnityWebRequest.Result.Success)
                {
                    var error = new NetworkErrorMessage(www);
                    callbackActionFailure?.Invoke(error);
                }
                else
                {
                    foreach (var action in additionalSuccessCallbackActions)
                    {
                        action?.Invoke(www.downloadHandler.text);
                    }
                }
            }
        }

        public static async UniTask RequestCommandFromStringInputAsync(string input, Action<NetworkErrorMessage> callbackFailure = null, Action<float> callbackUploading = null, Action<float> callbackWaiting = null, params Action<string>[] successCallbacks)
        {
            if (!AnythingSettings.HasAPIKey) return;
            WWWForm form = new WWWForm();
            form.AddField("text", input);
            using (UnityWebRequest www = UnityWebRequest.Post(Utilities.NetworkConfig.TextToCommandUri(), form))
            {
                var operation = www.SendWebRequest();
                while (!operation.isDone)
                {
                    if (www.uploadProgress < 1)
                    {
                        callbackUploading?.Invoke(www.uploadProgress);
                    }
                    else
                    {
                        callbackWaiting?.Invoke(www.uploadProgress);
                    }
                    await UniTask.Yield();
                }
                if (www.result != UnityWebRequest.Result.Success)
                {
                    var error = new NetworkErrorMessage(www);
                    callbackFailure?.Invoke(error);
                }
                else
                {
                    foreach (var action in successCallbacks) action?.Invoke(www.downloadHandler.text);
                }
            }
        }

        /// <summary>
        /// Trim audio clip to stopped position.
        /// </summary>
        /// <param name="recordedClip"></param>
        /// <param name="stoppedPosition"></param>
        /// <returns></returns>
        public static AudioClip TrimAudioClip(AudioClip recordedClip, int stoppedPosition)
        {
            if (recordedClip == null) return null;
            if (stoppedPosition == 0) return null;
            //Capture the current clip data
            var soundData = new float[recordedClip.samples * recordedClip.channels];
            recordedClip.GetData(soundData, 0);
            //Create shortened array for the data that was used for recording
            var newData = new float[stoppedPosition * recordedClip.channels];

            //Copy the used samples to a new array
            for (int i = 0; i < newData.Length; i++)
            {
                newData[i] = soundData[i];
            }

            //One does not simply shorten an AudioClip,
            //so we make a new one with the appropriate length
            var newClip = AudioClip.Create(recordedClip.name, stoppedPosition, recordedClip.channels, recordedClip.frequency, false);
            //var newClip = AudioClip.Create(recordedClip.name, stoppedPosition, recordedClip.channels, recordedClip.frequency, false, false);

            newClip.SetData(newData, 0);        //Give it the data from the old clip

            //Replace the old clip
            AudioClip.DestroyImmediate(recordedClip);
            return newClip;
        }
        /// <summary>
        /// Trim audio clip between two positions and return new clip to use.
        /// </summary>
        /// <param name="recordedClip"></param>
        /// <param name="startPosition"></param>
        /// <param name="endPosition"></param>
        /// <returns></returns>
        private static AudioClip TrimAudioClip(AudioClip recordedClip, int startPosition, int endPosition)
        {
            if (recordedClip == null) return null;
            if (endPosition == 0) return null;
            //Capture the current clip data
            var soundData = new float[recordedClip.samples * recordedClip.channels];
            recordedClip.GetData(soundData, 0);
            //Create shortened array for the data that was used for recording
            var newData = new float[(endPosition-startPosition) * recordedClip.channels];

            //Copy the used samples to a new array
            for (int i = 0; i < newData.Length; i++)
            {
                newData[i] = soundData[i+startPosition];
            }

            //One does not simply shorten an AudioClip,
            //so we make a new one with the appropriate length
            var newClip = AudioClip.Create(recordedClip.name, endPosition-startPosition, recordedClip.channels, recordedClip.frequency, false);
            //var newClip = AudioClip.Create(recordedClip.name, stoppedPosition, recordedClip.channels, recordedClip.frequency, false, false);

            newClip.SetData(newData, 0);        //Give it the data from the old clip

            //Replace the old clip
            AudioClip.DestroyImmediate(recordedClip);
            return newClip;
        }
    }
}
