using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Skirmish brain for one AI faction, in the spirit of Army Men RTS / Halo
/// Wars skirmish opponents. It earns silver the same ways players do - base
/// income, workers harvesting resource nodes, holding villages - and spends
/// it like a player would: training a mixed army, keeping it home as
/// defenders, launching waves at the nearest enemy base, buying upgrade
/// levels, and eventually summoning its own Avatar.
/// Attached automatically by MatchManager to AI command centers, or add it by
/// hand in the inspector.
/// </summary>
public class AICommander : MonoBehaviour
{
    [Header("Identity")]
    public int factionId = 1;
    public NationData nationData;

    [Header("Army")]
    public float decisionInterval = 4f;
    public float attackWaveInterval = 60f;
    public int minWaveSize = 4;
    public float spawnRadius = 6f;

    [Header("Economy")]
    [Tooltip("How many harvesters to keep alive working nearby nodes.")]
    public int targetWorkerCount = 2;
    [Tooltip("Chance per decision tick to buy an upgrade level when affordable.")]
    [Range(0f, 1f)] public float upgradeChance = 0.15f;

    [Header("Avatar")]
    [Tooltip("Save up for its Avatar once its treasury exceeds cost * this factor.")]
    public float avatarSavingsFactor = 1.5f;

    private MatchManager matchManager;
    private readonly List<GameObject> army = new List<GameObject>();
    private readonly List<GameObject> workers = new List<GameObject>();
    private float waveTimer;

    public void Configure(int factionId, NationData nationData, MatchManager matchManager)
    {
        this.factionId = factionId;
        this.nationData = nationData;
        this.matchManager = matchManager;
    }

    private void Start()
    {
        if (matchManager == null) matchManager = MatchManager.Instance;
        if (nationData == null && matchManager != null && matchManager.nationDatabase != null)
        {
            Faction faction = FactionManager.Get(factionId);
            if (faction != null) nationData = matchManager.nationDatabase.Get(faction.nation);
        }

        waveTimer = attackWaveInterval;
        StartCoroutine(ThinkLoop());
    }

    private IEnumerator ThinkLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(decisionInterval);
        while (true)
        {
            yield return wait;

            if (GameManager.Instance != null && GameManager.Instance.GameIsOver) yield break;

            Faction faction = FactionManager.Get(factionId);
            if (faction == null || faction.isDefeated) yield break;

            workers.RemoveAll(w => w == null);
            army.RemoveAll(u => u == null);

            // Economy first, then military, mirroring a human's priorities.
            TryTrainWorker();
            TryBuyUpgrade();
            TryTrainAvatar();
            TryTrainSoldier();

            waveTimer -= decisionInterval;
            if (waveTimer <= 0f && army.Count >= minWaveSize)
            {
                LaunchAttackWave();
                waveTimer = attackWaveInterval;
            }
        }
    }

    // ------------------------------------------------------------------
    // Economy decisions
    // ------------------------------------------------------------------

    private void TryTrainWorker()
    {
        if (workers.Count >= targetWorkerCount) return;

        NationData.UnitEntry entry = FindRosterEntry(UnitCategory.Worker);
        if (entry == null) return;
        if (!HasPopulationRoom(entry.prefab)) return;
        if (!Economy.TrySpend(factionId, entry.cost)) return;

        GameObject worker = SpawnAround(entry.prefab);
        workers.Add(worker);
        // ResourceCollector auto-finds the nearest node; nothing else to do.
    }

    private void TryBuyUpgrade()
    {
        if (nationData == null || nationData.upgrades == null || nationData.upgrades.Length == 0) return;
        if (Random.value > upgradeChance) return;

        UpgradeData upgrade = nationData.upgrades[Random.Range(0, nationData.upgrades.Length)];
        UpgradeManager.TryPurchase(factionId, upgrade); // pays via Economy internally
    }

    // ------------------------------------------------------------------
    // Military decisions
    // ------------------------------------------------------------------

    private void TryTrainAvatar()
    {
        if (AvatarUnit.FactionHasAvatar(factionId)) return;

        NationData.UnitEntry entry = FindRosterEntry(UnitCategory.Avatar);
        if (entry == null) return;

        // Don't bankrupt the army fund - wait until comfortably affordable.
        if (!HasPopulationRoom(entry.prefab)) return;
        if (Economy.GetBalance(factionId) < entry.cost * avatarSavingsFactor) return;
        if (!Economy.TrySpend(factionId, entry.cost)) return;

        GameObject avatar = SpawnAround(entry.prefab);
        army.Add(avatar);
        Debug.Log($"AICommander ({nationData?.displayName}): their Avatar has awakened!");
    }

    private void TryTrainSoldier()
    {
        if (nationData == null || nationData.units == null || nationData.units.Length == 0) return;

        // Random pick among combat entries (infantry, animals, vehicles).
        NationData.UnitEntry entry = null;
        for (int attempts = 0; attempts < 5 && entry == null; attempts++)
        {
            NationData.UnitEntry candidate = nationData.units[Random.Range(0, nationData.units.Length)];
            if (candidate != null && candidate.prefab != null &&
                candidate.category != UnitCategory.Worker &&
                candidate.category != UnitCategory.Avatar)
            {
                entry = candidate;
            }
        }
        if (entry == null) return;

        if (!HasPopulationRoom(entry.prefab)) return;
        if (!Economy.TrySpend(factionId, entry.cost)) return;

        GameObject unit = SpawnAround(entry.prefab);

        // Hold position as a defender until the next wave launches.
        EnemyAI brain = unit.GetComponent<EnemyAI>();
        if (brain != null) brain.chaseCommandCenters = false;

        army.Add(unit);
    }

    private void LaunchAttackWave()
    {
        army.RemoveAll(u => u == null);
        Debug.Log($"AICommander ({nationData?.displayName}): launching wave of {army.Count} units.");

        foreach (GameObject unit in army)
        {
            EnemyAI brain = unit.GetComponent<EnemyAI>();
            if (brain != null) brain.chaseCommandCenters = true;
        }
        army.Clear();
    }

    // ------------------------------------------------------------------

    /// <summary>AI obeys the same population rules the player does.</summary>
    private bool HasPopulationRoom(GameObject prefab)
    {
        return PopulationManager.HasRoomFor(factionId, prefab);
    }

    private NationData.UnitEntry FindRosterEntry(UnitCategory category)
    {
        if (nationData == null || nationData.units == null) return null;
        foreach (NationData.UnitEntry entry in nationData.units)
        {
            if (entry != null && entry.prefab != null && entry.category == category) return entry;
        }
        return null;
    }

    private GameObject SpawnAround(GameObject prefab)
    {
        Vector2 circle = Random.insideUnitCircle.normalized * spawnRadius;
        Vector3 spawnPos = transform.position + new Vector3(circle.x, 0f, circle.y);

        if (matchManager != null)
        {
            return matchManager.SpawnUnitFor(factionId, prefab, spawnPos);
        }

        GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);
        FactionUtility.SetFaction(go, factionId);
        return go;
    }
}
