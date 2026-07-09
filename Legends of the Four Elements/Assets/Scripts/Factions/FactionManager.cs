using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central registry of every faction in the current match, plus the diplomacy
/// rules (who is hostile to whom). Replaces the old two-value Team enum while
/// staying backward compatible with it: if a scene never registers factions
/// (the original Level1 flow), a legacy "Player vs Fire AI" pair is created
/// on demand so all existing behaviour is preserved.
/// </summary>
public static class FactionManager
{
    /// <summary>Truly neutral beings: villagers, untamed friendly spirits.
    /// Nobody auto-attacks them and they attack nobody.</summary>
    public const int NoFaction = -1;

    /// <summary>Dark spirits. Hostile to every faction, including neutrals.</summary>
    public const int HostileSpiritsFaction = 90;

    private static readonly Dictionary<int, Faction> factions = new Dictionary<int, Faction>();

    public static int LocalPlayerFactionId { get; set; } = 0;

    // Statics survive disabled domain reload in the editor, so reset explicitly.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        factions.Clear();
        LocalPlayerFactionId = 0;
    }

    public static void Reset()
    {
        factions.Clear();
        LocalPlayerFactionId = 0;
    }

    public static void Register(Faction faction)
    {
        factions[faction.id] = faction;
        if (faction.isLocalPlayer)
        {
            LocalPlayerFactionId = faction.id;
        }
    }

    public static Faction Get(int factionId)
    {
        EnsureLegacyFactions();
        factions.TryGetValue(factionId, out Faction faction);
        return faction;
    }

    public static IEnumerable<Faction> All
    {
        get
        {
            EnsureLegacyFactions();
            return factions.Values;
        }
    }

    public static bool AreHostile(int a, int b)
    {
        if (a == b) return false;

        // Dark spirits hate everyone - even neutrals.
        if (a == HostileSpiritsFaction || b == HostileSpiritsFaction) return true;

        // Neutrals fight nobody and nobody fights them.
        if (a == NoFaction || b == NoFaction) return false;

        EnsureLegacyFactions();

        if (factions.TryGetValue(a, out Faction fa) && factions.TryGetValue(b, out Faction fb))
        {
            return fa.teamGroup != fb.teamGroup;
        }

        // Unknown faction ids: assume hostile (safe default for combat).
        return true;
    }

    public static bool IsAIControlled(int factionId)
    {
        if (factionId == HostileSpiritsFaction) return true;
        if (factionId == NoFaction) return false;

        Faction faction = Get(factionId);
        if (faction != null) return faction.isAI;

        // Legacy fallback: faction 1 was the old Team.Enemy.
        return factionId != LocalPlayerFactionId;
    }

    public static bool IsTeamDefeated(int teamGroup)
    {
        bool foundAny = false;
        foreach (Faction faction in factions.Values)
        {
            if (faction.teamGroup != teamGroup) continue;
            foundAny = true;
            if (!faction.isDefeated) return false;
        }
        return foundAny;
    }

    /// <summary>True when every faction hostile to the local player is defeated.</summary>
    public static bool AllEnemyTeamsDefeated()
    {
        Faction local = Get(LocalPlayerFactionId);
        if (local == null) return false;

        foreach (Faction faction in factions.Values)
        {
            if (faction.teamGroup == local.teamGroup) continue;
            if (!faction.isDefeated) return false;
        }
        return true;
    }

    /// <summary>
    /// Creates the classic "Player (Air) vs Fire AI" pair when a scene is
    /// played without a MatchManager (the original Level1 survival flow).
    /// </summary>
    public static void EnsureLegacyFactions()
    {
        if (factions.Count > 0) return;

        factions[0] = new Faction
        {
            id = 0,
            nation = GameSetup.PlayerNation,
            displayName = NationInfo.DisplayName(GameSetup.PlayerNation),
            teamGroup = 0,
            isAI = false,
            isLocalPlayer = true
        };
        factions[1] = new Faction
        {
            id = 1,
            nation = Nation.Fire,
            displayName = NationInfo.DisplayName(Nation.Fire),
            teamGroup = 1,
            isAI = true
        };
        LocalPlayerFactionId = 0;
    }
}
