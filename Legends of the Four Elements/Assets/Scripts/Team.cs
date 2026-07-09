using UnityEngine;

/// <summary>
/// LEGACY: the original two-sided team enum, kept so existing prefabs and
/// scenes deserialize unchanged. New code should use FactionMember /
/// FactionManager, which support all four nations, neutrals and multiplayer.
/// Team.Player maps to faction 0, Team.Enemy to faction 1.
/// </summary>
public enum Team
{
    Player,
    Enemy
}
