namespace AnythingWorld.Editor
{
    using Utilities;
    using Utilities.Data;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// This class provides functionality to draw a texture in the Unity Scene view. 
    /// It also allows the user to drag and drop the texture onto objects within the scene.
    /// </summary>
    public class SceneTextureDrawer : Editor
    {
        // Delegate definition for handling the drag event
        public delegate GameObject OnDrag(SearchResult result, Vector3 position, bool forceSerialize = false);

        private OnDrag _onDrag;
        
        private bool _isDragging = false;
        private bool _loading = false;

        // Textures for the object and its loading state
        private Texture2D _objTexture;
        private Texture2D _objLoadingTexture;

        // To store the current position of the mouse within the scene view
        private Vector2 _currentMousePosition;

        // Singleton instance to ensure only one drawer is active at a time
        private static SceneTextureDrawer _instance;

        // Flag to control the activation state of the drawer
        private bool _enabled = false;

        // Modifier key to continue creating until released
        private readonly EventModifiers continueUntil = EventModifiers.Shift;

        private SearchResult _searchResult;

        // Store the position where the object will be placed
        private Vector3 _objectPosition;

        // Store the generated game object after a successful drag and drop
        private GameObject _generatedGameObject;

        /// <summary>
        /// Singleton instance of the SceneTextureDrawer.
        /// </summary>
        public static SceneTextureDrawer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = CreateInstance<SceneTextureDrawer>();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Called when the object is enabled. It subscribes to the scene GUI event.
        /// </summary>
        private void OnEnable()
        {
            Enable();
        }

        /// <summary>
        /// Public method to enable the SceneTextureDrawer, ensuring there is only one instance active.
        /// </summary>
        public void Enable()
        {
            if (_instance != null && _instance != this)
            {
                DestroyImmediate(_instance);
            }
            _instance = this;
            if (_enabled)
            {
                return;
            }
            _enabled = true;
            SceneView.duringSceneGui += OnSceneGUI;
            _isDragging = false;
            _loading = false;
        }

        /// <summary>
        /// Checks if the SceneTextureDrawer is currently enabled.
        /// </summary>
        /// <returns>True if enabled, false otherwise.</returns>
        public bool IsEnabled()
        {
            return _enabled;
        }

        /// <summary>
        /// Called when the object is disabled. It unsubscribes from the scene GUI event to prevent memory leaks.
        /// </summary>
        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            _enabled = false;
        }

        /// <summary>
        /// Sets up the callback for dragging, initializing necessary state variables.
        /// </summary>
        public void SetCallBack(Texture2D texture, OnDrag onDrag, SearchResult result, ref bool isDrag)
        {
            this._onDrag = onDrag;
            _searchResult = result;
            _objTexture = texture;
            _isDragging = true;
            isDrag = true;
            _loading = false;
        }
        
        //Cancel the drag
        public void CancelDrag()
        {
            _isDragging = false;
            _loading = false;
            if (_onDrag != null)
                _onDrag(null, Vector3.zero, false);
        }
        
        private void OnSceneGUI(SceneView sceneView)
        {
            Event e = Event.current;
            switch (e.type)
            {
                case EventType.DragUpdated:
                    HandleDragUpdate();
                    break;
                case EventType.DragPerform:
                    HandleDragExited(sceneView, e);
                    break;
                case EventType.MouseDown:
                    HandleMouseDown(sceneView, e);
                    break;
                case EventType.MouseLeaveWindow:
                    if (_isDragging)
                    {
                        CancelDrag();
                    }
                    break;
            }
            if (_loading && _generatedGameObject != null)
            {
                HandleLoadingAnimation();
            }
            else
            {
                IconFloating(sceneView, e);
            }
        }
        
