using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Rendering;

namespace AnythingWorld.PathCreation
{
    /// <summary>
    /// Editor class for the creation of Bezier and Vertex paths.
    /// </summary>
    [CustomEditor(typeof(PathCreator))]
    public class PathEditor : Editor
    {
        // Interaction:
        private const float SegmentSelectDistanceThreshold = 10f;
        private const float ScreenPolylineMaxAngleError = .3f;
        private const float ScreenPolylineMinVertexDst = .01f;

        // Help messages:
        private static readonly string[] SpaceNames = { "3D (xyz)", "Flat (xz)" };
        private const string HelpInfo = "Shift-click to add to end or insert new points. Shift-Control-click to add a point to start." +
                                        " Control-click to delete points. " +
                                        "For more detailed information, please refer to the documentation.";
        private static readonly string[] TabNames = { "Bézier Path", "Vertex Path" };
        private const string ConstantSizeTooltip = "If true, anchor and control points will keep a constant size when " +
                                                   "zooming in the editor.";
        
        private const int BezierPathTab = 0;
        private const int VertexPathTab = 1;
        
        // Display:
        private const int InspectorSectionSpacing = 10;
        private const float ConstantHandleScale = .01f;
        private const float NormalsSpacing = .2f;
        private GUIStyle _boldFoldoutStyle;

        private BezierPath BezierPath => Data.BezierPath;
        private PathCreatorData Data => _creator.EditorData;
        
        // References:
        private PathCreator _creator;
        private Editor _globalDisplaySettingsEditor;
        private ScreenSpacePolyLine _screenSpaceLine;
        private ScreenSpacePolyLine.MouseInfo _pathMouseInfo;
        private GlobalDisplaySettings _globalDisplaySettings;
        private PathHandle.HandleColours _splineAnchorColours;
        private PathHandle.HandleColours _splineControlColours;
        private ArcHandle _anchorAngleHandle = new ArcHandle();
        private VertexPath _normalsVertexPath;

        // State variables:
        private int _selectedSegmentIndex;
        private int _draggingHandleIndex;
        private int _mouseOverHandleIndex;
        private int _handleIndexToDisplayAsTransform;

        private bool _shiftLastFrame;
        private bool _hasUpdatedScreenSpaceLine;
        private bool _hasUpdatedNormalsVertexPath;
        private bool _editingNormalsOld;

        private Vector3 _transformPos;
        private Vector3 _prevHandlePos;
        private Vector3 _transformScale;
        private Vector3 _transformGlobalScale;
        private Quaternion _transformRot;

        private Color _handlesStartCol;
        
