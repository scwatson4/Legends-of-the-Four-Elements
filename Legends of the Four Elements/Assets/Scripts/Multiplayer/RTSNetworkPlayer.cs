using Unity.Netcode;
using UnityEngine;

/// <summary>
/// One connected human in a multiplayer match. Lives on the player prefab
/// assigned in the NetworkManager. Holds the player's nation pick, faction id,
/// team, and server-authoritative credits, and registers the corresponding
/// faction on every machine.
/// </summary>
public class RTSNetworkPlayer : NetworkBehaviour
{
    public static RTSNetworkPlayer Local { get; private set; }

    public NetworkVariable<int> NationIndex = new NetworkVariable<int>(
        (int)Nation.Air, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<int> FactionId = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> TeamGroup = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> Credits = new NetworkVariable<int>(
        300, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Local = this;
            NationIndex.Value = (int)GameSetup.PlayerNation;
        }

        if (IsServer)
        {
            FactionId.Value = (int)OwnerClientId;
            TeamGroup.Value = GameSetup.MultiplayerCoop ? 0 : (int)OwnerClientId;
        }

        NationIndex.OnValueChanged += (_, __) => RegisterFaction();
        FactionId.OnValueChanged += (_, __) => RegisterFaction();
        TeamGroup.OnValueChanged += (_, __) => RegisterFaction();
        Credits.OnValueChanged += OnCreditsChanged;

        RegisterFaction();
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkDespawn()
    {
        if (Local == this) Local = null;
    }

    private void RegisterFaction()
    {
        if (FactionId.Value < 0) return;

        FactionManager.Register(new Faction
        {
            id = FactionId.Value,
            nation = (Nation)NationIndex.Value,
            displayName = NationInfo.DisplayName((Nation)NationIndex.Value),
            teamGroup = TeamGroup.Value,
            isAI = false,
            isLocalPlayer = IsOwner
        });
    }

    private void OnCreditsChanged(int oldValue, int newValue)
    {
        // Mirror server credits into the local HUD.
        if (IsOwner && PlayerResources.Instance != null)
        {
            PlayerResources.Instance.SetCredits(newValue);
        }
    }

    public void SetNation(Nation nation)
    {
        if (IsOwner) NationIndex.Value = (int)nation;
    }

    /// <summary>Server-side: lock in the player's team when the match starts.</summary>
    public void AssignTeam(int teamGroup)
    {
        if (IsServer) TeamGroup.Value = teamGroup;
    }

    // ------------------------------------------------------------------
    // Building units
    // ------------------------------------------------------------------

    /// <summary>
    /// Called by UnitSpawner build buttons. Returns true when the request was
    /// handled by the network layer (i.e. we are in a multiplayer session).
    /// </summary>
    public static bool TryRelayBuildRequest(int rosterIndex)
    {
        if (!NetworkGuard.IsNetworked || Local == null) return false;
        Local.RequestBuildUnitServerRpc(rosterIndex);
        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestBuildUnitServerRpc(int rosterIndex, ServerRpcParams rpcParams = default)
    {
        // Validate the sender is asking through their own player object.
        if (rpcParams.Receive.SenderClientId != OwnerClientId) return;

        NationDatabase db = NationDatabase.Load();
        NationData data = db != null ? db.Get((Nation)NationIndex.Value) : null;
        NationData.UnitEntry entry = data != null ? data.GetUnit(rosterIndex) : null;
        if (entry == null || entry.prefab == null)
        {
            Debug.LogWarning($"[Multiplayer] No roster unit {rosterIndex} for faction {FactionId.Value}.");
            return;
        }

        if (Credits.Value < entry.cost) return;

        Vector3 spawnPosition;
        if (NetworkMatchManager.Instance == null ||
            !NetworkMatchManager.Instance.TryGetSpawnPoint(FactionId.Value, out spawnPosition))
        {
            Debug.LogWarning($"[Multiplayer] No base registered for faction {FactionId.Value}.");
            return;
        }

        Credits.Value -= entry.cost;

        GameObject unitGo = Instantiate(entry.prefab, spawnPosition, Quaternion.identity);
        NetworkObject netObj = unitGo.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError($"[Multiplayer] Unit prefab {entry.prefab.name} has no NetworkObject. " +
                           "Add one and register the prefab with the NetworkManager.");
            Destroy(unitGo);
            Credits.Value += entry.cost;
            return;
        }

        netObj.Spawn();

        NetworkUnit networkUnit = unitGo.GetComponent<NetworkUnit>();
        if (networkUnit != null) networkUnit.FactionId.Value = FactionId.Value;
        else FactionUtility.SetFaction(unitGo, FactionId.Value);
    }
}
