using UnityEngine;
using System.Collections;

/// <summary>
/// Static class to manage the focus on IMGUI editable fields.
/// The only public function CheckOnEndChanges() will return a true when the value has been changed, and will return false while the value is being changing, so we don't get notified for the intermediate changes while writting a value.
/// We detect ENTER to finish, and also the change of focus, using TAB or moving to another input field with the mouse.
/// There is a counter in case a field in focus disappears by closing the window, as we don't have another way to know if the id is ok.
/// The counter works like a kind of timeout, if current focus is not alive in 100 frame events the ignore it
/// </summary>
namespace AnythingWorld.Editor
{
    public static class InputFocusManager
    {
        private static int currentFocusId = -1;
        private static string currentFocusValue = "";
        private static int newFocusId = -1;
        private static string newFocusValue = "";
        private static int countDownBeforeIgnore = 0;
        private static int maxCountDownBeforeIgnore = 100;

        public static bool CheckOnEndChanges(int _id, string _value, Event _currentEvent)
        {
            bool endChanges = false;
            bool changed = false;

            if (_id == 0) return false;

            // check for changes on focus
            CheckOnNewFocus(_id, _value);

            // check to finish changes
            if (_currentEvent.isKey && _currentEvent.type == EventType.KeyUp)
            {
                switch (_currentEvent.keyCode)
                {
                    case KeyCode.Return:
                    case KeyCode.KeypadEnter:
                    case KeyCode.Tab:
                        endChanges = true;
                        break;
                }
            }

            // handle changing of focus
            if (newFocusId != -1 && currentFocusId != -1 && currentFocusId != newFocusId)
            {
                endChanges = true;
            }

            // finishing edition ?
            if (endChanges)
            {
                if (_id == currentFocusId || currentFocusId == 0 || ++countDownBeforeIgnore >= maxCountDownBeforeIgnore)
                {

                    // does the value changed ?
                    if (currentFocusValue != _value)
                    {
                        currentFocusValue = _value;
                        changed = true;
                    }

                    // change focus if one is next
                    if (newFocusId != -1 || currentFocusId == 0)
                    {
                        currentFocusId = newFocusId;
                        currentFocusValue = newFocusValue;
                        newFocusId = -1;
                        newFocusValue = "";
                    }

                    if (countDownBeforeIgnore >= maxCountDownBeforeIgnore)
                    {
                        changed = false;
                    }
                    countDownBeforeIgnore = 0;
                }
                return changed;
            }
            return false;
        }

        private static bool CheckOnNewFocus(int _id, string _value)
        {
            if (_id == 0) return false;

            if (currentFocusId != GUIUtility.keyboardControl && (_id == GUIUtility.keyboardControl || GUIUtility.keyboardControl == 0))
            {
                if (currentFocusId == -1)
                {
                    currentFocusId = GUIUtility.keyboardControl;
                    currentFocusValue = _value;
                }
                else if (newFocusId == -1)
                {
                    newFocusId = GUIUtility.keyboardControl;
                    newFocusValue = _value;
                    return true;
                }
            }
            return false;
        }

        /// Sample code of how to use with 3 textfields
        /*
        void OnGUI ()
        {
            int id = 0;

            // first textfield
            id = GUIUtility.GetControlID(FocusType.Passive) + 1;
            textFieldString = GUI.TextField (new Rect (25, 25, 100, 30), textFieldString);
            if (CheckOnEndChanges(id, textFieldString, Event.current))
                Debug.Log("Value changed on textField1: " + currentFocusId.ToString() + "=" + textFieldString);

            // second textfield
            id = GUIUtility.GetControlID(FocusType.Passive) + 1;
            textFieldString2 = GUI.TextField (new Rect (25, 65, 100, 30), textFieldString2);
            if (CheckOnEndChanges(id, textFieldString2, Event.current))
                Debug.Log("Value changed on textField2: " + currentFocusId.ToString() + "=" + textFieldString2);

            // third textfield
            id = GUIUtility.GetControlID(FocusType.Passive) + 1;
            textFieldString3 = GUI.TextField (new Rect (25, 105, 100, 30), textFieldString3);
            if (CheckOnEndChanges(id, textFieldString3, Event.current))
                Debug.Log("Value changed on textField3: " + currentFocusId.ToString() + "=" + textFieldString3);
        }
        */
    }
}