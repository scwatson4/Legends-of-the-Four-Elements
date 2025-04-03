using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackController : MonoBehaviour
{

    public Transform targetToAttack;

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Enemy") && targetToAttack == null)
        {
            targetToAttack = other.transform;
            // Add attack logic here
            Debug.Log("Attacking " + targetToAttack.name);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Enemy") && targetToAttack != null)
        {
            targetToAttack = null;
            // Stop attack logic here
            Debug.Log("Stopped attacking " + other.name);
        }
    }
}
