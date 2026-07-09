using UnityEngine;

/// <summary>
/// A harvestable spot on the map, themed per element in the world of Avatar:
/// fish shoals, crystal deposits, coal seams, and spirit groves. Worker units
/// with a ResourceCollector walk here, gather a load, and carry it home.
/// Any nation can work any node type - the type is flavor plus map strategy.
/// </summary>
public class ResourceNode : MonoBehaviour
{
    public enum NodeType
    {
        SpiritGrove = 0,  // shimmering trees - Air flavor
        FishShoal = 1,    // river/coast spot - Water flavor
        CrystalDeposit = 2, // green crystals - Earth flavor
        CoalSeam = 3      // dark rock - Fire flavor
    }

    public NodeType nodeType = NodeType.CrystalDeposit;

    [Tooltip("Total silver in the node. It depletes; <= 0 means infinite.")]
    public int totalAmount = 1500;

    [Tooltip("Shrink the model as the node runs out.")]
    public bool shrinkAsDepleted = true;

    private int remaining;
    private Vector3 initialScale;

    public bool IsDepleted => totalAmount > 0 && remaining <= 0;

    private void Start()
    {
        remaining = totalAmount;
        initialScale = transform.localScale;
    }

    /// <summary>Takes up to <paramref name="requested"/> from the node.
    /// Returns how much was actually extracted.</summary>
    public int Extract(int requested)
    {
        if (totalAmount <= 0) return requested; // infinite node

        int taken = Mathf.Min(requested, remaining);
        remaining -= taken;

        if (shrinkAsDepleted && totalAmount > 0)
        {
            float fraction = Mathf.Clamp01((float)remaining / totalAmount);
            transform.localScale = initialScale * Mathf.Lerp(0.4f, 1f, fraction);
        }

        if (IsDepleted)
        {
            Destroy(gameObject, 2f);
        }
        return taken;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 2f);
    }
}
