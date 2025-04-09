using AnythingWorld.Utilities;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AnythingWorld.Behaviour
{
    public class HuntGoal : AnythingCharacterGoal
    {
        [Header("Gizmos Controller")]
        [Tooltip("Show the perimeter in where an animal will move to search for its preys.")]
        public bool showSearchingForPreyRange = false;
        [Tooltip("Show the range in where an animal can detect the presence of its preys.")]
        public bool showDetectPreyRange = false;

        [Header("Hunting Settings")]
        [Tooltip("List of animals that will be hunted (based on the animal Entity).")]
        public string[] animalsToHuntList;
        [Tooltip("Set the perimeter in where an animal will move to search for its preys.")]
        public float searchingForPreyMovementRange = 20f;
        [Tooltip("Set the range in where an animal can detect the presence of its preys.")]
        public float preyDetectionRange = 10f;
        [Tooltip("Select to choose a new center point of the perimeter in where an animal will move to search for its preys.")]
        public bool setNewSearchingPreyCenterPoint = false;
        [Tooltip("Set the new center point Axis X of the perimeter in where an animal will move to search for its preys.")]
        public float newSearchingPreyCenterPointX;
        [Tooltip("Set the new center point Axis Z of the perimeter in where an animal will move to search for its preys.")]
        public float newSearchingPreyCenterPointZ;

        protected GoToDestination goToDestinationBehaviourComponent;
        protected bool goalActivated = false;
        protected bool isChasingPrey = false;
        protected float detectPreyTimer = 0f;
        protected GameObject chasedPrey;
        protected Vector3 spawnPoint;
        protected float defaultTurnSpeed;
        protected float defaultGoalRadius;

        protected const float huntingTurnSpeed = 5f; //@Hardcoded
        protected const float huntingGoalRadius = 0.5f; //@Hardcoded
        protected const float predatorAditionalSpeed = 1f; //@Hardcoded

        //Brain Related Variables
        private float hunger;
        private const float hungerIncreaseFactor = 0.2f; //@Hardcoded

        public const float searchForPreyFrequencyInSeconds = 1f; //@Hardcoded


        protected override void Start()
        {
            base.Start();
            CreateTriggerOnlyBoxCollider();
            detectPreyTimer = searchForPreyFrequencyInSeconds;
            spawnPoint = gameObject.transform.position;

            //For now, the animals will not start with hunger (and will not go hunt right away) because it is conflicting with Pack Behaviour
            //When a follower elects a new leader that goes to hunt it will not receive from the leader a destination to go (so the follower will be freezed)
            //Possible Solution (TODO): when the Pack Behaviour is not selected by the brain and the animal was selected as leader make its followers choose another leader
            //The brain will inform the Pack Behaviour component to do that.
            hunger = Random.Range(0, 49f);
        }

        protected void CreateTriggerOnlyBoxCollider()
        {
            //This box collider is created in order that this predator can be detected by its preys
            //so its preys can trigger fleeing behaviour
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
        }

        protected virtual void Update()
        {
            if (!goalActivated) { return; }

            if (!isChasingPrey)
            {
                detectPreyTimer += Time.deltaTime;
                if(detectPreyTimer >= searchForPreyFrequencyInSeconds)
                {
                    TryToDetectPrey();
                    detectPreyTimer -= searchForPreyFrequencyInSeconds;
                }
            }
            else
            {
                if(goToDestinationBehaviourComponent && chasedPrey)
                {
                    if(Vector3.Distance(transform.position, chasedPrey.transform.position) <= preyDetectionRange)
                    {
                        goToDestinationBehaviourComponent.speed = goToDestinationBehaviourComponent.maxSpeed + predatorAditionalSpeed;
                        goToDestinationBehaviourComponent.turnSpeed = huntingTurnSpeed; 
                        goToDestinationBehaviourComponent.goalRadius = huntingGoalRadius; 
                        goToDestinationBehaviourComponent.SetMyDestination(new Vector3(chasedPrey.transform.position.x, spawnPoint.y, chasedPrey.transform.position.z));
                    }
                    else
                    {
                        isChasingPrey = false;
                        chasedPrey = null;
                        SearchForPrey();
                    }
                    
                }
            }
        }

        public override void ExecuteGoal()
        {
            if (!goToDestinationBehaviourComponent)
            {
                goToDestinationBehaviourComponent = gameObject.AddComponent(typeof(GoToDestination)) as GoToDestination;
                defaultTurnSpeed = goToDestinationBehaviourComponent.turnSpeed;
                defaultGoalRadius = goToDestinationBehaviourComponent.goalRadius;
                goalActivated = true;
                goToDestinationBehaviourComponent.onDestinationReached += OnReachedDestination;
                SearchForPrey();
            }
        }

        protected virtual void SearchForPrey()
        {
            Vector3 searchPreyCenterPoint;
            if (setNewSearchingPreyCenterPoint)
            {
                searchPreyCenterPoint = new Vector3(newSearchingPreyCenterPointX, spawnPoint.y, newSearchingPreyCenterPointZ);
            }
            else
            {
                searchPreyCenterPoint = spawnPoint;
            }
            goToDestinationBehaviourComponent.turnSpeed = defaultTurnSpeed;
            goToDestinationBehaviourComponent.goalRadius = defaultGoalRadius;
            goToDestinationBehaviourComponent.speed = UnityEngine.Random.Range(goToDestinationBehaviourComponent.minSpeed, goToDestinationBehaviourComponent.maxSpeed);
            Vector3 randomDestination = goToDestinationBehaviourComponent.GetARandomDestinationInsideAPerimeter(searchPreyCenterPoint, searchingForPreyMovementRange);
            goToDestinationBehaviourComponent.SetMyDestination(randomDestination);
        }

        public void OnReachedDestination()
        {
            if(isChasingPrey)
            {
                if(chasedPrey)
                {
                    Destroy(chasedPrey);
                    chasedPrey = null;
                    hunger = 0f;
                }
                isChasingPrey = false;
                SearchForPrey();
            }
            else
            {
                SearchForPrey();
            }
        }

        public override float UpdatePriority(float priority)
        {
            hunger += (Time.deltaTime * hungerIncreaseFactor);

            if(hunger > 100f) { hunger = 100f;  }

            if (hunger <= 25f)
            {
                priority = 5f;
            }
            else if (hunger > 25 && hunger <= 50)
            {
                priority = 20f;
            }
            else if (hunger > 50 && hunger <= 75)
            {
                priority = 60f;
            }
            else
            {
                priority = 90f;
            }

            return priority;
        }

        protected void TryToDetectPrey()
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, preyDetectionRange);

            foreach (var hitCollider in hitColliders)
            {
                ModelDataInspector hitcolliderModelDataInspector = hitCollider.GetComponent<ModelDataInspector>();
                if(hitcolliderModelDataInspector)
                {
                    if(animalsToHuntList.Contains<string>(hitcolliderModelDataInspector.entity))
                    {
                        StartChasingPrey(hitCollider.gameObject);
                        return;
                    }
                }
            }
        }

        private void StartChasingPrey(GameObject prey)
        {
            chasedPrey = prey;
            isChasingPrey = true;
        }

        public override void TerminateGoalExecution()
        {
            if (goToDestinationBehaviourComponent)
            {
                goalActivated = false;
                goToDestinationBehaviourComponent.onDestinationReached -= OnReachedDestination;
                Destroy(goToDestinationBehaviourComponent);
            }
        }

        private void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR
            if (showSearchingForPreyRange)
            {
                Handles.color = Color.yellow;
                Vector3 searchPreyCenterPoint;
                if (setNewSearchingPreyCenterPoint)
                {
                    searchPreyCenterPoint = new Vector3(newSearchingPreyCenterPointX, spawnPoint.y, newSearchingPreyCenterPointZ);
                }
                else
                {
                        searchPreyCenterPoint = spawnPoint;
                    
                
                    
                }
                UnityEditor.Handles.DrawWireDisc(searchPreyCenterPoint, Vector3.up, searchingForPreyMovementRange);
                GUI.color = Color.white;
                UnityEditor.Handles.Label(searchPreyCenterPoint + (Vector3.left * searchingForPreyMovementRange), new GUIContent("Searching for Prey movement range"));
            }

            if (showDetectPreyRange)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(gameObject.transform.position, preyDetectionRange);
                GUI.color = Color.white;
                UnityEditor.Handles.Label(gameObject.transform.position + (Vector3.left * preyDetectionRange), new GUIContent("Prey Detection Range"));
            }
#endif
        }
    }
}
