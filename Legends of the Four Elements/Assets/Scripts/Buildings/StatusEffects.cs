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
/// Total immobilization: earthbenders bending the ground around a foe's
/// feet, or waterbenders flash-freezing them. The victim can still attack -
/// they just cannot move until it wears off.
/// </summary>
public class RootEffect : MonoBehaviour
{
    private NavMeshAgent agent;
    private float remaining;

    public static void Apply(Unit target, float duration)
    {
        if (target == null) return;
        NavMeshAgent agent = target.GetComponent<NavMeshAgent>();
        if (agent == null || !agent.enabled) return;

        RootEffect root = target.GetComponent<RootEffect>();
        if (root == null)
        {
            root = target.gameObject.AddComponent<RootEffect>();
            root.agent = agent;
            agent.isStopped = true;
        }
        root.remaining = Mathf.Max(root.remaining, duration);
    }

    private void Update()
    {
        remaining -= Time.deltaTime;
        if (remaining <= 0f)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = false;
            Destroy(this);
        }
    }

    private void OnDestroy()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = false;
    }
}

/// <summary>
/// True when a point is close to a large water source: River Lands or
/// Glacier biome, or a Fish Shoal node. Waterbenders can only flash-freeze
/// enemies where there is real water to bend. Node lookups are cached.
/// </summary>
public static class WaterProximity
{
    private static ResourceNode[] cachedNodes;
    private static float cacheTime = -99f;

    public static bool IsNearWater(Vector3 position, float radius = 18f)
    {
        // Watery climates count as standing water.
        if (BiomeZone.IsClimateNear(position, radius,
                BiomeZone.ClimateType.RiverLands, BiomeZone.ClimateType.Glacier))
        {
            return true;
        }

        // So do fishing waters.
        if (Time.time - cacheTime > 3f)
        {
            cachedNodes = Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None);
            cacheTime = Time.time;
        }
        foreach (ResourceNode node in cachedNodes)
        {
            if (node == null || node.nodeType != ResourceNode.NodeType.FishShoal) continue;
            if (Vector3.Distance(position, node.transform.position) <= radius) return true;
        }
        return false;
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
