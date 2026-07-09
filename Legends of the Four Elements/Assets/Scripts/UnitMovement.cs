using UnityEngine;
using UnityEngine.AI;

public class UnitMovement : MonoBehaviour
{
    Camera cam;
    NavMeshAgent agent;
    public LayerMask ground;
    public bool isCommandedToMove;
    DirectionIndicator directionIndicator;
    AttackController attackController;

    private void Start()
    {
        cam = Camera.main;
        agent = GetComponent<NavMeshAgent>();
        directionIndicator = GetComponent<DirectionIndicator>();
        attackController = GetComponent<AttackController>();
    }

    private bool AgentReady =>
        agent != null && agent.enabled && agent.isOnNavMesh;

    private void Update()
    {
        // Handle right-click movement command
        if (Input.GetMouseButtonDown(1))
        {
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, ground))
            {
                if (attackController != null)
                {
                    attackController.targetToAttack = null; // Stop attacking
                }
                isCommandedToMove = true;

                // In multiplayer the server owns the simulation: relay the
                // order instead of moving the local (visual-only) agent.
                if (!NetworkUnit.TryRelayMove(gameObject, hit.point) && AgentReady)
                {
                    agent.SetDestination(hit.point);
                }

                if (directionIndicator != null) directionIndicator.DrawLine(hit);
            }
        }

        if (!AgentReady) return;

        // Handle attack target movement
        if (attackController != null && attackController.targetToAttack != null && !isCommandedToMove)
        {
            float distanceToTarget = Vector3.Distance(transform.position, attackController.targetToAttack.position);
            if (distanceToTarget <= attackController.attackDistance)
            {
                agent.SetDestination(transform.position); // Stop moving
            }
            else
            {
                NavMeshHit navHit;
                if (NavMesh.SamplePosition(attackController.targetToAttack.position, out navHit, 5f, NavMesh.AllAreas))
                {
                    agent.SetDestination(navHit.position);
                }
            }
        }

        // Clear isCommandedToMove when destination is reached
        if (isCommandedToMove && (agent.hasPath == false || agent.remainingDistance <= agent.stoppingDistance))
        {
            isCommandedToMove = false;
        }
    }
}
