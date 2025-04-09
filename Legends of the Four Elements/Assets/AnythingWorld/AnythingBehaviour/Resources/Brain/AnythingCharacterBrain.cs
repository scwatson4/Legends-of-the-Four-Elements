using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnythingWorld.Behaviour
{
    public class AnythingCharacterBrain : MonoBehaviour
    {
        [HideInInspector]
        public List<AnythingCharacterGoal> characterGoals = new List<AnythingCharacterGoal>();

        private AnythingCharacterGoal currentGoalInExecution = null;
        private AnythingCharacterGoal higherPriorityGoal = null;


        private void Update()
        {
            if (characterGoals.Count > 0)
            {
                if (characterGoals.Count >= 2)
                {
                    higherPriorityGoal = GetHigherPriorityGoal();
                }
                else
                {
                    higherPriorityGoal = characterGoals[0];
                }

                if (higherPriorityGoal != currentGoalInExecution)
                {
                    if (currentGoalInExecution)
                    {
                        currentGoalInExecution.TerminateGoalExecution();
                    }
                    ExecuteHigherPriorityGoal();
                    currentGoalInExecution = higherPriorityGoal;
                }
            }
        }

        private AnythingCharacterGoal GetHigherPriorityGoal()
        {
            AnythingCharacterGoal higherPGoal = null;
            float higherPGoalPriority = -1;

            foreach (var goal in characterGoals)
            {
                if (higherPGoal)
                {
                    float currentGoalpriority = goal.GetPriority();
                    if (currentGoalpriority > higherPGoalPriority)
                    {
                        higherPGoal = goal;
                        higherPGoalPriority = currentGoalpriority;
                    }
                }
                else
                {
                    higherPGoal = goal;
                    higherPGoalPriority = higherPGoal.GetPriority();
                }
            }

            return higherPGoal;
        }

        private void ExecuteHigherPriorityGoal()
        {
            if (higherPriorityGoal)
            {
                higherPriorityGoal.ExecuteGoal();
            }
        }
    }
}
