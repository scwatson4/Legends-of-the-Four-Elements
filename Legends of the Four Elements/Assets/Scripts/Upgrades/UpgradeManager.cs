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

    /// <summary>
    /// Tech-tree gate: can this faction buy the next level right now?
    /// Checks max level, branch prerequisites, exclusive-branch locks, and
    /// the research building requirement. `reason` explains a refusal.
    /// (Money is NOT checked here - TryPurchase handles that.)
    /// </summary>
    public static bool CanPurchase(int factionId, UpgradeData upgrade, out string reason)
    {
        reason = "";
        if (upgrade == null) { reason = "No such upgrade."; return false; }

        if (GetLevel(factionId, upgrade) >= upgrade.maxLevel)
        {
            reason = $"{upgrade.displayName} is already mastered.";
            return false;
        }

        NationData data = GetNationData(factionId);

        // Branch prerequisites: every listed id must be owned at level 1+.
        if (upgrade.prerequisiteUpgradeIds != null)
        {
            foreach (string prereqId in upgrade.prerequisiteUpgradeIds)
            {
                if (string.IsNullOrEmpty(prereqId)) continue;
                UpgradeData prereq = FindById(data, prereqId);
                if (prereq == null || GetLevel(factionId, prereq) <= 0)
                {
                    reason = $"{upgrade.displayName} requires {(prereq != null ? prereq.displayName : prereqId)} first.";
                    return false;
                }
            }
        }

        // Exclusive branches: owning either side locks the other, permanently.
        if (IsLockedOut(factionId, upgrade, data, out string lockedBy))
        {
            reason = $"{upgrade.displayName} is sealed - your benders chose {lockedBy}.";
            return false;
        }

        // Research building: some branches are studied in a specific hall.
        if (!string.IsNullOrEmpty(upgrade.requiredBuildingKeyword) &&
            !FactionOwnsBuilding(factionId, upgrade.requiredBuildingKeyword))
        {
            reason = $"{upgrade.displayName} is researched at a {upgrade.requiredBuildingKeyword} - build one first.";
            return false;
        }

        return true;
    }

    /// <summary>True when an exclusive rival branch is already owned.
    /// Checked in BOTH directions so one side listing the other suffices.</summary>
    public static bool IsLockedOut(int factionId, UpgradeData upgrade, NationData data, out string lockedBy)
    {
        lockedBy = "";
        if (upgrade == null) return false;

        if (upgrade.exclusiveWithUpgradeIds != null)
        {
            foreach (string rivalId in upgrade.exclusiveWithUpgradeIds)
            {
                UpgradeData rival = FindById(data, rivalId);
                if (rival != null && GetLevel(factionId, rival) > 0)
                {
                    lockedBy = rival.displayName;
                    return true;
                }
            }
        }

        if (data != null && data.upgrades != null)
        {
            foreach (UpgradeData other in data.upgrades)
            {
                if (other == null || other == upgrade || other.exclusiveWithUpgradeIds == null) continue;
                if (GetLevel(factionId, other) <= 0) continue;
                foreach (string rivalId in other.exclusiveWithUpgradeIds)
                {
                    if (rivalId == upgrade.upgradeId)
                    {
                        lockedBy = other.displayName;
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private static UpgradeData FindById(NationData data, string upgradeId)
    {
        if (data == null || data.upgrades == null || string.IsNullOrEmpty(upgradeId)) return null;
        foreach (UpgradeData upgrade in data.upgrades)
        {
            if (upgrade != null && upgrade.upgradeId == upgradeId) return upgrade;
        }
        return null;
    }

    private static NationData GetNationData(int factionId)
    {
        Faction faction = FactionManager.Get(factionId);
        NationDatabase db = NationDatabase.Load();
        return db != null && faction != null ? db.Get(faction.nation) : null;
    }

    private static bool FactionOwnsBuilding(int factionId, string keyword)
    {
        foreach (Structure structure in Object.FindObjectsByType<Structure>(FindObjectsSortMode.None))
        {
            if (structure.FactionId == factionId &&
                structure.gameObject.name.Contains(keyword)) return true;
        }
        foreach (CommandCenter cc in Object.FindObjectsByType<CommandCenter>(FindObjectsSortMode.None))
        {
            if (FactionUtility.GetFactionId(cc.gameObject) == factionId &&
                cc.gameObject.name.Contains(keyword)) return true;
        }
        return false;
    }

    /// <summary>Buys the next level of an upgrade for a faction. Pays via Economy.
    /// Enforces the tech tree (prerequisites, exclusive branches, research buildings).</summary>
    public static bool TryPurchase(int factionId, UpgradeData upgrade)
    {
        if (upgrade == null) return false;
        if (!CanPurchase(factionId, upgrade, out _)) return false;

        int currentLevel = GetLevel(factionId, upgrade);

        int cost = upgrade.CostForLevel(currentLevel + 1);
        if (!Economy.TrySpend(factionId, cost)) return false;

        levels[(factionId, upgrade.upgradeId)] = currentLevel + 1;
        Debug.Log($"Faction {factionId} bought {upgrade.displayName} level {currentLevel + 1}.");

        if (factionId == FactionManager.LocalPlayerFactionId) TutorialSignals.UpgradesPurchased++;

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
        int grantedHealPerSecond = 0;
        float grantedHealRadius = 7f;
        float redirectChance = 0f;
        bool metalBending = false;
        bool lavaBending = false;
        bool tremorAssault = false;
        bool gliderFlight = false;
        bool tornadoSummon = false;
        bool everfrost = false;
        bool flameDive = false;
        bool fireSpray = false;
        float avatarEnergyMult = 1f;
        float avatarPowerMult = 1f;

        foreach (UpgradeData upgrade in data.upgrades)
        {
            if (upgrade == null || !upgrade.AppliesToUnit(unit)) continue;
            int level = GetLevel(factionId, upgrade);
            if (level <= 0) continue;

            damageMult += level * upgrade.damageBonus;
            healthMult += level * upgrade.healthBonus;
            speedMult += level * upgrade.speedBonus;
            sightMult += level * upgrade.sightBonus;

            // Granted abilities (e.g. Healing Waters: waterbenders heal allies).
            if (upgrade.grantsHealing)
            {
                grantedHealPerSecond += level * upgrade.healPerSecondPerLevel;
                grantedHealRadius = Mathf.Max(grantedHealRadius, upgrade.healRadius);
            }
            if (upgrade.grantsLightningRedirect)
            {
                redirectChance += level * upgrade.redirectChancePerLevel;
            }
            if (upgrade.grantsMetalBending)
            {
                metalBending = true;
            }
            if (upgrade.grantsLavaBending)
            {
                lavaBending = true;
            }
            if (upgrade.grantsTremorAssault)
            {
                tremorAssault = true;
            }
            if (upgrade.grantsGliderFlight) gliderFlight = true;
            if (upgrade.grantsTornadoSummon) tornadoSummon = true;
            if (upgrade.grantsEverfrost) everfrost = true;
            if (upgrade.grantsFlameDive) flameDive = true;
            if (upgrade.grantsFireSpray) fireSpray = true;
            if (upgrade.improvesAvatarEnergy) avatarEnergyMult += level * upgrade.avatarEnergyRegenBonus;
            if (upgrade.improvesAvatarPower) avatarPowerMult += level * upgrade.avatarStatePowerBonus;
        }

        if (grantedHealPerSecond > 0)
        {
            Healer healer = unit.GetComponent<Healer>();
            if (healer == null) healer = unit.gameObject.AddComponent<Healer>();
            healer.healAmount = grantedHealPerSecond;
            healer.healInterval = 1f;
            healer.healRadius = grantedHealRadius;
        }

        if (redirectChance > 0f)
        {
            LightningRedirect redirect = unit.GetComponent<LightningRedirect>();
            if (redirect == null) redirect = unit.gameObject.AddComponent<LightningRedirect>();
            redirect.chance = Mathf.Min(0.9f, redirectChance);
        }

        if (metalBending && unit.GetComponent<MetalBending>() == null)
        {
            unit.gameObject.AddComponent<MetalBending>();
        }

        if (lavaBending && unit.GetComponent<LavaBending>() == null)
        {
            unit.gameObject.AddComponent<LavaBending>();
        }

        if (tremorAssault && unit.GetComponent<TremorAssault>() == null)
        {
            unit.gameObject.AddComponent<TremorAssault>();
        }

        // Branch abilities.
        if (gliderFlight)
        {
            AirbenderMobility mobility = unit.GetComponent<AirbenderMobility>();
            if (mobility != null) mobility.gliderUnlocked = true;
        }
        if (tornadoSummon && unit.GetComponent<TornadoSummon>() == null)
        {
            unit.gameObject.AddComponent<TornadoSummon>();
        }
        if (everfrost && unit.GetComponent<Everfrost>() == null)
        {
            unit.gameObject.AddComponent<Everfrost>();
        }
        if (flameDive && unit.GetComponent<FlameDive>() == null)
        {
            unit.gameObject.AddComponent<FlameDive>();
        }
        if (fireSpray && unit.GetComponent<FireSpray>() == null)
        {
            unit.gameObject.AddComponent<FireSpray>();
        }

        // Avatar tracks: recomputed totals, so reapplication never stacks.
        AvatarUnit avatarUnit = unit.GetComponent<AvatarUnit>();
        if (avatarUnit != null)
        {
            avatarUnit.energyRegenMultiplier = avatarEnergyMult;
            avatarUnit.statePowerMultiplier = avatarPowerMult;
        }

        if (Mathf.Approximately(damageMult, 1f) &&
            Mathf.Approximately(healthMult, 1f) &&
            Mathf.Approximately(speedMult, 1f) &&
            Mathf.Approximately(sightMult, 1f)) return;

        UnitBaseStats baseStats = unit.GetComponent<UnitBaseStats>();
        if (baseStats == null) baseStats = unit.gameObject.AddComponent<UnitBaseStats>();
        baseStats.ApplyMultipliers(damageMult, healthMult, speedMult, sightMult);
    }

    /// <summary>Shield-mastery bonuses for a unit's faction+type: strength and
    /// duration multipliers (>= 1) and a cooldown multiplier (<= 1).</summary>
    public static void GetShieldMultipliers(Unit unit,
        out float strengthMult, out float durationMult, out float cooldownMult)
    {
        strengthMult = 1f;
        durationMult = 1f;
        cooldownMult = 1f;
        if (unit == null) return;

        int factionId = unit.FactionId;
        Faction faction = FactionManager.Get(factionId);
        NationDatabase db = NationDatabase.Load();
        NationData data = db != null && faction != null ? db.Get(faction.nation) : null;
        if (data == null || data.upgrades == null) return;

        foreach (UpgradeData upgrade in data.upgrades)
        {
            if (upgrade == null || !upgrade.improvesShields || !upgrade.AppliesToUnit(unit)) continue;
            int level = GetLevel(factionId, upgrade);
            if (level <= 0) continue;

            strengthMult += level * upgrade.shieldStrengthBonus;
            durationMult += level * upgrade.shieldDurationBonus;
            cooldownMult *= Mathf.Pow(1f - upgrade.shieldCooldownReduction, level);
        }
    }

    /// <summary>Frozen Grasp: how much longer this unit's freezes last (>= 1).</summary>
    public static float GetFreezeDurationMultiplier(Unit unit)
    {
        float multiplier = 1f;
        if (unit == null) return multiplier;

        int factionId = unit.FactionId;
        NationData data = GetNationData(factionId);
        if (data == null || data.upgrades == null) return multiplier;

        foreach (UpgradeData upgrade in data.upgrades)
        {
            if (upgrade == null || !upgrade.improvesFreezing || !upgrade.AppliesToUnit(unit)) continue;
            int level = GetLevel(factionId, upgrade);
            if (level > 0) multiplier += level * upgrade.freezeDurationBonus;
        }
        return multiplier;
    }

    /// <summary>Applies Building-category upgrades to a tower/wall/building.</summary>
    public static void ApplyToBuilding(Structure structure)
    {
        if (structure == null) return;

        int factionId = structure.FactionId;
        Faction faction = FactionManager.Get(factionId);
        if (faction == null) return;

        NationDatabase db = NationDatabase.Load();
        NationData data = db != null ? db.Get(faction.nation) : null;
        if (data == null || data.upgrades == null || data.upgrades.Length == 0) return;

        float damageMult = 1f, healthMult = 1f, rateMult = 1f, rangeMult = 1f;
        foreach (UpgradeData upgrade in data.upgrades)
        {
            if (upgrade == null || !upgrade.AppliesTo(UnitCategory.Building)) continue;
            int level = GetLevel(factionId, upgrade);
            if (level <= 0) continue;

            damageMult += level * upgrade.damageBonus;
            healthMult += level * upgrade.healthBonus;
            rateMult += level * upgrade.speedBonus;   // speed = fire rate for towers
            rangeMult += level * upgrade.sightBonus;  // sight = range for towers
        }

        if (Mathf.Approximately(damageMult, 1f) && Mathf.Approximately(healthMult, 1f) &&
            Mathf.Approximately(rateMult, 1f) && Mathf.Approximately(rangeMult, 1f)) return;

        StructureBaseStats baseStats = structure.GetComponent<StructureBaseStats>();
        if (baseStats == null) baseStats = structure.gameObject.AddComponent<StructureBaseStats>();
        baseStats.ApplyMultipliers(damageMult, healthMult, rateMult, rangeMult);
    }

    private static void ReapplyToFaction(int factionId)
    {
        if (UnitSelectionManager.Instance != null)
        {
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

        // Buildings too: towers hit harder, walls stand longer.
        foreach (Structure structure in Object.FindObjectsByType<Structure>(FindObjectsSortMode.None))
        {
            if (structure.FactionId == factionId)
            {
                ApplyToBuilding(structure);
            }
        }
    }
}
