using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// The Air Nomads' famous mobility, auto-added to every airbender:
///
/// AIR SCOOTER - on long ground moves the airbender conjures an air ball
/// and rides it (+60% speed, a swirling greybox sphere underfoot),
/// dismounting as they near the destination.
///
/// STAFF GLIDER - very long move orders make them take off entirely: a
/// FlyingMover carries them smoothly over water, hills, buildings and trees
/// at glide height, steering around true mountains, then they land on the
/// NavMesh at the destination. While airborne they cannot fight and their
/// NavMeshAgent sleeps; landing restores normal ground behaviour.
/// </summary>
[RequireComponent(typeof(Unit))]
public class AirbenderMobility : MonoBehaviour
{
    [Header("Air Scooter (long ground moves)")]
    public float scooterMinDistance = 15f;
    public float scooterSpeedMultiplier = 1.6f;

    [Header("Staff Glider (very long moves)")]
    public float glideMinDistance = 45f;
    public float glideSpeed = 11f;
    public float glideHeight = 7f;

    public bool IsGliding { get; private set; }

    private NavMeshAgent agent;
    private AttackController attackController;
    private FlyingMover glider;
    private GameObject scooterBall;
    private bool scooting;
    private float preScootSpeed;
    private Vector3 glideDestination;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        attackController = GetComponent<AttackController>();
    }

    /// <summary>Called by UnitMovement on move orders. True = we're flying
    /// there ourselves; false = normal ground movement (scooter may assist).</summary>
    public bool ConsiderTravel(Vector3 destination)
    {
        if (IsGliding) { if (glider != null) glider.SetDestination(destination); return true; }
        if (agent == null || Vector3.Distance(transform.position, destination) < glideMinDistance)
        {
            return false;
        }

        TakeOff(destination);
        return true;
    }

    private void TakeOff(Vector3 destination)
    {
        IsGliding = true;
        glideDestination = destination;
        StopScooting();

        if (attackController != null) attackController.targetToAttack = null;
        if (agent != null && agent.enabled) agent.enabled = false;

        if (glider == null)
        {
            glider = gameObject.AddComponent<FlyingMover>();
            glider.OnArrived = Land;
        }
        glider.enabled = true;
        glider.speed = glideSpeed;
        glider.cruiseHeight = glideHeight;
        glider.SetDestination(destination);

        Debug.Log($"{gameObject.name} opens their glider!");
    }

    private void Land()
    {
        // Find solid ground at (or as near as possible to) the destination.
        Vector3 landing = transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(glideDestination, out hit, 15f, NavMesh.AllAreas) ||
            NavMesh.SamplePosition(transform.position, out hit, 30f, NavMesh.AllAreas))
        {
            landing = hit.position;
        }

        if (glider != null) glider.enabled = false;
        transform.position = landing;

        if (agent != null)
        {
            agent.enabled = true;
            if (agent.isOnNavMesh) agent.Warp(landing);
        }

        IsGliding = false;
        Debug.Log($"{gameObject.name} touches down.");
    }

    private void Update()
    {
        if (IsGliding) return;

        // Air scooter: kicks in automatically on long ground journeys.
        bool wantsScooter = agent != null && agent.enabled && agent.isOnNavMesh &&
                            agent.hasPath && agent.remainingDistance > scooterMinDistance;

        if (wantsScooter && !scooting) StartScooting();
        else if (!wantsScooter && scooting) StopScooting();
    }

    private void StartScooting()
    {
        scooting = true;
        preScootSpeed = agent.speed;
        agent.speed = preScootSpeed * scooterSpeedMultiplier;

        if (scooterBall == null)
        {
            scooterBall = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            scooterBall.name = "AirScooter";
            Destroy(scooterBall.GetComponent<Collider>());
            scooterBall.transform.SetParent(transform, false);
            scooterBall.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            scooterBall.transform.localScale = Vector3.one * 1.1f;

            Renderer renderer = scooterBall.GetComponent<Renderer>();
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            Color puff = new Color(0.95f, 0.95f, 1f, 0.4f);
            block.SetColor("_BaseColor", puff);
            block.SetColor("_Color", puff);
            renderer.SetPropertyBlock(block);
        }
        scooterBall.SetActive(true);
    }

    private void StopScooting()
    {
        if (!scooting) return;
        scooting = false;
        if (agent != null && agent.enabled) agent.speed = preScootSpeed;
        if (scooterBall != null) scooterBall.SetActive(false);
    }

    private void OnDestroy()
    {
        if (scooterBall != null) Destroy(scooterBall);
    }
}
