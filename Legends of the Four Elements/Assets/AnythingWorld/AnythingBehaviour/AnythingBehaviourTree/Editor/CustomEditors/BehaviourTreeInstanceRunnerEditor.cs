using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnythingWorld.Behaviour.Tree 
{
    /// <summary>
    /// Custom editor class for BehaviourTreeInstanceRunner, defining the layout and behavior of its properties
    /// in the Unity Inspector.
    /// </summary>
    [CustomEditor(typeof(BehaviourTreeInstanceRunner))]
    public class BehaviourTreeInstanceRunnerEditor : Editor
    {
        /// <summary>
        /// Creates the custom inspector GUI elements for BehaviourTreeInstanceRunner.
        /// </summary>
        public override VisualElement CreateInspectorGUI() 
        {
            var btRunner = target as BehaviourTreeInstanceRunner;
            
            VisualElement container = new VisualElement();

            var treeField = new ObjectField
            {
                label = "Behaviour Tree",
                objectType = typeof(BehaviourTree),
                allowSceneObjects = false
            };
            
            treeField.bindingPath = nameof(BehaviourTreeInstanceRunner.behaviourTree);

            PropertyField validateField = new PropertyField();
            validateField.bindingPath = nameof(BehaviourTreeInstanceRunner.validate);

            PropertyField publicKeys = new PropertyField();
            publicKeys.bindingPath = nameof(BehaviourTreeInstanceRunner.blackboardOverrides);
                
            var openEditorButton = new Button(() =>
            {
                if (btRunner.behaviourTree != null)
                {
                    BehaviourTreeEditorWindow.OpenWindow(btRunner.behaviourTree);
                }
            });
            openEditorButton.text = "Open Selected Tree";
            
            container.Add(treeField);
            container.Add(openEditorButton);
            container.Add(validateField);
            container.Add(publicKeys);

            return container;
        }
    }
}
