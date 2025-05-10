using UnityEngine;
using System;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [SerializeField] private float timeBetweenWaves = 30f;
    [SerializeField] private float initialWaveDelay = 10f;
    [SerializeField] private int baseEnemyCount = 5;
    [SerializeField] private float enemyCountMultiplier = 1.2f;

    private int currentWave = 0;
    private float countdownTimer;
    private EnemyCommandCenter enemyCommandCenter;

    public float TimeBetweenWaves => timeBetweenWaves; // Getter for WaveUI
    public event Action<int> OnWaveStarted;
    public event Action<float> OnTimerUpdated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"Another WaveManager exists. Destroying this duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log("WaveManager initialized.");
    }

    private void Start()
    {
        enemyCommandCenter = FindFirstObjectByType<EnemyCommandCenter>();
        if (enemyCommandCenter == null)
        {
            Debug.LogError("EnemyCommandCenter not found! Ensure enemy CommandCenter has EnemyCommandCenter.cs.");
            return;
        }

        countdownTimer = initialWaveDelay;
        OnTimerUpdated?.Invoke(countdownTimer);
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.GameIsOver || enemyCommandCenter == null)
            return;

        countdownTimer -= Time.deltaTime;
        OnTimerUpdated?.Invoke(countdownTimer);

        if (countdownTimer <= 0)
        {
            StartWave();
            countdownTimer = timeBetweenWaves;
            OnTimerUpdated?.Invoke(countdownTimer);
        }
    }

    private void StartWave()
    {
        currentWave++;
        int enemyCount = Mathf.RoundToInt(baseEnemyCount * Mathf.Pow(enemyCountMultiplier, currentWave - 1));
        Debug.Log($"Starting Wave {currentWave} with {enemyCount} enemies.");

        enemyCommandCenter.SpawnWave(enemyCount);
        OnWaveStarted?.Invoke(currentWave);

        int creditsToAdd = 50 + (currentWave * 20);
        PlayerResources.Instance.AddCredits(creditsToAdd);
        Debug.Log($"Gained {creditsToAdd} credits for wave {currentWave}.");

    }
}