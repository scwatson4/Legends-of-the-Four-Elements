using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnythingWorld.Voice
{
    public class VoiceJsonParser
    {
        public static ParsedSpeechCommand ProcessReturnedCommand(string _json, CommandResult _result)
        {
            try
            {
                var speechCommand = Newtonsoft.Json.JsonConvert.DeserializeObject<ParsedSpeechCommand>(_json);
                speechCommand.rawJson = _json;
                GetEnumSchema(speechCommand);
                speechCommand.result = _result;
                return speechCommand;

            }
            catch(System.Exception e)
            {
                Debug.LogException(e);
                Debug.Log($"Could not deserialize json into ParsedSpeech object: {_json}");
                return null;
            }
          
        }
        public static void ProcessReturnedSpeech(string _json)
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.DeserializeObject<ParsedSpeech>(_json);
            }
            catch
            {
                Debug.Log($"Could not deserialize json into ParsedSpeech object: {_json}");
            }
        }
        public static void GetEnumSchema(ParsedSpeechCommand speechCommand)
        {
            foreach(var action in speechCommand?.actions)
            {
                action.actionEnum = CommandSchema.ParseAction(action?.action);
                action.habitatEnum = CommandSchema.ParseHabitat(action?.habitat);
                foreach(var model in action?.models)
                {
                    if (model.location != null)
                    {
                        model.location.relativeEnum = CommandSchema.ParseRelativeLocation(model?.location?.relative);
                    }
                   
                }
            }
        }

    }
}
