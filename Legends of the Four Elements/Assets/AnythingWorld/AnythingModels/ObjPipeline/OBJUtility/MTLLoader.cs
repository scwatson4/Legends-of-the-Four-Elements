/*
 * Copyright (c) 2019 Dummiesman
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
*/



using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AnythingWorld.ObjUtility
{
    public class MTLLoader
    {
        public List<string> SearchPaths = new List<string>() { "%FileName%_Textures", string.Empty };
        private int GetArgValueCount(string arg)
        {
            switch (arg)
            {
                case "-bm":
                case "-clamp":
                case "-blendu":
                case "-blendv":
                case "-imfchan":
                case "-texres":
                    return 1;
                case "-mm":
                    return 2;
                case "-o":
                case "-s":
                case "-t":
                    return 3;
            }
            return -1;
        }

        private int GetTexNameIndex(string[] components)
        {
            for (var i = 1; i < components.Length; i++)
            {
                var cmpSkip = GetArgValueCount(components[i]);
                if (cmpSkip < 0)
                {
                    return i;
                }
                i += cmpSkip;
            }
            return -1;
        }

        private float GetArgValue(string[] components, string arg, float fallback = 1f)
        {
            var argLower = arg.ToLower();
            for (var i = 1; i < components.Length - 1; i++)
            {
                var cmp = components[i].ToLower();
                if (argLower == cmp)
                {
                    return OBJLoaderHelper.FastFloatParse(components[i + 1]);
                }
            }
            return fallback;
        }

        private string GetTexPathFromMapStatement(string processedLine, string[] splitLine)
        {
            var texNameCmpIdx = GetTexNameIndex(splitLine);
            if (texNameCmpIdx < 0)
            {
                Debug.LogError($"texNameCmpIdx < 0 on line {processedLine}. Texture not loaded.");
                return null;
            }
            var texNameIdx = processedLine.IndexOf(splitLine[texNameCmpIdx]);
            var texturePath = processedLine.Substring(texNameIdx);
            return texturePath;
        }
        private bool TryGetVectorArgFromSplitLine(string processedLine, string[] splitline, string arg, out Vector3 argVector)
        {
            argVector = Vector3.zero;
            //Debug.Log("looking for arg " + arg +" in "+processedLine);
            var argIndex = GetArgIndex(arg, splitline, processedLine);
            
            if (argIndex <= 0) return false;
            
            var argSubstring = processedLine.Substring(argIndex);
            
            var argSplit = argSubstring.Split(' ');
            
            if (argSplit.Length < 3) return false;
            
            for (var i = 1; i < 4; i++)
            {
                var result = OBJLoaderHelper.FastFloatParse(argSplit[i]);
                if (result == float.NaN) return false;
                Debug.Log($"Index {i} with value {argSplit[i]} being parsed to {result}");
                argVector[i - 1] = result;
            }
            return true;
        }
        private int GetArgIndex(string arg, string[] splitline, string processedLine)
        {
            var argLower = arg.ToLower();
            for (var i = 1; i < splitline.Length - 1; i++)
            {
                var cmp = splitline[i].ToLower();
                if (argLower != cmp) continue;
                
                var processedLineIndx = processedLine.IndexOf(cmp);
                return processedLineIndx;
            }
            return -1;
        }


        public Dictionary<string, Material> Load(Stream input, Dictionary<string, Texture> textures)
        {
            var reader = new StreamReader(input);
            var mtlDict = new Dictionary<string, Material>();
            Material currentMaterial = null;
            var dValueAssigned = false;
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var processedLine = line.Clean();
                var splitLine = processedLine.Split(' ');

                //blank or comment
                if (splitLine.Length < 2 || processedLine[0] == '#')
                    continue;


                // If a new material is specified in the MTL file, create a new Material object 
                // with the Universal Render Pipeline/Lit shader and assign it to the currentMaterial variable.
                if (splitLine[0] == "newmtl")
                {
                    var materialName = processedLine.Substring(7);
                    var newMtl = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = materialName };
                    mtlDict[materialName] = newMtl;
                    currentMaterial = newMtl;

                    // Reset the dValueAssigned variable to false to ensure a new value will be assigned if necessary.
                    dValueAssigned = false;
                    continue;
                }

                //anything past here requires a material instance
                if (currentMaterial == null)
                    continue;

                // This section of the code handles the diffuse color property of the material
                // If the first keyword in the split line is "Kd" or "kd"
                if (splitLine[0] == "Kd" || splitLine[0] == "kd")
                {
                    // Get the current color of the material
                    var currentColor = currentMaterial.GetColor("_BaseColor");
                    // Get the diffuse color from the split line
                    var kdColor = OBJLoaderHelper.ColorFromStrArray(splitLine);

                    // Set the new diffuse color of the material
                    currentMaterial.SetColor("_BaseColor", new Color(kdColor.r, kdColor.g, kdColor.b, currentColor.a));

                    // Continue to the next line of the MTL file
                    continue;
                }


                // diffuse map
                if (splitLine[0] == "map_Kd" || splitLine[0] == "map_kd" || splitLine[0] == "map-Kd" || splitLine[0] == "map-kd")
                {
                    // get the texture path from the map statement
                    var texturePath = GetTexPathFromMapStatement(processedLine, splitLine);

                    // check if there are scaling arguments and set the texture tiling accordingly
                    if (TryGetVectorArgFromSplitLine(processedLine, splitLine, "-s", out var scaleVector))
                    {
                        currentMaterial.SetVector("_Tiling", scaleVector);
                    }
                    // check if there are offset arguments and set the texture offset accordingly
                    if (TryGetVectorArgFromSplitLine(processedLine, splitLine, "-o", out var offsetVector))
                    {
                        currentMaterial.SetVector("_Offset", offsetVector);
                    }

                    // if there is no texture path, log an error and skip to the next line
                    if (texturePath == null)
                    {
                        Debug.Log("map_Kd specified but no texturePath found");
                        continue;
                    }

                    // check if the texture path exists in the textures dictionary
                    if (textures.ContainsKey(texturePath))
                    {
                        Texture loadedTex;
                        textures.TryGetValue(texturePath, out loadedTex);

                        // if the texture was loaded successfully, set it as the base map texture
                        if (loadedTex != null)
                        {
                            var KdTexture = loadedTex as Texture2D;
                            if (KdTexture != null)
                            {
                                currentMaterial.SetTexture("_BaseMap", KdTexture);
                            }
                            else
                            {
                                Debug.Log("Error loading texture" + texturePath + " to texture 2D");
                                continue;
                            }

                            //WORK AROUND: set kd to white if texture present and is pitch black
                            // if things are turning white instead of black this is the culprit. apologies.
                            if (currentMaterial.GetColor("_BaseColor") == Color.black)
                            {
                                currentMaterial.SetColor("_BaseColor", Color.white);
                            }
                        }
                        else
                        {
                            Debug.Log("Texture key \"" + texturePath + "\" found in dictionary but texture was not loaded.");
                        }
                    }

                    continue;
                }

                // This code block handles bump map textures in the OBJ file
                if (splitLine[0] == "map_Bump" || splitLine[0] == "map_bump")
                {
                    // Get the texture path from the statement
                    var texturePath = GetTexPathFromMapStatement(processedLine, splitLine);
                    if (texturePath == null)
                    {
                        continue; // Invalid arguments
                    }

                    // Check if the texture has already been loaded
                    if (textures.ContainsKey(texturePath))
                    {
                        textures.TryGetValue(texturePath, out var loadedTex);
                        if (loadedTex != null)
                        {
                            // Set the texture as the bump map and set the bump scale
                            var bumpTex = loadedTex as Texture2D;
                            if (bumpTex != null)
                            {
                                currentMaterial.SetTexture("_BumpMap", bumpTex);
                                var bumpScale = GetArgValue(splitLine, "-bm", 1.0f);
                                currentMaterial.SetFloat("_BumpScale", bumpScale);
                                currentMaterial.EnableKeyword("_NORMALMAP");
                            }
                            else
                            {
                                Debug.Log("Error loading texture " + texturePath + " to texture 2D");
                                continue;
                            }

                            // Enable material transparency if the bump texture is in DXT5 or ARGB32 format
                            if (bumpTex != null && (bumpTex.format == TextureFormat.DXT5 || bumpTex.format == TextureFormat.ARGB32))
                            {
                                OBJLoaderHelper.EnableMaterialTransparency(currentMaterial);
                            }
                        }
                        else
                        {
                            Debug.Log("Texture key \"" + texturePath + "\" found in dictionary but texture was not loaded.");
                        }
                    }

                    continue; // Continue parsing the OBJ file
                }
                // Check if the line specifies a bump map
                if (splitLine[0] == "bump" || splitLine[0] == "Bump")
                {
                    // Get the texture path from the last argument of the line
                    var texturePath = splitLine[splitLine.Length - 1];
                    if (texturePath == null)
                    {
                        continue; // Invalid arguments, skip processing
                    }
                    // Check if the texture is already loaded
                    if (textures.ContainsKey(texturePath))
                    {
                        textures.TryGetValue(texturePath, out var loadedTex);
                        if (loadedTex != null)
                        {
                            // Set the bump map texture and bump scale
                            var bumpTex = loadedTex as Texture2D;
                            if (bumpTex != null)
                            {
                                currentMaterial.SetTexture("_BumpMap", bumpTex);
                                var bumpScale = GetArgValue(splitLine, "-bm", 1.0f);
                                currentMaterial.SetFloat("_BumpScale", bumpScale);
                                currentMaterial.EnableKeyword("_NORMALMAP");
                            }
                            else
                            {
                                Debug.Log("Error loading texture" + texturePath + " to texture 2D");
                                continue;
                            }

                            // Enable material transparency if the bump map format requires it
                            if (bumpTex != null && (bumpTex.format == TextureFormat.DXT5 || bumpTex.format == TextureFormat.ARGB32))
                            {
                                OBJLoaderHelper.EnableMaterialTransparency(currentMaterial);
                            }

                        }
                    }
                }

                // alpha clipping
                if (splitLine[0] == "map_d" || splitLine[0] == "map_D")
                {
                    // Get the path to the texture file
                    var texturePath = GetTexPathFromMapStatement(processedLine, splitLine);

                    // Check if texturePath is null
                    if (texturePath == null)
                    {
                        // Skip this iteration of the loop
                        continue;
                    }

                    // Check if textures dictionary contains texturePath
                    if (textures.ContainsKey(texturePath))
                    {
                        // Try to get the texture associated with texturePath
                        textures.TryGetValue(texturePath, out var loadedTex);

                        // Check if loadedTex is not null
                        if (loadedTex != null)
                        {
                            // Cast loadedTex to Texture2D
                            var alphaTex = loadedTex as Texture2D;

                            // Check if alphaTex is not null
                            if (alphaTex != null)
                            {
                                // Set the alpha texture for the current material
                                currentMaterial.SetTexture("_AlphaMap", alphaTex);
                            }
                            else
                            {
                                // Log an error message and skip this iteration of the loop
                                Debug.Log("Error loading texture" + texturePath + " to texture 2D");
                            }
                        }
                        else
                        {
                            // Log an error message and skip this iteration of the loop
                            Debug.Log("Texture key \"" + texturePath + "\" found in dictionary but texture was not loaded.");
                        }
                    }

                    // Skip to the next iteration of the loop
                    continue;
                }

                //specular color
                if (splitLine[0] == "Ks" || splitLine[0] == "ks")
                {
                    // Get the specular color from the split line array.
                    var specularColor = OBJLoaderHelper.ColorFromStrArray(splitLine);

                    // Check if the color is not null and set it as the specular color of the current material.
                    if (specularColor != null)
                    {
                        currentMaterial.SetColor("_SpecColor", specularColor);
                    }

                    // Continue to the next iteration of the loop.
                    continue;
                }


                //emission color
                if (splitLine[0] == "Ka" || splitLine[0] == "ka")
                {
                    // Get the emission color from the string array, with a fallback value of 0.05f if it fails
                    var emissionColor = OBJLoaderHelper.ColorFromStrArray(splitLine, 0.05f);
                    // If emission color is successfully parsed from the line
                    if (emissionColor != null)
                    {
                        // Enable emission keyword for current material
                        currentMaterial.EnableKeyword("_EmissionColor");
                        // Set the emission color of current material
                        currentMaterial.SetColor("_EmissionColor", emissionColor);
                    }

                    // Continue to the next line of the OBJ file
                    continue;
                }

                //ambient color map/emission map 
                if (splitLine[0] == "map_Ka" || splitLine[0] == "map_ka")
                {
                    // Get the texture path from the map statement
                    var texturePath = GetTexPathFromMapStatement(processedLine, splitLine);

                    // Check if texture path is invalid
                    if (texturePath == null)
                    {
                        continue;
                    }

                    // Check if texture is already loaded
                    if (textures.ContainsKey(texturePath))
                    {
                        // Get the loaded texture
                        textures.TryGetValue(texturePath, out var loadedTex);

                        // Check if texture is not null
                        if (loadedTex != null)
                        {
                            // Cast the loaded texture to Texture2D
                            var emissionTex = loadedTex as Texture2D;

                            // Check if cast was successful
                            if (emissionTex != null)
                            {
                                // Set the material's emission texture
                                currentMaterial.SetTexture("_EmissionMap", emissionTex);
                            }
                            else
                            {
                                // Continue to next line if cast was unsuccessful
                                continue;
                            }

                            // Enable transparency if texture format is DXT5 or ARGB32
                            if (emissionTex != null && (emissionTex.format == TextureFormat.DXT5 || emissionTex.format == TextureFormat.ARGB32))
                            {
                                OBJLoaderHelper.EnableMaterialTransparency(currentMaterial);
                            }
                        }
                    }
                    else
                    {
                        // Continue to next line if texture is not already loaded
                        continue;
                    }
                }


                //specular map
                if (splitLine[0] == "map_Ks" || splitLine[0] == "map_ks")
                {
                    // Get the texture path from the map statement in the line
                    var texturePath = GetTexPathFromMapStatement(processedLine, splitLine);

                    // Check if the texture path is valid, if not skip to the next line
                    if (texturePath == null)
                    {
                        continue;
                    }

                    // Check if the textures dictionary contains the texture path
                    if (textures.ContainsKey(texturePath))
                    {
                        // Attempt to retrieve the loaded texture from the textures dictionary
                        textures.TryGetValue(texturePath, out var loadedTex);

                        if (loadedTex != null)
                        {
                            // Check if the loaded texture is a Texture2D
                            var specularMap = loadedTex as Texture2D;
                            if (specularMap != null)
                            {
                                // Set the loaded texture as the specular map for the current material
                                currentMaterial.SetTexture("_SpecularMap", specularMap);
                            }
                            else
                            {
                                // If the loaded texture is not a Texture2D, skip to the next line
                                continue;
                            }

                            // If the loaded texture format is DXT5 or ARGB32, enable material transparency
                            if (specularMap != null && (specularMap.format == TextureFormat.DXT5 || specularMap.format == TextureFormat.ARGB32))
                            {
                                OBJLoaderHelper.EnableMaterialTransparency(currentMaterial);
                            }
                        }
                        else
                        {
                            Debug.Log("Texture key \"" + texturePath + "\" found in dictionary but texture was not loaded.");
                        }
                    }
                    else
                    {
                        continue;
                    }
                }

                if (splitLine[0] == "d" || splitLine[0] == "D" || splitLine[0] == "Tr" || splitLine[0] == "tr")
                {
                    // Prefer D values over Tr values as if both present, D more likely to be correct.
                    if (splitLine[0] == "Tr" || splitLine[0] == "tr" && dValueAssigned)
                    {
                        // Skip Tr statement if D value already assigned.
                        continue;
                    }

                    // Get visibility value from string array and convert to float.
                    var visibility = OBJLoaderHelper.FastFloatParse(splitLine[1]);

                    // Tr statement is just D inverted.
                    if (splitLine[0] == "Tr" || splitLine[0] == "tr")
                    {
                        visibility = 1f - visibility;
                    }
                    else
                    {
                        // If D value, toggle bool that D has been assigned so future Tr does not overwrite it.
                        dValueAssigned = true;
                    }

                    if (visibility < 1f - Mathf.Epsilon)
                    {
                        // If visibility is less than 1, set material to STANDARD_TRANSPARENT shader and adjust alpha.
                        if (AnythingWorld.ObjUtility.MaterialCacheUtil.MaterialCacheDict.TryGetValue("STANDARD_TRANSPARENT", out var template))
                        {
                            var newMat = new Material(template);
                            newMat.name = currentMaterial.name;
                            if (mtlDict.ContainsKey(currentMaterial.name))
                            {
                                mtlDict[currentMaterial.name] = newMat;
                            }
                            else
                            {
                                Debug.LogWarning($"Mtl dict does not contain key:{currentMaterial.name}");
                            }
                            currentMaterial = newMat;
                        }
                        var currentColor = currentMaterial.GetColor("_BaseColor");
                        currentColor.a = visibility;
                        currentMaterial.SetColor("_BaseColor", currentColor);
                    }
                }

                if (splitLine[0] == "Ns" || splitLine[0] == "ns")
                {
                    var glossiness = OBJLoaderHelper.FastFloatParse(splitLine[1]);
                    glossiness = (glossiness / 1000f);
                    currentMaterial.SetFloat("_Glossiness", glossiness);
                }
            }
            reader.Close();
            return mtlDict;
        }
    }
}
