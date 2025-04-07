using UnityEngine;

namespace AnythingWorld.Utilities
{
    public class Tex2dUtils
    {
        /// <summary>
        /// Converts a texture to grayscale.
        /// </summary>
        public static Texture2D ConvertToGrayscale(Texture2D original)
        {
           // If the original texture is null, return null this happens when the texture is not processed yet.
           if(original == null) return null;
            Texture2D grayscaleImage = new Texture2D(original.width, original.height, TextureFormat.RGBA32, false);
            Color32[] originalPixels = original.GetPixels32();
            Color32[] grayscalePixels = new Color32[originalPixels.Length];

            for (int i = 0; i < originalPixels.Length; i++)
            {
                byte l = (byte)(0.2126f * originalPixels[i].r + 0.7152f * originalPixels[i].g + 0.0722f * originalPixels[i].b);
                grayscalePixels[i] = new Color32(l, l, l, originalPixels[i].a);
            }

            grayscaleImage.SetPixels32(grayscalePixels);
            grayscaleImage.Apply();

            return grayscaleImage;
        }
        /// <summary>
        /// Repositions an image to the specified offset.
        /// </summary>
        public static Texture2D RepositionImage(Texture2D image, int newHeight, Vector2Int offset)
        {
            Texture2D repositionedImage = new Texture2D(image.width, newHeight, TextureFormat.RGBA32, false);

            // Initialize the new texture with transparent black.
            Color[] clearPixels = new Color[repositionedImage.width * repositionedImage.height];
            for (int i = 0; i < clearPixels.Length; i++)
            {
                clearPixels[i] = Color.clear;
            }
            repositionedImage.SetPixels(clearPixels);
            repositionedImage.Apply();

            // Copy pixels from the original image to the new position
            for (int x = 0; x < image.width; x++)
            {
                for (int y = 0; y < image.height; y++)
                {
                    if (x + offset.x < repositionedImage.width && y + offset.y < repositionedImage.height)
                    {
                        repositionedImage.SetPixel(x + offset.x, y + offset.y, image.GetPixel(x, y));
                    }
                }
            }
            repositionedImage.Apply();
            return repositionedImage;
        }
        /// <summary>
        /// Scales an image to the specified width and height.
        /// </summary>
        public static Texture2D ScaleImage(Texture2D image, int newWidth, int newHeight)
        {
            Texture2D scaledImage = new Texture2D(newWidth, newHeight, image.format, false);

            float xRatio = image.width / (float)newWidth;
            float yRatio = image.height / (float)newHeight;

            for (int x = 0; x < newWidth; x++)
            {
                for (int y = 0; y < newHeight; y++)
                {
                    int newX = Mathf.Min((int)(x * xRatio), image.width - 1);
                    int newY = Mathf.Min((int)(y * yRatio), image.height - 1);
                    Color color = image.GetPixel(newX, newY);
                    scaledImage.SetPixel(x, y, color);
                }
            }
            scaledImage.Apply();
            return scaledImage;
        }
    }
}
