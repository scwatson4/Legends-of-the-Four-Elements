using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AnythingWorld.Editor
{
    public static class ScriptableObjectExtensions
    {
        /// <summary>
        /// Get all instances of scriptable objects with given type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static List<T> GetAllInstances<T>() where T : ScriptableObject
        {
            return AssetDatabase.FindAssets($"t: {typeof(T).Name}").ToList()
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .Select(AssetDatabase.LoadAssetAtPath<T>)
                        .ToList();
        }

        public static T GetInstance<T>() where T : ScriptableObject
        {
            return AssetDatabase.FindAssets($"t: {typeof(T).Name}").ToList()
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .Select(AssetDatabase.LoadAssetAtPath<T>).First();
        }
        public static bool TryGetInstance<T>(out T scriptableObject) where T : ScriptableObject
        {
            scriptableObject = AssetDatabase.FindAssets($"t: {typeof(T).Name}").ToList()
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .Select(AssetDatabase.LoadAssetAtPath<T>).First();
            return (scriptableObject != null);
        }
    }
}
