using UnityEngine;

/// <summary>
/// Helpers to resolve which faction a GameObject belongs to, with a fallback
/// to the legacy Team enum so existing prefabs and scenes keep working
/// without any editor changes.
/// </summary>
public static class FactionUtility
{
    public static int GetFactionId(GameObject go)
    {
        if (go == null) return FactionManager.NoFaction;

        FactionMember member = go.GetComponentInParent<FactionMember>();
        if (member != null) return member.factionId;

        Unit unit = go.GetComponentInParent<Unit>();
        if (unit != null) return unit.team == Team.Player ? 0 : 1;

        CommandCenter commandCenter = go.GetComponentInParent<CommandCenter>();
        if (commandCenter != null) return commandCenter.team == Team.Player ? 0 : 1;

        return FactionManager.NoFaction;
    }

    public static bool AreHostile(GameObject a, GameObject b)
    {
        return FactionManager.AreHostile(GetFactionId(a), GetFactionId(b));
    }

    public static bool IsLocallyControlled(GameObject go)
    {
        return GetFactionId(go) == FactionManager.LocalPlayerFactionId;
    }

    /// <summary>Assigns a faction, adding the FactionMember component if needed.</summary>
    public static FactionMember SetFaction(GameObject go, int factionId)
    {
        FactionMember member = go.GetComponent<FactionMember>();
        if (member == null) member = go.AddComponent<FactionMember>();
        member.factionId = factionId;

        // Keep the legacy team field coherent for old code paths.
        Unit unit = go.GetComponent<Unit>();
        if (unit != null)
        {
            unit.team = factionId == FactionManager.LocalPlayerFactionId ? Team.Player : Team.Enemy;
        }
        return member;
    }
}
