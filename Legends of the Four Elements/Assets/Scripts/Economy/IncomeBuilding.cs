using UnityEngine;

/// <summary>
/// A building that generates silver over time for its owner. Nation flavor:
/// Air Meditation Pavilion, Water Fishing Dock, Earth Mine, Fire Coal
/// Refinery. Optionally require a matching ResourceNode nearby (a Mine only
/// works next to a Crystal Deposit) - BuildingPlacer enforces it at placement
/// and this component enforces it at runtime, draining the node as it earns.
/// </summary>
public class IncomeBuilding : MonoBehaviour
{
    public int incomePerTick = 10;
    public float tickInterval = 6f;

    [Header("Node Requirement (optional)")]
    public bool requiresNearbyNode = false;
    public ResourceNode.NodeType requiredNodeType = ResourceNode.NodeType.CrystalDeposit;
    public float nodeSearchRadius = 12f;

    private float timer;
    private ResourceNode linkedNode;

    private void Start()
    {
        timer = tickInterval;
        if (requiresNearbyNode) linkedNode = FindMatchingNode();
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = tickInterval;

        int amount = incomePerTick;

        if (requiresNearbyNode)
        {
            if (linkedNode == null || linkedNode.IsDepleted)
            {
                linkedNode = FindMatchingNode();
                if (linkedNode == null) return; // nothing left to work
            }
            amount = linkedNode.Extract(incomePerTick);
            if (amount <= 0) return;
        }

        Economy.Award(FactionUtility.GetFactionId(gameObject), amount);
    }

    public ResourceNode FindMatchingNode()
    {
        foreach (ResourceNode node in FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
        {
            if (node.nodeType != requiredNodeType || node.IsDepleted) continue;
            if (Vector3.Distance(transform.position, node.transform.position) <= nodeSearchRadius)
            {
                return node;
            }
        }
        return null;
    }

    /// <summary>Used by BuildingPlacer to validate a placement spot.</summary>
    public static bool HasMatchingNodeNear(Vector3 position, ResourceNode.NodeType type, float radius)
    {
        foreach (ResourceNode node in FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
        {
            if (node.nodeType != type || node.IsDepleted) continue;
            if (Vector3.Distance(position, node.transform.position) <= radius) return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (requiresNearbyNode)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, nodeSearchRadius);
        }
    }
}
