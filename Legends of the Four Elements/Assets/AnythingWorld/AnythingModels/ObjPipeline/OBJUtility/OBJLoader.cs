
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

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
#endif

namespace AnythingWorld.ObjUtility
{
    public enum SplitMode
    {
        None,
        Object,
        Material
    }

    public class OBJLoader
    {
        //options
        /// <summary>
        /// Determines how objects will be created
        /// </summary>
        public SplitMode SplitMode = SplitMode.Object;

        //global lists, accessed by objobjectbuilder
        internal List<Vector3> Vertices = new List<Vector3>();
        internal List<Vector3> Normals = new List<Vector3>();
        internal List<Vector2> UVs = new List<Vector2>();

        //materials, accessed by objobjectbuilder
        internal Dictionary<string, Material> Materials;

        //file info for files loaded from file path, used for GameObject naming and MTL finding
        private FileInfo _objInfo;
        
        private MTLLoader mtlLoader;

        public OBJLoader()
        {
        }

        public GameObject Load(byte[] input)
        {
            Stream stream = new MemoryStream(input);

            return Load(stream);
        }

        /// <summary>
        /// Load an OBJ file from a stream. No materials will be loaded, and will instead be supplemented by a blank white material.
        /// </summary>
        /// <param name="input">Input OBJ stream</param>
        /// <returns>Returns a GameObject represeting the OBJ file, with each imported object as a child.</returns>
        public GameObject Load(Stream input)
        {
            var reader = new StreamReader(input);
            // var reader = new StringReader(inputReader.ReadToEnd());

            var builderDict = new Dictionary<string, OBJObjectBuilder>();
            OBJObjectBuilder currentBuilder = null;
            var currentMaterial = "default";

            //lists for face data
            //prevents excess GC
            var vertexIndices = new List<int>();
            var normalIndices = new List<int>();
            var uvIndices = new List<int>();

            //helper func
            Action<string> setCurrentObjectFunc = (string objectName) =>
            {
                if (!builderDict.TryGetValue(objectName, out currentBuilder))
                {
                    currentBuilder = new OBJObjectBuilder(objectName, this);
                    builderDict[objectName] = currentBuilder;
                }
            };

            //create default object
            setCurrentObjectFunc.Invoke("default");

            //var buffer = new DoubleBuffer(reader, 256 * 1024);
            var buffer = new CharWordReader(reader, 16 * 1024);

            //do the reading
            while (true)
            {
                buffer.SkipWhitespaces();

                if (buffer.endReached == true)
                {
                    break;
                }

                buffer.ReadUntilWhiteSpace();

                //comment or blank
                if (buffer.Is("#"))
                {
                    buffer.SkipUntilNewLine();
                    continue;
                }

                if (Materials == null && buffer.Is("mtllib"))
                {
                    Debug.LogWarning("Materials are null but have mtllib");
                    buffer.SkipWhitespaces();
                    buffer.ReadUntilNewLine();
                    var mtlLibPath = buffer.GetString();
                    // LoadMaterialLibrary(mtlLibPath);
                    continue;
                }

                if (buffer.Is("v"))
                {

                    var verts = buffer.ReadVector();
                    
                    Vertices.Add(verts);
                    continue;
                }

                //normal
                if (buffer.Is("vn"))
                {
                    Normals.Add(buffer.ReadVector());
                    continue;
                }

                //uv
                if (buffer.Is("vt"))
                {
                    UVs.Add(buffer.ReadVector());
                    continue;
                }

                //new material
                if (buffer.Is("usemtl"))
                {
                    // Debug.Log("create new material");
                    buffer.SkipWhitespaces();
                    buffer.ReadUntilNewLine();
                    var materialName = buffer.GetString();
                    currentMaterial = materialName;

                    if (SplitMode == SplitMode.Material)
                    {
                        setCurrentObjectFunc.Invoke(materialName);
                    }
                    continue;
                }

                //new object
                if ((buffer.Is("o") || buffer.Is("g")) && SplitMode == SplitMode.Object)
                {
                    buffer.ReadUntilNewLine();
                    var objectName = buffer.GetString(1);
                    setCurrentObjectFunc.Invoke(objectName);
                    continue;
                }

                //face data (the fun part)
                if (buffer.Is("f"))
                {
                    //loop through indices
                    while (true)
                    {
                        bool newLinePassed;
                        buffer.SkipWhitespaces(out newLinePassed);
                        if (newLinePassed == true)
                        {
                            break;
                        }

                        var vertexIndex = int.MinValue;
                        var normalIndex = int.MinValue;
                        var uvIndex = int.MinValue;

                        vertexIndex = buffer.ReadInt();

                        if (buffer.currentChar == '/')
                        {
                            buffer.MoveNext();
                            if (buffer.currentChar != '/')
                            {
                                uvIndex = buffer.ReadInt();
                            }
                            if (buffer.currentChar == '/')
                            {
                                buffer.MoveNext();
                                normalIndex = buffer.ReadInt();
                            }
                        }

                        //"postprocess" indices
                        if (vertexIndex > int.MinValue)
                        {
                            if (vertexIndex < 0)
                            {
                                vertexIndex = Vertices.Count - vertexIndex;
                            }
                            else
                            {
                                vertexIndex--;
                            }
                        }
                        if (normalIndex > int.MinValue)
                        {
                            if (normalIndex < 0)
                            {
                                normalIndex = Normals.Count - normalIndex;
                            }
                            else
                            {
                                normalIndex--;
                            }
                        }
                        if (uvIndex > int.MinValue)
                        {
                            if (uvIndex < 0)
                            {
                                uvIndex = UVs.Count - uvIndex;
                            }
                            else
                            {
                                uvIndex--;
                            }
                        }

                        //set array values
                        vertexIndices.Add(vertexIndex);
                        normalIndices.Add(normalIndex);
                        uvIndices.Add(uvIndex);
                    }
    
                    
                    //push to builder
                    currentBuilder.PushFace(currentMaterial, vertexIndices, normalIndices, uvIndices);

                    //clear lists
                    vertexIndices.Clear();
                    normalIndices.Clear();
                    uvIndices.Clear();

                    continue;
                }

                buffer.SkipUntilNewLine();
            }

            //finally, put it all together
            var obj = new GameObject("Loaded");
            obj.transform.localScale = new Vector3(1f, 1f, 1f);

            foreach (var builder in builderDict)
            {
                //empty object
                if (builder.Value.PushedFaceCount == 0)
                    continue;

                var builtObj = builder.Value.Build();
                builtObj.transform.localScale = new Vector3(1f, 1f, 1f);
                builtObj.transform.SetParent(obj.transform, false);
            }

            return obj;
        }

        /// <summary>
        /// Load an OBJ and MTL file from a stream.
        /// </summary>
        /// <param name="input">Input OBJ stream</param>
        /// /// <param name="mtlInput">Input MTL stream</param>
        /// <returns>Returns a GameObject represeting the OBJ file, with each imported object as a child.</returns>
        public GameObject Load(Stream input, Stream mtlInput, Dictionary<string, Texture> textureInput)
        {
            mtlLoader = new MTLLoader();
            Materials = mtlLoader.Load(mtlInput, textureInput);
            var objGo = Load(input);
            return objGo;
        }

        public GameObject Load(Stream input, Dictionary<string, Material> materialDict)
        {
            Materials = materialDict;
            if (input != null)
            {
                var objGo = Load(input);
                return objGo;
            }

            Debug.Log("No input from obj stream to load.");
            return null;
        }

        MTLLoader persistentMtlLoader = null;
        public GameObject LoadWithSameMaterials(Stream input, Stream mtlInput, Dictionary<string, Texture> textureInput)
        {
            if(persistentMtlLoader == null)
            {
                persistentMtlLoader = new MTLLoader();
                Materials = persistentMtlLoader.Load(mtlInput, textureInput);
            }
            var objGo = Load(input);
            return objGo;
        }
    }
}