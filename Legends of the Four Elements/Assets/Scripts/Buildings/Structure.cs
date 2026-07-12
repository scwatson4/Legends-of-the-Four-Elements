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

    [Tooltip("What this cost to build (set automatically by BuildingPlacer). Selling refunds half.")]
    public int buildCost = 150;

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

        // Apply any Building-category upgrade levels the owner has bought.
        UpgradeManager.ApplyToBuilding(this);
    }

    /// <summary>Upgrade system: rescale max health keeping the same fraction.</summary>
    internal void SetMaxHealth(float newMax)
    {
        if (newMax <= 0f || isDestroyed) return;
        float fraction = maxHealth > 0f ? Mathf.Clamp01(health / maxHealth) : 1f;
        maxHealth = newMax;
        health = newMax * fraction;
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

    /// <summary>Workers repair damaged buildings (costs silver at the call site).</summary>
    public void Repair(float amount)
    {
        if (isDestroyed || health >= maxHealth) return;
        health = Mathf.Min(maxHealth, health + amount);
        UpdateHealthUI();
        HealthChanged?.Invoke(health, maxHealth);
    }

    public bool IsDamaged => !isDestroyed && health < maxHealth;

    /// <summary>C&amp;C-style sell: refund half the build cost and demolish.</summary>
    public void Sell()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        int refund = Mathf.Max(0, buildCost / 2);
        Economy.Award(FactionId, refund);
        Debug.Log($"{gameObject.name} sold for {refund} silver.");

        if (SoundManager.Instance != null) SoundManager.Instance.PlayStructureDestructionSound();
        Destroy(gameObject, 0.2f);
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
