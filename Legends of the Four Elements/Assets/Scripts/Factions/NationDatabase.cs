using UnityEngine;

/// <summary>
/// Registry of the four NationData assets. Create exactly one via
/// Assets > Create > Legends > Nation Database and save it as
/// Assets/Resources/NationDatabase.asset so it can be loaded from any scene.
/// </summary>
[CreateAssetMenu(fileName = "NationDatabase", menuName = "Legends/Nation Database")]
public class NationDatabase : ScriptableObject
{
    public NationData[] nations;

    private static NationDatabase cached;

    public NationData Get(Nation nation)
    {
        if (nations == null) return null;
        foreach (NationData data in nations)
        {
            if (data != null && data.nation == nation) return data;
        }
        return null;
    }

    /// <summary>Loads Assets/Resources/NationDatabase.asset (cached).</summary>
    public static NationDatabase Load()
    {
        if (cached == null)
        {
            cached = Resources.Load<NationDatabase>("NationDatabase");
            if (cached == null)
            {
                Debug.LogWarning("NationDatabase.asset not found in a Resources folder. " +
                                 "Create it via Assets > Create > Legends > Nation Database " +
                                 "and place it at Assets/Resources/NationDatabase.asset.");
            }
        }
        return cached;
    }
}
