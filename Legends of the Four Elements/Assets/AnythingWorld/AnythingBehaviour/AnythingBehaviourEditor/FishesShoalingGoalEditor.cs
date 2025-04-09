using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AnythingWorld.Behaviour.Editor
{
    [CustomEditor(typeof(FishesShoalingGoal))]
    public class FishesShoalingGoalEditor : UnityEditor.Editor
    {
        private void OnSceneGUI()
        {
            var controller = (WalkInPackGoal)target;
            if (controller.walkingAnimalsPackController != null)
            {
                controller.walkingAnimalsPackController.anchor.position = Handles.PositionHandle(controller.walkingAnimalsPackController.anchor.position, Quaternion.identity);
                Handles.Label(controller.walkingAnimalsPackController.anchor.position, controller.walkingAnimalsPackController.anchor.position.ToString());
                //Handles.DrawWireDisc(controller.walkingAnimalsPackController.anchor.position, UnityEditor.SceneView.lastActiveSceneView.camera.transform.forward, controller.packMovementRange);
            }
    
        }
    }
}
