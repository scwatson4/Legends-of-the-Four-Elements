#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AnythingWorld.Behaviour
{
    [CustomEditor(typeof(PlayerCoreMovement))]
    public class PlayerCoreMovementEditor : UnityEditor.Editor 
    {
        bool displayLookVariables, displayMoveVariables, displayJumpVariables;

        /// <summary>
        /// Draws a custom inspector UI depending on what player controller type is in use.
        /// </summary>
        public override void OnInspectorGUI()
        {
            PlayerCoreMovement core = (PlayerCoreMovement)target;

            EditorGUI.BeginChangeCheck();
            core.MovementType = (PlayerCoreMovement.PlayerMovementType)EditorGUILayout.EnumPopup(new GUIContent("Movement Type", "What type of controller is this?"), core.MovementType);

            #region Look Controls
            switch (core.MovementType)
            {
                case PlayerCoreMovement.PlayerMovementType.FirstPerson:
                case PlayerCoreMovement.PlayerMovementType.ThirdPerson:
                    EditorGUILayout.Space();
                    displayLookVariables = EditorGUILayout.BeginFoldoutHeaderGroup(displayLookVariables, "Look Variables");
                    if (displayLookVariables)
                    {
                        core.RotationSensitivity = EditorGUILayout.FloatField(new GUIContent("Rotation Sensitivity", "How sensitive is the camera?"), core.RotationSensitivity);
                        core.RotationLimitY = EditorGUILayout.FloatField(new GUIContent("Rotation Pitch Limit", "How much is the camera allowed to rotate on the pitch axis?"), core.RotationLimitY);
                        core.InvertX = EditorGUILayout.Toggle(new GUIContent("Invert X", "Should the camera controls be inverted on the X axis?"), core.InvertX);
                        core.InvertY = EditorGUILayout.Toggle(new GUIContent("Invert Y", "Should the camera controls be inverted on the Y axis?"), core.InvertY);
                    }
                    EditorGUILayout.EndFoldoutHeaderGroup();
                    break;
                default:
                    break;
            }
            #endregion Look Controls

            #region Movement Controls
            EditorGUILayout.Space();
            displayMoveVariables = EditorGUILayout.BeginFoldoutHeaderGroup(displayMoveVariables, "Movement Variables");
            if (displayMoveVariables)
            {
                core.MovementSpeed = EditorGUILayout.FloatField(new GUIContent("Movement Speed", "What is the speed of the player?"), core.MovementSpeed);
                core.MovementAcceleration = EditorGUILayout.FloatField(new GUIContent("Resting Ease Speed", "How quickly should the player be able to reach their top walk speed from a resting state, and vice versa?"), core.MovementAcceleration);
                EditorGUILayout.Space();
                core.EnableRunning = EditorGUILayout.Toggle(new GUIContent("Enable Running", "Should the player be allowed to run?"), core.EnableRunning);
                if (core.EnableRunning)
                {
                    core.MovementRunSpeed = EditorGUILayout.FloatField(new GUIContent("Running Speed", "What is the speed of the player when running?"), core.MovementRunSpeed);
                    core.MovementRunTransitionSpeed = EditorGUILayout.FloatField(new GUIContent("Walk-Run Ease Speed", "How quickly should the player be able to reach their top running speed from a walking state, and vice versa?"), core.MovementRunTransitionSpeed);
                }
                EditorGUILayout.Space();
                core.EnableCrouching = EditorGUILayout.Toggle(new GUIContent("Enable Crouching", "Should the player be allowed to crouch?"), core.EnableCrouching);
                if (core.EnableCrouching)
                {
                    core.MovementCrouchSpeed = EditorGUILayout.FloatField(new GUIContent("Crouching Speed", "What is the speed of the player when crouching?"), core.MovementCrouchSpeed);
                    core.CharacterCrouchHeight = EditorGUILayout.FloatField(new GUIContent("Crouch Height", "What is the height of the player when crouching?"), core.CharacterCrouchHeight);
                    core.CharacterCrouchTransitionSpeed = EditorGUILayout.FloatField(new GUIContent("Crouch Height Ease Speed", "How quickly should the player be able to get into and out of crouching height?"), core.CharacterCrouchTransitionSpeed);
                }
                EditorGUILayout.Space();
                if (core.EnableRunning && core.EnableCrouching)
                {
                    core.EnableSliding = EditorGUILayout.Toggle(new GUIContent("Enable Sliding", "Should the player be allowed to slide?"), core.EnableSliding);
                    if (core.EnableSliding)
                    {
                        core.MovementSlideLength = EditorGUILayout.FloatField(new GUIContent("Slide Length", "What's the max length of time a player can slide?"), core.MovementSlideLength);
                        core.MovementSlideSpeed = EditorGUILayout.FloatField(new GUIContent("Slide Speed", "What is the speed of the player when sliding?"), core.MovementSlideSpeed);
                        core.CharacterSlideHeight = EditorGUILayout.FloatField(new GUIContent("Slide Height", "What is the height of the player when sliding?"), core.CharacterSlideHeight);
                        core.CharacterSlideTransitionSpeed = EditorGUILayout.FloatField(new GUIContent("Slide Height Ease Speed", "How quickly should the player be able to get into sliding height?"), core.CharacterSlideTransitionSpeed);
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            #endregion Movement Controls

            #region Gravity Controls
            switch (core.MovementType)
            {
                case PlayerCoreMovement.PlayerMovementType.FirstPerson:
                case PlayerCoreMovement.PlayerMovementType.ThirdPerson:
                case PlayerCoreMovement.PlayerMovementType.SideScroller:
                    EditorGUILayout.Space();
                    displayJumpVariables = EditorGUILayout.BeginFoldoutHeaderGroup(displayJumpVariables, "Jump/Gravity Variables");
                    if (displayJumpVariables)
                    {
                        core.EnableJumping = EditorGUILayout.Toggle(new GUIContent("Enable Jumping", "Should the player be allowed to jump?"), core.EnableJumping);
                        if (core.EnableJumping)
                        {
                            core.JumpForce = EditorGUILayout.FloatField(new GUIContent("Jump Force", "How much force should the player's jump have?"), core.JumpForce);
                            core.LowJumpMultiplier = EditorGUILayout.FloatField(new GUIContent("Low Jump Multiplier", "How much is the breaking force of a low jump? (Multiplier applied on Gravity)"), core.LowJumpMultiplier);
                        }
                        EditorGUILayout.Space();
                        core.FallMultiplier = EditorGUILayout.FloatField(new GUIContent("Fall Multiplier", "How much more forcefully should the player fall? (Multiplier applied on Gravity)"), core.FallMultiplier);
                        core.AerialMovementAcceleration = EditorGUILayout.FloatField(new GUIContent("Aerial Resting Ease Speed", "How quickly should the player be able to slow down to no movement speed when in the air?"), core.AerialMovementAcceleration);
                        core.GroundCheckRadius = EditorGUILayout.FloatField(new GUIContent("Ground Check Radius", "How far from the player's feet should the ground be detected?"), core.GroundCheckRadius);
                        core.GroundLayerMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(EditorGUILayout.MaskField(new GUIContent("Ground Layer Mask", "What layers count as ground to the player?"), InternalEditorUtility.LayerMaskToConcatenatedLayersMask(core.GroundLayerMask), InternalEditorUtility.layers));
                    }
                    EditorGUILayout.EndFoldoutHeaderGroup();
                    break;
                default:
                    break;
            }
            #endregion Gravity Controls

            #region Input Controls
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(new GUIContent("Inputs", "As per Unity's Input Manager, what inputs are to be used for this controller?"), new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            if (core.MovementType != PlayerCoreMovement.PlayerMovementType.SideScroller) core.MouseXLookAxisName = EditorGUILayout.TextField(new GUIContent("Yaw View Axis"), core.MouseXLookAxisName);
            if (core.MovementType != PlayerCoreMovement.PlayerMovementType.SideScroller) core.MouseYLookAxisName = EditorGUILayout.TextField(new GUIContent("Pitch View Axis"), core.MouseYLookAxisName);
            core.HorizontalMovementAxisName = EditorGUILayout.TextField(new GUIContent("Horizontal Movement Axis"), core.HorizontalMovementAxisName);
            if (core.MovementType != PlayerCoreMovement.PlayerMovementType.SideScroller) core.VerticalMovementAxisName = EditorGUILayout.TextField(new GUIContent("Vertical Movement Axis"), core.VerticalMovementAxisName);
            if (core.EnableJumping) core.JumpButtonName = EditorGUILayout.TextField(new GUIContent("Jump Button"), core.JumpButtonName);
            if (core.EnableRunning) core.RunButtonName = EditorGUILayout.TextField(new GUIContent("Run Button"), core.RunButtonName);
            if (core.EnableCrouching) core.CrouchButtonName = EditorGUILayout.TextField(new GUIContent("Crouch Button"), core.CrouchButtonName);
            #endregion Input Controls

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(target);
            }
        }
    }
}
#endif
