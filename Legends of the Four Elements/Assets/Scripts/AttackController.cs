using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackController : MonoBehaviour
{
    public Transform targetToAttack;
    public Material idleStateMaterial;
    public Material followStateMaterial;
    public Material attackStateMaterial;
    public Team team; // Team affiliation
    public int unitDamage = 10;
    public GameObject flamethrowerEffect;
    public float detectionRadius = 10f; // Radius to detect enemies
    public float attackDistance = 1f; // Distance to start attacking

    private void Start()
    {
        team = GetComponent<Unit>().team; // Sync with Unit's team
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Unit"))
        {
            Unit otherUnit = other.GetComponent<Unit>();
            if (otherUnit != null && otherUnit.team != team && targetToAttack == null)
            {
                targetToAttack = other.transform;
            }
        }
        else if (other.CompareTag("CommandCenter"))
        {
            CommandCenter commandCenter = other.GetComponent<CommandCenter>();
            if (commandCenter != null && commandCenter.team != team && targetToAttack == null)
            {
                targetToAttack = other.transform;
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Unit"))
        {
            Unit otherUnit = other.GetComponent<Unit>();
            if (otherUnit != null && otherUnit.team != team && targetToAttack == null)
            {
                targetToAttack = other.transform;
            }
        }
        else if (other.CompareTag("CommandCenter"))
        {
            CommandCenter commandCenter = other.GetComponent<CommandCenter>();
            if (commandCenter != null && commandCenter.team != team && targetToAttack == null)
            {
                targetToAttack = other.transform;
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
                    Debug.Log("Stopped attacking " + other.name);
                }
            }
            else if (other.CompareTag("CommandCenter"))
            {
                CommandCenter commandCenter = other.GetComponent<CommandCenter>();
                if (commandCenter != null && commandCenter.team != team)
                {
                    targetToAttack = null;
                    Debug.Log("Stopped attacking " + other.name);
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
        Gizmos.DrawWireSphere(transform.position, detectionRadius); // Detection radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance); // Attack distance
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 1.5f); // Stop attack distance
    }
}