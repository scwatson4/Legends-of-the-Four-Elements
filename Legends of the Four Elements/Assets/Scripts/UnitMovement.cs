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
                agent.SetDestination(hit.point);
                directionIndicator.DrawLine(hit);
                Debug.Log($"{gameObject.name} commanded to move to {hit.point}");
            }
        }

        // Handle attack target movement
        if (attackController != null && attackController.targetToAttack != null && !isCommandedToMove)
        {
            float distanceToTarget = Vector3.Distance(transform.position, attackController.targetToAttack.position);
            if (distanceToTarget <= attackController.attackDistance)
            {
                agent.SetDestination(transform.position); // Stop moving
                Debug.Log($"{gameObject.name} within attack distance of {attackController.targetToAttack.name}");
            }
            else
            {
                NavMeshHit navHit;
                if (NavMesh.SamplePosition(attackController.targetToAttack.position, out navHit, 5f, NavMesh.AllAreas))
                {
                    agent.SetDestination(navHit.position);
                    Debug.Log($"{gameObject.name} moving to attack target {attackController.targetToAttack.name}");
                }
            }
        }

        // Clear isCommandedToMove when destination is reached
        if (isCommandedToMove && (agent.hasPath == false || agent.remainingDistance <= agent.stoppingDistance))
        {
            isCommandedToMove = false;
            Debug.Log($"{gameObject.name} reached destination, isCommandedToMove = false");
        }
    }
}