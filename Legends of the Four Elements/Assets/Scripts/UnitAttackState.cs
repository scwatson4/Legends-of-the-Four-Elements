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

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        agent = animator.GetComponent<NavMeshAgent>();
        attackController = animator.GetComponent<AttackController>();
        attackController.SetAttackStateMaterial(); // Set the attack state material to red
        attackController.flamethrowerEffect.SetActive(true); // Activate the flamethrower effect
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if(attackController.targetToAttack != null && animator.transform.GetComponent<UnitMovement>().isCommandedToMove == false)
        {
            LookAtTarget();

            // Keep moving towards enemy
            //agent.SetDestination(attackController.targetToAttack.position);

            if(attackTimer <= 0)
            {
                Attack();
                attackTimer = 1f / attackRate; ; // Reset the attack timer
            }
            else
            {
                attackTimer -= Time.deltaTime; // Decrease the attack timer
            }

            // Should unit still attack?
            float distanceFromTarget = Vector3.Distance(attackController.targetToAttack.position, animator.transform.position);
            if (distanceFromTarget > stopAttackingDistance || attackController.targetToAttack == null)
            {
                animator.SetBool("isAttacking", false); // Move to Following state
            }
        }
        else if (attackController.targetToAttack == null)
        {
            animator.SetBool("isAttacking", false); // Move to Following state
        }
    }

    private void Attack()
    {
        var damageToInflict = attackController.unitDamage;

        SoundManager.Instance.PlayInfantryAttackSound(); // Play the attack sound

        // Actually attack the enemy
        attackController.targetToAttack.GetComponent<Unit>().TakeDamage(damageToInflict);
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
        attackController.flamethrowerEffect.SetActive(false); // Deactivate the flamethrower effect
        SoundManager.Instance.StopInfantryAttackSound(); // Stop the attack sound
    }
}
