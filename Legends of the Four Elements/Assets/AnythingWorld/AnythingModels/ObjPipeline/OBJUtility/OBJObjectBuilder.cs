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
using UnityEngine;

namespace AnythingWorld.ObjUtility
{
    public class OBJObjectBuilder
    {
        //
        public int PushedFaceCount { get; private set; } = 0;

        //stuff passed in by ctor
        private OBJLoader _loader;
        private string _name;

        private Dictionary<ObjLoopHash, int> _globalIndexRemap = new Dictionary<ObjLoopHash, int>();
        private Dictionary<string, List<int>> _materialIndices = new Dictionary<string, List<int>>();
        private List<int> _currentIndexList;
        private string _lastMaterial = null;

        //our local vert/normal/uv
        private List<Vector3> _vertices = new List<Vector3>();
        private List<Vector3> _normals = new List<Vector3>();
        private List<Vector2> _uvs = new List<Vector2>();

        //this will be set if the model has no normals or missing normal info
        private bool recalculateNormals = false;

        /// <summary>
        /// Loop hasher helper class
        /// </summary>
        private class ObjLoopHash
        {
            public int vertexIndex;
            public int normalIndex;
            public int uvIndex;

            public override bool Equals(object obj)
            {
                if (!(obj is ObjLoopHash))
                    return false;

                var hash = obj as ObjLoopHash;
                return (hash.vertexIndex == vertexIndex) && (hash.uvIndex == uvIndex) && (hash.normalIndex == normalIndex);
            }

            public override int GetHashCode()
            {
                var hc = 3;
                hc = unchecked(hc * 314159 + vertexIndex);
                hc = unchecked(hc * 314159 + normalIndex);
                hc = unchecked(hc * 314159 + uvIndex);
                return hc;
            }
        }


        /// <summary>
        /// Builds and returns a game object with mesh data and materials.
        /// </summary>
        /// <returns>The built game object.</returns>
        public GameObject Build()
        {
            // Create a new game object with the given name
            var go = new GameObject(_name);
            // Add a MeshRenderer component to the game object
            var mr = go.AddComponent<MeshRenderer>();

            // Create submeshes for each material index
            var submesh = 0;
            var materialArray = new Material[_materialIndices.Count];

            // Iterate through each material index
            foreach (var kvp in _materialIndices)
            {
                Material material = null;

                // If there are no materials in the loader, create a null material
                if (_loader.Materials == null)
                {
                    material = OBJLoaderHelper.CreateNullMaterial();
                    material.name = kvp.Key + " null replace";
                }
                else
                {
                    // If the material exists in the loader, use it
                    if (!_loader.Materials.TryGetValue(kvp.Key, out material))
                    {
                        // If the material does not exist in the loader, create a null material and add it to the loader
                        material = OBJLoaderHelper.CreateNullMaterial();
                        material.name = kvp.Key + " invalid replace";
                        _loader.Materials[kvp.Key] = material;
                    }
                }

                materialArray[submesh] = material;
                submesh++;
            }

            // Assign the material array to the MeshRenderer component
            mr.sharedMaterials = materialArray;

            // Add a MeshFilter component to the game object
            var mf = go.AddComponent<MeshFilter>();
            submesh = 0;

            // Create a new mesh and set its name, index format, and submesh count
            var msh = new Mesh()
            {
                name = _name,
                indexFormat = (_vertices.Count > 65535) ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16,
                subMeshCount = _materialIndices.Count
            };

            // Set the vertex, normal, and UV data for the mesh
            msh.SetVertices(_vertices);
            msh.SetNormals(_normals);
            msh.SetUVs(0, _uvs);

            // Set the triangle data for each submesh
            foreach (var kvp in _materialIndices)
            {
                msh.SetTriangles(kvp.Value, submesh);
                submesh++;
            }

            // Recalculate the mesh's normals, tangents, and bounds if necessary
            if (recalculateNormals)
                msh.RecalculateNormals();
            msh.RecalculateTangents();
            msh.RecalculateBounds();

            // Assign the mesh to the MeshFilter component and return the game object
            mf.sharedMesh = msh;
            return go;
        }

