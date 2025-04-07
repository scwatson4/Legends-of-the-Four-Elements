using AnythingWorld.Networking;
using AnythingWorld.Utilities;
using AnythingWorld.Utilities.Networking;
using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;

#if !UNITY_WEBGL
namespace AnythingWorld.Voice
{
    /// <summary>
    /// Class for recording and parsing audio using the Anything World API.
    /// </summary>
    public class AnythingVoice
    {
        public delegate void OnErrorDelegate(NetworkErrorMessage errorMessage);

        private static OnErrorDelegate failDelegate;
        private static OnErrorDelegate localFailDelegate;
        /// <summary>
        /// Stores audio clip that we record to.
        /// </summary>
        public static AudioClip audioClip;
        public static float Volume;
        /// <summary>
        /// Name of recording microphone.
        /// </summary>
        public static string recordingMic;
        /// <summary>
        /// Are we currently recording to the clip?
        /// </summary>
        public static bool isRecording = false;
  

        /// <summary>
        /// Are we currently uploading audioclip to api?
        /// </summary>
        public static bool uploadInProgress = false;
        public static float uploadProgress = 0;

        public static float volumeThreshold = 0.2f;
        /// <summary>
        /// Was json returned successfully?
        /// </summary>
        public static bool jsonReturned = false;

        /// <summary>
        /// Command parsed from last uploaded audio clip?
        /// </summary>
        [SerializeField]
        public static ParsedSpeechCommand parsedCommand;

        


        /// <summary>
        /// Start microphone and record audio to audioClip.
        /// </summary>
        public static void StartRecording()
        {
            parsedCommand = null;
            isRecording = true;

            audioClip = null;
            foreach (var device in Microphone.devices)
            {
                Microphone.End(device);
            }
            if (Microphone.devices.Length > 0)
            {
                recordingMic = Microphone.devices[0];
                audioClip = Microphone.Start(recordingMic, true, 999, 44100);
                //CoroutineExtension.StartCoroutine(GetKeyWords());
            }
            else
            {
                isRecording = false;
#if UNITY_EDITOR
                Debug.Log("No microphone found, try going into play mode and out again.");
#else
                Debug.Log("No microphone found");
#endif



            }
        }
        /// <summary>
        /// Stop audio and clip the empty space in the audioclip.
        /// </summary>
        /// <returns>Trimmed audio clip.</returns>
        public static AudioClip StopRecording()
        {
            var position = Microphone.GetPosition(recordingMic);
            foreach (var device in Microphone.devices)
            {
                Microphone.End(device);
            }
            isRecording = false;
            recordingMic = "";

            var shortenedClip = AudioProcessor.TrimAudioClip(audioClip, position);
            return shortenedClip;
        }
        private static int lastPosition;
        private static bool isWord = false;
        public static IEnumerator GetKeyWords()
        {
            while (isRecording)
            {
                if (GetCurrentVolumeRange() > volumeThreshold)
                {
                    if (isWord) continue;
                    else
                    {
                        isWord = true;
                    }
                }
                else
                {
                    if (isWord)
                    {
                        isWord = false;
                    }
                    else continue;
                }
                yield return new WaitForEndOfFrame();
            }
        }
        
        /// <summary>
        /// Convert audioclip sample data to Wav format and then send .wav bytes to be processed by API.
        /// </summary>
        /// <param name="clip">AudioClip to be converted and parsed.</param>
        public static async UniTask ExtractBytesAndProcessAsync(AudioClip clip, OnErrorDelegate onErrorDelegate)
        {
            try
            {
                localFailDelegate = onErrorDelegate;
                if (clip?.length > 0)
                {
                    var bytes = SavWav.GetWavByteArray(clip);
                    await AudioProcessor.RequestCommandFromSpeechFileAsync(bytes, OnFail, UpdateProgressBar, OnSuccess);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"An error occurred during request command from speech: {ex.Message}");
            }

        }

