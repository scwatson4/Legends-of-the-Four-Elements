using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Branch-end abilities granted by tech-tree upgrades (see UpgradeManager).
/// None of these are added by hand - buy the upgrade and every matching
/// unit, current and future, learns the art.
/// </summary>

/// <summary>
/// Tornado Summoning - the Air storm branch's ultimate. In combat the
/// airbender periodically conjures a tornado on their target: everything
/// hostile around it takes damage and is hurled apart (great for breaking
/// packed formations). A spinning greybox funnel marks it until you swap in
/// real VFX (assign tornadoEffectPrefab).
/// </summary>
public class TornadoSummon : MonoBehaviour
{
    public float cooldownSeconds = 12f;
    public float tornadoRadius = 6f;
    public int damage = 15;
    public float scatterDistance = 6f;
    [Tooltip("Optional real VFX; empty = a greybox funnel is spun up.")]
    public GameObject tornadoEffectPrefab;

    private AttackController attackController;
    private float cooldownRemaining;

    private void Start()
    {
        attackController = GetComponent<AttackController>();
        cooldownRemaining = cooldownSeconds * Random.value; // desync squads
    }

    private void Update()
    {
        if (NetworkGuard.BlockLocalSimulation) return;

        cooldownRemaining -= Time.deltaTime;
        if (cooldownRemaining > 0f) return;
        if (attackController == null || attackController.targetToAttack == null) return;

        cooldownRemaining = cooldownSeconds;
        Vector3 center = attackController.targetToAttack.position;

        int myFaction = FactionUtility.GetFactionId(gameObject);
        foreach (Collider hit in Physics.OverlapSphere(center, tornadoRadius))
        {
            Unit victim = hit.GetComponentInParent<Unit>();
            if (victim == null) continue;
            if (!FactionManager.AreHostile(myFaction, FactionUtility.GetFactionId(victim.gameObject))) continue;

            victim.TakeDamage(damage);
            KnockbackUtility.Push(victim, center, scatterDistance);
        }

        SpawnVisual(center);
        Debug.Log($"{gameObject.name} summons a TORNADO!");
    }

    private void SpawnVisual(Vector3 center)
    {
        if (tornadoEffectPrefab != null)
        {
            Destroy(Instantiate(tornadoEffectPrefab, center, Quaternion.identity), 2.5f);
            return;
        }

        // Greybox funnel: a pale spinning cone that fades out on its own.
        GameObject funnel = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        GreyboxMaterial.Harmonize(funnel); // URP-safe material for runtime primitives
        funnel.name = "Tornado";
        Destroy(funnel.GetComponent<Collider>());
        funnel.transform.position = center + Vector3.up * 2f;
        funnel.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);

        Renderer renderer = funnel.GetComponent<Renderer>();
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        Color pale = new Color(0.9f, 0.92f, 1f, 0.5f);
        block.SetColor("_BaseColor", pale);
        block.SetColor("_Color", pale);
        renderer.SetPropertyBlock(block);

        funnel.AddComponent<TornadoSpin>();
        Destroy(funnel, 2.5f);
    }

    /// <summary>Spins the greybox funnel so it reads as a tornado.</summary>
    private class TornadoSpin : MonoBehaviour
    {
        private void Update() => transform.Rotate(0f, 720f * Time.deltaTime, 0f);
    }
}

/// <summary>
/// Flame Dive - the Fire inferno branch's ultimate. In combat the firebender
/// periodically LEAPS onto their target and slams into the ground, igniting
/// a ring of fire around the impact: every nearby enemy takes damage and
/// starts burning. The bender relocates to the impact point (fire jumps in).
/// </summary>
public class FlameDive : MonoBehaviour
{
    public float cooldownSeconds = 14f;
    [Tooltip("Max leap distance to the target.")]
    public float diveRange = 14f;
    public float ringRadius = 5f;
    public int impactDamage = 20;
    public float burnDamagePerSecond = 4f;
    public float burnDuration = 3f;
    [Tooltip("Optional real VFX; empty = a greybox fire ring flashes.")]
    public GameObject ringEffectPrefab;

