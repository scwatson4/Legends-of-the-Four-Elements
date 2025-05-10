using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    [System.Serializable]
    public class UnitToBuild
    {
        public GameObject prefab;
        public int cost;
        public float buildTime;
    }

    [Header("Spawn Settings")]
    public Vector3 spawnOffset = new Vector3(2f, 0f, 0f);
    public float arcRadius = 4f;
    public float arcAngle = 90f; // Total arc spread in degrees
    public float unitSpacingDegrees = 15f;

    private Queue<UnitToBuild> buildQueue = new Queue<UnitToBuild>();
    private bool isBuilding = false;
    private int builtUnitsThisSession = 0;

    public void QueueUnit(UnitToBuild unit)
    {
        if (PlayerResources.Instance == null)
        {
            Debug.LogWarning("PlayerResources not found.");
            return;
        }

        if (PlayerResources.Instance.SpendCredits(unit.cost))
        {
            buildQueue.Enqueue(unit);
            if (!isBuilding)
                StartCoroutine(ProcessQueue());
        }
        else
        {
            Debug.Log("Not enough credits to queue unit.");
        }
    }

    private IEnumerator ProcessQueue()
    {
        isBuilding = true;
        builtUnitsThisSession = 0;

        while (buildQueue.Count > 0)
        {
            UnitToBuild next = buildQueue.Dequeue();
            Debug.Log($"Building {next.prefab.name}...");

            yield return new WaitForSeconds(next.buildTime);

            Vector3 spawnPos = GetArcSpawnPosition(builtUnitsThisSession);
            Instantiate(next.prefab, spawnPos, Quaternion.identity);

            builtUnitsThisSession++;
        }

        isBuilding = false;
    }

    private Vector3 GetArcSpawnPosition(int index)
    {
        float halfArc = arcAngle / 2f;
        float angleStep = unitSpacingDegrees;

        float angleDeg = -halfArc + (index * angleStep);
        float angleRad = angleDeg * Mathf.Deg2Rad;

        Vector3 arcDirection = new Vector3(Mathf.Sin(angleRad), 0f, Mathf.Cos(angleRad));
        Vector3 arcOffset = arcDirection * arcRadius;

        return transform.position + spawnOffset + arcOffset;
    }
}
