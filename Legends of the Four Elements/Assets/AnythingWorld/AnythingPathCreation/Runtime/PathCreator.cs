using UnityEngine;

namespace AnythingWorld.PathCreation 
{
    ///<summary>
    /// This class stores data for the path editor, and provides accessors to get the current vertex and bezier path.
    /// Attach to a GameObject to create a new path editor.
    ///</summary>
    public class PathCreator : MonoBehaviour 
    { 
        public event System.Action PathUpdated;

        [HideInInspector]
        public PathCreatorData editorData;
        [HideInInspector]
        public bool initialized;

        private GlobalDisplaySettings _globalEditorDisplaySettings;

        // Vertex path created from the current bezier path.
        public VertexPath Path 
        { 
            get 
            {
                if (!initialized) 
                {
                    InitializeEditorData();
                }
                return editorData.GetVertexPath(transform);
            }
        }

        // The bezier path created in the editor.
        public BezierPath BezierPath 
        {
            get 
            {
                if (!initialized) 
                {
                    InitializeEditorData();
                }
                return editorData.BezierPath;
            }
            set 
            {
                if (!initialized) 
                {
                    InitializeEditorData();
                }
                editorData.BezierPath = value;
            }
        }

        public PathCreatorData EditorData => editorData;

        /// <summary>
        /// Used by the path editor to initialise some data.
        /// </summary>
        public void InitializeEditorData() 
        {
            if (editorData == null) 
            {
                editorData = new PathCreatorData();
            }
            editorData.BezierOrVertexPathModified -= TriggerPathUpdate;
            editorData.BezierOrVertexPathModified += TriggerPathUpdate;

#if UNITY_EDITOR
            if (!_globalEditorDisplaySettings)
            {
                _globalEditorDisplaySettings = GlobalDisplaySettings.Load();
            }
#endif
            
            editorData.Initialize(_globalEditorDisplaySettings, transform);
            initialized = true;
        }

        public void TriggerPathUpdate()
        {
            PathUpdated?.Invoke();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Draw the path when path objected is not selected (if enabled in settings).
        /// </summary>
        private void OnDrawGizmos() 
        {
            // Only draw path gizmo if the path object is not selected
            // (editor script is responsible for drawing when selected).
            GameObject selectedObj = UnityEditor.Selection.activeGameObject;
            if (selectedObj != gameObject) 
            {
                if (Path != null) 
                {
                    Path.UpdateTransform(transform);

                    if (_globalEditorDisplaySettings == null) 
                    {
                        _globalEditorDisplaySettings = GlobalDisplaySettings.Load();
                    }

                    if (_globalEditorDisplaySettings.visibleWhenNotSelected) 
                    {
                        Gizmos.color = _globalEditorDisplaySettings.BezierPathColor;

                        for (int i = 0; i < Path.NumPoints; i++) 
                        {
                            int nextI = i + 1;
                            if (nextI >= Path.NumPoints) 
                            {
                                if (Path.IsClosedLoop) 
                                {
                                    nextI %= Path.NumPoints;
                                } 
                                else 
                                {
                                    break;
                                }
                            }
                            Gizmos.DrawLine(Path.GetPoint(i), Path.GetPoint(nextI));
                        }
                    }
                }
            }
        }
#endif
    }
}
