using UnityEngine;

/// <summary>
/// How far this unit or building can see through the fog of war.
/// Units without one use FogOfWar.defaultSightRange. Give scouts and flyers
/// (winged lemurs, sky bison, war balloons) a big value; watchtowers too.
/// The "sensing" upgrades (UpgradeData.sightBonus) raise it at runtime.
/// </summary>
public class VisionSource : MonoBehaviour
{
    public float sightRange = 15f;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, sightRange);
    }
}
