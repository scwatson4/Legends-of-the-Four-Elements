using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Unit : MonoBehaviour
{
    private float unitHealth;
    public float maxUnitHealth = 100f;
    public Team team; // Will be set based on tag

    //NeutralVillage is for villages that don't send out units to attack, but will enagage in combat if attacked
    //HostileVillage is for villages that send out units to attack but not the AI Player
    //Spirit is for spirits that will attack the player and AI Player
    public enum Team { Player, AI_Player, NeutralVillage, HostileVillage, Spirit }

    public HealthTracker healthTracker;

    Animator animator;
    NavMeshAgent navMeshAgent;

    void Start()
    {
        UnitSelectionManager.Instance.allUnitsList.Add(gameObject);

        team = GetTeamFromTag(tag);

        unitHealth = maxUnitHealth;
        UpdateHealthUI();

        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();

        //// Ensure unit is on NavMesh
        //NavMeshHit hit;
        //if (!NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
        //{
        //    Debug.LogWarning("Unit not on NavMesh: " + gameObject.name + " at " + transform.position);
        //}
        //else
        //{
        //    transform.position = hit.position;
        //}
    }

    private void OnDestroy()
    {
        // Notify UnitSelectionManager to remove this unit
        if (UnitSelectionManager.Instance != null)
        {
            UnitSelectionManager.Instance.OnUnitDestroyed(gameObject);
        }
    }

    private Team GetTeamFromTag(string tag)
    {
        switch (tag)
        {
            case "Player":
                return Team.Player;
            case "AI_Player":
                return Team.AI_Player;
            case "NeutralVillage":
                return Team.NeutralVillage;
            case "HostileVillage":
                return Team.HostileVillage;
            case "Spirit":
                return Team.Spirit;
            default:
                Debug.LogWarning($"Unrecognized tag '{tag}' on unit '{name}', defaulting to NeutralVillage.");
                return Team.NeutralVillage;
        }
    }

    public bool IsHostileTo(Team otherTeam)
    {
        return (team == Team.Player && (otherTeam == Team.AI_Player || otherTeam == Team.HostileVillage || otherTeam == Team.Spirit))
            || (team == Team.AI_Player && (otherTeam == Team.Player || otherTeam == Team.Spirit))
            || (team == Team.HostileVillage && otherTeam == Team.Player)
            || (team == Team.Spirit && (otherTeam == Team.Player || otherTeam == Team.AI_Player));
    }

    public bool CanSendWavesOfUnits()
    {
        return team == Team.AI_Player || team == Team.HostileVillage || team == Team.Spirit;
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