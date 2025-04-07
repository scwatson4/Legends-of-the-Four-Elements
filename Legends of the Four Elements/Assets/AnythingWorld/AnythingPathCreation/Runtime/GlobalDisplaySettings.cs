using UnityEngine;

namespace AnythingWorld.PathCreation
{
    /// <summary>
    /// ScriptableObject that stores settings for the path editor.
    /// </summary>
    public class GlobalDisplaySettings : ScriptableObject
    {
        public float defaultNormalsAngle = 90;
        
        [Header("Spline controls size")]
        public float anchorSize = 10;
        public float controlSize = 7f;

        [Header("Spline points positioning settings")]
        [Tooltip("Should anchor and control be snapped to surface as they are moved?")]
        public bool snapToSurface;
        [Tooltip("What orientation should be used when snapping points to surface")]
        public BezierPath.SurfaceOrientation surfaceOrientation = BezierPath.SurfaceOrientation.Up;
        
        [Header("Visibility options")]
        [Tooltip("Should the path still be drawn when behind objects in the scene?")]
        public bool visibleBehindObjects;
        [Tooltip("Should the path be drawn even when the path object is not selected?")]
        public bool visibleWhenNotSelected = true;
        [Tooltip( "Should the path be deselected when clicking outside of it?")]
        public bool deselectWhenClickingOutsideSpline;
        
        public const float NormalsLength = .2f;
        public readonly Color Anchor = new Color(0.95f, 0.25f, 0.25f, 0.85f);
        public readonly Color AnchorHighlighted = new Color(1, 0.57f, 0.4f);
        public readonly Color AnchorSelected = Color.white;
        public readonly Color Control = new Color(0.35f, 0.6f, 1, 0.85f);
        public readonly Color ControlHighlighted = new Color(0.8f, 0.67f, 0.97f);
        public readonly Color ControlSelected = Color.white;
        public readonly Color HandleDisabled = new Color(1, 1, 1, 0.2f);
        public readonly Color ControlLine = new Color(0, 0, 0, 0.35f);
        public readonly Color BezierPathColor = Color.green;
        public readonly Color HighlightedPath = new Color(1, 0.6f, 0);
        public readonly Color VertexPath = Color.white;
        public readonly Color Normals = Color.yellow;

#if UNITY_EDITOR
        /// <summary>
        /// Finds or creates the GlobalDisplaySettings asset and returns it.
        /// </summary>
        public static GlobalDisplaySettings Load() 
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:GlobalDisplaySettings");
            if (guids.Length == 0)
            {
                Debug.LogWarning("Could not find DisplaySettings asset. Will use default settings instead.");
                return CreateInstance<GlobalDisplaySettings>();
            }

            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GlobalDisplaySettings>(path);
        }
#endif

    }
}
