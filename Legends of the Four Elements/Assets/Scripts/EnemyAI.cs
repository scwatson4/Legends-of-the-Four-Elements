using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Autonomous combat brain for AI-controlled units. Despite the legacy name
/// (kept so existing prefabs stay wired), it now drives units of ANY AI
/// faction - Fire waves, skirmish nations and dark spirits alike. It attacks
/// the nearest hostile unit it finds, and optionally marches on the nearest
/// hostile command center when idle.
/// </summary>
public class EnemyAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private AttackController attackController;
    private Animator animator;
    private Unit unit;

    public float searchInterval = 2f; // How often to search for targets

    [Tooltip("When idle, march on the nearest hostile command center. " +
             "Turn off for defenders and roaming spirits.")]
    public bool chaseCommandCenters = true;

    private float searchTimer;
    private Transform commandCenterTarget;
    private float commandCenterRefreshTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        attackController = GetComponent<AttackController>();
        animator = GetComponent<Animator>();
        unit = GetComponent<Unit>();
        searchTimer = searchInterval;

        RefreshCommandCenterTarget();
    }

    private void RefreshCommandCenterTarget()
    {
        commandCenterTarget = null;
        if (!chaseCommandCenters) return;

        float closestDistance = Mathf.Infinity;
        foreach (CommandCenter cc in FindObjectsByType<CommandCenter>(FindObjectsSortMode.None))
        {
            if (!FactionUtility.AreHostile(gameObject, cc.gameObject)) continue;

            float distance = Vector3.Distance(transform.position, cc.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                commandCenterTarget = cc.transform;
            }
        }
    }

    void Update()
    {
        // Only think for AI-controlled factions (works for legacy Team.Enemy too).
        if (unit == null || !FactionManager.IsAIControlled(unit.FactionId)) return;
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        searchTimer -= Time.deltaTime;
        if (searchTimer <= 0)
        {
            FindNearestEnemyUnit();
            searchTimer = searchInterval;
        }

        // Re-resolve the command center goal now and then (bases get destroyed/built).
        commandCenterRefreshTimer -= Time.deltaTime;
        if (commandCenterRefreshTimer <= 0f || commandCenterTarget == null)
        {
            RefreshCommandCenterTarget();
            commandCenterRefreshTimer = 5f;
        }

        Transform currentTarget = attackController != null && attackController.targetToAttack != null
            ? attackController.targetToAttack
            : commandCenterTarget;

        if (currentTarget != null)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(currentTarget.position, out hit, 5f, NavMesh.AllAreas))
            {
                float distanceToTarget = Vector3.Distance(transform.position, hit.position);
                if (distanceToTarget <= attackController.attackDistance)
                {
                    agent.SetDestination(transform.position); // Stop moving
                    SetAnim("isAttacking", true);
                }
                else
                {
                    agent.SetDestination(hit.position);
                    SetAnim("isFollowing", true);
                    SetAnim("isAttacking", false);
                }
            }
        }
        else
        {
            SetAnim("isFollowing", false);
            SetAnim("isAttacking", false);
        }
    }

    private void SetAnim(string name, bool value)
    {
        if (animator != null) animator.SetBool(name, value);
    }

    void FindNearestEnemyUnit()
    {
        if (attackController == null) return;

        // Keep the current unit target if it is still alive.
        if (attackController.targetToAttack != null &&
            attackController.targetToAttack.GetComponent<Unit>() != null)
        {
            return;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, attackController.detectionRadius);
        Transform closestEnemyUnit = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            Unit targetUnit = hit.GetComponentInParent<Unit>();
            if (targetUnit == null || targetUnit.gameObject == gameObject) continue;

            if (!FactionUtility.AreHostile(gameObject, targetUnit.gameObject)) continue;

            float distance = Vector3.Distance(transform.position, targetUnit.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemyUnit = targetUnit.transform;
            }
        }

        attackController.targetToAttack = closestEnemyUnit;
    }

    public void OnTargetDestroyed()
    {
        if (attackController != null) attackController.targetToAttack = null;
    }
}
