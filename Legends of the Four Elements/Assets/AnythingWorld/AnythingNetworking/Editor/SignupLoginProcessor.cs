using AnythingWorld.Utilities;

using System;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace AnythingWorld.Networking.Editor
{
    public static class SignupLoginProcessor
    {
        public struct SignupLoginError
        {
            public string code;
            public string message;

            public SignupLoginError(string code, string message) { this.code = code; this.message = message; }
        }

        public delegate void SubmitSignupLoginError(SignupLoginError error);
        private static SubmitSignupLoginError signupErrorDelegate;
        private static SubmitSignupLoginError loginErrorDelegate;

        public delegate void SubmitCredentialsSuccess();
        private static SubmitCredentialsSuccess credentialsSuccessDelegate;

        public static void CheckAPIKeyValidity(string apiKey, SubmitSignupLoginError errorDelegate, SubmitCredentialsSuccess successDelegate, object owner)
        {
            CheckAPIKeyValidityAsync(apiKey, errorDelegate, successDelegate).Forget();
        }

        public static void LogIn(string loginEmail, string loginPassword, SubmitSignupLoginError errorDelegate, SubmitCredentialsSuccess successDelegate, object owner)
        {
            LogInAsync(loginEmail, loginPassword, errorDelegate, successDelegate).Forget();
        }

        private static async UniTask CheckAPIKeyValidityAsync(string apiKey, SubmitSignupLoginError errorDelegate, SubmitCredentialsSuccess successDelegate)
        {
            credentialsSuccessDelegate += successDelegate;
            loginErrorDelegate += errorDelegate;
            signupLoginError = new SignupLoginError();

            try
            {
                if (string.IsNullOrEmpty(apiKey))
                {
                    signupLoginError = new SignupLoginError("Missing fields", "The API key field must be filled.");
                    loginErrorDelegate(signupLoginError);
                    loginErrorDelegate -= errorDelegate;
                    credentialsSuccessDelegate -= successDelegate;
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(new Exception("Error initializing signupErrors, returning", e));
                loginErrorDelegate -= errorDelegate;
                credentialsSuccessDelegate -= successDelegate;
                return;
            }

            var apiCall = NetworkConfig.APIKeyValidityUri(apiKey);
            var www = UnityWebRequest.Get(apiCall);
            www.timeout = 5;
            await www.SendWebRequest().ToUniTask();

            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    ParseAPIKeyValidity(apiKey);
                    ApplyLoginResponse(false);
                    credentialsSuccessDelegate();
                }
                catch { }
            }
            else
            {
                signupLoginError = new SignupLoginError("Invalid API key", "The API key was invalid.");
                loginErrorDelegate(signupLoginError);
            }
            www.Dispose();
            loginErrorDelegate -= errorDelegate;
            credentialsSuccessDelegate -= successDelegate;
        }

        private static async UniTask LogInAsync(string rawLoginEmail, string loginPass, SubmitSignupLoginError errorDelegate, SubmitCredentialsSuccess successDelegate)
        {
            credentialsSuccessDelegate += successDelegate;
            loginErrorDelegate += errorDelegate;
            signupLoginError = new SignupLoginError();

            var cleanedLoginEmail = rawLoginEmail.ToLower();

            try
            {
                if (string.IsNullOrEmpty(cleanedLoginEmail) || string.IsNullOrEmpty(loginPass))
                {
                    signupLoginError = new SignupLoginError("Missing fields", "The email and password fields must be filled.");
                    loginErrorDelegate(signupLoginError);
                    loginErrorDelegate -= errorDelegate;
                    credentialsSuccessDelegate -= successDelegate;
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(new System.Exception("Error initializing signupErrors, returning", e));
                loginErrorDelegate -= errorDelegate;
                credentialsSuccessDelegate -= successDelegate;
                return;
            }

            WWWForm form = new WWWForm();
            form.AddField("email", cleanedLoginEmail);
            form.AddField("password", loginPass);

            UnityWebRequest www = UnityWebRequest.Post("https://subscription-portal-backend.herokuapp.com/users/login", form);
            www.timeout = 15;
            await www.SendWebRequest().ToUniTask();

            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    ParseLoginResponse(www.downloadHandler.text);
                    ApplyLoginResponse();
                    credentialsSuccessDelegate();
                }
                catch { }
            }
            else
            {
                var errorResponse = JsonUtility.FromJson<LoginErrorResponse>(www.downloadHandler.text);
                ParseSignupLoginError(errorResponse);
                loginErrorDelegate(signupLoginError);
                if (AnythingSettings.DebugEnabled) Debug.LogError($"Error logging into Anything World: {www.downloadHandler.text}");
            }
            www.Dispose();
            loginErrorDelegate -= errorDelegate;
            credentialsSuccessDelegate -= successDelegate;
        }

        #region Parsers
        private static string fetchedEmail = "";
        private static string apiKey = "";
        private static void ParseLoginResponse(string text)
        {
            string cleanedText = Regex.Replace(text, @"[[\]]", "");
            string[] arr = cleanedText.Split(',');
            apiKey = arr[3].ToString().Split(':')[1].Trim('\"');
            fetchedEmail = arr[5].ToString().Split(':')[1].Trim('\"');
        }

        private static void ParseAPIKeyValidity(string text)
        {
            apiKey = text;
        }

        private static SignupLoginError signupLoginError;
        private static void ParseSignupLoginError(LoginErrorResponse error)
        {
            if (AnythingSettings.DebugEnabled) Debug.LogError($"Error code {error.code}: {error.msg}");
            signupLoginError = new SignupLoginError(error.code, error.msg);
        }

        private static void ApplyLoginResponse(bool hasEmail = true)
        {
            AnythingSettings.APIKey = apiKey;
            if (hasEmail) AnythingSettings.Email = fetchedEmail;
            Undo.RecordObject(AnythingSettings.Instance, $"Added API Key {(hasEmail ? "and Email " : "")}to AnythingSettings");
            EditorUtility.SetDirty(AnythingSettings.Instance);
        }
        #endregion Parsers
    }

    public class LoginErrorResponse
    {
        public string code = "";
        public string msg = "";
    }
}