using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Turns a unit into a worker: it finds (or is ordered to) a ResourceNode,
/// gathers a load, carries it to the nearest friendly ResourceDropoff, and
/// repeats. Fully autonomous, so it works identically for human players and
/// AI commanders. Right-clicking a node with workers selected assigns them.
///
/// Nation flavor (set the model per prefab): Air Acolyte forager, Water Tribe
/// fisherman, Earth Kingdom miner, Fire Nation coal engineer.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class ResourceCollector : MonoBehaviour
{
    public int carryCapacity = 25;
    public float harvestDuration = 3f;
    public float interactRange = 3.5f;
    public float nodeSearchRadius = 80f;

    [Tooltip("Optional: shown while carrying a load (basket/sack model).")]
    public GameObject carryVisual;

    [Header("Repair")]
    public float repairPerSecond = 8f;
    [Tooltip("Silver per point of health repaired.")]
    public float repairCostPerHp = 0.5f;

    private enum State { Idle, MovingToNode, Harvesting, MovingToDropoff, Repairing }

    private State state = State.Idle;
    private ResourceNode targetNode;
    private ResourceDropoff targetDropoff;
    private Structure repairTarget;
    private float repairTick;
    private NavMeshAgent agent;
    private UnitMovement unitMovement;
    private float harvestTimer;
    private float searchCooldown;
    private int carried;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        unitMovement = GetComponent<UnitMovement>();
        if (carryVisual != null) carryVisual.SetActive(false);
    }

    /// <summary>Player/AI order: work this node.</summary>
    public void SetTargetNode(ResourceNode node)
    {
        targetNode = node;
        repairTarget = null;
        state = node != null ? State.MovingToNode : State.Idle;
    }

    /// <summary>Player order: repair this building (right-click a damaged friendly structure).</summary>
    public void SetRepairTarget(Structure structure)
    {
        if (structure == null || !structure.IsDamaged) return;
        repairTarget = structure;
        state = State.Repairing;
    }

    private void Update()
    {
        // A direct move order from the player always wins; resume after.
        if (unitMovement != null && unitMovement.isCommandedToMove) return;
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        switch (state)
        {
            case State.Idle:
                if (carried > 0) { state = State.MovingToDropoff; break; }
                searchCooldown -= Time.deltaTime;
                if (searchCooldown > 0f) break;
                searchCooldown = 1.5f;
                targetNode = FindNearestNode();
                if (targetNode != null) state = State.MovingToNode;
                break;

            case State.MovingToNode:
                if (targetNode == null || targetNode.IsDepleted) { state = State.Idle; break; }
                if (InRangeOf(targetNode.transform.position))
                {
                    agent.SetDestination(transform.position);
                    harvestTimer = harvestDuration;
                    state = State.Harvesting;
                }
                else
                {
                    agent.SetDestination(targetNode.transform.position);
                }
                break;

            case State.Harvesting:
                if (targetNode == null || targetNode.IsDepleted) { state = State.Idle; break; }
                harvestTimer -= Time.deltaTime;
                if (harvestTimer <= 0f)
                {
                    carried = targetNode.Extract(carryCapacity);
                    if (carryVisual != null) carryVisual.SetActive(carried > 0);
                    state = carried > 0 ? State.MovingToDropoff : State.Idle;
                }
                break;

            case State.MovingToDropoff:
                if (targetDropoff == null) targetDropoff = FindNearestDropoff();
                if (targetDropoff == null) { state = State.Idle; break; }

                if (InRangeOf(targetDropoff.transform.position))
                {
                    Deposit();
                }
                else
                {
                    agent.SetDestination(targetDropoff.transform.position);
                }
                break;

            case State.Repairing:
                if (repairTarget == null || !repairTarget.IsDamaged)
                {
                    repairTarget = null;
                    state = State.Idle; // job done, back to work
                    break;
                }

                if (!InRangeOf(repairTarget.transform.position))
                {
                    agent.SetDestination(repairTarget.transform.position);
                    break;
                }

                agent.SetDestination(transform.position); // hold and hammer
                repairTick -= Time.deltaTime;
                if (repairTick <= 0f)
                {
                    repairTick = 0.5f;
                    float amount = repairPerSecond * 0.5f;
                    int cost = Mathf.CeilToInt(amount * repairCostPerHp);
                    if (Economy.TrySpend(FactionUtility.GetFactionId(gameObject), cost))
                    {
                        repairTarget.Repair(amount);
                    }
                    else
                    {
                        state = State.Idle; // can't afford repairs right now
                    }
                }
                break;
        }
    }

    private void Deposit()
    {
        int factionId = FactionUtility.GetFactionId(gameObject);
        Economy.Award(factionId, carried);
        carried = 0;
        if (carryVisual != null) carryVisual.SetActive(false);

        // Head back for another load (or find a new node if ours ran dry).
        state = targetNode != null && !targetNode.IsDepleted ? State.MovingToNode : State.Idle;
    }

    private bool InRangeOf(Vector3 position)
    {
        position.y = transform.position.y;
        return Vector3.Distance(transform.position, position) <= interactRange +
               (agent != null ? agent.stoppingDistance : 0f);
    }

    private ResourceNode FindNearestNode()
    {
        ResourceNode best = null;
        float bestDistance = nodeSearchRadius;
        foreach (ResourceNode node in FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
        {
            if (node.IsDepleted) continue;
            float distance = Vector3.Distance(transform.position, node.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = node;
            }
        }
        return best;
    }

    private ResourceDropoff FindNearestDropoff()
    {
        int myFaction = FactionUtility.GetFactionId(gameObject);
        ResourceDropoff best = null;
        float bestDistance = Mathf.Infinity;
        foreach (ResourceDropoff dropoff in FindObjectsByType<ResourceDropoff>(FindObjectsSortMode.None))
        {
            if (dropoff.OwnerFactionId != myFaction) continue;
            float distance = Vector3.Distance(transform.position, dropoff.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = dropoff;
            }
        }
        return best;
    }
}
