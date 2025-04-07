using UnityEngine;

namespace AnythingWorld.ObjUtility
{
    public static class ImageUtils
    {
        public static Texture2D ConvertToNormalMap(Texture2D tex)
        {
            var returnTex = tex;
            if (tex.format != TextureFormat.RGBA32 && tex.format != TextureFormat.ARGB32)
            {
                returnTex = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, true);
            }

            var pixels = tex.GetPixels();
            for (var i = 0; i < pixels.Length; i++)
            {
                var temp = pixels[i];
                temp.a = pixels[i].r;
                temp.r = 0f;
                temp.g = pixels[i].g;
                temp.b = 0f;
                pixels[i] = temp;
            }

            returnTex.SetPixels(pixels);
            returnTex.Apply(true);
            return returnTex;
        }

    }
}

