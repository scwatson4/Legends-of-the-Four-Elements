using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Multiplayer sync for a command center: faction ownership plus
/// server-authoritative structure health. Add to command center prefabs
/// together with a NetworkObject, and register them with the NetworkManager.
/// </summary>
[RequireComponent(typeof(CommandCenter))]
public class NetworkCommandCenter : NetworkFactionSync
{
    public NetworkVariable<float> Health = new NetworkVariable<float>(
        1000f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private CommandCenter commandCenter;

    private void Awake()
    {
        commandCenter = GetComponent<CommandCenter>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            Health.Value = commandCenter.maxStructureHealth;
            commandCenter.HealthChanged += OnServerHealthChanged;
        }
        else
        {
            Health.OnValueChanged += OnClientHealthChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (commandCenter != null) commandCenter.HealthChanged -= OnServerHealthChanged;
        Health.OnValueChanged -= OnClientHealthChanged;
    }

    private void OnServerHealthChanged(float current, float max)
    {
        Health.Value = current;
    }

    private void OnClientHealthChanged(float oldValue, float newValue)
    {
        commandCenter.SetHealthFromNetwork(newValue);
    }
}
