using AnythingWorld.Utilities.Networking;

namespace AnythingWorld.Utilities 
{
    public static class NetworkConfig
    {
        // Production api stem
        private const string AW_API_STEM = "https://api.anything.world";
        
        private static string ApiKey => AnythingSettings.APIKey;
        private static string AppName => AnythingSettings.AppName;
        public static string ApiUrlStem => AW_API_STEM;
        
        // Get the complete endpoint URI with the model name
        public static string GetNameEndpointUri(string modelName)
        {
            return $"{AW_API_STEM}/anything?key={ApiKey}&app={Encode(AppName)}&name={Encode(modelName)}";
        }
        
        // Get the complete endpoint URI with the model id
        public static string GetIdEndpointUri(string modelId)
        {
            return $"{AW_API_STEM}/anything?key={ApiKey}&app={Encode(AppName)}&id={Encode(modelId)}";
        }
        
        // Function to get the uri for the search endpoint
        public static string SearchUri(string searchTerm, string sortingType)
        {
            return $"{AW_API_STEM}/anything?key={ApiKey}&search={Encode(searchTerm)}{sortingType}&includeFiles=false&maxResultNum=-1";
        }
        
        // Get the Feature URI with key
        public static string FeaturedUri()
        {
            return $"{AW_API_STEM}/featured?key={ApiKey}";
        }
        
        public static string VoteUri(string voteType, string name, string id)
        {
            return $"{AW_API_STEM}/vote?key={ApiKey}&type={voteType}&name={name}&guid={id}"; 
        }
        
        public static string MyLikesUri()
        {
            return $"{AW_API_STEM}/voted?key={ApiKey}";
        }
        
        public static string ReportUri(string name, string id, string reason)
        {
            return $"{AW_API_STEM}/report?key={ApiKey}&name={name}&guid={id}&reason={reason}";
        }

        public static string SpeechToTextUri(string locale = "en-US")
        {
            return $"{AW_API_STEM}/speech-to-text?key={ApiKey}&locale={locale}";
        }
        
        public static string SpeechToCommandUri(string locale = "en-US")
        {
            return $"{AW_API_STEM}/parse-speech-command?key={ApiKey}&locale={locale}";
        }

        public static string TextToCommandUri()
        {
            return $"{AW_API_STEM}/parse-text-command?key={ApiKey}";
        }
        
        #region Collections
        public static string UserCollectionsUri(bool namesOnly)
        {
            return $"{AW_API_STEM}/user-collections?key={ApiKey}&onlyName={namesOnly.ToString().ToLower()}";
        }
        
        public static string AddCollectionUri(string collection)
        {
            return $"{AW_API_STEM}/add-collection?key={ApiKey}&collection={Encode(collection)}";
        }
        
        public static string RemoveCollectionUri(string collection)
        {
            return $"{AW_API_STEM}/remove-collection?key={ApiKey}&collection={Encode(collection)}";
        }
        
        public static string AddToCollectionUri(string collection, string name, string id)
        {
            return $"{AW_API_STEM}/add-to-collection?key={ApiKey}&collection={Encode(collection)}&name={name}&guid={id}";
        }
        
        public static string RemoveFromCollectionUri(string collection, string name, string id)
        {
            return $"{AW_API_STEM}/remove-from-collection?key={ApiKey}&collection={Encode(collection)}&name={name}&guid={id}";
        }
        
        #endregion Collections

        // User processed models following the:
        // https://anything-world.atlassian.net/wiki/spaces/AW/pages/865927169/Web+API+documentation#user-processed-models
        public static string GetUserProcessedUri(bool namesOnly)
        {
            return $"{AW_API_STEM}/user-processed-models?key={ApiKey}&onlyName={namesOnly.ToString().ToLower()}&includeFiles=false";
        }
        
        // User processed models following the:
        // https://anything-world.atlassian.net/wiki/spaces/AW/pages/865927169/Web+API+documentation#user-processed-model
        public static string GetUserProcessed(string id, string stage = null)
        {
            return $"{AW_API_STEM}/user-processed-model?key={ApiKey}&id={Encode(id)}{(stage != null ? $"&stage={stage}" : "")}";
        }
        
        public static string ProcessUserModel()
        {
            return $"{AW_API_STEM}/animate";
        }

        public static string APIKeyValidityUri(string apiKey)
        {
            return $"{AW_API_STEM}/has-valid-key?key={apiKey}";
        }

        private static string Encode(string str)
        {
            return UrlEncoder.Encode(str);
        }
    }
}
