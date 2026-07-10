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

    private static readonly System.Collections.Generic.List<RTSNetworkPlayer> allPlayers =
        new System.Collections.Generic.List<RTSNetworkPlayer>();

    /// <summary>Every connected player's wallet lives here; Economy uses this.</summary>
    public static RTSNetworkPlayer FindByFaction(int factionId)
    {
        foreach (RTSNetworkPlayer player in allPlayers)
        {
            if (player != null && player.FactionId.Value == factionId) return player;
        }
        return null;
    }

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
        if (!allPlayers.Contains(this)) allPlayers.Add(this);

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
        allPlayers.Remove(this);
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

        // One Avatar per player, in multiplayer too.
        if (entry.category == UnitCategory.Avatar && AvatarUnit.FactionHasAvatar(FactionId.Value)) return;

        // Population cap applies on the server as well.
        if (!PopulationManager.HasRoomFor(FactionId.Value, entry.prefab)) return;

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

    // ------------------------------------------------------------------
    // Placing buildings
    // ------------------------------------------------------------------

    /// <summary>Called by BuildingPlacer. True = handled by the network layer.</summary>
    public static bool TryRelayPlaceBuilding(int buildingIndex, Vector3 position, Quaternion rotation)
    {
        if (!NetworkGuard.IsNetworked || Local == null) return false;
        Local.RequestPlaceBuildingServerRpc(buildingIndex, position, rotation);
        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestPlaceBuildingServerRpc(int buildingIndex, Vector3 position, Quaternion rotation,
        ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId) return;

        NationDatabase db = NationDatabase.Load();
        NationData data = db != null ? db.Get((Nation)NationIndex.Value) : null;
        NationData.BuildingEntry entry = null;
        if (data != null)
        {
            entry = buildingIndex == BuildingPlacer.CommandCenterIndex
                ? BuildingPlacer.MakeCommandCenterEntry(data)
                : data.GetBuilding(buildingIndex);
        }
        if (entry == null || entry.prefab == null) return;

        if (Credits.Value < entry.cost) return;
        Credits.Value -= entry.cost;

        GameObject building = Instantiate(entry.prefab, position, rotation);
        NetworkObject netObj = building.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
            NetworkFactionSync sync = building.GetComponent<NetworkFactionSync>();
            if (sync != null) { sync.FactionId.Value = FactionId.Value; return; }
        }
        FactionUtility.SetFaction(building, FactionId.Value);
    }

    // ------------------------------------------------------------------
    // Buying upgrades
    // ------------------------------------------------------------------

    /// <summary>Called by UpgradePurchaser. True = handled by the network layer.</summary>
    public static bool TryRelayPurchaseUpgrade(int upgradeIndex)
    {
        if (!NetworkGuard.IsNetworked || Local == null) return false;
        Local.RequestPurchaseUpgradeServerRpc(upgradeIndex);
        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestPurchaseUpgradeServerRpc(int upgradeIndex, ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId) return;

        NationDatabase db = NationDatabase.Load();
        NationData data = db != null ? db.Get((Nation)NationIndex.Value) : null;
        if (data == null || data.upgrades == null) return;
        if (upgradeIndex < 0 || upgradeIndex >= data.upgrades.Length) return;

        // Server applies the purchase, then tells everyone so stat bonuses
        // stay identical on every client.
        if (UpgradeManager.TryPurchase(FactionId.Value, data.upgrades[upgradeIndex]))
        {
            ApplyUpgradeClientRpc(upgradeIndex);
        }
    }

    [ClientRpc]
    private void ApplyUpgradeClientRpc(int upgradeIndex)
    {
        if (IsServer) return; // the server already applied it

        NationDatabase db = NationDatabase.Load();
        NationData data = db != null ? db.Get((Nation)NationIndex.Value) : null;
        if (data == null || data.upgrades == null) return;
        if (upgradeIndex < 0 || upgradeIndex >= data.upgrades.Length) return;

        UpgradeManager.ApplyPurchasedLevel(FactionId.Value, data.upgrades[upgradeIndex]);
    }
}
