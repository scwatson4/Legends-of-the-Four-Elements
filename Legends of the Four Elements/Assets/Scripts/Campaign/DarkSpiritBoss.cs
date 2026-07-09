using UnityEngine;

/// <summary>
/// Umbriss, the First Shadow - the final boss and the true cause of the
/// rupture. Immensely powerful, and INVULNERABLE unless enough Avatars
/// (your own plus the redeemed ones) stand together near it: their combined
/// presence breaks its shroud. It periodically calls dark spirits through.
/// Added at runtime by CampaignManager, or place it on a boss prefab.
/// </summary>
[RequireComponent(typeof(Unit))]
public class DarkSpiritBoss : MonoBehaviour, IDamageInterceptor
{
    public string bossName = "Umbriss, the First Shadow";

    [Header("Boss Scaling (applied once at spawn)")]
    public float healthMultiplier = 20f;
    public float damageMultiplier = 3f;
    public float sizeMultiplier = 2.5f;

    [Header("Harmonic Shroud")]
    [Tooltip("Avatars that must stand within shroudRadius for the boss to take damage.")]
    public int requiredAvatarsNearby = 4;
    public float shroudRadius = 40f;

    [Header("Summoning")]
    [Tooltip("Optional dark spirit prefab (auto-loads Resources/Campaign/DarkSpirit if empty).")]
    public GameObject minionPrefab;
    public float summonInterval = 30f;
    public int minionsPerSummon = 2;

    private Unit unit;
    private float summonTimer;
    private float shroudMessageCooldown;
    private bool reported;

    private void Start()
    {
        unit = GetComponent<Unit>();

        FactionUtility.SetFaction(gameObject, FactionManager.HostileSpiritsFaction);
        unit.unitType = Unit.UnitType.Spirit;
        unit.killBounty = 500;

        unit.SetMaxHealth(unit.maxUnitHealth * healthMultiplier);
        transform.localScale *= sizeMultiplier;

        AttackController attack = GetComponent<AttackController>();
        if (attack != null)
        {
            attack.unitDamage = Mathf.RoundToInt(attack.unitDamage * damageMultiplier);
        }

        EnemyAI brain = GetComponent<EnemyAI>();
        if (brain == null) brain = gameObject.AddComponent<EnemyAI>();
        brain.chaseCommandCenters = true;

        if (minionPrefab == null)
        {
            minionPrefab = Resources.Load<GameObject>("Campaign/DarkSpirit");
        }
        summonTimer = summonInterval;

        unit.HealthChanged += OnHealthChanged;
        MinimapPOI poi = MinimapPOI.Ensure(gameObject, MinimapPOI.POIType.Custom);
        poi.colorOverride = new Color(0.6f, 0f, 1f);
    }

    private void Update()
    {
        if (NetworkGuard.BlockLocalSimulation) return;
        shroudMessageCooldown -= Time.deltaTime;

        if (minionPrefab == null) return;
        summonTimer -= Time.deltaTime;
        if (summonTimer > 0f) return;
        summonTimer = summonInterval;

        for (int i = 0; i < minionsPerSummon; i++)
        {
            Vector2 circle = Random.insideUnitCircle.normalized * 6f;
            Instantiate(minionPrefab, transform.position + new Vector3(circle.x, 0f, circle.y),
                Quaternion.identity);
        }
        Debug.Log($"{bossName} tears more spirits through the rupture!");
    }

    /// <summary>The Harmonic Shroud: no Avatars together, no damage.</summary>
    public int ModifyIncomingDamage(int damage)
    {
        if (CountAvatarsNearby() >= requiredAvatarsNearby) return damage;

        if (shroudMessageCooldown <= 0f)
        {
            shroudMessageCooldown = 5f;
            Debug.Log($"{bossName} is beyond mortal harm - bring the Avatars together " +
                      $"({CountAvatarsNearby()}/{requiredAvatarsNearby} nearby)!");
        }
        return 0;
    }

    private int CountAvatarsNearby()
    {
        int count = 0;
        foreach (AvatarUnit avatar in FindObjectsByType<AvatarUnit>(FindObjectsSortMode.None))
        {
            if (avatar.gameObject == gameObject) continue;
            if (FactionUtility.GetFactionId(avatar.gameObject) == FactionManager.HostileSpiritsFaction) continue;
            if (Vector3.Distance(avatar.transform.position, transform.position) <= shroudRadius) count++;
        }
        return count;
    }

    private void OnHealthChanged(float current, float max)
    {
        if (current <= 0f) ReportDefeat();
    }

    private void OnDestroy()
    {
        if (unit != null) unit.HealthChanged -= OnHealthChanged;
        if (unit != null && unit.CurrentHealth <= 0f) ReportDefeat();
    }

    private void ReportDefeat()
    {
        if (reported) return;
        reported = true;

        if (CampaignManager.Instance != null)
        {
            CampaignManager.Instance.OnDarkSpiritDefeated();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0f, 1f);
        Gizmos.DrawWireSphere(transform.position, shroudRadius);
    }
}
