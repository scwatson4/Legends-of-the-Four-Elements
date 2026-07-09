using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Multiplayer sync for a regular building (barracks, towers, refineries):
/// faction ownership plus server-authoritative health. Add to building
/// prefabs together with a NetworkObject, and register them with the
/// NetworkManager.
/// </summary>
[RequireComponent(typeof(Structure))]
public class NetworkStructure : NetworkFactionSync
{
    public NetworkVariable<float> Health = new NetworkVariable<float>(
        500f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Structure structure;

    private void Awake()
    {
        structure = GetComponent<Structure>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            Health.Value = structure.maxHealth;
            structure.HealthChanged += OnServerHealthChanged;
        }
        else
        {
            Health.OnValueChanged += OnClientHealthChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (structure != null) structure.HealthChanged -= OnServerHealthChanged;
        Health.OnValueChanged -= OnClientHealthChanged;
    }

    private void OnServerHealthChanged(float current, float max)
    {
        Health.Value = current;
    }

    private void OnClientHealthChanged(float oldValue, float newValue)
    {
        structure.SetHealthFromNetwork(newValue);
    }
}
