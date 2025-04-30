using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Unit : MonoBehaviour
{
    private float unitHealth;
    public float maxUnitHealth = 100f;
    public Team team = Team.Player; // Team affiliation

    public HealthTracker healthTracker;

    Animator animator;
    NavMeshAgent navMeshAgent;

    void Start()
    {
        UnitSelectionManager.Instance.allUnitsList.Add(gameObject);

        unitHealth = maxUnitHealth;
        UpdateHealthUI();

        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();

        // Ensure unit is on NavMesh
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
        // Notify UnitSelectionManager to remove this unit
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
            // Play dying animation
            if (animator != null)
            {
                animator.SetTrigger("Die");
            }

            // Play dying sound effect
            SoundManager.Instance.PlayUnitDeathSound();

            if (navMeshAgent != null)
            {
                navMeshAgent.enabled = false; // Disable agent to prevent Update errors
            }

            Destroy(gameObject, 1f); // Delay destruction to allow animation
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
            if (navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance)
            {
                animator.SetBool("isMoving", true);
            }
            else
            {
                animator.SetBool("isMoving", false);
            }
        }
        else
        {
            // Ensure animator reflects stationary state if agent is invalid
            if (animator != null)
            {
                animator.SetBool("isMoving", false);
            }
        }
    }
}