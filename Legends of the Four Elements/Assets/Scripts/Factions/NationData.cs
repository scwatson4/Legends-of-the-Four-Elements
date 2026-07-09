using UnityEngine;

/// <summary>What kind of thing a roster unit is. Serialized - append only.</summary>
public enum UnitCategory
{
    Infantry = 0,  // benders and soldiers
    Animal = 1,    // sky bison, polar bear dogs, badgermoles, komodo rhinos
    Vehicle = 2,   // ships, tanks, war balloons
    Worker = 3,    // harvesters: acolytes, fishermen, miners, engineers
    Avatar = 4     // the one-per-player hero
}

/// <summary>Building roles for UI grouping. Serialized - append only.</summary>
public enum BuildingCategory
{
    Economy = 0,     // income buildings (docks, mines, refineries, pavilions)
    Production = 1,  // trains units (barracks, stables, shipyards, factories)
    Defense = 2,     // towers
    Special = 3      // shrines, wonders
}

/// <summary>
/// Everything that makes a nation playable: its unit roster, buildings,
/// upgrades and presentation. Create one asset per nation via
/// Assets > Create > Legends > Nation Data, then reference all four from the
/// NationDatabase asset. See docs/ROSTERS.md for the full recommended
/// roster/cost table for each nation.
/// </summary>
[CreateAssetMenu(fileName = "NationData", menuName = "Legends/Nation Data")]
public class NationData : ScriptableObject
{
    [System.Serializable]
    public class UnitEntry
    {
        public string unitName = "Bender";
        public GameObject prefab;
        public int cost = 50;
        public float buildTime = 3f;
        public Sprite icon;
        public UnitCategory category = UnitCategory.Infantry;
    }

    [System.Serializable]
    public class BuildingEntry
    {
        public string buildingName = "Building";
        public GameObject prefab;
        public int cost = 150;
        public Sprite icon;
        public BuildingCategory category = BuildingCategory.Production;
    }

    [Header("Identity")]
    public Nation nation = Nation.Air;
    public string displayName = "Air Nomads";
    [TextArea] public string description;
    public Color themeColor = Color.white;
    public Sprite emblem;

    [Header("Roster (slot 0 = basic unit / starting squad)")]
    public UnitEntry[] units;

    [Header("Buildings")]
    public GameObject commandCenterPrefab;
    public GameObject towerPrefab;
    public BuildingEntry[] buildings;

    [Header("Upgrades")]
    public UpgradeData[] upgrades;

    [Header("Audio")]
    public AudioClip attackSound;

    public UnitEntry GetUnit(int index)
    {
        if (units == null || index < 0 || index >= units.Length) return null;
        return units[index];
    }

    public BuildingEntry GetBuilding(int index)
    {
        if (buildings == null || index < 0 || index >= buildings.Length) return null;
        return buildings[index];
    }
}
