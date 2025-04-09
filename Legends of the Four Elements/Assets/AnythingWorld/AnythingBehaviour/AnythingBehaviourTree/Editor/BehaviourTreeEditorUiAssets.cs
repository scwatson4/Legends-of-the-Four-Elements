using UnityEngine;
using UnityEngine.UIElements;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// UXML and USS assets for the Behaviour Tree Editor Window UI.
    /// </summary>
    public class BehaviourTreeEditorUiAssets : ScriptableObject
    {
        /// <summary>
        /// UXML that defines the structure and bindings of node inspector, blackboard and other editor window panels.
        /// </summary>
        public VisualTreeAsset behaviourTreeEditorXml;
        
        /// <summary>
        /// USS file that defines the visual style of the behaviour tree editor window.
        /// </summary>
        public StyleSheet behaviourTreeEditorStyle;
        
        /// <summary>
        /// UXML that defines the structure and bindings of node graph view.
        /// </summary>
        public VisualTreeAsset nodeViewXml;
    }
}
