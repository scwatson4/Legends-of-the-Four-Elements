using System;
using UnityEditor;

namespace AnythingWorld
{
    [System.Serializable]
    public class FolderReference
    {
        public string GUID;
        public string Path => AssetDatabase.GUIDToAssetPath(GUID);
    }
}