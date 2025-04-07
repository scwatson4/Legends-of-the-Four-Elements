using AnythingWorld.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AnythingWorld.Behaviour
{
    public class FleeFromPredatorsGoal : AnythingCharacterGoal
    {
        [Header("Gizmos Controller")]
        [Tooltip("Show the perimeter in where a animal can go when moving randomly")]
        public bool showMovingRandomlyPerimeter = false;
        [Tooltip("Show the range in where an animal can detect the presence of its predators.")]
        public bool showDetectPredatorsRange = false;

        [Header("Flee From Predators Settings")]
        [Tooltip("List of animals which this animal will flee (based on the predator Entity).")]
        public string[] predatorsList;
        [Tooltip("Set the perimeter size in where a animal can go when moving randomly")]
        public float movingRandomlyMovementRange = 15f;
        [Tooltip("Set the range in where an animal can detect the presence of its predators.")]
        public float predatorDetectionRange = 5f;
        [Tooltip("Select to choose a new center point of the perimeter in where a animal can go when moving randomly")]
        public bool setNewWalkingRandomlyCenterPoint = false;
        [Tooltip("Set the new center point Axis X of the perimeter in where a animal can go when moving randomly.")]
        public float newWalkingRandomlyCenterPointX;
        [Tooltip("Set the new center point Axis Z of the perimeter in where a animal can go when moving randomly.")]
        public float newWalkingRandomlyCenterPointZ;

        protected bool goalActivated = false;
        protected GoToDestination goToDestinationBehaviourComponent;
        protected Vector3 spawnPoint;
        protected List<GameObject> detectedPredators = new List<GameObject>();
        protected float detectPredatorTimer = 0f;
        protected float defaultTurnSpeed;
        protected float defaultGoalRange;
        protected bool predatorDetected = false;
        protected bool spawnPointDefined = false;

        protected const float searchForPredatorFrequencyInSeconds = 0.5f; //@Hardcoded
        protected const float fleeingFromPredatorGoalRadius = 0.5f; //@Hardcoded
        protected const float fleeingFromPredatorTurnSpeed = 5f; //@Hardcoded

        private void Awake()
        {
            spawnPoint = gameObject.transform.position;
        }
        protected override void Start()
        {
            base.Start();
            CreateTriggerOnlyBoxCollider();
            detectPredatorTimer = searchForPredatorFrequencyInSeconds;
            spawnPointDefined = true;
        }

        protected void CreateTriggerOnlyBoxCollider()
        {
            //This box collider is created in order that this prey can be detected by its predators
            //so its predators can trigger hunt behaviour
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
        }

        protected virtual void Update()
        {
            detectPredatorTimer += Time.deltaTime;
            if (detectPredatorTimer >= searchForPredatorFrequencyInSeconds)
            {
                TryToDetectPredator();
                detectPredatorTimer -= searchForPredatorFrequencyInSeconds;
            }

            if (!goalActivated) return;

            if (goToDestinationBehaviourComponent && predatorDetected)
            {
                GameObject closestPredator = null;
                float closestDistanceFromPredator = 0f;

                for (int i = 0; i < detectedPredators.Count; i++)
                {
                    if(i <= 0)
                    {
                        closestPredator = detectedPredators[i];
                        closestDistanceFromPredator = Vector3.Distance(transform.position, closestPredator.transform.position);
                    }
                    else
                    {
                        if (Vector3.Distance(transform.position, detectedPredators[i].transform.position) < closestDistanceFromPredator)
                        {
                            closestPredator = detectedPredators[i];
                            closestDistanceFromPredator = Vector3.Distance(transform.position, closestPredator.transform.position);
                        }
                    }
                }

                if (closestPredator)
                {
                    Vector3 fleeLocation = transform.position + (transform.position - closestPredator.transform.position).normalized;
                    goToDestinationBehaviourComponent.speed = goToDestinationBehaviourComponent.maxSpeed;
                    goToDestinationBehaviourComponent.goalRadius = fleeingFromPredatorGoalRadius;
                    goToDestinationBehaviourComponent.turnSpeed = fleeingFromPredatorTurnSpeed;
                    goToDestinationBehaviourComponent.SetMyDestination(new Vector3(fleeLocation.x, spawnPoint.y, fleeLocation.z));
                }
            }
        }

        public override void ExecuteGoal()
        {
            if (!goToDestinationBehaviourComponent)
            {
                goToDestinationBehaviourComponent = gameObject.AddComponent(typeof(GoToDestination)) as GoToDestination;
                defaultTurnSpeed = goToDestinationBehaviourComponent.turnSpeed;
                defaultGoalRange = goToDestinationBehaviourComponent.goalRadius;
                goToDestinationBehaviourComponent.onDestinationReached += OnReachedDestination;

                if (!predatorDetected)
                {
                    WalkRandomly();
                }

                goalActivated = true;
            }
        }

        protected void WalkRandomly()
        {
            Vector3 walkingRandomlyCenterPoint;
            if (setNewWalkingRandomlyCenterPoint)
            {
                walkingRandomlyCenterPoint = new Vector3(newWalkingRandomlyCenterPointX, spawnPoint.y, newWalkingRandomlyCenterPointZ);
            }
            else
            {
                walkingRandomlyCenterPoint = spawnPoint;
                
            }
            Vector3 randomDestination = goToDestinationBehaviourComponent.GetARandomDestinationInsideAPerimeter(walkingRandomlyCenterPoint, movingRandomlyMovementRange);
            goToDestinationBehaviourComponent.SetMyDestination(randomDestination);
        }
        
        public void OnReachedDestination()
        {
            if (!predatorDetected)
            {
                WalkRandomly();
            }
        }

        public override float UpdatePriority(float priority)
        {
            if(predatorDetected)
            {
                priority = 95f;
            }
            else
            {
                priority = 0f;
            }

            return priority;
        }

        protected void TryToDetectPredator()
        {
            detectedPredators.Clear();
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, predatorDetectionRange);

            foreach (var hitCollider in hitColliders)
            {
                ModelDataInspector hitcolliderModelDataInspector = hitCollider.GetComponent<ModelDataInspector>();
                if (hitcolliderModelDataInspector)
                {
                    if (predatorsList.Contains<string>(hitcolliderModelDataInspector.entity))
                    {
                        detectedPredators.Add(hitCollider.gameObject);
                        
                    }
                }
            }

            if(detectedPredators.Count > 0)
            {
                predatorDetected = true;
            }
            else
            {
                if (predatorDetected)
                {
                    goToDestinationBehaviourComponent.speed = UnityEngine.Random.Range(goToDestinationBehaviourComponent.minSpeed, goToDestinationBehaviourComponent.maxSpeed);
                    goToDestinationBehaviourComponent.goalRadius = defaultGoalRange;
                    goToDestinationBehaviourComponent.turnSpeed = defaultTurnSpeed;
                    predatorDetected = false;
                    detectedPredators.Clear();
                }
            }
        }

        public override void TerminateGoalExecution()
        {
            if (goToDestinationBehaviourComponent)
            {
                goToDestinationBehaviourComponent.onDestinationReached -= OnReachedDestination;
                goalActivated = false;
                Destroy(goToDestinationBehaviourComponent);
            }
        }

        protected virtual void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR
            if (showDetectPredatorsRange)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(gameObject.transform.position, predatorDetectionRange);
                GUI.color = Color.white;
                UnityEditor.Handles.Label(gameObject.transform.position + (Vector3.left * predatorDetectionRange), new GUIContent("Predator Detection Range"));
            }

            if (showMovingRandomlyPerimeter)
            {
                Handles.color = Color.blue;
                Vector3 walkingRandomlyCenterPoint;
                if (setNewWalkingRandomlyCenterPoint)
                {
                    walkingRandomlyCenterPoint = new Vector3(newWalkingRandomlyCenterPointX, spawnPoint.y, newWalkingRandomlyCenterPointZ);
                }
                else
                {
                    if (spawnPointDefined)
                    {
                        walkingRandomlyCenterPoint = spawnPoint;
                    }
                    else
                    {
                        walkingRandomlyCenterPoint = gameObject.transform.position;
                    }

                }
                UnityEditor.Handles.DrawWireDisc(walkingRandomlyCenterPoint, Vector3.up, movingRandomlyMovementRange);
                GUI.color = Color.white;
                UnityEditor.Handles.Label(walkingRandomlyCenterPoint + (Vector3.left * movingRandomlyMovementRange), new GUIContent("Walking Randomly movement range"));
            }
#endif
        }
    }
}
