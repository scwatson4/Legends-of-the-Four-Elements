using UnityEngine;

/// <summary>
/// Marks a building where workers deliver harvested resources (command
/// centers, docks, refineries...). Add to any building prefab; ownership
/// comes from the building's FactionMember / legacy team.
/// </summary>
public class ResourceDropoff : MonoBehaviour
{
    public int OwnerFactionId => FactionUtility.GetFactionId(gameObject);
}
