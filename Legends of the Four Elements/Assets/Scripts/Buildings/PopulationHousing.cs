using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A building that raises its owner's population cap while it stands:
/// Air Dormitories, Water Tribe Lodges, Earth Tenements, Fire Barracks
/// Quarters - and the command center itself. Destroying housing shrinks the
/// enemy's cap: a real reason to raid.
/// Add to building prefabs alongside Structure.
/// </summary>
public class PopulationHousing : MonoBehaviour
{
    public int populationProvided = 10;

    public static readonly List<PopulationHousing> All = new List<PopulationHousing>();

    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);
}
