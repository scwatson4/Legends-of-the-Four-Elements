using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Visual element representing an overlay in the behavior tree editor, used for creating or opening behavior trees.
    /// </summary>
    public class OverlayView : VisualElement
    {
        /// <summary>
        /// UxmlFactory class for BehaviourTreeView, enabling UIElements UXML instantiation.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<OverlayView, UxmlTraits>{}

        public System.Action<BehaviourTree> OnTreeSelected;
        public bool isShown;

        private const string DefaultTreeName = "New Behaviour Tree";
        private PopupField<string> assetSelector;
        private TextField treeNameField;
        private TextField locationPathField;
        
        /// <summary>
        /// Shows the overlay, optionally displaying only the creation menu.
        /// </summary>
        public void Show(bool isOnlyCreateMenuShown)
        {
            var settings = new SerializedObject(BehaviourTreeEditorWindow.Instance.settings);

            // Hidden in UIBuilder while editing..
            isShown = true;
            style.visibility = Visibility.Visible;

            // Configure fields
            treeNameField = this.Q<TextField>("TreeName");
            treeNameField.value = DefaultTreeName;
            locationPathField = this.Q<TextField>("LocationPath");
            var openButton = this.Q<Button>("OpenButton");
            var createButton = this.Q<Button>("CreateButton");
            var popupContainer = this.Q<VisualElement>("OpenAsset");
            var titleLabel = this.Q<Label>("title");
            var createLabel = this.Q<Label>("createLabel");
            
            if (isOnlyCreateMenuShown)
            {
                titleLabel.text = "Create Behaviour Tree";
                createLabel.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
                popupContainer.parent.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            }
            
            locationPathField.BindProperty(settings.FindProperty("newTreePath"));
            
            if (!isOnlyCreateMenuShown)
            {
                assetSelector = new PopupField<string>();
                assetSelector.label = "Asset";
                assetSelector.style.flexGrow = 1;
                assetSelector.style.flexShrink = 1;
                
                popupContainer.parent.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
                createLabel.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
                
                // Configure asset selection dropdown menu
                var behaviourTrees = BehaviourTreeEditorUtility.GetAssetPaths<BehaviourTree>();
#if !UNITY_2021_3_OR_NEWER && UNITY_2021
                var prop = assetSelector.GetType().GetField("m_Choices", System.Reflection.BindingFlags.NonPublic
                                                                         | System.Reflection.BindingFlags.Instance);
                var choices = prop.GetValue(assetSelector) as List<string>;
               
                behaviourTrees.ForEach(treePath => choices.Add(ToMenuFormat(treePath)));
                prop.SetValue(assetSelector, choices);
#else
                behaviourTrees.ForEach(treePath => assetSelector.choices.Add(ToMenuFormat(treePath)));
#endif
                popupContainer.Clear();
                popupContainer.Add(assetSelector);

                // // Configure open asset button
                openButton.clicked -= OnOpenAsset;
                openButton.clicked += OnOpenAsset;
            }

            // Configure create asset button
            createButton.clicked -= OnCreateAsset;
            createButton.clicked += OnCreateAsset;
        }

        /// <summary>
        /// Hides the overlay from view.
        /// </summary>
        public void Hide()
        {
            style.visibility = Visibility.Hidden;
            isShown = false;
        }

        /// <summary>
        /// Converts a tree path to a format suitable for use in a menu (replacing slashes with pipe characters).
        /// </summary>
        public string ToMenuFormat(string one)
        {
            // Using the slash creates submenus...
            return one.Replace("/", "|");
        }

        /// <summary>
        /// Converts a menu format string back to a tree path (replacing pipe characters with slashes).
        /// </summary>
        public string ToAssetFormat(string one)
        {
            // Using the slash creates submenus...
            return one.Replace("|", "/");
        }

        // Handler for the "Open" button click event, loading and selecting the chosen behavior tree asset.
        void OnOpenAsset()
        {
            string path = ToAssetFormat(assetSelector.text);
            BehaviourTree tree = AssetDatabase.LoadAssetAtPath<BehaviourTree>(path);
            if (tree)
            {
                TreeSelected(tree);
                style.visibility = Visibility.Hidden;
            }
        }

        // Handler for the "Create" button click event, creating a new behavior tree asset with the specified name and path.
        void OnCreateAsset()
        {
            BehaviourTree tree = BehaviourTreeEditorUtility.CreateNewTree(treeNameField.text, locationPathField.text);
            if (tree)
            {
                TreeSelected(tree);
                style.visibility = Visibility.Hidden;
            }
        }

        // Invokes the OnTreeSelected event with the specified behavior tree.
        void TreeSelected(BehaviourTree tree)
        {
            OnTreeSelected.Invoke(tree);
        }
    }
}