        /// <summary>
        /// Initializes GUI styles and displays custom inspector GUI elements.
        /// It handles tab switching and draws the appropriate inspector based on the selected tab.
        /// </summary>
        public override void OnInspectorGUI()
        {
            // Initialize GUI styles.
            if (_boldFoldoutStyle == null)
            {
                _boldFoldoutStyle = new GUIStyle(EditorStyles.foldout);
                _boldFoldoutStyle.fontStyle = FontStyle.Bold;
            }

            Undo.RecordObject(_creator, "Path settings changed");

            // Draw Bezier and Vertex tabs.
            int tabIndex = GUILayout.Toolbar(Data.tabIndex, TabNames);

            if (tabIndex != Data.tabIndex)
            {
                Data.tabIndex = tabIndex;
                TabChanged();
            }
            
            // Draw inspector for active tab.
            switch (Data.tabIndex)
            {
                case BezierPathTab:
                    DrawBezierPathInspector();
                    break;
                case VertexPathTab:
                    DrawVertexPathInspector();
                    break;
            }

            // Notify of undo/redo that might modify the path.
            if (Event.current.type == EventType.ValidateCommand && Event.current.commandName == "UndoRedoPerformed")
            {
                Data.PathModifiedByUndo();
            }
        }

       
        /// <summary>
        /// Draws the inspector for the Bezier path, including path options, normals, and display settings.
        /// Handles user input for modifying the Bezier path and updates the scene view accordingly.
        /// </summary>
        private void DrawBezierPathInspector()
        {
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.HelpBox(HelpInfo, MessageType.Info);
                GUILayout.Space(InspectorSectionSpacing);
                
                // Path options:
                Data.showPathOptions = EditorGUILayout.Foldout(Data.showPathOptions, 
                    new GUIContent("Bézier Path Options"), true, _boldFoldoutStyle);
                
                if (Data.showPathOptions)
                {
                    BezierPath.Space = (PathSpace)EditorGUILayout.Popup("Space", (int)BezierPath.Space, SpaceNames);
                    
                    BezierPath.ControlPointMode = (BezierPath.ControlMode)EditorGUILayout.EnumPopup(
                        new GUIContent("Control Mode"), BezierPath.ControlPointMode);
                    
                    if (BezierPath.ControlPointMode == BezierPath.ControlMode.Automatic)
                    {
                        BezierPath.AutoControlLength = EditorGUILayout.Slider(new GUIContent("Control Spacing"), 
                            BezierPath.AutoControlLength, 0, 1);
                    }
                    
                    BezierPath.IsClosed = EditorGUILayout.Toggle("Closed Path", BezierPath.IsClosed);
                    
                    if (BezierPath.Space == PathSpace.XYZ)
                    {
                        BezierPath.isSnappedToSurface = EditorGUILayout.Toggle("Snap To Surface", 
                            BezierPath.isSnappedToSurface);
                        
                        if (BezierPath.isSnappedToSurface)
                        {
                            BezierPath.surfaceOrientation = (BezierPath.SurfaceOrientation)EditorGUILayout.EnumPopup(
                                new GUIContent("Surface Orientation"), BezierPath.surfaceOrientation);
                        }
                    }
                    
                    // Check if out of bounds (can occur after undo operations).
                    if (_handleIndexToDisplayAsTransform >= BezierPath.NumPoints)
                    {
                        _handleIndexToDisplayAsTransform = -1;
                    }

                    // If a point has been selected.
                    if (_handleIndexToDisplayAsTransform != -1)
                    {
                        EditorGUILayout.LabelField("Selected Point:");

                        using (new EditorGUI.IndentLevelScope())
                        {
                            var currentPosition = _creator.BezierPath[_handleIndexToDisplayAsTransform];
                            var newPosition = EditorGUILayout.Vector3Field("Position", currentPosition);
                            if (newPosition != currentPosition)
                            {
                                Undo.RecordObject(_creator, "Move point");
                                _creator.BezierPath.MovePoint(_handleIndexToDisplayAsTransform, newPosition);
                            }
                            // Don't draw the angle field if we aren't selecting an anchor point/not in 3d space.
                            if (_handleIndexToDisplayAsTransform % 3 == 0 && _creator.BezierPath.Space == PathSpace.XYZ)
                            {
                                var anchorIndex = _handleIndexToDisplayAsTransform / 3;
                                var currentAngle = _creator.BezierPath.GetAnchorNormalAngle(anchorIndex);
                                var newAngle = EditorGUILayout.FloatField("Angle", currentAngle);
                                if (newAngle != currentAngle)
                                {
                                    Undo.RecordObject(_creator, "Set Angle");
                                    _creator.BezierPath.SetAnchorNormalAngle(anchorIndex, newAngle);
                                }
                            }
                        }
                    }

                    if (_handleIndexToDisplayAsTransform == -1)
                    {
                        if (GUILayout.Button("Centre Transform"))
                        {
                            Vector3 worldCentre = BezierPath.CalculateBoundsWithTransform(_creator.transform).center;
                            Vector3 transformPos = _creator.transform.position;

                            Vector3 worldCentreToTransform = transformPos - worldCentre;

                            if (worldCentre != _creator.transform.position)
                            {
                                if (worldCentreToTransform != Vector3.zero)
                                {
                                    Vector3 localCentreToTransform = MathUtility.InverseTransformVector(
                                        worldCentreToTransform, _creator.transform, BezierPath.Space);
                                    for (int i = 0; i < BezierPath.NumPoints; i++)
                                    {
                                        BezierPath.SetPoint(i, BezierPath.GetPoint(i) + localCentreToTransform, true);
                                    }
                                }

                                _creator.transform.position = worldCentre;
                                BezierPath.NotifyPathModified();
                            }
                        }
                    }

                    if (GUILayout.Button("Reset Path"))
                    {
                        Undo.RecordObject(_creator, "Reset Path");
                        Data.ResetBezierPath(_creator.transform);
                        EditorApplication.QueuePlayerLoopUpdate();
                    }

                    GUILayout.Space(InspectorSectionSpacing);
                }

                Data.showNormals = EditorGUILayout.Foldout(Data.showNormals, new GUIContent("Normals Options"), true, 
                    _boldFoldoutStyle);
                if (Data.showNormals)
                {
                    BezierPath.FlipNormals = EditorGUILayout.Toggle(new GUIContent("Flip Normals"), BezierPath.FlipNormals);
                    if (BezierPath.Space == PathSpace.XYZ)
                    {
                        BezierPath.GlobalNormalsAngle = EditorGUILayout.Slider(new GUIContent("Global Angle"), BezierPath.GlobalNormalsAngle, 0, 360);

                        if (GUILayout.Button("Reset Normals"))
                        {
                            Undo.RecordObject(_creator, "Reset Normals");
                            BezierPath.FlipNormals = false;
                            BezierPath.ResetNormalAngles();
                        }
                    }
                    GUILayout.Space(InspectorSectionSpacing);
                }

                // Editor display options.
                Data.showDisplayOptions = EditorGUILayout.Foldout(Data.showDisplayOptions, 
                    new GUIContent("Display Options"), true, _boldFoldoutStyle);
                if (Data.showDisplayOptions)
                {
                    Data.keepConstantHandleSize = GUILayout.Toggle(Data.keepConstantHandleSize, 
                        new GUIContent("Constant Point Size", ConstantSizeTooltip));
                    Data.bezierHandleScale = Mathf.Max(0, EditorGUILayout.FloatField(new GUIContent("Handle Scale"), 
                        Data.bezierHandleScale));
                    DrawGlobalDisplaySettingsInspector();
                }

                if (check.changed)
                {
                    SceneView.RepaintAll();
                    EditorApplication.QueuePlayerLoopUpdate();
                }
            }
        }

