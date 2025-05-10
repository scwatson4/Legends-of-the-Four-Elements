using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Unit : MonoBehaviour
{

    public enum UnitType
    {
        Airbender,
        Firebender
    }

    private float unitHealth;
    public float maxUnitHealth = 100f;
    public Team team = Team.Player;
    public UnitType unitType;

    public HealthTracker healthTracker;

    Animator animator;
    NavMeshAgent navMeshAgent;
    AttackController attackController;
    UnitMovement unitMovement;

    void Start()
    {
        UnitSelectionManager.Instance.allUnitsList.Add(gameObject);

        unitHealth = maxUnitHealth;
        UpdateHealthUI();

        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        attackController = GetComponent<AttackController>();
        unitMovement = GetComponent<UnitMovement>();

        if (unitMovement == null)
        {
            Debug.LogError($"{gameObject.name} is missing UnitMovement component!");
        }

        NavMeshHit hit;
        if (!NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
        {
            Debug.LogWarning("Unit not on NavMesh: " + gameObject.name + " at " + transform.position);
        }
        else
        {
            transform.position = hit.position;
        }
    }

    private void OnDestroy()
    {
        if (UnitSelectionManager.Instance != null)
        {
            UnitSelectionManager.Instance.OnUnitDestroyed(gameObject);
        }
    }

    private void UpdateHealthUI()
    {
        healthTracker.UpdateSliderValue(unitHealth, maxUnitHealth);

        if (unitHealth <= 0)
        {
            if (animator != null)
            {
                animator.SetTrigger("Die");
            }

            SoundManager.Instance.PlayUnitDeathSound();

            if (navMeshAgent != null)
            {
                navMeshAgent.enabled = false;
            }

            Destroy(gameObject, 1f);
        }
    }

    internal void TakeDamage(int damageToInflict)
    {
        unitHealth -= damageToInflict;
        UpdateHealthUI();
    }

    private void Update()
    {
        if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
        {
            // Handle movement animation
            if (navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance)
            {
                animator.SetBool("isMoving", true);
            }
            else
            {
                animator.SetBool("isMoving", false);
            }

            // Handle attacking for player units
            if (team == Team.Player && attackController != null && attackController.targetToAttack != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, attackController.targetToAttack.position);
                if (distanceToTarget <= attackController.attackDistance && !(unitMovement != null && unitMovement.isCommandedToMove))
                {
                    animator.SetBool("isAttacking", true);
                    //Debug.Log($"{gameObject.name} setting isAttacking = true for target {attackController.targetToAttack.name}");
                }
                else
                {
                    animator.SetBool("isAttacking", false);
                    //Debug.Log($"{gameObject.name} setting isAttacking = false (distance: {distanceToTarget}, commanded: {unitMovement?.isCommandedToMove})");
                }
            }
            else
            {
                animator.SetBool("isAttacking", false);
                if (unitMovement != null && unitMovement.isCommandedToMove)
                {
                    //Debug.Log($"{gameObject.name} stopping attack due to movement command");
                }
            }
        }
        else
        {
            if (animator != null)
            {
                animator.SetBool("isMoving", false);
                animator.SetBool("isAttacking", false);
            }
        }
    }
}