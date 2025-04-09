using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
namespace AnythingWorld.Animation
{
    [CustomEditor(typeof(LegacyAnimationController),true)]
    public class LegacyAnimationControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
    
        }
    }
}
#endif