        /// <summary>
        /// Draws the inspector for the vertex path, displaying vertex count, options, and display settings.
        /// Allows modifying vertex path settings and updates the scene view when changes are made.
        /// </summary>
        private void DrawVertexPathInspector()
        {
            GUILayout.Space(InspectorSectionSpacing);
            EditorGUILayout.LabelField("Vertex count: " + _creator.Path.NumPoints);
            GUILayout.Space(InspectorSectionSpacing);

            Data.showVertexPathOptions = EditorGUILayout.Foldout(Data.showVertexPathOptions, 
                new GUIContent("Vertex Path Options"), true, _boldFoldoutStyle);
            if (Data.showVertexPathOptions)
            {
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    Data.vertexPathMaxAngleError = EditorGUILayout.Slider(new GUIContent("Max Angle Error"), 
                        Data.vertexPathMaxAngleError, 0, 45);
                    Data.vertexPathMinVertexSpacing = EditorGUILayout.Slider(new GUIContent("Min Vertex Dst"), 
                        Data.vertexPathMinVertexSpacing, 0, 1);

                    GUILayout.Space(InspectorSectionSpacing);
                    if (check.changed)
                    {
                        Data.VertexPathSettingsChanged();
                        SceneView.RepaintAll();
                        EditorApplication.QueuePlayerLoopUpdate();
                    }
                }
            }

