using UnityEngine;

/// <summary>
/// Attach to any unit or building to declare which faction owns it.
/// Objects without a FactionMember fall back to the legacy Team enum
/// (Team.Player = faction 0, Team.Enemy = faction 1) via FactionUtility.
/// </summary>
public class FactionMember : MonoBehaviour
{
    public int factionId = FactionManager.NoFaction;
}
