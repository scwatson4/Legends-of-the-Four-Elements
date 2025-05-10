using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommandCenterSpawner : MonoBehaviour
{
    [System.Serializable]
    public class UnitToBuild
    {
        public GameObject prefab;
        public int cost;
        public float buildTime;
    }

    public Vector3 spawnOffset = new Vector3(2f, 0f, 0f);
    private Queue<UnitToBuild> buildQueue = new Queue<UnitToBuild>();
    private bool isBuilding = false;

    public void QueueUnit(UnitToBuild unit)
    {
        if (PlayerResources.Instance.SpendCredits(unit.cost))
        {
            buildQueue.Enqueue(unit);
            if (!isBuilding)
                StartCoroutine(ProcessQueue());
        }
        else
        {
            Debug.Log("Not enough credits.");
        }
    }

    private IEnumerator ProcessQueue()
    {
        isBuilding = true;

        while (buildQueue.Count > 0)
        {
            UnitToBuild next = buildQueue.Dequeue();
            Debug.Log($"Building {next.prefab.name}...");

            yield return new WaitForSeconds(next.buildTime);

            Vector3 spawnPos = transform.position + spawnOffset;
            Instantiate(next.prefab, spawnPos, Quaternion.identity);
            Debug.Log($"{next.prefab.name} built.");
        }

        isBuilding = false;
    }
}
