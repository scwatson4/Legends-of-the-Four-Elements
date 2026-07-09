using UnityEngine;

/// <summary>
/// The single entry point for earning and spending silver (credits).
/// Routes to the right wallet no matter who is asking:
///  - local player, single-player  -> PlayerResources (drives the HUD)
///  - AI factions                  -> Faction.credits
///  - multiplayer (on the server)  -> that player's RTSNetworkPlayer.Credits
/// Always call Economy.TrySpend / Economy.Award instead of touching wallets
/// directly - this is what keeps the money system consistent everywhere.
/// </summary>
public static class Economy
{
    public static bool TrySpend(int factionId, int amount)
    {
        if (amount <= 0) return true;

        // Multiplayer: the server owns every human player's wallet.
        if (NetworkGuard.IsNetworked)
        {
            if (!NetworkGuard.IsServer) return false; // clients ask via RPCs

            RTSNetworkPlayer player = RTSNetworkPlayer.FindByFaction(factionId);
            if (player != null)
            {
                if (player.Credits.Value < amount) return false;
                player.Credits.Value -= amount;
                return true;
            }
            // fall through: AI faction in a network match
        }
        else if (factionId == FactionManager.LocalPlayerFactionId && PlayerResources.Instance != null)
        {
            return PlayerResources.Instance.SpendCredits(amount);
        }

        Faction faction = FactionManager.Get(factionId);
        if (faction == null || faction.credits < amount) return false;
        faction.credits -= amount;
        return true;
    }

    public static void Award(int factionId, int amount)
    {
        if (amount <= 0) return;
        if (factionId == FactionManager.NoFaction ||
            factionId == FactionManager.HostileSpiritsFaction) return;

        if (NetworkGuard.IsNetworked)
        {
            if (!NetworkGuard.IsServer) return; // replicated down via NetworkVariable

            RTSNetworkPlayer player = RTSNetworkPlayer.FindByFaction(factionId);
            if (player != null)
            {
                player.Credits.Value += amount;
                return;
            }
        }
        else if (factionId == FactionManager.LocalPlayerFactionId && PlayerResources.Instance != null)
        {
            PlayerResources.Instance.AddCredits(amount);
            return;
        }

        Faction faction = FactionManager.Get(factionId);
        if (faction != null) faction.credits += amount;
    }

    public static int GetBalance(int factionId)
    {
        if (NetworkGuard.IsNetworked)
        {
            RTSNetworkPlayer player = RTSNetworkPlayer.FindByFaction(factionId);
            if (player != null) return player.Credits.Value;
        }
        else if (factionId == FactionManager.LocalPlayerFactionId && PlayerResources.Instance != null)
        {
            return PlayerResources.Instance.Credits;
        }

        Faction faction = FactionManager.Get(factionId);
        return faction != null ? faction.credits : 0;
    }
}
