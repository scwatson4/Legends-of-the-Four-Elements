using UnityEditor;
using UnityEngine;

namespace AnythingWorld.Behaviour
{
    /// <summary>
    /// Custom editor for RandomModelSpawner to handle GUI and scene interactions.
    /// </summary>
    [CustomEditor(typeof(ProceduralModelSpawner))]
    public class ProcedualModelSpawnerEditor : Editor
    {
        private Vector3 _areaNormal;
        private bool _isRmbDragging;
        private Color _fillColor = new Color(0.5f, 0.5f, 1.0f, 0.5f);
        private Color _outlineColor = Color.blue;
        private SerializedProperty _useGridSpawnProperty;
        private SerializedProperty _spawnWidthProperty;
        private SerializedProperty _spawnHeightProperty;
        private SerializedProperty _showGridSlotsProperty;
        private SerializedProperty _spawnRadiusProperty;
        private SerializedProperty _canModelsOverlapProperty;
        private SerializedProperty _modelsProperty;
        private SerializedProperty _modelWeightsProperty;
        private float _startTime;
        private bool _isLmbDragging;

        /// <summary>
        /// Initializes properties and registers scene GUI callbacks.
        /// </summary>
        void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUIDraw;
            _useGridSpawnProperty = serializedObject.FindProperty("useGridSpawn");
            _spawnWidthProperty = serializedObject.FindProperty("spawnWidth");
            _spawnHeightProperty = serializedObject.FindProperty("spawnHeight");
            _showGridSlotsProperty = serializedObject.FindProperty("showGridSlots");
            _spawnRadiusProperty = serializedObject.FindProperty("spawnRadius");
            _canModelsOverlapProperty = serializedObject.FindProperty("canModelsOverlap");
            _modelsProperty = serializedObject.FindProperty("models");
            _modelWeightsProperty = serializedObject.FindProperty("modelWeights");
        }

