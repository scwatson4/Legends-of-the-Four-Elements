using UnityEngine;

public class SpawnButton : MonoBehaviour
{
    public UnitSpawner unitSpawner;
    public GameObject unitPrefab;
    public int cost = 100;
    public float buildTime = 3f;

    public void Spawn()
    {
        if (unitSpawner == null || unitPrefab == null)
        {
            Debug.LogWarning("Spawner or prefab not assigned.");
            return;
        }

        UnitSpawner.UnitToBuild unit = new UnitSpawner.UnitToBuild
        {
            prefab = unitPrefab,
            cost = cost,
            buildTime = buildTime
        };

        unitSpawner.QueueUnit(unit);
    }
}
