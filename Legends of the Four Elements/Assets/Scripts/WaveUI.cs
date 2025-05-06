using UnityEngine;
using TMPro;

public class WaveUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI timerText;

    private void Start()
    {
        if (waveText == null || timerText == null)
        {
            Debug.LogError("WaveText or TimerText not assigned in WaveUI Inspector!");
            return;
        }

        WaveManager waveManager = WaveManager.Instance;
        if (waveManager == null)
        {
            Debug.LogError("WaveManager not found! Ensure WaveManager is in the scene.");
            return;
        }

        waveManager.OnWaveStarted += UpdateWaveText;
        waveManager.OnTimerUpdated += UpdateTimerText;

        UpdateWaveText(0);
        UpdateTimerText(waveManager.TimeBetweenWaves);
    }

    private void UpdateWaveText(int waveNumber)
    {
        waveText.text = $"Wave: {waveNumber}";
        Debug.Log($"WaveUI: Updated to Wave {waveNumber}");
    }

    private void UpdateTimerText(float timeRemaining)
    {
        timerText.text = $"Next Wave: {Mathf.CeilToInt(timeRemaining)}";
    }

    private void OnDestroy()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStarted -= UpdateWaveText;
            WaveManager.Instance.OnTimerUpdated -= UpdateTimerText;
        }
    }
}