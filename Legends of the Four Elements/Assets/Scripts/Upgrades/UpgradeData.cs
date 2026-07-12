using UnityEngine;

/// <summary>
/// A purchasable upgrade track for a nation (e.g. "Master Waterbending
/// Scrolls": +15% damage per level for Water infantry). Create via
/// Assets > Create > Legends > Upgrade, add to the nation's NationData.
/// Each level costs more; effects stack per level and apply to existing AND
/// future units of the matching categories.
/// </summary>
[CreateAssetMenu(fileName = "Upgrade", menuName = "Legends/Upgrade")]
public class UpgradeData : ScriptableObject
{
    [Tooltip("Unique id, e.g. 'water_damage'. Used to track purchased levels.")]
    public string upgradeId = "upgrade_id";
    public string displayName = "Upgrade";
    [TextArea] public string description;
    public Sprite icon;

    [Header("Cost")]
    public int baseCost = 150;
    [Tooltip("Each level costs baseCost * level (level 1 = baseCost, level 2 = 2x...).")]
    public int maxLevel = 3;

    [Header("Effect per level (fractions: 0.15 = +15%)")]
    public float damageBonus = 0.15f;
    public float healthBonus = 0f;
    public float speedBonus = 0f;
    [Tooltip("Extends fog-of-war sight range, e.g. an earthbender 'seismic sensing' track.")]
    public float sightBonus = 0f;

    [Header("Granted Ability")]
    [Tooltip("This upgrade teaches the units to HEAL nearby allies " +
             "(e.g. Water's 'Healing Waters' for waterbenders). Healing power scales per level.")]
    public bool grantsHealing = false;
    public int healPerSecondPerLevel = 2;
    public float healRadius = 7f;

    [Header("Shield Mastery")]
    [Tooltip("This upgrade strengthens the benders' Q shields (use restrictToUnitTypes " +
             "to target one bender type - it applies to ALL units of that type).")]
    public bool improvesShields = false;
    [Tooltip("Per level: shield strength (+15% reduction/absorb), duration, cooldown cut.")]
    public float shieldStrengthBonus = 0.15f;
    public float shieldDurationBonus = 0.2f;
    [Range(0f, 0.3f)] public float shieldCooldownReduction = 0.1f;

    [Header("Applies to")]
    public UnitCategory[] categories = { UnitCategory.Infantry };
    [Tooltip("Optional extra filter: only these unit types benefit " +
             "(e.g. Waterbender only). Empty = every unit in the categories.")]
    public Unit.UnitType[] restrictToUnitTypes;

    public int CostForLevel(int level) => baseCost * Mathf.Max(1, level);

    public bool AppliesTo(UnitCategory category)
    {
        if (categories == null || categories.Length == 0) return true;
        foreach (UnitCategory c in categories)
        {
            if (c == category) return true;
        }
        return false;
    }

    public bool AppliesToUnit(Unit unit)
    {
        if (unit == null) return false;
        if (!AppliesTo(unit.category)) return false;

        if (restrictToUnitTypes == null || restrictToUnitTypes.Length == 0) return true;
        foreach (Unit.UnitType type in restrictToUnitTypes)
        {
            if (type == unit.unitType) return true;
        }
        return false;
    }
}
