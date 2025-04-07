using System.Globalization;
using UnityEngine;

namespace AnythingWorld.ObjUtility
{
    public static class OBJLoaderHelper
    {
        /// <summary>
        /// Enables transparency mode on standard materials
        /// </summary>
        public static void EnableMaterialTransparency(Material mtl)
        {
            // transparent set up for Anything World/Simple Lit/Simple Lit - GM
            mtl.SetFloat("_Surface", 1);
            mtl.SetFloat("_Blend", 0);
            mtl.SetOverrideTag("RenderType", "Transparent");
        }

        /// <summary>
        /// Modified from https://codereview.stackexchange.com/a/76891. Faster than float.Parse
        /// </summary>
        public static float FastFloatParse(string input)
        {
            if (input.Contains("e") || input.Contains("E"))
                return float.Parse(input, CultureInfo.InvariantCulture);

            float result = 0;
            var pos = 0;
            var len = input.Length;

            if (len == 0) return float.NaN;
            var c = input[0];
            float sign = 1;
            if (c == '-')
            {
                sign = -1;
                ++pos;
                if (pos >= len) return float.NaN;
            }

            while (true) // breaks inside on pos >= len or non-digit character
            {
                if (pos >= len) return sign * result;
                c = input[pos++];
                if (c < '0' || c > '9') break;
                result = (result * 10.0f) + (c - '0');
            }

            if (c != '.' && c != ',') return float.NaN;
            var exp = 0.1f;
            while (pos < len)
            {
                c = input[pos++];
                if (c < '0' || c > '9') return float.NaN;
                result += (c - '0') * exp;
                exp *= 0.1f;
            }
            return sign * result;
        }

        /// <summary>
        /// Modified from http://cc.davelozinski.com/c-sharp/fastest-way-to-convert-a-string-to-an-int. Faster than int.Parse
        /// </summary>
        public static int FastIntParse(string input)
        {
            var result = 0;
            var isNegative = (input[0] == '-');

            for (var i = (isNegative) ? 1 : 0; i < input.Length; i++)
                result = result * 10 + (input[i] - '0');
            return (isNegative) ? -result : result;
        }

        public static Material CreateNullMaterial()
        {
            var nullMaterial = new Material(Shader.Find("Standard"));
            //nullMaterial.SetColor("_BaseColor", Color.white);
            // nullMaterial.name = "Missing Material Replacement";
            return nullMaterial;
        }

        public static Vector3 VectorFromStrArray(string[] cmps)
        {
            var x = FastFloatParse(cmps[1]);
            var y = FastFloatParse(cmps[2]);
            if (cmps.Length == 4)
            {
                var z = FastFloatParse(cmps[3]);
                return new Vector3(x, y, z);
            }
            return new Vector2(x, y);
        }

        public static Color ColorFromStrArray(string[] cmps, float scalar = 1.0f)
        {
            var Kr = FastFloatParse(cmps[1]) * scalar;
            var Kg = FastFloatParse(cmps[2]) * scalar;
            var Kb = FastFloatParse(cmps[3]) * scalar;
            return new Color(Kr, Kg, Kb);
        }
    }
}