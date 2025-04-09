using AnythingWorld.Utilities;
using AnythingWorld.Voice;

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace AnythingWorld.Editor
{
    public class AICreatorEditor : AnythingCreatorEditor
    {
        private enum AIListenState
        {
            Waiting,
            Listening,
            Processing
        }

        private struct LabelInfo
        {
            public string label;
            public GUIStyle style;
        }

#if !UNITY_WEBGL
        private bool micActive = false;
        private string writtenPrompt = "";

        private AIListenState currentListenState = AIListenState.Waiting;
        private bool allCommandsExecuted;
        private bool writtenSubmission = false;
#region Wave Form Variables
        private int waveCount = 11;
        private float wavePadding = 0.6f;
        private float waveSensitivityScalar = 40f;
        private float waveIntensityScalar = 4f;
#endregion Wave Form Variables
#region Processing Variables
        private bool processing;
        public bool autoMake;
        public ParsedSpeechCommand lastMadeCommand;
#endregion Processing Variables
#endif

#region Textures
        private static Texture2D baseSpeechBubbleIcon;
        protected static Texture2D BaseSpeechBubbleIcon
        {
            get
            {
                if (baseSpeechBubbleIcon == null)
                {
                    baseSpeechBubbleIcon = Resources.Load("Editor/AICreator/speechBubbleIcon") as Texture2D;
                }
                return baseSpeechBubbleIcon;
            }
        }

        private static Texture2D baseMutedMicrophoneIcon;
        protected static Texture2D BaseMutedMicrophoneIcon
        {
            get
            {
                if (baseMutedMicrophoneIcon == null)
                {
                    baseMutedMicrophoneIcon = Resources.Load("Editor/AICreator/mutedMicrophoneIcon") as Texture2D;
                }
                return baseMutedMicrophoneIcon;
            }
        }

        private static Texture2D baseMicrophoneButtonInactive;
        protected static Texture2D BaseMicrophoneButtonInactive
        {
            get
            {
                if (baseMicrophoneButtonInactive == null)
                {
                    baseMicrophoneButtonInactive = Resources.Load("Editor/AICreator/inactiveMicrophone") as Texture2D;
                }
                return baseMicrophoneButtonInactive;
            }
        }

        private static Texture2D baseMicrophoneButtonActive;
        protected static Texture2D BaseMicrophoneButtonActive
        {
            get
            {
                if (baseMicrophoneButtonActive == null)
                {
                    baseMicrophoneButtonActive = Resources.Load("Editor/AICreator/activeMicrophone") as Texture2D;
                }
                return baseMicrophoneButtonActive;
            }
        }

        private static Texture2D baseWaveFormTopCap;
        protected static Texture2D BaseWaveFormTopCap
        {
            get
            {
                if (baseWaveFormTopCap == null)
                {
                    baseWaveFormTopCap = Resources.Load("Editor/AICreator/waveformTopCap") as Texture2D;
                }
                return baseWaveFormTopCap;
            }
        }

        private static Texture2D baseWaveFormBottomCap;
        protected static Texture2D BaseWaveFormBottomCap
        {
            get
            {
                if (baseWaveFormBottomCap == null)
                {
                    baseWaveFormBottomCap = Resources.Load("Editor/AICreator/waveformBottomCap") as Texture2D;
                }
                return baseWaveFormBottomCap;
            }
        }

        private static Texture2D baseWaveFormCore;
        protected static Texture2D BaseWaveFormCore
        {
            get
            {
                if (baseWaveFormCore == null)
                {
                    baseWaveFormCore = Resources.Load("Editor/AICreator/waveformFill") as Texture2D;
                }
                return baseWaveFormCore;
            }
        }

        private static Texture2D baseInactiveSpeechCircle;
        protected static Texture2D BaseInactiveSpeechCircle
        {
            get
            {
                if (baseInactiveSpeechCircle == null)
                {
                    baseInactiveSpeechCircle = Resources.Load("Editor/AICreator/inactiveSpeechCircle") as Texture2D;
                }
                return baseInactiveSpeechCircle;
            }
        }

        private static Texture2D baseActiveSpeechCircle;
        protected static Texture2D BaseActiveSpeechCircle
        {
            get
            {
                if (baseActiveSpeechCircle == null)
                {
                    baseActiveSpeechCircle = Resources.Load("Editor/AICreator/activeSpeechCircle") as Texture2D;
                }
                return baseActiveSpeechCircle;
            }
        }



        private Texture2D tintedMicrophoneButtonInactive;
        protected Texture2D TintedMicrophoneButtonInactive
        {
            get
            {
                if (tintedMicrophoneButtonInactive == null)
                {
                    tintedMicrophoneButtonInactive = TintTexture(BaseMicrophoneButtonInactive, HexToColour("CCCCCC"));
                }
                return tintedMicrophoneButtonInactive;
            }
        }

        private Texture2D tintedMicrophoneButtonActive;
        protected Texture2D TintedMicrophoneButtonActive
        {
            get
            {
                if (tintedMicrophoneButtonActive == null)
                {
                    tintedMicrophoneButtonActive = TintTexture(BaseMicrophoneButtonActive, HexToColour("DDDDDD"));
                }
                return tintedMicrophoneButtonActive;
            }
        }
#endregion Textures

#region Initialization
        /// <summary>
        /// Initializes and shows window, called from Anything World top bar menu.
        /// </summary>
        [MenuItem("Tools/Anything World/AI Creator", false, 23)]
        internal static void Initialize()
        {
            AnythingCreatorEditor tabWindow;
            Vector2 windowSize = new Vector2(500, 800);
            if (AnythingSettings.HasAPIKey)
            {
#if !UNITY_WEBGL
                CloseWindowIfOpen<AICreatorEditor>();
                var browser = GetWindow(typeof(AICreatorEditor), false, "AI Creator");
                browser.position = new Rect(EditorGUIUtility.GetMainWindowPosition().center - windowSize / 2, windowSize);

                browser.Show();
                browser.Focus();
#else
                DisplayAWDialog("WebGL Error!", "The AI Creator cannot work in WebGL, as the WebGL platform does not support Unity's Microphone class. Apologies for the inconvenience!");
#endif
            }
            else
            {
                CloseWindowIfOpen<ModelBrowserEditor>();
                CloseWindowIfOpen<MyWorldEditor>();
                CloseWindowIfOpen<AICreatorEditor>();
                tabWindow = GetWindow<LogInEditor>("Log In | Sign Up", false);
                tabWindow.position = new Rect(EditorGUIUtility.GetMainWindowPosition().center - windowSize / 2, windowSize);
            }
        }


        protected new void Awake()
        {
            base.Awake();
#if !UNITY_WEBGL
            AnythingVoice.parsedCommand = null;
#endif
            windowTitle = "AI Creator";
            bannerTintA = HexToColour("FFB800");
            bannerTintB = HexToColour("EB00FF");
        }
#endregion Initialization

#region Editor Drawing
        protected new void OnGUI()
        {
            InitializeResources();
            var banner = MaskWindowBanner();

            if (Event.current.type == EventType.Repaint && !AnythingSettings.HasAPIKey) Close();
#region Overwriting Editor Styles
            var backupLabelStyle = new GUIStyle(EditorStyles.label);
            var backupObjectStyle = new GUIStyle(EditorStyles.objectField);
            var backupNumberStyle = new GUIStyle(EditorStyles.numberField);
            var backupFoldoutStyle = new GUIStyle(EditorStyles.foldout);

            EditorStyles.label.font = GetPoppinsFont(PoppinsStyle.Bold);
            EditorStyles.objectField.font = GetPoppinsFont(PoppinsStyle.Medium);
            EditorStyles.numberField.font = GetPoppinsFont(PoppinsStyle.Medium);
            EditorStyles.foldout.font = GetPoppinsFont(PoppinsStyle.Bold);
            EditorStyles.foldout.fontSize = 16;
#endregion Overwriting Editor Styles

            if (!EditorGUIUtility.wideMode)
            {
                EditorGUIUtility.wideMode = true;
                EditorGUIUtility.labelWidth = 170;
            }

            try
            {
#if !UNITY_WEBGL
                DrawAICreator();
#endif
                DrawWindowBanner(banner);

#region Resetting Editor Styles
                EditorStyles.label.font = backupLabelStyle.font;
                EditorStyles.objectField.font = backupObjectStyle.font;
                EditorStyles.numberField.font = backupNumberStyle.font;
                EditorStyles.foldout.font = backupFoldoutStyle.font;
                EditorStyles.foldout.fontSize = backupFoldoutStyle.fontSize;
#endregion Resetting Editor Styles
            }
            catch (Exception e)
            {
                Debug.LogException(e);

#region Resetting Editor Styles
                EditorStyles.label.font = backupLabelStyle.font;
                EditorStyles.objectField.font = backupObjectStyle.font;
                EditorStyles.numberField.font = backupNumberStyle.font;
                EditorStyles.foldout.font = backupFoldoutStyle.font;
                EditorStyles.foldout.fontSize = backupFoldoutStyle.fontSize;
#endregion Resetting Editor Styles
            }
        }

#if !UNITY_WEBGL
        private void DrawAICreator()
        {
            var speechRectHeight = position.height - 250f;
            var speechRect = GUILayoutUtility.GetRect(position.width, speechRectHeight);

            DrawCirclePulse(DrawMicrophoneButton(speechRect, -speechRectHeight / 5), speechRect, AnythingVoice.GetCurrentVolumeRange(), 2, 0.55f, 1.2f, 25f);

            var genericStateRectHeightOffset = speechRectHeight / 4;
            var genericStateRectSize = new Vector2(position.width / 2f, 100f);
            var genericStateRect = new Rect(speechRect.center.x - (genericStateRectSize.x / 2), speechRect.center.y + genericStateRectHeightOffset - (genericStateRectSize.y / 2), genericStateRectSize.x, genericStateRectSize.y);

            switch(currentListenState)
            {
                case AIListenState.Waiting:
                    LabelInfo[] labels = {
                        new LabelInfo() { label = "TRY SAYING",                     style = new GUIStyle() { alignment = TextAnchor.MiddleCenter, fontSize = 18, font = GetPoppinsFont(PoppinsStyle.Bold), normal = SetStyleState(HexToColour("AAAAAA")) } },
                        new LabelInfo() { label = "\"make a horse\"",               style = new GUIStyle() { alignment = TextAnchor.MiddleCenter, fontSize = 18, font = GetPoppinsFont(PoppinsStyle.Regular), normal = SetStyleState(HexToColour("AAAAAA")) } },
                        new LabelInfo() { label = "\"create 20 cats\"",             style = new GUIStyle() { alignment = TextAnchor.MiddleCenter, fontSize = 18, font = GetPoppinsFont(PoppinsStyle.Regular), normal = SetStyleState(HexToColour("AAAAAA")) } }
                    };

                    for (int i = 0; i < labels.Length; i++)
                    {
                        var labelHeight = genericStateRect.height / labels.Length;
                        var labelRect = new Rect(genericStateRect.x, genericStateRect.y + (labelHeight * i), genericStateRect.width, labelHeight);

                        GUI.Label(labelRect, labels[i].label, labels[i].style);
                    }

                    break;
                case AIListenState.Listening:
                    DrawWaveForm(AnythingVoice.GetCurrentVolumeRange(), genericStateRect);
                    break;
                case AIListenState.Processing:
                    DrawLoadingSmall(genericStateRect, 0.5f);
                    break;
            }

            bool gridWindowOpen = HasOpenInstances<AISettingsEditor>();
            var creatorSettingsContent = new GUIContent("Settings", gridWindowOpen ? StateTransformIcon.activeTexture : StateTransformIcon.inactiveTexture);
            var creatorSettingsRectSize = new Vector2(90, 25);
            var creatorSettingsOffset = 10f;
            var creatorSettingsButtonRect = new Rect(speechRect.xMax - creatorSettingsRectSize.x - creatorSettingsOffset, speechRect.y + creatorSettingsOffset, creatorSettingsRectSize.x, creatorSettingsRectSize.y);

            if (DrawRoundedButton(creatorSettingsButtonRect, creatorSettingsContent, gridWindowOpen))
            {
                if (gridWindowOpen)
                {
                    CloseWindowIfOpen<AISettingsEditor>();
                }
                else
                {
                    AISettingsEditor.Initialize();
                }
            }

            Repaint();
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();

            var lineBarrier = GUILayoutUtility.GetRect(position.width, 1);
            EditorGUI.DrawRect(new Rect(lineBarrier.position.x, lineBarrier.position.y, position.width, position.xMax - lineBarrier.position.x), HexToColour("373839"));
            EditorGUI.DrawRect(lineBarrier, HexToColour("575859"));

            var textInputMargins = 0.1f;

            GUILayout.FlexibleSpace();
            var textRect = GUILayoutUtility.GetRect(position.width, 40);
            textRect.width -= position.width * (textInputMargins * 2);
            textRect.x += position.width * textInputMargins;
            if (Event.current.Equals(Event.KeyboardEvent("return")))
            {
                SubmitStringCommand().Forget();
            }

            DrawTextInputBox(textRect);

            GUILayout.Space(20f);

            var submitRect = GUILayoutUtility.GetRect(position.width, 40);
            submitRect.width -= position.width * (textInputMargins * 2);
            submitRect.x += position.width * textInputMargins;
            DrawSubmitButton(submitRect);
            GUILayout.FlexibleSpace();

            DrawResults(genericStateRect.center, position.width * 0.9f, 2f);

            processing = AnythingVoice.uploadInProgress;
            if(processing) currentListenState = AIListenState.Processing;

            if (!AnythingVoice.uploadInProgress && currentListenState == AIListenState.Processing)
            {
                currentListenState = AIListenState.Waiting;
            }

        }

        private void DrawSubmitButton(Rect buttonRect)
        {
            if (DrawRoundedButton(buttonRect, new GUIContent("Submit"), 18))
            {
                SubmitStringCommand().Forget();
            }
        }

        private async UniTask SubmitStringCommand()
        {
            await AnythingVoice.RequestCommandsFromStringAsync(writtenPrompt, EditorNetworkErrorHandler.HandleError);
            allCommandsExecuted = false;
            writtenPrompt = "";
            writtenSubmission = true;
        }

        private async UniTask SubmitVoiceCommandAsync()
        {
            AnythingVoice.isRecording = false;
            var clip = AnythingVoice.StopRecording();
            await AnythingVoice.ExtractBytesAndProcessAsync(clip, EditorNetworkErrorHandler.HandleError);
            allCommandsExecuted = false;
            writtenSubmission = false;
        }

        private void DrawCirclePulse(Vector2 centre, Rect speechRect, float waveIntensity, int circleCount = 2, float circleSpacer = 0.75f, float overflowScalar = 1.1f, float pulseScalar = 1f)
        {
            var maxDimension = speechRect.width * overflowScalar;

            for (int i = 0; i < circleCount; i++)
            {
                var localCircleSize = i == 0 ? maxDimension : maxDimension * (circleSpacer / i);
                var originalCircleSize = localCircleSize;
                switch (currentListenState)
                {
                    case AIListenState.Waiting:
                    case AIListenState.Processing:
                        localCircleSize = originalCircleSize + (Mathf.Sin((float)EditorApplication.timeSinceStartup) * pulseScalar);
                        break;
                    case AIListenState.Listening:
                        localCircleSize = originalCircleSize + (waveIntensity * 5f * pulseScalar);
                        break;
                }

                var circle = new Rect(centre.x - (localCircleSize / 2), centre.y - (localCircleSize / 2), localCircleSize, localCircleSize);
                GUI.DrawTexture(circle, currentListenState == AIListenState.Listening ? BaseActiveSpeechCircle : BaseInactiveSpeechCircle, ScaleMode.ScaleAndCrop);
            }
        }

        private Vector2 DrawMicrophoneButton(Rect speechRect, float positionOffset = 0f)
        {
            var buttonSize = Mathf.Min(position.width * 0.2f, position.height * 0.2f, 128f);

            var buttonRect = new Rect(speechRect.center.x - (buttonSize / 2), (speechRect.center.y + positionOffset) - (buttonSize / 2), buttonSize, buttonSize);

            var textStyle = new GUIStyle(EditorStyles.label) { font = GetPoppinsFont(PoppinsStyle.Medium), fontSize = 20, alignment = TextAnchor.MiddleCenter, wordWrap = true, normal = SetStyleState(Color.white), hover = SetStyleState(Color.white) };
            var textContent = new GUIContent(processing ? "Processing..." : "Tap to record, tap again to stop");
            var textRect = new Rect(speechRect.center.x / 2, buttonRect.yMax, speechRect.width - speechRect.center.x, textStyle.CalcHeight(textContent, speechRect.width - speechRect.center.x));

            var activeStyle = new GUIStyle(IconStyle) { normal = SetStyleState(BaseMicrophoneButtonActive), hover = SetStyleState(TintedMicrophoneButtonActive) };
            var inactiveStyle = new GUIStyle(IconStyle) { normal = SetStyleState(BaseMicrophoneButtonInactive), hover = SetStyleState(TintedMicrophoneButtonInactive) };

            GUI.enabled = !AnythingVoice.uploadInProgress;
            if (!AnythingVoice.isRecording)
            {
                if (GUI.Button(buttonRect, "", inactiveStyle))
                {
                    currentListenState = AIListenState.Listening;
                    micActive = !micActive;
                    AnythingVoice.StartRecording();
                }
            }
            else
            {
                if (GUI.Button(buttonRect, "", activeStyle))
                {
                    SubmitVoiceCommandAsync().Forget();
                }
            }
            GUI.enabled = true;

            GUI.Label(textRect, textContent, textStyle);
            return buttonRect.center;
        }

        private bool DrawTextInputBox(Rect fieldRect)
        {
            bool changed = false;
            writtenPrompt = DrawRoundedInputField(fieldRect, writtenPrompt, ref changed);
            if (string.IsNullOrEmpty(writtenPrompt)) GUI.Label(fieldRect, "...or type what you'd like to create!", new GUIStyle(BodyLabelStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 14, font = GetPoppinsFont(PoppinsStyle.Regular), normal = SetStyleState(HexToColour("575859")) });
            GUI.enabled = true;

            return changed;
        }

        private void DrawResults(Vector2 centre, float width, float marginSize)
        {
            if (AnythingVoice.parsedCommand != null)
            {
                var actionHeight = 40f;
                var actionPadding = 16f;

                if (string.IsNullOrEmpty(AnythingVoice.parsedCommand.text))
                {
                    List<Voice.Action> defaultActions = new List<Voice.Action>
                    {
                        new Voice.Action()
                        {
                            action = "add_model",
                            actionEnum = CommandSchema.ActionSchema.add_model,
                            models = new List<Model>()
                            {
                                new Model() { name = "cat#0000", number = 4 }
                            },
                            text = "Create 4 cats"
                        },
                        new Voice.Action()
                        {
                            action = "add_model",
                            actionEnum = CommandSchema.ActionSchema.add_model,
                            models = new List<Model>()
                            {
                                new Model() { name = "llama#0000", number = 2 }
                            },
                            text = "Create 2 llamas"
                        },
                        new Voice.Action()
                        {
                            action = "add_model",
                            actionEnum = CommandSchema.ActionSchema.add_model,
                            models = new List<Model>()
                            {
                                new Model() { name = "bumblebee#0000", number = 20 }
                            },
                            text = "Create 20 bumblebees"
                        }
                    };

                    var actionCount = defaultActions.Count;

                    var introContent = new GUIContent($"We didn't quite get that. Why not try...");
                    var introStyle = new GUIStyle(BodyLabelStyle) { font = GetPoppinsFont(PoppinsStyle.Regular), wordWrap = true, fontSize = 16, alignment = TextAnchor.MiddleCenter };

                    var introHeight = introStyle.CalcHeight(introContent, width);
                    introContent.image = BaseMutedMicrophoneIcon;
                    var iconScalar = 0.8f;

                    EditorGUIUtility.SetIconSize(Vector2.one * introHeight * iconScalar);

                    var totalHeight = (actionHeight * actionCount) + (actionPadding * (actionCount + 1)) + introHeight + actionPadding;

                    var resultsRect = DrawSquareInSquare(centre, width, totalHeight, marginSize);

                    var introRect = new Rect(resultsRect.x + actionPadding, resultsRect.y + actionPadding, resultsRect.width - (actionPadding * 2), introHeight);
                    GUI.Label(introRect, introContent, introStyle);
                    for (int i = 0; i < actionCount; i++)
                    {
                        var yPos = introRect.yMax + actionPadding + (actionHeight * i) + (actionPadding * i);
                        var buttonRect = new Rect(resultsRect.x + actionPadding, yPos, resultsRect.width - (actionPadding * 2), actionHeight);

                        var buttonContent = new GUIContent(defaultActions[i].text, defaultActions[i].actionExecuted ? BaseConfirmIcon : null);
                        if (DrawRoundedButton(buttonRect, buttonContent, 16) && !defaultActions[i].actionExecuted)
                        {
                            for (int j = 0; j < defaultActions[i].models.Count; j++)
                            {
                                Voice.CommandHandler.ParseActionModel(defaultActions[i], defaultActions[i].models[j], DefaultBehavioursUtility.CreateNewTemporaryInstance(DefaultBehaviourDictionary));
                            }
                            defaultActions[i].actionExecuted = true;
                        }
                    }
                }
                else
                {
                    var actionCount = AnythingVoice.parsedCommand.actions.Count;

                    var introContent = new GUIContent($"{(writtenSubmission ? "You wrote" : "We heard")}: \"{AnythingVoice.parsedCommand.text}\"");
                    var introStyle = new GUIStyle(BodyLabelStyle) { font = GetPoppinsFont(PoppinsStyle.Regular), wordWrap = true, fontSize = 16, alignment = TextAnchor.MiddleCenter };

                    var introHeight = introStyle.CalcHeight(introContent, width);
                    introContent.image = BaseSpeechBubbleIcon;
                    var iconScalar = 0.8f;

                    EditorGUIUtility.SetIconSize(Vector2.one * introHeight * iconScalar);

                    var totalHeight = (actionHeight * (actionCount + 1)) + (actionPadding * (actionCount + 2)) + introHeight + actionPadding;

                    var resultsRect = DrawSquareInSquare(centre, width, totalHeight, marginSize);

                    GUI.Label(new Rect(resultsRect.x + actionPadding, resultsRect.y + actionPadding, resultsRect.width - (actionPadding * 2), introHeight), introContent, introStyle);
                    DrawCreationCommands(AnythingVoice.parsedCommand.actions, resultsRect, actionPadding, actionHeight, resultsRect.width * 0.4f, introHeight + actionPadding);
                }
            }
        }

        private Rect DrawCreationCommands(List<Voice.Action> actions, Rect resultsRect, float actionPadding, float actionHeight, float actionWidth, float offset)
        {
            var labelStyle = new GUIStyle(BodyLabelStyle) { font = GetPoppinsFont(PoppinsStyle.Regular), wordWrap = true, fontSize = 14, alignment = TextAnchor.MiddleLeft };

            for (int i = 0; i < actions.Count; i++)
            {
                var yPos = resultsRect.y + offset + actionPadding + (actionHeight * i) + (actionPadding * i);
                var buttonRect = new Rect(resultsRect.xMax - actionWidth - actionPadding, yPos, actionWidth, actionHeight);
                var labelRect = new Rect(resultsRect.x + actionPadding, yPos, resultsRect.width - actionWidth - (actionPadding * 2), actionHeight);

                var labelContent = new GUIContent($"\"{actions[i].text}\"", BaseSpeechBubbleIcon);

                var iconScalar = 0.8f;
                EditorGUIUtility.SetIconSize((Vector2.one * labelRect.height) * iconScalar);

                GUI.Label(labelRect, labelContent, labelStyle);

                var buttonContent = new GUIContent(GetHumanReadableAction(actions[i].actionEnum), actions[i].actionExecuted ? BaseConfirmIcon : null);
                if (DrawRoundedButton(buttonRect, buttonContent, 16) && !actions[i].actionExecuted)
                {
                    for (int j = 0; j < actions[i].models.Count; j++)
                    {
                        if (AnythingVoice.parsedCommand != null && AnythingVoice.parsedCommand.result == CommandResult.Success)
                        {
                            Voice.CommandHandler.ParseActionModel(actions[i], actions[i].models[j], DefaultBehavioursUtility.CreateNewTemporaryInstance(DefaultBehaviourDictionary));
                        }
                    }
                    actions[i].actionExecuted = true;
                }
            }

            allCommandsExecuted = actions.TrueForAll(x => x.actionExecuted);
            var allCommandsButtonRect = new Rect(resultsRect.x + actionPadding, resultsRect.y + offset + actionPadding + (actionHeight * actions.Count) + (actionPadding * actions.Count), resultsRect.width - (actionPadding * 2), actionHeight);
            var allCommandsButtonContent = new GUIContent("Execute All Commands", allCommandsExecuted ? BaseConfirmIcon : null);

            if (AnythingSettings.AutoCreateInAICreator)
            {
                if (AnythingVoice.parsedCommand != null && AnythingVoice.parsedCommand.result == CommandResult.Success)
                {
                    if (lastMadeCommand == null || lastMadeCommand != AnythingVoice.parsedCommand)
                    {
                        Voice.CommandHandler.ParseCommand(AnythingVoice.parsedCommand, DefaultBehavioursUtility.CreateNewTemporaryInstance(DefaultBehaviourDictionary));
                        lastMadeCommand = AnythingVoice.parsedCommand;
                    }
                }

                for (int i = 0; i < actions.Count; i++) actions[i].actionExecuted = true;

                var autoCreateContent = new GUIContent("You have Auto-Create turned on. This automatically executes all commands.");
                GUI.Label(allCommandsButtonRect, autoCreateContent, labelStyle);
            }
            else
            {
                if (DrawRoundedButton(allCommandsButtonRect, allCommandsButtonContent, 16) && !allCommandsExecuted)
                {
                    if (AnythingVoice.parsedCommand != null && AnythingVoice.parsedCommand.result == CommandResult.Success)
                    {
                        Voice.CommandHandler.ParseCommand(AnythingVoice.parsedCommand, DefaultBehavioursUtility.CreateNewTemporaryInstance(DefaultBehaviourDictionary));
                    }
                    for (int i = 0; i < actions.Count; i++) actions[i].actionExecuted = true;
                }
            }

            return new Rect(resultsRect.x, resultsRect.y + offset, resultsRect.width, allCommandsButtonRect.yMax - (resultsRect.y + offset));
        }

        private string GetHumanReadableAction(CommandSchema.ActionSchema schema)
        {
            var stringSchema = schema.ToString();

            string returnStr = "";
            foreach (var str in stringSchema.Split('_'))
            {
                returnStr = string.Join(" ", new[] { returnStr, (str[0].ToString().ToUpper() + str.Substring(1)) });
            }

            return returnStr;
        }

        private void DrawWaveForm(float waveIntensity, Rect waveFormRect)
        {
            for (int i = 0; i < waveCount; i++)
            {
                var minWaveHeight = BaseWaveFormBottomCap.height + BaseWaveFormTopCap.height;
                var maxWaveHeight = waveFormRect.height / 2;
                var waveDistance = ((waveCount - 1) / 2) - (waveCount - 1) + i;
                var waveTrailOff = waveIntensity * -Mathf.Abs(waveDistance);
                float localWaveHeight;

#region Noise
                localWaveHeight = Mathf.Clamp(minWaveHeight + (waveSensitivityScalar * waveTrailOff) + (waveIntensityScalar * UnityEngine.Random.Range(0f, 1f) * (maxWaveHeight - minWaveHeight) * waveIntensity), minWaveHeight, maxWaveHeight);
#endregion Noise

                var waveRect = new Rect(waveFormRect.center.x + waveDistance * ((waveFormRect.width / (waveCount - 1)) * wavePadding),
                                        waveFormRect.yMin + ((waveFormRect.height - localWaveHeight) / 2),
                                        BaseWaveFormCore.width,
                                        localWaveHeight);

                var waveTopCapRect = new Rect(waveRect.xMin, waveRect.yMin, BaseWaveFormTopCap.width, BaseWaveFormTopCap.height);
                var waveBottomCapRect = new Rect(waveRect.xMin, (waveRect.yMax - BaseWaveFormBottomCap.height), BaseWaveFormBottomCap.width, BaseWaveFormBottomCap.height);
                var waveCoreRect = new Rect(waveRect.xMin, waveTopCapRect.yMax, BaseWaveFormCore.width, waveRect.height - waveBottomCapRect.height - waveTopCapRect.height + 2);

                GUI.DrawTexture(waveTopCapRect, BaseWaveFormTopCap);
                GUI.DrawTexture(waveBottomCapRect, BaseWaveFormBottomCap);
                GUI.DrawTexture(waveCoreRect, BaseWaveFormCore, ScaleMode.StretchToFill);
            }
        }
#endif
#endregion Editor Drawing
    }
}
