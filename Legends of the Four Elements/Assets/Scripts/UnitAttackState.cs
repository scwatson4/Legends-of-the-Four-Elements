using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class UnitAttackState : StateMachineBehaviour
{
    NavMeshAgent agent;
    AttackController attackController;
    public float stopAttackingDistance = 1.5f;
    private float attackRate = 2f; // Attacks per second
    private float attackTimer;
    private EnemyAI enemyAI;
    private Unit unit;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        agent = animator.GetComponent<NavMeshAgent>();
        attackController = animator.GetComponent<AttackController>();
        enemyAI = animator.GetComponent<EnemyAI>();
        unit = animator.GetComponent<Unit>();
        attackController.SetAttackStateMaterial();
        if (attackController.flamethrowerEffect != null)
        {
            attackController.flamethrowerEffect.SetActive(true);
        }
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        UnitMovement movement = animator.transform.GetComponent<UnitMovement>();
        bool commandedToMove = movement != null && movement.isCommandedToMove;

        if (attackController.targetToAttack != null && !commandedToMove)
        {
            LookAtTarget();

            if (attackTimer <= 0)
            {
                Attack();
                attackTimer = 1f / attackRate;
            }
            else
            {
                attackTimer -= Time.deltaTime;
            }

            if (attackController.targetToAttack == null) return;

            float distanceFromTarget = Vector3.Distance(attackController.targetToAttack.position, animator.transform.position);
            if (distanceFromTarget > stopAttackingDistance)
            {
                animator.SetBool("isAttacking", false);
            }
        }
        else
        {
            animator.SetBool("isAttacking", false);
        }
    }

    private void Attack()
    {
        if (attackController.targetToAttack == null) return;

        // Elemental climates buff or weaken the attacker (volcano vs glacier...).
        float biomeMultiplier = BiomeZone.GetAttackMultiplier(attackController.gameObject);
        int damageToInflict = Mathf.Max(1, Mathf.RoundToInt(attackController.unitDamage * biomeMultiplier));

        if (SoundManager.Instance != null && unit != null)
        {
            SoundManager.Instance.PlayAttackSound(unit.unitType);
        }

        Unit targetUnit = attackController.targetToAttack.GetComponent<Unit>();
        CommandCenter targetCommandCenter = attackController.targetToAttack.GetComponent<CommandCenter>();
        Structure targetStructure = attackController.targetToAttack.GetComponent<Structure>();

        if (targetUnit != null && attackController.IsHostileTo(targetUnit.gameObject))
        {
            targetUnit.TakeDamage(damageToInflict);

            // Kill bounty: slaying spirits (or bounty-carrying units) pays silver.
            if (targetUnit.CurrentHealth <= 0 && targetUnit.killBounty > 0)
            {
                Economy.Award(FactionUtility.GetFactionId(attackController.gameObject), targetUnit.killBounty);
            }

            if (targetUnit.CurrentHealth <= 0 && enemyAI != null)
            {
                enemyAI.OnTargetDestroyed();
            }
        }
        else if (targetCommandCenter != null && attackController.IsHostileTo(targetCommandCenter.gameObject))
        {
            targetCommandCenter.TakeDamage(damageToInflict);
        }
        else if (targetStructure != null && attackController.IsHostileTo(targetStructure.gameObject))
        {
            targetStructure.TakeDamage(damageToInflict);

            if (targetStructure.CurrentHealth <= 0)
            {
                if (targetStructure.killBounty > 0)
                {
                    Economy.Award(FactionUtility.GetFactionId(attackController.gameObject), targetStructure.killBounty);
                }
                if (enemyAI != null) enemyAI.OnTargetDestroyed();
            }
        }
    }

    private void LookAtTarget()
    {
        Vector3 direction = attackController.targetToAttack.position - agent.transform.position;
        agent.transform.rotation = Quaternion.LookRotation(direction);
        var yRotation = agent.transform.eulerAngles.y;
        agent.transform.rotation = Quaternion.Euler(0, yRotation, 0);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (attackController.flamethrowerEffect != null)
        {
            attackController.flamethrowerEffect.SetActive(false);
        }
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopAttackSound();
        }
    }
}
