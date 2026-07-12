using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Spreads group move orders into a natural battle formation instead of
/// every unit piling onto one exact point: slot 0 takes the center, then
/// concentric rings of 6, 12, 18... units around it, snapped to the
/// NavMesh. Used by move orders, attack-move, and voice reinforcements.
/// </summary>
public static class FormationUtility
{
    public const float DefaultSpacing = 2.4f;

    /// <summary>Formation offset for one slot of a group of `count`.</summary>
    public static Vector3 GetOffset(int index, int count, float spacing = DefaultSpacing)
    {
        if (count <= 1 || index <= 0) return Vector3.zero;

        // Ring layout: 6 in ring 1, 12 in ring 2, 18 in ring 3...
        int slot = index - 1; // slot 0 of ring 1
        int ring = 1;
        int ringCapacity = 6;
        while (slot >= ringCapacity)
        {
            slot -= ringCapacity;
            ring++;
            ringCapacity = 6 * ring;
        }

        float angle = (Mathf.PI * 2f / ringCapacity) * slot
                      + ring * 0.35f; // stagger rings so lines don't align
        float radius = ring * spacing;
        return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
    }

    /// <summary>Formation destination around a point, snapped onto the NavMesh.</summary>
    public static Vector3 GetDestination(Vector3 center, int index, int count,
        float spacing = DefaultSpacing)
    {
        Vector3 target = center + GetOffset(index, count, spacing);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(target, out hit, spacing * 2f, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return center; // blocked slot: fall back to the ordered point
    }
}
