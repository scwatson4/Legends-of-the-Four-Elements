using UnityEngine;

namespace AnythingWorld.PathCreation 
{
    ///<summary>
    /// Stores state data for the path creator editor.
    ///</summary>
    [System.Serializable]
    public class PathCreatorData 
    {
        public event System.Action BezierOrVertexPathModified;
        public event System.Action BezierCreated;

        [SerializeField]
        private BezierPath bezierPath;
        private VertexPath _vertexPath;

        [SerializeField]
        private bool vertexPathUpToDate;

        // Vertex path settings.
        public float vertexPathMaxAngleError = .3f;
        public float vertexPathMinVertexSpacing = 0.01f;

        // Bezier display settings.
        public float bezierHandleScale = 1;
        public bool globalDisplaySettingsFoldout;
        public bool keepConstantHandleSize;

        // Vertex display settings.
        public bool showNormalsInVertexMode;
        public bool showBezierPathInVertexMode;

        // Editor display states.
        public bool showDisplayOptions;
        public bool showPathOptions = true;
        public bool showVertexPathDisplayOptions;
        public bool showVertexPathOptions = true;
        public bool showNormals;
        public int tabIndex;

        private GlobalDisplaySettings _gds;

        ///<summary>
        /// Gets or sets the BezierPath object and invokes events when modified.
        ///</summary>
        public BezierPath BezierPath 
        {
            get => bezierPath;
            set
            {
                bezierPath.OnModified -= BezierPathEdited;
                vertexPathUpToDate = false;
                bezierPath = value;
                bezierPath.OnModified += BezierPathEdited;

                BezierOrVertexPathModified?.Invoke();

                BezierCreated?.Invoke();
            }
        }

        /// <summary>
        /// Initializes the PathCreatorData with the provided settings and path transform.
        /// </summary>
        /// <param name="isPathSnappedToSurface">Flag indicating if the path is snapped to a surface.</param>
        /// <param name="surfaceOrientation">The orientation of the surface the path is snapped to.</param>
        /// <param name="pathTransform">The transform of the path.</param>
        public void Initialize(GlobalDisplaySettings globalDisplaySettings, Transform pathTransform) 
        {
            _gds = globalDisplaySettings;
            if (bezierPath == null || !bezierPath.IsInitialized) 
            {
                CreateBezier(pathTransform);
            }

            globalDisplaySettingsFoldout = true;
            vertexPathUpToDate = false;
            bezierPath.OnModified -= BezierPathEdited;
            bezierPath.OnModified += BezierPathEdited;
        }

        /// <summary>
        /// Resets the BezierPath to a two-point 3D curve with the provided transform.
        /// </summary>
        /// <param name="pathTransform">The transform of the path.</param>
        public void ResetBezierPath(Transform pathTransform)
        {
            CreateBezier(pathTransform);
        }

        /// <summary>
        /// Creates a new BezierPath with the provided settings and invokes associated events.
        /// </summary>
        /// <param name="centre">The center position of the new BezierPath.</param>
        /// <param name="pathTransform">The transform of the path (optional).</param>
        /// <param name="isPathSnappedToSurface">Flag indicating if the path is snapped to a surface (default: false).</param>
        /// <param name="surfaceOrientation">The orientation of the surface the path is snapped to (default: Up).</param>
        private void CreateBezier(Transform pathTransform = null)
        {
            if (bezierPath != null && bezierPath.IsInitialized) 
            {
                bezierPath.OnModified -= BezierPathEdited;
            }
            
            bezierPath = new BezierPath(Vector3.zero, _gds.snapToSurface, _gds.surfaceOrientation, pathTransform, 
                _gds.defaultNormalsAngle);

            bezierPath.OnModified += BezierPathEdited;
            vertexPathUpToDate = false;

            BezierOrVertexPathModified?.Invoke();

            BezierCreated?.Invoke();
        }
        
        /// <summary>
        /// Gets the current VertexPath, creating a new one if necessary.
        /// </summary>
        /// <param name="transform">The transform of the path.</param>
        /// <returns>The current VertexPath.</returns>
        public VertexPath GetVertexPath(Transform transform) 
        {
            // Create new vertex path if path was modified since this vertex path was created.
            if (!vertexPathUpToDate || _vertexPath == null) 
            {
                vertexPathUpToDate = true;
                _vertexPath = new VertexPath(BezierPath, transform, vertexPathMaxAngleError, vertexPathMinVertexSpacing);
            }
            return _vertexPath;
        }

        /// <summary>
        /// Invokes the BezierOrVertexPathModified event when the path is transformed.
        /// </summary>
        public void PathTransformed()
        {
            BezierOrVertexPathModified?.Invoke();
        }

        /// <summary>
        /// Updates the vertexPathUpToDate flag and invokes the BezierOrVertexPathModified event when vertex path settings are changed.
        /// </summary>
        public void VertexPathSettingsChanged() 
        {
            vertexPathUpToDate = false;
            BezierOrVertexPathModified?.Invoke();
        }

        /// <summary>
        /// Updates the vertexPathUpToDate flag and invokes the BezierOrVertexPathModified event when the path is modified by an undo operation.
        /// </summary>
        public void PathModifiedByUndo() 
        {
            vertexPathUpToDate = false;
            BezierOrVertexPathModified?.Invoke();
        }

        /// <summary>
        /// Updates the vertexPathUpToDate flag and invokes the BezierOrVertexPathModified event when the BezierPath is edited.
        /// </summary>
        private void BezierPathEdited() 
        {
            vertexPathUpToDate = false;
            BezierOrVertexPathModified?.Invoke();
        }
    }
}
