using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Provides a window for searching and adding nodes of different types to a behavior tree,
    /// supporting dynamic node and script creation.
    /// </summary>
    public class CreateNodeWindow : ScriptableObject, ISearchWindowProvider
    {
        private Texture2D _icon;
        private BehaviourTreeView _treeView;
        private NodeView _source;
        private bool _isSourceParent;
        private BehaviourTreeEditorUtility.ScriptTemplate[] _scriptFileAssets;

        /// <summary>
        /// Static method to display the create node window at a specified position with the given context.
        /// </summary>
        public static void Show(Vector2 mousePosition, NodeView source, bool isSourceParent = false)
        {
            Vector2 screenPoint = GUIUtility.GUIToScreenPoint(mousePosition);
            CreateNodeWindow searchWindowProvider = ScriptableObject.CreateInstance<CreateNodeWindow>();
            searchWindowProvider.Initialise(BehaviourTreeEditorWindow.Instance.treeView, source, isSourceParent);
            SearchWindowContext windowContext = new SearchWindowContext(screenPoint, 240, 320);
            SearchWindow.Open(windowContext, searchWindowProvider);
        }
        
        /// <summary>
        /// Constructs the search tree for the create node window, defining the structure of the node
        /// and script creation options.
        /// </summary>
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Node")) { level = 0 }
            };

            // Action nodes can only be added as children.
            if (_isSourceParent || _source == null)
            {
                tree.Add(new SearchTreeGroupEntry(new GUIContent("Actions")) { level = 1 });
                var types = TypeCache.GetTypesDerivedFrom<ActionNode>().ToList();

                var movementTypes = new List<Type>();
                foreach (var t in types)
                {
                    if (t.Name.ToLower().Contains("move"))
                    {
                        movementTypes.Add(t);
                    } 
                }
                
                if (movementTypes.Count != 0)
                {
                    tree.Add(new SearchTreeGroupEntry(new GUIContent("Movement")) { level = 2 });
                    foreach (var type in movementTypes)
                    {
                        if (type.IsAbstract)
                        {
                            continue;
                        }
                        
                        AddCreateNodeSearchTreeEntry(tree, type, context, 3);
                    }
                }
                
                var randomGoalTypes = TypeCache.GetTypesDerivedFrom<RandomGoalBase>().ToList();
                if (randomGoalTypes.Count != 0)
                {
                    tree.Add(new SearchTreeGroupEntry(new GUIContent("RandomGoal")) { level = 2 });
                    foreach (var type in randomGoalTypes)
                    {
                        if (type.IsAbstract)
                        {
                            continue;
                        }
                        
                        AddCreateNodeSearchTreeEntry(tree, type, context, 3);
                    }
                }
                
                types = types.Except(movementTypes.Concat(randomGoalTypes)).ToList();
                
                foreach (var type in types)
                {
                    if (type.IsAbstract)
                    {
                        continue;
                    }

                    AddCreateNodeSearchTreeEntry(tree, type, context, 2);
                }
            }

            // Adds composite nodes to the search tree.
            tree.Add(new SearchTreeGroupEntry(new GUIContent("Composites")) { level = 1 });
            {
                var types = TypeCache.GetTypesDerivedFrom<CompositeNode>();
                
                foreach (var type in types)
                {
                    AddCreateNodeSearchTreeEntry(tree, type, context, 2);
                }
            }

            // Adds decorator nodes to the search tree.
            tree.Add(new SearchTreeGroupEntry(new GUIContent("Decorators")) { level = 1 });
            {
                var types = TypeCache.GetTypesDerivedFrom<DecoratorNode>();
                foreach (var type in types)
                {
                    AddCreateNodeSearchTreeEntry(tree, type, context, 2);
                }
            }

            // Adds options for creating new scripts
            tree.Add(new SearchTreeGroupEntry(new GUIContent("New Script...")) { level = 1 });

            Action createActionScript = () => CreateScript(_scriptFileAssets[0], context);
            CreateAndAddSearchTreeEntry(tree, "New Action Script", createActionScript, 2);

            Action createCompositeScript = () => CreateScript(_scriptFileAssets[1], context);
            CreateAndAddSearchTreeEntry(tree, "New Composite Script", createCompositeScript, 2);
            
            Action createDecoratorScript = () => CreateScript(_scriptFileAssets[2], context);
            CreateAndAddSearchTreeEntry(tree, "New Decorator Script", createDecoratorScript, 2);

            return tree;
        }

        /// <summary>
        /// Handles the selection of an item in the search tree, invoking the associated action.
        /// </summary>
        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            Action invoke = (Action)searchTreeEntry.userData;
            invoke();
            return true;
        }
        
        /// <summary>
        /// Retrieves the appropriate script template based on the specified type.
        /// </summary>
        private TextAsset GetScriptTemplate(int type)
        {
            var projectSettings = BehaviourTreeProjectSettings.GetOrCreateSettings();

            switch (type)
            {
                case 0:
                    if (projectSettings.scriptTemplateActionNode)
                    {
                        return projectSettings.scriptTemplateActionNode;
                    }
                    return BehaviourTreeEditorWindow.Instance.settings.scriptTemplateActionNode;
                case 1:
                    if (projectSettings.scriptTemplateCompositeNode)
                    {
                        return projectSettings.scriptTemplateCompositeNode;
                    }
                    return BehaviourTreeEditorWindow.Instance.settings.scriptTemplateCompositeNode;
                case 2:
                    if (projectSettings.scriptTemplateDecoratorNode)
                    {
                        return projectSettings.scriptTemplateDecoratorNode;
                    }
                    return BehaviourTreeEditorWindow.Instance.settings.scriptTemplateDecoratorNode;
            }
            Debug.LogError("Unhandled script template type:" + type);
            return null;
        }
        
        /// <summary>
        /// Creates a new node of the specified type at the determined position within the behavior tree view.
        /// </summary>
        private void CreateNode(Type type, SearchWindowContext context)
        {
            BehaviourTreeEditorWindow editorWindow = BehaviourTreeEditorWindow.Instance;
            
            var windowMousePosition = editorWindow.rootVisualElement.ChangeCoordinatesTo(
                editorWindow.rootVisualElement.parent, context.screenMousePosition - editorWindow.position.position);
            var graphMousePosition = editorWindow.treeView.contentViewContainer.WorldToLocal(windowMousePosition);
            var nodeOffset = new Vector2(-75, -20);
            var nodePosition = graphMousePosition + nodeOffset;
            
            BehaviourTreeEditorUtility.CreateAndSelectNode(_source, _treeView, type, 
                nodePosition, _isSourceParent);
        }
        
        /// <summary>
        /// Initiates the creation of a new script based on the specified template.
        /// </summary>
        private void CreateScript(BehaviourTreeEditorUtility.ScriptTemplate scriptTemplate, SearchWindowContext context)
        {
            BehaviourTreeEditorWindow editorWindow = BehaviourTreeEditorWindow.Instance;

            var windowMousePosition = editorWindow.rootVisualElement.ChangeCoordinatesTo(
                editorWindow.rootVisualElement.parent, context.screenMousePosition - editorWindow.position.position);
            var graphMousePosition = editorWindow.treeView.contentViewContainer.WorldToLocal(windowMousePosition);
            var nodeOffset = new Vector2(-75, -20);
            var nodePosition = graphMousePosition + nodeOffset;

            BehaviourTreeEditorUtility.CreateNewScript(scriptTemplate, _source, _isSourceParent, nodePosition);
        }
        
        /// <summary>
        /// Initialises the create node window with the necessary context for node creation.
        /// </summary>
        private void Initialise(BehaviourTreeView treeView, NodeView source, bool isSourceParent)
        {
            _treeView = treeView;
            _source = source;
            _isSourceParent = isSourceParent;

            _icon = new Texture2D(1, 1);
            _icon.SetPixel(0, 0, new Color(0, 0, 0, 0));
            _icon.Apply();

            _scriptFileAssets = new BehaviourTreeEditorUtility.ScriptTemplate[]
            {
                new BehaviourTreeEditorUtility.ScriptTemplate { templateFile = GetScriptTemplate(0), 
                    defaultFileName = "NewActionNode", subFolder = "Actions" },
                new BehaviourTreeEditorUtility.ScriptTemplate { templateFile = GetScriptTemplate(1), 
                    defaultFileName = "NewCompositeNode", subFolder = "Composites" },
                new BehaviourTreeEditorUtility.ScriptTemplate { templateFile = GetScriptTemplate(2), 
                    defaultFileName = "NewDecoratorNode", subFolder = "Decorators" },
            };
        }

        /// <summary>
        /// Helper method to add a node creation option to the search tree.
        /// </summary>
        private void AddCreateNodeSearchTreeEntry(List<SearchTreeEntry> tree, Type type, SearchWindowContext context, 
            int level)
        {
            Action invoke = () => CreateNode(type, context);
            CreateAndAddSearchTreeEntry(tree, $"{type.Name}", invoke, level);
        }
        
        /// <summary>
        /// Helper method to create and add a search tree entry with the specified action.
        /// </summary>
        private void CreateAndAddSearchTreeEntry(List<SearchTreeEntry> tree, string guiContentName, Action action,
            int level)
        {
            tree.Add(new SearchTreeEntry(new GUIContent(guiContentName)) { level = level, userData = action });
        }
    }
}
