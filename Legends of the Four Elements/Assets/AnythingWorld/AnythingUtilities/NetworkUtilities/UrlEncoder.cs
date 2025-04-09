namespace AnythingWorld.Utilities.Networking
{
    public static class UrlEncoder
    {
        /// <summary>
        /// Encode special characters in names so they are URL-safe.
        /// </summary>
        public static string Encode(string inputString)
        {
            if (inputString == null) return inputString;
            if (inputString == "") return inputString;
            char[] charsToTrim = { '*', ' ', '\'', ',', '.' };
            inputString = inputString.Trim(charsToTrim);

            var encodedString = System.Uri.EscapeUriString(inputString);
            if (encodedString.Contains("#"))
            {
                encodedString = encodedString.Replace("#", "%23");
            }
            if (encodedString.Contains(" "))
            {
                encodedString = encodedString.Replace(" ", "%20");
            }
            return encodedString;
        }
    }
}
