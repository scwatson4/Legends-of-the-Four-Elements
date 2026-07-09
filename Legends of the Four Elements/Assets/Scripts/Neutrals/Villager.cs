using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A neutral tribe villager: wanders near its home, and runs for its life
/// when dark spirits (or anything hostile to neutrals) comes close.
/// Prefab needs: NavMeshAgent, Unit (type Villager, low health) and this.
/// FactionMember is added automatically as NoFaction.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class Villager : MonoBehaviour
{
    [Header("Wandering")]
    public float wanderRadius = 8f;
    public float wanderInterval = 5f;

    [Header("Panic")]
    public float panicRadius = 8f;
    public float fleeDistance = 12f;
    public float fleeSpeedMultiplier = 1.6f;

    [HideInInspector] public Vector3 homePosition;

    private NavMeshAgent agent;
    private float wanderTimer;
    private float baseSpeed;
    private int myFactionId;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        baseSpeed = agent.speed;
        if (homePosition == Vector3.zero) homePosition = transform.position;

        if (GetComponent<FactionMember>() == null)
        {
            FactionUtility.SetFaction(gameObject, FactionManager.NoFaction);
        }
        myFactionId = FactionUtility.GetFactionId(gameObject);

        wanderTimer = Random.Range(0f, wanderInterval);
    }

    private void Update()
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;

        Transform threat = FindNearestThreat();
        if (threat != null)
        {
            Flee(threat);
            return;
        }

        agent.speed = baseSpeed;
        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0f)
        {
            Wander();
            wanderTimer = wanderInterval + Random.Range(-1f, 1f);
        }
    }

    private Transform FindNearestThreat()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, panicRadius);
        Transform closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            Unit other = hit.GetComponentInParent<Unit>();
            if (other == null || other.gameObject == gameObject) continue;

            if (!FactionManager.AreHostile(myFactionId, FactionUtility.GetFactionId(other.gameObject)))
                continue;

            float distance = Vector3.Distance(transform.position, other.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = other.transform;
            }
        }
        return closest;
    }

    private void Flee(Transform threat)
    {
        agent.speed = baseSpeed * fleeSpeedMultiplier;
        Vector3 away = (transform.position - threat.position).normalized * fleeDistance;
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(transform.position + away, out navHit, 5f, NavMesh.AllAreas))
        {
            agent.SetDestination(navHit.position);
        }
    }

    private void Wander()
    {
        Vector2 circle = Random.insideUnitCircle * wanderRadius;
        Vector3 target = homePosition + new Vector3(circle.x, 0f, circle.y);
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(target, out navHit, 3f, NavMesh.AllAreas))
        {
            agent.SetDestination(navHit.position);
        }
    }
}
