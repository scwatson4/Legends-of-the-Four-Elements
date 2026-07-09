using UnityEngine;

/// <summary>
/// Marks a base start position in a skirmish/multiplayer map. Place empty
/// GameObjects with this component around the map; index 0 is the local
/// player (or host), the rest are assigned in order.
/// </summary>
public class StartLocation : MonoBehaviour
{
    public int index;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 5f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 10f);
    }
}