        //Handle DragUpdated event
        private void HandleDragUpdate()
        {
            if (!_isDragging) {
                //check if the dragged object is from the AnythingCreatorEditor
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is AnythingCreatorEditor)
                    {
                        _isDragging = true;
                        _loading = false;
                    }
                }
            }
            else
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
            }
        }
        
        //Handle DragExited event
        private void HandleDragExited(SceneView sceneView, Event e)
        {
            if (_isDragging)
            {
                //accept the drag and clear the drag buffer
                DragAndDrop.AcceptDrag();
                //call the delegate to create the object
                _generatedGameObject = _onDrag(_searchResult, _objectPosition, false);
                //look at the object
                if (TransformSettings.FollowCam)
                {
                    sceneView.LookAt(_objectPosition);
                }
                //start the loading animation
                if (TransformSettings.LoadingImageActive)
                {
                    StartLoadingAnimation();
                }
                if (e.modifiers == continueUntil || TransformSettings.ContinueUntilCancelled) return;
                _isDragging = false;
                e.Use();
            }
        }
        
        /// <summary>
        /// Handles the mouse down event loading the object into the scene.
        /// </summary>
        private void HandleMouseDown(SceneView sceneView, Event e)
        {
            if (e.button == 0 && e.isMouse)
            {
                if (!_isDragging)
                {
                    return;
                }
                
                //call the delegate to create the object
                _generatedGameObject = _onDrag(_searchResult, _objectPosition, false);

                if (TransformSettings.FollowCam)
                {
                    sceneView.LookAt(_objectPosition);
                }

                if (TransformSettings.LoadingImageActive)
                {
                    StartLoadingAnimation();
                }

                e.Use();
                if (e.modifiers == continueUntil || TransformSettings.ContinueUntilCancelled) return;
                _isDragging = false;
            }
            else if (_isDragging)
            {
                CancelDrag();
            }
        }
        
        /// <summary>
        /// Starts the loading animation of the object.
        /// </summary>
        private void StartLoadingAnimation()
        {
            _loading = true;
            _objLoadingTexture = Tex2dUtils.ConvertToGrayscale(_objTexture);
        }
        
        /// <summary>
        /// Handles the loading animation of the object.
        /// </summary>
        private void HandleLoadingAnimation()
        {
            if (!_objLoadingTexture)
            {
                return;
            }

            Vector2 guiPosition = HandleUtility.WorldToGUIPoint(_objectPosition);
            Vector2 texSize = new Vector2(_objLoadingTexture.width, _objLoadingTexture.height) * 0.4f;

            Handles.BeginGUI();
            GUI.DrawTexture(new Rect(guiPosition.x - texSize.x / 2, guiPosition.y - texSize.y / 2, texSize.x, texSize.y), _objLoadingTexture);
            Handles.EndGUI();

            if (_generatedGameObject.transform.childCount > 0)
            {
                _loading = false;
            }
        }
        /// <summary>
        /// handle the dragging of the texture in the scene view
        /// </summary>
        void IconFloating(SceneView sceneView, Event e)
        {
            if (!_isDragging || _objTexture == null) return;

            // Store the current mouse position
            _currentMousePosition = e.mousePosition;

            // Convert mouse position to world ray
            Ray ray = HandleUtility.GUIPointToWorldRay(_currentMousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                DrawTextureAtPoint(_objTexture,hit.point, 0.5f);
                _objectPosition = hit.point;
            }
            else
            {
                DrawTextureAtPoint(_objTexture,_currentMousePosition, 0.8f, true);
                _objectPosition = ray.origin + ray.direction * 10;
            }

            // Ensure the scene view is updated
            sceneView.Repaint();
        }
        /// <summary>
        /// Draws a texture at a given point in the scene view.
        /// </summary>
        void DrawTextureAtPoint(Texture2D texture,Vector3 point, float scale, bool isGuiPoint = false)
        {
            Handles.BeginGUI();
            Vector2 texSize = new Vector2(texture.width, texture.height) * scale;
            Vector2 position = isGuiPoint ? (Vector2)point : HandleUtility.WorldToGUIPoint(point);
            GUI.DrawTexture(new Rect(position.x - texSize.x / 2, position.y - texSize.y / 2, texSize.x, texSize.y), texture);
            Handles.EndGUI();
        }
    }
}