        /// <summary>
        /// Request input string parsed into command.
        /// </summary>
        public static async UniTask RequestCommandsFromStringAsync(string input, OnErrorDelegate onErrorDelegate)
        {
            try
            {
                localFailDelegate = onErrorDelegate;
                parsedCommand = null;
                if (input != "")
                {
                    await AudioProcessor.RequestCommandFromStringInputAsync(input, OnFail, UpdateProgressBar, OnSuccess);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"An error occurred during request command from string: {e.Message}");
            }
        }
        /// <summary>
        /// Update upload progress metrics.
        /// </summary>
        /// <param name="update"></param>
        public static void UpdateProgressBar(float update)
        {
            uploadInProgress = true;
            uploadProgress = update;
        }
        /// <summary>
        /// On Success parse and save json data.
        /// </summary>
        /// <param name="_json"></param>
        public static void OnSuccess(string _json)
        {
            jsonReturned = true;
            uploadInProgress = false;
            parsedCommand = VoiceJsonParser.ProcessReturnedCommand(_json, CommandResult.Success);
        }
        /// <summary>
        /// On fail reset voice panel and handle error.
        /// </summary>
        /// <param name="error"></param>
        public static void OnFail(NetworkErrorMessage error)
        {

#if UNITY_EDITOR
            failDelegate += localFailDelegate;
            failDelegate(error);
            failDelegate -= localFailDelegate;
#else
            NetworkErrorHandler.HandleError(error);
#endif

            parsedCommand = new ParsedSpeechCommand();
            //parsedCommand.text = error;
            parsedCommand.result = CommandResult.Fail;
            uploadInProgress = false;
            uploadProgress = 0;
        }
        /// <summary>
        /// Convert from custom range of decibels to 0-1 range.
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>

        private static float lastVolume = 0;
        public static float GetCurrentVolumeRange()
        {
            if (isRecording && audioClip != null)
            {
                var decibels = LevelMax(audioClip);
                var range = DecibelToRange(decibels);

                range = 0.6f * Mathf.Pow(Mathf.Tan(range), 3);
                range = Mathf.Clamp01(range);
                if (range < 0.02f) range = 0;

                //var lerped = Mathf.Lerp(lastVolume, range, Time.deltaTime);
                lastVolume = range;
                return range;
            }
            else
            {
                return 0;
            }
        }


        private static float DecibelToRange(float db)
        {
            return (float)ConvertFrom_Range1_Input_To_Range2_Output(-60, 0, 0, 1, db);
        }


        /// <summary>
        /// Caculate max volume of sample window in DB.
        /// </summary>
        static int _sampleWindow = 10;
        private const int QSamples = 1024;
        private const float RefValue = 0.1f;
        private static float LevelMax(AudioClip clip)
        {

            if (clip == null) return 0;
            float levelMax = 0;
            float[] waveData = new float[_sampleWindow];
            int micPosition = Microphone.GetPosition(null) - (_sampleWindow + 1); // null means the first microphone
            if (micPosition < 0) return 0;
            clip.GetData(waveData, micPosition);
            float sum = 0;
            // Getting a peak on the last 128 samples
            for (int i = 0; i < _sampleWindow; i++)
            {
                float wavePeak = waveData[i] * waveData[i];
                sum += wavePeak;
                if (levelMax < wavePeak)
                {
                    levelMax = wavePeak;
                }
            }
            var rmsVal = Mathf.Sqrt(sum / QSamples); // rms = square root of average
            var dbVal = 20 * Mathf.Log10(rmsVal / RefValue); // calculate dB

            float db = 20 * Mathf.Log10(Mathf.Abs(levelMax));
            return dbVal;
        }


        /// <summary>
        /// Converts one range to another. 
        /// </summary>
        private static double ConvertFrom_Range1_Input_To_Range2_Output(double _input_range_min,
        double _input_range_max, double _output_range_min,
        double _output_range_max, double _input_value_tobe_converted)
        {
            double diffOutputRange = Math.Abs((_output_range_max - _output_range_min));
            double diffInputRange = Math.Abs((_input_range_max - _input_range_min));
            double convFactor = (diffOutputRange / diffInputRange);
            return (_output_range_min + (convFactor * (_input_value_tobe_converted - _input_range_min)));
        }

    }
}
#endif