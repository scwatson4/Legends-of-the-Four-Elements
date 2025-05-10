using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackController : MonoBehaviour
{
    public Transform targetToAttack;
    public Material idleStateMaterial;
    public Material followStateMaterial;
    public Material attackStateMaterial;
    public Team team;
    public int unitDamage = 10;
    public GameObject flamethrowerEffect;
    public float detectionRadius = 10f;
    public float attackDistance = 1f;

    private UnitMovement unitMovement; // Added to check isCommandedToMove

    private void Start()
    {
        team = GetComponent<Unit>().team;
        unitMovement = GetComponent<UnitMovement>(); // Initialize UnitMovement
    }

    private void OnTriggerEnter(Collider other)
    {
        if (unitMovement != null && unitMovement.isCommandedToMove) return; // Skip if moving

        if (other.CompareTag("Unit"))
        {
            Unit otherUnit = other.GetComponent<Unit>();
            if (otherUnit != null && otherUnit.team != team && targetToAttack == null)
            {
                targetToAttack = other.transform;
                Debug.Log($"{gameObject.name} set target to Unit: {other.name}");
            }
        }
        else if (other.CompareTag("CommandCenter"))
        {
            CommandCenter commandCenter = other.GetComponent<CommandCenter>();
            if (commandCenter != null && commandCenter.team != team && targetToAttack == null)
            {
                targetToAttack = other.transform;
                Debug.Log($"{gameObject.name} set target to CommandCenter: {other.name}");
            }
        }
        else if (other.CompareTag("EnemyCommandCenter"))
        {
            CommandCenter commandCenter = other.GetComponent<CommandCenter>();
            if (commandCenter != null && commandCenter.team != team && targetToAttack == null)
            {
                targetToAttack = other.transform;
                Debug.Log($"{gameObject.name} set target to EnemyCommandCenter: {other.name}");
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (unitMovement != null && unitMovement.isCommandedToMove) return; // Skip if moving

        if (other.CompareTag("Unit"))
        {
            Unit otherUnit = other.GetComponent<Unit>();
            if (otherUnit != null && otherUnit.team != team && targetToAttack == null)
            {
                targetToAttack = other.transform;
                Debug.Log($"{gameObject.name} set target to Unit (stay): {other.name}");
            }
        }
        else if (other.CompareTag("CommandCenter"))
        {
            CommandCenter commandCenter = other.GetComponent<CommandCenter>();
            if (commandCenter != null && commandCenter.team != team && targetToAttack == null)
            {
                targetToAttack = other.transform;
                Debug.Log($"{gameObject.name} set target to CommandCenter (stay): {other.name}");
            }
        }
        else if (other.CompareTag("EnemyCommandCenter"))
        {
            CommandCenter commandCenter = other.GetComponent<CommandCenter>();
            if (commandCenter != null && commandCenter.team != team && targetToAttack == null)
            {
                targetToAttack = other.transform;
                Debug.Log($"{gameObject.name} set target to EnemyCommandCenter (stay): {other.name}");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (targetToAttack != null && targetToAttack == other.transform)
        {
            if (other.CompareTag("Unit"))
            {
                Unit otherUnit = other.GetComponent<Unit>();
                if (otherUnit != null && otherUnit.team != team)
                {
                    targetToAttack = null;
                    Debug.Log($"{gameObject.name} stopped attacking Unit: {other.name}");
                }
            }
            else if (other.CompareTag("CommandCenter"))
            {
                CommandCenter commandCenter = other.GetComponent<CommandCenter>();
                if (commandCenter != null && commandCenter.team != team)
                {
                    targetToAttack = null;
                    Debug.Log($"{gameObject.name} stopped attacking CommandCenter: {other.name}");
                }
            }
            else if (other.CompareTag("EnemyCommandCenter"))
            {
                CommandCenter commandCenter = other.GetComponent<CommandCenter>();
                if (commandCenter != null && commandCenter.team != team)
                {
                    targetToAttack = null;
                    Debug.Log($"{gameObject.name} stopped attacking EnemyCommandCenter: {other.name}");
                }
            }
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