    private AttackController attackController;
    private NavMeshAgent agent;
    private float cooldownRemaining;

    private void Start()
    {
        attackController = GetComponent<AttackController>();
        agent = GetComponent<NavMeshAgent>();
        cooldownRemaining = cooldownSeconds * Random.value;
    }

    private void Update()
    {
        if (NetworkGuard.BlockLocalSimulation) return;

        cooldownRemaining -= Time.deltaTime;
        if (cooldownRemaining > 0f) return;
        if (attackController == null || attackController.targetToAttack == null) return;

        Vector3 targetPos = attackController.targetToAttack.position;
        float distance = Vector3.Distance(transform.position, targetPos);
        if (distance < 3f || distance > diveRange) return; // worth a leap?

        cooldownRemaining = cooldownSeconds;

        // The leap: land beside the target (fire rockets them across the gap).
        Vector3 landing = targetPos - (targetPos - transform.position).normalized * 1.5f;
        if (agent != null && agent.enabled && NavMesh.SamplePosition(landing, out NavMeshHit hit, 4f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }

        // The slam: a ring of fire erupts around the impact.
        int myFaction = FactionUtility.GetFactionId(gameObject);
        foreach (Collider nearby in Physics.OverlapSphere(transform.position, ringRadius))
        {
            Unit victim = nearby.GetComponentInParent<Unit>();
            if (victim == null) continue;
            if (!FactionManager.AreHostile(myFaction, FactionUtility.GetFactionId(victim.gameObject))) continue;

            victim.TakeDamage(impactDamage);
            BurnEffect.Apply(victim, burnDamagePerSecond, burnDuration);
        }

        SpawnRing();
        Debug.Log($"{gameObject.name} FLAME DIVES into the fray!");
    }

    private void SpawnRing()
    {
        if (ringEffectPrefab != null)
        {
            Destroy(Instantiate(ringEffectPrefab, transform.position, Quaternion.identity), 2f);
            return;
        }

        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        GreyboxMaterial.Harmonize(ring); // URP-safe material for runtime primitives
        ring.name = "FireRing";
        Destroy(ring.GetComponent<Collider>());
        ring.transform.position = transform.position + Vector3.up * 0.1f;
        ring.transform.localScale = new Vector3(ringRadius * 2f, 0.08f, ringRadius * 2f);

        Renderer renderer = ring.GetComponent<Renderer>();
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        Color flame = new Color(1f, 0.45f, 0.1f, 0.6f);
        block.SetColor("_BaseColor", flame);
        block.SetColor("_Color", flame);
        renderer.SetPropertyBlock(block);

        Destroy(ring, 1.5f);
    }
}

/// <summary>
/// Fire Spray (Dragon's Breath Nozzles) - upgraded Fire Nation war machines
/// vent burning fuel: any enemy that presses close to the hull is scorched
/// continuously. Turns balloons and tanks into zone-control pieces.
/// </summary>
public class FireSpray : MonoBehaviour
{
    public float sprayRadius = 6f;
    public float tickInterval = 1f;
    public int tickDamage = 2;
    public float burnDamagePerSecond = 4f;
    public float burnDuration = 2f;

    private float timer;

    private void Update()
    {
        if (NetworkGuard.BlockLocalSimulation) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = tickInterval;

        int myFaction = FactionUtility.GetFactionId(gameObject);
        foreach (Collider hit in Physics.OverlapSphere(transform.position, sprayRadius))
        {
            Unit victim = hit.GetComponentInParent<Unit>();
            if (victim == null) continue;
            if (!FactionManager.AreHostile(myFaction, FactionUtility.GetFactionId(victim.gameObject))) continue;

            victim.TakeDamage(tickDamage);
            BurnEffect.Apply(victim, burnDamagePerSecond, burnDuration);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, sprayRadius);
    }
}

/// <summary>
/// Everfrost - the Water ice branch's ultimate. Marker component: this
/// waterbender's Ice Prison freezes enemies solid ANYWHERE - no river,
/// glacier or shoal required. They carry their own winter.
/// AttackStyleSet checks for it when resolving freezes.
/// </summary>
public class Everfrost : MonoBehaviour
{
}
