using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A spirit roaming the map. Friendly spirits drift around peacefully and can
/// be befriended right away. Dark spirits attack everything - players, AI
/// nations and villagers - and can only be tamed once weakened.
///
/// Prefab needs: NavMeshAgent, Unit (type Spirit), AttackController + collider
/// trigger (for dark spirits), Tameable, and this. FactionMember and EnemyAI
/// are added automatically to match the alignment.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class Spirit : MonoBehaviour
{
    public enum Alignment
    {
        Friendly = 0,
        Dark = 1
    }

    public Alignment alignment = Alignment.Friendly;

    [Header("Wandering")]
    public float wanderRadius = 15f;
    public float wanderInterval = 6f;

    [HideInInspector] public bool isTamed;

    private NavMeshAgent agent;
    private AttackController attackController;
    private float wanderTimer;
    private Vector3 homePosition;

    private void Awake()
    {
        // Alignment decides the starting faction.
        int factionId = alignment == Alignment.Dark
            ? FactionManager.HostileSpiritsFaction
            : FactionManager.NoFaction;
        FactionUtility.SetFaction(gameObject, factionId);

        if (alignment == Alignment.Dark && GetComponent<EnemyAI>() == null)
        {
            EnemyAI brain = gameObject.AddComponent<EnemyAI>();
            brain.chaseCommandCenters = false; // raid what it stumbles into
        }
    }

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        attackController = GetComponent<AttackController>();
        homePosition = transform.position;
        wanderTimer = Random.Range(0f, wanderInterval);
    }

    private void Update()
    {
        if (isTamed) return; // the player commands it now
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        // Don't wander away while fighting.
        if (attackController != null && attackController.targetToAttack != null) return;

        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0f)
        {
            Wander();
            wanderTimer = wanderInterval + Random.Range(-1f, 1f);
        }
    }

    private void Wander()
    {
        Vector2 circle = Random.insideUnitCircle * wanderRadius;
        Vector3 target = homePosition + new Vector3(circle.x, 0f, circle.y);
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(target, out navHit, 5f, NavMesh.AllAreas))
        {
            agent.SetDestination(navHit.position);
        }
    }

    /// <summary>Called by Tameable when the spirit joins a faction.</summary>
    public void OnTamed(int newFactionId)
    {
        isTamed = true;

        if (attackController != null) attackController.targetToAttack = null;

        EnemyAI brain = GetComponent<EnemyAI>();
        if (brain != null)
        {
            // Player-owned spirits are driven by orders; AI-owned keep the brain.
            brain.enabled = FactionManager.IsAIControlled(newFactionId);
            brain.chaseCommandCenters = false;
        }

        // Make it orderable like any other unit.
        UnitMovement movement = GetComponent<UnitMovement>();
        if (movement == null) movement = gameObject.AddComponent<UnitMovement>();
        movement.enabled = false; // UnitSelectionManager enables it on selection
    }
}
