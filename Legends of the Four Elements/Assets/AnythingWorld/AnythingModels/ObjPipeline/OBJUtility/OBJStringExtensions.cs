using System;
using System.Globalization;
using UnityEngine;

namespace AnythingWorld.ObjUtility
{
    public static class OBJStringExtensions
    {
        public static string Clean(this string str)
        {
            var rstr = str.Replace('\t', ' ');
            while (rstr.Contains("  "))
                rstr = rstr.Replace("  ", " ");
            return rstr.Trim();
        }
    }
}
