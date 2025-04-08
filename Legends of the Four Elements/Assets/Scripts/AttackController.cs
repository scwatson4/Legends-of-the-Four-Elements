using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackController : MonoBehaviour
{

    public Transform targetToAttack;

    public Material idleStateMaterial;
    public Material followStateMaterial;
    public Material attackStateMaterial;

    public bool isPlayer;

    public int unitDamage;

    public GameObject flamethrowerEffect;

    private void OnTriggerEnter(Collider other)
    {
        if(isPlayer && other.CompareTag("Enemy") && targetToAttack == null)
        {
            targetToAttack = other.transform;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (isPlayer && other.CompareTag("Enemy") && targetToAttack == null)
        {
            targetToAttack = other.transform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(isPlayer && other.CompareTag("Enemy") && targetToAttack != null)
        {
            targetToAttack = null;
            // Stop attack logic here
            Debug.Log("Stopped attacking " + other.name);
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
        // Follow Distance
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 10f*0.2f); // Follow Distance Area

        //Attack Distance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 1f); // Attack Distance Area

        //Stop Attack Distance  / Area
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 1.5f); // Stop Attack Distance Area
    }
}
