using UnityEngine;

/// <summary>
/// Marks a building as bender-raised: it can only be CONSTRUCTED while the
/// owner has living benders of the required discipline in their army
/// (walls and gates are earthbent into existence - no earthbenders, no
/// walls). BuildingPlacer and the voice commander both enforce it.
/// Add to the wall/gate prefab root.
/// </summary>
public class RequiresBenderPresence : MonoBehaviour
{
    public Unit.UnitType requiredBender = Unit.UnitType.Earthbender;
    public int minimumCount = 1;

    /// <summary>Does this faction currently field the required benders?</summary>
    public static bool SatisfiedFor(GameObject prefab, int factionId)
    {
        RequiresBenderPresence requirement =
            prefab != null ? prefab.GetComponent<RequiresBenderPresence>() : null;
        if (requirement == null) return true; // no requirement on this building

        return CountBenders(factionId, requirement.requiredBender) >= requirement.minimumCount;
    }

    public static int CountBenders(int factionId, Unit.UnitType benderType)
    {
        if (UnitSelectionManager.Instance == null) return 0;

        int count = 0;
        foreach (GameObject go in UnitSelectionManager.Instance.allUnitsList)
        {
            if (go == null) continue;
            if (FactionUtility.GetFactionId(go) != factionId) continue;

            Unit unit = go.GetComponent<Unit>();
            if (unit != null && unit.unitType == benderType) count++;
        }
        return count;
    }
}
