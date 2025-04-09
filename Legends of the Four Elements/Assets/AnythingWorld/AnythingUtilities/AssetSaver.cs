using AnythingWorld.Behaviour.Tree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace AnythingWorld.Utilities
{
    public static class AssetSaver
    {
        private const string SavedAssetsFolder = "SavedAssets";
        private const string AnimationClipsFolder = "Animation Clips";
        private const string RootPath = "Assets/" + SavedAssetsFolder;

#if UNITY_EDITOR
        /// <summary>
        /// Creates an asset from the linked object in the callback data, only in the Unity Editor.
        /// </summary>
        /// <param name="callbackData">The callback data containing the linked GameObject.</param>
        public static void CreateAssetFromData(CallbackInfo callbackData)
        {
            CreateAssetFromGameObject(callbackData.linkedObject);
        }
        
        /// <summary>
        /// Tries to load a serialized prefab by asset name.
        /// </summary>
        /// <param name="assetName">Name of the asset to load.</param>
        /// <param name="prefab">The loaded prefab if found.</param>
        /// <returns>True if the prefab is loaded successfully, false otherwise.</returns>
        public static bool TryGetSerializedPrefab(string assetName, out GameObject prefab)
        {
            prefab = null;
            if (!CheckIfSavedAssetExists(assetName))
            {
                return false;
            }

            prefab = LoadAssetAtPath<GameObject>($"{RootPath}/{assetName}/{assetName}.prefab");
            return true;
        }
        
        public static void SerializeAnimationClips(Dictionary<string, AnimationClip> animationClips, string name)
        {
            Debug.Log("Serializing modern animation clips");

            List<string> paths = new List<string>();
            foreach (var kvp in animationClips)
            {
                if (kvp.Value == null) continue;
                //kvp.Value.legacy = true;
                TryCreateAsset<AnimationClip>(kvp.Value, kvp.Key, name, AnimationClipsFolder, out var path, out _);
                if (path != null) paths.Add(path);
            }
            
            // EditorUtility.SetDirty(animationComponent);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        /// <summary>
        /// Creates an asset from a GameObject by serializing its components and saving it as a prefab.
        /// </summary>
        /// <param name="streamedObject">The GameObject to be serialized and saved as an asset.</param>
        private static void CreateAssetFromGameObject(GameObject streamedObject)
        {
            if (CheckIfSavedAssetExists(streamedObject.name))
            {
                return;
            }
            
            CreateDefaultFolder();
            CreateFolder(RootPath, streamedObject.name);

            SerializeAnimator(streamedObject);
            SerializeSkinnedMeshRenderers(streamedObject);
            SerializeMeshRenderers(streamedObject);
            //copy BehaviourTree to asset and use
            BehaviourTree behaviourTree = streamedObject.GetComponent<BehaviourTreeInstanceRunner>().behaviourTree;
            TryCreateAsset<BehaviourTree>(behaviourTree, behaviourTree.name, streamedObject.name, "Behaviour Trees", out _, out _);
            PrefabUtility.SaveAsPrefabAssetAndConnect(streamedObject, 
                $"{RootPath}/{streamedObject.name}/{streamedObject.name}.prefab", InteractionMode.AutomatedAction);
            File.Create($"{Application.dataPath}/{SavedAssetsFolder}/{streamedObject.name}/.flag");
            Debug.Log($"Saved asset to {RootPath}/{streamedObject.name}");
        }

        
        
        /// <summary>
        /// Checks if a saved asset with the given name already exists.
        /// </summary>
        /// <param name="assetName">The asset name to check.</param>
        /// <returns>True if the asset exists, otherwise false.</returns>
        private static bool CheckIfSavedAssetExists(string assetName)
        {
            return File.Exists($"{Application.dataPath}/{SavedAssetsFolder}/{assetName}/.flag");
        }

        private static void SerializeAnimator(GameObject streamedObject)
        {
            if(streamedObject.GetComponentInChildren<Animator>())
            {
                var controller = streamedObject.GetComponentInChildren<Animator>().runtimeAnimatorController;
                TryCreateAsset<RuntimeAnimatorController>(controller, streamedObject.name, streamedObject.name, 
                    AnimationClipsFolder, out _, out _);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
        
        /// <summary>
        /// Serializes legacy animations from a GameObject, removes the existing animation component,
        /// saves the clips as assets, and re-adds the animation component with the new clips.
        /// </summary>
        /// <param name="streamedObject">The GameObject containing the legacy animation component.</param>
        /// <param name="callbackData">The callback data containing information about the loaded legacy animation clips.</param>
        private static void SerializeLegacyAnimations(GameObject streamedObject, CallbackInfo callbackData)
        {
            Debug.Log("Serializing legacy animations");
            if (streamedObject.GetComponentInChildren<Animation>())
            {
                var animationComponent = streamedObject.GetComponentInChildren<Animation>();
                var animationContainer = animationComponent.gameObject;
                GameObject.DestroyImmediate(animationComponent);

                List<string> paths = new List<string>();
                foreach (var kvp in callbackData.data.loadedData.gltf.animationClipsLegacy)
                {
                    if (kvp.Value == null) continue;
                    kvp.Value.legacy = true;
                    TryCreateAsset<AnimationClip>(kvp.Value, kvp.Key, streamedObject.name, "Legacy Animations", 
                        out var path, out _);
                    if (path != null) paths.Add(path);
                }

                animationComponent = animationContainer.AddComponent<Animation>();
                foreach (string path in paths)
                {
                    var clip = AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip)) as AnimationClip;
                    animationComponent.AddClip(clip, clip.name);
                }

                EditorUtility.SetDirty(animationComponent);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static void SerializeMeshRenderers(GameObject streamedObject)
        {
            if (streamedObject.GetComponentsInChildren<MeshRenderer>()?.Length > 0)
            {
                foreach (var meshRenderer in streamedObject.GetComponentsInChildren<MeshRenderer>())
                {
                    var gameObject = meshRenderer.gameObject;

                    List<Material> serializedSharedMaterials = new List<Material>();
                    foreach (Material mat in meshRenderer.sharedMaterials)
                    {
                        SerializeMaterialTextures(streamedObject, mat);
                        TryCreateAsset<Material>(mat, mat.name, streamedObject.name, "Materials", 
                            out _, out var material, false);
                        serializedSharedMaterials.Add(material);
                    }
                    meshRenderer.sharedMaterials = serializedSharedMaterials.ToArray();

                    if (meshRenderer.TryGetComponent<MeshFilter>(out var meshFilter) && meshFilter.sharedMesh != null)
                    {
                        var mesh = meshFilter.sharedMesh;
                        TryCreateAsset<Mesh>(mesh, mesh.name, streamedObject.name, "Meshes", out _, 
                            out var serializedMesh);
                        meshFilter.sharedMesh = serializedMesh;
                    }
                    else
                    {
                        Debug.LogWarning("Could not find mesh filter for mesh renderer:", meshRenderer.gameObject);
                    }
                }

            }

        }

        /// <summary>
        /// Serializes the mesh renderers and their associated materials and meshes within a GameObject,
        /// saving them as assets.
        /// </summary>
        /// <param name="streamedObject">The GameObject containing the mesh renderers to serialize.</param>
        private static void SerializeSkinnedMeshRenderers(GameObject streamedObject)
        {
            if (streamedObject.GetComponentInChildren<SkinnedMeshRenderer>())
            {
                var smRenderer = streamedObject.GetComponentInChildren<SkinnedMeshRenderer>();
                List<Material> serializedSharedMaterials = new List<Material>();
                foreach (Material mat in smRenderer.sharedMaterials)
                {
                    SerializeMaterialTextures(streamedObject, mat);
                    TryCreateAsset<Material>(mat, mat.name, streamedObject.name, "Materials", out _, 
                        out var serializedMaterial, false);
                    serializedSharedMaterials.Add(serializedMaterial);
                }

                smRenderer.sharedMaterials = serializedSharedMaterials.ToArray();

                if (streamedObject.GetComponentInChildren<SkinnedMeshRenderer>() && smRenderer.sharedMesh != null)
                {
                    var mesh = streamedObject.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
                    TryCreateAsset<Mesh>(mesh, mesh.name, streamedObject.name, "Meshes", out var path, out var serializedMesh);
                    streamedObject.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh = serializedMesh;
                }
            }
        }
        
        /// <summary>
        /// Serializes the textures used by a material, saving them as assets and updating the material with the serialized textures.
        /// </summary>
        /// <param name="streamedObject">The GameObject associated with the material being serialized.</param>
        /// <param name="mat">The material whose textures are being serialized.</param>
        private static void SerializeMaterialTextures(GameObject streamedObject, Material mat)
        {
            List<Tuple<Texture, string>> allTexture = GetTextures(mat);
            for (int i = 0; i < allTexture.Count; i++)
            {
                if (allTexture[i].Item1 == null) continue;
                TryCreateAsset<Texture>(allTexture[i].Item1, allTexture[i].Item2, streamedObject.name, 
                    "Textures", out _, out var texture);
                mat.SetTexture(allTexture[i].Item2, texture);
            }
        }

        private static List<Tuple<Texture, string>> GetTextures(Material mat)
        {
            List<Tuple<Texture, string>> allTexture = new List<Tuple<Texture, string>>();
            Shader shader = mat.shader;
            for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    var textureName = ShaderUtil.GetPropertyName(shader, i);
                    Texture texture = mat.GetTexture(textureName);

                    allTexture.Add(new Tuple<Texture, string>(texture, textureName));
                }
            }
            return allTexture;
        }

        /// <summary>
        /// Retrieves a list of all textures used by a material, along with their associated shader property names.
        /// </summary>
        /// <param name="mat">The material from which to extract the textures.</param>
        /// <returns>A list of tuples containing the texture and its corresponding shader property name.</returns>
        private static bool TryCreateAsset<T>(UnityEngine.Object asset, string name, string guid, string subFolder, 
            out string path, out T loadedAsset, bool allowDuplicate = true) where T : UnityEngine.Object
        {
            path = "";
            loadedAsset = null;

            if (asset == null || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(guid) || 
                string.IsNullOrWhiteSpace(subFolder))
            {
                Debug.LogError("Invalid input parameters for asset creation.");
                return false;
            }

            try
            {
                CreateDefaultFolder();
                string guidFolder = CreateFolder(RootPath, guid);
                CreateFolder(guidFolder, subFolder);
                string originalAssetPath = BuildAssetPath<T>(guid, subFolder, name, allowDuplicate);

                if (!IsAssetCreationAllowed<T>(originalAssetPath, asset, allowDuplicate, out path))
                {
                    loadedAsset = LoadAssetAtPath<T>(path);
                    return true;
                }

                if (CheckIfAssetExists(originalAssetPath))
                {
                    path = originalAssetPath;
                    loadedAsset = LoadAssetAtPath<T>(path);
                    return true;
                }

                string assetPath = CreateUniqueAssetPath(originalAssetPath);
                CreateAndSaveAsset(asset, assetPath);
                path = assetPath;
                loadedAsset = LoadAssetAtPath<T>(path);

                return true;
            }   
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }
        
        /// <summary>
        /// Builds the file path for saving an asset, ensuring the path is safe and optionally unique.
        /// </summary>
        /// <typeparam name="T">The type of asset being saved (e.g., AnimationClip, Mesh, Material).</typeparam>
        /// <param name="guid">The unique identifier for the asset.</param>
        /// <param name="subFolder">The subfolder where the asset will be saved.</param>
        /// <param name="name">The name of the asset.</param>
        /// <param name="uniquePaths">Determines whether to generate a unique path to avoid overwriting existing assets.</param>
        /// <returns>The generated file path for the asset.</returns>
        private static string BuildAssetPath<T>(string guid, string subFolder, string name, bool uniquePaths)
        {
            string extension = typeof(T) == typeof(AnimationClip) ? ".anim" : ".asset";
            string safeFilterName = GenerateSafeFilePath(name);
            string path = $"{RootPath}/{guid}/{subFolder}/{safeFilterName}{extension}";
            if(uniquePaths)
            {
                return AssetDatabase.GenerateUniqueAssetPath(path);
            }
            return path;
        }

        /// <summary>
        /// Determines if asset creation is allowed based on whether the asset already exists and if duplicates are permitted.
        /// </summary>
        /// <typeparam name="T">The type of the asset being checked.</typeparam>
        /// <param name="originalAssetPath">The original file path of the asset.</param>
        /// <param name="asset">The asset object being checked for existence.</param>
        /// <param name="allowDuplicate">Specifies if duplicate assets are allowed.</param>
        /// <param name="existingPath">Outputs the existing asset's path if it is found.</param>
        /// <returns>True if asset creation is allowed, false otherwise.</returns>
        private static bool IsAssetCreationAllowed<T>(string originalAssetPath, UnityEngine.Object asset, 
            bool allowDuplicate, out string existingPath) where T : UnityEngine.Object
        {
            existingPath = "";
            var isExistingAsset = CheckIfAssetExists(originalAssetPath);
            if (AssetDatabase.Contains(asset) || (!allowDuplicate && isExistingAsset))
            {
                existingPath = AssetDatabase.Contains(asset) ? AssetDatabase.GetAssetPath(asset) : originalAssetPath;
                Debug.Log($"{asset} already serialized within database.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Loads an asset at the specified path.
        /// </summary>
        /// <typeparam name="T">The type of the asset to load.</typeparam>
        /// <param name="path">The file path of the asset to load.</param>
        /// <returns>The loaded asset of type T.</returns>
        private static T LoadAssetAtPath<T>(string path) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        /// <summary>
        /// Creates and saves an asset to the specified path, and marks it as dirty for saving.
        /// </summary>
        /// <param name="asset">The asset to create and save.</param>
        /// <param name="assetPath">The file path where the asset will be saved.</param>
        private static void CreateAndSaveAsset(UnityEngine.Object asset, string assetPath)
        {
            AssetDatabase.CreateAsset(asset, assetPath);
            EditorUtility.SetDirty(asset);
        }

        /// <summary>
        /// Generates a unique asset path to avoid overwriting existing assets.
        /// </summary>
        /// <param name="originalAssetPath">The original asset path.</param>
        /// <returns>A unique asset path.</returns>
        private static string CreateUniqueAssetPath(string originalAssetPath)
        {
#if UNITY_2021_1_OR_NEWER
            return AssetDatabase.GenerateUniqueAssetPath(originalAssetPath);
#else
            return originalAssetPath;
#endif
        }

        /// <summary>
        /// Checks if an asset already exists at the given path.
        /// </summary>
        /// <param name="assetPath">The path of the asset to check.</param>
        /// <returns>True if the asset exists, false otherwise.</returns>
        private static bool CheckIfAssetExists(string assetPath)
        {
#if UNITY_2021_1_OR_NEWER
            return !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(assetPath, 
                AssetPathToGUIDOptions.OnlyExistingAssets));
#else
            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null;
#endif
        }

        /// <summary>
        /// Creates the default folder if it doesn't exist.
        /// </summary>
        /// <returns>The GUID of the default folder.</returns>
        private static string CreateDefaultFolder()
        {
            return AssetDatabase.IsValidFolder(RootPath) ? AssetDatabase.AssetPathToGUID(RootPath) : 
                CreateFolder("Assets", SavedAssetsFolder);
        }

        /// <summary>
        /// Creates a new folder within the specified root directory.
        /// </summary>
        /// <param name="rootDirectory">The root directory where the new folder will be created.</param>
        /// <param name="folderName">The name of the new folder.</param>
        /// <returns>The path to the newly created folder.</returns>
        static string CreateFolder(string rootDirectory, string folderName)
        {
            string newDirectory = rootDirectory + "/" + folderName;
            if (AssetDatabase.IsValidFolder(newDirectory)) return newDirectory;
            string guid = AssetDatabase.CreateFolder(rootDirectory, folderName);
            AssetDatabase.GUIDToAssetPath(guid);

            return newDirectory;
        }

        /// <summary>
        /// Generates a safe file path by removing invalid characters.
        /// </summary>
        /// <param name="inputPath">The original file path.</param>
        /// <returns>A safe file path without invalid characters.</returns>
        private static string GenerateSafeFilePath(string inputPath)
        {
            string illegalChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex($"[{Regex.Escape(illegalChars)}]");
            var safePath = r.Replace(inputPath, "");
            return safePath;
        }

        /// <summary>
        /// Attempts to get the path of an existing asset, generating a safe file name and checking if the asset exists.
        /// </summary>
        /// <typeparam name="T">The type of the asset.</typeparam>
        /// <param name="asset">The asset to check.</param>
        /// <param name="name">The name of the asset.</param>
        /// <param name="guid">The GUID of the asset.</param>
        /// <param name="path">Outputs the existing asset path if found.</param>
        /// <returns>True if the asset path exists, false otherwise.</returns>
        private static bool TryGetPath<T>(T asset, string name, string guid, out string path) where T : UnityEngine.Object
        {
            //CreateDefaultFolder();
            //CreateFolder(rootPath, guid);

            var safeFilterName = GenerateSafeFilePath(name);
            string extension = ".asset";
            if (typeof(T) == typeof(AnimationClip)) extension = ".anim";
            var assetPath = $"{RootPath}/{guid}/{safeFilterName}{extension}";

            if (AssetDatabase.LoadAssetAtPath(assetPath, typeof(T)))
            {
                path = AssetDatabase.GetAssetPath(asset);
                return true;
            }

            path = null;
            return false;
        }
#endif
    }
}


