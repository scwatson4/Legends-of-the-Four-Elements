using AnythingWorld.ObjUtility;
using AnythingWorld.Utilities.Data;
using System;
using System.Collections;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AnythingWorld.Models
{
    /// <summary>
    /// Provides functionality to load OBJ models asynchronously, handling textures, materials, and object parts.
    /// </summary>
    public static class ObjLoader
    {
        /// <summary>
        /// Loads the model data asynchronously, handling textures, materials, and object parts.
        /// </summary>
        /// <param name="data">The model data to load.</param>
        /// <param name="isStatic">Indicates whether the model is static or not.</param>
        public static async UniTask LoadAsync(ModelData data, bool isStatic)
        {
            await ObjTextureRequester.RequestAsync(data);

            if (data.isModelProcessingStopped)
            {
                return;
            }

            await ObjMtlRequester.RequestAsync(data);

            if (data.isModelProcessingStopped)
            {
                return;
            }

            if (isStatic)
            {
                await ObjBytesRequester.RequestSingleStaticAsync(data);
            }
            else
            {
                await ObjBytesRequester.RequestPartsAsync(data);
            }

            if (data.isModelProcessingStopped)
            {
                return;
            }

            if (isStatic)
            {
                BuildObjSingle(data);
            }
            else
            {
                BuildObjParts(data);
            }
        }

        /// <summary>
        /// Builds a single object from the model data.
        /// </summary>
        /// <param name="data">The model data containing the object parts.</param>
        private static void BuildObjSingle(ModelData data)
        {
            foreach (var kvp in data.loadedData.obj.partsBytes)
            {
                var loader = new OBJLoader();
                Stream stream = new MemoryStream(kvp.Value);
                try
                {
                    GameObject partGameObject = loader.Load(stream, new MemoryStream(data.loadedData.obj.mtlString),
                        data.loadedData.obj.loadedTextures);
                    partGameObject.name = kvp.Key;
                    data.loadedData.obj.loadedParts.Add(kvp.Key, partGameObject);
                    partGameObject.transform.parent = data.model.transform;

                }
                catch (Exception e)
                {
                    data.actions?.onFailureException(data, e, "Exception generated while loading OBJ.");
                    return;
                }
            }
        }

        /// <summary>
        /// Builds multiple object parts from the model data.
        /// </summary>
        /// <param name="data">The model data containing the object parts.</param>
        private static void BuildObjParts(ModelData data)
        {
            foreach (var kvp in data.loadedData.obj.partsBytes)
            {
                var loader = new OBJLoader();
                Stream stream = new MemoryStream(kvp.Value);

                GameObject partGameObject = loader.Load(stream, new MemoryStream(data.loadedData.obj.mtlString),
                    data.loadedData.obj.loadedTextures);
                partGameObject.name = kvp.Key;

                data.loadedData.obj.loadedParts.Add(kvp.Key, partGameObject);
                partGameObject.transform.parent = data.model.transform;
            }
        }
    }
}