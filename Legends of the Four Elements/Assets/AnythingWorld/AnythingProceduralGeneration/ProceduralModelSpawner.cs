#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using AnythingWorld.Behaviour.Tree;
using AnythingWorld.PostProcessing;
using UnityEditor;
using Debug = UnityEngine.Debug;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace AnythingWorld.Behaviour
{
    /// <summary>
    /// Spawns random models within a specified area, supporting grid and random placement with overlap control.
    /// </summary>
    [ExecuteInEditMode]
    public class ProceduralModelSpawner : MonoBehaviour
    {
        public const float SpawnInterval = 0.2f;

        private const string ModelLayerName = "AWProcGenModel";
        private const int MaxAttempts = 500;

        public Transform spawnRoot;
        [Range(0, 1)] public float spawnDensity = 1;

        [HideInInspector] public bool useGridSpawn;
        [HideInInspector] public float spawnRadius = 1;
        [HideInInspector] public bool canModelsOverlap;
        [HideInInspector] public float spawnWidth = 1;
        [HideInInspector] public float spawnHeight = 1;
        [HideInInspector] public bool showGridSlots;
        [HideInInspector] public Vector3 spawnAreaCenter;
        [HideInInspector] public List<GameObject> models = new List<GameObject>();
        [HideInInspector] public List<float> modelWeights = new List<float>();
        [HideInInspector] public int spawnedModelsLayerMask;

        private readonly List<List<GameObject>> _spawnedModelsLists = new List<List<GameObject>>();
        private readonly List<Vector3> _createdGridPositions = new List<Vector3>();
        private readonly Collider[] _modelSpawnCollisionsBuffer = new Collider[1];
        private readonly List<float> _modelRadii = new List<float>();
        private readonly List<Vector3> _modelExtents = new List<Vector3>();
        private readonly List<Vector3> _gridPoints = new List<Vector3>();
        private readonly List<BehaviourTreeInstanceRunner> _behaviourTreeRunners = new List<BehaviourTreeInstanceRunner>();
        private Vector3 _gridSlotExtents;
        private readonly List<float> _weightsNormalized = new List<float>();
        private int _spawnedModelsLayer = -1;
        private int _layerMaskWithoutSpawnedModels;
        private int _totalModelsToSpawn;
        private bool _isSpawnParametersCalculated;
        private bool _drawGizmos;
        private float _maxModelHeight;

        /// <summary>
        /// Initializes the spawner by recalculating parameters and setting up layers.
        /// </summary>
        private void Start()
        {
            CreateModelLayer();
        }

        /// <summary>
        /// Recalculates parameters needed for model spawning, including weights and grid points.
        /// </summary>
        public void RecalculateSpawnParameters()
        {
            _isSpawnParametersCalculated = true;

            if (_spawnedModelsLayer == -1)
            {
                _spawnedModelsLayer = LayerMask.NameToLayer(ModelLayerName);
                if (_spawnedModelsLayer == -1)
                {
                    Debug.LogError("AWProcGenModel layer not found please create it " +
                                   "for random model generation to work.");
                    _isSpawnParametersCalculated = false;
                    return;
                }
                spawnedModelsLayerMask = LayerMask.GetMask(ModelLayerName);
            }

            if (!_isSpawnParametersCalculated || !TryAddModelsExtents() || !TryNormalizeWeights())
            {
                _isSpawnParametersCalculated = false;
                return;
            }

            if (_spawnedModelsLists.Count < models.Count)
            {
                var listsToAdd = models.Count - _spawnedModelsLists.Count;
                for (int i = 0; i < listsToAdd; i++)
                {
                    _spawnedModelsLists.Add(new List<GameObject>());
                }
            }

            GetBehaviourTreeRunners();
            CalculateModelsRadius();
            if (useGridSpawn)
            {
                GenerateGridPoints();
            }
        }

        private void GetBehaviourTreeRunners()
        {
            _behaviourTreeRunners.Clear();
            foreach (var model in models)
            {
                var runner = model.GetComponentInChildren<BehaviourTreeInstanceRunner>();
                _behaviourTreeRunners.Add(runner ? runner : null);
            }
        }

        /// <summary>
        /// Spawns models based on current settings; supports random and grid-based spawning.
        /// </summary>
        public void SpawnModels()
        {
            if (!_isSpawnParametersCalculated)
            {
                return;
            }

            if (!spawnRoot)
            {
                spawnRoot = new GameObject("SpawnRoot").transform;
            }

            if (!useGridSpawn)
            {
                SpawnModelsRandomly();
                return;
            }

            SpawnModelsOnGrid();
        }

        /// <summary>
        /// Deletes all models that have been spawned under the spawn root.
        /// </summary>
        public void DeleteSpawnedModels()
        {
            _drawGizmos = false;
            while (spawnRoot.childCount > 0)
            {
                Undo.DestroyObjectImmediate(spawnRoot.GetChild(0).gameObject);
            }
        }

        /// <summary>
        /// Deletes all models in spawn area.
        /// </summary>
        public void RemoveModelsInSpawnArea()
        {
            var modelsInArea = GetModelsInSpawnArea();
            foreach (var model in modelsInArea)
            {
                Undo.DestroyObjectImmediate(model.gameObject);
            }
        }

        /// <summary>
        /// Creates a dedicated layer for spawned models if it doesn't already exist.
        /// </summary>
        private void CreateModelLayer()
        {
            _spawnedModelsLayer = LayerMask.NameToLayer(ModelLayerName);
            if (_spawnedModelsLayer != -1)
            {
                spawnedModelsLayerMask = LayerMask.GetMask(ModelLayerName);
                return;
            }

            SerializedObject tagManager =
                new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layersProp = tagManager.FindProperty("layers");

            for (int i = 6; i < 31; i++)
            {
                SerializedProperty layerSp = layersProp.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layerSp.stringValue))
                {
                    layerSp.stringValue = ModelLayerName;
                    tagManager.ApplyModifiedProperties();
                    _spawnedModelsLayer = i;
                    spawnedModelsLayerMask = LayerMask.GetMask(ModelLayerName);
                    return;
                }
            }
            Debug.LogError("Could not find an empty layer slot, maximum layers reached. Please create AWProcGenModel layer " +
                           "manually for random model generation to work.");
        }

        /// <summary>
        /// Normalizes model weights to create a proper probability distribution.
        /// </summary>
        private bool TryNormalizeWeights()
        {
            if (models.Count != modelWeights.Count)
            {
                Debug.LogError("Please add weights for each model before using random model spawner.");
                return false;
            }

            var minWeight = modelWeights.Min();
            var totalWeight = modelWeights.Sum();

            var absMin = minWeight < 0 ? Mathf.Abs(minWeight) : 0;
            totalWeight += absMin * models.Count;

            _weightsNormalized.Clear();

            foreach (var w in modelWeights)
            {
                var weight = w + absMin;
                _weightsNormalized.Add(weight / totalWeight);
            }
            return true;
        }

        /// <summary>
        /// Adds and stores the extents of each model for collision calculations.
        /// </summary>
        private bool TryAddModelsExtents()
        {
            _modelExtents.Clear();

            foreach (var model in models)
            {
                if (!model)
                {
                    Debug.LogError("Please assign all models before spawning.");
                    return false;
                }

                var collider = model.GetComponentInChildren<Collider>();

                if (!collider)
                {
                    Debug.LogError($"Model {model.name} doesn't have a collider. Only models with colliders are currently" +
                                   "supported for random spawning. Remove this model from the list and try again.");
                    return false;
                }

                ModelDimensionsUtility.TryGetDimensions(model.transform, out var extents, out _);
                _modelExtents.Add(extents);
            }

            return true;
        }

        /// <summary>
        /// Calculates radius for each model based on their extents.
        /// </summary>
        private void CalculateModelsRadius()
        {
            _modelRadii.Clear();
            _gridSlotExtents = Vector3.zero;
            _maxModelHeight = 0;

            for (int i = 0; i < models.Count; i++)
            {
                var extents = _modelExtents[i];
                var radius = Mathf.Max(extents.x, extents.z);
                _maxModelHeight = Mathf.Max(_maxModelHeight, extents.y);

                _gridSlotExtents = Vector3.Max(_gridSlotExtents, extents);
                _modelRadii.Add(radius);
            }

            _maxModelHeight *= 2;
        }

        /// <summary>
        /// Spawns models at random positions, considering overlap settings and spawn density.
        /// </summary>
        private void SpawnModelsRandomly()
        {
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Spawn models by weights");

            if (IsModelInSpawnPosition(spawnAreaCenter))
            {
                return;
            }

            var attempts = 0;

            var modelIdx = SelectModelToSpawnByWeight();
            var modelToSpawn = models[modelIdx];

            var isAreaFullyFilled = spawnDensity >= 0.99f;

            if (!isAreaFullyFilled)
            {
                foreach (var modelsList in _spawnedModelsLists)
                {
                    modelsList.Clear();
                }
            }

            while (attempts < MaxAttempts)
            {
                Vector2 randomPoint2D = Random.insideUnitCircle * spawnRadius;
                Vector3 randomPosition = new Vector3(randomPoint2D.x, 0f, randomPoint2D.y);
                var spawnPosition = randomPosition + spawnAreaCenter;

                var isSpawned = false;

                if ((canModelsOverlap && !IsModelInSpawnPosition(spawnPosition)) || !IsOverlappingSphere(spawnPosition, _modelRadii[modelIdx]))
                {
                    isSpawned = true;
                    var spawnedModel = SpawnModelOnGround(modelToSpawn, spawnPosition, modelIdx);

                    if (!isAreaFullyFilled)
                    {
                        _spawnedModelsLists[modelIdx].Add(spawnedModel);
                    }
                }
                else
                {
                    attempts++;
                }

                if (!isSpawned)
                {
                    continue;
                }

                modelIdx = SelectModelToSpawnByWeight();
                modelToSpawn = models[modelIdx];
            }

            if (isAreaFullyFilled)
            {
                return;
            }

            var percentageToDelete = 1 - spawnDensity;
            foreach (var modelsList in _spawnedModelsLists)
            {
                if (modelsList.Count == 0)
                {
                    break;
                }

                var deleteCount = Mathf.CeilToInt(modelsList.Count * percentageToDelete);
                for (int i = 0; i < deleteCount; i++)
                {
                    DestroyImmediate(modelsList[i]);
                }
            }

            Undo.CollapseUndoOperations(group);
        }

        /// <summary>
        /// Spawns models on predefined grid points within the spawn radius.
        /// </summary>
        private void SpawnModelsOnGrid()
        {
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Spawn models in grid");

            if (_gridPoints.Count == 0)
            {
                Debug.LogError("No grid points available for spawning. Make sure that with and height are " +
                               "at least as large as your largest model size.");
                return;
            }

            var numberOfModelsToSpawn = Mathf.CeilToInt(_gridPoints.Count * spawnDensity);

            if (numberOfModelsToSpawn == 0)
            {
                Debug.LogError("No models to spawn. Spawn density is too low.");
                return;
            }

            _drawGizmos = false;
            ShuffleList(_gridPoints);

            var spawnAreaOrigin =
                new Vector3(spawnAreaCenter.x - spawnWidth * 0.5f, spawnAreaCenter.y, spawnAreaCenter.z - spawnHeight * 0.5f);

            int modelsSpawned = 0;

            _createdGridPositions.Clear();

            for (int i = 0; i < _gridPoints.Count && modelsSpawned < numberOfModelsToSpawn; i++)
            {
                Vector3 position = spawnAreaOrigin + _gridPoints[i];
                _createdGridPositions.Add(position);
                var modelIdx = SelectModelToSpawnByWeight();
                GameObject modelToSpawn = models[modelIdx];

                if (IsOverlappingBox(position))
                    break;

                SpawnModelOnGround(modelToSpawn, position, modelIdx);
                modelsSpawned++;
            }

            Undo.CollapseUndoOperations(group);

            _drawGizmos = true;
        }

        /// <summary>
        /// Selects a model index based on normalized weights for probability-based spawning.
        /// </summary>
        private int SelectModelToSpawnByWeight()
        {
            float currentWeight = 0;
            var randomValue = Random.value;
            for (var i = 0; i < _weightsNormalized.Count; i++)
            {
                currentWeight += _weightsNormalized[i];
                if (randomValue <= currentWeight)
                {
                    return i;
                }
            }

            return 0;
        }

        /// <summary>
        /// Generates grid slots based on grid width and height.
        /// </summary>
        private void GenerateGridPoints()
        {
            _gridPoints.Clear();

            var gridSlotWidth = _gridSlotExtents.x * 2;
            var gridSlotHeight = _gridSlotExtents.z * 2;

            var columnsCount = Mathf.CeilToInt(spawnWidth / gridSlotWidth);
            var rowsCount = Mathf.CeilToInt(spawnHeight / gridSlotHeight);

            for (int x = 0; x < columnsCount; x++)
            {
                for (int y = 0; y < rowsCount; y++)
                {
                    var position =
                        new Vector3(x * gridSlotWidth + _gridSlotExtents.x, 0f, y * gridSlotHeight + _gridSlotExtents.z);
                    _gridPoints.Add(position);
                }
            }
        }

        /// <summary>
        /// Instantiates a model at the given position and sets its layer and height.
        /// </summary>
        private GameObject SpawnModelOnGround(GameObject modelToSpawn, Vector3 position, int modelIdx)
        {
            GameObject newModel =
                Instantiate(modelToSpawn, position, Quaternion.identity, spawnRoot);

            newModel.GetComponentInChildren<Collider>().gameObject.layer = _spawnedModelsLayer;

            newModel.transform.position = position + Vector3.up * _modelExtents[modelIdx].y;

            Undo.RegisterCreatedObjectUndo(newModel, "Spawn model by weight");

            var behTreeRunner = _behaviourTreeRunners[modelIdx];
            if (behTreeRunner)
            {
                behTreeRunner.InitializeTree();
            }

            return newModel;
        }

        /// <summary>
        /// Checks if the specified position overlaps with any existing spawned models.
        /// </summary>
        private bool IsModelInSpawnPosition(Vector3 position)
        {
            var origin = position + Vector3.up * _maxModelHeight * 1.5f;
            return Physics.Raycast(origin, Vector3.down, out _, _maxModelHeight * 2, spawnedModelsLayerMask);
        }

        /// <summary>
        /// Checks if the specified position overlaps with any existing spawned models.
        /// </summary>
        private bool IsOverlappingSphere(Vector3 position, float radius)
        {
            var size = Physics.OverlapSphereNonAlloc(position, radius * 0.95f, _modelSpawnCollisionsBuffer,
                spawnedModelsLayerMask);
            return size > 0;
        }

        /// <summary>
        /// Get all the models that are in the current spawn area.
        /// </summary>
        private Collider[] GetModelsInSpawnArea()
        {
            if (!useGridSpawn)
            {
                return Physics.OverlapSphere(spawnAreaCenter, spawnRadius, spawnedModelsLayerMask);
            }

            return Physics.OverlapBox(spawnAreaCenter + Vector3.up * 1,
                new Vector3(spawnWidth * .5f, .5f, spawnHeight * .5f), Quaternion.identity, spawnedModelsLayerMask);
        }

        /// <summary>
        /// Checks if the specified position overlaps with any existing spawned models.
        /// </summary>
        private bool IsOverlappingBox(Vector3 position)
        {
            var size = Physics.OverlapBoxNonAlloc(position, _gridSlotExtents * 0.99f, _modelSpawnCollisionsBuffer,
                Quaternion.identity, spawnedModelsLayerMask);
            return size > 0;
        }

        /// <summary>
        /// Randomly shuffles the elements of a list in place.
        /// </summary>
        private void ShuffleList<T>(List<T> list)
        {
            int n = list.Count;
            System.Random rng = new System.Random();
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        /// <summary>
        /// Draws gizmos for grid slots if enabled.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!useGridSpawn || !showGridSlots || !_drawGizmos)
            {
                return;
            }

            Gizmos.color = Color.blue;
            foreach (var gridPoint in _createdGridPositions)
            {
                Gizmos.DrawWireCube(gridPoint + Vector3.up * _gridSlotExtents.y, _gridSlotExtents * 2);
            }
        }
    }
}
#endif
