using UnityEngine;

/// <summary>
/// Everything that makes a nation playable: its unit roster, buildings and
/// presentation. Create one asset per nation via
/// Assets > Create > Legends > Nation Data, then reference all four from the
/// NationDatabase asset.
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
    }

    [Header("Identity")]
    public Nation nation = Nation.Air;
    public string displayName = "Air Nomads";
    [TextArea] public string description;
    public Color themeColor = Color.white;
    public Sprite emblem;

    [Header("Roster")]
    public UnitEntry[] units;

    [Header("Buildings")]
    public GameObject commandCenterPrefab;
    public GameObject towerPrefab;

    [Header("Audio")]
    public AudioClip attackSound;

    public UnitEntry GetUnit(int index)
    {
        if (units == null || index < 0 || index >= units.Length) return null;
        return units[index];
    }
}
