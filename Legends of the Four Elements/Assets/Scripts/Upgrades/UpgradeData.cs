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

    [Header("Applies to")]
    public UnitCategory[] categories = { UnitCategory.Infantry };

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
}
