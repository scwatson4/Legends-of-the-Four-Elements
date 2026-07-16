using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A pier built at the water's edge that launches FISHING BOATS - visible
/// little vessels that shuttle between the pier and the nearest fish shoal,
/// hauling bigger loads than a fisherman on foot. Boats are killable, so
/// raiding a fishery hurts.
///
/// Prefab recipe: pier model + Structure + ResourceDropoff + IncomeBuilding
/// (Requires Nearby Node = FishShoal, so the placement ghost forces it
/// beside the water) + this. Assign a boat prefab, or leave it empty and
/// greybox boats are assembled at runtime.
/// </summary>
[RequireComponent(typeof(ResourceDropoff))]
public class FishingPier : MonoBehaviour
{
    public int boatCount = 2;
    public float launchInterval = 8f;
    [Tooltip("Optional boat prefab (Unit + NavMeshAgent + ResourceCollector). Empty = greybox boat.")]
    public GameObject boatPrefab;
    public int boatCarryCapacity = 40;
    public int boatHealth = 80;
    public float boatSpeed = 4.5f;

    private readonly System.Collections.Generic.List<GameObject> boats =
        new System.Collections.Generic.List<GameObject>();
    private float launchTimer = 2f;

    private void Update()
    {
        if (NetworkGuard.BlockLocalSimulation) return;

        boats.RemoveAll(b => b == null);
        if (boats.Count >= boatCount) return;

        launchTimer -= Time.deltaTime;
        if (launchTimer > 0f) return;
        launchTimer = launchInterval;

        ResourceNode shoal = FindNearestShoal();
        if (shoal == null) return; // fished out

        LaunchBoat(shoal);
    }

    private ResourceNode FindNearestShoal()
    {
        ResourceNode best = null;
        float bestDistance = 60f;
        foreach (ResourceNode node in FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
        {
            if (node.nodeType != ResourceNode.NodeType.FishShoal || node.IsDepleted) continue;
            float distance = Vector3.Distance(transform.position, node.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = node;
            }
        }
        return best;
    }

    private void LaunchBoat(ResourceNode shoal)
    {
        int factionId = FactionUtility.GetFactionId(gameObject);
        Vector3 launchPos = transform.position + transform.forward * 3f;

        GameObject boat;
        if (boatPrefab != null)
        {
            boat = Instantiate(boatPrefab, launchPos, transform.rotation);
        }
        else
        {
            // Greybox boat: a flat hull that reads instantly.
            boat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GreyboxMaterial.Harmonize(boat); // URP-safe material for runtime primitives
            boat.name = "Fishing Boat";
            boat.transform.position = launchPos + Vector3.up * 0.4f;
            boat.transform.localScale = new Vector3(1f, 0.5f, 2.2f);

            NavMeshAgent agent = boat.AddComponent<NavMeshAgent>();
            agent.speed = boatSpeed;
            agent.radius = 0.7f;

            Unit unit = boat.AddComponent<Unit>();
            unit.maxUnitHealth = boatHealth;
            unit.category = UnitCategory.Worker;
            unit.populationCost = 0; // the pier crews them

            ResourceCollector collector = boat.AddComponent<ResourceCollector>();
            collector.carryCapacity = boatCarryCapacity;
            boat.AddComponent<NationColorizer>();
        }

        FactionUtility.SetFaction(boat, factionId);

        ResourceCollector boatCollector = boat.GetComponent<ResourceCollector>();
        if (boatCollector != null) boatCollector.SetTargetNode(shoal);

        boats.Add(boat);
        Debug.Log($"{gameObject.name} launches a fishing boat toward the shoal.");
    }
}
