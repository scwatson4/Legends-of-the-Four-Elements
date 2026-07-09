using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Damage-over-time from fire towers / firebending. Reapplying refreshes the
/// duration and keeps the strongest tick.
/// </summary>
public class BurnEffect : MonoBehaviour
{
    private Unit unit;
    private float damagePerSecond;
    private float remaining;
    private float tickTimer;

    public static void Apply(Unit target, float damagePerSecond, float duration)
    {
        if (target == null) return;
        BurnEffect burn = target.GetComponent<BurnEffect>();
        if (burn == null) burn = target.gameObject.AddComponent<BurnEffect>();

        burn.unit = target;
        burn.damagePerSecond = Mathf.Max(burn.damagePerSecond, damagePerSecond);
        burn.remaining = Mathf.Max(burn.remaining, duration);
    }

    private void Update()
    {
        remaining -= Time.deltaTime;
        tickTimer -= Time.deltaTime;

        if (tickTimer <= 0f)
        {
            tickTimer = 0.5f;
            if (unit != null) unit.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(damagePerSecond * 0.5f)));
        }

        if (remaining <= 0f) Destroy(this);
    }
}

/// <summary>
/// Movement slow from water towers (ice crystals). Restores the original
/// speed when it expires; reapplying refreshes the duration.
/// </summary>
public class SlowEffect : MonoBehaviour
{
    private NavMeshAgent agent;
    private float originalSpeed;
    private float remaining;

    public static void Apply(Unit target, float speedMultiplier, float duration)
    {
        if (target == null) return;
        NavMeshAgent agent = target.GetComponent<NavMeshAgent>();
        if (agent == null) return;

        SlowEffect slow = target.GetComponent<SlowEffect>();
        if (slow == null)
        {
            slow = target.gameObject.AddComponent<SlowEffect>();
            slow.agent = agent;
            slow.originalSpeed = agent.speed;
            agent.speed = agent.speed * Mathf.Clamp01(speedMultiplier);
        }
        slow.remaining = Mathf.Max(slow.remaining, duration);
    }

    private void Update()
    {
        remaining -= Time.deltaTime;
        if (remaining <= 0f)
        {
            if (agent != null) agent.speed = originalSpeed;
            Destroy(this);
        }
    }
}
