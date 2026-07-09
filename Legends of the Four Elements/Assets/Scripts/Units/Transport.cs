using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Garrison/transport: Sky Bison and War Balloons can carry infantry.
/// Right-click the transport with units selected - they run over and climb
/// aboard (their GameObjects deactivate). Select the transport and press U
/// (or wire a button to UnloadAll) to deploy everyone around it. If the
/// transport dies, survivors bail out where it fell.
/// Add to the transport prefab (which is itself a normal Unit).
/// </summary>
[RequireComponent(typeof(Unit))]
public class Transport : MonoBehaviour
{
    public int capacity = 4;
    public float boardRadius = 4f;
    public KeyCode unloadKey = KeyCode.U;
    [Tooltip("Only infantry/workers can ride by default; animals and vehicles cannot.")]
    public bool infantryOnly = true;

    private readonly List<GameObject> passengers = new List<GameObject>();
    private readonly List<GameObject> boarding = new List<GameObject>();

    public int PassengerCount => passengers.Count;
    public bool HasRoom => passengers.Count + boarding.Count < capacity;

    /// <summary>Orders a unit to run over and board.</summary>
    public bool OrderBoard(GameObject unit)
    {
        if (unit == null || unit == gameObject || !HasRoom) return false;
        if (unit.GetComponent<Transport>() != null) return false; // no transport-ception

        Unit u = unit.GetComponent<Unit>();
        if (u == null) return false;
        if (infantryOnly && u.category != UnitCategory.Infantry && u.category != UnitCategory.Worker)
        {
            return false;
        }
        // Same side only.
        if (FactionUtility.GetFactionId(unit) != FactionUtility.GetFactionId(gameObject)) return false;

        if (!boarding.Contains(unit)) boarding.Add(unit);

        AttackController attack = unit.GetComponent<AttackController>();
        if (attack != null) attack.targetToAttack = null;

        NavMeshAgent agent = unit.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.SetDestination(transform.position);
        }
        return true;
    }

    private void Update()
    {
        // Passengers climbing aboard.
        for (int i = boarding.Count - 1; i >= 0; i--)
        {
            GameObject unit = boarding[i];
            if (unit == null) { boarding.RemoveAt(i); continue; }

            if (Vector3.Distance(unit.transform.position, transform.position) <= boardRadius)
            {
                boarding.RemoveAt(i);
                Board(unit);
            }
            else
            {
                // Keep chasing the transport (it may be moving).
                NavMeshAgent agent = unit.GetComponent<NavMeshAgent>();
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.SetDestination(transform.position);
                }
            }
        }

        // Unload when the selected transport's owner presses U.
        if (passengers.Count > 0 && Input.GetKeyDown(unloadKey) &&
            UnitSelectionManager.Instance != null &&
            UnitSelectionManager.Instance.selectedUnitsList.Contains(gameObject))
        {
            UnloadAll();
        }
    }

    private void Board(GameObject unit)
    {
        if (!HasRoom) return;

        passengers.Add(unit);
        if (UnitSelectionManager.Instance != null)
        {
            UnitSelectionManager.Instance.OnUnitDestroyed(unit); // deselect + delist while aboard
        }
        unit.SetActive(false);
        Debug.Log($"{unit.name} boarded {gameObject.name} ({passengers.Count}/{capacity}).");
    }

    /// <summary>Deploys every passenger in a ring around the transport.</summary>
    public void UnloadAll()
    {
        for (int i = 0; i < passengers.Count; i++)
        {
            GameObject unit = passengers[i];
            if (unit == null) continue;

            float angle = (360f / Mathf.Max(1, passengers.Count)) * i * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * (boardRadius + 1f);
            Vector3 dropPos = transform.position + offset;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(dropPos, out hit, 8f, NavMesh.AllAreas)) dropPos = hit.position;

            unit.transform.position = dropPos;
            unit.SetActive(true);

            NavMeshAgent agent = unit.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled) agent.Warp(dropPos);

            if (UnitSelectionManager.Instance != null &&
                !UnitSelectionManager.Instance.allUnitsList.Contains(unit))
            {
                UnitSelectionManager.Instance.allUnitsList.Add(unit);
            }
        }

        Debug.Log($"{gameObject.name} deployed {passengers.Count} unit(s).");
        passengers.Clear();
    }

    private void OnDestroy()
    {
        // Shot down: survivors bail out where it fell.
        foreach (GameObject unit in passengers)
        {
            if (unit == null) continue;
            unit.transform.position = transform.position;
            unit.SetActive(true);

            NavMeshAgent agent = unit.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled) agent.Warp(transform.position);

            if (UnitSelectionManager.Instance != null &&
                !UnitSelectionManager.Instance.allUnitsList.Contains(unit))
            {
                UnitSelectionManager.Instance.allUnitsList.Add(unit);
            }
        }
        passengers.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, boardRadius);
    }
}
