using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnythingWorld.Behaviour
{
    public abstract class AnythingCharacterGoal : MonoBehaviour
    {
        protected float priority;

        protected AnythingCharacterBrain animalBrain;

        protected AnythingCharacterGoal[] animalGoals;

        [HideInInspector]
        public static float distanceScaleFactor = 1f;

        public float GetPriority() { return UpdatePriority(priority); }

        public abstract float UpdatePriority(float priority);

        public abstract void ExecuteGoal();

        public abstract void TerminateGoalExecution();

        protected virtual void Start()
        {
            animalBrain = GetComponent<AnythingCharacterBrain>();

            if(animalBrain)
            {
                animalBrain.characterGoals.Add(this);
            }
            else
            {
                Debug.Assert(GetComponents<AnythingCharacterGoal>().Length == 1, "You cannot have more than one Goal on the gameobject [" + gameObject.name + "] without a Goal controller (such as a Brain).");
                ExecuteGoal();
            }
        }                                           
    }
}
