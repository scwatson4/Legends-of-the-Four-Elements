using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    [System.Serializable]
    public class UnitToBuild
    {
        public GameObject prefab;
        public int cost;
        public float buildTime;
        [HideInInspector] public bool isAvatar;
    }

    [Header("Spawn Settings")]
    public Vector3 spawnOffset = new Vector3(2f, 0f, 0f);
    public float arcRadius = 4f;
    public float arcAngle = 90f; // Total arc spread in degrees
    public float unitSpacingDegrees = 15f;

    [Header("Nation Roster")]
    [Tooltip("When on, QueueRosterUnit(index) builds from the selected nation's " +
             "roster in the NationDatabase, so the same build buttons work for " +
             "Air, Water, Earth and Fire alike.")]
    public bool useSelectedNationRoster = true;

    private Queue<UnitToBuild> buildQueue = new Queue<UnitToBuild>();
    private bool isBuilding = false;
    private int builtUnitsThisSession = 0;
    private int queuedAvatars = 0;

    /// <summary>The faction that owns units built here (from the FactionMember
    /// on this building, falling back to the local player).</summary>
    private int OwnerFactionId
    {
        get
        {
            int id = FactionUtility.GetFactionId(gameObject);
            return id == FactionManager.NoFaction ? FactionManager.LocalPlayerFactionId : id;
        }
    }

    /// <summary>
    /// Wire UI build buttons to this (0 = first roster slot, etc.). Uses the
    /// player's chosen nation, and relays to the server in multiplayer.
    /// </summary>
    public void QueueRosterUnit(int rosterIndex)
    {
        // Multiplayer client: the server builds and spawns for us.
        if (RTSNetworkPlayer.TryRelayBuildRequest(rosterIndex)) return;

        NationData data = ResolveNationData();
        NationData.UnitEntry entry = data != null ? data.GetUnit(rosterIndex) : null;
        if (entry == null || entry.prefab == null)
        {
            Debug.LogWarning($"UnitSpawner: no roster unit at index {rosterIndex} for the selected nation. " +
                             "Fill in the NationData assets.");
            return;
        }

        // There can only be one Avatar per player, alive or in the queue.
        if (entry.category == UnitCategory.Avatar &&
            (AvatarUnit.FactionHasAvatar(OwnerFactionId) || queuedAvatars > 0))
        {
            Debug.Log("The Avatar already walks among your forces.");
            return;
        }

        QueueUnit(new UnitToBuild
        {
            prefab = entry.prefab,
            cost = entry.cost,
            buildTime = entry.buildTime,
            isAvatar = entry.category == UnitCategory.Avatar
        });
    }

    private NationData ResolveNationData()
    {
        NationDatabase db = NationDatabase.Load();
        if (db == null) return null;

        Faction owner = FactionManager.Get(OwnerFactionId);
        Nation nation = owner != null ? owner.nation : GameSetup.PlayerNation;
        return db.Get(nation);
    }

    public void QueueUnit(UnitToBuild unit)
    {
        // Spend from the OWNER's wallet - the local player's HUD credits, an
        // AI faction's treasury, or (in multiplayer) the server-side wallet.
        if (Economy.TrySpend(OwnerFactionId, unit.cost))
        {
            if (unit.isAvatar) queuedAvatars++;
            buildQueue.Enqueue(unit);
            if (!isBuilding)
                StartCoroutine(ProcessQueue());
        }
        else
        {
            Debug.Log("Not enough silver to queue unit.");
        }
    }

    private IEnumerator ProcessQueue()
    {
        isBuilding = true;
        builtUnitsThisSession = 0;

        while (buildQueue.Count > 0)
        {
            UnitToBuild next = buildQueue.Dequeue();
            Debug.Log($"Building {next.prefab.name}...");

            yield return new WaitForSeconds(next.buildTime);

            Vector3 spawnPos = GetArcSpawnPosition(builtUnitsThisSession);
            GameObject spawned = Instantiate(next.prefab, spawnPos, Quaternion.identity);
            FactionUtility.SetFaction(spawned, OwnerFactionId);
            if (next.isAvatar) queuedAvatars = Mathf.Max(0, queuedAvatars - 1);

            builtUnitsThisSession++;
        }

        isBuilding = false;
    }

    private Vector3 GetArcSpawnPosition(int index)
    {
        float halfArc = arcAngle / 2f;
        float angleStep = unitSpacingDegrees;

        float angleDeg = -halfArc + (index * angleStep);
        float angleRad = angleDeg * Mathf.Deg2Rad;

        Vector3 arcDirection = new Vector3(Mathf.Sin(angleRad), 0f, Mathf.Cos(angleRad));
        Vector3 arcOffset = arcDirection * arcRadius;

        return transform.position + spawnOffset + arcOffset;
    }
}
