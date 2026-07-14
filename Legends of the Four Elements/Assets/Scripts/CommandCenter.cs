using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommandCenter : MonoBehaviour
{
    [Tooltip("Legacy two-team field. Add a FactionMember component for four-nation/multiplayer ownership.")]
    public Team team;
    private float structureHealth;
    public float maxStructureHealth = 1000f;
    public GameObject CommandCenterModel;
    public HealthTracker healthTracker;

    public int FactionId => FactionUtility.GetFactionId(gameObject);

    /// <summary>Fired with (current, max) whenever structure health changes. Used by the network sync layer.</summary>
    public event System.Action<float, float> HealthChanged;

    private bool isDestroyed;

    void Start()
    {
        structureHealth = maxStructureHealth;
        UpdateHealthUI();

        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.RegisterCommandCenter(this);
        }

        MinimapPOI.Ensure(gameObject, MinimapPOI.POIType.CommandCenter);

        // Command centers block pathing too - units walk around, not through.
        NavObstacleUtility.Ensure(gameObject);
    }

    private void UpdateHealthUI()
    {
        if (healthTracker != null)
        {
            healthTracker.UpdateSliderValue(structureHealth, maxStructureHealth);
        }

        if (structureHealth <= 0 && !isDestroyed)
        {
            isDestroyed = true;

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayStructureDestructionSound();
            }

            if (MatchManager.Instance != null)
            {
                // Multi-faction flow: MatchManager decides victory/defeat.
                MatchManager.Instance.OnCommandCenterDestroyed(this);
            }
            else
            {
                // Legacy two-team flow (original Level1 without a MatchManager).
                if (team == Team.Player)
                {
                    if (GameManager.Instance != null)
                        StartCoroutine(TriggerGameOver());
                    else
                        Debug.LogError("GameManager.Instance is null! Cannot trigger Game Over.");
                }
                else if (team == Team.Enemy)
                {
                    if (GameManager.Instance != null)
                        StartCoroutine(TriggerWin());
                    else
                        Debug.LogError("GameManager.Instance is null! Cannot trigger Win.");
                }
            }

            Destroy(CommandCenterModel);
            Destroy(gameObject, 1f);
        }
    }

    public void TakeDamage(int damageToInflict)
    {
        if (NetworkGuard.BlockLocalSimulation) return;

        structureHealth -= damageToInflict;
        UpdateHealthUI();
        HealthChanged?.Invoke(structureHealth, maxStructureHealth);
    }

    /// <summary>Used by the network layer to mirror the server's health on clients.</summary>
    internal void SetHealthFromNetwork(float value)
    {
        structureHealth = value;
        UpdateHealthUI();
    }

    private IEnumerator TriggerGameOver()
    {
        yield return new WaitForEndOfFrame();
        if (GameManager.Instance != null)
            GameManager.Instance.OnPlayerCommandCenterDestroyed();
        else
            Debug.LogError("GameManager.Instance is null during TriggerGameOver!");
    }

    private IEnumerator TriggerWin()
    {
        yield return new WaitForEndOfFrame();
        if (GameManager.Instance != null)
            GameManager.Instance.OnEnemyCommandCenterDestroyed();
        else
            Debug.LogError("GameManager.Instance is null during TriggerWin!");
    }
}
