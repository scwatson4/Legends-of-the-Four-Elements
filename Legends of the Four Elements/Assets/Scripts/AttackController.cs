using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackController : MonoBehaviour
{
    public Transform targetToAttack;
    public Material idleStateMaterial;
    public Material followStateMaterial;
    public Material attackStateMaterial;

    [Tooltip("Legacy mirror of Unit.team. Hostility is decided by FactionManager now.")]
    public Team team;
    public int unitDamage = 10;
    public GameObject flamethrowerEffect;
    public float detectionRadius = 10f;
    public float attackDistance = 1f;

    private UnitMovement unitMovement;

    private void Start()
    {
        Unit unit = GetComponent<Unit>();
        if (unit != null) team = unit.team;
        unitMovement = GetComponent<UnitMovement>();
    }

    /// <summary>Faction-aware hostility check (works for all four nations,
    /// neutral villagers, dark spirits, and multiplayer factions).</summary>
    public bool IsHostileTo(GameObject other)
    {
        return FactionUtility.AreHostile(gameObject, other);
    }

    private bool IsValidTarget(Collider other)
    {
        // Units (benders, villagers, spirits)
        Unit otherUnit = other.GetComponentInParent<Unit>();
        if (otherUnit != null)
        {
            return IsHostileTo(otherUnit.gameObject);
        }

        // Structures
        CommandCenter commandCenter = other.GetComponentInParent<CommandCenter>();
        if (commandCenter != null)
        {
            return IsHostileTo(commandCenter.gameObject);
        }

        Structure structure = other.GetComponentInParent<Structure>();
        if (structure != null)
        {
            return IsHostileTo(structure.gameObject);
        }

        return false;
    }

    private void TryAcquireTarget(Collider other)
    {
        if (unitMovement != null && unitMovement.isCommandedToMove) return; // don't interrupt move orders
        if (targetToAttack != null) return;

        if (IsValidTarget(other))
        {
            targetToAttack = other.transform;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryAcquireTarget(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryAcquireTarget(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (targetToAttack != null && targetToAttack == other.transform)
        {
            targetToAttack = null;
        }
    }

    public void SetIdleStateMaterial()
    {
        //GetComponent<Renderer>().material = idleStateMaterial;
    }

    public void SetFollowStateMaterial()
    {
        //GetComponent<Renderer>().material = followStateMaterial;
    }

    public void SetAttackStateMaterial()
    {
        //GetComponent<Renderer>().material = attackStateMaterial;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 1.5f);
    }
}
