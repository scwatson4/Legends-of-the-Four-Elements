using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Server-side match setup for multiplayer: when the level loads, spawns a
/// command center (and starting units) for every connected player at the
/// scene's StartLocation markers. Add one to each multiplayer level scene,
/// on a GameObject with a NetworkObject component.
/// </summary>
public class NetworkMatchManager : NetworkBehaviour
{
    public static NetworkMatchManager Instance { get; private set; }

    public NationDatabase nationDatabase;
    public int startingUnitsPerPlayer = 3;

    private readonly Dictionary<int, Transform> basesByFaction = new Dictionary<int, Transform>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (nationDatabase == null) nationDatabase = NationDatabase.Load();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        SpawnAllBases();
    }

    private void SpawnAllBases()
    {
        List<StartLocation> locations = new List<StartLocation>(
            FindObjectsByType<StartLocation>(FindObjectsSortMode.None));
        locations.Sort((a, b) => a.index.CompareTo(b.index));

        if (locations.Count == 0)
        {
            Debug.LogError("[Multiplayer] No StartLocation markers in this scene.");
            return;
        }

        int slot = 0;
        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;
            RTSNetworkPlayer player = client.PlayerObject.GetComponent<RTSNetworkPlayer>();
            if (player == null) continue;

            if (slot >= locations.Count)
            {
                Debug.LogWarning("[Multiplayer] More players than StartLocations; skipping extras.");
                break;
            }

            SpawnBaseFor(player, locations[slot].transform);
            slot++;
        }
    }

    private void SpawnBaseFor(RTSNetworkPlayer player, Transform location)
    {
        Nation nation = (Nation)player.NationIndex.Value;
        NationData data = nationDatabase != null ? nationDatabase.Get(nation) : null;
        if (data == null || data.commandCenterPrefab == null)
        {
            Debug.LogError($"[Multiplayer] No command center prefab for {nation}. Fill in NationData.");
            return;
        }

        GameObject baseGo = Instantiate(data.commandCenterPrefab, location.position, location.rotation);
        NetworkObject baseNetObj = baseGo.GetComponent<NetworkObject>();
        if (baseNetObj == null)
        {
            Debug.LogError($"[Multiplayer] Command center prefab {data.commandCenterPrefab.name} " +
                           "needs a NetworkObject + NetworkFactionSync, and must be registered " +
                           "with the NetworkManager.");
            Destroy(baseGo);
            return;
        }
        baseNetObj.Spawn();

        int factionId = player.FactionId.Value;
        NetworkFactionSync factionSync = baseGo.GetComponent<NetworkFactionSync>();
        if (factionSync != null) factionSync.FactionId.Value = factionId;
        else FactionUtility.SetFaction(baseGo, factionId);

        basesByFaction[factionId] = baseGo.transform;

        // Starting squad.
        NationData.UnitEntry starter = data.GetUnit(0);
        if (starter != null && starter.prefab != null)
        {
            for (int i = 0; i < startingUnitsPerPlayer; i++)
            {
                float angle = (360f / Mathf.Max(1, startingUnitsPerPlayer)) * i * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * 6f;

                GameObject unitGo = Instantiate(starter.prefab, location.position + offset, Quaternion.identity);
                NetworkObject unitNetObj = unitGo.GetComponent<NetworkObject>();
                if (unitNetObj == null)
                {
                    Debug.LogError($"[Multiplayer] Unit prefab {starter.prefab.name} needs a NetworkObject.");
                    Destroy(unitGo);
                    continue;
                }
                unitNetObj.Spawn();

                NetworkUnit networkUnit = unitGo.GetComponent<NetworkUnit>();
                if (networkUnit != null) networkUnit.FactionId.Value = factionId;
                else FactionUtility.SetFaction(unitGo, factionId);
            }
        }
    }

    /// <summary>Where faction's new units should appear (near their base).</summary>
    public bool TryGetSpawnPoint(int factionId, out Vector3 position)
    {
        Transform baseTransform;
        if (basesByFaction.TryGetValue(factionId, out baseTransform) && baseTransform != null)
        {
            Vector2 circle = Random.insideUnitCircle.normalized * 6f;
            position = baseTransform.position + new Vector3(circle.x, 0f, circle.y);
            return true;
        }

        position = Vector3.zero;
        return false;
    }
}
