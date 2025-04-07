using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Provides utility functions for behavior tree editor operations such as creating new trees, nodes, scripts,
    /// and loading assets.
    /// </summary>
    public static class BehaviourTreeEditorUtility
    {
        /// <summary>
        /// Represents a script template with associated default file name and subfolder location.
        /// </summary>
        public struct ScriptTemplate
        {
            public TextAsset templateFile;
            public string defaultFileName;
            public string subFolder;
        }

        /// <summary>
        /// Creates a new Behavior Tree asset at the specified location with the given name.
        /// </summary>
        public static BehaviourTree CreateNewTree(string assetName, string folder)
        {
#if UNITY_2021_3_OR_NEWER
            string path = Path.Join(folder, $"{assetName}.asset");
#else
            string path = Path.Combine(folder, $"{assetName}.asset");
#endif
            string assetPath = null;
            
            if (File.Exists(path))
                assetPath = AssetDatabase.GenerateUniqueAssetPath(path);

            if (!string.IsNullOrEmpty(assetPath))
            {
                assetName = Path.GetFileNameWithoutExtension(assetPath);
                Debug.LogWarning($"Asset at path: \"{path}\" already exists, creating \"{assetName}.asset\" instead.");
                path = assetPath;
            }
            
            BehaviourTree tree = ScriptableObject.CreateInstance<BehaviourTree>();
            tree.name = assetName;
            AssetDatabase.CreateAsset(tree, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(tree);
            return tree;
        }

        /// <summary>
        /// Creates a node with the specified type and position in the provided tree view, and selects it.
        /// </summary>
        public static void CreateAndSelectNode(NodeView source, BehaviourTreeView treeView, Type nodeType, 
            Vector2 nodePosition, bool isSourceParent)
        {
            NodeView createdNode;
            if (source != null)
            {
                if (isSourceParent)
                {
                    createdNode = treeView.CreateNode(nodeType, nodePosition, source);
                }
                else
                {
                    createdNode = treeView.CreateNodeWithChild(nodeType, nodePosition, source);
                }
            }
            else
            {
                createdNode = treeView.CreateNode(nodeType, nodePosition, null);
            }

            treeView.SelectNode(createdNode);
        } 

        /// <summary>
        /// Initiates the creation of a new script based on the provided template and associated with a specific node.
        /// </summary>
        public static void CreateNewScript(ScriptTemplate scriptTemplate, NodeView source, bool isSourceParent, Vector2 position)
        {
            BehaviourTreeEditorWindow.Instance.newScriptDialog.CreateScript(scriptTemplate, source, isSourceParent, position);
        }

        public static List<string> GetAssetPaths<T>() where T : UnityEngine.Object
        {
            string[] assetIds = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            List<string> paths = new List<string>();
            foreach (var assetId in assetIds)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetId);
                paths.Add(path);
            }
            return paths;
        }

        /// <summary>
        /// Rounds a given floating-point value to the nearest specified integer.
        /// </summary>
        public static float RoundTo(float value, int nearestInteger)
        {
            return (Mathf.FloorToInt(value / nearestInteger)) * nearestInteger;
        }
        
#if !UNITY_2021_3_OR_NEWER     
        /// <summary>
        /// Gets the target object of a serialized property, accounting for nested properties and arrays.
        /// </summary>
        public static object GetTargetObjectOfProperty(SerializedProperty prop)
        {
            var path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            var elements = path.Split('.');
            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetValue_Imp(obj, elementName, index);
                }
                else
                {
                    obj = GetValue_Imp(obj, element);
                }
            }
            return obj;
        }
 
        /// <summary>
        /// Helper method to retrieve a field or property value from an object by name.
        /// </summary>
        private static object GetValue_Imp(object source, string name)
        {
            if (source == null)
                return null;
            var type = source.GetType();
 
            while (type != null)
            {
                var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (f != null)
                    return f.GetValue(source);
 
                var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null)
                    return p.GetValue(source, null);
 
                type = type.BaseType;
            }
            return null;
        }
 
        /// <summary>
        /// Helper method to retrieve a value from a field or property that is an array or collection by index.
        /// </summary>
        private static object GetValue_Imp(object source, string name, int index)
        {
            var enumerable = GetValue_Imp(source, name) as System.Collections.IEnumerable;
            if (enumerable == null) return null;
            var enm = enumerable.GetEnumerator();
            //while (index-- >= 0)
            //    enm.MoveNext();
            //return enm.Current;
 
            for (int i = 0; i <= index; i++)
            {
                if (!enm.MoveNext()) return null;
            }
            return enm.Current;
        }
#endif 
    }
}
