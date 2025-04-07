using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Visual element representing the new script dialog in the behavior tree editor,
    /// facilitating the creation of new node scripts.
    /// </summary>
    public class NewScriptDialogView : VisualElement
    {
        /// <summary>
        /// UxmlFactory class for BehaviourTreeView, enabling UIElements UXML instantiation.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<NewScriptDialogView, UxmlTraits>{}

        private const string DefaultPath = "Assets/";
        private BehaviourTreeEditorUtility.ScriptTemplate _scriptTemplate;
        private TextField _textField;
        private Button _confirmButton;
        private NodeView _source;
        private bool _isSourceParent;
        private Vector2 _nodePosition;

        /// <summary>
        /// Creates a new script of specific node type and adds the node to current behavior tree.
        /// </summary>
        public void CreateScript(BehaviourTreeEditorUtility.ScriptTemplate scriptTemplate, NodeView source, 
            bool isSourceParent, Vector2 position)
        {
            _scriptTemplate = scriptTemplate;
            _source = source;
            _isSourceParent = isSourceParent;
            _nodePosition = position;

            style.visibility = Visibility.Visible;

            var background = this.Q<VisualElement>("Background");
            var titleLabel = this.Q<Label>("Title");
            _textField = this.Q<TextField>("FileName");
            _confirmButton = this.Q<Button>();

            titleLabel.text = $"New {scriptTemplate.subFolder.TrimEnd('s')} Script";

            _textField.focusable = true;
            RegisterCallback<PointerEnterEvent>(_ =>
            {
                _textField[0].Focus();
            });

            _textField.RegisterCallback<KeyDownEvent>((e) =>
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    OnConfirm();
                }
            });

            _confirmButton.clicked -= OnConfirm;
            _confirmButton.clicked += OnConfirm;

            background.RegisterCallback<PointerDownEvent>((e) =>
            {
                e.StopImmediatePropagation(); 
                Close();
            });
        }

        // Closes the new script dialog view, hiding it from the user.
        void Close()
        {
            style.visibility = Visibility.Hidden;
        }

        // Confirms the creation of a new script, generating the script file and closing the dialog.
        void OnConfirm()
        {
            string scriptName = _textField.text;

            var newNodePath = $"{BehaviourTreeEditorWindow.Instance.settings.newNodePath}";
            if (newNodePath == DefaultPath || AssetDatabase.IsValidFolder(newNodePath))
            {
                var destinationFolder = System.IO.Path.Combine(newNodePath, _scriptTemplate.subFolder);
                var destinationPath = System.IO.Path.Combine(destinationFolder, $"{scriptName}.cs");

                System.IO.Directory.CreateDirectory(destinationFolder);

                var parentPath = System.IO.Directory.GetParent(Application.dataPath);

                string templateString = _scriptTemplate.templateFile.text;
                templateString = templateString.Replace("#SCRIPTNAME#", scriptName);
                string scriptPath = System.IO.Path.Combine(parentPath.ToString(), destinationPath);

                if (!System.IO.File.Exists(scriptPath))
                {
                    AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
                    System.IO.File.WriteAllText(scriptPath, templateString);
                    
                    BehaviourTreeEditorWindow.Instance.nodeScriptCompilationTracker.isPendingCreation = true;
                    BehaviourTreeEditorWindow.Instance.nodeScriptCompilationTracker.scriptName = scriptName;
                    BehaviourTreeEditorWindow.Instance.nodeScriptCompilationTracker.nodePosition = _nodePosition;
                    if (_source != null)
                    {
                        BehaviourTreeEditorWindow.Instance.nodeScriptCompilationTracker.sourceGuid = _source.node.guid;
                        BehaviourTreeEditorWindow.Instance.nodeScriptCompilationTracker.isSourceParent = _isSourceParent;
                    }
                    
                    AssetDatabase.ImportAsset(destinationPath, ImportAssetOptions.ForceUpdate);
                    _confirmButton.SetEnabled(false);
                }
                else
                {
                    Debug.LogError($"Script with that name already exists:{scriptPath}");
                    Close();
                }
            }
            else
            {
                Debug.LogError($"Invalid folder path:{newNodePath}. Check the project configuration settings " +
                               "'newNodePath' is configured to a valid folder");
            }
        }
        
        /// <summary>
        /// Disables tree interactions until the assembly with added script is finished reloading.
        /// </summary>
        private void OnAfterAssemblyReload()
        {
            _confirmButton.SetEnabled(true);
            Close();
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
        }
    }
}
