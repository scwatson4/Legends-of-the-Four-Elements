using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Base class that replicates faction ownership for any networked object
/// (units and buildings). The server sets FactionId; every client stamps it
/// onto the local FactionMember so selection, hostility and UI all agree.
/// </summary>
public class NetworkFactionSync : NetworkBehaviour
{
    public NetworkVariable<int> FactionId = new NetworkVariable<int>(
        FactionManager.NoFaction,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        ApplyFaction(FactionId.Value);
        FactionId.OnValueChanged += OnFactionChanged;
    }

    public override void OnNetworkDespawn()
    {
        FactionId.OnValueChanged -= OnFactionChanged;
    }

    private void OnFactionChanged(int oldValue, int newValue)
    {
        ApplyFaction(newValue);
    }

    protected virtual void ApplyFaction(int factionId)
    {
        if (factionId == FactionManager.NoFaction) return;
        FactionUtility.SetFaction(gameObject, factionId);
    }
}
