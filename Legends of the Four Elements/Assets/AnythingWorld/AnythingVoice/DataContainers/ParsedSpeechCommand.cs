using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static AnythingWorld.Voice.CommandSchema;

namespace AnythingWorld.Voice
{
    public enum CommandResult{
        Success,
        Fail
    };
    [System.Serializable]
    public class ParsedSpeechCommand
    {
        /// <summary>
        /// Raw transcription of speech from audio/text input.
        /// </summary>
        public string text = "";

        public string rawJson;
        /// <summary>
        /// List of actions parsed from transcribed speech.
        /// </summary>
        public List<Action> actions = new List<Action>();
        /// <summary>
        /// Result of request (Fail/Success)
        /// </summary>
        public CommandResult result;
    }
    [System.Serializable]
    public class Action
    { 
        /// <summary>
        /// Description of action to be done (eg. add_model, remove_model)
        /// </summary>
        public string action;

        /// <summary>
        /// Transcription of speech for this specific action
        /// </summary>
        public string text;

        /// <summary>
        /// Action parsed to enum.
        /// </summary>
        public ActionSchema actionEnum;

        public string habitat;
        public HabitatSchema habitatEnum;
        /// <summary>
        /// List of models that this action applies to.
        /// </summary>
        public List<Model> models = new List<Model>();

        public bool actionExecuted;
    }

    [System.Serializable]
    public class Model
    {
        /// <summary>
        /// Name of model.
        /// </summary>
        public string name = "";

        public Location location;
        /// <summary>
        /// Quantity of this model.
        /// </summary>
        public int number = 1;
    }
    [System.Serializable]
    public class Location
    {
        public string relative;
        public string model_name;
        public RelativeLocationSchema relativeEnum;
    }

}