        /// <summary>
        /// Unregisters scene GUI callbacks when the editor is disabled.
        /// </summary>
        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUIDraw;
        }

        /// <summary>
        /// Draws the custom inspector GUI for the RandomModelSpawner.
        /// </summary>
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("You can delete spawned models by holding Ctrl while clicking or dragging.",
                MessageType.Info);

            base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUILayout.PropertyField(_useGridSpawnProperty, new GUIContent("Use Grid Spawn"));

            if (_useGridSpawnProperty.boolValue)
            {
                EditorGUILayout.PropertyField(_spawnWidthProperty, new GUIContent("Spawn Width"));
                EditorGUILayout.PropertyField(_spawnHeightProperty, new GUIContent("Spawn Height"));
                EditorGUILayout.PropertyField(_showGridSlotsProperty, new GUIContent("Show Grid Slots"));
            }
            else
            {
                EditorGUILayout.PropertyField(_spawnRadiusProperty, new GUIContent("Spawn Radius"));
                EditorGUILayout.PropertyField(_canModelsOverlapProperty, new GUIContent("Can Models Overlap"));
            }

            if (GUILayout.Button("Delete all spawned models"))
            {
                var modelSpawner = (ProceduralModelSpawner)target;
                modelSpawner.DeleteSpawnedModels();
            }

            EditorGUILayout.LabelField("Models and Weights", EditorStyles.boldLabel);

            if (_modelsProperty.arraySize != _modelWeightsProperty.arraySize)
            {
                EditorGUILayout.HelpBox("Models and Model Weights arrays must have the same size!", MessageType.Error);
            }
            else
            {
                int arraySize = _modelsProperty.arraySize;
                for (int i = 0; i < arraySize; i++)
                {
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    EditorGUILayout.PropertyField(_modelsProperty.GetArrayElementAtIndex(i), new GUIContent("Model " + i));
                    EditorGUILayout.PropertyField(_modelWeightsProperty.GetArrayElementAtIndex(i), new GUIContent("Weight " + i));
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Model"))
                {
                    _modelsProperty.arraySize++;
                    _modelWeightsProperty.arraySize++;
                    _modelsProperty.GetArrayElementAtIndex(_modelsProperty.arraySize - 1).objectReferenceValue = null;
                    _modelWeightsProperty.GetArrayElementAtIndex(_modelWeightsProperty.arraySize - 1).floatValue = 0f;
                }
                if (GUILayout.Button("Remove Model"))
                {
                    if (_modelsProperty.arraySize > 0)
                    {
                        _modelsProperty.arraySize--;
                        _modelWeightsProperty.arraySize--;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Handles scene GUI events and updates spawn area visualization.
        /// </summary>
        private void OnSceneGUIDraw(SceneView sceneView)
        {
            var modelSpawner = target as ProceduralModelSpawner;
            if (!modelSpawner)
            {
                return;
            }

            var e = Event.current;

            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 1)
            {
                _isRmbDragging = true;
            }

            if (e.type == EventType.MouseUp && e.button == 1)
            {
                _isRmbDragging = false;
                UpdateAreaPosition(modelSpawner);
            }

            if (_isRmbDragging)
            {
                return;
            }

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            if (e.type == EventType.MouseDrag && e.button == 0)
            {
                UpdateAreaPosition(modelSpawner);
                if (_isLmbDragging == false)
                {
                    _startTime = Time.realtimeSinceStartup;
                    e.Use();
                }
                _isLmbDragging = true;
            }

            if (e.type == EventType.MouseUp && e.button == 0)
            {
                _isLmbDragging = false;
                e.Use();
            }

            if (_isLmbDragging && !IsMouseInSceneView(sceneView, e))
            {
                _isLmbDragging = false;
            }

            if (_isLmbDragging && e.type == EventType.MouseDrag)
            {
                if (e.control)
                {
                    modelSpawner.RemoveModelsInSpawnArea();
                    e.Use();
                }
                else if (Time.realtimeSinceStartup - _startTime > ProceduralModelSpawner.SpawnInterval)
                {
                    _startTime = Time.realtimeSinceStartup;
                    modelSpawner.RecalculateSpawnParameters();
                    modelSpawner.SpawnModels();
                    e.Use();
                }
            }

            if (!_isLmbDragging)
            {
                if (e.type == EventType.MouseMove)
                {
                    UpdateAreaPosition(modelSpawner);
                    e.Use();
                }

                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    if (e.control)
                    {
                        modelSpawner.RemoveModelsInSpawnArea();
                    }
                    else
                    {
                        modelSpawner.RecalculateSpawnParameters();
                        modelSpawner.SpawnModels();
                    }
                    e.Use();
                }
            }

            if (e.type != EventType.Repaint)
            {
                return;
            }

            if (modelSpawner.useGridSpawn)
            {
                var halfWidth = modelSpawner.spawnWidth / 2;
                var halfHeight = modelSpawner.spawnHeight / 2;

                Vector3 pos = modelSpawner.spawnAreaCenter;

                Vector3[] verts =
                {
                new Vector3(pos.x - halfWidth, pos.y, pos.z - halfHeight),
                new Vector3(pos.x - halfWidth, pos.y, pos.z + halfHeight),
                new Vector3(pos.x + halfWidth, pos.y, pos.z + halfHeight),
                new Vector3(pos.x + halfWidth, pos.y, pos.z - halfHeight)
            };

                Handles.DrawSolidRectangleWithOutline(verts, _fillColor, _outlineColor);
                return;
            }

            Handles.color = _fillColor;
            Handles.DrawSolidDisc(modelSpawner.spawnAreaCenter, _areaNormal, modelSpawner.spawnRadius);
            Handles.color = _outlineColor;
            Handles.DrawWireDisc(modelSpawner.spawnAreaCenter, _areaNormal, modelSpawner.spawnRadius);
        }

        /// <summary>
        /// Updates the spawn area's position based on the mouse location.
        /// </summary>
        private void UpdateAreaPosition(ProceduralModelSpawner spawner)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

            if (!Physics.Raycast(ray, out var hit, Mathf.Infinity, ~spawner.spawnedModelsLayerMask))
            {
                return;
            }

            spawner.spawnAreaCenter = hit.point;
            _areaNormal = hit.normal;

            SceneView.RepaintAll();
        }

        /// <summary>
        /// Determines if the mouse cursor is currently within the bounds of the given SceneView.
        /// </summary>
        private bool IsMouseInSceneView(SceneView sceneView, Event e)
        {
            Rect sceneRect = sceneView.position;
            Vector2 mousePosition = e.mousePosition;
            mousePosition.y = sceneRect.height - mousePosition.y;
            return sceneRect.Contains(mousePosition);
        }
    }
}
