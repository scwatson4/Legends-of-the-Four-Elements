using UnityEngine;

/// <summary>
/// Halo Wars-style population: every unit costs population (Unit.populationCost),
/// and a faction's cap is the base allowance plus everything its
/// PopulationHousing buildings provide. Training is blocked at the cap, so
/// growing your army means growing your base. Computed live, so deaths free
/// population and losing a dormitory shrinks your cap immediately.
/// </summary>
public static class PopulationManager
{
    [Tooltip("Cap every faction has with no housing at all.")]
    public static int basePopulationCap = 25;

    public static int GetPopulation(int factionId)
    {
        if (UnitSelectionManager.Instance == null) return 0;

        int total = 0;
        foreach (GameObject go in UnitSelectionManager.Instance.allUnitsList)
        {
            if (go == null) continue;
            if (FactionUtility.GetFactionId(go) != factionId) continue;

            Unit unit = go.GetComponent<Unit>();
            if (unit != null) total += Mathf.Max(1, unit.populationCost);
        }
        return total;
    }

    public static int GetCap(int factionId)
    {
        int cap = basePopulationCap;
        foreach (PopulationHousing housing in PopulationHousing.All)
        {
            if (housing == null) continue;
            if (FactionUtility.GetFactionId(housing.gameObject) != factionId) continue;
            cap += housing.populationProvided;
        }
        return cap;
    }

    public static int PopulationCostOf(GameObject prefab)
    {
        if (prefab == null) return 1;
        Unit unit = prefab.GetComponent<Unit>();
        return unit != null ? Mathf.Max(1, unit.populationCost) : 1;
    }

    public static bool HasRoomFor(int factionId, GameObject prefab)
    {
        return GetPopulation(factionId) + PopulationCostOf(prefab) <= GetCap(factionId);
    }
}