            Data.showVertexPathDisplayOptions = EditorGUILayout.Foldout(Data.showVertexPathDisplayOptions, 
                new GUIContent("Display Options"), true, _boldFoldoutStyle);
            if (Data.showVertexPathDisplayOptions)
            {
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    Data.showNormalsInVertexMode = GUILayout.Toggle(Data.showNormalsInVertexMode, 
                        new GUIContent("Show Normals"));
                    Data.showBezierPathInVertexMode = GUILayout.Toggle(Data.showBezierPathInVertexMode, 
                        new GUIContent("Show Bezier Path"));

                    if (check.changed)
                    {
                        SceneView.RepaintAll();
                        EditorApplication.QueuePlayerLoopUpdate();
                    }
                }
                DrawGlobalDisplaySettingsInspector();
            }
        }

        /// <summary>
        /// Draws the inspector for global display settings, using a foldout and a custom editor.
        /// Updates the global display settings and repaints the scene view when changes are made.
        /// </summary>
        private void DrawGlobalDisplaySettingsInspector()
        {
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                Data.globalDisplaySettingsFoldout = EditorGUILayout.InspectorTitlebar(Data.globalDisplaySettingsFoldout,
                    _globalDisplaySettings);
                if (Data.globalDisplaySettingsFoldout)
                {
                    CreateCachedEditor(_globalDisplaySettings, null, ref _globalDisplaySettingsEditor);
                    _globalDisplaySettingsEditor.OnInspectorGUI();
                }
                if (check.changed)
                {
                    UpdateGlobalDisplaySettings();
                    SceneView.RepaintAll();
                }
            }
        }

        /// <summary>
        /// Handles the scene GUI events, including drawing the Bezier path or vertex path based on the active tab.
        /// Processes input events for the Bezier path and updates the transform state.
        /// </summary>
        private void OnSceneGUI()
        {
            if (!_globalDisplaySettings.visibleBehindObjects)
            {
                Handles.zTest = CompareFunction.LessEqual;
            }

            EventType eventType = Event.current.type;

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                _handlesStartCol = Handles.color;
                switch (Data.tabIndex)
                {
                    case BezierPathTab:
                        if (eventType != EventType.Repaint && eventType != EventType.Layout)
                        {
                            ProcessBezierPathInput(Event.current);
                        }

                        DrawBezierPathSceneEditor();
                        break;
                    case VertexPathTab:
                        if (eventType == EventType.Repaint)
                        {
                            DrawVertexPathSceneEditor();
                        }
                        break;
                }

                // Don't allow clicking over empty space to deselect the object.
                if (eventType == EventType.Layout && !_globalDisplaySettings.deselectWhenClickingOutsideSpline)
                {
                    HandleUtility.AddDefaultControl(0);
                }

                if (check.changed)
                {
                    EditorApplication.QueuePlayerLoopUpdate();
                }
            }

            SetTransformState();
        }

        
        /// <summary>
        /// Draws the vertex path in the scene view, including Bezier lines, vertex lines, and normals if enabled.
        /// Uses the global display settings to determine colors and visibility.
        /// </summary>
        private void DrawVertexPathSceneEditor()
        {
            Color bezierCol = _globalDisplaySettings.BezierPathColor;
            bezierCol.a *= .5f;

            if (Data.showBezierPathInVertexMode)
            {
                for (int i = 0; i < BezierPath.NumSegments; i++)
                {
                    Vector3[] points = BezierPath.GetPointsInSegment(i);
                    for (int j = 0; j < points.Length; j++)
                    {
                        points[j] = MathUtility.TransformPoint(points[j], _creator.transform, BezierPath.Space);
                    }
                    Handles.DrawBezier(points[0], points[3], points[1], points[2], bezierCol, null, 2);
                }
            }

            Handles.color = _globalDisplaySettings.VertexPath;

            for (int i = 0; i < _creator.Path.NumPoints; i++)
            {
                int nextIndex = (i + 1) % _creator.Path.NumPoints;
                if (nextIndex != 0 || BezierPath.IsClosed)
                {
                    Handles.DrawLine(_creator.Path.GetPoint(i), _creator.Path.GetPoint(nextIndex));
                }
            }

            if (Data.showNormalsInVertexMode)
            {
                Handles.color = _globalDisplaySettings.Normals;
                Vector3[] normalLines = new Vector3[_creator.Path.NumPoints * 2];
                for (int i = 0; i < _creator.Path.NumPoints; i++)
                {
                    normalLines[i * 2] = _creator.Path.GetPoint(i);
                    normalLines[i * 2 + 1] = _creator.Path.GetPoint(i) + _creator.Path.LocalNormals[i] * 
                        GlobalDisplaySettings.NormalsLength;
                }
                Handles.DrawLines(normalLines);
            }
        }

        /// <summary>
        /// Processes input events for the Bezier path in the scene view, such as adding, removing, or selecting segments.
        /// Handles mouse input for interacting with the Bezier path handles and updates the path accordingly.
        /// </summary>
        private void ProcessBezierPathInput(Event e)
        {
            // Find which handle mouse is over. Start by looking at previous handle index first,
            // as most likely to still be closest to mouse.
            int previousMouseOverHandleIndex = (_mouseOverHandleIndex == -1) ? 0 : _mouseOverHandleIndex;
            _mouseOverHandleIndex = -1;
            for (int i = 0; i < BezierPath.NumPoints; i += 3)
            {
                int handleIndex = (previousMouseOverHandleIndex + i) % BezierPath.NumPoints;
                float handleRadius = GetHandleDiameter(_globalDisplaySettings.anchorSize * Data.bezierHandleScale, 
                    BezierPath[handleIndex]) / 2f;
                Vector3 pos = MathUtility.TransformPoint(BezierPath[handleIndex], _creator.transform, BezierPath.Space);
                float dst = HandleUtility.DistanceToCircle(pos, handleRadius);
                if (dst == 0)
                {
                    _mouseOverHandleIndex = handleIndex;
                    break;
                }
            }

            // Shift-left click (when mouse not over a handle) to split or add segment.
            if (_mouseOverHandleIndex == -1)
            {
                if (e.type == EventType.MouseDown && e.button == 0 && e.shift)
                {
                    UpdatePathMouseInfo();
                    // Insert point along selected segment.
                    if (_selectedSegmentIndex != -1 && _selectedSegmentIndex < BezierPath.NumSegments)
                    {
                        Vector3 newPathPoint = _pathMouseInfo.ClosestWorldPointToMouse;
                        newPathPoint = MathUtility.InverseTransformPoint(newPathPoint, _creator.transform, 
                            BezierPath.Space);
                        Undo.RecordObject(_creator, "Split segment");
                        BezierPath.SplitSegment(newPathPoint, _selectedSegmentIndex, _pathMouseInfo.TimeOnBezierSegment);
                    }
                    // If path is not a closed loop, add new point on to the end of the path.
                    else if (!BezierPath.IsClosed)
                    {
                        // If control/command are held down, the point gets pre-pended, so we want to check distance
                        // to the endpoint we are adding to.
                        var pointIdx = e.control || e.command ? 0 : BezierPath.NumPoints - 1;
                        // insert new point at same dst from scene camera as the point that comes before it (for a 3d path).
                        var endPointLocal = BezierPath[pointIdx];
                        var endPointGlobal =
                            MathUtility.TransformPoint(endPointLocal, _creator.transform, BezierPath.Space);
                        var distanceCameraToEndpoint = (Camera.current.transform.position - endPointGlobal).magnitude;
                        var newPointGlobal =
                            PathHandle.GetMouseWorldPosition(BezierPath.Space, _creator.transform.position.y, 
                                distanceCameraToEndpoint);
                        var newPointLocal =
                            MathUtility.InverseTransformPoint(newPointGlobal, _creator.transform, BezierPath.Space);

                        Undo.RecordObject(_creator, "Add segment");
                        if (e.control || e.command)
                        {
                            BezierPath.AddSegmentToStart(newPointLocal);
                        }
                        else
                        {
                            BezierPath.AddSegmentToEnd(newPointLocal);
                        }
                    }
                }
            }

            // Control click or backspace/delete to remove point.
            if (e.keyCode == KeyCode.Backspace || e.keyCode == KeyCode.Delete || ((e.control || e.command) && 
                    e.type == EventType.MouseDown && e.button == 0))
            {
                if (_mouseOverHandleIndex != -1)
                {
                    Undo.RecordObject(_creator, "Delete segment");
                    BezierPath.DeleteSegment(_mouseOverHandleIndex);
                    if (_mouseOverHandleIndex == _handleIndexToDisplayAsTransform)
                    {
                        _handleIndexToDisplayAsTransform = -1;
                    }
                    _mouseOverHandleIndex = -1;
                    Repaint();
                }
            }

            // Holding shift and moving mouse (but mouse not over a handle/dragging a handle).
            if (_draggingHandleIndex == -1 && _mouseOverHandleIndex == -1)
            {
                bool shiftDown = e.shift && !_shiftLastFrame;
                if (shiftDown || ((e.type == EventType.MouseMove || e.type == EventType.MouseDrag) && e.shift))
                {
                    UpdatePathMouseInfo();
                    bool notSplittingAtControlPoint = _pathMouseInfo.TimeOnBezierSegment > 0 && 
                                                      _pathMouseInfo.TimeOnBezierSegment < 1;
                    if (_pathMouseInfo.MouseDstToLine < SegmentSelectDistanceThreshold && notSplittingAtControlPoint)
                    {
                        if (_pathMouseInfo.ClosestSegmentIndex != _selectedSegmentIndex)
                        {
                            _selectedSegmentIndex = _pathMouseInfo.ClosestSegmentIndex;
                            HandleUtility.Repaint();
                        }
                    }
                    else
                    {
                        _selectedSegmentIndex = -1;
                        HandleUtility.Repaint();
                    }

                }
            }

            _shiftLastFrame = e.shift;

        }

        /// <summary>
        /// Draws the Bezier path handles in the scene view, including control points, tangents, and normals if enabled.
        /// Handles user input for modifying the Bezier path and updates the path data accordingly.
        /// </summary>
        private void DrawBezierPathSceneEditor()
        {
            var displayControlPoints = BezierPath.ControlPointMode != BezierPath.ControlMode.Automatic;

            if (Event.current.type == EventType.Repaint)
            {
                for (int i = 0; i < BezierPath.NumSegments; i++)
                {
                    Vector3[] points = BezierPath.GetPointsInSegment(i);
                    for (int j = 0; j < points.Length; j++)
                    {
                        points[j] = MathUtility.TransformPoint(points[j], _creator.transform, BezierPath.Space);
                    }

                    // Draw lines between control points.
                    if (displayControlPoints)
                    {
                        Handles.color = _globalDisplaySettings.ControlLine;
                        Handles.DrawLine(points[1], points[0]);
                        Handles.DrawLine(points[2], points[3]);
                    }

                    // Draw path.
                    bool highlightSegment = i == _selectedSegmentIndex && Event.current.shift && 
                                            _draggingHandleIndex == -1 && _mouseOverHandleIndex == -1;
                    Color segmentCol = highlightSegment ? 
                        _globalDisplaySettings.HighlightedPath : _globalDisplaySettings.BezierPathColor;
                    Handles.DrawBezier(points[0], points[3], points[1], points[2], segmentCol, null, 2);
                }

                // Draw normals.
                if (Data.showNormals)
                {
                    if (!_hasUpdatedNormalsVertexPath)
                    {
                        _normalsVertexPath = new VertexPath(BezierPath, _creator.transform, NormalsSpacing);
                        _hasUpdatedNormalsVertexPath = true;
                    }

                    if (_editingNormalsOld != Data.showNormals)
                    {
                        _editingNormalsOld = Data.showNormals;
                        Repaint();
                    }

                    Vector3[] normalLines = new Vector3[_normalsVertexPath.NumPoints * 2];
                    Handles.color = _globalDisplaySettings.Normals;
                    for (int i = 0; i < _normalsVertexPath.NumPoints; i++)
                    {
                        normalLines[i * 2] = _normalsVertexPath.GetPoint(i);
                        normalLines[i * 2 + 1] = _normalsVertexPath.GetPoint(i) + _normalsVertexPath.GetNormal(i) * 
                            GlobalDisplaySettings.NormalsLength;
                    }
                    Handles.DrawLines(normalLines);
                }
            }
            
            for (int i = 0; i < BezierPath.NumPoints; i += 3)
            {
                DrawHandle(i);
            }
            
            if (displayControlPoints)
            {
                for (int i = 1; i < BezierPath.NumPoints - 1; i += 3)
                {
                    DrawHandle(i);
                    DrawHandle(i + 1);
                }
            }
        }

        
        /// <summary>
        /// Draws a handle for a point on the Bezier path, considering the handle type (anchor or control point).
        /// Handles user input for dragging the handle, displaying the transform handle, and updating the path data.
        /// </summary>
        private void DrawHandle(int i)
        {
            Vector3 initialHandlePosition = MathUtility.TransformPoint(BezierPath[i], _creator.transform, 
                BezierPath.Space);

            float anchorHandleSize = GetHandleDiameter(_globalDisplaySettings.anchorSize * Data.bezierHandleScale, 
                BezierPath[i]);
            float controlHandleSize = GetHandleDiameter(_globalDisplaySettings.controlSize * Data.bezierHandleScale, 
                BezierPath[i]);

            bool isAnchorPoint = i % 3 == 0;
            bool isInteractive = isAnchorPoint || BezierPath.ControlPointMode != BezierPath.ControlMode.Automatic;
            float handleSize = (isAnchorPoint) ? anchorHandleSize : controlHandleSize;
            bool doTransformHandle = i == _handleIndexToDisplayAsTransform;

            PathHandle.HandleColours handleColours = isAnchorPoint ? _splineAnchorColours : _splineControlColours;
            if (i == _handleIndexToDisplayAsTransform)
            {
                handleColours.DefaultColour = isAnchorPoint ? 
                    _globalDisplaySettings.AnchorSelected : _globalDisplaySettings.ControlSelected;
            }

            var handlePosition = PathHandle.DrawHandle(initialHandlePosition, isInteractive, 
                BezierPath.isSnappedToSurface, BezierPath.Space, handleSize, Handles.SphereHandleCap, 
                handleColours, out var handleInputType, i, _creator.transform.position.y);

            if (doTransformHandle)
            {
                // Show normals rotate tool.
                if (Data.showNormals && Tools.current == Tool.Rotate && isAnchorPoint && 
                    BezierPath.Space == PathSpace.XYZ)
                {
                    if (BezierPath.isSnappedToSurface)
                    {
                        Handles.zTest = CompareFunction.Always;
                    }
                    Handles.color = _handlesStartCol;

                    int attachedControlIndex = (i == BezierPath.NumPoints - 1) ? i - 1 : i + 1;
                    Vector3 dir = (BezierPath[attachedControlIndex] - handlePosition).normalized;
                    float handleRotOffset = (360 + BezierPath.GlobalNormalsAngle) % 360;
                    _anchorAngleHandle.radius = handleSize * 3;
                    _anchorAngleHandle.angle = handleRotOffset + BezierPath.GetAnchorNormalAngle(i / 3);
                    Vector3 handleDirection = Vector3.Cross(dir, Vector3.up);
                    Matrix4x4 handleMatrix = Matrix4x4.TRS(
                        handlePosition,
                        Quaternion.LookRotation(handleDirection, dir),
                        Vector3.one
                    );

                    using (new Handles.DrawingScope(handleMatrix))
                    {
                        // Draw the handle.
                        EditorGUI.BeginChangeCheck();
                        _anchorAngleHandle.DrawHandle();
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(_creator, "Set angle");
                            BezierPath.SetAnchorNormalAngle(i / 3, _anchorAngleHandle.angle - handleRotOffset);
                        }
                    }
                }
                else
                {
                    handlePosition = Handles.DoPositionHandle(handlePosition, Quaternion.identity);
                }
            }

            switch (handleInputType)
            {
                case PathHandle.HandleInputType.LMBDrag:
                    _draggingHandleIndex = i;
                    _handleIndexToDisplayAsTransform = -1;
                    Repaint();
                    break;
                case PathHandle.HandleInputType.LMBRelease:
                    _draggingHandleIndex = -1;
                    _handleIndexToDisplayAsTransform = -1;
                    Repaint();
                    break;
                case PathHandle.HandleInputType.LMBClick:
                    _draggingHandleIndex = -1;
                    if (Event.current.shift)
                    {
                        // Disable move tool if new point added.
                        _handleIndexToDisplayAsTransform = -1; 
                    }
                    else
                    {
                        if (_handleIndexToDisplayAsTransform == i)
                        {
                            // Disable move tool if clicking on point under move tool.
                            _handleIndexToDisplayAsTransform = -1; 
                        }
                        else
                        {
                            _handleIndexToDisplayAsTransform = i;
                        }
                    }
                    Repaint();
                    break;
                case PathHandle.HandleInputType.LMBPress:
                    if (_handleIndexToDisplayAsTransform != i)
                    {
                        _handleIndexToDisplayAsTransform = -1;
                        Repaint();
                    }
                    break;
            }

            var localHandlePosition = MathUtility.InverseTransformPoint(handlePosition, _creator.transform, 
                BezierPath.Space);

            if (BezierPath[i] == localHandlePosition)
            {
                return;
            }
            
            Undo.RecordObject(_creator, "Move point");
            if (BezierPath.Space == PathSpace.XYZ && BezierPath.isSnappedToSurface)
            {
                BezierPath.MovePoint(i, localHandlePosition);
            }
            else
            {
                BezierPath.MovePoint(i, localHandlePosition);
            }
        }
        
        /// <summary>
        /// Cleans up the editor state when the editor becomes disabled, hiding the tools.
        /// </summary>
        private void OnDisable()
        {
            Tools.hidden = false;
        }

        /// <summary>
        /// Initializes the editor when it becomes enabled, setting up event listeners and loading display settings.
        /// Resets the editor state and sets the initial transform state.
        /// </summary>
        private void OnEnable()
        {
            _creator = (PathCreator)target;
            _creator.InitializeEditorData();

            Data.BezierCreated -= ResetState;
            Data.BezierCreated += ResetState;
            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.undoRedoPerformed += OnUndoRedo;

            LoadDisplaySettings();
            UpdateGlobalDisplaySettings();
            ResetState();
            SetTransformState(true);
        }

        /// <summary>
        /// Sets the transform state of the path creator, updating the transform position, scale, and rotation.
        /// Notifies the path creator of any changes to the transform if not initializing.
        /// </summary>
        private void SetTransformState(bool initialize = false)
        {
            var t = _creator.transform;
            if (!initialize)
            {
                if (_transformPos != t.position || t.localScale != _transformScale || t.rotation != _transformRot)
                {
                    Data.PathTransformed();
                }

                if (_transformGlobalScale != t.lossyScale)
                {
                    _creator.Path.ScalePathLength();
                } 
            }
            
            _transformPos = t.position;
            _transformScale = t.localScale;
            _transformRot = t.rotation;
            _transformGlobalScale = t.lossyScale;
        }
        
        /// <summary>
        /// Handles undo/redo operations, resetting the screen space line and normals vertex path.
        /// Deselects the currently selected segment and repaints the editor.
        /// </summary>
        private void OnUndoRedo()
        {
            _hasUpdatedScreenSpaceLine = false;
            _hasUpdatedNormalsVertexPath = false;
            _selectedSegmentIndex = -1;

            Repaint();
        }

        /// <summary>
        /// Handles tab changes between the Bezier path and vertex path, repainting all scene views.
        /// </summary>
        private void TabChanged()
        {
            SceneView.RepaintAll();
            RepaintUnfocusedSceneViews();
        }
        
        /// <summary>
        /// Loads the global display settings from the GlobalDisplaySettings scriptable object.
        /// </summary>
        private void LoadDisplaySettings()
        {
            _globalDisplaySettings = GlobalDisplaySettings.Load();
        }

        /// <summary>
        /// Updates the global display settings, setting up handle colors and angle handle properties.
        /// </summary>
        private void UpdateGlobalDisplaySettings()
        {
            var gds = _globalDisplaySettings;
            _splineAnchorColours = new PathHandle.HandleColours(gds.Anchor, gds.AnchorHighlighted, gds.AnchorSelected, 
                gds.HandleDisabled);
            _splineControlColours = new PathHandle.HandleColours(gds.Control, gds.ControlHighlighted, 
                gds.ControlSelected, gds.HandleDisabled);

            _anchorAngleHandle.fillColor = new Color(1, 1, 1, .05f);
            _anchorAngleHandle.wireframeColor = Color.grey;
            _anchorAngleHandle.radiusHandleColor = Color.clear;
            _anchorAngleHandle.angleHandleColor = Color.white;
        }

        /// <summary>
        /// Resets the editor state, clearing selected segments, dragging handles, and updating flags.
        /// Sets up event listeners for path modification and repaints the scene view.
        /// </summary>
        private void ResetState()
        {
            _selectedSegmentIndex = -1;
            _draggingHandleIndex = -1;
            _mouseOverHandleIndex = -1;
            _handleIndexToDisplayAsTransform = -1;
            _hasUpdatedScreenSpaceLine = false;
            _hasUpdatedNormalsVertexPath = false;

            BezierPath.OnModified -= OnPathModified;
            BezierPath.OnModified += OnPathModified;

            SceneView.RepaintAll();
            EditorApplication.QueuePlayerLoopUpdate();
        }

        /// <summary>
        /// Handles path modification events, resetting the screen space line and normals vertex path.
        /// Repaints unfocused scene views to reflect the changes.
        /// </summary>
        private void OnPathModified()
        {
            _hasUpdatedScreenSpaceLine = false;
            _hasUpdatedNormalsVertexPath = false;

            RepaintUnfocusedSceneViews();
        }
        
        /// <summary>
        /// Repaints unfocused scene views when the path is modified, ensuring all views are up to date.
        /// </summary>
        private void RepaintUnfocusedSceneViews()
        {
            // If multiple scene views are open, repaint those which do not have focus.
            if (SceneView.sceneViews.Count > 1)
            {
                foreach (SceneView sv in SceneView.sceneViews)
                {
                    if (EditorWindow.focusedWindow != sv)
                    {
                        sv.Repaint();
                    }
                }
            }
        }

        /// <summary>
        /// Updates the path mouse information, calculating the closest point on the path to the mouse cursor.
        /// Uses a screen space polyline to approximate the path and improve performance.
        /// </summary>
        private void UpdatePathMouseInfo()
        {
            if (!_hasUpdatedScreenSpaceLine || (_screenSpaceLine != null && _screenSpaceLine.TransformIsOutOfDate()))
            {
                _screenSpaceLine = new ScreenSpacePolyLine(BezierPath, _creator.transform, ScreenPolylineMaxAngleError, 
                    ScreenPolylineMinVertexDst);
                _hasUpdatedScreenSpaceLine = true;
            }
            _pathMouseInfo = _screenSpaceLine.CalculateMouseInfo();
        }

        /// <summary>
        /// Calculates the handle diameter based on the provided diameter and handle position.
        /// Considers the constant handle scale and adjusts the diameter based on the handle size and zoom level.
        /// </summary>
        private float GetHandleDiameter(float diameter, Vector3 handlePosition)
        {
            float scaledDiameter = diameter * ConstantHandleScale;
            if (Data.keepConstantHandleSize)
            {
                scaledDiameter *= HandleUtility.GetHandleSize(handlePosition) * 2.5f;
            }
            return scaledDiameter;
        }
    }
}
