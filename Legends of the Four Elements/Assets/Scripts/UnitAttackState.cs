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
        attackController.flamethrowerEffect.SetActive(true);
        Debug.Log($"{animator.gameObject.name} entered attack state. Target: {attackController.targetToAttack?.name}");
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (attackController.targetToAttack != null && !animator.transform.GetComponent<UnitMovement>().isCommandedToMove)
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

            float distanceFromTarget = Vector3.Distance(attackController.targetToAttack.position, animator.transform.position);
            if (distanceFromTarget > stopAttackingDistance || attackController.targetToAttack == null)
            {
                Debug.Log($"{animator.gameObject.name} exiting attack state. Distance: {distanceFromTarget}, Target: {attackController.targetToAttack?.name}");
                animator.SetBool("isAttacking", false);
            }
        }
        else
        {
            Debug.Log($"{animator.gameObject.name} exiting attack state. Target null or commanded to move.");
            animator.SetBool("isAttacking", false);
        }
    }

    private void Attack()
    {
        if (attackController.targetToAttack == null) return;

        var damageToInflict = attackController.unitDamage;
        SoundManager.Instance.PlayAttackSound(unit.unitType);

        Unit targetUnit = attackController.targetToAttack.GetComponent<Unit>();
        CommandCenter targetCommandCenter = attackController.targetToAttack.GetComponent<CommandCenter>();

        if (targetUnit != null && targetUnit.team != attackController.team)
        {
            targetUnit.TakeDamage(damageToInflict);
            if (targetUnit == null || !targetUnit.gameObject.activeSelf)
            {
                if (enemyAI != null)
                {
                    enemyAI.OnTargetDestroyed();
                }
            }
        }
        else if (targetCommandCenter != null && targetCommandCenter.team != attackController.team)
        {
            targetCommandCenter.TakeDamage(damageToInflict);
            Debug.Log($"{attackController.gameObject.name} attacking CommandCenter: {targetCommandCenter.name}, Damage: {damageToInflict}");
            if (targetCommandCenter == null || !targetCommandCenter.gameObject.activeSelf)
            {
                if (enemyAI != null)
                {
                    enemyAI.OnTargetDestroyed();
                }
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
        attackController.flamethrowerEffect.SetActive(false);
        SoundManager.Instance.StopAttackSound();
        Debug.Log($"{animator.gameObject.name} exited attack state.");
    }
}