using System;

/// <summary>
/// A faction is one seat in a match: a human player, an AI commander, or the
/// hostile spirits. Factions on the same teamGroup are allies (co-op);
/// factions on different teamGroups are enemies (versus).
/// </summary>
[Serializable]
public class Faction
{
    public int id;
    public Nation nation = Nation.Air;
    public string displayName;
    public int teamGroup;
    public bool isAI;
    public bool isLocalPlayer;
    public bool isDefeated;

    /// <summary>Credits for AI / remote factions. The local player's credits
    /// live in PlayerResources so the existing UI keeps working.</summary>
    public int credits = 300;
}
