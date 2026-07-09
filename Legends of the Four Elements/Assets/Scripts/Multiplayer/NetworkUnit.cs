using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Multiplayer sync for a unit: faction ownership, server-authoritative
/// health, and command relaying (clients ask the server to move/attack).
/// Add to every unit prefab together with NetworkObject + NetworkTransform,
/// and register the prefab with the NetworkManager.
/// </summary>
[RequireComponent(typeof(Unit))]
public class NetworkUnit : NetworkFactionSync
{
    public NetworkVariable<float> Health = new NetworkVariable<float>(
        100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Unit unit;

    private void Awake()
    {
        unit = GetComponent<Unit>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            // Publish every health change the local simulation produces.
            Health.Value = unit.maxUnitHealth;
            unit.HealthChanged += OnServerHealthChanged;
        }
        else
        {
            // Clients render the server's simulation: position comes from
            // NetworkTransform, health from the variable, brains stay off.
            Health.OnValueChanged += OnClientHealthChanged;

            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            EnemyAI brain = GetComponent<EnemyAI>();
            if (brain != null) brain.enabled = false;

            AttackController attack = GetComponent<AttackController>();
            if (attack != null) attack.enabled = false;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (unit != null) unit.HealthChanged -= OnServerHealthChanged;
        Health.OnValueChanged -= OnClientHealthChanged;
    }

    private void OnServerHealthChanged(float current, float max)
    {
        Health.Value = current;
    }

    private void OnClientHealthChanged(float oldValue, float newValue)
    {
        unit.SetHealthFromNetwork(newValue);
    }

    // ------------------------------------------------------------------
    // Command relay (client -> server)
    // ------------------------------------------------------------------

    /// <summary>Returns true if the move order was relayed to the server
    /// (multiplayer client). False = caller should move the unit locally.</summary>
    public static bool TryRelayMove(GameObject unitGo, Vector3 destination)
    {
        if (!NetworkGuard.IsNetworked || NetworkGuard.IsServer) return false;

        NetworkUnit networkUnit = unitGo != null ? unitGo.GetComponent<NetworkUnit>() : null;
        if (networkUnit == null) return false;

        networkUnit.RequestMoveServerRpc(destination);
        return true;
    }

    /// <summary>Returns true if the attack order was relayed to the server.</summary>
    public static bool TryRelayAttack(GameObject unitGo, GameObject targetGo)
    {
        if (!NetworkGuard.IsNetworked || NetworkGuard.IsServer) return false;

        NetworkUnit networkUnit = unitGo != null ? unitGo.GetComponent<NetworkUnit>() : null;
        NetworkObject targetObj = targetGo != null ? targetGo.GetComponentInParent<NetworkObject>() : null;
        if (networkUnit == null || targetObj == null) return false;

        networkUnit.RequestAttackServerRpc(targetObj);
        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestMoveServerRpc(Vector3 destination, ServerRpcParams rpcParams = default)
    {
        if (!SenderOwnsThisUnit(rpcParams)) return;

        AttackController attack = GetComponent<AttackController>();
        if (attack != null) attack.targetToAttack = null;

        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.SetDestination(destination);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestAttackServerRpc(NetworkObjectReference targetRef, ServerRpcParams rpcParams = default)
    {
        if (!SenderOwnsThisUnit(rpcParams)) return;

        NetworkObject targetObj;
        if (!targetRef.TryGet(out targetObj)) return;

        // Only allow attacks on actual hostiles.
        if (!FactionUtility.AreHostile(gameObject, targetObj.gameObject)) return;

        AttackController attack = GetComponent<AttackController>();
        if (attack != null) attack.targetToAttack = targetObj.transform;
    }

    /// <summary>Anti-cheat: a client may only command units of its own faction.</summary>
    private bool SenderOwnsThisUnit(ServerRpcParams rpcParams)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (!NetworkManager.ConnectedClients.TryGetValue(senderId, out NetworkClient client) ||
            client.PlayerObject == null)
        {
            return false;
        }

        RTSNetworkPlayer player = client.PlayerObject.GetComponent<RTSNetworkPlayer>();
        return player != null && player.FactionId.Value == FactionId.Value;
    }
}
