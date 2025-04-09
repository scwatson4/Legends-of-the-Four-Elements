using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace AnythingWorld.Utilities
{
    public static class StringExtensions
    {
        public static string DeepClean(this string str)
        {
            var rstr = str.Replace('\t', ' ').Replace('_', ' ');
            while (rstr.Contains("  "))
                rstr = rstr.Replace("  ", " ");
            return rstr.Trim();
        }

        public static string Capitalise(this string str) =>
            str switch
            {
                null => str,
                "" => str,
                _ => str[0].ToString().ToUpper() + str.Substring(1)
            };

        public static string CapitaliseAll(this string str) =>
            str switch
            {
                null => str,
                "" => str,
                _ => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str)
            };
    }
}
