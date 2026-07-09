using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks purchased upgrade levels per faction and applies the stat bonuses
/// to every existing and future unit. Purchases go through Economy, so the
/// money side is consistent for players and AI.
/// </summary>
public static class UpgradeManager
{
    private static readonly Dictionary<(int factionId, string upgradeId), int> levels =
        new Dictionary<(int, string), int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        levels.Clear();
    }

    public static void Reset() => levels.Clear();

    public static int GetLevel(int factionId, UpgradeData upgrade)
    {
        if (upgrade == null) return 0;
        levels.TryGetValue((factionId, upgrade.upgradeId), out int level);
        return level;
    }

    /// <summary>Buys the next level of an upgrade for a faction. Pays via Economy.</summary>
    public static bool TryPurchase(int factionId, UpgradeData upgrade)
    {
        if (upgrade == null) return false;

        int currentLevel = GetLevel(factionId, upgrade);
        if (currentLevel >= upgrade.maxLevel) return false;

        int cost = upgrade.CostForLevel(currentLevel + 1);
        if (!Economy.TrySpend(factionId, cost)) return false;

        levels[(factionId, upgrade.upgradeId)] = currentLevel + 1;
        Debug.Log($"Faction {factionId} bought {upgrade.displayName} level {currentLevel + 1}.");

        ReapplyToFaction(factionId);
        return true;
    }

    /// <summary>Campaign: seed an already-owned permanent level without paying.</summary>
    public static void SetLevelDirect(int factionId, UpgradeData upgrade, int level)
    {
        if (upgrade == null || level <= 0) return;
        levels[(factionId, upgrade.upgradeId)] = Mathf.Min(level, upgrade.maxLevel);
        ReapplyToFaction(factionId);
    }

    /// <summary>Multiplayer clients: mirror a level the server already paid for.</summary>
    public static void ApplyPurchasedLevel(int factionId, UpgradeData upgrade)
    {
        if (upgrade == null) return;
        int currentLevel = GetLevel(factionId, upgrade);
        if (currentLevel >= upgrade.maxLevel) return;

        levels[(factionId, upgrade.upgradeId)] = currentLevel + 1;
        ReapplyToFaction(factionId);
    }

    /// <summary>Recomputes and applies all upgrade bonuses to one unit.</summary>
    public static void ApplyTo(Unit unit)
    {
        if (unit == null) return;

        int factionId = unit.FactionId;
        Faction faction = FactionManager.Get(factionId);
        if (faction == null) return;

        NationDatabase db = NationDatabase.Load();
        NationData data = db != null ? db.Get(faction.nation) : null;
        if (data == null || data.upgrades == null || data.upgrades.Length == 0) return;

        float damageMult = 1f, healthMult = 1f, speedMult = 1f, sightMult = 1f;
        foreach (UpgradeData upgrade in data.upgrades)
        {
            if (upgrade == null || !upgrade.AppliesTo(unit.category)) continue;
            int level = GetLevel(factionId, upgrade);
            if (level <= 0) continue;

            damageMult += level * upgrade.damageBonus;
            healthMult += level * upgrade.healthBonus;
            speedMult += level * upgrade.speedBonus;
            sightMult += level * upgrade.sightBonus;
        }

        if (Mathf.Approximately(damageMult, 1f) &&
            Mathf.Approximately(healthMult, 1f) &&
            Mathf.Approximately(speedMult, 1f) &&
            Mathf.Approximately(sightMult, 1f)) return;

        UnitBaseStats baseStats = unit.GetComponent<UnitBaseStats>();
        if (baseStats == null) baseStats = unit.gameObject.AddComponent<UnitBaseStats>();
        baseStats.ApplyMultipliers(damageMult, healthMult, speedMult, sightMult);
    }

    private static void ReapplyToFaction(int factionId)
    {
        if (UnitSelectionManager.Instance == null) return;

        foreach (GameObject go in UnitSelectionManager.Instance.allUnitsList)
        {
            if (go == null) continue;
            Unit unit = go.GetComponent<Unit>();
            if (unit != null && unit.FactionId == factionId)
            {
                ApplyTo(unit);
            }
        }
    }
}
