using UnityEngine;

/// <summary>
/// Health + destruction for any non-command-center building (barracks,
/// docks, refineries, towers, pens...). Command centers keep their own
/// CommandCenter component because destroying one affects victory; losing a
/// Structure just hurts.
/// </summary>
public class Structure : MonoBehaviour
{
    public float maxHealth = 500f;
    public HealthTracker healthTracker;

    [Tooltip("Silver awarded to whoever destroys this building.")]
    public int killBounty = 0;

    /// <summary>Fired with (current, max) whenever health changes. Used by the network sync layer.</summary>
    public event System.Action<float, float> HealthChanged;

    public int FactionId => FactionUtility.GetFactionId(gameObject);
    public float CurrentHealth => health;

    private float health;
    private bool isDestroyed;

    private void Start()
    {
        health = maxHealth;
        UpdateHealthUI();
    }

    public void TakeDamage(int damage)
    {
        if (NetworkGuard.BlockLocalSimulation) return;

        health -= damage;
        UpdateHealthUI();
        HealthChanged?.Invoke(health, maxHealth);
    }

    internal void SetHealthFromNetwork(float value)
    {
        health = value;
        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        if (healthTracker != null)
        {
            healthTracker.UpdateSliderValue(health, maxHealth);
        }

        if (health <= 0 && !isDestroyed)
        {
            isDestroyed = true;
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayStructureDestructionSound();
            }
            Destroy(gameObject, 0.5f);
        }
    }
}