        public void SetMaterial(string name)
        {
            if (!_materialIndices.TryGetValue(name, out _currentIndexList))
            {
                _currentIndexList = new List<int>();
                _materialIndices[name] = _currentIndexList;
            }
        }

        ///<summary>
        ///Pushes a new face with the given material, vertex indices, normal indices, and UV indices.
        ///</summary>
        public void PushFace(string material, List<int> vertexIndices, List<int> normalIndices, List<int> uvIndices)
        {
            //invalid face size?
            if (vertexIndices.Count < 3)
            {
                return;
            }

            //set material
            if (material != _lastMaterial)
            {
                SetMaterial(material);
                _lastMaterial = material;
            }
            //reverse vertex indices for correct winding order (due to mirroring below)
            vertexIndices.Reverse();
            uvIndices.Reverse();
            normalIndices.Reverse();

            //remap indices to the global index list
            var indexRemap = new int[vertexIndices.Count];
            for (var i = 0; i < vertexIndices.Count; i++)
            {
                var vertexIndex = vertexIndices[i];
                var normalIndex = normalIndices[i];
                var uvIndex = uvIndices[i];

                var hashObj = new ObjLoopHash()
                {
                    vertexIndex = vertexIndex,
                    normalIndex = normalIndex,
                    uvIndex = uvIndex
                };
                var remap = -1;

                //if this vertex/normals/UVs combo hasn't been seen before, add it to the global list
                if (!_globalIndexRemap.TryGetValue(hashObj, out remap))
                {
                    _globalIndexRemap.Add(hashObj, _vertices.Count);
                    remap = _vertices.Count;

                    var vertex = (vertexIndex >= 0 && vertexIndex < _loader.Vertices.Count) ? _loader.Vertices[vertexIndex] : Vector3.zero;
                    var normal = (normalIndex >= 0 && normalIndex < _loader.Normals.Count) ? _loader.Normals[normalIndex] : Vector3.zero;
                    var uv = (uvIndex >= 0 && uvIndex < _loader.UVs.Count) ? _loader.UVs[uvIndex] : Vector2.zero;

                    //reflect the vertex position (x is mirrored in obj)
                    vertex = Reflect(vertex);

                    //add the vertex, normal, and UV data to the global lists
                    _vertices.Add(vertex);
                    _normals.Add(normal);
                    _uvs.Add(uv);

                    //mark the flag to recalculate normals if the normal index is negative
                    if (normalIndex < 0)
                        recalculateNormals = true;
                }

                indexRemap[i] = remap;
            }

            //add the remapped indices to the current index list
            if (indexRemap.Length == 3)
            {
                _currentIndexList.AddRange(new int[] { indexRemap[0], indexRemap[1], indexRemap[2] });
            }
            else if (indexRemap.Length == 4)
            {
                _currentIndexList.AddRange(new int[] { indexRemap[0], indexRemap[1], indexRemap[2] });
                _currentIndexList.AddRange(new int[] { indexRemap[2], indexRemap[3], indexRemap[0] });
            }
            else if (indexRemap.Length > 4)
            {
                for (var i = indexRemap.Length - 1; i >= 2; i--)
                {
                    _currentIndexList.AddRange(new int[] { indexRemap[0], indexRemap[i - 1], indexRemap[i] });
                }
            }

            PushedFaceCount++;
        }
        /// <summary>
        /// Reflects the given vector about the X axis (i.e. mirrors it across the XZ plane).
        /// </summary>
        /// <param name="vec">The vector to reflect.</param>
        /// <returns>The reflected vector.</returns>
        public Vector3 Reflect(Vector3 vec)
        {
            // Create a matrix to mirror the vector across the X axis.
            Matrix4x4 mirrorMatrix = Matrix4x4.identity;
            mirrorMatrix.m00 = -1;

            // Apply the matrix transformation to the vector.
            return mirrorMatrix.MultiplyPoint3x4(vec);
        }

        public OBJObjectBuilder(string name, OBJLoader loader)
        {
            _name = name;
            _loader = loader;
        }
    }
}