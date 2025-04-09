using System;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Main editor window for the Behavior Tree, providing UI for editing and visualizing behavior trees.
    /// </summary>
    public class BehaviourTreeEditorWindow : EditorWindow
    {
        /// <summary>
        /// Tracks state for scripts pending creation, supporting delayed node creation after script compilation.
        /// </summary>
        [Serializable]
        public class NodeScriptCompilationTracker 
        {
            public bool isPendingCreation;
            public string scriptName = "";
            public string sourceGuid = "";
            public bool isSourceParent;
            public Vector2 nodePosition;

            public void Reset()
            {
                isPendingCreation = false;
                scriptName = "";
                sourceGuid = "";
                isSourceParent = false;
                nodePosition = Vector2.zero;
            }
        }

#if UNITY_2021_3_OR_NEWER
        /// <summary>
        /// Asset modification processor to handle behavior tree assets during delete operations.
        /// </summary>
        public class BehaviourTreeEditorAssetModificationProcessor : AssetModificationProcessor
        {
#else
        public class BehaviourTreeEditorAssetModificationProcessor : UnityEditor.AssetModificationProcessor
        {
#endif
            private static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions opt)
            {
                if (HasOpenInstances<BehaviourTreeEditorWindow>())
                {
                    BehaviourTreeEditorWindow wnd = GetWindow<BehaviourTreeEditorWindow>();
                    wnd.ClearIfSelected(path);
                }
                return AssetDeleteResult.DidNotDelete;
            }
        }
        private const string UiAssetsPath = 
            "Assets/AnythingWorld/AnythingBehaviour/AnythingBehaviourTree/Editor/EditorWindowUIAssets.asset";
        
        public static BehaviourTreeEditorWindow Instance;
        public BehaviourTreeProjectSettings settings;
        public BehaviourTreeEditorUiAssets uiAssets;

        public BehaviourTreeView treeView;
        public NewScriptDialogView newScriptDialog;
        public bool shouldOpenTree = true;

        [SerializeField]
        public NodeScriptCompilationTracker nodeScriptCompilationTracker = new NodeScriptCompilationTracker();

        public SerializedBehaviourTree serializer;
        
        private BehaviourTree _tree;
        private InspectorView _inspectorView;
        private BlackboardView _blackboardView;
        private OverlayView _overlayView;
        private ToolbarMenu _toolbarMenu;
        private ToolbarBreadcrumbs _breadcrumbs;
        private bool _isSubtreeSelected;

        /// <summary>
        /// Opens the behaviour tree editor window through the Unity menu.
        /// </summary>
        [MenuItem("Tools/Anything World/Behaviour Tree Editor")]
        public static void OpenWindow()
        {
            BehaviourTreeEditorWindow btEditorWindow = GetWindow<BehaviourTreeEditorWindow>();
            btEditorWindow._overlayView?.Show(false);
            btEditorWindow.titleContent = new GUIContent("Behaviour Tree Editor");
            btEditorWindow.minSize = new Vector2(800, 600);
        }
        
        /// <summary>
        /// Overload to open the window focused on a specific tree.
        /// </summary>
        public static void OpenWindow(BehaviourTree tree)
        {
            BehaviourTreeEditorWindow btEditorWindow = GetWindow<BehaviourTreeEditorWindow>();
            btEditorWindow.titleContent = new GUIContent("Behaviour Tree Editor");
            btEditorWindow.minSize = new Vector2(800, 600);
            btEditorWindow.SelectNewTree(tree);
        }
        
        /// <summary>
        /// Handles the event when a behaviour tree asset is double-clicked in the Project view.
        /// </summary>
        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            if (Instance != null && !Instance.shouldOpenTree)
            {
                Instance.shouldOpenTree = true;
                return false;
            }
            
            if (Selection.activeObject is BehaviourTree) 
            {
                OpenWindow(Selection.activeObject as BehaviourTree);
                return true;
            }
            
            UnityEngine.Object asset = EditorUtility.InstanceIDToObject(instanceId);
            if (asset is BehaviourTree behaviourTree)
            {
                OpenWindow(behaviourTree);
                return true;
            }
            
            return false;
        }

#if !UNITY_2021_3_OR_NEWER
        /// <summary>
        /// Makes blackboard elements selectable in the editor window.
        /// </summary>
        public void EnableBlackboardSelection() => _blackboardView.EnableSelection();
        
        /// <summary>
        /// Makes blackboard elements unselectable in the editor window.
        /// </summary>
        public void DisableBlackboardSelection() => _blackboardView.DisableSelection();
