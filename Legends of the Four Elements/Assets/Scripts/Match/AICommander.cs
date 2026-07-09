using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Skirmish brain for one AI faction, in the spirit of Army Men RTS / Halo
/// Wars skirmish opponents: saves up credits, trains units from its nation's
/// roster, keeps them home as defenders, then periodically launches the whole
/// group at the nearest enemy base. Attached automatically by MatchManager to
/// AI command centers, or add it by hand in the inspector.
/// </summary>
public class AICommander : MonoBehaviour
{
    [Header("Identity")]
    public int factionId = 1;
    public NationData nationData;

    [Header("Behaviour")]
    public float decisionInterval = 4f;
    public float attackWaveInterval = 60f;
    public int minWaveSize = 4;
    public float spawnRadius = 6f;

    private MatchManager matchManager;
    private readonly List<GameObject> army = new List<GameObject>();
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

            TryTrainUnit();

            waveTimer -= decisionInterval;
            if (waveTimer <= 0f && army.Count >= minWaveSize)
            {
                LaunchAttackWave();
                waveTimer = attackWaveInterval;
            }
        }
    }

    private void TryTrainUnit()
    {
        if (nationData == null || nationData.units == null || nationData.units.Length == 0)
        {
            return;
        }

        // Pick a random affordable roster entry.
        NationData.UnitEntry entry = nationData.units[Random.Range(0, nationData.units.Length)];
        if (entry == null || entry.prefab == null) return;

        if (matchManager == null || !matchManager.TrySpendCredits(factionId, entry.cost)) return;

        Vector2 circle = Random.insideUnitCircle.normalized * spawnRadius;
        Vector3 spawnPos = transform.position + new Vector3(circle.x, 0f, circle.y);

        GameObject unit = matchManager.SpawnUnitFor(factionId, entry.prefab, spawnPos);

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
}
