using System;
using System.IO;
using UnityEngine;

namespace AnythingWorld.ObjUtility
{
    public static class DDSLoader
    {
        public static Texture2D Load(Stream ddsStream)
        {
            var buffer = new byte[ddsStream.Length];
            ddsStream.Read(buffer, 0, (int)ddsStream.Length);
            return Load(buffer);
        }

        public static Texture2D Load(string ddsPath)
        {
            return Load(File.ReadAllBytes(ddsPath));
        }

        public static Texture2D Load(byte[] ddsBytes)
        {
            try
            {

                //do size check
                var ddsSizeCheck = ddsBytes[4];
                if (ddsSizeCheck != 124)
                    throw new System.Exception("Invalid DDS header. Structure length is incrrrect."); //this header byte should be 124 for DDS image files

                //verify we have a readable tex
                var DXTType = ddsBytes[87];
                if (DXTType != 49 && DXTType != 53)
                    throw new System.Exception("Cannot load DDS due to an unsupported pixel format. Needs to be DXT1 or DXT5.");

                var height = ddsBytes[13] * 256 + ddsBytes[12];
                var width = ddsBytes[17] * 256 + ddsBytes[16];
                var mipmaps = ddsBytes[28] > 0;
                var textureFormat = DXTType == 49 ? TextureFormat.DXT1 : TextureFormat.DXT5;

                var DDS_HEADER_SIZE = 128;
                var dxtBytes = new byte[ddsBytes.Length - DDS_HEADER_SIZE];
                Buffer.BlockCopy(ddsBytes, DDS_HEADER_SIZE, dxtBytes, 0, ddsBytes.Length - DDS_HEADER_SIZE);

                var texture = new Texture2D(width, height, textureFormat, mipmaps);
                texture.LoadRawTextureData(dxtBytes);
                texture.Apply();

                return texture;
            }
            catch (System.Exception ex)
            {
                throw new Exception("An error occured while loading DirectDraw Surface: " + ex.Message);
            }
        }
    }
}