#endif
        /// <summary>
        /// Clears the current selection in the blackboard view.
        /// </summary>
        public void ClearBlackboardSelection() => _blackboardView.ClearSelection();

        /// <summary>
        /// Initializes the GUI for the Behavior Tree Editor window, setting up UI elements and bindings.
        /// </summary>
        public void CreateGUI()
        {
            Instance = this;
            
            if (Application.isPlaying)
            {
                uiAssets = AssetDatabase.LoadAssetAtPath<BehaviourTreeEditorUiAssets>(UiAssetsPath);
                settings = BehaviourTreeProjectSettings.GetOrCreateSettings();
            }
            
            // Each editor window contains a root VisualElement object.
            VisualElement root = rootVisualElement;
            
            // Import UXML
            var visualTree = uiAssets.behaviourTreeEditorXml;
            visualTree.CloneTree(root);

            // A stylesheet can be added to a VisualElement.
            // The style will be applied to the VisualElement and all of its children.
            var styleSheet = uiAssets.behaviourTreeEditorStyle;
            root.styleSheets.Add(styleSheet);
            
            // Main treeview.
            treeView = root.Q<BehaviourTreeView>();
            _inspectorView = root.Q<InspectorView>();
            _blackboardView = root.Q<BlackboardView>();
            _toolbarMenu = root.Q<ToolbarMenu>();
            _overlayView = root.Q<OverlayView>("OverlayView");
            newScriptDialog = root.Q<NewScriptDialogView>("NewScriptDialogView");
            _breadcrumbs = root.Q<ToolbarBreadcrumbs>("breadcrumbs");

            treeView.styleSheets.Add(uiAssets.behaviourTreeEditorStyle);

            // Toolbar assets menu.
            _toolbarMenu.RegisterCallback<MouseEnterEvent>(_ =>
            {
                // Refresh the menu options just before it's opened (on mouse enter).
                _toolbarMenu.menu.MenuItems().Clear();
                var behaviourTrees = BehaviourTreeEditorUtility.GetAssetPaths<BehaviourTree>();
                behaviourTrees.ForEach(path =>
                {
                    var fileName = System.IO.Path.GetFileName(path);
                    _toolbarMenu.menu.AppendAction($"{fileName}", _ =>
                    {
                        var tree = AssetDatabase.LoadAssetAtPath<BehaviourTree>(path);
                        SelectNewTree(tree);
                    });
                });
                _toolbarMenu.menu.AppendSeparator();
                _toolbarMenu.menu.AppendAction("New Tree...", _ => OnToolbarNewAsset());
            });
            
            treeView.OnNodeSelected -= OnNodeSelectionChanged;
            treeView.OnNodeSelected += OnNodeSelectionChanged;

            // Overlay view.
            _overlayView.OnTreeSelected -= SelectTree;
            _overlayView.OnTreeSelected += SelectTree;

            // New Script Dialog.
            newScriptDialog.style.visibility = Visibility.Hidden;

            if (serializer == null)
            {
                _overlayView.Show(false);
            }
            else
            {
                SelectTree(serializer.tree);
            }

            // Create new node for any scripts just created coming back from a compile.
            if (nodeScriptCompilationTracker != null && nodeScriptCompilationTracker.isPendingCreation)
            {
                CreatePendingScriptNode();
            }
        }

        /// <summary>
        /// Pushes a sub-tree view onto the editor, allowing editing of nested behavior trees.
        /// </summary>
        public void PushSubTreeView(SubTree subtreeNode)
        {
            if (subtreeNode.treeAsset != null)
            {
                if (subtreeNode.treeAsset == _tree)
                {
                    Debug.LogError("You have assigned subtree equal to the current one. Assign a different subtree to avoid circular reference.");
                    return;
                }
                _isSubtreeSelected = true;
                if (Application.isPlaying)
                {
                    SelectTree(subtreeNode.treeInstance);
                }
                else
                {
                    SelectTree(subtreeNode.treeAsset);
                }
            }
            else
            {
                Debug.LogError("No subtree assigned. Assign a behaviour tree to the tree asset field.");
            }
        }

        /// <summary>
        /// Pops the editor view back to a parent tree when navigating out of a sub-tree.
        /// </summary>
        public void PopToSubtree(int depth, BehaviourTree tree)
        {
            while (_breadcrumbs != null && _breadcrumbs.childCount > depth)
            {
                _breadcrumbs.PopItem();
            }

            _isSubtreeSelected = true;
            if (tree)
            {
                SelectTree(tree);
            }
        }

        /// <summary>
        /// Clears the breadcrumb navigation in the editor, resetting it to the root tree view.
        /// </summary>
        public void ClearBreadcrumbs()
        {
            while (_breadcrumbs != null && _breadcrumbs.childCount > 0)
            {
                _breadcrumbs.PopItem();
            }
        }
        
        /// <summary>
        /// Unity callback for drawing GUI elements in the editor window.
        /// </summary>
        private void OnGUI()
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape && 
                serializer != null && _overlayView.isShown)
            {
                _overlayView.Hide();
            }
        }

        /// <summary>
        /// Creates a new node in the behavior tree for a script that was just created and compiled.
        /// </summary>
        private void CreatePendingScriptNode()
        {
            NodeView source = treeView.GetNodeByGuid(nodeScriptCompilationTracker.sourceGuid) as NodeView;
            var nodeType = Type.GetType($"{nodeScriptCompilationTracker.scriptName}, Assembly-CSharp");
            
            var typeName = nodeScriptCompilationTracker.scriptName;

            if (nodeType == null)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    nodeType = assembly.GetType(typeName);
                    if (nodeType != null)
                    {
                        break;
                    }
                }
            }

            if (nodeType != null)
            {
                BehaviourTreeEditorUtility.CreateAndSelectNode(source, treeView, nodeType, 
                    nodeScriptCompilationTracker.nodePosition, nodeScriptCompilationTracker.isSourceParent);
            }

            nodeScriptCompilationTracker.Reset();
        }

        /// <summary>
        /// Callback function for handling undo and redo operations within the editor.
        /// </summary>
        private void OnUndoRedo()
        {
            if (_tree != null && serializer != null && serializer.serializedObject != null)
            {
                serializer.serializedObject.Update();
                treeView.PopulateView(serializer);
                _blackboardView?.RefreshListView();
            }
        }

        /// <summary>
        /// Unity callback for when the editor window is enabled, setting up various event listeners.
        /// </summary>
        private void OnEnable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.undoRedoPerformed += OnUndoRedo;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>
        /// Unity callback for when the editor window is disabled, cleaning up event listeners.
        /// </summary>
        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        /// <summary>
        /// Callback for handling changes in the play mode state, used to refresh or clear the editor view as needed.
        /// </summary>
        private void OnPlayModeStateChanged(PlayModeStateChange stateChange)
        {
            switch (stateChange)
            {
                case PlayModeStateChange.EnteredEditMode:
                    EditorApplication.delayCall -= OnSelectionChange;
                    EditorApplication.delayCall += OnSelectionChange;
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    EditorApplication.delayCall -= OnSelectionChange;
                    EditorApplication.delayCall += OnSelectionChange;
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    _inspectorView?.Clear();
                    break;
            }
        }

        /// <summary>
        /// Callback for Unity's selection change event, used to update the editor based on the current selection.
        /// </summary>
        private void OnSelectionChange()
        {
            if (Selection.activeGameObject)
            {
                BehaviourTreeInstanceRunner instanceRunner = 
                    Selection.activeGameObject.GetComponent<BehaviourTreeInstanceRunner>();
                if (instanceRunner && instanceRunner.behaviourTree)
                {
                    SelectNewTree(instanceRunner.behaviourTree);
                }
            }
        }

        /// <summary>
        /// Selects a new behavior tree to be edited in the window, updating the view and internal state.
        /// </summary>
        private void SelectNewTree(BehaviourTree tree)
        {
            ClearBreadcrumbs();
            SelectTree(tree);
        }

        /// <summary>
        /// Selects and focuses on a given behavior tree, updating all relevant views and editor components.
        /// </summary>
        private void SelectTree(BehaviourTree newTree)
        {
            // If tree view is null the window is probably unfocused
            if (treeView == null)
            {
                return;
            }

            if (!newTree)
            {
                ClearBreadcrumbs();
                ClearSelection();
                _overlayView.Show(false);
                return;
            }

            if (newTree != _tree)
            {
                if (!_isSubtreeSelected)
                {
                    ClearBreadcrumbs();
                }

                _isSubtreeSelected = false;
                ClearSelection();
            }
            
            _tree = newTree;
            serializer = new SerializedBehaviourTree(newTree);

            int childCount = _breadcrumbs.childCount;
            _breadcrumbs.PushItem($"{serializer.tree.name}", () => PopToSubtree(childCount, newTree));

            _overlayView?.Hide();
            treeView?.PopulateView(serializer);
            _blackboardView?.Bind(serializer);
        }

        /// <summary>
        /// Clears the current selection in the editor, resetting the view and internal state.
        /// </summary>
        private void ClearSelection()
        {
            _tree = null;
            serializer = null;
            _inspectorView?.Clear();
            treeView?.ClearView();
            _blackboardView?.ClearView();
        }

        /// <summary>
        /// Clears the editor view if the currently selected tree is being deleted.
        /// </summary>
        private void ClearIfSelected(string path)
        {
            if (serializer == null)
            {
                return;
            }

            if (AssetDatabase.GetAssetPath(serializer.tree) == path)
            {
                // Need to delay because this is called from a will delete asset callback
                EditorApplication.delayCall += () =>
                {
                    SelectTree(null);
                };
            }
        }

        /// <summary>
        /// Updates the inspector view based on the current node selection within the behavior tree.
        /// </summary>
        private void OnNodeSelectionChanged(NodeView node)
        {
            _inspectorView.UpdateSelection(serializer, node);
        }

        /// <summary>
        /// Unity callback for periodic updates in the inspector, used to refresh node states during play mode.
        /// </summary>
        private void OnInspectorUpdate()
        {
            if (Application.isPlaying)
            {
                treeView?.UpdateNodeStates();
            }
        }

        /// <summary>
        /// Handles creating a new behavior tree asset through the editor's toolbar.
        /// </summary>
        private void OnToolbarNewAsset()
        {
            ClearBreadcrumbs();
            _overlayView?.Show(true);
        }
    }
}
