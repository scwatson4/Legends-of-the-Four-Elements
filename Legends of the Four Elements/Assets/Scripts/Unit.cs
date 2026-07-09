using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Unit : MonoBehaviour
{
    // Serialized in prefabs - append only, never reorder.
    public enum UnitType
    {
        Airbender = 0,
        Firebender = 1,
        Waterbender = 2,
        Earthbender = 3,
        Villager = 4,
        Spirit = 5
    }

    private float unitHealth;
    public float maxUnitHealth = 100f;

    [Tooltip("Legacy two-team field. Add a FactionMember component for full four-nation/multiplayer ownership.")]
    public Team team = Team.Player;
    public UnitType unitType;

    [Tooltip("Roster category: infantry, animal, vehicle, worker or Avatar. Drives upgrades and AI decisions.")]
    public UnitCategory category = UnitCategory.Infantry;

    [Tooltip("Silver awarded to the killer's faction (spirit energy, war spoils). 0 = none.")]
    public int killBounty = 0;

    public HealthTracker healthTracker;

    /// <summary>Fired with (current, max) whenever health changes. Used by the network sync layer.</summary>
    public event Action<float, float> HealthChanged;

    public float CurrentHealth => unitHealth;
    public float HealthFraction => maxUnitHealth > 0f ? Mathf.Clamp01(unitHealth / maxUnitHealth) : 0f;
    public int FactionId => FactionUtility.GetFactionId(gameObject);

    Animator animator;
    NavMeshAgent navMeshAgent;
    AttackController attackController;
    UnitMovement unitMovement;
    bool isDying;

    void Start()
    {
        if (UnitSelectionManager.Instance != null)
        {
            UnitSelectionManager.Instance.allUnitsList.Add(gameObject);
        }

        unitHealth = maxUnitHealth;
        UpdateHealthUI();

        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        attackController = GetComponent<AttackController>();
        unitMovement = GetComponent<UnitMovement>();

        NavMeshHit hit;
        if (!NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
        {
            Debug.LogWarning("Unit not on NavMesh: " + gameObject.name + " at " + transform.position);
        }
        else
        {
            transform.position = hit.position;
        }

        // Apply any upgrade levels the owning faction has already purchased.
        UpgradeManager.ApplyTo(this);
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
        if (healthTracker != null)
        {
            healthTracker.UpdateSliderValue(unitHealth, maxUnitHealth);
        }

        if (unitHealth <= 0 && !isDying)
        {
            isDying = true;

            if (animator != null)
            {
                animator.SetTrigger("Die");
            }

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayUnitDeathSound();
            }

            if (navMeshAgent != null)
            {
                navMeshAgent.enabled = false;
            }

            Destroy(gameObject, 1f);
        }
    }

    internal void TakeDamage(int damageToInflict)
    {
        // In multiplayer only the server applies damage; clients receive the
        // result through NetworkUnit's synced health.
        if (NetworkGuard.BlockLocalSimulation) return;

        ApplyHealth(unitHealth - damageToInflict);
    }

    /// <summary>Used by the network layer to mirror the server's health on clients.</summary>
    internal void SetHealthFromNetwork(float value)
    {
        ApplyHealth(value);
    }

    /// <summary>Restores health (waterbending healers), capped at max.</summary>
    internal void Heal(int amount)
    {
        if (NetworkGuard.BlockLocalSimulation) return;
        if (isDying || unitHealth >= maxUnitHealth) return;

        ApplyHealth(Mathf.Min(maxUnitHealth, unitHealth + amount));
    }

    /// <summary>Upgrade system: rescale max health, keeping the same health fraction.</summary>
    internal void SetMaxHealth(float newMax)
    {
        if (newMax <= 0f) return;
        float fraction = HealthFraction;
        maxUnitHealth = newMax;
        ApplyHealth(newMax * fraction);
    }

    private void ApplyHealth(float value)
    {
        unitHealth = value;
        UpdateHealthUI();
        HealthChanged?.Invoke(unitHealth, maxUnitHealth);
    }

    private void Update()
    {
        if (animator == null) return;

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

            // Attack animation for directly-commanded units. AI-controlled
            // units are animated by their EnemyAI brain instead.
            bool aiControlled = FactionManager.IsAIControlled(FactionId);
            if (!aiControlled)
            {
                if (attackController != null && attackController.targetToAttack != null)
                {
                    float distanceToTarget = Vector3.Distance(transform.position, attackController.targetToAttack.position);
                    if (distanceToTarget <= attackController.attackDistance && !(unitMovement != null && unitMovement.isCommandedToMove))
                    {
                        animator.SetBool("isAttacking", true);
                    }
                    else
                    {
                        animator.SetBool("isAttacking", false);
                    }
                }
                else
                {
                    animator.SetBool("isAttacking", false);
                }
            }
        }
        else
        {
            animator.SetBool("isMoving", false);
            animator.SetBool("isAttacking", false);
        }
    }
}
