using UnityEngine;

/// <summary>
/// Lightning redirection - the Iroh technique. Granted to firebenders by an
/// upgrade (UpgradeData.grantsLightningRedirect): when a lightning attack
/// (a firebender's Lightning Jolt or a Flame Turret's lightning special)
/// strikes them, they have a chance to catch it and hurl it BACK at the
/// source. Applied by UpgradeManager; don't add by hand.
/// </summary>
public class LightningRedirect : MonoBehaviour
{
    [Range(0f, 0.9f)] public float chance = 0.25f;

    /// <summary>Rolls redirection. True = the damage went back to the source
    /// instead; the caller should NOT damage the target.</summary>
    public static bool TryRedirect(Unit target, int damage, GameObject source)
    {
        if (target == null || source == null) return false;

        LightningRedirect redirect = target.GetComponent<LightningRedirect>();
        if (redirect == null || Random.value > redirect.chance) return false;

        // The lightning flows through them and back out.
        Unit sourceUnit = source.GetComponentInParent<Unit>();
        if (sourceUnit != null)
        {
            sourceUnit.TakeDamage(damage);
        }
        else
        {
            Structure sourceStructure = source.GetComponentInParent<Structure>();
            if (sourceStructure != null) sourceStructure.TakeDamage(damage);
            else
            {
                CommandCenter sourceCc = source.GetComponentInParent<CommandCenter>();
                if (sourceCc != null) sourceCc.TakeDamage(damage);
            }
        }

        Debug.DrawLine(target.transform.position, source.transform.position, Color.cyan, 0.5f);
        Debug.Log($"{target.name} REDIRECTS the lightning back at {source.name}!");
        return true;
    }
}

/// <summary>
/// Metalbending - the absolute top-tier earthbender technique (Toph's gift).
/// Granted by an upgrade (UpgradeData.grantsMetalBending): earthbenders tear
/// into machines and fortifications - bonus damage against Vehicles and
/// buildings. Applied by UpgradeManager; don't add by hand.
/// </summary>
public class MetalBending : MonoBehaviour
{
    public float vehicleDamageMultiplier = 1.6f;
    public float structureDamageMultiplier = 1.3f;
}

/// <summary>
/// Mountain Breaker - the counter to Air Nomad mountain perches. Granted by
/// an upgrade (UpgradeData.grantsTremorAssault): earthbenders standing at a
/// mountain's foot bend TREMORS up through the rock itself, steadily shaking
/// apart any hostile building perched on the slopes above - the one way a
/// ground army can touch a perch. Damage stacks per earthbender, so a full
/// squad brings a temple down fast. Applied by UpgradeManager; don't add
/// by hand.
/// </summary>
public class TremorAssault : MonoBehaviour
{
    [Tooltip("How far up/around the tremors reach - perched hostile buildings " +
             "inside this radius take damage.")]
    public float tremorRadius = 25f;
    public int damagePerTick = 6;
    public float tickInterval = 1f;

    private float timer;

    // Perches are rare; share one cached lookup between all tremor-benders.
    private static MountainPerch[] cachedPerches;
    private static float cacheTime = -99f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        cachedPerches = null;
        cacheTime = -99f;
    }

    private void Update()
    {
        if (NetworkGuard.BlockLocalSimulation) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = tickInterval;

        if (Time.time - cacheTime > 3f)
        {
            cachedPerches = FindObjectsByType<MountainPerch>(FindObjectsSortMode.None);
            cacheTime = Time.time;
        }
        if (cachedPerches == null || cachedPerches.Length == 0) return;

        int myFaction = FactionUtility.GetFactionId(gameObject);
        foreach (MountainPerch perch in cachedPerches)
        {
            if (perch == null) continue;
            if (Vector3.Distance(transform.position, perch.transform.position) > tremorRadius) continue;
            if (!FactionManager.AreHostile(myFaction, FactionUtility.GetFactionId(perch.gameObject))) continue;

            Structure structure = perch.GetComponentInParent<Structure>();
            if (structure != null)
            {
                structure.TakeDamage(damagePerTick);
                Debug.DrawLine(transform.position, perch.transform.position, new Color(0.5f, 0.35f, 0.1f), tickInterval);
                continue;
            }

            CommandCenter commandCenter = perch.GetComponentInParent<CommandCenter>();
            if (commandCenter != null)
            {
                commandCenter.TakeDamage(damagePerTick);
                Debug.DrawLine(transform.position, perch.transform.position, new Color(0.5f, 0.35f, 0.1f), tickInterval);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.4f, 0.15f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, tremorRadius);
    }
}

/// <summary>
/// Lavabending - the other ultimate earthbender art (Ghazan/Bolin's gift),
/// the Earth Kingdom's answer to lightning. Granted by an upgrade
/// (UpgradeData.grantsLavaBending): every strike superheats the stone -
/// the victim IGNITES (lava burn over time) and molten rock splashes onto
/// enemies packed around them; fortifications melt for bonus damage.
/// Stacks with Metalbending (metal vs machines, lava vs flesh and walls).
/// Applied by UpgradeManager; don't add by hand.
/// </summary>
public class LavaBending : MonoBehaviour
{
    [Tooltip("The lava burn each strike leaves on the victim.")]
    public float burnDamagePerSecond = 6f;
    public float burnDuration = 4f;

    [Tooltip("Molten splash around the victim: nearby enemies take this " +
             "fraction of the strike's damage and a short scorch.")]
    public float splashRadius = 4f;
    [Range(0f, 1f)] public float splashFraction = 0.4f;

    [Tooltip("Molten rock eats fortifications: bonus damage vs buildings.")]
    public float structureDamageMultiplier = 1.4f;

    /// <summary>Ignites the struck unit and splashes molten rock onto every
    /// hostile unit packed around it. Call after the main hit lands.</summary>
    public void Erupt(Unit victim, int strikeDamage)
    {
        if (victim == null) return;

        BurnEffect.Apply(victim, burnDamagePerSecond, burnDuration);

        int splashDamage = Mathf.Max(1, Mathf.RoundToInt(strikeDamage * splashFraction));
        int myFaction = FactionUtility.GetFactionId(gameObject);
        foreach (Collider hit in Physics.OverlapSphere(victim.transform.position, splashRadius))
        {
            Unit other = hit.GetComponentInParent<Unit>();
            if (other == null || other == victim) continue;
            if (!FactionManager.AreHostile(myFaction, FactionUtility.GetFactionId(other.gameObject))) continue;

            other.TakeDamage(splashDamage);
            BurnEffect.Apply(other, burnDamagePerSecond * 0.5f, burnDuration * 0.5f);
        }
    }
}
