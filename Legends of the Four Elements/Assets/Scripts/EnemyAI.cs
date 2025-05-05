using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private AttackController attackController;
    private Animator animator;
    private Unit unit;
    public float searchInterval = 2f; // How often to search for targets
    private float searchTimer;
    private Transform commandCenterTarget; // Primary target (command center)
    private bool isTargetingCommandCenter = true;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        attackController = GetComponent<AttackController>();
        animator = GetComponent<Animator>();
        unit = GetComponent<Unit>();
        searchTimer = searchInterval;

        // Find the command center at start
        GameObject commandCenter = GameObject.FindGameObjectWithTag("CommandCenter");
        if (commandCenter != null)
        {
            commandCenterTarget = commandCenter.transform;
        }
        else
        {
            Debug.LogWarning("No CommandCenter found in the scene!");
        }
    }

    void Update()
    {
        if (unit.team != Team.Enemy) return; // Only run for enemy units

        searchTimer -= Time.deltaTime;
        if (searchTimer <= 0)
        {
            FindNearestEnemyUnit();
            searchTimer = searchInterval;
        }

        // Determine current target
        Transform currentTarget = attackController.targetToAttack != null ? attackController.targetToAttack : commandCenterTarget;

        if (currentTarget != null)
        {
            // Check if target position is on NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(currentTarget.position, out hit, 5f, NavMesh.AllAreas))
            {
                float distanceToTarget = Vector3.Distance(transform.position, hit.position);
                if (distanceToTarget <= attackController.attackDistance)
                {
                    agent.SetDestination(transform.position); // Stop moving
                    animator.SetBool("isAttacking", true);
                    isTargetingCommandCenter = (currentTarget == commandCenterTarget);
                }
                else
                {
                    agent.SetDestination(hit.position);
                    animator.SetBool("isFollowing", true);
                    animator.SetBool("isAttacking", false);
                    isTargetingCommandCenter = (currentTarget == commandCenterTarget);
                }
            }
            else
            {
                Debug.LogWarning("Target position is off NavMesh: " + currentTarget.position);
            }
        }
        else
        {
            animator.SetBool("isFollowing", false);
            animator.SetBool("isAttacking", false);
            isTargetingCommandCenter = true; // Revert to command center if no target
        }
    }

    void FindNearestEnemyUnit()
    {
        // Only search for enemy units if not already attacking one
        if (attackController.targetToAttack != null && attackController.targetToAttack.GetComponent<Unit>() != null)
        {
            return;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, attackController.detectionRadius);
        Transform closestEnemyUnit = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            Unit targetUnit = hit.GetComponent<Unit>();
            if (targetUnit != null && targetUnit.team != unit.team)
            {
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemyUnit = hit.transform;
                }
            }
        }

        // If an enemy unit is found, prioritize it over the command center
        if (closestEnemyUnit != null)
        {
            attackController.targetToAttack = closestEnemyUnit;
        }
        else
        {
            // Revert to command center if no enemy units are nearby
            attackController.targetToAttack = null;
        }
    }

    public void OnTargetDestroyed()
    {
        // Called when the current target is destroyed
        attackController.targetToAttack = null;
        isTargetingCommandCenter = true; // Resume targeting command center
    }
}