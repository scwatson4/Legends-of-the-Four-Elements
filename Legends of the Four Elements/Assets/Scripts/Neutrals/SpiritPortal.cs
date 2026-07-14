using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A tear between the physical and spirit worlds. Periodically releases
/// spirits (mostly dark ones) up to a living cap - a persistent threat and a
/// bounty/taming farm worth controlling. Shows on the minimap once
/// discovered. Place by hand or via MapGenerator.
/// </summary>
public class SpiritPortal : MonoBehaviour
{
    public GameObject darkSpiritPrefab;
    public GameObject friendlySpiritPrefab;
    [Range(0f, 1f)] public float darkSpiritChance = 0.75f;

    public float spawnInterval = 45f;
    public int maxAliveSpirits = 3;
    public float spawnRadius = 5f;

    [Tooltip("Optional swirling portal VFX, purely cosmetic.")]
    public GameObject portalEffect;

    [Header("Spirit-World Travel")]
    [Tooltip("Units can enter this portal and emerge from the linked one - " +
             "a shortcut through the spirit world.")]
    public bool travelEnabled = true;
    [Tooltip("Explicit exit portal. Empty = auto-links to the FARTHEST other portal.")]
    public SpiritPortal linkedPortal;
    public float transitSeconds = 4f;
    public float entryRadius = 5f;

    private readonly List<GameObject> alive = new List<GameObject>();
    private readonly List<GameObject> boarding = new List<GameObject>();
    private float timer;

    private void Start()
    {
        MinimapPOI.Ensure(gameObject, MinimapPOI.POIType.SpiritPortal);
        timer = spawnInterval * 0.5f; // first spirit comes fairly soon
    }

    // ------------------------------------------------------------------
    // Spirit-world travel: enter here, emerge at the linked portal
    // ------------------------------------------------------------------

    /// <summary>The exit portal - explicit link, or the farthest other portal
    /// (long shortcuts are the interesting ones).</summary>
    public SpiritPortal ResolveLink()
    {
        if (linkedPortal != null) return linkedPortal;

        SpiritPortal best = null;
        float bestDistance = 0f;
        foreach (SpiritPortal portal in FindObjectsByType<SpiritPortal>(FindObjectsSortMode.None))
        {
            if (portal == this || !portal.travelEnabled) continue;
            float distance = Vector3.Distance(transform.position, portal.transform.position);
            if (distance > bestDistance)
            {
                bestDistance = distance;
                best = portal;
            }
        }
        return best;
    }

    /// <summary>Orders a unit to walk into the portal (right-click order).</summary>
    public bool OrderTravel(GameObject unit)
    {
        if (!travelEnabled || unit == null || ResolveLink() == null) return false;

        if (!boarding.Contains(unit)) boarding.Add(unit);

        AttackController attack = unit.GetComponent<AttackController>();
        if (attack != null) attack.targetToAttack = null;

        UnityEngine.AI.NavMeshAgent agent = unit.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.SetDestination(transform.position);
        }
        return true;
    }

    private System.Collections.IEnumerator Transit(GameObject unit, SpiritPortal exit)
    {
        // Step through the veil...
        foreach (Renderer r in unit.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        foreach (Collider c in unit.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        UnityEngine.AI.NavMeshAgent agent = unit.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;
        foreach (Canvas canvas in unit.GetComponentsInChildren<Canvas>(true)) canvas.enabled = false;

        yield return new WaitForSeconds(transitSeconds);

        if (unit == null) yield break;

        // ...and out the other side.
        Vector2 circle = Random.insideUnitCircle.normalized * (exit.entryRadius + 1f);
        Vector3 emerge = exit.transform.position + new Vector3(circle.x, 0f, circle.y);
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(emerge, out hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
        {
            emerge = hit.position;
        }
        unit.transform.position = emerge;

        foreach (Renderer r in unit.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
        foreach (Collider c in unit.GetComponentsInChildren<Collider>(true)) c.enabled = true;
        foreach (Canvas canvas in unit.GetComponentsInChildren<Canvas>(true)) canvas.enabled = true;
        if (agent != null)
        {
            agent.enabled = true;
            if (agent.isOnNavMesh) agent.Warp(emerge);
        }

        Debug.Log($"{unit.name} emerges from the spirit world at {exit.name}.");
    }

    private void Update()
    {
        if (NetworkGuard.BlockLocalSimulation) return;

        // Travelers stepping in.
        for (int i = boarding.Count - 1; i >= 0; i--)
        {
            GameObject unit = boarding[i];
            if (unit == null) { boarding.RemoveAt(i); continue; }

            if (Vector3.Distance(unit.transform.position, transform.position) <= entryRadius)
            {
                boarding.RemoveAt(i);
                SpiritPortal exit = ResolveLink();
                if (exit != null)
                {
                    Debug.Log($"{unit.name} steps into the spirit world...");
                    StartCoroutine(Transit(unit, exit));
                }
            }
        }

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = spawnInterval;

        alive.RemoveAll(s => s == null);
        if (alive.Count >= maxAliveSpirits) return;

        GameObject prefab = Random.value < darkSpiritChance ? darkSpiritPrefab : friendlySpiritPrefab;
        if (prefab == null) prefab = darkSpiritPrefab != null ? darkSpiritPrefab : friendlySpiritPrefab;
        if (prefab == null) return;

        Vector2 circle = Random.insideUnitCircle.normalized * spawnRadius;
        Vector3 pos = transform.position + new Vector3(circle.x, 0f, circle.y);
        alive.Add(Instantiate(prefab, pos, Quaternion.identity));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.8f, 0.3f, 1f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
