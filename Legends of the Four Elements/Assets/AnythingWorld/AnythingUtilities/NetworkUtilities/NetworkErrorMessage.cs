using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace AnythingWorld.Utilities.Networking
{
    [Serializable]
    public class NetworkErrorMessage
    {
        public string errorCode;
        public string code = "";
        public string message = "";
        public NetworkErrorMessage()
        {

        }
        public NetworkErrorMessage(UnityWebRequest request)
        {
            try
            {
                var json = request.downloadHandler.text;
                var _ = JsonUtility.FromJson<NetworkErrorMessage>(json);
                code = _.code;
                message = _.message;
                errorCode = ParseErrorCode(request.error);

            }
            catch
            {
                Debug.LogWarning("Problem parsing request into NetworkErrorMessage: " + request.error);
            }
        }
     
        private string ParseErrorCode(string errorString)
        {
            //Expects input of eg. "HTTPS/1.1 405"
            var split = errorString.Split(' ');
            if (split.Length >=2)
            {
                return split[1];
            }
            else
            {
                return "Not defined";
            }
        }
    }
}
